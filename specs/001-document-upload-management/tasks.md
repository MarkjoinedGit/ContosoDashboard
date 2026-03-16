# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: `spec.md` (required), `plan.md` (required), `research.md`, `data-model.md`, `contracts/`

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1..US5)
- Tasks include exact file paths for this repository (`ContosoDashboard/...`)

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Update plan file location so feature docs live together
  - **Files**: (optional) copy `specs/main/plan.md` -> `specs/001-document-upload-management/plan.md`
  - **Why**: Keeps spec/plan/tasks co-located for review.

- [x] T002 [P] Add configuration for document storage root
  - **Files**: `ContosoDashboard/appsettings.json`, `ContosoDashboard/appsettings.Development.json` (if needed)
  - **Output**: A config value like `DocumentStorage:RootPath` defaulting to `AppData/uploads`
  - **Maps to**: offline-first storage requirements

- [x] T003 [P] Add a small constants/utility location for document rules
  - **Files**: `ContosoDashboard/Services/DocumentRules.cs` (new)
  - **Rules**: allowed extensions, max bytes (25MB), previewable MIME types
  - **Maps to**: FR-002, FR-003, FR-014

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: No user story UI work should begin until these are complete.

- [x] T004 [P] Add new EF Core models for documents
  - **Files**:
    - `ContosoDashboard/Models/Document.cs`
    - `ContosoDashboard/Models/DocumentShare.cs`
    - `ContosoDashboard/Models/DocumentActivity.cs`
  - **Include**: DataAnnotations for required fields + max lengths (from `data-model.md`)
  - **Maps to**: Key entities section, FR-005, FR-015, FR-019

- [x] T005 Update `ApplicationDbContext` to include document entities
  - **Files**: `ContosoDashboard/Data/ApplicationDbContext.cs`
  - **Add**: `DbSet<Document> Documents`, `DbSet<DocumentShare> DocumentShares`, `DbSet<DocumentActivity> DocumentActivities`
  - **Add**: relationships and indexes described in `data-model.md`
  - **Maps to**: FR-006, FR-007, FR-008, FR-019

- [x] T006 [P] Implement local file storage abstraction
  - **Files**:
    - `ContosoDashboard/Services/IFileStorageService.cs`
    - `ContosoDashboard/Services/LocalFileStorageService.cs`
  - **Behavior**:
    - accept `Stream` and generated relative key
    - write to disk under configured root
    - read stream for download/preview
    - delete file
  - **Security**: ensure paths are combined safely; never accept user-supplied paths
  - **Maps to**: FR-001, FR-013, security constraints

- [x] T007 [P] Implement file scanning abstraction (training default)
  - **Files**:
    - `ContosoDashboard/Services/IFileScanService.cs`
    - `ContosoDashboard/Services/NoOpFileScanService.cs`
  - **Behavior**: always returns “clean” for offline training
  - **Maps to**: stakeholder scanning requirement

- [x] T008 Implement `DocumentService` (core business + authorization)
  - **Files**:
    - `ContosoDashboard/Services/IDocumentService.cs`
    - `ContosoDashboard/Services/DocumentService.cs`
  - **Depends on**: T004, T005, T006, T007
  - **Must include**:
    - upload orchestration (generate key → save file → save DB)
    - service-level authorization checks (owner/member/PM/admin/shared)
    - create `DocumentActivity` entries for upload/download/preview/delete/share/metadata update/replace
  - **Maps to**: FR-001..FR-005, FR-018, FR-019

- [x] T009 Register new services in DI
  - **Files**: `ContosoDashboard/Program.cs`
  - **Register**: `IDocumentService`, `IFileStorageService`, `IFileScanService`

**Checkpoint**: Foundation ready — document entities and service layer exist; file operations are possible via service.

---

## Phase 3: User Story 1 — Upload and Organize Documents (Priority: P1) 🎯

**Goal**: Upload supported files with required metadata and see them in “My Documents”.

**Independent Test**:
- Login as an employee
- Upload a supported file with Title+Category
- Confirm it appears in `/documents` list
- Confirm oversized/unsupported files are rejected with clear messages

### Implementation

- [x] T010 [US1] Create Documents page (My Documents + Upload)
  - **Files**: `ContosoDashboard/Pages/Documents.razor` (new)
  - **UI**:
    - list table with sorting + filtering
    - upload dialog/section using `InputFile` (multiple)
    - status/progress indicators and success/error messages
  - **Maps to**: FR-001, FR-004, FR-006

- [x] T011 [US1] Implement service methods for “My Documents” view
  - **Files**: `ContosoDashboard/Services/DocumentService.cs`
  - **Add methods**:
    - get documents uploaded by user
    - get documents shared with user
    - apply sort/filter (server-side)
  - **Perf**: page results capped/sane ordering; use indexes
  - **Maps to**: FR-006, FR-015, SC-002

- [x] T012 [US1] Add validation and user-friendly errors
  - **Files**: `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Pages/Documents.razor`
  - **Include**:
    - 25MB enforcement
    - extension whitelist enforcement
    - required title/category
  - **Maps to**: FR-002, FR-003, FR-004

**Checkpoint**: US1 complete and fully usable (upload + browse personal docs).

---

## Phase 4: User Story 2 — Project Document Collaboration (Priority: P1)

**Goal**: Project members can view project documents; PM can upload to project; non-members cannot access.

**Independent Test**:
- Upload project-associated doc
- View it as another project member
- Attempt view as non-member and confirm denial/redirect

### Implementation

- [ ] T020 [US2] Add project document queries + authorization rules
  - **Files**: `ContosoDashboard/Services/DocumentService.cs`
  - **Behavior**:
    - ensure project membership/PM/admin on list and access
  - **Maps to**: FR-007, FR-018

