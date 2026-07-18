using NAU.Domain.Entities;

namespace NAU.Application.Features.Profiles;

internal static class ProfileMapping
{
    /// <summary>
    /// Profile-completion percentage (Phase 3 §5.3 completion ring). Weighted across the
    /// fields that make a profile useful in the directory; batch is implicitly required.
    /// </summary>
    public static int CalculateCompletion(AlumniProfile p)
    {
        var checks = new[]
        {
            p.Batch > 0,
            !string.IsNullOrWhiteSpace(p.House),
            p.DateOfBirth is not null,
            !string.IsNullOrWhiteSpace(p.Mobile),
            !string.IsNullOrWhiteSpace(p.CurrentCity),
            !string.IsNullOrWhiteSpace(p.CurrentCountry),
            !string.IsNullOrWhiteSpace(p.Company),
            !string.IsNullOrWhiteSpace(p.Designation),
            !string.IsNullOrWhiteSpace(p.Industry),
            !string.IsNullOrWhiteSpace(p.Education),
            !string.IsNullOrWhiteSpace(p.Bio),
            !string.IsNullOrWhiteSpace(p.PhotoKey),
            p.Skills.Count > 0,
        };
        return (int)Math.Round(checks.Count(c => c) * 100.0 / checks.Length);
    }

    public static ProfileDto ToDto(AlumniProfile p, string fullName, string email) => new(
        p.Id, p.UserId, fullName, email, p.Batch, p.House, p.RollNumber, p.DateOfBirth,
        p.Mobile, p.Address, p.CurrentCity, p.CurrentCountry, p.Company, p.Designation,
        p.Industry, p.Education, p.Bio, p.LinkedInUrl, p.GitHubUrl, p.PhotoKey,
        p.Skills.Select(s => s.Name).OrderBy(n => n).ToList(),
        new ProfilePrivacyDto(p.Privacy.Contact, p.Privacy.Professional, p.Privacy.Academic),
        p.CompletionPct, p.IsVerified, p.DirectoryVisible);
}
