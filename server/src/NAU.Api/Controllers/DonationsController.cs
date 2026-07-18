using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Application.Features.Donations;
using NAU.Domain.Constants;
using NAU.Domain.Enums;

namespace NAU.Api.Controllers;

[ApiController]
[Route("api/v1/donations")]
public sealed class DonationsController(ISender mediator, ICurrentUser currentUser) : ControllerBase
{
    private bool IsAdmin => currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.AssociationAdmin);

    /// <summary>Create a payment order. Anonymous (guest) donations are allowed (Decision D7).</summary>
    [HttpPost("order")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<DonationOrderDto>>> CreateOrder(CreateDonationDto body, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateDonationOrderCommand(currentUser.Id, body), ct);
        return Ok(ApiResponse<DonationOrderDto>.Ok(result));
    }

    /// <summary>Verify the checkout callback signature and capture the donation.</summary>
    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DonationReceiptDto>>> Verify(VerifyDonationDto body, CancellationToken ct)
    {
        var result = await mediator.Send(new VerifyDonationCommand(body), ct);
        return Ok(ApiResponse<DonationReceiptDto>.Ok(result, "Thank you! Your donation is confirmed."));
    }

    /// <summary>The caller's own donation history.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResult<DonationListItemDto>>>> Mine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new ForbiddenException();
        var result = await mediator.Send(new GetMyDonationsQuery(userId, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<DonationListItemDto>>.Ok(result));
    }

    /// <summary>Receipt for a captured donation (owner or admin).</summary>
    [HttpGet("{id:guid}/receipt")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DonationReceiptDto>>> Receipt(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDonationReceiptQuery(id, currentUser.Id, IsAdmin), ct);
        return Ok(ApiResponse<DonationReceiptDto>.Ok(result));
    }

    /// <summary>Admin: all donations, filterable by campaign and status.</summary>
    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<DonationListItemDto>>>> List(
        [FromQuery] Guid? campaign, [FromQuery] DonationStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListDonationsQuery(campaign, status, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<DonationListItemDto>>.Ok(result));
    }
}
