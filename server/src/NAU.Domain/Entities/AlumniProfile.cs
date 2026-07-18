namespace NAU.Domain.Entities;

/// <summary>
/// An alumnus's profile (Phase 2 §5.2). One-to-one with an authenticated user.
/// Only <see cref="IsVerified"/> profiles that are <see cref="DirectoryVisible"/> appear
/// in the public directory; verification is granted by an admin (M3).
/// </summary>
public class AlumniProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SchoolId { get; set; }

    // Academic
    public int Batch { get; set; }
    public string? House { get; set; }
    public string? RollNumber { get; set; }

    // Contact
    public DateOnly? DateOfBirth { get; set; }
    public string? Mobile { get; set; }
    public string? Address { get; set; }
    public string? CurrentCity { get; set; }
    public string? CurrentCountry { get; set; }

    // Professional
    public string? Company { get; set; }
    public string? Designation { get; set; }
    public string? Industry { get; set; }
    public string? Education { get; set; }
    public string? Bio { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? GitHubUrl { get; set; }

    public string? PhotoKey { get; set; }

    public ProfilePrivacy Privacy { get; set; } = new();
    public int CompletionPct { get; set; }
    public bool IsVerified { get; set; }
    public bool DirectoryVisible { get; set; } = true;

    public List<Skill> Skills { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
