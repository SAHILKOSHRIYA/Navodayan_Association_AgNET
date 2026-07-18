using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Interfaces;
using NAU.Domain.Entities;

namespace NAU.Application.Features.Donations;

public enum WebhookOutcome { Processed, Duplicate, Ignored }

/// <summary>
/// Processes a (signature-verified) payment webhook — the source of truth for donation status
/// (Phase 2 §7). The raw event is stored before processing and is idempotent by provider event id,
/// so replays are safe. Signature verification happens at the API edge before this runs.
/// </summary>
public sealed record HandleWebhookCommand(
    string EventId, string EventType, string? OrderId, string? PaymentId, string RawPayload)
    : IRequest<WebhookOutcome>;

public sealed class HandleWebhookHandler(IAppDbContext db) : IRequestHandler<HandleWebhookCommand, WebhookOutcome>
{
    private static readonly string[] CaptureEvents = ["payment.captured", "order.paid"];

    public async Task<WebhookOutcome> Handle(HandleWebhookCommand cmd, CancellationToken ct)
    {
        // Idempotency: a provider event id we've already stored is a no-op.
        var seen = await db.PaymentEvents.AnyAsync(e => e.ProviderEventId == cmd.EventId, ct);
        if (seen) return WebhookOutcome.Duplicate;

        var evt = new PaymentEvent
        {
            Id = Guid.NewGuid(),
            EventType = cmd.EventType,
            ProviderEventId = cmd.EventId,
            Payload = cmd.RawPayload,
        };
        db.PaymentEvents.Add(evt);

        var outcome = WebhookOutcome.Ignored;
        if (CaptureEvents.Contains(cmd.EventType) && !string.IsNullOrEmpty(cmd.OrderId))
        {
            var donation = await db.Donations.FirstOrDefaultAsync(d => d.RazorpayOrderId == cmd.OrderId, ct);
            if (donation is not null)
            {
                await DonationCapture.EnsureCapturedAsync(db, donation, cmd.PaymentId, signature: null, ct);
                evt.DonationId = donation.Id;
                outcome = WebhookOutcome.Processed;
            }
        }

        evt.Processed = true;
        await db.SaveChangesAsync(ct);
        return outcome;
    }
}
