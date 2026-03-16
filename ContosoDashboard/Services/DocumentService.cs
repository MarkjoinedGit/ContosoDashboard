using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IFileScanService _scan;
    private readonly INotificationService _notifications;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService storage,
        IFileScanService scan,
        INotificationService notifications)
    {
        _context = context;
        _storage = storage;
        _scan = scan;
        _notifications = notifications;
    }

    public async Task<Document> UploadAsync(
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
        CancellationToken cancellationToken = default)
    {
        ValidateMetadata(title, category);
        ValidateFile(originalFileName, fileSizeBytes);

        // If project-scoped, user must be member or PM/admin.
        if (projectId.HasValue)
        {
            var canUploadToProject = await CanAccessProjectAsync(projectId.Value, requestingUserId, cancellationToken);
            if (!canUploadToProject)
            {
                throw new InvalidOperationException("Not authorized to upload to this project.");
            }
        }

        var ext = Path.GetExtension(originalFileName);
        var safeContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;

        // Some incoming streams (e.g., from Blazor InputFile) don't support seeking.
        // We buffer once so we can scan and then store without relying on Position.
        await using var buffered = new MemoryStream();
        await fileStream.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;

        // Virus scan - read-only in training, but keep the contract.
        var scanResult = await _scan.ScanAsync(buffered, originalFileName, safeContentType, cancellationToken);
        if (!scanResult.IsClean)
        {
            throw new InvalidOperationException(scanResult.Message ?? "File failed security scan.");
        }

        // Generate unique storage key BEFORE inserting DB.
        var key = GenerateStorageKey(requestingUserId, projectId, ext);

        // Save file first.
    buffered.Position = 0;
    await _storage.SaveAsync(buffered, key, cancellationToken);

        // Now create DB record.
        var now = DateTime.UtcNow;
        var doc = new Document
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Category = category.Trim(),
            Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim(),
            UploaderUserId = requestingUserId,
            ProjectId = projectId,
            TaskId = taskId,
            FileNameOriginal = SafeOriginalName(originalFileName),
            FileExtension = ext,
            FileType = safeContentType,
            FileSizeBytes = fileSizeBytes,
            StorageKey = key,
            UploadedUtc = now,
            UpdatedUtc = now,
            IsDeleted = false
        };

        _context.Documents.Add(doc);
        await _context.SaveChangesAsync(cancellationToken);

        await AddActivityAsync(doc.DocumentId, requestingUserId, "Upload", null, cancellationToken);

        // Notify project members when a project document is added.
        if (projectId.HasValue)
        {
            await NotifyProjectMembersDocumentAddedAsync(projectId.Value, doc, requestingUserId, cancellationToken);
        }

        return doc;
    }

    public Task<List<Document>> GetMyDocumentsAsync(int requestingUserId, CancellationToken cancellationToken = default)
        => _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Where(d => !d.IsDeleted && d.UploaderUserId == requestingUserId)
            .OrderByDescending(d => d.UploadedUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

    public Task<List<Document>> GetSharedWithMeAsync(int requestingUserId, CancellationToken cancellationToken = default)
        => _context.DocumentShares
            .AsNoTracking()
            .Include(s => s.Document)
                .ThenInclude(d => d.Project)
            .Where(s => s.RecipientUserId == requestingUserId && !s.Document.IsDeleted)
            .OrderByDescending(s => s.CreatedUtc)
            .Select(s => s.Document)
            .Take(500)
            .ToListAsync(cancellationToken);

    public async Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessProjectAsync(projectId, requestingUserId, cancellationToken))
        {
            return new List<Document>();
        }

        return await _context.Documents
            .AsNoTracking()
            .Include(d => d.Uploader)
            .Where(d => !d.IsDeleted && d.ProjectId == projectId)
            .OrderByDescending(d => d.UploadedUtc)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<(Document Document, Stream Content)> OpenForDownloadAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var doc = await GetAuthorizedDocumentAsync(documentId, requestingUserId, cancellationToken);
        var stream = await _storage.OpenReadAsync(doc.StorageKey, cancellationToken);
        await AddActivityAsync(doc.DocumentId, requestingUserId, "Download", null, cancellationToken);
        return (doc, stream);
    }

    public async Task<(Document Document, Stream Content)> OpenForPreviewAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var doc = await GetAuthorizedDocumentAsync(documentId, requestingUserId, cancellationToken);
        if (!DocumentRules.PreviewableMimeTypes.Contains(doc.FileType))
        {
            throw new InvalidOperationException("This document type cannot be previewed.");
        }

        var stream = await _storage.OpenReadAsync(doc.StorageKey, cancellationToken);
        await AddActivityAsync(doc.DocumentId, requestingUserId, "Preview", null, cancellationToken);
        return (doc, stream);
    }

    public async Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, string title, string category, string? description, string? tags, CancellationToken cancellationToken = default)
    {
        ValidateMetadata(title, category);

        var doc = await GetAuthorizedDocumentForWriteAsync(documentId, requestingUserId, cancellationToken);
        doc.Title = title.Trim();
        doc.Category = category.Trim();
        doc.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        doc.Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim();
        doc.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await AddActivityAsync(doc.DocumentId, requestingUserId, "MetadataUpdate", null, cancellationToken);
        return true;
    }

    public async Task<bool> ReplaceFileAsync(int documentId, int requestingUserId, Stream fileStream, string originalFileName, string? contentType, long fileSizeBytes, CancellationToken cancellationToken = default)
    {
        ValidateFile(originalFileName, fileSizeBytes);

        var doc = await GetAuthorizedDocumentForWriteAsync(documentId, requestingUserId, cancellationToken);

        var ext = Path.GetExtension(originalFileName);
        var safeContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;

        // Buffer (InputFile streams may not support seeking)
        await using var buffered = new MemoryStream();
        await fileStream.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;

        // Scan
        var scanResult = await _scan.ScanAsync(buffered, originalFileName, safeContentType, cancellationToken);
        if (!scanResult.IsClean)
        {
            throw new InvalidOperationException(scanResult.Message ?? "File failed security scan.");
        }

        // New key so old content isn't reused.
        var newKey = GenerateStorageKey(doc.UploaderUserId, doc.ProjectId, ext);

    buffered.Position = 0;
    await _storage.SaveAsync(buffered, newKey, cancellationToken);

        // Delete old file best-effort.
        await _storage.DeleteAsync(doc.StorageKey, cancellationToken);

        doc.StorageKey = newKey;
        doc.FileNameOriginal = SafeOriginalName(originalFileName);
        doc.FileExtension = ext;
        doc.FileType = safeContentType;
        doc.FileSizeBytes = fileSizeBytes;
        doc.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await AddActivityAsync(doc.DocumentId, requestingUserId, "ReplaceFile", null, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var doc = await GetAuthorizedDocumentForDeleteAsync(documentId, requestingUserId, cancellationToken);
        if (doc.IsDeleted) return true;

        doc.IsDeleted = true;
        doc.DeletedUtc = DateTime.UtcNow;
        doc.UpdatedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Remove file physically.
        await _storage.DeleteAsync(doc.StorageKey, cancellationToken);

        await AddActivityAsync(doc.DocumentId, requestingUserId, "Delete", null, cancellationToken);
        return true;
    }

    public async Task<bool> ShareAsync(int documentId, int requestingUserId, int recipientUserId, string? note, CancellationToken cancellationToken = default)
    {
        if (recipientUserId <= 0) return false;
        if (recipientUserId == requestingUserId) return false;

        var doc = await GetAuthorizedDocumentForWriteAsync(documentId, requestingUserId, cancellationToken);

        // Ensure recipient exists.
        var recipient = await _context.Users.FindAsync(new object[] { recipientUserId }, cancellationToken);
        if (recipient == null) return false;

        var share = new DocumentShare
        {
            DocumentId = doc.DocumentId,
            RecipientUserId = recipientUserId,
            SharedByUserId = requestingUserId,
            CreatedUtc = DateTime.UtcNow,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };

        _context.DocumentShares.Add(share);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Likely duplicate share; treat as no-op.
            return false;
        }

        await AddActivityAsync(doc.DocumentId, requestingUserId, "Share", $"Shared with user {recipientUserId}", cancellationToken);

        await _notifications.CreateNotificationAsync(new Notification
        {
            UserId = recipientUserId,
            Title = "Document Shared",
            Message = $"A document was shared with you: {doc.Title}",
            Type = NotificationType.SystemAnnouncement,
            Priority = NotificationPriority.Important
        });

        return true;
    }

    public async Task<List<Document>> SearchAsync(int requestingUserId, string? query, string? category, int? projectId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        // Limit scope to accessible documents: owned, shared, project membership.
        var accessibleDocIdsQuery = _context.Documents
            .Where(d => !d.IsDeleted)
            .Where(d => d.UploaderUserId == requestingUserId);

        var sharedDocIdsQuery = _context.DocumentShares
            .Where(s => s.RecipientUserId == requestingUserId)
            .Select(s => s.DocumentId);

        // Project docs where member/PM.
        var projectIdsQuery = _context.Projects
            .Where(p => p.ProjectManagerId == requestingUserId || p.ProjectMembers.Any(pm => pm.UserId == requestingUserId))
            .Select(p => p.ProjectId);

        var projectDocIdsQuery = _context.Documents
            .Where(d => d.ProjectId.HasValue && projectIdsQuery.Contains(d.ProjectId.Value))
            .Select(d => d.DocumentId);

        var allAccessibleIds = accessibleDocIdsQuery.Select(d => d.DocumentId)
            .Union(sharedDocIdsQuery)
            .Union(projectDocIdsQuery);

        var docs = _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Where(d => allAccessibleIds.Contains(d.DocumentId));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            docs = docs.Where(d => d.Title.Contains(q) || (d.Description != null && d.Description.Contains(q)) || (d.Tags != null && d.Tags.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            docs = docs.Where(d => d.Category == category);
        }

        if (projectId.HasValue)
        {
            docs = docs.Where(d => d.ProjectId == projectId.Value);
        }

        if (fromUtc.HasValue)
        {
            docs = docs.Where(d => d.UploadedUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            docs = docs.Where(d => d.UploadedUtc <= toUtc.Value);
        }

        return await docs
            .OrderByDescending(d => d.UploadedUtc)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    private static void ValidateFile(string originalFileName, long fileSizeBytes)
    {
        if (fileSizeBytes <= 0)
        {
            throw new InvalidOperationException("File is empty.");
        }

        if (fileSizeBytes > DocumentRules.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File exceeds maximum size of {DocumentRules.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !DocumentRules.AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException("Unsupported file type.");
        }
    }

    private static void ValidateMetadata(string title, string category)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("Category is required.");
        }

        if (!DocumentRules.AllowedCategories.Contains(category))
        {
            throw new InvalidOperationException("Invalid category.");
        }
    }

    private async Task<Document> GetAuthorizedDocumentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken)
    {
        var doc = await _context.Documents
            .Include(d => d.Project)
                .ThenInclude(p => p!.ProjectMembers)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);

        if (doc == null)
        {
            throw new KeyNotFoundException("Document not found.");
        }

        if (!await CanAccessDocumentAsync(doc, requestingUserId, cancellationToken))
        {
            throw new InvalidOperationException("Not authorized.");
        }

        return doc;
    }

    private async Task<Document> GetAuthorizedDocumentForWriteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken)
    {
        var doc = await _context.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);

        if (doc == null)
        {
            throw new KeyNotFoundException("Document not found.");
        }

        // Owners can edit; PM can manage docs within their projects.
        var isOwner = doc.UploaderUserId == requestingUserId;
        var isProjectManager = doc.ProjectId.HasValue && await IsProjectManagerAsync(doc.ProjectId.Value, requestingUserId, cancellationToken);
        var isAdmin = await IsAdminAsync(requestingUserId, cancellationToken);

        if (!isOwner && !isProjectManager && !isAdmin)
        {
            throw new InvalidOperationException("Not authorized.");
        }

        return doc;
    }

    private async Task<Document> GetAuthorizedDocumentForDeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken)
    {
        // Same as write for now.
        return await GetAuthorizedDocumentForWriteAsync(documentId, requestingUserId, cancellationToken);
    }

    private async Task<bool> CanAccessDocumentAsync(Document doc, int requestingUserId, CancellationToken cancellationToken)
    {
        if (doc.UploaderUserId == requestingUserId) return true;

        if (await IsAdminAsync(requestingUserId, cancellationToken)) return true;

        // Project scope
        if (doc.ProjectId.HasValue)
        {
            if (await CanAccessProjectAsync(doc.ProjectId.Value, requestingUserId, cancellationToken)) return true;
        }

        // Explicit share
        var isShared = await _context.DocumentShares
            .AnyAsync(s => s.DocumentId == doc.DocumentId && s.RecipientUserId == requestingUserId, cancellationToken);

        return isShared;
    }

    private Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken)
        => _context.Users.AnyAsync(u => u.UserId == userId && u.Role == UserRole.Administrator, cancellationToken);

    private async Task<bool> CanAccessProjectAsync(int projectId, int userId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);
        if (project == null) return false;

        if (project.ProjectManagerId == userId) return true;
        if (project.ProjectMembers.Any(pm => pm.UserId == userId)) return true;
        if (await IsAdminAsync(userId, cancellationToken)) return true;

        return false;
    }

    private Task<bool> IsProjectManagerAsync(int projectId, int userId, CancellationToken cancellationToken)
        => _context.Projects.AnyAsync(p => p.ProjectId == projectId && p.ProjectManagerId == userId, cancellationToken);

    private string GenerateStorageKey(int uploaderUserId, int? projectId, string extension)
    {
        var projectSegment = projectId.HasValue ? projectId.Value.ToString() : "personal";
        var guid = Guid.NewGuid().ToString("N");
        return $"{uploaderUserId}/{projectSegment}/{guid}{extension}";
    }

    private static string SafeOriginalName(string original)
    {
        // Keep only file name, avoid paths.
        return Path.GetFileName(original);
    }

    private async Task AddActivityAsync(int documentId, int userId, string activityType, string? details, CancellationToken cancellationToken)
    {
        _context.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = documentId,
            UserId = userId,
            ActivityType = activityType,
            CreatedUtc = DateTime.UtcNow,
            Details = details
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyProjectMembersDocumentAddedAsync(int projectId, Document doc, int uploadingUserId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);
        if (project == null) return;

        var recipientIds = project.ProjectMembers
            .Select(pm => pm.UserId)
            .Append(project.ProjectManagerId)
            .Distinct()
            .Where(id => id != uploadingUserId)
            .ToList();

        foreach (var recipientId in recipientIds)
        {
            await _notifications.CreateNotificationAsync(new Notification
            {
                UserId = recipientId,
                Title = "New Project Document",
                Message = $"A new document was added to your project: {doc.Title}",
                Type = NotificationType.ProjectUpdate,
                Priority = NotificationPriority.Informational
            });
        }
    }
}
