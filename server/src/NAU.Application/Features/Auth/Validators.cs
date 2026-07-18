using FluentValidation;

namespace NAU.Application.Features.Auth;

/// <summary>Password policy (Phase 2 §7): min 8 chars, at least one upper, lower and digit.</summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
        .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
        .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
        .Matches("[0-9]").WithMessage("Password must contain a digit.");
}

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).StrongPassword();
    }
}

public sealed class VerifyEmailValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
    }
}

public sealed class ResendVerificationValidator : AbstractValidator<ResendVerificationCommand>
{
    public ResendVerificationValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}
