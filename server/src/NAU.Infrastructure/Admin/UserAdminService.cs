using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Models;
using NAU.Application.Features.Admin;
using NAU.Domain.Constants;
using NAU.Domain.Entities;
using NAU.Domain.Enums;
using NAU.Infrastructure.Identity;
using NAU.Infrastructure.Persistence;

namespace NAU.Infrastructure.Admin;

public sealed class UserAdminService(UserManager<AppUser> userManager, AppDbContext db) : IUserAdminService
{
    public async Task<PagedResult<UserAdminDto>> ListAsync(
        string? query, string? role, UserStatus? status, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var users = userManager.Users.Where(u => u.Status != UserStatus.Deleted);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim().ToLower()}%";
            users = users.Where(u =>
                EF.Functions.Like(u.FullName.ToLower(), pattern) || EF.Functions.Like(u.Email!.ToLower(), pattern));
        }
        if (status is UserStatus st) users = users.Where(u => u.Status == st);

        // Role filter needs the join tables; resolve matching ids first when requested.
        if (!string.IsNullOrWhiteSpace(role))
        {
            var inRole = await userManager.GetUsersInRoleAsync(role);
            var ids = inRole.Select(u => u.Id).ToHashSet();
            users = users.Where(u => ids.Contains(u.Id));
        }

        users = users.OrderByDescending(u => u.CreatedAt);
        var total = await users.CountAsync(ct);
        var pageUsers = await users.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var items = new List<UserAdminDto>(pageUsers.Count);
        foreach (var u in pageUsers)
        {
            var roles = await userManager.GetRolesAsync(u);
            items.Add(new UserAdminDto(u.Id, u.FullName, u.Email!, roles.ToList(), u.Status, u.EmailConfirmed, u.CreatedAt));
        }
        return new PagedResult<UserAdminDto>(items, page, pageSize, total);
    }

    public async Task SetRolesAsync(Guid userId, IReadOnlyList<string> roles, Guid actorId, string? ip, CancellationToken ct)
    {
        var invalid = roles.Except(Roles.All).ToList();
        if (invalid.Count != 0) throw new DomainRuleException($"Unknown role(s): {string.Join(", ", invalid)}.");

        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new NotFoundException("User", userId);
        var current = await userManager.GetRolesAsync(user);

        await userManager.RemoveFromRolesAsync(user, current.Except(roles));
        await userManager.AddToRolesAsync(user, roles.Except(current));

        await AuditAsync(actorId, "user.roles_changed", "User", userId, $"[{string.Join(",", roles)}]", ip, ct);
    }

    public async Task SetStatusAsync(Guid userId, UserStatus status, Guid actorId, string? ip, CancellationToken ct)
    {
        if (status == UserStatus.Deleted)
            throw new DomainRuleException("Use delete to remove a user.");

        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new NotFoundException("User", userId);
        user.Status = status;
        await userManager.UpdateAsync(user);

        if (status == UserStatus.Suspended)
            await RevokeSessionsAsync(userId, ct);

        await AuditAsync(actorId, "user.status_changed", "User", userId, status.ToString(), ip, ct);
    }

    public async Task SoftDeleteAsync(Guid userId, Guid actorId, string? ip, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new NotFoundException("User", userId);
        user.Status = UserStatus.Deleted;
        user.DeletedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        await RevokeSessionsAsync(userId, ct);
        await AuditAsync(actorId, "user.deleted", "User", userId, null, ip, ct);
    }

    private async Task RevokeSessionsAsync(Guid userId, CancellationToken ct) =>
        await db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);

    private async Task AuditAsync(Guid actorId, string action, string entityType, Guid entityId, string? details, string? ip, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Details = details,
            Ip = ip,
        });
        await db.SaveChangesAsync(ct);
    }
}
