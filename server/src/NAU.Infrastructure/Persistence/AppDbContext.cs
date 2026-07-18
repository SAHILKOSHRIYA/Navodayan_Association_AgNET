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
    public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignUpdate> CampaignUpdates => Set<CampaignUpdate>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();

    /// <summary>
    /// Read-only view of Identity users for the Application layer. Mapped to the same
    /// "users" relation (read-only, excluded from migrations) so joins and filters translate
    /// to SQL — while keeping the concrete AppUser type inside Infrastructure.
    /// </summary>
    public IQueryable<AppUserRef> Users => Set<AppUserRef>();

    public Task<AppUserRef?> FindUserAsync(Guid id, CancellationToken ct = default) =>
        Set<AppUserRef>().FirstOrDefaultAsync(u => u.Id == id, ct);

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

        // Read-only projection over the users table (Application layer sees this, not AppUser).
        builder.Entity<AppUserRef>(r =>
        {
            r.HasNoKey();
            r.ToView("users");
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

        builder.Entity<VerificationRequest>(r =>
        {
            r.ToTable("verification_requests");
            r.Property(x => x.RejectionReason).HasMaxLength(500);
            r.Property(x => x.AdminNotes).HasMaxLength(500);
            r.HasIndex(x => new { x.UserId, x.Status });
            r.HasIndex(x => x.Status);
            r.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Campaign>(c =>
        {
            c.ToTable("campaigns");
            c.Property(x => x.Title).HasMaxLength(200);
            c.Property(x => x.Slug).HasMaxLength(220);
            c.Property(x => x.Description).HasMaxLength(8000);
            c.Property(x => x.CoverImageKey).HasMaxLength(300);
            c.Property(x => x.OrganizerName).HasMaxLength(150);
            c.Property(x => x.Currency).HasMaxLength(3);
            c.Property(x => x.GoalAmount).HasColumnType("numeric(12,2)");
            c.HasIndex(x => x.Slug).IsUnique();
            c.HasIndex(x => x.Status);
            c.HasQueryFilter(x => x.DeletedAt == null); // soft delete (Decision D9)
            c.HasMany(x => x.Updates).WithOne().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
            c.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CampaignUpdate>(u =>
        {
            u.ToTable("campaign_updates");
            u.Property(x => x.Title).HasMaxLength(200);
            u.Property(x => x.Body).HasMaxLength(4000);
            u.HasIndex(x => x.CampaignId);
        });

        builder.Entity<Donation>(d =>
        {
            d.ToTable("donations");
            d.Property(x => x.DonorName).HasMaxLength(120);
            d.Property(x => x.DonorEmail).HasMaxLength(256);
            d.Property(x => x.Amount).HasColumnType("numeric(12,2)");
            d.Property(x => x.Currency).HasMaxLength(3);
            d.Property(x => x.RazorpayOrderId).HasMaxLength(64);
            d.Property(x => x.RazorpayPaymentId).HasMaxLength(64);
            d.Property(x => x.RazorpaySignature).HasMaxLength(256);
            d.Property(x => x.ReceiptNumber).HasMaxLength(40);
            d.Property(x => x.FailureReason).HasMaxLength(500);
            d.HasIndex(x => x.RazorpayOrderId).IsUnique();
            d.HasIndex(x => new { x.CampaignId, x.Status });
            d.HasIndex(x => x.UserId);
            d.HasIndex(x => x.ReceiptNumber).IsUnique().HasFilter("receipt_number IS NOT NULL");
            d.HasOne<Campaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentEvent>(e =>
        {
            e.ToTable("payment_events");
            e.Property(x => x.Provider).HasMaxLength(30);
            e.Property(x => x.EventType).HasMaxLength(60);
            e.Property(x => x.ProviderEventId).HasMaxLength(80);
            e.Property(x => x.Payload).HasColumnType("jsonb");
            e.Property(x => x.Error).HasMaxLength(1000);
            e.HasIndex(x => x.ProviderEventId).IsUnique();
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
