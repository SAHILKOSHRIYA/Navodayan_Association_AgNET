using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NAU.Domain.Entities;
using NAU.Infrastructure.Identity;

namespace NAU.Infrastructure.Persistence;

/// <summary>
/// Application database context (ASP.NET Identity + domain tables).
/// All names map to snake_case via UseSnakeCaseNamingConvention (registered in DI).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<School> Schools => Set<School>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("citext");

        // Friendlier table names than AspNetUsers etc.
        builder.Entity<AppUser>().ToTable("users");
        builder.Entity<AppRole>().ToTable("roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("role_claims");

        builder.Entity<AppUser>(u =>
        {
            u.Property(x => x.FullName).HasMaxLength(120);
            u.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            u.HasIndex(x => x.SchoolId);
        });

        builder.Entity<School>(s =>
        {
            s.ToTable("schools");
            s.Property(x => x.Name).HasMaxLength(200);
            s.Property(x => x.Code).HasMaxLength(50);
            s.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<RefreshToken>(t =>
        {
            t.ToTable("refresh_tokens");
            t.Property(x => x.TokenHash).HasMaxLength(88);
            t.HasIndex(x => x.TokenHash).IsUnique();
            t.HasIndex(x => x.UserId);
            t.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            t.Ignore(x => x.IsActive);
            t.Ignore(x => x.IsExpired);
        });
    }
}
