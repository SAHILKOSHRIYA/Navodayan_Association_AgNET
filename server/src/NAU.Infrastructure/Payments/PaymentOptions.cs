namespace NAU.Infrastructure.Payments;

public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    /// <summary>"test" (deterministic, no network) or "razorpay" (live/live-test keys).</summary>
    public string Provider { get; init; } = "test";
    public string KeyId { get; init; } = "rzp_test_placeholder";
    public string KeySecret { get; init; } = "test_secret_change_me";
    public string WebhookSecret { get; init; } = "test_webhook_secret_change_me";
}
