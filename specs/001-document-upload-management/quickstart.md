# Quickstart: Document Upload and Management (Training)

**Feature**: `001-document-upload-management`
**Date**: 2026-03-16

This quickstart describes how to run and manually verify the feature locally once implemented.

## Prerequisites

- .NET SDK (repo targets .NET 10)
- SQL Server LocalDB (default connection string)

## Local Storage Layout

- Documents are stored on disk outside `wwwroot`.
- Recommended storage root (configurable): `AppData/uploads`
- Stored key pattern:

- `{uploaderUserId}/{projectId-or-personal}/{guid}{extension}`

## Manual Verification Checklist

### Upload (FR-001..FR-005)

1. Login as an employee.
2. Navigate to `/documents`.
3. Upload a small supported file with Title and Category.
4. Confirm:
   - Success message
   - Document appears in list
   - Metadata includes upload date/time, size, type

### Validation (FR-002..FR-003)

- Try uploading an unsupported extension (expect clear rejection).
- Try uploading a >25MB file (expect clear rejection).

### My Documents (FR-006)

- Sort by upload date and category.
- Filter by category.
- Confirm the list contains only your own documents + items explicitly shared with you.

### Project Documents (FR-007)

1. Upload a document associated with a project where you are a member.
2. Open the project documents view.
3. Confirm other project members can see it.
4. Confirm a non-member cannot see that project’s documents (access denied or redirect).

### Download & Preview (FR-013..FR-014)

- Download a document and ensure it downloads successfully.
- Preview a PDF/image and ensure it renders inline.

### Audit (FR-019)

- Upload and download a document.
- Confirm `DocumentActivity` entries were recorded.

## Expected Security Properties

- Files are not directly accessible via static URLs.
- Server-side endpoints authorize access using service-layer checks.
- Paths are generated (GUID) and do not use user-provided filenames.
