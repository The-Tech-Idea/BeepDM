# Phase 1 - Contracts and Core Lifecycle Baseline

## Objective
Define explicit behavior contracts for list lifecycle, mutation, and event ordering.

## Audited Hotspots
- `ObservableBindingList.Constructors.cs`: `ClearAll`, `Dispose(bool)`, `ResetToDataSource`.
- `ObservableBindingList.ListChanges.cs`: `InsertItem`, `RemoveItem`, `SetItem`, `Item_PropertyChanged`, `OnListChanged`.
- `ObservableBindingList.CRUD.cs`: `AddRange`, `RemoveRange`, `RemoveAll`.
- `ObservableBindingListEventArgs.cs`: event payload consistency.

## File Targets
- `ObservableBindingList.cs`
- `ObservableBindingList.Constructors.cs`
- `ObservableBindingList.CRUD.cs`
- `ObservableBindingListEventArgs.cs`

## Real Constraints to Address
- Freeze checks are not consistently applied (`AddRange` bypass path).
- Event/state transitions are spread across files without explicit invariant table.
- Notification suppression behavior must be fail-safe under exceptions.

## Acceptance Criteria
- Public lifecycle invariants documented and test-covered.
- Event ordering is deterministic across add/update/remove/reset.
- Core APIs are lean and consistently validated.
