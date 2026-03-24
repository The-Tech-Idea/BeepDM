# Phase 7 - Master-Detail and Virtual Loading at Scale

## Objective
Improve consistency and scalability for hierarchical and virtualized list usage.

## Audited Hotspots
- `ObservableBindingList.MasterDetail.cs`: `RegisterDetail`, `SyncDetailLists`, `UnregisterDetail`.
- `ObservableBindingList.VirtualLoading.cs`: `SetDataProvider`, `GoToPageAsync`, cache trim/prefetch.

## File Targets
- `ObservableBindingList.MasterDetail.cs`
- `ObservableBindingList.VirtualLoading.cs`

## Real Constraints to Address
- Virtual page replacement currently risks event-handler accumulation when items are revisited.
- Detail sync must remain correct while master list is filtered/sorted/paged.
- Page cache eviction policy needs deterministic memory bounds.

## Acceptance Criteria
- Master/detail synchronization remains correct under paging/filtering.
- Virtual loading avoids over-fetch and memory spikes.
- High-volume scenarios pass latency and correctness gates.
