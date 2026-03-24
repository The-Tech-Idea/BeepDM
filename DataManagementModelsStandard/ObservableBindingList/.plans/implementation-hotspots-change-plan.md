# ObservableBindingList Implementation Hotspots Change Plan

This document lists the **exact planned code changes** for the audited hotspots that replaced generic phase text.

## 1) `ObservableBindingList.Filter.cs` -> `ApplyFilter()` state leak

### Current risk
- Private `ApplyFilter()` can return early when filter parsing fails and may not restore notification flags (`SuppressNotification`, `RaiseListChangedEvents`) in all branches.

### Exact change
- Wrap suppression toggles in `try/finally` so restoration always occurs.
- Centralize state restoration in one helper method (e.g., `RestoreNotificationState(...)`).
- Ensure `OnListChanged(Reset)` is fired only when flags are restored and working set is consistent.

## 2) `ObservableBindingList.Search.cs` -> `AdvancedSearch(...)` predicate composition

### Current risk
- Combined predicate composition is self-referential and can recurse incorrectly.

### Exact change
- Replace recursive closure composition with iterative combination:
  - keep local `left` predicate snapshot before composing next predicate.
  - set `combined = item => left(item) && next(item)` (or `||`) without referencing the evolving variable directly.
- Add null-safe guard for empty criteria list and unsupported operators.

## 3) `ObservableBindingList.Tracking.cs` -> `GetTrackingItem(...)` deleted lookup mismatch

### Current risk
- Deleted items can become unreachable because lookup path depends on `originalList.IndexOf(item)` after item removal.

### Exact change
- Introduce stable key-based tracking lookup:
  - prefer entity identity key (if available) or reference-based dictionary key.
  - fallback to snapshot key captured at track-registration time.
- Decouple deleted lookup from `originalList` index position.
- Update `GetPendingChanges()` and delete paths to use the same key strategy.

## 4) `ObservableBindingList.CRUD.cs` -> `AddRange(...)` freeze guard bypass

### Current risk
- `AddRange` path uses `base.InsertItem(...)` directly and can bypass freeze invariants.

### Exact change
- Enforce `ThrowIfFrozen()` at batch start and before each insert.
- Route inserts through shared guarded mutation helper instead of direct base call when frozen check is required.
- Keep event suppression behavior but always restore it in `finally`.

## 5) `ObservableBindingList.VirtualLoading.cs` -> `GoToPageAsync(...)` handler lifecycle

### Current risk
- Replacing page items can lead to repeated `PropertyChanged` subscriptions on reused/cached objects.

### Exact change
- Track page-bound subscriptions explicitly:
  - unhook previous page item handlers before clear/reset.
  - hook only once per item reference.
- Add helper methods:
  - `AttachPageItemHandlers(...)`
  - `DetachPageItemHandlers(...)`
- Ensure detach runs in both success and error/cancel paths.

## 6) `ObservableBindingList.ThreadSafety.cs` + mutator partials

### Current risk
- Lock wrappers exist, but core mutator/query paths rely on caller discipline and are not consistently internalized.

### Exact change
- Define lock policy by operation class:
  - write lock: insert/remove/set/reset/filter/sort/page materialization.
  - read lock: passive queries/lookup operations.
- Wrap internal hotspot methods with lock helpers (not just public wrappers).
- Add comments documenting lock boundaries to prevent deadlock-prone nested locking.

## 7) `ObservableBindingList.Sort.cs` + `Filter.cs` + `Pagination.cs` composition consistency

### Current risk
- Some sort paths use `originalList` directly and do not consistently compose with active filter/page state.

### Exact change
- Normalize transformation pipeline order:
  1. source (`originalList`)
  2. filter
  3. sort
  4. page
- Introduce one internal reapply pipeline method used by all sort/filter/page entry points.
- Remove divergent reset logic from individual methods where pipeline method should apply.

## 8) `ObservableBindingList.Tracking.cs` commit event semantics

### Current risk
- `AfterSave` can be raised after state mutation to `Unchanged`, hiding pre-commit state.

### Exact change
- Capture pre-commit snapshot state before persistence call.
- Raise `BeforeSave` and `AfterSave` with explicit state payload (before/after).
- Ensure event args expose both state values for consumers.

## 9) `ObservableBindingList.Computed.cs` + `Validation.cs` cache invalidation

### Current risk
- Computed/validation caches can retain stale data after some batch/remove/reset paths.

### Exact change
- Add centralized invalidation hooks called from all mutation paths:
  - item-level invalidation on property changes.
  - list-level invalidation on bulk operations and reset.
- Ensure invalidation runs even when notifications are suppressed.

## 10) `ObservableBindingList.Export.cs` contract stability

### Current risk
- Complex values are stringified without explicit schema/version contract.

### Exact change
- Introduce export metadata contract header:
  - `SchemaVersion`
  - `ExportedAtUtc`
  - deterministic column ordering map.
- Add configurable complex-type serialization policy (string/json/skip).

## Execution Note
- Changes will be implemented in lean partial-class style, preserving existing file boundaries and adding helper methods only where needed to reduce method complexity and repeated state-handling logic.
