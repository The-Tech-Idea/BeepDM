# Phase 2 - Change Tracking and Diff Correctness

## Objective
Guarantee deterministic and auditable tracking of insert/update/delete operations.

## Audited Hotspots
- `ObservableBindingList.Tracking.cs`: `GetTrackingItem`, `GetPendingChanges`, `AcceptChanges`, `RejectChanges`, `CommitAllAsync`.
- `ObservableBindingList.ListChanges.cs`: delete and property-change flows that set tracking state.
- `Tracking.cs` / `TrackedEntity.cs`: state containers.

## File Targets
- `ObservableBindingList.Tracking.cs`
- `ObservableBindingList.ListChanges.cs`
- `ObservableChanges.cs`
- `Tracking.cs`
- `TrackedEntity.cs`

## Real Constraints to Address
- Deleted-item lookup depends on `originalList.IndexOf(item)` after removal, which can make deleted tracking unreachable.
- State transitions (`Unchanged`/`Modified`/`Deleted`) are not centrally validated.
- Commit events currently observe post-mutation state in some paths.

## Acceptance Criteria
- Same sequence of edits yields identical change-set output.
- Track state transitions are explicit and conflict-safe.
- Diff/merge tests cover edge cases and rollback paths.
