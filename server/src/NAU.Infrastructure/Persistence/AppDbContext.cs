using Microsoft.EntityFrameworkCore;

namespace NAU.Infrastructure.Persistence;

/// <summary>
/// Application database context. M1 replaces the base class with
/// IdentityDbContext&lt;AppUser, AppRole, Guid&gt; when the auth module lands.
/// All entities map to snake_case via UseSnakeCaseNamingConvention (registered in DI).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("citext");
    }
}
