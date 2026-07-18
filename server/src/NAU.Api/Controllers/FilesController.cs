using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAU.Application.Common.Interfaces;

namespace NAU.Api.Controllers;

/// <summary>
/// Streams stored files by key (profile photos, campaign covers, …). Storage-agnostic:
/// with S3 in production this can be swapped for a presigned-URL redirect (Phase 2 §7).
/// </summary>
[ApiController]
[Route("api/v1/files")]
public sealed class FilesController(IFileStorage storage) : ControllerBase
{
    [HttpGet("{**key}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        var file = await storage.GetAsync(key, ct);
        if (file is null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=86400";
        return File(file.Content, file.ContentType);
    }
}
