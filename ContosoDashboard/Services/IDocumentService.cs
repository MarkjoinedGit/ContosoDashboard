using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<Document> UploadAsync(
        int requestingUserId,
        Stream fileStream,
        string originalFileName,
        string? contentType,
        long fileSizeBytes,
        string title,
        string category,
        string? description,
        int? projectId,
        string? tags,
        int? taskId,
        CancellationToken cancellationToken = default);

    Task<List<Document>> GetMyDocumentsAsync(int requestingUserId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetSharedWithMeAsync(int requestingUserId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId, CancellationToken cancellationToken = default);

    Task<(Document Document, Stream Content)> OpenForDownloadAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<(Document Document, Stream Content)> OpenForPreviewAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);

    Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, string title, string category, string? description, string? tags, CancellationToken cancellationToken = default);
    Task<bool> ReplaceFileAsync(int documentId, int requestingUserId, Stream fileStream, string originalFileName, string? contentType, long fileSizeBytes, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> ShareAsync(int documentId, int requestingUserId, int recipientUserId, string? note, CancellationToken cancellationToken = default);

    Task<List<Document>> SearchAsync(int requestingUserId, string? query, string? category, int? projectId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
}
