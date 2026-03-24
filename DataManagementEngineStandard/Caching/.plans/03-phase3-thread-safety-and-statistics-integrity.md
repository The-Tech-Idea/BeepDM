# Phase 3 - Thread Safety and Statistics Integrity

## Objective
Ensure counters, memory tracking, and success aggregation remain correct under concurrency.

## Scope
- Fix count/memory drift paths.
- Remove shared mutable flags across parallel tasks.
- Validate concurrent operation invariants.

## File Targets
- `Caching/Providers/SimpleCacheProvider.cs`
- `Caching/Providers/MemoryCacheProvider.cs`
- `Caching/Providers/HybridCacheProvider.cs`

## Audited Hotspots
- `SimpleCacheProvider.SetAsync`, `ClearAsync`
- `MemoryCacheProvider.SetAsync`, `RemoveEntryAsync`, `EnsureSizeLimit`
- `HybridCacheProvider.SetAsync`, `RemoveAsync`, `RefreshAsync`

## Real Constraints to Address
- Item count increments can overcount on overwrite paths.
- Clear paths can reset memory usage without stable reconciliation.
- Shared `l1Success/l2Success/refreshed` booleans are mutated from parallel tasks.

## Acceptance Criteria
- Concurrent stress tests show stable item/memory counters.
- Hybrid operation outcomes are deterministic and race-safe.
