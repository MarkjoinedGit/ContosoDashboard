namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task SaveAsync(Stream content, string relativeKey, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string relativeKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativeKey, CancellationToken cancellationToken = default);
    bool Exists(string relativeKey);
}
