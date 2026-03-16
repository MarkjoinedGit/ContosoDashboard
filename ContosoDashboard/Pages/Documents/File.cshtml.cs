using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ContosoDashboard.Pages.DocumentFiles;

[Authorize]
public class FileModel : PageModel
{
    private readonly IDocumentService _documents;

    public FileModel(IDocumentService documents)
    {
        _documents = documents;
    }

    public async Task<IActionResult> OnGetAsync(int id, string mode = "download", CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return Forbid();
        }

        try
        {
            var isPreview = string.Equals(mode, "preview", StringComparison.OrdinalIgnoreCase);
            var result = isPreview
                ? await _documents.OpenForPreviewAsync(id, userId, cancellationToken)
                : await _documents.OpenForDownloadAsync(id, userId, cancellationToken);

            // Stream back through ASP.NET Core; content type comes from stored metadata.
            if (isPreview)
            {
                return File(result.Content, result.Document.FileType);
            }

            var downloadName = string.IsNullOrWhiteSpace(result.Document.FileNameOriginal)
                ? (result.Document.Title + result.Document.FileExtension)
                : result.Document.FileNameOriginal;

            return File(result.Content, result.Document.FileType, downloadName);
        }
        catch
        {
            // Avoid leaking which doc exists.
            return NotFound();
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
    }
}
