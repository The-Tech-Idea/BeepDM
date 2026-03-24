# Phase 5 - Filter, Sort, Search, and Pagination Performance

## Objective
Optimize query-like operations with predictable output and strong performance at scale.

## Audited Hotspots
- `ObservableBindingList.Filter.cs`: `ApplyFilter` overloads, `ParseFilter`, `ResetItems`.
- `ObservableBindingList.Sort.cs`: `ApplySort`, `ApplySort(string, ...)`, `RemoveSortCore`.
- `ObservableBindingList.Search.cs`: `AdvancedSearch` predicate composition.
- `ObservableBindingList.Pagination.cs`: `ApplyPaging`, working-set construction.
- `PropertyComparer.cs`: reflection-based compare path.

## File Targets
- `ObservableBindingList.Filter.cs`
- `ObservableBindingList.Sort.cs`
- `ObservableBindingList.Search.cs`
- `ObservableBindingList.Find.cs`
- `ObservableBindingList.Pagination.cs`
- `PropertyComparer.cs`

## Real Constraints to Address
- Private filter apply path can leak suppressed notification state on early return.
- Sort/filter/page composition is not consistently unified across all entrypoints.
- `AdvancedSearch` combined predicate construction risks recursive self-reference.

## Acceptance Criteria
- Operator composition (filter+sort+page) yields deterministic results.
- Performance remains within target for large lists.
- Caching/plan reuse strategy is bounded and invalidation-safe.
