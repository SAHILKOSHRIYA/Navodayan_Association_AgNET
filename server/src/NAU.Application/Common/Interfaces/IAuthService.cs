using NAU.Application.Features.Auth;

namespace NAU.Application.Common.Interfaces;

/// <summary>
/// Identity operations implemented in Infrastructure (ASP.NET Identity + JWT).
/// Failures are signalled with the typed app exceptions so the API middleware
/// maps them to the right status codes.
/// </summary>
public interface IAuthService
{
    /// <summary>Creates the account (role: Alumni) and emails a verification link. Returns the new user id.</summary>
    Task<Guid> RegisterAsync(string fullName, string email, string password, CancellationToken ct);

    Task VerifyEmailAsync(string email, string token, CancellationToken ct);

    Task ResendVerificationAsync(string email, CancellationToken ct);

    Task<AuthResultDto> LoginAsync(string email, string password, string? ip, CancellationToken ct);

    Task<AuthResultDto> RefreshAsync(string refreshToken, string? ip, CancellationToken ct);

    Task LogoutAsync(string refreshToken, CancellationToken ct);

    Task ForgotPasswordAsync(string email, CancellationToken ct);

    Task ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct);
}
