using NAU.Domain.Enums;

namespace NAU.Application.Features.Profiles;

public sealed record ProfilePrivacyDto(
    SectionVisibility Contact,
    SectionVisibility Professional,
    SectionVisibility Academic);

/// <summary>Full profile, returned to the owner (all fields, no privacy filtering).</summary>
public sealed record ProfileDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    int Batch,
    string? House,
    string? RollNumber,
    DateOnly? DateOfBirth,
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
    ProfilePrivacyDto Privacy,
    int CompletionPct,
    bool IsVerified,
    bool DirectoryVisible);

/// <summary>Payload to create or update the caller's own profile.</summary>
public sealed record UpsertProfileDto(
    int Batch,
    string? House,
    string? RollNumber,
    DateOnly? DateOfBirth,
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
    IReadOnlyList<string>? Skills,
    ProfilePrivacyDto? Privacy,
    bool DirectoryVisible = true);
