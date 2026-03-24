# 00 - Overview: ObservableBindingList Gap Matrix

## Objective
Baseline current `ObservableBindingList` capabilities and define phased enhancements for enterprise reliability and performance.

## Gap Matrix

| Capability | Current State | Gap | Target |
|---|---|---|---|
| Core lifecycle contracts | Strong partial decomposition in constructors/list-change/CRUD files | Invariants are implicit across files | Explicit contract and lifecycle invariants |
| Change tracking/diff | `Tracking.cs` + `ObservableBindingList.Tracking.cs` are feature-rich | Deleted tracking path can become inconsistent (`GetTrackingItem` vs `originalList`) | Deterministic tracking and delete semantics |
| Thread safety | Lock wrappers exist in `ObservableBindingList.ThreadSafety.cs` | Core mutators are not consistently lock-scoped | Internalized concurrency model with guarantees |
| Validation/computed | Validation/computed/aggregate partials exist | Dependency recompute and cache invalidation not uniformly bounded | Deterministic recompute and bounded caches |
| Query-like features | Filter/sort/search/paging implemented | Known correctness gaps (`Filter.ApplyFilter` suppress-state leak; `Search.AdvancedSearch` recursive predicate issue) | Correct and composable query pipeline |
| Undo/redo/nav | Undo manager + bookmarks/movement implemented | History bounds and state replay consistency need explicit guarantees | Reliable bounded history and navigation |
| Virtual/master-detail | Virtual paging + detail sync implemented | Handler lifecycle and sync stability at scale need hardening | Stable high-volume virtual/detail behavior |
| Export/serialization | DataTable export works | Complex-value serialization/versioning not contract-driven | Versioned, deterministic interchange contracts |
| Observability | Change logging exists | Operational diagnostics/KPI model not formalized | Structured telemetry and rollout KPI gates |

## Concrete Constraints Observed
- `Filter.ApplyFilter()` private path can return early without restoring notification flags.
- `Search.AdvancedSearch()` composes predicate with self-reference risk.
- `AddRange()` path uses `base.InsertItem(...)` and bypasses freeze checks.
- `GoToPageAsync()` page replacement can accumulate property subscriptions on reused items.
