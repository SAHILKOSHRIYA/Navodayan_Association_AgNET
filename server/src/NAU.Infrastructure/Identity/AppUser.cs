using Microsoft.AspNetCore.Identity;
using NAU.Domain.Enums;

namespace NAU.Infrastructure.Identity;

public class AppUser : IdentityUser<Guid>
{
    public required string FullName { get; set; }
    public Guid SchoolId { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

public class AppRole : IdentityRole<Guid>
{
    public AppRole() { }
    public AppRole(string name) : base(name) { }
}
