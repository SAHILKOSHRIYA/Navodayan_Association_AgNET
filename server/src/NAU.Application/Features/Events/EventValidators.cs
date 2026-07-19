using FluentValidation;

namespace NAU.Application.Features.Events;

public sealed class UpsertEventValidator : AbstractValidator<UpsertEventDto>
{
    public UpsertEventValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.EventDate)
            .When(x => x.EndDate.HasValue).WithMessage("End must be on or after the start.");
    }
}

public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator() => RuleFor(x => x.Data).SetValidator(new UpsertEventValidator());
}

public sealed class UpdateEventValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventValidator() => RuleFor(x => x.Data).SetValidator(new UpsertEventValidator());
}
