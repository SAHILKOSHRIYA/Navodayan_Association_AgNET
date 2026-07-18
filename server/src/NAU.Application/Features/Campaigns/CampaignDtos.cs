using NAU.Domain.Enums;

namespace NAU.Application.Features.Campaigns;

public sealed record CampaignUpdateDto(Guid Id, string Title, string Body, DateTime CreatedAt);

public sealed record DonorDto(string Name, decimal Amount, DateTime At);

/// <summary>Campaign list card (public).</summary>
public sealed record CampaignCardDto(
    Guid Id,
    string Title,
    string Slug,
    string? CoverImageKey,
    decimal GoalAmount,
    decimal RaisedAmount,
    string Currency,
    CampaignStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    int ProgressPct);

/// <summary>Full campaign detail (public) with derived totals, recent donors and updates.</summary>
public sealed record CampaignDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? CoverImageKey,
    decimal GoalAmount,
    decimal RaisedAmount,
    string Currency,
    CampaignStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? OrganizerName,
    int ProgressPct,
    int DonorCount,
    IReadOnlyList<DonorDto> RecentDonors,
    IReadOnlyList<DonorDto> TopDonors,
    IReadOnlyList<CampaignUpdateDto> Updates);

public sealed record UpsertCampaignDto(
    string Title,
    string? Description,
    decimal GoalAmount,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? OrganizerName);

public sealed record PostUpdateDto(string Title, string Body);
