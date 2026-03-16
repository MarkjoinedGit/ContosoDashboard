namespace ContosoDashboard.Services;

public interface IFileScanService
{
    Task<FileScanResult> ScanAsync(Stream content, string fileName, string? contentType, CancellationToken cancellationToken = default);
}

public sealed class FileScanResult
{
    public bool IsClean { get; init; }
    public string? Message { get; init; }

    public static FileScanResult Clean() => new() { IsClean = true };
    public static FileScanResult Infected(string message) => new() { IsClean = false, Message = message };
}
