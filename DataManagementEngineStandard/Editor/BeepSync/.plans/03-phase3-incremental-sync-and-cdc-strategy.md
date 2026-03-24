# Phase 3 - Incremental Sync and CDC Strategy

## Objective
Formalize incremental synchronization with watermark/CDC policies and replay-safe execution.

## Scope
- Watermark semantics and replay windows.
- CDC-compatible source/destination alignment rules.

## File Targets
- `BeepSync/Helpers/SyncSchemaTranslator.cs`
- `BeepSync/Helpers/SyncValidationHelper.cs`
- `BeepSync/BeepSyncManager.Orchestrator.cs`

## Planned Enhancements
- Watermark modes:
  - timestamp watermark
  - numeric sequence watermark
  - composite key window
- Replay policy:
  - overlap window
  - dedupe strategy
  - last-success checkpoint
- CDC constraints:
  - source ordering assumptions
  - late-arrival handling
  - delete/tombstone strategy

## Acceptance Criteria
- Incremental policy is explicit per schema.
- Replay runs are idempotent within configured windows.
- CDC drift warnings are emitted before sync apply.
