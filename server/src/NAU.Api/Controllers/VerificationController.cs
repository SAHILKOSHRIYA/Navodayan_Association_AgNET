using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Application.Features.Verification;

namespace NAU.Api.Controllers;

[ApiController]
[Route("api/v1/verification")]
[Authorize]
public sealed class VerificationController(ISender mediator, ICurrentUser currentUser) : ControllerBase
{
    public sealed record ReviewNotesRequest(string? Notes);
    public sealed record RejectRequest(string Reason);

    private Guid UserId => currentUser.Id ?? throw new ForbiddenException();

    /// <summary>Submit the caller's profile for verification.</summary>
    [HttpPost("requests")]
    public async Task<ActionResult<ApiResponse<VerificationRequestDto>>> Submit(CancellationToken ct)
    {
        var result = await mediator.Send(new SubmitVerificationCommand(UserId), ct);
        return Ok(ApiResponse<VerificationRequestDto>.Ok(result, "Submitted for review. You'll be notified once an admin verifies your profile."));
    }

    /// <summary>The caller's latest verification request/status (204 if never submitted).</summary>
    [HttpGet("requests/me")]
    public async Task<ActionResult<ApiResponse<VerificationRequestDto>>> Mine(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyVerificationQuery(UserId), ct);
        return result is null ? NoContent() : Ok(ApiResponse<VerificationRequestDto>.Ok(result));
    }

    // ── Admin ─────────────────────────────────────────────────────────────

    /// <summary>Pending verification queue (admin).</summary>
    [HttpGet("requests")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<VerificationQueueItemDto>>>> Queue(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetVerificationQueueQuery(page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<VerificationQueueItemDto>>.Ok(result));
    }

    /// <summary>Approve a pending request (admin) — grants the verified badge.</summary>
    [HttpPost("requests/{id:guid}/approve")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Approve(Guid id, ReviewNotesRequest body, CancellationToken ct)
    {
        await mediator.Send(new ApproveVerificationCommand(id, UserId, body?.Notes), ct);
        return Ok(ApiResponse<object>.Ok(new(), "Alumnus verified."));
    }

    /// <summary>Reject a pending request with a required reason (admin).</summary>
    [HttpPost("requests/{id:guid}/reject")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Reject(Guid id, RejectRequest body, CancellationToken ct)
    {
        await mediator.Send(new RejectVerificationCommand(id, UserId, body.Reason), ct);
        return Ok(ApiResponse<object>.Ok(new(), "Request rejected."));
    }
}
