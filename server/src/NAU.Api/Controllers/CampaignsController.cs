using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Application.Features.Campaigns;
using NAU.Domain.Enums;

namespace NAU.Api.Controllers;

[ApiController]
[Route("api/v1/campaigns")]
public sealed class CampaignsController(ISender mediator, ICurrentUser currentUser) : ControllerBase
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxCoverBytes = 5 * 1024 * 1024;

    private Guid AdminId => currentUser.Id ?? throw new ForbiddenException();

    public sealed record ChangeStatusRequest(CampaignStatus Status);

    // ── Public reads ────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<CampaignCardDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken ct = default)
    {
        // Admins see all statuses (incl. drafts); everyone else sees active/completed only.
        var includeAll = currentUser.IsInRole("SuperAdmin") || currentUser.IsInRole("AssociationAdmin");
        var result = await mediator.Send(new ListCampaignsQuery(includeAll, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<CampaignCardDto>>.Ok(result));
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CampaignDetailDto>>> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCampaignBySlugQuery(slug), ct);
        return Ok(ApiResponse<CampaignDetailDto>.Ok(result));
    }

    // ── Admin writes ────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(UpsertCampaignDto body, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateCampaignCommand(AdminId, body), ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.Ok(id, "Campaign created as draft."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, UpsertCampaignDto body, CancellationToken ct)
    {
        await mediator.Send(new UpdateCampaignCommand(id, body), ct);
        return Ok(ApiResponse<object>.Ok(new(), "Campaign updated."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> ChangeStatus(Guid id, ChangeStatusRequest body, CancellationToken ct)
    {
        await mediator.Send(new ChangeCampaignStatusCommand(id, body.Status), ct);
        return Ok(ApiResponse<object>.Ok(new(), $"Campaign status set to {body.Status}."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCampaignCommand(id), ct);
        return Ok(ApiResponse<object>.Ok(new(), "Campaign deleted."));
    }

    [HttpPost("{id:guid}/updates")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<Guid>>> PostUpdate(Guid id, PostUpdateDto body, CancellationToken ct)
    {
        var updateId = await mediator.Send(new PostCampaignUpdateCommand(id, AdminId, body), ct);
        return Ok(ApiResponse<Guid>.Ok(updateId, "Update posted."));
    }

    [HttpPost("{id:guid}/cover")]
    [Authorize(Policy = "Admin")]
    [RequestSizeLimit(MaxCoverBytes + 1024)]
    public async Task<ActionResult<ApiResponse<object>>> UploadCover(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) throw new DomainRuleException("No file was uploaded.");
        if (file.Length > MaxCoverBytes) throw new DomainRuleException("Image must be 5 MB or smaller.");
        if (!AllowedImageTypes.Contains(file.ContentType)) throw new DomainRuleException("Only JPEG, PNG or WebP images are allowed.");

        await using var stream = file.OpenReadStream();
        var key = await mediator.Send(new SetCampaignCoverCommand(id, stream, file.FileName, file.ContentType), ct);
        return Ok(ApiResponse<object>.Ok(new { coverImageKey = key }, "Cover updated."));
    }
}
