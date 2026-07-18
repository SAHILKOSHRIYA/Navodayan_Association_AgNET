using MediatR;
using NAU.Application.Common.Interfaces;

namespace NAU.Application.Features.Auth;

// ── Commands ────────────────────────────────────────────────────────────────

public sealed record RegisterCommand(string FullName, string Email, string Password) : IRequest<Guid>;
public sealed record VerifyEmailCommand(string Email, string Token) : IRequest;
public sealed record ResendVerificationCommand(string Email) : IRequest;
public sealed record LoginCommand(string Email, string Password, string? Ip) : IRequest<AuthResultDto>;
public sealed record RefreshTokenCommand(string RefreshToken, string? Ip) : IRequest<AuthResultDto>;
public sealed record LogoutCommand(string RefreshToken) : IRequest;
public sealed record ForgotPasswordCommand(string Email) : IRequest;
public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest;

// ── Handlers (thin — delegate to IAuthService) ─────────────────────────────

public sealed class RegisterHandler(IAuthService auth) : IRequestHandler<RegisterCommand, Guid>
{
    public Task<Guid> Handle(RegisterCommand r, CancellationToken ct) =>
        auth.RegisterAsync(r.FullName.Trim(), r.Email.Trim(), r.Password, ct);
}

public sealed class VerifyEmailHandler(IAuthService auth) : IRequestHandler<VerifyEmailCommand>
{
    public Task Handle(VerifyEmailCommand r, CancellationToken ct) =>
        auth.VerifyEmailAsync(r.Email.Trim(), r.Token, ct);
}

public sealed class ResendVerificationHandler(IAuthService auth) : IRequestHandler<ResendVerificationCommand>
{
    public Task Handle(ResendVerificationCommand r, CancellationToken ct) =>
        auth.ResendVerificationAsync(r.Email.Trim(), ct);
}

public sealed class LoginHandler(IAuthService auth) : IRequestHandler<LoginCommand, AuthResultDto>
{
    public Task<AuthResultDto> Handle(LoginCommand r, CancellationToken ct) =>
        auth.LoginAsync(r.Email.Trim(), r.Password, r.Ip, ct);
}

public sealed class RefreshTokenHandler(IAuthService auth) : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    public Task<AuthResultDto> Handle(RefreshTokenCommand r, CancellationToken ct) =>
        auth.RefreshAsync(r.RefreshToken, r.Ip, ct);
}

public sealed class LogoutHandler(IAuthService auth) : IRequestHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand r, CancellationToken ct) => auth.LogoutAsync(r.RefreshToken, ct);
}

public sealed class ForgotPasswordHandler(IAuthService auth) : IRequestHandler<ForgotPasswordCommand>
{
    public Task Handle(ForgotPasswordCommand r, CancellationToken ct) => auth.ForgotPasswordAsync(r.Email.Trim(), ct);
}

public sealed class ResetPasswordHandler(IAuthService auth) : IRequestHandler<ResetPasswordCommand>
{
    public Task Handle(ResetPasswordCommand r, CancellationToken ct) =>
        auth.ResetPasswordAsync(r.Email.Trim(), r.Token, r.NewPassword, ct);
}
