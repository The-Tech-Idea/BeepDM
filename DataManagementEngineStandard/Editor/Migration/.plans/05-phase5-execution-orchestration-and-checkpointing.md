# Phase 5 - Execution Orchestration and Checkpointing

## Objective
Make migration execution resumable, traceable, and safe under partial failures.

## Scope
- Step orchestration and checkpoint persistence.
- Resume/retry behavior for multi-step migrations.

## File Targets
- `Migration/MigrationManager.cs`

## Planned Enhancements
- Execution steps with explicit sequence and dependencies.
- Checkpoint model:
  - last completed step
  - elapsed time
  - execution token/correlation id
- Retry policy:
  - transient failure retry
  - hard-fail categories
  - operator intervention hooks

## Acceptance Criteria
- Migration runs can resume from checkpoints.
- Partial failure states are clearly represented and recoverable.
- Retry behavior is deterministic and policy-driven.
