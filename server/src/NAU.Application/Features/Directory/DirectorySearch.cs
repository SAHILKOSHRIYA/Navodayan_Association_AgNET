using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Application.Features.Profiles;

namespace NAU.Application.Features.Directory;

/// <summary>A directory result card (privacy-filtered for the searching member).</summary>
public sealed record DirectoryCardDto(
    Guid ProfileId,
    Guid UserId,
    string FullName,
    int? Batch,
    string? House,
    string? Company,
    string? Designation,
    string? Industry,
    string? CurrentCity,
    string? CurrentCountry,
    string? PhotoKey,
    IReadOnlyList<string> Skills);

/// <summary>
/// Directory search (Phase 1 FR-5). Members-only; returns verified, directory-visible
/// profiles matching the (AND-combined) filters, privacy-filtered for the viewer.
/// </summary>
public sealed record DirectorySearchQuery(
    Guid ViewerId,
    string? Name,
    int? Batch,
    string? Company,
    string? City,
    string? Country,
    string? Industry,
    string? Skill,
    string? Sort,
    int Page,
    int PageSize) : IRequest<PagedResult<DirectoryCardDto>>;

public sealed class DirectorySearchHandler(IAppDbContext db)
    : IRequestHandler<DirectorySearchQuery, PagedResult<DirectoryCardDto>>
{
    public async Task<PagedResult<DirectoryCardDto>> Handle(DirectorySearchQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 50);

        var profiles = db.AlumniProfiles.Where(p => p.IsVerified && p.DirectoryVisible);

        if (q.Batch is int batch)
            profiles = profiles.Where(p => p.Batch == batch);
        if (Has(q.Company))
            profiles = profiles.Where(p => p.Company != null && EF.Functions.Like(p.Company!.ToLower(), Contains(q.Company)));
        if (Has(q.City))
            profiles = profiles.Where(p => p.CurrentCity != null && EF.Functions.Like(p.CurrentCity!.ToLower(), Contains(q.City)));
        if (Has(q.Country))
            profiles = profiles.Where(p => p.CurrentCountry != null && EF.Functions.Like(p.CurrentCountry!.ToLower(), Contains(q.Country)));
        if (Has(q.Industry))
            profiles = profiles.Where(p => p.Industry != null && EF.Functions.Like(p.Industry!.ToLower(), Contains(q.Industry)));
        if (Has(q.Skill))
            profiles = profiles.Where(p => p.Skills.Any(s => s.Name == q.Skill!.Trim()));
        if (Has(q.Name))
        {
            var pattern = Contains(q.Name);
            var matchingUsers = db.Users.Where(u => EF.Functions.Like(u.FullName.ToLower(), pattern)).Select(u => u.Id);
            profiles = profiles.Where(p => matchingUsers.Contains(p.UserId));
        }

        // Join users so we can sort by name and page deterministically.
        var joined = from p in profiles
                     join u in db.Users on p.UserId equals u.Id
                     select new { p.Id, p.Batch, p.CurrentCity, u.FullName };

        joined = q.Sort?.ToLowerInvariant() switch
        {
            "batch" => joined.OrderByDescending(x => x.Batch).ThenBy(x => x.FullName),
            "city" => joined.OrderBy(x => x.CurrentCity).ThenBy(x => x.FullName),
            _ => joined.OrderBy(x => x.FullName), // default: name
        };

        var total = await joined.CountAsync(ct);
        var pageIds = await joined.Skip((page - 1) * size).Take(size).Select(x => x.Id).ToListAsync(ct);

        // Load full profiles (+ skills) for the page, then preserve the sorted order.
        var loaded = await db.AlumniProfiles.Include(p => p.Skills)
            .Where(p => pageIds.Contains(p.Id))
            .ToListAsync(ct);
        var ordered = pageIds.Select(id => loaded.First(p => p.Id == id)).ToList();

        var userIds = ordered.Select(p => p.UserId).ToList();
        var names = await db.Users.Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var items = ordered.Select(p =>
        {
            // Single privacy-enforcement path (member viewer).
            var view = ProfilePrivacyFilter.Apply(p, names[p.UserId], q.ViewerId, viewerIsAdmin: false);
            return new DirectoryCardDto(
                view.Id, view.UserId, view.FullName, view.Batch, view.House,
                view.Company, view.Designation, view.Industry, view.CurrentCity, view.CurrentCountry,
                view.PhotoKey, view.Skills);
        }).ToList();

        return new PagedResult<DirectoryCardDto>(items, page, size, total);
    }

    private static bool Has(string? s) => !string.IsNullOrWhiteSpace(s);

    /// <summary>Case-insensitive "contains" LIKE pattern (both sides lower-cased by the caller/column).</summary>
    private static string Contains(string? s) => $"%{s!.Trim().ToLowerInvariant()}%";
}
