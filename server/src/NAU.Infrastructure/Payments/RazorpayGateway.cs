using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NAU.Application.Common.Interfaces;

namespace NAU.Infrastructure.Payments;

/// <summary>
/// Live Razorpay gateway: creates orders via the Razorpay Orders API and verifies
/// checkout/webhook signatures. Selected when Payments:Provider == "razorpay".
/// Signature schemes are identical to <see cref="TestPaymentGateway"/> (real HMAC).
/// </summary>
public sealed class RazorpayGateway(HttpClient http, IOptions<PaymentOptions> options) : IPaymentGateway
{
    private readonly PaymentOptions _opt = options.Value;

    public string PublicKeyId => _opt.KeyId;

    public async Task<PaymentOrder> CreateOrderAsync(decimal amount, string currency, string receiptRef, CancellationToken ct = default)
    {
        var amountMinor = (long)Math.Round(amount * 100);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { amount = amountMinor, currency, receipt = receiptRef }),
                Encoding.UTF8, "application/json"),
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opt.KeyId}:{_opt.KeySecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Razorpay order creation failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var orderId = doc.RootElement.GetProperty("id").GetString()!;
        return new PaymentOrder(orderId, amountMinor, currency, _opt.KeyId);
    }

    public bool VerifyPaymentSignature(string orderId, string paymentId, string signature) =>
        RazorpaySignature.Verify($"{orderId}|{paymentId}", _opt.KeySecret, signature);

    public bool VerifyWebhookSignature(string payload, string signature) =>
        RazorpaySignature.Verify(payload, _opt.WebhookSecret, signature);
}
