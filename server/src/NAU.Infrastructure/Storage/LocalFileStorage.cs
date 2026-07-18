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
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ContentTypes.TryGetContentType(fileName, out _) ? ext : ".bin";

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
