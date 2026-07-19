using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Domain.Entities;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Events;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record EventCardDto(Guid Id, string Title, DateTime EventDate, DateTime? EndDate,
    string? Location, string? CoverImageKey, EventStatus Status, int GoingCount);

public sealed record EventDetailDto(Guid Id, string Title, string? Description, DateTime EventDate,
    DateTime? EndDate, string? Location, string? CoverImageKey, EventStatus Status,
    int GoingCount, int MaybeCount, RsvpStatus? MyRsvp,
    IReadOnlyList<string> GalleryKeys);

public sealed record UpsertEventDto(string Title, string? Description, DateTime EventDate,
    DateTime? EndDate, string? Location);

public sealed record ParticipantDto(Guid UserId, string FullName, string Email, RsvpStatus Status, DateTime At);

// ── Queries ──────────────────────────────────────────────────────────────────

/// <summary>Public event list, split into upcoming vs past.</summary>
public sealed record ListEventsQuery(string? Scope, bool IncludeAll, int Page, int PageSize)
    : IRequest<PagedResult<EventCardDto>>;

public sealed class ListEventsHandler(IAppDbContext db) : IRequestHandler<ListEventsQuery, PagedResult<EventCardDto>>
{
    public async Task<PagedResult<EventCardDto>> Handle(ListEventsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 50);
        var now = DateTime.UtcNow;

        var events = db.Events.AsQueryable();
        if (!q.IncludeAll)
            events = events.Where(e => e.Status == EventStatus.Published || e.Status == EventStatus.Completed);

        events = q.Scope?.ToLowerInvariant() switch
        {
            "past" => events.Where(e => e.EventDate < now).OrderByDescending(e => e.EventDate),
            _ => events.Where(e => e.EventDate >= now).OrderBy(e => e.EventDate),
        };

        var total = await events.CountAsync(ct);
        var items = await events.Skip((page - 1) * size).Take(size)
            .Select(e => new EventCardDto(e.Id, e.Title, e.EventDate, e.EndDate, e.Location, e.CoverImageKey, e.Status,
                e.Rsvps.Count(r => r.Status == RsvpStatus.Going)))
            .ToListAsync(ct);

        return new PagedResult<EventCardDto>(items, page, size, total);
    }
}

public sealed record GetEventQuery(Guid Id, Guid? ViewerId) : IRequest<EventDetailDto>;

public sealed class GetEventHandler(IAppDbContext db) : IRequestHandler<GetEventQuery, EventDetailDto>
{
    public async Task<EventDetailDto> Handle(GetEventQuery q, CancellationToken ct)
    {
        var e = await db.Events.Include(x => x.Gallery)
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct)
            ?? throw new NotFoundException("Event", q.Id);

        var going = await db.EventRsvps.CountAsync(r => r.EventId == e.Id && r.Status == RsvpStatus.Going, ct);
        var maybe = await db.EventRsvps.CountAsync(r => r.EventId == e.Id && r.Status == RsvpStatus.Maybe, ct);
        RsvpStatus? mine = null;
        if (q.ViewerId is Guid vid)
            mine = await db.EventRsvps.Where(r => r.EventId == e.Id && r.UserId == vid)
                .Select(r => (RsvpStatus?)r.Status).FirstOrDefaultAsync(ct);

        return new EventDetailDto(e.Id, e.Title, e.Description, e.EventDate, e.EndDate, e.Location,
            e.CoverImageKey, e.Status, going, maybe, mine,
            e.Gallery.Select(g => g.FileKey).ToList());
    }
}

public sealed record GetParticipantsQuery(Guid EventId) : IRequest<IReadOnlyList<ParticipantDto>>;

public sealed class GetParticipantsHandler(IAppDbContext db) : IRequestHandler<GetParticipantsQuery, IReadOnlyList<ParticipantDto>>
{
    public async Task<IReadOnlyList<ParticipantDto>> Handle(GetParticipantsQuery q, CancellationToken ct) =>
        await (from r in db.EventRsvps.Where(x => x.EventId == q.EventId)
               join u in db.Users on r.UserId equals u.Id
               orderby r.Status, u.FullName
               select new ParticipantDto(r.UserId, u.FullName, u.Email, r.Status, r.UpdatedAt))
            .ToListAsync(ct);
}

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record CreateEventCommand(Guid AdminId, UpsertEventDto Data) : IRequest<Guid>;

