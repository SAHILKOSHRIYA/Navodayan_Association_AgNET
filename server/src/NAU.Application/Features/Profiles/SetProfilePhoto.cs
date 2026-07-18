using MediatR;
using Microsoft.EntityFrameworkCore;
using NAU.Application.Common.Exceptions;
using NAU.Application.Common.Interfaces;

namespace NAU.Application.Features.Profiles;

/// <summary>Stores an uploaded profile photo and points the profile at its new storage key.</summary>
public sealed record SetProfilePhotoCommand(
    Guid UserId, Stream Content, string FileName, string ContentType) : IRequest<string>;

public sealed class SetProfilePhotoHandler(IAppDbContext db, IFileStorage storage)
    : IRequestHandler<SetProfilePhotoCommand, string>
{
    public async Task<string> Handle(SetProfilePhotoCommand cmd, CancellationToken ct)
    {
        var profile = await db.AlumniProfiles.FirstOrDefaultAsync(p => p.UserId == cmd.UserId, ct)
            ?? throw new DomainRuleException("Create your profile before uploading a photo.");

        var key = await storage.SaveAsync(cmd.Content, cmd.FileName, cmd.ContentType, "profiles", ct);

        var oldKey = profile.PhotoKey;
        profile.PhotoKey = key;
        profile.CompletionPct = ProfileMapping.CalculateCompletion(
            await db.AlumniProfiles.Include(p => p.Skills).FirstAsync(p => p.Id == profile.Id, ct));
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // Best-effort cleanup of the previous photo.
        if (!string.IsNullOrEmpty(oldKey))
            await storage.DeleteAsync(oldKey, ct);

        return key;
    }
}
