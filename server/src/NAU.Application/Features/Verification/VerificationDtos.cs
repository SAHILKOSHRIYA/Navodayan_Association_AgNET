using NAU.Domain.Enums;

namespace NAU.Application.Features.Verification;

/// <summary>An alumnus's own verification request (for their status screen).</summary>
public sealed record VerificationRequestDto(
    Guid Id,
    VerificationStatus Status,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? RejectionReason);

/// <summary>A pending request in the admin queue, with enough profile context to decide.</summary>
public sealed record VerificationQueueItemDto(
    Guid RequestId,
    Guid UserId,
    Guid ProfileId,
    string FullName,
    string Email,
    int Batch,
    string? House,
    string? CurrentCity,
    string? Company,
    string? Designation,
    int CompletionPct,
    DateTime SubmittedAt);
