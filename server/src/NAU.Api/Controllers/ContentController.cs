using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAU.Application.Common.Models;
using NAU.Application.Features.Content;

namespace NAU.Api.Controllers;

[ApiController]
[Route("api/v1/content")]
public sealed class ContentController(ISender mediator) : ControllerBase
{
    /// <summary>Aggregated landing-page content: stats, latest campaigns, upcoming events, announcements.</summary>
    [HttpGet("home")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<HomeContentDto>>> Home(CancellationToken ct)
    {
        var result = await mediator.Send(new GetHomeContentQuery(), ct);
        return Ok(ApiResponse<HomeContentDto>.Ok(result));
    }
}
