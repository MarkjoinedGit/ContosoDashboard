namespace ContosoDashboard.Services;

public static class DocumentRules
{
    public const long MaxFileSizeBytes = 25L * 1024 * 1024; // 25 MB

    // Keep this training-friendly: validate by extension whitelist.
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".jpg",
        ".jpeg",
        ".png"
    };

    public static readonly HashSet<string> PreviewableMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    public static readonly IReadOnlyList<string> AllowedCategories = new[]
    {
        "Project Documents",
        "Team Resources",
        "Personal Files",
        "Reports",
        "Presentations",
        "Other"
    };
}
