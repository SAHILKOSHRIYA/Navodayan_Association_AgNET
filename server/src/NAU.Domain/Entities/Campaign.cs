using NAU.Domain.Enums;

namespace NAU.Domain.Entities;

/// <summary>
/// A fundraising campaign (Phase 2 §5.2). The amount raised is never stored on the campaign —
/// it is always derived from captured donations (Decision D3), so the ledger cannot be edited by hand.
/// </summary>
public class Campaign
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? CoverImageKey { get; set; }
    public decimal GoalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public string? OrganizerName { get; set; }
    public Guid CreatedBy { get; set; }

    public List<CampaignUpdate> Updates { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

/// <summary>A progress update posted by an admin on a campaign.</summary>
public class CampaignUpdate
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
