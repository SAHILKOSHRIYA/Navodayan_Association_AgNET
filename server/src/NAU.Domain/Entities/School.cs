namespace NAU.Domain.Entities;

/// <summary>
/// A Jawahar Navodaya Vidyalaya. V1 seeds exactly one (JNV Raipur) but every
/// domain table references a school so multi-school needs no schema change (Phase 2 §1).
/// </summary>
public class School
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    /// <summary>Stable unique code, e.g. "JNV-RAIPUR".</summary>
    public required string Code { get; set; }
    public string? District { get; set; }
    public string? State { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
