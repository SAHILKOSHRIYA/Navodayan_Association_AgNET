using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Application.Features.Events;
using NAU.Domain.Constants;
using NAU.Domain.Enums;

namespace NAU.Api.Controllers;

[ApiController]
[Route("api/v1/events")]
public sealed class EventsController(ISender mediator, ICurrentUser currentUser) : ControllerBase
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private bool IsAdmin => currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.AssociationAdmin);
    private Guid AdminId => currentUser.Id ?? throw new ForbiddenException();

    public sealed record ChangeStatusRequest(EventStatus Status);
    public sealed record RsvpRequest(RsvpStatus Status);

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<EventCardDto>>>> List(
        [FromQuery] string? scope, [FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListEventsQuery(scope, IsAdmin, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<EventCardDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<EventDetailDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEventQuery(id, currentUser.Id), ct);
        return Ok(ApiResponse<EventDetailDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(UpsertEventDto body, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateEventCommand(AdminId, body), ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.Ok(id, "Event created as draft."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, UpsertEventDto body, CancellationToken ct)
    {
        await mediator.Send(new UpdateEventCommand(id, body), ct);
        return Ok(ApiResponse<object>.Ok(new(), "Event updated."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> ChangeStatus(Guid id, ChangeStatusRequest body, CancellationToken ct)
    {
        await mediator.Send(new ChangeEventStatusCommand(id, body.Status), ct);
        return Ok(ApiResponse<object>.Ok(new(), $"Event status set to {body.Status}."));
    }

    [HttpPost("{id:guid}/rsvp")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Rsvp(Guid id, RsvpRequest body, CancellationToken ct)
    {
        await mediator.Send(new RsvpCommand(id, AdminId, body.Status), ct);
        return Ok(ApiResponse<object>.Ok(new(), "RSVP saved."));
    }

    [HttpGet("{id:guid}/participants")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ParticipantDto>>>> Participants(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetParticipantsQuery(id), ct);
        return Ok(ApiResponse<IReadOnlyList<ParticipantDto>>.Ok(result));
    }

    [HttpPost("{id:guid}/gallery")]
    [Authorize(Policy = "Admin")]
    [RequestSizeLimit(MaxImageBytes + 1024)]
    public async Task<ActionResult<ApiResponse<object>>> AddGalleryImage(Guid id, IFormFile file, [FromForm] string? caption, CancellationToken ct)
    {
        if (file is null || file.Length == 0) throw new DomainRuleException("No file was uploaded.");
        if (file.Length > MaxImageBytes) throw new DomainRuleException("Image must be 5 MB or smaller.");
        if (!AllowedImageTypes.Contains(file.ContentType)) throw new DomainRuleException("Only JPEG, PNG or WebP images are allowed.");

        await using var stream = file.OpenReadStream();
        var key = await mediator.Send(new AddEventGalleryCommand(id, AdminId, stream, file.FileName, file.ContentType, caption), ct);
        return Ok(ApiResponse<object>.Ok(new { fileKey = key }, "Image added."));
    }
}
