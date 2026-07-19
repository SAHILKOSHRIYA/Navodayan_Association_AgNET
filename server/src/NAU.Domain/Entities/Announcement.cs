using NAU.Domain.Enums;

namespace NAU.Domain.Entities;

/// <summary>An admin-published announcement (Phase 1 FR-9).</summary>
public class Announcement
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public AnnouncementCategory Category { get; set; } = AnnouncementCategory.General;
    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.Public;
    public DateTime? PublishedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
