using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;
using NAU.Application.Common.Models;
using NAU.Application.Features.Profiles;
using NAU.Domain.Constants;

namespace NAU.Api.Controllers;

[ApiController]
[Route("api/v1/profiles")]
[Authorize]
public sealed class ProfilesController(ISender mediator, ICurrentUser currentUser) : ControllerBase
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxPhotoBytes = 5 * 1024 * 1024; // 5 MB

    private Guid UserId => currentUser.Id ?? throw new ForbiddenException();

    /// <summary>The caller's own profile (all fields), or 204 if not yet created.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> GetMine(CancellationToken ct)
    {
        var profile = await mediator.Send(new GetMyProfileQuery(UserId), ct);
        return profile is null
            ? NoContent()
            : Ok(ApiResponse<ProfileDto>.Ok(profile));
    }

    /// <summary>Create or update the caller's own profile.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> UpsertMine(UpsertProfileDto body, CancellationToken ct)
    {
        var result = await mediator.Send(new UpsertMyProfileCommand(UserId, body), ct);
        return Ok(ApiResponse<ProfileDto>.Ok(result, "Profile saved."));
    }

    /// <summary>Upload/replace the caller's profile photo (jpeg/png/webp, ≤ 5 MB).</summary>
    [HttpPost("me/photo")]
    [RequestSizeLimit(MaxPhotoBytes + 1024)]
    public async Task<ActionResult<ApiResponse<object>>> UploadPhoto(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new DomainRuleException("No file was uploaded.");
        if (file.Length > MaxPhotoBytes)
            throw new DomainRuleException("Image must be 5 MB or smaller.");
        if (!AllowedImageTypes.Contains(file.ContentType))
            throw new DomainRuleException("Only JPEG, PNG or WebP images are allowed.");

        await using var stream = file.OpenReadStream();
        var key = await mediator.Send(
            new SetProfilePhotoCommand(UserId, stream, file.FileName, file.ContentType), ct);

        return Ok(ApiResponse<object>.Ok(new { photoKey = key }, "Photo updated."));
    }

    /// <summary>View another member's profile (privacy-filtered). Verified members only.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PublicProfileDto>>> GetById(Guid id, CancellationToken ct)
    {
        var isAdmin = currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.AssociationAdmin);
        var result = await mediator.Send(new GetProfileByIdQuery(id, currentUser.Id, isAdmin), ct);
        return Ok(ApiResponse<PublicProfileDto>.Ok(result));
    }
}
