using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NAU.Application.Common.Models;
using NAU.Application.Features.Auth;

namespace NAU.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(ISender mediator) : ControllerBase
{
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    public sealed record RegisterRequest(string FullName, string Email, string Password);
    public sealed record VerifyEmailRequest(string Email, string Token);
    public sealed record EmailRequest(string Email);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);
    public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<Guid>>> Register(RegisterRequest r, CancellationToken ct)
    {
        var id = await mediator.Send(new RegisterCommand(r.FullName, r.Email, r.Password), ct);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<Guid>.Ok(id, "Account created. Please check your email to verify your address."));
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> VerifyEmail(VerifyEmailRequest r, CancellationToken ct)
    {
        await mediator.Send(new VerifyEmailCommand(r.Email, r.Token), ct);
        return Ok(ApiResponse<object>.Ok(new(), "Email verified. You can now sign in."));
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResendVerification(EmailRequest r, CancellationToken ct)
    {
        await mediator.Send(new ResendVerificationCommand(r.Email), ct);
        return Ok(ApiResponse<object>.Ok(new(), "If the account exists and is unverified, a new link has been sent."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Login(LoginRequest r, CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(r.Email, r.Password, ClientIp), ct);
        return Ok(ApiResponse<AuthResultDto>.Ok(result));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Refresh(RefreshRequest r, CancellationToken ct)
    {
        var result = await mediator.Send(new RefreshTokenCommand(r.RefreshToken, ClientIp), ct);
        return Ok(ApiResponse<AuthResultDto>.Ok(result));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Logout(RefreshRequest r, CancellationToken ct)
    {
        await mediator.Send(new LogoutCommand(r.RefreshToken), ct);
        return Ok(ApiResponse<object>.Ok(new(), "Signed out."));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(EmailRequest r, CancellationToken ct)
    {
        await mediator.Send(new ForgotPasswordCommand(r.Email), ct);
        return Ok(ApiResponse<object>.Ok(new(), "If the account exists, a reset link has been sent."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest r, CancellationToken ct)
    {
        await mediator.Send(new ResetPasswordCommand(r.Email, r.Token, r.NewPassword), ct);
        return Ok(ApiResponse<object>.Ok(new(), "Password updated. Please sign in."));
    }

    /// <summary>Current authenticated identity (from the access token).</summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<ApiResponse<object>> Me() => Ok(ApiResponse<object>.Ok(new
    {
        Id = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
        Name = User.FindFirst("name")?.Value,
        Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
        Roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value),
        SchoolId = User.FindFirst("school_id")?.Value,
    }));
}
