# Contracts: UI Behaviors (Blazor)

**Feature**: `001-document-upload-management`
**Date**: 2026-03-16

## My Documents Page

- **Route**: `/documents`
- **Auth**: Required
- **Data shown (table)**:
  - Title
  - Category
  - Uploaded date
  - File size
  - Associated project (optional)
  - Actions: Preview (if supported), Download, Edit metadata, Delete
- **Sort**: title, upload date, category, file size
- **Filter**: category, project, date range
- Must include "Shared with me" toggle/section.

## Upload Flow

- Users can select one or more files (InputFile multiple).
- For each file user must provide:
  - Title (required)
  - Category (required)
  - Description (optional)
  - Project (optional)
  - Tags (optional)
- Validation:
  - Reject >25MB with clear message
  - Reject unsupported extensions with clear message
- UX:
  - Show progress indicator while uploading (at least "uploading" state and per-file status)
  - Show success/error message per file

## Project Documents View

- **Route**: `/projects/{projectId}/documents` (or embedded section within existing `/projects/{projectId}`)
- **Auth**: Required
- Must only show documents the viewer is authorized to see (project members + PM + admins).

## Task Integration (later/P3)

- In task detail view, show documents for that task.
- Upload from task detail associates document to task and its project (if any).
