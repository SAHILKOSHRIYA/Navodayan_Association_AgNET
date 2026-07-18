namespace NAU.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC signing secret — must be ≥ 32 bytes; supplied via environment in production.</summary>
    public required string Secret { get; init; }
    public string Issuer { get; init; } = "nau-api";
    public string Audience { get; init; } = "nau-clients";
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}
