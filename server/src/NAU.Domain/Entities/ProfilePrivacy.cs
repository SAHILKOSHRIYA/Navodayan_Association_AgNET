using NAU.Domain.Enums;

namespace NAU.Domain.Entities;

/// <summary>
/// Visibility settings for the three profile sections. Persisted as a jsonb column
/// (owned type). Defaults chosen for a trusting alumni community while protecting contact info.
/// </summary>
public class ProfilePrivacy
{
    /// <summary>Mobile, address, date of birth.</summary>
    public SectionVisibility Contact { get; set; } = SectionVisibility.Members;

    /// <summary>Company, designation, industry, education, social links.</summary>
    public SectionVisibility Professional { get; set; } = SectionVisibility.Members;

    /// <summary>Batch, house — the Navodaya identity, public by default.</summary>
    public SectionVisibility Academic { get; set; } = SectionVisibility.Public;
}
