using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Donations;

/// <summary>The caller's own donation history.</summary>
public sealed record GetMyDonationsQuery(Guid UserId, int Page, int PageSize) : IRequest<PagedResult<DonationListItemDto>>;

public sealed class GetMyDonationsHandler(IAppDbContext db) : IRequestHandler<GetMyDonationsQuery, PagedResult<DonationListItemDto>>
{
    public async Task<PagedResult<DonationListItemDto>> Handle(GetMyDonationsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 50);

        var query = from d in db.Donations.Where(x => x.UserId == q.UserId)
                    join c in db.Campaigns on d.CampaignId equals c.Id
                    orderby d.CreatedAt descending
                    select new DonationListItemDto(d.Id, c.Title, d.DonorName, d.IsAnonymous, d.Amount,
                        d.Currency, d.Status, d.ReceiptNumber, d.CreatedAt, d.CapturedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<DonationListItemDto>(items, page, size, total);
    }
}

/// <summary>Admin: all donations with optional filters.</summary>
public sealed record ListDonationsQuery(Guid? CampaignId, DonationStatus? Status, int Page, int PageSize)
    : IRequest<PagedResult<DonationListItemDto>>;

public sealed class ListDonationsHandler(IAppDbContext db) : IRequestHandler<ListDonationsQuery, PagedResult<DonationListItemDto>>
{
    public async Task<PagedResult<DonationListItemDto>> Handle(ListDonationsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var donations = db.Donations.AsQueryable();
        if (q.CampaignId is Guid cid) donations = donations.Where(d => d.CampaignId == cid);
        if (q.Status is DonationStatus st) donations = donations.Where(d => d.Status == st);

        var query = from d in donations
                    join c in db.Campaigns on d.CampaignId equals c.Id
                    orderby d.CreatedAt descending
                    select new DonationListItemDto(d.Id, c.Title, d.DonorName, d.IsAnonymous, d.Amount,
                        d.Currency, d.Status, d.ReceiptNumber, d.CreatedAt, d.CapturedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<DonationListItemDto>(items, page, size, total);
    }
}

/// <summary>Receipt for a captured donation. Only the owner or an admin may view it.</summary>
public sealed record GetDonationReceiptQuery(Guid DonationId, Guid? ViewerId, bool ViewerIsAdmin) : IRequest<DonationReceiptDto>;

public sealed class GetDonationReceiptHandler(IAppDbContext db) : IRequestHandler<GetDonationReceiptQuery, DonationReceiptDto>
{
    public async Task<DonationReceiptDto> Handle(GetDonationReceiptQuery q, CancellationToken ct)
    {
        var d = await db.Donations.FirstOrDefaultAsync(x => x.Id == q.DonationId, ct)
            ?? throw new NotFoundException("Donation", q.DonationId);

        var isOwner = d.UserId is not null && d.UserId == q.ViewerId;
        if (!isOwner && !q.ViewerIsAdmin)
            throw new ForbiddenException("You can only view your own receipts.");

        var campaign = await db.Campaigns.FirstAsync(c => c.Id == d.CampaignId, ct);

        if (d.Status != DonationStatus.Captured)
            throw new DomainRuleException("A receipt is available only after the donation is captured.");

        return new DonationReceiptDto(d.Id, d.ReceiptNumber, d.DonorName, campaign.Title,
            d.Amount, d.Currency, d.Status, d.CapturedAt);
    }
}
