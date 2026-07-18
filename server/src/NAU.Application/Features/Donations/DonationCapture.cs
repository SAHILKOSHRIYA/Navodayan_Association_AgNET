using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Interfaces;
using NAU.Domain.Entities;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Donations;

/// <summary>
/// Shared capture logic used by both the checkout-verify path and the webhook (source of truth).
/// Capturing assigns a sequential per-financial-year receipt number (Decision D8). Idempotent:
/// an already-captured donation is left unchanged.
/// </summary>
public static class DonationCapture
{
    public static async Task EnsureCapturedAsync(
        IAppDbContext db, Donation d, string? paymentId, string? signature, CancellationToken ct)
    {
        if (d.Status == DonationStatus.Captured) return;

        d.Status = DonationStatus.Captured;
        d.RazorpayPaymentId = paymentId ?? d.RazorpayPaymentId;
        d.RazorpaySignature = signature ?? d.RazorpaySignature;
        d.CapturedAt = DateTime.UtcNow;
        d.ReceiptNumber = await NextReceiptNumberAsync(db, d.CapturedAt.Value, ct);
    }

    /// <summary>Indian financial year label for a UTC instant, e.g. "2026-27" (approximated from UTC).</summary>
    public static string FinancialYear(DateTime utc)
    {
        var year = utc.Month >= 4 ? utc.Year : utc.Year - 1;
        return $"{year}-{(year + 1) % 100:00}";
    }

    private static async Task<string> NextReceiptNumberAsync(IAppDbContext db, DateTime capturedAt, CancellationToken ct)
    {
        var prefix = $"NAU/{FinancialYear(capturedAt)}/";
        var count = await db.Donations.CountAsync(
            x => x.ReceiptNumber != null && x.ReceiptNumber.StartsWith(prefix), ct);
        return $"{prefix}{count + 1:000000}";
    }
}
