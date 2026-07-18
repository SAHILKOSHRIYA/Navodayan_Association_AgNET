using FluentValidation;

namespace NAU.Application.Features.Verification;

public sealed class RejectVerificationValidator : AbstractValidator<RejectVerificationCommand>
{
    public RejectVerificationValidator() =>
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A rejection reason is required.")
            .MaximumLength(500);
}