- [ ] T021 [US2] Add project documents UI
  - **Files**: either
    - `ContosoDashboard/Pages/ProjectDocuments.razor` (new route), OR
    - extend `ContosoDashboard/Pages/ProjectDetails.razor` with a “Documents” section
  - **Maps to**: FR-007

- [ ] T022 [US2] Notify project members when a new project document is added
  - **Files**: `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Services/NotificationService.cs` (if new notification type needed)
  - **Maps to**: FR-016

**Checkpoint**: US2 done — project document collaboration works and is access-controlled.

---

## Phase 5: User Story 3 — Search and Find Documents Quickly (Priority: P2)

**Goal**: Search across user’s accessible documents (owned + project + shared) with filters.

**Independent Test**:
- Upload a few docs w/ metadata (title/tags)
- Search keyword and confirm results are scoped to authorized docs only

### Implementation

- [ ] T030 [US3] Implement document search API in service layer
  - **Files**: `ContosoDashboard/Services/DocumentService.cs`
  - **Search fields**: title, description, tags, uploader, project
  - **Scope**: only documents user can access
  - **Maps to**: FR-008, FR-018, SC-003

- [ ] T031 [US3] Add search UI to `/documents`
  - **Files**: `ContosoDashboard/Pages/Documents.razor`
  - **UX**: keyword box + filters for category/project/date range

**Checkpoint**: US3 complete — search works within scope and is performant for training volume.

---

## Phase 6: User Story 4 — Manage and Share Documents (Priority: P2)

**Goal**: Owners/PM can edit metadata, replace file, delete with confirmation, share with users; recipients see “Shared with me” + get notification.

**Independent Test**:
- Upload then edit metadata
- Replace file, download verifies new version
- Delete confirms removal
- Share with another user: verify notification + appears in shared view

### Implementation

- [ ] T040 [US4] Implement update metadata + replace file
  - **Files**: `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Pages/Documents.razor`
  - **Maps to**: FR-009, FR-010

- [ ] T041 [US4] Implement delete workflow (confirmation + file removal)
  - **Files**: `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Pages/Documents.razor`
  - **Audit**: Create `DocumentActivity` for delete
  - **Maps to**: FR-011, FR-012, FR-019

- [ ] T042 [US4] Implement sharing model + notifications
  - **Files**:
    - `ContosoDashboard/Services/DocumentService.cs`
    - `ContosoDashboard/Services/NotificationService.cs` (add a new notification type if desired)
  - **Data**: create `DocumentShare` records and enforce uniqueness
  - **Maps to**: FR-015, FR-016

- [ ] T043 [US4] Add “Shared with Me” section in UI
  - **Files**: `ContosoDashboard/Pages/Documents.razor`

**Checkpoint**: US4 complete — manage/share flows work and generate notifications/activity.

---

## Phase 7: User Story 5 — Integrate Documents into Existing Dashboard Features (Priority: P3)

**Goal**: Surface documents in tasks/projects/dashboard.

**Independent Test**:
- Associate docs to tasks/projects
- See recent documents widget on dashboard

### Implementation

- [ ] T050 [US5] Add task association support
  - **Files**:
    - `ContosoDashboard/Services/DocumentService.cs`
    - (optional) new/updated task detail page component
  - **Behavior**: Upload from task context auto-associates to task’s project
  - **Maps to**: FR-017

- [ ] T051 [US5] Add Recent Documents widget to dashboard
  - **Files**: `ContosoDashboard/Pages/Index.razor`, `ContosoDashboard/Services/DashboardService.cs`
  - **Maps to**: FR-017

- [ ] T052 [US5] Add document count to dashboard summary
  - **Files**: `ContosoDashboard/Services/DashboardService.cs`, `ContosoDashboard/Pages/Index.razor`

---

## Phase 8: Document Download/Preview Endpoints (Cross-cutting, required for FR-013/FR-014)

These can be implemented as soon as `DocumentService` can authorize + provide a stream.

- [x] T060 Add authorized endpoints for download + preview
  - **Files**: `ContosoDashboard/Controllers/DocumentsController.cs` (new) OR Razor Page handlers
  - **Routes**: per `/specs/001-document-upload-management/contracts/endpoints.md`
  - **Maps to**: FR-013, FR-014, FR-018

- [x] T061 Wire UI actions (Download/Preview) to endpoints
  - **Files**: `ContosoDashboard/Pages/Documents.razor`, project docs UI

---

## Phase 9: Audit, Reporting, and Admin Summaries

- [ ] T070 [P] Ensure all document actions create `DocumentActivity`
  - **Files**: `ContosoDashboard/Services/DocumentService.cs`
  - **Maps to**: FR-019

- [ ] T071 [US4/US5] Add basic admin summary view/service method
  - **Files**: `ContosoDashboard/Services/DocumentService.cs` (aggregation), plus a page under `Pages/` guarded by Administrator policy
  - **Maps to**: FR-020

---

## Dependencies & Execution Order

- Phase 1 can start immediately.
- Phase 2 blocks all user story UI work.
- US1 and US2 are P1 and should be implemented first after Phase 2.
- Search (US3) and Manage/Share (US4) depend on document foundation & are P2.
- Integrations (US5) are P3.
- Endpoints (Phase 8) are required for download/preview (FR-013/FR-014) and can be done after DocumentService supports stream access.

---

## Requirement Coverage (quick map)

- FR-001..FR-005: T008, T010, T012
- FR-006: T010, T011
- FR-007: T020, T021
- FR-008: T030, T031
- FR-009..FR-012: T040, T041
- FR-013..FR-014: T060, T061
- FR-015..FR-016: T042, T043, T022
- FR-017: T050..T052
- FR-018: T008, T020, T030, T060
- FR-019: T008, T070
- FR-020: T071
