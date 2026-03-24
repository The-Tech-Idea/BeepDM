# Phase 1 - Contracts and Sync Plan Foundation

## Objective
Establish a plan-first contract for sync operations while keeping existing schema-driven execution backward-compatible.

## Scope
- Sync plan artifact model.
- Execution lifecycle states and non-breaking API integration.

## File Targets
- `BeepSync/BeepSyncManager.Orchestrator.cs`
- `BeepSync/Interfaces/ISyncHelpers.cs`

## Planned Enhancements
- Add sync plan metadata:
  - schema version
  - plan id/hash
  - owner and approval status
  - environment target
- Define lifecycle:
  - draft
  - validated
  - approved
  - executed
  - verified
- Keep current `SyncDataAsync` path operational in compatibility mode.

## Implementation Rules (Skill Constraints)
- Keep orchestration in `IDMEEditor` boundary (`beepdm`).
- Delegate data movement to `DataImportManager` via translator (`beepsync` architecture rule).
- Persist plan/schema artifacts through helper-based persistence; avoid ad-hoc storage logic.

## Acceptance Criteria
- Plans can be generated/validated without executing sync.
- Existing schema-only runs continue to function.
- Plan metadata is persisted and queryable.
