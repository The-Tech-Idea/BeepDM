# Phase 4 - Expiration, Eviction, and Memory Accounting

## Objective
Make expiration/eviction behavior and memory accounting accurate and predictable.

## Scope
- Eviction correctness under capacity pressure.
- Expiration cleanup correctness and cost control.
- Memory usage accounting reconciliation.

## File Targets
- `Caching/Providers/SimpleCacheProvider.cs`
- `Caching/Providers/InMemoryCacheProvider.cs`
- `Caching/Providers/MemoryCacheProvider.cs`

## Audited Hotspots
- `SimpleCacheProvider.EvictOldestItemsAsync`, `CleanupExpiredItemsAsync`
- `InMemoryCacheProvider` compression hooks (`CompressAsync`, `DecompressAsync`, `IsCompressed`)
- `MemoryCacheProvider.EvictLeastRecentlyUsedAsync`

## Real Constraints to Address
- Compression support is configured but effectively placeholder in `InMemoryCacheProvider`.
- Cleanup and eviction can run while counters are updated by other paths.
- Memory usage can drift if replacement and clear operations are not reconciled.

## Acceptance Criteria
- Expiration/eviction behavior matches documented policy.
- Memory/counter metrics remain within strict reconciliation thresholds.
