using NAU.Domain.Enums;

namespace NAU.Application.Features.Donations;

/// <summary>Returned after creating an order — everything the browser checkout needs.</summary>
public sealed record DonationOrderDto(
    Guid DonationId,
    string OrderId,
    string KeyId,
    long AmountMinor,
    string Currency,
    string CampaignTitle);

public sealed record DonationReceiptDto(
    Guid DonationId,
    string? ReceiptNumber,
    string DonorName,
    string CampaignTitle,
    decimal Amount,
    string Currency,
    DonationStatus Status,
    DateTime? CapturedAt);

/// <summary>A row in the caller's own donation history / the admin donation list.</summary>
public sealed record DonationListItemDto(
    Guid Id,
    string CampaignTitle,
    string DonorName,
    bool IsAnonymous,
    decimal Amount,
    string Currency,
    DonationStatus Status,
    string? ReceiptNumber,
    DateTime CreatedAt,
    DateTime? CapturedAt);

public sealed record CreateDonationDto(
    Guid CampaignId,
    decimal Amount,
    string DonorName,
    string DonorEmail,
    bool IsAnonymous);

public sealed record VerifyDonationDto(string OrderId, string PaymentId, string Signature);
