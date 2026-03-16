namespace ContosoDashboard.Services;

/// <summary>
/// Training/offline implementation: always returns clean.
/// </summary>
public class NoOpFileScanService : IFileScanService
{
    public Task<FileScanResult> ScanAsync(Stream content, string fileName, string? contentType, CancellationToken cancellationToken = default)
        => Task.FromResult(FileScanResult.Clean());
}
