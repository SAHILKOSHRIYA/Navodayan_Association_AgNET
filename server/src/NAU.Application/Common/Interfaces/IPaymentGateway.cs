namespace NAU.Application.Common.Interfaces;

public sealed record PaymentOrder(string OrderId, long AmountMinor, string Currency, string KeyId);

/// <summary>
/// Payment provider abstraction (Phase 2 §7). The concrete Razorpay implementation lives in
/// Infrastructure; a deterministic test implementation lets the full donation flow be verified
/// without live keys. Signature checks MUST be constant-time and server-side.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Public key id handed to the browser checkout (never the secret).</summary>
    string PublicKeyId { get; }

    Task<PaymentOrder> CreateOrderAsync(decimal amount, string currency, string receiptRef, CancellationToken ct = default);

    /// <summary>Verifies the checkout callback signature: HMAC(order_id|payment_id, secret).</summary>
    bool VerifyPaymentSignature(string orderId, string paymentId, string signature);

    /// <summary>Verifies a webhook body signature against the webhook secret.</summary>
    bool VerifyWebhookSignature(string payload, string signature);
}
