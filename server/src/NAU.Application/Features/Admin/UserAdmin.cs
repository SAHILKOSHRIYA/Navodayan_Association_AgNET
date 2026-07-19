using NAU.Application.Common.Models;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Admin;

public sealed record UserAdminDto(
    Guid Id, string FullName, string Email, IReadOnlyList<string> Roles,
    UserStatus Status, bool EmailConfirmed, DateTime CreatedAt);

/// <summary>
/// User administration (Phase 1 FR-11). Implemented in Infrastructure because it needs ASP.NET
/// Identity's UserManager. Every mutating call writes an audit-log entry (Phase 2 §7).
/// </summary>
public interface IUserAdminService
{
    Task<PagedResult<UserAdminDto>> ListAsync(
        string? query, string? role, UserStatus? status, int page, int pageSize, CancellationToken ct);

    Task SetRolesAsync(Guid userId, IReadOnlyList<string> roles, Guid actorId, string? ip, CancellationToken ct);
    Task SetStatusAsync(Guid userId, UserStatus status, Guid actorId, string? ip, CancellationToken ct);
    Task SoftDeleteAsync(Guid userId, Guid actorId, string? ip, CancellationToken ct);
}
