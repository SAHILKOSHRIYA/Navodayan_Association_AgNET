using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using NAU.Application.Common.Interfaces;

namespace NAU.Infrastructure.Storage;

/// <summary>
/// Development file storage backed by the local disk. Implements the same
/// <see cref="IFileStorage"/> contract as the future S3 provider, so swapping is a DI change only.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public LocalFileStorage(IConfiguration configuration)
    {
        _root = configuration["Storage:LocalRootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "storage");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        // Derive the extension from the (already-validated) content type, NOT the client-supplied
        // filename. This prevents a caller from smuggling an executable/HTML extension past the
        // content-type check and having it served back inline (stored XSS).
        var ext = ExtensionFor(contentType) ?? SafeExtension(fileName);

        var key = $"{folder}/{Guid.NewGuid():N}{ext}".Replace("\\", "/");
        var fullPath = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, ct);
        return key;
    }

    public Task<StoredFile?> GetAsync(string key, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(key);
        if (!File.Exists(fullPath)) return Task.FromResult<StoredFile?>(null);

        if (!ContentTypes.TryGetContentType(fullPath, out var contentType))
            contentType = "application/octet-stream";

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<StoredFile?>(new StoredFile(stream, contentType, Path.GetFileName(fullPath)));
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(key);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    /// <summary>Safe extension for a known-allowed content type, or null if unrecognised.</summary>
    private static string? ExtensionFor(string contentType) => contentType?.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "application/pdf" => ".pdf",
        _ => null,
    };

    /// <summary>Fallback: keep only a short alphanumeric extension from the filename, else ".bin".</summary>
    private static string SafeExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return System.Text.RegularExpressions.Regex.IsMatch(ext, @"^\.[a-z0-9]{1,5}$") ? ext : ".bin";
    }

    /// <summary>Resolves a key to a path under the storage root, rejecting path traversal.</summary>
    private string ResolvePath(string key)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, key));
        var rootFull = Path.GetFullPath(_root);
        if (!fullPath.StartsWith(rootFull, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");
        return fullPath;
    }
}
