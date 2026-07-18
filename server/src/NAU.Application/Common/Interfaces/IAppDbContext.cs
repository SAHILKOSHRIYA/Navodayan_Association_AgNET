using Microsoft.EntityFrameworkCore;
using NAU.Domain.Entities;

namespace NAU.Application.Common.Interfaces;

/// <summary>
/// Persistence surface exposed to Application handlers (Clean Architecture: the concrete
/// <c>AppDbContext</c> lives in Infrastructure and implements this). Only domain entities
/// are exposed as writable sets; users are a read-only projection (<see cref="AppUserRef"/>).
/// </summary>
public interface IAppDbContext
{
    DbSet<AlumniProfile> AlumniProfiles { get; }
    DbSet<Skill> Skills { get; }
    DbSet<School> Schools { get; }

    /// <summary>Read-only, SQL-translatable view of authenticated users (for joins/listings).</summary>
    IQueryable<AppUserRef> Users { get; }

    /// <summary>Single-user lookup (filters before projecting, so it always translates).</summary>
    Task<AppUserRef?> FindUserAsync(Guid id, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
