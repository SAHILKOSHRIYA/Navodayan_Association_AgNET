using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Application.Features.Directory;

namespace NAU.Api.Controllers;

[ApiController]
[Route("api/v1/directory")]
[Authorize] // members-only; unverified users are handled at the frontend guard
public sealed class DirectoryController(ISender mediator, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Search the alumni directory (verified, directory-visible profiles), privacy-filtered.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResult<DirectoryCardDto>>>> Search(
        [FromQuery] string? name,
        [FromQuery] int? batch,
        [FromQuery] string? company,
        [FromQuery] string? city,
        [FromQuery] string? country,
        [FromQuery] string? industry,
        [FromQuery] string? skill,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var viewerId = currentUser.Id ?? throw new ForbiddenException();
        var result = await mediator.Send(new DirectorySearchQuery(
            viewerId, name, batch, company, city, country, industry, skill, sort, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<DirectoryCardDto>>.Ok(result));
    }
}
