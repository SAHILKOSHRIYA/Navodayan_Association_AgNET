namespace NAU.Application.Common.Interfaces;

public sealed record StoredFile(Stream Content, string ContentType, string FileName);

/// <summary>
/// Object-storage abstraction (Phase 2 §2 — S3-compatible in production, local disk in dev).
/// Handlers work only with opaque keys; the implementation decides physical layout and URLs.
/// </summary>
public interface IFileStorage
{
    /// <summary>Persists a file under <paramref name="folder"/> and returns its storage key.</summary>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default);

    Task<StoredFile?> GetAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}
