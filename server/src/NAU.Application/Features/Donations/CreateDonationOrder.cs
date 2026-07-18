using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Domain.Entities;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Donations;

/// <summary>
/// Creates a payment order for a donation and records it as <see cref="DonationStatus.Created"/>.
/// Amount is validated server-side and never trusted from the client at capture time (Phase 2 §7).
/// Guest donations are allowed (Decision D7): <paramref name="UserId"/> is null for guests.
/// </summary>
public sealed record CreateDonationOrderCommand(Guid? UserId, CreateDonationDto Data) : IRequest<DonationOrderDto>;

public sealed class CreateDonationOrderHandler(IAppDbContext db, IPaymentGateway gateway)
    : IRequestHandler<CreateDonationOrderCommand, DonationOrderDto>
{
    public async Task<DonationOrderDto> Handle(CreateDonationOrderCommand cmd, CancellationToken ct)
    {
        var d = cmd.Data;
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == d.CampaignId, ct)
            ?? throw new NotFoundException("Campaign", d.CampaignId);

        if (campaign.Status != CampaignStatus.Active)
            throw new DomainRuleException("This campaign is not currently accepting donations.");

        var order = await gateway.CreateOrderAsync(d.Amount, campaign.Currency, $"donation_{Guid.NewGuid():N}", ct);

        var donation = new Donation
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            UserId = cmd.UserId,
            DonorName = d.DonorName.Trim(),
            DonorEmail = d.DonorEmail.Trim(),
            IsAnonymous = d.IsAnonymous,
            Amount = d.Amount,
            Currency = campaign.Currency,
            Status = DonationStatus.Created,
            RazorpayOrderId = order.OrderId,
        };
        db.Donations.Add(donation);
        await db.SaveChangesAsync(ct);

        return new DonationOrderDto(donation.Id, order.OrderId, order.KeyId, order.AmountMinor, order.Currency, campaign.Title);
    }
}
