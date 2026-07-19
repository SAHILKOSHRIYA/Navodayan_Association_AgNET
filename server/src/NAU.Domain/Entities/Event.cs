using NAU.Domain.Enums;

namespace NAU.Domain.Entities;

/// <summary>An alumni event (meet, webinar, reunion). Phase 1 FR-8.</summary>
public class Event
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
    public string? CoverImageKey { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public Guid CreatedBy { get; set; }

    public List<EventRsvp> Rsvps { get; set; } = [];
    public List<EventGalleryImage> Gallery { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class EventRsvp
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public RsvpStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class EventGalleryImage
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public required string FileKey { get; set; }
    public string? Caption { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
