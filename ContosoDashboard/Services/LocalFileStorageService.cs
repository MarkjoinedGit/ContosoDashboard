using Microsoft.Extensions.Configuration;

namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _rootPath = configuration["DocumentStorage:RootPath"] ?? "AppData/uploads";
    }

    public bool Exists(string relativeKey)
    {
        var path = GetFullPath(relativeKey);
        return File.Exists(path);
    }

    public async Task SaveAsync(Stream content, string relativeKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativeKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Overwrite by default (replacement workflows will delete or overwrite explicitly).
        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativeKey);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativeKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativeKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string GetFullPath(string relativeKey)
    {
        // Ensure no rooted paths or traversal.
        var sanitized = relativeKey
            .Replace('\\', '/')
            .TrimStart('/');

        if (sanitized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }

        return Path.GetFullPath(Path.Combine(_rootPath, sanitized));
    }
}
