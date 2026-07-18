using Microsoft.Extensions.Options;
using NAU.Application.Common.Interfaces;

namespace NAU.Infrastructure.Payments;

/// <summary>
/// Deterministic gateway for development/tests — no network calls. Signatures use the same
/// HMAC scheme as Razorpay so the end-to-end verify/webhook flow is exercised locally.
/// Selected when Payments:Provider != "razorpay".
/// </summary>
public sealed class TestPaymentGateway(IOptions<PaymentOptions> options) : IPaymentGateway
{
    private readonly PaymentOptions _opt = options.Value;

    public string PublicKeyId => _opt.KeyId;

    public Task<PaymentOrder> CreateOrderAsync(decimal amount, string currency, string receiptRef, CancellationToken ct = default)
    {
        var orderId = $"order_test_{Guid.NewGuid():N}";
        var amountMinor = (long)Math.Round(amount * 100);
        return Task.FromResult(new PaymentOrder(orderId, amountMinor, currency, _opt.KeyId));
    }

    public bool VerifyPaymentSignature(string orderId, string paymentId, string signature) =>
        RazorpaySignature.Verify($"{orderId}|{paymentId}", _opt.KeySecret, signature);

    public bool VerifyWebhookSignature(string payload, string signature) =>
        RazorpaySignature.Verify(payload, _opt.WebhookSecret, signature);
}