public sealed class CreateEventHandler(IAppDbContext db) : IRequestHandler<CreateEventCommand, Guid>
{
    public async Task<Guid> Handle(CreateEventCommand cmd, CancellationToken ct)
    {
        var school = await db.Schools.FirstOrDefaultAsync(s => s.IsActive, ct)
            ?? throw new DomainRuleException("No active school is configured.");
        var d = cmd.Data;
        var e = new Event
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Title = d.Title.Trim(),
            Description = d.Description?.Trim(),
            EventDate = d.EventDate,
            EndDate = d.EndDate,
            Location = d.Location?.Trim(),
            Status = EventStatus.Draft,
            CreatedBy = cmd.AdminId,
        };
        db.Events.Add(e);
        await db.SaveChangesAsync(ct);
        return e.Id;
    }
}

public sealed record UpdateEventCommand(Guid Id, UpsertEventDto Data) : IRequest;

public sealed class UpdateEventHandler(IAppDbContext db) : IRequestHandler<UpdateEventCommand>
{
    public async Task Handle(UpdateEventCommand cmd, CancellationToken ct)
    {
        var e = await db.Events.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            ?? throw new NotFoundException("Event", cmd.Id);
        var d = cmd.Data;
        e.Title = d.Title.Trim();
        e.Description = d.Description?.Trim();
        e.EventDate = d.EventDate;
        e.EndDate = d.EndDate;
        e.Location = d.Location?.Trim();
        e.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

public sealed record ChangeEventStatusCommand(Guid Id, EventStatus Status) : IRequest;

public sealed class ChangeEventStatusHandler(IAppDbContext db) : IRequestHandler<ChangeEventStatusCommand>
{
    public async Task Handle(ChangeEventStatusCommand cmd, CancellationToken ct)
    {
        var e = await db.Events.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            ?? throw new NotFoundException("Event", cmd.Id);
        e.Status = cmd.Status;
        e.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Member RSVP (idempotent upsert).</summary>
public sealed record RsvpCommand(Guid EventId, Guid UserId, RsvpStatus Status) : IRequest;

public sealed class RsvpHandler(IAppDbContext db) : IRequestHandler<RsvpCommand>
{
    public async Task Handle(RsvpCommand cmd, CancellationToken ct)
    {
        var e = await db.Events.FirstOrDefaultAsync(x => x.Id == cmd.EventId, ct)
            ?? throw new NotFoundException("Event", cmd.EventId);
        if (e.Status != EventStatus.Published)
            throw new DomainRuleException("This event is not open for RSVP.");

        var rsvp = await db.EventRsvps.FirstOrDefaultAsync(r => r.EventId == cmd.EventId && r.UserId == cmd.UserId, ct);
        if (rsvp is null)
        {
            db.EventRsvps.Add(new EventRsvp { Id = Guid.NewGuid(), EventId = cmd.EventId, UserId = cmd.UserId, Status = cmd.Status });
        }
        else
        {
            rsvp.Status = cmd.Status;
            rsvp.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }
}

public sealed record AddEventGalleryCommand(Guid EventId, Guid AdminId, Stream Content, string FileName, string ContentType, string? Caption) : IRequest<string>;

public sealed class AddEventGalleryHandler(IAppDbContext db, IFileStorage storage) : IRequestHandler<AddEventGalleryCommand, string>
{
    public async Task<string> Handle(AddEventGalleryCommand cmd, CancellationToken ct)
    {
        var e = await db.Events.FirstOrDefaultAsync(x => x.Id == cmd.EventId, ct)
            ?? throw new NotFoundException("Event", cmd.EventId);
        var key = await storage.SaveAsync(cmd.Content, cmd.FileName, cmd.ContentType, "events", ct);
        db.EventGalleryImages.Add(new EventGalleryImage
        {
            Id = Guid.NewGuid(), EventId = e.Id, FileKey = key, Caption = cmd.Caption?.Trim(), UploadedBy = cmd.AdminId,
        });
        await db.SaveChangesAsync(ct);
        return key;
    }
}
