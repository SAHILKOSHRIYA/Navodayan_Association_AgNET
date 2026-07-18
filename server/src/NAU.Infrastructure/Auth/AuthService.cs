using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Features.Auth;
using NAU.Domain.Constants;
using NAU.Domain.Entities;
using NAU.Domain.Enums;
using NAU.Infrastructure.Identity;
using NAU.Infrastructure.Persistence;

namespace NAU.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    IJwtTokenService jwt,
    IEmailSender email,
    IOptions<JwtOptions> jwtOptions,
    IConfiguration configuration,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwtOpt = jwtOptions.Value;

    // ── Registration & verification ─────────────────────────────────────────

    public async Task<Guid> RegisterAsync(string fullName, string email_, string password, CancellationToken ct)
    {
        if (await userManager.FindByEmailAsync(email_) is not null)
            throw new ConflictException("An account with this email already exists.");

        var school = await db.Schools.FirstOrDefaultAsync(s => s.IsActive, ct)
            ?? throw new DomainRuleException("No active school is configured.");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email_,
            Email = email_,
            FullName = fullName,
            SchoolId = school.Id,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new DomainRuleException(string.Join(" ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, Roles.Alumni);
        await SendVerificationEmailAsync(user, ct);

        logger.LogInformation("User {UserId} registered with email {Email}", user.Id, email_);
        return user.Id;
    }

    public async Task VerifyEmailAsync(string email_, string token, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email_)
            ?? throw new NotFoundException("Account", email_);

        if (user.EmailConfirmed) return; // idempotent

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            throw new DomainRuleException("The verification link is invalid or has expired. Please request a new one.");

        user.EmailVerifiedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
    }

    public async Task ResendVerificationAsync(string email_, CancellationToken ct)
    {
        // Never reveal whether the account exists (Phase 2 §7).
        var user = await userManager.FindByEmailAsync(email_);
        if (user is null || user.EmailConfirmed) return;
        await SendVerificationEmailAsync(user, ct);
    }

    // ── Login / refresh / logout ────────────────────────────────────────────

    public async Task<AuthResultDto> LoginAsync(string email_, string password, string? ip, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email_);
        if (user is null || user.Status == UserStatus.Deleted)
            throw new ForbiddenException("Invalid email or password.");

        if (await userManager.IsLockedOutAsync(user))
            throw new ForbiddenException("Account temporarily locked after repeated failures. Try again later.");

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user); // drives Identity lockout
            throw new ForbiddenException("Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        if (!user.EmailConfirmed)
            throw new DomainRuleException("Please verify your email before signing in.");
        if (user.Status == UserStatus.Suspended)
            throw new ForbiddenException("This account is suspended. Contact the association admin.");

        return await IssueTokensAsync(user, ip, ct);
    }

    public async Task<AuthResultDto> RefreshAsync(string refreshToken, string? ip, CancellationToken ct)
    {
        var hash = jwt.Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new ForbiddenException("Invalid session. Please sign in again.");

        if (stored.RevokedAt is not null)
        {
            // Reuse of a revoked token ⇒ treat the whole family as compromised (Phase 2 §7).
            await db.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
            logger.LogWarning("Refresh token reuse detected for user {UserId}; family revoked", stored.UserId);
            throw new ForbiddenException("Session security issue detected. Please sign in again.");
        }

        if (stored.IsExpired)
            throw new ForbiddenException("Session expired. Please sign in again.");

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || user.Status != UserStatus.Active)
            throw new ForbiddenException("Account is not active.");

        var result = await IssueTokensAsync(user, ip, ct);

        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByTokenHash = jwt.Hash(result.RefreshToken);
        await db.SaveChangesAsync(ct);

        return result;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var hash = jwt.Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null) return; // idempotent

        await db.RefreshTokens
            .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }

    // ── Password reset ──────────────────────────────────────────────────────

    public async Task ForgotPasswordAsync(string email_, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email_);
        if (user is null || !user.EmailConfirmed) return; // never reveal account existence

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = BuildClientLink("/auth/reset-password", email_, token);

        await email.SendAsync(email_, "Reset your NAU password",
            $"""
             <p>Hello {user.FullName},</p>
             <p>We received a request to reset your password. Click the link below to choose a new one:</p>
             <p><a href="{link}">Reset password</a></p>
             <p>If you did not request this, you can safely ignore this email.</p>
             """, ct);
    }

    public async Task ResetPasswordAsync(string email_, string token, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email_)
            ?? throw new NotFoundException("Account", email_);

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            throw new DomainRuleException("The reset link is invalid or has expired. Please request a new one.");

        // New password ⇒ invalidate all sessions.
        await db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<AuthResultDto> IssueTokensAsync(AppUser user, string? ip, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (access, expiresAt) = jwt.CreateAccessToken(user, roles);

        var refresh = jwt.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = jwt.Hash(refresh),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOpt.RefreshTokenDays),
            CreatedByIp = ip,
        });
        await db.SaveChangesAsync(ct);

        return new AuthResultDto(access, expiresAt, refresh,
            new AuthUserDto(user.Id, user.FullName, user.Email!, roles.ToList(), user.EmailConfirmed));
    }

    private async Task SendVerificationEmailAsync(AppUser user, CancellationToken ct)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = BuildClientLink("/auth/verify-email", user.Email!, token);

        await email.SendAsync(user.Email!, "Verify your email — Navodaya Alumni Platform",
            $"""
             <p>Hello {user.FullName},</p>
             <p>Welcome to the Navodaya Alumni platform! Please confirm your email address:</p>
             <p><a href="{link}">Verify my email</a></p>
             <p>Once a Navodayan, always a Navodayan.</p>
             """, ct);
    }

    private string BuildClientLink(string path, string email_, string token)
    {
        var baseUrl = configuration["Client:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        return $"{baseUrl}{path}?email={HttpUtility.UrlEncode(email_)}&token={HttpUtility.UrlEncode(token)}";
    }
}
