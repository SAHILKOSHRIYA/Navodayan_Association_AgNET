using FluentValidation;

namespace NAU.Application.Features.Profiles;

public sealed class UpsertMyProfileValidator : AbstractValidator<UpsertMyProfileCommand>
{
    // JNV Raipur's first batch graduated in the early 1990s; guard against typos and future dates.
    private const int MinBatch = 1990;

    public UpsertMyProfileValidator()
    {
        RuleFor(x => x.Data.Batch)
            .InclusiveBetween(MinBatch, DateTime.UtcNow.Year)
            .WithMessage($"Batch year must be between {MinBatch} and {DateTime.UtcNow.Year}.");

        RuleFor(x => x.Data.House).MaximumLength(50);
        RuleFor(x => x.Data.RollNumber).MaximumLength(30);
        RuleFor(x => x.Data.Mobile)
            .Matches(@"^[0-9+\-\s]{7,20}$").When(x => !string.IsNullOrWhiteSpace(x.Data.Mobile))
            .WithMessage("Enter a valid phone number.");
        RuleFor(x => x.Data.CurrentCity).MaximumLength(100);
        RuleFor(x => x.Data.CurrentCountry).MaximumLength(100);
        RuleFor(x => x.Data.Company).MaximumLength(150);
        RuleFor(x => x.Data.Designation).MaximumLength(150);
        RuleFor(x => x.Data.Industry).MaximumLength(100);
        RuleFor(x => x.Data.Bio).MaximumLength(1000);
        RuleFor(x => x.Data.LinkedInUrl)
            .Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.Data.LinkedInUrl))
            .WithMessage("LinkedIn must be a valid URL.");
        RuleFor(x => x.Data.GitHubUrl)
            .Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.Data.GitHubUrl))
            .WithMessage("GitHub must be a valid URL.");
        RuleForEach(x => x.Data.Skills).MaximumLength(50).When(x => x.Data.Skills is not null);
        RuleFor(x => x.Data.Skills!.Count).LessThanOrEqualTo(30).When(x => x.Data.Skills is not null)
            .WithMessage("Please list at most 30 skills.");
    }

    private static bool BeAValidUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}
