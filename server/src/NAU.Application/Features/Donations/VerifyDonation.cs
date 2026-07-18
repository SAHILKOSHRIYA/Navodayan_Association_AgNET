using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Donations;

/// <summary>
/// Verifies the browser checkout callback and captures the donation. The signature is checked
/// server-side (Phase 2 §7); a captured donation is left unchanged (idempotent). The webhook is
/// the ultimate source of truth, but verifying here gives the donor an instant receipt.
/// </summary>
public sealed record VerifyDonationCommand(VerifyDonationDto Data) : IRequest<DonationReceiptDto>;

public sealed class VerifyDonationHandler(IAppDbContext db, IPaymentGateway gateway, IEmailSender email)
    : IRequestHandler<VerifyDonationCommand, DonationReceiptDto>
{
    public async Task<DonationReceiptDto> Handle(VerifyDonationCommand cmd, CancellationToken ct)
    {
        var v = cmd.Data;
        var donation = await db.Donations.FirstOrDefaultAsync(d => d.RazorpayOrderId == v.OrderId, ct)
            ?? throw new NotFoundException("Donation", v.OrderId);

        var campaign = await db.Campaigns.FirstAsync(c => c.Id == donation.CampaignId, ct);

        if (donation.Status == DonationStatus.Captured)
            return Receipt(donation, campaign.Title); // idempotent

        if (!gateway.VerifyPaymentSignature(v.OrderId, v.PaymentId, v.Signature))
            throw new DomainRuleException("Payment verification failed. If money was debited it will be reconciled automatically.");

        await DonationCapture.EnsureCapturedAsync(db, donation, v.PaymentId, v.Signature, ct);
        await db.SaveChangesAsync(ct);

        await SendReceiptEmailAsync(donation, campaign.Title, ct);
        return Receipt(donation, campaign.Title);
    }

    private static DonationReceiptDto Receipt(Domain.Entities.Donation d, string campaignTitle) =>
        new(d.Id, d.ReceiptNumber, d.DonorName, campaignTitle, d.Amount, d.Currency, d.Status, d.CapturedAt);

    private async Task SendReceiptEmailAsync(Domain.Entities.Donation d, string campaignTitle, CancellationToken ct)
    {
        await email.SendAsync(d.DonorEmail, $"Thank you for your donation — {campaignTitle}",
            $"""
             <p>Dear {d.DonorName},</p>
             <p>Thank you for your generous donation of ₹{d.Amount:N2} to <strong>{campaignTitle}</strong>.</p>
             <p>Your receipt number is <strong>{d.ReceiptNumber}</strong>.</p>
             <p>With gratitude,<br/>Navodaya Alumni Association</p>
             """, ct);
    }
}
