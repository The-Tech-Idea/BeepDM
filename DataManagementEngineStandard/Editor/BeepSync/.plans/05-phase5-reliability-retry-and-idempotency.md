# Phase 5 - Reliability, Retry, and Idempotency

## Objective
Harden sync reliability with explicit retry categories, idempotency guarantees, and checkpoint-safe recovery.

## Scope
- Retry policy classification and limits.
- Idempotent write strategy and duplicate prevention.

## File Targets
- `BeepSync/BeepSyncManager.Orchestrator.cs`
- `BeepSync/Helpers/SyncSchemaTranslator.cs`
- `BeepSync/Helpers/SyncValidationHelper.cs`

## Planned Enhancements
- Retry classes:
  - transient transport/provider errors
  - validation failures (non-retry)
  - data conflicts (policy-driven)
- Idempotency controls:
  - sync key enforcement
  - dedupe key strategy
  - replay-safe write mode
- Checkpoint-safe resume model for partial sync runs.

## Acceptance Criteria
- Sync retries follow documented error categories.
- Re-run of same window does not duplicate destination data.
- Partial failures are resumable with preserved progress.
