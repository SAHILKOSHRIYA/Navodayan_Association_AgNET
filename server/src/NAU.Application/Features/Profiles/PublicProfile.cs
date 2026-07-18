using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Domain.Entities;
using NAU.Domain.Enums;

namespace NAU.Application.Features.Profiles;

/// <summary>Profile as seen by another member — privacy-filtered; hidden fields are simply absent.</summary>
public sealed record PublicProfileDto(
    Guid Id,
    Guid UserId,
    string FullName,
    int? Batch,
    string? House,
    string? Mobile,
    string? Address,
    string? CurrentCity,
    string? CurrentCountry,
    string? Company,
    string? Designation,
    string? Industry,
    string? Education,
    string? Bio,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? PhotoKey,
    IReadOnlyList<string> Skills,
    bool IsVerified);

public sealed record GetProfileByIdQuery(Guid ProfileId, Guid? ViewerId, bool ViewerIsAdmin)
    : IRequest<PublicProfileDto>;

public sealed class GetProfileByIdHandler(IAppDbContext db) : IRequestHandler<GetProfileByIdQuery, PublicProfileDto>
{
    public async Task<PublicProfileDto> Handle(GetProfileByIdQuery q, CancellationToken ct)
    {
        var profile = await db.AlumniProfiles.Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.Id == q.ProfileId, ct)
            ?? throw new NotFoundException("Profile", q.ProfileId);

        var user = await db.FindUserAsync(profile.UserId, ct)
            ?? throw new NotFoundException("User", profile.UserId);

        return ProfilePrivacyFilter.Apply(profile, user.FullName, q.ViewerId, q.ViewerIsAdmin);
    }
}

/// <summary>
/// Single source of truth for profile-privacy enforcement (Phase 2 §7). Used by both the
/// profile-detail endpoint and the directory so visibility rules can never diverge.
/// </summary>
public static class ProfilePrivacyFilter
{
    public static PublicProfileDto Apply(AlumniProfile p, string fullName, Guid? viewerId, bool viewerIsAdmin)
    {
        bool isOwner = viewerId is not null && viewerId == p.UserId;
        bool full = isOwner || viewerIsAdmin;
        bool memberView = full || viewerId is not null; // any authenticated verified member

        bool CanSee(SectionVisibility v) => full
            || v == SectionVisibility.Public
            || (v == SectionVisibility.Members && memberView);

        bool contact = CanSee(p.Privacy.Contact);
        bool prof = CanSee(p.Privacy.Professional);
        bool acad = CanSee(p.Privacy.Academic);

        return new PublicProfileDto(
            p.Id, p.UserId, fullName,
            acad ? p.Batch : null,
            acad ? p.House : null,
            contact ? p.Mobile : null,
            contact ? p.Address : null,
            prof ? p.CurrentCity : null,
            prof ? p.CurrentCountry : null,
            prof ? p.Company : null,
            prof ? p.Designation : null,
            prof ? p.Industry : null,
            prof ? p.Education : null,
            prof ? p.Bio : null,
            prof ? p.LinkedInUrl : null,
            prof ? p.GitHubUrl : null,
            p.PhotoKey,
            p.Skills.Select(s => s.Name).OrderBy(n => n).ToList(),
            p.IsVerified);
    }
}
