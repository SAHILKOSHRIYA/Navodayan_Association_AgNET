using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;

namespace NAU.Application.Features.Profiles;

/// <summary>Returns the caller's own profile (unfiltered), or null if not yet created.</summary>
public sealed record GetMyProfileQuery(Guid UserId) : IRequest<ProfileDto?>;

public sealed class GetMyProfileHandler(IAppDbContext db) : IRequestHandler<GetMyProfileQuery, ProfileDto?>
{
    public async Task<ProfileDto?> Handle(GetMyProfileQuery q, CancellationToken ct)
    {
        var profile = await db.AlumniProfiles
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.UserId == q.UserId, ct);

        if (profile is null) return null;

        var user = await db.FindUserAsync(q.UserId, ct)
            ?? throw new NotFoundException("User", q.UserId);

        return ProfileMapping.ToDto(profile, user.FullName, user.Email);
    }
}
