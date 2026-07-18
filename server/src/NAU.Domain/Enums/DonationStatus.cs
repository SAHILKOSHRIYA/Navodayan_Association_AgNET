namespace NAU.Domain.Enums;

public enum DonationStatus
{
    /// <summary>Order created with the gateway; payment not yet completed.</summary>
    Created = 0,
    /// <summary>Payment verified and captured — counts toward the campaign total.</summary>
    Captured = 1,
    Failed = 2,
    Refunded = 3
}
