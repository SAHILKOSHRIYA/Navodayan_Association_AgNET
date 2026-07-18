using NAU.Domain.Enums;

namespace NAU.Domain.Entities;

/// <summary>
/// An alumnus's request to be verified (Phase 2 §5.2, Phase 1 FR-4). Each submission is a new
/// row — never overwritten — so the full verification history is preserved for audit.
/// </summary>
public class VerificationRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? AdminNotes { get; set; }
}
