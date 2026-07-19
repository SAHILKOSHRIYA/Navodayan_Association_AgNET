using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Domain.Entities;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Announcements;

public sealed record AnnouncementDto(Guid Id, string Title, string Body, AnnouncementCategory Category,
    AnnouncementAudience Audience, DateTime? PublishedAt, DateTime CreatedAt);

public sealed record UpsertAnnouncementDto(string Title, string Body, AnnouncementCategory Category,
    AnnouncementAudience Audience, bool Publish);

/// <summary>
/// Lists announcements the viewer is allowed to see. Public for everyone; Members for signed-in
/// users; Students for the student role. Admins (IncludeAll) also see unpublished drafts.
/// </summary>
public sealed record ListAnnouncementsQuery(
    AnnouncementCategory? Category, bool IsAuthenticated, bool IsStudent, bool IncludeAll, int Page, int PageSize)
    : IRequest<PagedResult<AnnouncementDto>>;

public sealed class ListAnnouncementsHandler(IAppDbContext db) : IRequestHandler<ListAnnouncementsQuery, PagedResult<AnnouncementDto>>
{
    public async Task<PagedResult<AnnouncementDto>> Handle(ListAnnouncementsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 50);

        var query = db.Announcements.AsQueryable();
        if (!q.IncludeAll)
        {
            query = query.Where(a => a.PublishedAt != null);
            var audiences = new List<AnnouncementAudience> { AnnouncementAudience.Public };
            if (q.IsAuthenticated) audiences.Add(AnnouncementAudience.Members);
            if (q.IsStudent) audiences.Add(AnnouncementAudience.Students);
            query = query.Where(a => audiences.Contains(a.Audience));
        }
        if (q.Category is AnnouncementCategory cat) query = query.Where(a => a.Category == cat);

        query = query.OrderByDescending(a => a.PublishedAt ?? a.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * size).Take(size)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Body, a.Category, a.Audience, a.PublishedAt, a.CreatedAt))
            .ToListAsync(ct);
        return new PagedResult<AnnouncementDto>(items, page, size, total);
    }
}

public sealed record CreateAnnouncementCommand(Guid AdminId, UpsertAnnouncementDto Data) : IRequest<Guid>;

public sealed class CreateAnnouncementHandler(IAppDbContext db) : IRequestHandler<CreateAnnouncementCommand, Guid>
{
    public async Task<Guid> Handle(CreateAnnouncementCommand cmd, CancellationToken ct)
    {
        var school = await db.Schools.FirstOrDefaultAsync(s => s.IsActive, ct)
            ?? throw new DomainRuleException("No active school is configured.");
        var d = cmd.Data;
        var a = new Announcement
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Title = d.Title.Trim(),
            Body = d.Body.Trim(),
            Category = d.Category,
            Audience = d.Audience,
            PublishedAt = d.Publish ? DateTime.UtcNow : null,
            CreatedBy = cmd.AdminId,
        };
        db.Announcements.Add(a);
        await db.SaveChangesAsync(ct);
        return a.Id;
    }
}

public sealed record UpdateAnnouncementCommand(Guid Id, UpsertAnnouncementDto Data) : IRequest;

public sealed class UpdateAnnouncementHandler(IAppDbContext db) : IRequestHandler<UpdateAnnouncementCommand>
{
    public async Task Handle(UpdateAnnouncementCommand cmd, CancellationToken ct)
    {
        var a = await db.Announcements.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            ?? throw new NotFoundException("Announcement", cmd.Id);
        var d = cmd.Data;
        a.Title = d.Title.Trim();
        a.Body = d.Body.Trim();
        a.Category = d.Category;
        a.Audience = d.Audience;
        // Publishing is sticky: keep original publish time unless publishing for the first time.
        if (d.Publish && a.PublishedAt is null) a.PublishedAt = DateTime.UtcNow;
        if (!d.Publish) a.PublishedAt = null;
        a.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

public sealed record DeleteAnnouncementCommand(Guid Id) : IRequest;

public sealed class DeleteAnnouncementHandler(IAppDbContext db) : IRequestHandler<DeleteAnnouncementCommand>
{
    public async Task Handle(DeleteAnnouncementCommand cmd, CancellationToken ct)
    {
        var a = await db.Announcements.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            ?? throw new NotFoundException("Announcement", cmd.Id);
        db.Announcements.Remove(a);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class UpsertAnnouncementValidator : AbstractValidator<UpsertAnnouncementDto>
{
    public UpsertAnnouncementValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(8000);
    }
}

public sealed class CreateAnnouncementValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementValidator() => RuleFor(x => x.Data).SetValidator(new UpsertAnnouncementValidator());
}

public sealed class UpdateAnnouncementValidator : AbstractValidator<UpdateAnnouncementCommand>
{
    public UpdateAnnouncementValidator() => RuleFor(x => x.Data).SetValidator(new UpsertAnnouncementValidator());
}
