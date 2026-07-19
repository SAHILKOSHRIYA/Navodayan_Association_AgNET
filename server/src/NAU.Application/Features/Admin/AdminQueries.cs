using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Application.Features.Donations;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Admin;

// ── Audit log ────────────────────────────────────────────────────────────────

public sealed record AuditLogDto(Guid Id, Guid? ActorId, string Action, string? EntityType,
    string? EntityId, string? Details, DateTime CreatedAt);

public sealed record GetAuditLogsQuery(int Page, int PageSize) : IRequest<PagedResult<AuditLogDto>>;

public sealed class GetAuditLogsHandler(IAppDbContext db) : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);
        var query = db.AuditLogs.OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * size).Take(size)
            .Select(a => new AuditLogDto(a.Id, a.ActorId, a.Action, a.EntityType, a.EntityId, a.Details, a.CreatedAt))
            .ToListAsync(ct);
        return new PagedResult<AuditLogDto>(items, page, size, total);
    }
}

// ── Reports ──────────────────────────────────────────────────────────────────

/// <summary>Flat rows for a donations report export (CSV/Excel formatted at the edge).</summary>
public sealed record GetDonationReportQuery(DateTime? From, DateTime? To, Guid? CampaignId)
    : IRequest<IReadOnlyList<DonationListItemDto>>;

public sealed class GetDonationReportHandler(IAppDbContext db) : IRequestHandler<GetDonationReportQuery, IReadOnlyList<DonationListItemDto>>
{
    public async Task<IReadOnlyList<DonationListItemDto>> Handle(GetDonationReportQuery q, CancellationToken ct)
    {
        var donations = db.Donations.Where(d => d.Status == DonationStatus.Captured);
        if (q.From is DateTime from) donations = donations.Where(d => d.CapturedAt >= from);
        if (q.To is DateTime to) donations = donations.Where(d => d.CapturedAt <= to);
        if (q.CampaignId is Guid cid) donations = donations.Where(d => d.CampaignId == cid);

        return await (from d in donations
                      join c in db.Campaigns on d.CampaignId equals c.Id
                      orderby d.CapturedAt
                      select new DonationListItemDto(d.Id, c.Title, d.DonorName, d.IsAnonymous, d.Amount,
                          d.Currency, d.Status, d.ReceiptNumber, d.CreatedAt, d.CapturedAt))
            .ToListAsync(ct);
    }
}
