using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Interfaces;
using NAU.Domain.Entities;
using NAU.Infrastructure.Identity;

namespace NAU.Infrastructure.Persistence;

/// <summary>
/// Application database context (ASP.NET Identity + domain tables). Implements
/// <see cref="IAppDbContext"/> so Application handlers can query without seeing Identity types.
/// All names map to snake_case via UseSnakeCaseNamingConvention (registered in DI).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IAppDbContext
{
    public DbSet<School> Schools => Set<School>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AlumniProfile> AlumniProfiles => Set<AlumniProfile>();
    public DbSet<Skill> Skills => Set<Skill>();

    /// <summary>Read-only projection of Identity users for the Application layer.</summary>
    public IQueryable<AppUserRef> Users =>
        Set<AppUser>().Select(u => new AppUserRef(u.Id, u.FullName, u.Email!, u.Status, u.EmailConfirmed));

    public Task<AppUserRef?> FindUserAsync(Guid id, CancellationToken ct = default) =>
        Set<AppUser>()
            .Where(u => u.Id == id)
            .Select(u => new AppUserRef(u.Id, u.FullName, u.Email!, u.Status, u.EmailConfirmed))
            .FirstOrDefaultAsync(ct);

    Task<int> IAppDbContext.SaveChangesAsync(CancellationToken cancellationToken) =>
        base.SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("citext");

        // Friendlier table names than AspNetUsers etc.
        builder.Entity<AppUser>().ToTable("users");
        builder.Entity<AppRole>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

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

        builder.Entity<AlumniProfile>(p =>
        {
            p.ToTable("alumni_profiles");
            p.HasIndex(x => x.UserId).IsUnique();
            p.HasIndex(x => new { x.SchoolId, x.Batch });
            p.HasIndex(x => x.CurrentCity);
            p.HasIndex(x => x.Company);
            p.Property(x => x.House).HasMaxLength(50);
            p.Property(x => x.RollNumber).HasMaxLength(30);
            p.Property(x => x.Mobile).HasMaxLength(20);
            p.Property(x => x.Address).HasMaxLength(300);
            p.Property(x => x.CurrentCity).HasMaxLength(100);
            p.Property(x => x.CurrentCountry).HasMaxLength(100);
            p.Property(x => x.Company).HasMaxLength(150);
            p.Property(x => x.Designation).HasMaxLength(150);
            p.Property(x => x.Industry).HasMaxLength(100);
            p.Property(x => x.Education).HasMaxLength(200);
            p.Property(x => x.Bio).HasMaxLength(1000);
            p.Property(x => x.LinkedInUrl).HasMaxLength(300);
            p.Property(x => x.GitHubUrl).HasMaxLength(300);
            p.Property(x => x.PhotoKey).HasMaxLength(300);

            // Privacy stored as a jsonb document (Phase 2 §5.2).
            p.OwnsOne(x => x.Privacy, o => o.ToJson());

            p.HasOne<AppUser>().WithOne().HasForeignKey<AlumniProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            p.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Skill>(s =>
        {
            s.ToTable("skills");
            s.Property(x => x.Name).HasColumnType("citext").HasMaxLength(50);
            s.HasIndex(x => x.Name).IsUnique();
            s.HasMany(x => x.Profiles).WithMany(x => x.Skills)
                .UsingEntity(j => j.ToTable("alumni_skills"));
        });
    }
}
