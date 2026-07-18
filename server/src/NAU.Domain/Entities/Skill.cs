namespace NAU.Domain.Entities;

/// <summary>A normalized skill tag (citext-unique name), shared across alumni profiles.</summary>
public class Skill
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public List<AlumniProfile> Profiles { get; set; } = [];
}
