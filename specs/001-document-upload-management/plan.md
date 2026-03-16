# Implementation Plan: Document Upload and Management

**Branch**: `main` | **Date**: 2026-03-16 | **Spec**: `/specs/001-document-upload-management/spec.md`
**Input**: Feature specification from `/specs/001-document-upload-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Add secure document upload and management to ContosoDashboard so authenticated users can upload one or more supported files with required metadata (title + category), browse them in a "My Documents" view, see project-scoped documents as a project member, and download/preview documents they’re authorized to access.

The training implementation will store files on the local filesystem outside `wwwroot`, and all file operations (upload, download, preview, delete, share) will be orchestrated through a service layer that enforces authorization to prevent IDOR.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# / .NET 10 (repo targets `net10.0`)  
**Primary Dependencies**: ASP.NET Core (Blazor Server), EF Core (SQL Server provider), cookie auth (mock training auth)  
**Storage**: SQL Server LocalDB (EF Core) + local filesystem for uploaded content (outside `wwwroot`)  
**Testing**: No automated test project found yet (manual verification steps required; add tests if/when a test project exists)  
**Target Platform**: Windows dev workstation (LocalDB) / ASP.NET Core server  
**Project Type**: Web application (Blazor Server)  
**Performance Goals**: List/search views should return within ~2 seconds for typical user scope (~500 accessible docs)  
**Constraints**: Offline-first (no cloud services), least-privilege, service-level authorization, max 25MB/file, supported type whitelist  
**Scale/Scope**: Training dataset size; single app project; documents scoped by owner + project membership + explicit shares

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

✅ **Spec-first**: Spec exists at `/specs/001-document-upload-management/spec.md`.

⚠️ **Spec quality checklist**: `/specs/001-document-upload-management/checklists/requirements.md` is not yet checked off. Plan can proceed, but implementation should not start until the checklist is satisfied.

✅ **Testable requirements**: Requirements are expressed as FR-001..FR-020 with Given/When/Then acceptance scenarios.

✅ **Plan-before-code**: This plan describes the technical approach and will be followed by `/speckit.tasks`.

✅ **Security-first**:
- All pages/endpoints must require auth (`[Authorize]`) unless explicitly public.
- Authorization enforced in service layer for all document data + file access.
- Files stored outside `wwwroot`; all access via authorized endpoints.

✅ **Offline-first, cloud-ready**:
- Introduce `IFileStorageService` with a local implementation.
- Optional: `IFileScanService` abstraction with training no-op implementation.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── spec.md
├── plan.md              
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── endpoints.md
│   └── ui.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── (existing models...)
│   ├── Document.cs
│   ├── DocumentShare.cs
│   └── DocumentActivity.cs
├── Services/
│   ├── (existing services...)
│   ├── IDocumentService.cs / DocumentService.cs
│   ├── IFileStorageService.cs / LocalFileStorageService.cs
│   └── IFileScanService.cs / NoOpFileScanService.cs
├── Pages/
│   ├── Documents.razor
│   ├── ProjectDocuments.razor (or integrate into ProjectDetails.razor)
│   └── (optional) TaskDetails.razor integration
└── (optional) Controllers/
  └── DocumentsController.cs (download/preview endpoints)
```

**Structure Decision**: Single web app project (Blazor Server). Add models/services/pages inside existing `ContosoDashboard/` folders. Add a small server endpoint surface for secure streaming.

## Phase 0: Outline & Research (Output: `research.md`)

Completed: `/specs/001-document-upload-management/research.md`

Key outcomes:
- Files stored outside `wwwroot`.
- All file access via authorized endpoints.
- Upload sequence prevents orphaned metadata.
- Virus scanning satisfied via interface with training no-op implementation.

## Phase 1: Design & Contracts (Outputs: `data-model.md`, `contracts/*`, `quickstart.md`)

Completed artifacts:
- `/specs/001-document-upload-management/data-model.md`
- `/specs/001-document-upload-management/contracts/endpoints.md`
- `/specs/001-document-upload-management/contracts/ui.md`
- `/specs/001-document-upload-management/quickstart.md`

## Phase 2: Implementation Tasks (Output: `tasks.md`)

Next step: run `/speckit.tasks` to generate `specs/001-document-upload-management/tasks.md` broken into implementable work items.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
