namespace NAU.Domain.Constants;

/// <summary>Seeded platform roles (Phase 1 §3 hierarchy). Names are stable contract values.</summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string AssociationAdmin = "AssociationAdmin";
    public const string Teacher = "Teacher";
    public const string Alumni = "Alumni";
    public const string Student = "Student";

    public static readonly IReadOnlyList<string> All =
        [SuperAdmin, AssociationAdmin, Teacher, Alumni, Student];

    /// <summary>Roles allowed into the admin portal.</summary>
    public const string AdminPolicy = SuperAdmin + "," + AssociationAdmin;
}
