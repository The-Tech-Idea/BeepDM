# FormsManager — Performance and Paging

This document covers the performance subsystem: block-level paging, fetch-ahead, lazy load, caching, and cache statistics.

## Concepts

- **`PerformanceManager`** — the engine-side helper for caching and metrics.
- **`PagingManager`** — page state for blocks that have paging enabled.
- **`PageInfo`** — the result of `LoadPageAsync` (page number, record count, has next, has previous).
- **`CacheStats`** — the result of `GetCacheStats` (hits, misses, eviction count, current size).
- **`LazyLoadMode`** — enum: `Disabled`, `OnDemand`, `FetchAhead`.

## Block-level paging

```csharp
manager.SetBlockPageSize("ORDERS", pageSize: 50);
manager.SetTotalRecordCount("ORDERS", totalRecords: 1200);  // caller-owned

var page = await manager.LoadPageAsync("ORDERS", pageNumber: 3, ct: default);
// page.Records = the 50 records for page 3 (offset 100, count 50)
// page.PageNumber = 3
// page.TotalPages = 24
// page.HasNext = true, page.HasPrevious = true
```

**Paging is a per-block state** — `PagingManager` tracks `currentPage`, `pageSize`, `totalRecordCount`, and `currentOffset` for each block. The orchestrator's `LoadPageAsync` reads from this state and asks the UoW for the records in the offset range.

The **total record count is caller-owned**. The engine does NOT auto-count. Set it via `SetTotalRecordCount(name, count)` after a `Count(*)` query, or estimate it via a fast count query. Without a total count, paging still works (you can navigate next/previous), but you can't compute `TotalPages` or `HasNext` accurately.

## Fetch-ahead

```csharp
manager.SetFetchAheadDepth("ORDERS", depth: 2);
```

When the user navigates to page N, the engine pre-loads pages N+1 and N+2 in the background. The depth is the number of additional pages to pre-load. Set to 0 to disable.

The pre-load is **fire-and-forget** — the engine starts the load and returns immediately. The next page request will use the cached pre-loaded data if available, falling back to a fresh load.

## Lazy load

```csharp
manager.SetLazyLoadMode("ORDERS", LazyLoadMode.OnDemand);
manager.SetMaxRecordsPerFetch("ORDERS", max: 100);
```

`LazyLoadMode` is:

- `Disabled` — load all records when `ExecuteQueryAsync` runs.
- `OnDemand` — load records as the user navigates. The UoW may need to re-query on each navigation past the loaded set.
- `FetchAhead` — same as `OnDemand` but with pre-loading (combines with `FetchAheadDepth`).

`MaxRecordsPerFetch` is the upper bound on records per UoW load. Useful to prevent the engine from pulling millions of records in one query.

## Block cache

```csharp
manager.InvalidateBlockCache("ORDERS");
manager.SetBlockCacheTtl("ORDERS", TimeSpan.FromMinutes(10));

var stats = manager.GetCacheStats();
Console.WriteLine($"Hits: {stats.Hits}, Misses: {stats.Misses}, Size: {stats.SizeBytes / 1024} KB");
```

The cache is a per-block keyed store of `DataBlockInfo` (and any query result metadata). Default TTL is 5 minutes; default cache size limit is 256 MB.

### Cache invalidation

- **Manual** — `InvalidateBlockCache(name)`.
- **On commit** — after a successful commit, the cache for any block that had dirty state is invalidated.
- **On TTL expiry** — automatic, lazy (next access re-loads).
- **On memory pressure** — `CheckCacheMemoryPressure(thresholdMb)` evicts the least-recently-used blocks until the cache is below the threshold.

### Cache statistics

`GetCacheStats()` returns:

- `Hits` — total cache hits since startup.
- `Misses` — total cache misses.
- `Evictions` — total blocks evicted (manual or automatic).
- `SizeBytes` — current size of all cached blocks.
- `EntryCount` — number of blocks currently cached.

