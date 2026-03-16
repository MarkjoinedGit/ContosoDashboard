# Research: Document Upload and Management

**Feature**: `001-document-upload-management`
**Date**: 2026-03-16

This document resolves technical unknowns and captures key design decisions needed to plan implementation.

## Key Decisions

### Decision: Local filesystem storage behind an abstraction
- **Chosen**: Introduce `IFileStorageService` with a local implementation (`LocalFileStorageService`) that stores files outside `wwwroot`.
- **Rationale**:
  - Matches Offline-First / Cloud-Ready constitution principle.
  - Prevents direct unauthenticated access to uploaded files.
  - Enables future swap to blob storage without changing business logic.
- **Alternatives considered**:
  - Store files in `wwwroot`: rejected (bypasses authorization, violates security-first/least privilege).
  - Store file bytes in DB: rejected (complexity/perf for training; harder to migrate).

### Decision: Authorized download/preview via server endpoint (not static file)
- **Chosen**: Add server endpoint to stream document content after service-level authorization.
- **Rationale**:
  - Files are outside `wwwroot`.
  - Must enforce access checks (IDOR defense).
- **Alternatives considered**:
  - Signed URLs: rejected for offline-first/no external infra.

### Decision: Upload sequence to prevent orphan records
- **Chosen**: Generate unique path  save file  then insert DB record.
- **Rationale**:
  - Stakeholder doc highlights avoiding duplicate key violations and orphaned records.
- **Alternatives considered**:
  - Insert DB first then save file: rejected (leaves orphan metadata if file write fails).

### Decision: MIME type and extension validation
- **Chosen**: Validate both:
  - File size  25 MB
  - Extension whitelist (PDF, Office, txt, jpg/jpeg, png)
  - Basic MIME type sanity check (store MIME type as metadata; treat it as informational, not trust boundary)
- **Rationale**:
  - Blazor/Browser-provided MIME type is not fully trustworthy.
  - Extension whitelist is simple/training-friendly.
- **Alternatives considered**:
  - Full content sniffing: rejected (complexity, depends on libraries).

### Decision: Virus scanning as an interface with training default
- **Chosen**: Add `IFileScanService` with `NoOpFileScanService` (always clean) for training.
- **Rationale**:
  - Requirement says files "must scan"; offline training needs an implementation without external service.
  - Keeps migration path open.
- **Alternatives considered**:
  - Integrate real AV engine: rejected (heavy dependencies and environment requirements).

### Decision: Authorization rules centralized in `DocumentService`
- **Chosen**: Mirror existing pattern (`TaskService`, `ProjectService`): UI calls service with `requestingUserId`; service enforces authorization for:
  - Owner
  - Project membership (for project-associated docs)
  - Project manager
  - Administrator
  - Explicit shares
- **Rationale**:
  - Aligns with constitution (service-level authorization).
  - Prevents page-level bypass.

## Resolved Clarifications / Assumptions

- **Where does current user id come from?**
  - The app uses cookie auth and stores numeric user id in `ClaimTypes.NameIdentifier`.
- **What is the storage root path?**
  - Use an app-local directory like `AppData/uploads` (configurable via `appsettings.json`).
- **Preview support (FR-014)**
  - Implement preview for PDF + images by streaming with correct `Content-Type` and using inline display in UI. Other file types are download-only.
- **Search performance goals**
  - For training scale (~500 docs/user), a single EF query with `LIKE` predicates + indexes is adequate.

## Performance / Query Notes

- Add indexes for:
  - `UploaderUserId, UploadedUtc` (My Documents)
  - `ProjectId, UploadedUtc` (Project documents)
  - `Category` (filter)
  - A basic composite index for search fields where feasible (title, uploader, project) while keeping training simplicity.

## Security Notes

- Never use user-provided filenames in paths.
- Store only a generated relative path/key in DB.
- Enforce all access via `DocumentService` before any file read.
- Log document activities without leaking sensitive content.
