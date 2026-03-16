# Contracts: Document Access Endpoints

**Feature**: `001-document-upload-management`
**Date**: 2026-03-16

This project is a Blazor Server app; however, secure document serving requires explicit server endpoints because files must be stored outside `wwwroot`.

## Download Document

- **Route**: `GET /documents/{documentId:int}/download`
- **Auth**: Required (`[Authorize]`)
- **Behavior**:
  - Server calls `DocumentService.AuthorizeAccess(documentId, requestingUserId)` and returns 404/redirect if unauthorized.
  - Streams file content with:
    - `Content-Type`: stored `FileType`
    - `Content-Disposition`: `attachment; filename="{safe file name}"`
  - Logs `DocumentActivity: Download`.

## Preview Document (PDF/images)

- **Route**: `GET /documents/{documentId:int}/preview`
- **Auth**: Required (`[Authorize]`)
- **Behavior**:
  - Same authorization as download.
  - Only allowed if file type is previewable (PDF, JPEG, PNG).
  - Streams file content with inline disposition.
  - Logs `DocumentActivity: Preview`.

## Notes

- Endpoints may be implemented as MVC controller actions or Razor Page handlers.
- They must never map a physical path from user input.
- Only use `StorageKey` from DB after authorization.