Useful for monitoring / dashboard UI.

## `RefreshBlockAsync`

```csharp
await manager.RefreshBlockAsync("ORDERS", fromDatasource: true, ct: default);
```

Re-loads the block from the datasource, replacing the current record set. The cache for the block is invalidated automatically. The current record index is preserved (clamped to the new count if the block got smaller).

`fromDatasource: true` re-runs the query with the current filter values. `fromDatasource: false` reloads from the UoW's in-memory store (useful after a manual cache invalidation that left the records in place).

## The `Configuration.Performance` DTO

```csharp
manager.Configuration.Performance.CacheTtl = TimeSpan.FromMinutes(15);
manager.Configuration.Performance.MaxCacheSizeBytes = 512 * 1024 * 1024; // 512 MB
manager.Configuration.Performance.FetchAheadDepth = 3;
```

The configuration is read at `InitializeManager` and applied when the manager is constructed. Changes after construction may or may not be picked up — for runtime changes, use the `Set*` methods.

## Performance flow (worked example: `ExecuteQueryAndEnterCrudModeAsync` with paging)

1. Run the query against the datasource.
2. Load the first page (offset 0, count `pageSize`).
3. If `FetchAheadDepth > 0`, pre-load the next N pages in the background.
4. Cache the loaded pages.
5. Set `currentPage = 1`.
6. Navigate to the first record.

When the user calls `LoadPageAsync(name, pageNumber)`:

1. Check the cache for the requested page.
2. If hit, return immediately.
3. If miss, run the query for the offset.
4. If `FetchAheadDepth > 0`, pre-load the next N pages.
5. Update the cache.
6. Return the page.

## Concurrency and the cache

The cache is a `ConcurrentDictionary` (or similar thread-safe structure). Multiple threads can read concurrently. Writes (cache updates, invalidations) are serialized by the dictionary's internal locking.

A block's UoW is **not** thread-safe — concurrent reads/writes to the same block from different threads are the UoW's responsibility. The engine does not synchronize at this level.

## The `LazyLoadMode` lifecycle

When `OnDemand` mode is set:

- `ExecuteQueryAndEnterCrudModeAsync` runs the query and loads the first page.
- On `NextRecordAsync` past the current page boundary, the engine runs another query for the next page.
- The UoW appends the new records to its `Units` collection.

The UoW's `TotalItemCount` is the **total loaded so far**, not the total in the datasource. The engine's `PagingManager` keeps the actual total (set by the caller via `SetTotalRecordCount`).

## Memory-pressure check

```csharp
manager.CheckCacheMemoryPressure(thresholdMb: 256);
```

Walks the cache, evicts least-recently-used blocks until the total cache size is below the threshold. Returns the number of blocks evicted (via the `CacheEfficiencyMetrics`).

This is **not** automatic — call it from your host's idle handler or a background timer.

## Notes for callers

- The performance subsystem is **opt-in**. If you don't call any of the `Set*` methods, the engine uses defaults (no paging, no fetch-ahead, no lazy load, default cache TTL).
- The cache is **per-block**, not per-form. The same block registered in two different `FormsManager` instances has two independent caches.
- `SetTotalRecordCount` should be called **after** `ExecuteQueryAndEnterCrudModeAsync` if you want accurate `TotalPages`. The engine doesn't auto-detect the total.
- The `LazyLoadMode.FetchAhead` value is a `LazyLoadMode` value (3), distinct from the `FetchAheadDepth` integer setting. The mode controls *whether* to fetch-ahead; the depth controls *how many pages*.
- The cache is **read-through** — the engine never caches a write. The first read after a write goes to the datasource, then the result is cached.

## See also

- [`architecture.md`](../architecture.md) — where `PerformanceManager` and `PagingManager` sit in the helper layer.
- [`mode-transitions.md`](mode-transitions.md) — `ExecuteQueryAndEnterCrudModeAsync` and the initial page load.
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 18 — the performance/paging mapping.
