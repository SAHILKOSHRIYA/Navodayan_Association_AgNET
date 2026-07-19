using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Application.Features.Announcements;
using NAU.Domain.Constants;
using NAU.Domain.Enums;

namespace NAU.Api.Controllers;

[ApiController]
[Route("api/v1/announcements")]
public sealed class AnnouncementsController(ISender mediator, ICurrentUser currentUser) : ControllerBase
{
    private bool IsAdmin => currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.AssociationAdmin);
    private Guid AdminId => currentUser.Id ?? throw new ForbiddenException();

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<AnnouncementDto>>>> List(
        [FromQuery] AnnouncementCategory? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListAnnouncementsQuery(
            category, currentUser.IsAuthenticated, currentUser.IsInRole(Roles.Student), IsAdmin, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<AnnouncementDto>>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(UpsertAnnouncementDto body, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateAnnouncementCommand(AdminId, body), ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.Ok(id, "Announcement created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, UpsertAnnouncementDto body, CancellationToken ct)
    {
        await mediator.Send(new UpdateAnnouncementCommand(id, body), ct);
        return Ok(ApiResponse<object>.Ok(new(), "Announcement updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteAnnouncementCommand(id), ct);
        return Ok(ApiResponse<object>.Ok(new(), "Announcement deleted."));
    }
}
