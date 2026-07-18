namespace NAU.Application.Features.Auth;

public sealed record AuthUserDto(
    Guid Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    bool EmailVerified);

public sealed record AuthResultDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    AuthUserDto User);
