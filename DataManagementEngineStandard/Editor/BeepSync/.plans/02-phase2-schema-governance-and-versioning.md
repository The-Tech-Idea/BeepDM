# Phase 2 - Schema Governance and Versioning

## Objective
Introduce enterprise schema governance for `DataSyncSchema` with versioning, approval, and compatibility checks.

## Scope
- Schema versioning and diffing.
- Approval and deprecation lifecycle for sync schemas.

## File Targets
- `BeepSync/Helpers/SchemaPersistenceHelper.cs`
- `BeepSync/Helpers/SyncValidationHelper.cs`
- `BeepSync/BeepSyncManager.Orchestrator.cs`

## Planned Enhancements
- Add schema version metadata and change reason.
- Add schema compatibility checker:
  - field mapping drift
  - key/watermark rule drift
  - direction policy drift
- Add schema status:
  - active
  - draft
  - deprecated
  - blocked

## Acceptance Criteria
- Schema changes are versioned and diffable.
- Incompatible schema changes are surfaced before execution.
- Deprecated schemas cannot run without explicit override.
