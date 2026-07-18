namespace NAU.Domain.Enums;

/// <summary>Per-section profile visibility (Phase 3 D14 — privacy is per-section, not per-field).</summary>
public enum SectionVisibility
{
    /// <summary>Visible to anyone, including the public directory.</summary>
    Public = 0,
    /// <summary>Visible only to signed-in verified members.</summary>
    Members = 1,
    /// <summary>Visible only to the owner and admins.</summary>
    Private = 2
}
