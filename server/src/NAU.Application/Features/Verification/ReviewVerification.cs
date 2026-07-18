using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Verification;

/// <summary>Admin: paged queue of pending verification requests, oldest first.</summary>
public sealed record GetVerificationQueueQuery(int Page, int PageSize) : IRequest<PagedResult<VerificationQueueItemDto>>;

public sealed class GetVerificationQueueHandler(IAppDbContext db)
    : IRequestHandler<GetVerificationQueueQuery, PagedResult<VerificationQueueItemDto>>
{
    public async Task<PagedResult<VerificationQueueItemDto>> Handle(GetVerificationQueueQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var baseQuery =
            from r in db.VerificationRequests.Where(x => x.Status == VerificationStatus.Pending)
            join p in db.AlumniProfiles on r.UserId equals p.UserId
            join u in db.Users on r.UserId equals u.Id
            orderby r.SubmittedAt
            select new VerificationQueueItemDto(
                r.Id, r.UserId, p.Id, u.FullName, u.Email, p.Batch, p.House,
                p.CurrentCity, p.Company, p.Designation, p.CompletionPct, r.SubmittedAt);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery.Skip((page - 1) * size).Take(size).ToListAsync(ct);

        return new PagedResult<VerificationQueueItemDto>(items, page, size, total);
    }
}

/// <summary>Admin approves a pending request → grants the verified badge.</summary>
public sealed record ApproveVerificationCommand(Guid RequestId, Guid AdminId, string? Notes) : IRequest;

public sealed class ApproveVerificationHandler(IAppDbContext db) : IRequestHandler<ApproveVerificationCommand>
{
    public async Task Handle(ApproveVerificationCommand cmd, CancellationToken ct)
    {
        var request = await db.VerificationRequests.FirstOrDefaultAsync(r => r.Id == cmd.RequestId, ct)
            ?? throw new NotFoundException("VerificationRequest", cmd.RequestId);

        if (request.Status != VerificationStatus.Pending)
            throw new ConflictException("This request has already been reviewed.");

        var profile = await db.AlumniProfiles.FirstOrDefaultAsync(p => p.UserId == request.UserId, ct)
            ?? throw new NotFoundException("Profile", request.UserId);

        request.Status = VerificationStatus.Approved;
        request.ReviewedBy = cmd.AdminId;
        request.ReviewedAt = DateTime.UtcNow;
        request.AdminNotes = cmd.Notes?.Trim();

        profile.IsVerified = true;
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Admin rejects a pending request with a required reason.</summary>
public sealed record RejectVerificationCommand(Guid RequestId, Guid AdminId, string Reason) : IRequest;

public sealed class RejectVerificationHandler(IAppDbContext db) : IRequestHandler<RejectVerificationCommand>
{
    public async Task Handle(RejectVerificationCommand cmd, CancellationToken ct)
    {
        var request = await db.VerificationRequests.FirstOrDefaultAsync(r => r.Id == cmd.RequestId, ct)
            ?? throw new NotFoundException("VerificationRequest", cmd.RequestId);

        if (request.Status != VerificationStatus.Pending)
            throw new ConflictException("This request has already been reviewed.");

        request.Status = VerificationStatus.Rejected;
        request.ReviewedBy = cmd.AdminId;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectionReason = cmd.Reason.Trim();

        await db.SaveChangesAsync(ct);
    }
}
