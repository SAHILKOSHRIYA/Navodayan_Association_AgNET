using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAU.Application.Common.Interfaces;
using NAU.Application.Features.Donations;

namespace NAU.Api.Controllers;

/// <summary>
/// Payment provider webhooks. The signature is verified against the raw request body before any
/// processing (Phase 2 §7); Razorpay-specific JSON is parsed here so the Application layer stays
/// provider-agnostic. Always returns 200 on accepted events so the provider stops retrying.
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
public sealed class WebhooksController(ISender mediator, IPaymentGateway gateway, ILogger<WebhooksController> logger)
    : ControllerBase
{
    [HttpPost("razorpay")]
    [AllowAnonymous]
    public async Task<IActionResult> Razorpay(CancellationToken ct)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        if (string.IsNullOrEmpty(signature) || !gateway.VerifyWebhookSignature(body, signature))
        {
            logger.LogWarning("Rejected Razorpay webhook with invalid signature");
            return BadRequest(); // do not process unverified payloads
        }

        // Idempotency key: provider event id header, falling back to a hash of the body.
        var eventId = Request.Headers["X-Razorpay-Event-Id"].ToString();
        if (string.IsNullOrEmpty(eventId))
            eventId = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(body)));

        var (eventType, orderId, paymentId) = Parse(body);
        var outcome = await mediator.Send(new HandleWebhookCommand(eventId, eventType, orderId, paymentId, body), ct);

        logger.LogInformation("Razorpay webhook {EventType} → {Outcome}", eventType, outcome);
        return Ok(new { outcome = outcome.ToString() });
    }

    /// <summary>Extracts event type and the payment/order ids from a Razorpay webhook body.</summary>
    private static (string EventType, string? OrderId, string? PaymentId) Parse(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var eventType = root.TryGetProperty("event", out var e) ? e.GetString() ?? "unknown" : "unknown";

            string? orderId = null, paymentId = null;
            if (root.TryGetProperty("payload", out var payload) &&
                payload.TryGetProperty("payment", out var payment) &&
                payment.TryGetProperty("entity", out var entity))
            {
                if (entity.TryGetProperty("order_id", out var o)) orderId = o.GetString();
                if (entity.TryGetProperty("id", out var p)) paymentId = p.GetString();
            }
            return (eventType, orderId, paymentId);
        }
        catch (JsonException)
        {
            return ("unparseable", null, null);
        }
    }
}
