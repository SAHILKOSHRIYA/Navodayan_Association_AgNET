using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Domain.Entities;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Verification;

/// <summary>Alumnus submits their profile for admin verification (Phase 3 J1).</summary>
public sealed record SubmitVerificationCommand(Guid UserId) : IRequest<VerificationRequestDto>;

public sealed class SubmitVerificationHandler(IAppDbContext db)
    : IRequestHandler<SubmitVerificationCommand, VerificationRequestDto>
{
    // A reasonably complete profile is required before review is worthwhile.
    private const int MinCompletion = 60;

    public async Task<VerificationRequestDto> Handle(SubmitVerificationCommand cmd, CancellationToken ct)
    {
        var profile = await db.AlumniProfiles.FirstOrDefaultAsync(p => p.UserId == cmd.UserId, ct)
            ?? throw new DomainRuleException("Complete your profile before requesting verification.");

        if (profile.IsVerified)
            throw new ConflictException("Your profile is already verified.");

        if (profile.CompletionPct < MinCompletion)
            throw new DomainRuleException($"Please complete at least {MinCompletion}% of your profile before requesting verification.");

        var hasPending = await db.VerificationRequests
            .AnyAsync(r => r.UserId == cmd.UserId && r.Status == VerificationStatus.Pending, ct);
        if (hasPending)
            throw new ConflictException("You already have a verification request under review.");

        var request = new VerificationRequest { Id = Guid.NewGuid(), UserId = cmd.UserId };
        db.VerificationRequests.Add(request);
        await db.SaveChangesAsync(ct);

        return new VerificationRequestDto(request.Id, request.Status, request.SubmittedAt, null, null);
    }
}

/// <summary>The caller's latest verification request (status screen), or null if never submitted.</summary>
public sealed record GetMyVerificationQuery(Guid UserId) : IRequest<VerificationRequestDto?>;

public sealed class GetMyVerificationHandler(IAppDbContext db)
    : IRequestHandler<GetMyVerificationQuery, VerificationRequestDto?>
{
    public async Task<VerificationRequestDto?> Handle(GetMyVerificationQuery q, CancellationToken ct)
    {
        var r = await db.VerificationRequests
            .Where(x => x.UserId == q.UserId)
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync(ct);

        return r is null ? null
            : new VerificationRequestDto(r.Id, r.Status, r.SubmittedAt, r.ReviewedAt, r.RejectionReason);
    }
}
