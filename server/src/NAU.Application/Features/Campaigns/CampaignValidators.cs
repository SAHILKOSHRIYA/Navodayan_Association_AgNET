using FluentValidation;

namespace NAU.Application.Features.Campaigns;

public sealed class UpsertCampaignValidator : AbstractValidator<UpsertCampaignDto>
{
    public UpsertCampaignValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(8000);
        RuleFor(x => x.GoalAmount).GreaterThan(0).WithMessage("Goal amount must be greater than zero.");
        RuleFor(x => x.OrganizerName).MaximumLength(150);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be on or after the start date.");
    }
}

public sealed class CreateCampaignValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignValidator() => RuleFor(x => x.Data).SetValidator(new UpsertCampaignValidator());
}

public sealed class UpdateCampaignValidator : AbstractValidator<UpdateCampaignCommand>
{
    public UpdateCampaignValidator() => RuleFor(x => x.Data).SetValidator(new UpsertCampaignValidator());
}

public sealed class PostUpdateValidator : AbstractValidator<PostCampaignUpdateCommand>
{
    public PostUpdateValidator()
    {
        RuleFor(x => x.Data.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.Body).NotEmpty().MaximumLength(4000);
    }
}
