# ContosoDashboard Spec Constitution

## Core Principles

### I. Spec-First Development

Every non-trivial change starts from a written specification.

- Features, enhancements, and significant fixes MUST begin with a **Spec Kit feature spec** (via `/speckit.specify`).
- Specs MUST describe **user value, scenarios, and success criteria** without prescribing implementation details (frameworks, libraries, or class names).
- No feature branch is considered "ready to plan" until its spec passes the specification quality checklist.

### II. Testable Requirements

Specifications and plans MUST be expressed as testable requirements.

- Each functional requirement MUST be unambiguous, independently testable, and traceable to at least one user story or success criterion.
- Acceptance scenarios MUST use clear **Given / When / Then** language.
- Ambiguities are recorded as `[NEEDS CLARIFICATION: ...]` in the spec and resolved via `/speckit.clarify` before implementation begins.

### III. Plan-Before-Code

Implementation work MUST be planned before code changes begin.

- For every approved spec, a **technical plan** is created via `/speckit.plan`.
- Plans MUST identify impacted areas (models, services, pages, configuration) and describe changes conceptually: APIs, data flow, security checks, and migration concerns.
- Plan items are broken into implementable tasks using `/speckit.tasks` and, when appropriate, mapped to GitHub issues via `/speckit.taskstoissues`.

### IV. Security-First and Least Privilege

Security is a first-class, non-negotiable requirement, even in a training project.

- New features MUST respect existing security architecture:
	- Mock authentication model and cookie settings
	- Role-based policies and `[Authorize]` usage
	- Service-level authorization checks to prevent IDOR
- Any change that touches authentication, authorization, or data access MUST:
	- Explicitly document security implications in the spec and plan.
	- Include tests or structured manual verification steps for access control.
- Data exposure is minimized: only required fields are surfaced to UI layers and APIs.

### V. Offline-First, Cloud-Ready Architecture

The training implementation is offline-first but designed for straightforward cloud migration.

- Infrastructure concerns (database, storage, identity providers) MUST be abstracted behind interfaces.
- New features SHOULD prefer interface-based services (for example, `IFileStorageService`) so local and cloud implementations can be swapped via dependency injection.
- Specs and plans MUST call out where a feature depends on local-only behavior versus future cloud-ready behavior.

## Additional Constraints and Standards

### Technology and Architecture

- Runtime: .NET 10, ASP.NET Core with Blazor Server.
- Data access MUST use `ApplicationDbContext` and EF Core patterns; no ad-hoc SQL concatenation.
- All new services MUST be registered via dependency injection and follow existing patterns in `Services/`.
- Cross-cutting concerns (logging, security headers, authentication, authorization) MUST continue to be configured centrally in `Program.cs` unless the plan explicitly justifies a change.

### Security Requirements

- New endpoints and pages MUST:
	- Declare appropriate `[Authorize]` attributes or be explicitly documented as public.
	- Enforce authorization at the service layer, not only at the UI/page level.
- Sensitive data (user identifiers beyond what is necessary, project membership, notification contents) MUST NOT be logged beyond what is required for debugging in this training environment.
- For features involving file I/O (for example, document upload), specs and plans MUST address:
	- Validation of file type and size
	- Storage location and path safety
	- Authorization checks for download and access

### Performance and UX Expectations

- Specs SHOULD include performance expectations where relevant (for example, list load times, search latency, upload limits).
- Plans MUST note obvious performance risks (such as N+1 queries, unbounded result sets) and, where appropriate, propose incremental mitigations.
- UX changes SHOULD keep interactions simple and predictable (few clicks for primary flows, clear error messages, and visible loading states for long operations).

## Development Workflow and Quality Gates

### Specification Workflow

1. Capture stakeholder intent or user need (for example, from `StakeholderDocs/`).
2. Run `/speckit.specify` with the feature description to generate or update the spec.
3. Ensure the spec:
	 - Fills all mandatory sections (user scenarios, requirements, success criteria).
	 - Uses testable, non-implementation-centric language.
	 - Has no more than three `[NEEDS CLARIFICATION]` markers.
4. Resolve open questions via `/speckit.clarify`, updating the spec until requirements are clear and testable.

### Planning and Tasks

1. Once the spec is accepted, generate a **technical plan** via `/speckit.plan`.
2. Use `/speckit.tasks` to break the plan into implementable tasks.
3. Map tasks to GitHub issues via `/speckit.taskstoissues` where appropriate.
4. Each PR MUST:
	 - Reference the relevant spec and plan.
	 - Identify which tasks/issues it addresses.

### Implementation and Review

- Implementation follows the plan; deviations MUST be documented in the plan and/or spec and explained in the PR.
- Reviewers SHOULD check:
	- Alignment with the spec, plan, and this constitution.
	- Correctness and completeness of security and authorization logic.
	- That new behaviors are testable and clearly described (either via automated tests or documented manual steps for this training repo).

## Governance

- This constitution applies to all work in the **ContosoDashboard** repository that uses GitHub Spec Kit.
- In case of conflict between ad-hoc practices and this constitution, **this constitution takes precedence**.
- Changes to the constitution MUST:
	- Be proposed via a dedicated spec (for example, "Constitution Update").
	- Explain motivation, expected impact, and any migration or communication plan.
	- Be reviewed and approved through the normal PR process before merging.
- Reviewers are expected to:
	- Flag deviations from these principles during code and spec reviews.
	- Request updates to specs, plans, or tasks rather than accepting unclear or under-specified changes.

**Version**: 1.0.0 | **Ratified**: 2026-03-16 | **Last Amended**: 2026-03-16
