using NAU.Domain.Enums;

namespace NAU.Domain.Entities;

/// <summary>
/// A donation to a campaign (Phase 2 §5.2). Only <see cref="DonationStatus.Captured"/> donations
/// count toward the campaign total. A donation is recorded as captured only after server-side
/// signature verification and/or the Razorpay webhook (Phase 2 §7) — never from a client callback alone.
/// Guest donations are allowed (Decision D7): user id is null, name+email are captured.
/// </summary>
public class Donation
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid? UserId { get; set; }

    public required string DonorName { get; set; }
    public required string DonorEmail { get; set; }
    public bool IsAnonymous { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public DonationStatus Status { get; set; } = DonationStatus.Created;

    public required string RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }

    /// <summary>Sequential per school-year, assigned only on capture (Decision D8).</summary>
    public string? ReceiptNumber { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Raw payment-provider webhook, stored before processing for idempotency and audit
/// (Phase 2 §5.2). Replaying the same provider event id is a no-op.
/// </summary>
public class PaymentEvent
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = "razorpay";
    public required string EventType { get; set; }
    public required string ProviderEventId { get; set; }
    public required string Payload { get; set; }
    public Guid? DonationId { get; set; }
    public bool Processed { get; set; }
    public string? Error { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
