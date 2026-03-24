# Phase 6 - Data Quality and Reconciliation

## Objective
Add enterprise-grade sync quality controls and reconciliation outputs.

## Scope
- DQ checks on synced rows.
- Post-sync reconciliation reports.

## File Targets
- `BeepSync/Helpers/SyncValidationHelper.cs`
- `BeepSync/Helpers/SyncProgressHelper.cs`
- `BeepSync/BeepSyncManager.Orchestrator.cs`

## Planned Enhancements
- DQ policy controls:
  - required fields
  - type validity
  - key integrity
  - referential checks (where available)
- Reconciliation report model:
  - source rows scanned
  - destination rows written/updated/skipped
  - rejects/quarantined rows
  - mismatch count
- Optional reject channel integration through import error store.

## Acceptance Criteria
- Every sync run produces reconciliation metrics.
- DQ failures are classified and traceable.
- Schema-level quality thresholds can fail a run when configured.
