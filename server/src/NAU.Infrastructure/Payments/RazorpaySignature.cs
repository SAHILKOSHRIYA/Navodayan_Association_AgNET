using System.Security.Cryptography;
using System.Text;

namespace NAU.Infrastructure.Payments;

/// <summary>Razorpay HMAC-SHA256 signing helpers, shared by the live and test gateways.</summary>
internal static class RazorpaySignature
{
    public static string Compute(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Constant-time comparison to avoid timing side-channels (Phase 2 §7).</summary>
    public static bool Verify(string payload, string secret, string providedSignature)
    {
        var expected = Compute(payload, secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(providedSignature ?? string.Empty));
    }
}
