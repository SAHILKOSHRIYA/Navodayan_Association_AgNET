using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Domain.Entities;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Campaigns;

public static class CampaignTotals
{
    public static int Progress(decimal raised, decimal goal) =>
        goal <= 0 ? 0 : (int)Math.Min(100, Math.Round(raised * 100 / goal));
}

/// <summary>Public campaign list. Admins may include non-active statuses via <paramref name="IncludeAll"/>.</summary>
public sealed record ListCampaignsQuery(bool IncludeAll, int Page, int PageSize)
    : IRequest<PagedResult<CampaignCardDto>>;

public sealed class ListCampaignsHandler(IAppDbContext db) : IRequestHandler<ListCampaignsQuery, PagedResult<CampaignCardDto>>
{
    public async Task<PagedResult<CampaignCardDto>> Handle(ListCampaignsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 50);

        var query = db.Campaigns.AsQueryable();
        if (!q.IncludeAll)
            query = query.Where(c => c.Status == CampaignStatus.Active || c.Status == CampaignStatus.Completed);

        query = query.OrderByDescending(c => c.Status == CampaignStatus.Active).ThenByDescending(c => c.CreatedAt);

        var total = await query.CountAsync(ct);
        var page_ = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        var ids = page_.Select(c => c.Id).ToList();

        var raised = await RaisedByCampaignAsync(db, ids, ct);

        var items = page_.Select(c =>
        {
            var r = raised.GetValueOrDefault(c.Id);
            return new CampaignCardDto(c.Id, c.Title, c.Slug, c.CoverImageKey, c.GoalAmount, r,
                c.Currency, c.Status, c.StartDate, c.EndDate, CampaignTotals.Progress(r, c.GoalAmount));
        }).ToList();

        return new PagedResult<CampaignCardDto>(items, page, size, total);
    }

    internal static async Task<Dictionary<Guid, decimal>> RaisedByCampaignAsync(
        IAppDbContext db, List<Guid> campaignIds, CancellationToken ct) =>
        await db.Donations
            .Where(d => d.Status == DonationStatus.Captured && campaignIds.Contains(d.CampaignId))
            .GroupBy(d => d.CampaignId)
            .Select(g => new { g.Key, Sum = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, ct);
}

/// <summary>Full campaign detail by slug (public), with derived totals, donors and updates.</summary>
public sealed record GetCampaignBySlugQuery(string Slug) : IRequest<CampaignDetailDto>;

public sealed class GetCampaignBySlugHandler(IAppDbContext db) : IRequestHandler<GetCampaignBySlugQuery, CampaignDetailDto>
{
    public async Task<CampaignDetailDto> Handle(GetCampaignBySlugQuery q, CancellationToken ct)
    {
        var c = await db.Campaigns.Include(x => x.Updates)
            .FirstOrDefaultAsync(x => x.Slug == q.Slug, ct)
            ?? throw new NotFoundException("Campaign", q.Slug);

        var captured = db.Donations.Where(d => d.CampaignId == c.Id && d.Status == DonationStatus.Captured);

        var raised = await captured.SumAsync(d => (decimal?)d.Amount, ct) ?? 0m;
        var donorCount = await captured.CountAsync(ct);

        // Anonymous donors are shown as "Anonymous" but never with a real name (Phase 1 FR-6).
        var recentRaw = await captured.OrderByDescending(d => d.CapturedAt)
            .Take(10)
            .Select(d => new { d.IsAnonymous, d.DonorName, d.Amount, d.CapturedAt })
            .ToListAsync(ct);
        var recent = recentRaw
            .Select(d => new DonorDto(d.IsAnonymous ? "Anonymous" : d.DonorName, d.Amount, d.CapturedAt ?? default))
            .ToList();

        var topRaw = await captured.Where(d => !d.IsAnonymous)
            .GroupBy(d => d.DonorName)
            .Select(g => new { Name = g.Key, Amount = g.Sum(x => x.Amount), At = g.Max(x => x.CapturedAt) })
            .OrderByDescending(x => x.Amount)
            .Take(5)
            .ToListAsync(ct);
        var top = topRaw.Select(x => new DonorDto(x.Name, x.Amount, x.At ?? default)).ToList();

        var updates = c.Updates.OrderByDescending(u => u.CreatedAt)
            .Select(u => new CampaignUpdateDto(u.Id, u.Title, u.Body, u.CreatedAt)).ToList();

        return new CampaignDetailDto(c.Id, c.Title, c.Slug, c.Description, c.CoverImageKey,
            c.GoalAmount, raised, c.Currency, c.Status, c.StartDate, c.EndDate, c.OrganizerName,
            CampaignTotals.Progress(raised, c.GoalAmount), donorCount, recent, top, updates);
    }
}
