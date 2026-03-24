# Phase 4 - Bidirectional Conflict Resolution

## Objective
Define deterministic, policy-driven conflict resolution for bidirectional synchronization.

## Scope
- Conflict detection and tie-break policies.
- Per-entity and per-field conflict strategies.

## File Targets
- `BeepSync/BeepSyncManager.Orchestrator.cs`
- `BeepSync/Helpers/SyncSchemaTranslator.cs`
- `BeepSync/Helpers/FieldMappingHelper.cs`

## Planned Enhancements
- Conflict policies:
  - source-wins
  - destination-wins
  - latest-timestamp-wins
  - custom resolver callback
- Conflict evidence capture:
  - before/after values
  - chosen resolution
  - reason code
- Optional quarantine path for unresolved conflicts.

## Acceptance Criteria
- Bidirectional conflicts resolve deterministically.
- Conflict policy is explicit in schema/plan metadata.
- Unresolvable conflicts are quarantined with actionable diagnostics.
