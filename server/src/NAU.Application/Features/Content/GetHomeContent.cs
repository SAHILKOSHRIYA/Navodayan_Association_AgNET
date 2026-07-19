using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Interfaces;
using NAU.Application.Features.Announcements;
using NAU.Application.Features.Campaigns;
using NAU.Application.Features.Events;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Content;

public sealed record HomeStatsDto(int VerifiedAlumni, decimal TotalRaised, int ActiveCampaigns, int UpcomingEvents);

/// <summary>Everything the public landing page needs in one call (Phase 2 §6 /content/home).</summary>
public sealed record HomeContentDto(
    HomeStatsDto Stats,
    IReadOnlyList<CampaignCardDto> LatestCampaigns,
    IReadOnlyList<EventCardDto> UpcomingEvents,
    IReadOnlyList<AnnouncementDto> RecentAnnouncements);

public sealed record GetHomeContentQuery : IRequest<HomeContentDto>;

public sealed class GetHomeContentHandler(IAppDbContext db) : IRequestHandler<GetHomeContentQuery, HomeContentDto>
{
    public async Task<HomeContentDto> Handle(GetHomeContentQuery q, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var verifiedAlumni = await db.AlumniProfiles.CountAsync(p => p.IsVerified, ct);
        var totalRaised = await db.Donations.Where(d => d.Status == DonationStatus.Captured)
            .SumAsync(d => (decimal?)d.Amount, ct) ?? 0m;
        var activeCampaignsCount = await db.Campaigns.CountAsync(c => c.Status == CampaignStatus.Active, ct);
        var upcomingEventsCount = await db.Events.CountAsync(
            e => e.Status == EventStatus.Published && e.EventDate >= now, ct);

        // Latest active campaigns (with derived totals).
        var campaigns = await db.Campaigns
            .Where(c => c.Status == CampaignStatus.Active)
            .OrderByDescending(c => c.CreatedAt).Take(3).ToListAsync(ct);
        var raised = await ListCampaignsHandler.RaisedByCampaignAsync(db, campaigns.Select(c => c.Id).ToList(), ct);
        var latestCampaigns = campaigns.Select(c =>
        {
            var r = raised.GetValueOrDefault(c.Id);
            return new CampaignCardDto(c.Id, c.Title, c.Slug, c.CoverImageKey, c.GoalAmount, r,
                c.Currency, c.Status, c.StartDate, c.EndDate, CampaignTotals.Progress(r, c.GoalAmount));
        }).ToList();

        var events = await db.Events
            .Where(e => e.Status == EventStatus.Published && e.EventDate >= now)
            .OrderBy(e => e.EventDate).Take(3)
            .Select(e => new EventCardDto(e.Id, e.Title, e.EventDate, e.EndDate, e.Location, e.CoverImageKey, e.Status,
                e.Rsvps.Count(r => r.Status == RsvpStatus.Going)))
            .ToListAsync(ct);

        var announcements = await db.Announcements
            .Where(a => a.PublishedAt != null && a.Audience == AnnouncementAudience.Public)
            .OrderByDescending(a => a.PublishedAt).Take(5)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Body, a.Category, a.Audience, a.PublishedAt, a.CreatedAt))
            .ToListAsync(ct);

        return new HomeContentDto(
            new HomeStatsDto(verifiedAlumni, totalRaised, activeCampaignsCount, upcomingEventsCount),
            latestCampaigns, events, announcements);
    }
}
