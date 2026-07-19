namespace NAU.Domain.Entities;

/// <summary>Append-only record of a state-changing admin action (Phase 2 §7).</summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public required string Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
