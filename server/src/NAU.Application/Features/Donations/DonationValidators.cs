using FluentValidation;

namespace NAU.Application.Features.Donations;

public sealed class CreateDonationValidator : AbstractValidator<CreateDonationOrderCommand>
{
    // Razorpay minimum is ₹1; set a sane upper bound to catch fat-finger errors.
    public CreateDonationValidator()
    {
        RuleFor(x => x.Data.Amount)
            .GreaterThanOrEqualTo(1).WithMessage("Minimum donation is ₹1.")
            .LessThanOrEqualTo(10_000_000).WithMessage("For very large donations, please contact the association directly.");
        RuleFor(x => x.Data.DonorName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Data.DonorEmail).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class VerifyDonationValidator : AbstractValidator<VerifyDonationCommand>
{
    public VerifyDonationValidator()
    {
        RuleFor(x => x.Data.OrderId).NotEmpty();
        RuleFor(x => x.Data.PaymentId).NotEmpty();
        RuleFor(x => x.Data.Signature).NotEmpty();
    }
}
