using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Domain.Entities;

namespace NAU.Application.Features.Profiles;

/// <summary>Creates or updates the caller's own profile. Editing profile details never
/// re-triggers verification loss — verification state is owned by M3.</summary>
public sealed record UpsertMyProfileCommand(Guid UserId, UpsertProfileDto Data) : IRequest<ProfileDto>;

public sealed class UpsertMyProfileHandler(IAppDbContext db) : IRequestHandler<UpsertMyProfileCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(UpsertMyProfileCommand cmd, CancellationToken ct)
    {
        var user = await db.FindUserAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        var school = await db.Schools.FirstOrDefaultAsync(s => s.IsActive, ct)
            ?? throw new DomainRuleException("No active school is configured.");

        var d = cmd.Data;
        var profile = await db.AlumniProfiles
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.UserId == cmd.UserId, ct);

        var isNew = profile is null;
        if (profile is null)
        {
            profile = new AlumniProfile { Id = Guid.NewGuid(), UserId = cmd.UserId, SchoolId = school.Id };
            db.AlumniProfiles.Add(profile);
        }

        profile.Batch = d.Batch;
        profile.House = Trim(d.House);
        profile.RollNumber = Trim(d.RollNumber);
        profile.DateOfBirth = d.DateOfBirth;
        profile.Mobile = Trim(d.Mobile);
        profile.Address = Trim(d.Address);
        profile.CurrentCity = Trim(d.CurrentCity);
        profile.CurrentCountry = Trim(d.CurrentCountry);
        profile.Company = Trim(d.Company);
        profile.Designation = Trim(d.Designation);
        profile.Industry = Trim(d.Industry);
        profile.Education = Trim(d.Education);
        profile.Bio = Trim(d.Bio);
        profile.LinkedInUrl = Trim(d.LinkedInUrl);
        profile.GitHubUrl = Trim(d.GitHubUrl);
        profile.DirectoryVisible = d.DirectoryVisible;

        if (d.Privacy is not null)
        {
            profile.Privacy.Contact = d.Privacy.Contact;
            profile.Privacy.Professional = d.Privacy.Professional;
            profile.Privacy.Academic = d.Privacy.Academic;
        }

        await SyncSkillsAsync(profile, d.Skills ?? [], ct);

        profile.CompletionPct = ProfileMapping.CalculateCompletion(profile);
        profile.UpdatedAt = DateTime.UtcNow;
        if (isNew) profile.CreatedAt = profile.UpdatedAt;

        await db.SaveChangesAsync(ct);

        return ProfileMapping.ToDto(profile, user.FullName, user.Email);
    }

    /// <summary>Resolves skill names to shared <see cref="Skill"/> rows (case-insensitive, creating new ones).</summary>
    private async Task SyncSkillsAsync(AlumniProfile profile, IReadOnlyList<string> names, CancellationToken ct)
    {
        var wanted = names
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        profile.Skills.RemoveAll(s => !wanted.Contains(s.Name, StringComparer.OrdinalIgnoreCase));

        foreach (var name in wanted)
        {
            if (profile.Skills.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
                continue;

            var existing = await db.Skills.FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower(), ct);
            profile.Skills.Add(existing ?? new Skill { Id = Guid.NewGuid(), Name = name });
        }
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
