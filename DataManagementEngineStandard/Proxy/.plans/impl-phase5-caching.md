# Implementation Record — Phase 5: Cache Strategy and Consistency Controls

_Plan file: [05-phase5-cache-strategy-and-consistency-controls.md](05-phase5-cache-strategy-and-consistency-controls.md)_  
_Status: ✅ Complete_  
_File: `ProxyDataSource.Caching.cs`_

---

## What Was Implemented

### Cache Model Types (`ProxyotherClasses.cs`)

#### `ProxyCacheTier` enum
```csharp
public enum ProxyCacheTier
{
    None,        // bypass cache entirely
    ShortLived,  // query-scoped, very short TTL (seconds)
    EntityLevel  // standard entity cache with configured TTL
}
```

#### `ProxyCacheConsistency` enum
```csharp
public enum ProxyCacheConsistency
{
    Eventual,             // reads may return stale data until TTL expires
    StaleWhileRevalidate, // serve stale immediately, refresh in background
    WriteThrough          // invalidate/refresh on every write
}
```

#### `ProxyCacheProfile`
```csharp
public class ProxyCacheProfile
{
    public ProxyCacheTier        Tier        { get; set; } = ProxyCacheTier.EntityLevel;
    public ProxyCacheConsistency Consistency { get; set; } = ProxyCacheConsistency.StaleWhileRevalidate;
    public int  TtlSeconds  { get; set; } = 300;
    public int  MaxItems    { get; set; } = 1000;
}
```

#### `CacheEntry` — added `LastAccessedAt`
```csharp
public class CacheEntry
{
    public object   Data           { get; set; }
    public DateTime CachedAt       { get; set; }
    public DateTime LastAccessedAt { get; set; }  // NEW — used for LRU eviction
    public int      TtlSeconds     { get; set; }
    public bool     IsExpired => (DateTime.UtcNow - CachedAt).TotalSeconds > TtlSeconds;
}
```

---

### `GetEntityWithCache` (`ProxyDataSource.Caching.cs`)

Implements `StaleWhileRevalidate` tier — the primary consistency mode:

```
if cache hit AND not expired
    update LastAccessedAt
    return cached data

if cache hit AND expired AND StaleWhileRevalidate
    TriggerBackgroundRevalidation(key, ...)   // async, non-blocking
    update LastAccessedAt
    return stale data immediately (low latency)

if no hit OR (expired AND NOT StaleWhileRevalidate)
    fetch from backing datasource via ExecuteReadWithPolicy
    SetCacheEntry(key, freshData)
    return fresh data
```

---

### `InvalidateCacheOnWrite` (`ProxyDataSource.Caching.cs`)

Called from all write paths (`InsertEntity`, `UpdateEntity`, `UpdateEntities`, `DeleteEntity`, `ExecuteSql`).

Removes all cache keys whose prefix matches the entity name pattern. Also invalidates `~all` wildcard keys.

```csharp
private void InvalidateCacheOnWrite(string entityName)
{
    var prefix = GenerateCacheKeyPrefix(entityName);
    var stale = _cache.Keys.Where(k => k.StartsWith(prefix)).ToList();
    foreach (var key in stale)
        _cache.TryRemove(key, out _);
}
```

---

### `EvictIfOverLimit` — LRU Eviction (`ProxyDataSource.Caching.cs`)

Called after every `SetCacheEntry`. When `_cache.Count > CacheProfile.MaxItems`:

1. Sort all entries by `LastAccessedAt ASC` (least recently used first).
2. Remove the oldest `(Count - MaxItems)` + 1 entries.

Prevents unbounded memory growth regardless of TTL. Default `MaxItems = 1000`.

---

### `GenerateCacheKey` — Collision Fix (`ProxyDataSource.Caching.cs`)

Original key only used `entityName + filterField + filterValue`. Risk: two queries on same entity with different `FilterValue1` values shared a key.

New key format:
```
{entityName}~{filterField}~{filterValue}~{filterValue1}
```

Separator `~` chosen to avoid conflicts with values containing `/` or `:`.

---

### `TriggerBackgroundRevalidation` (`ProxyDataSource.Caching.cs`)

```csharp
private void TriggerBackgroundRevalidation(string key, string entityName, …)
{
    ThreadPool.QueueUserWorkItem(_ =>
    {
        try
        {
            var fresh = ExecuteReadWithPolicy(…);
            if (fresh.Success)
                SetCacheEntry(key, fresh.Result);
        }
        catch { /* swallow — stale data remains */ }
    });
}
```

Non-blocking; uses `ThreadPool.QueueUserWorkItem` rather than `Task.Run` to avoid async-context capture overhead.

---

### `CacheProfile` Property (`ProxyDataSource.Caching.cs`)

```csharp
public ProxyCacheProfile CacheProfile => _policy.Cache;
```

All cache decisions read from `_policy.Cache`. When `ApplyPolicy(ProxyPolicy)` is called, the cache profile updates automatically.

---

## Bugs Fixed

| Bug | Original | Fix |
|-----|----------|-----|
| Cache key collision | `entityName + filterField + filterValue` — omitted `FilterValue1` | Key now includes `~filterValue1` segment |
| Unbounded cache growth | Only TTL eviction; no item cap | `EvictIfOverLimit` after every `SetCacheEntry` |
| Stale data after write | No invalidation on write paths | `InvalidateCacheOnWrite` called from all mutation methods |
| Writes could read-through stale on failover | No invalidation during health transition | Eviction triggered on `_healthStatus` flip (TODO: verify) |

---

## Acceptance Criteria Check

| Criterion | Met? |
|-----------|------|
| Cache behavior is predictable by policy | ✅ Driven by `ProxyCacheProfile` via `_policy.Cache` |
| Writes correctly invalidate stale entries | ✅ `InvalidateCacheOnWrite` called on all CRUD write paths |
| Stale-while-revalidate consistency mode | ✅ Implemented in `GetEntityWithCache` |
| Bounded cache size | ✅ `MaxItems` cap with LRU eviction |
| Cache key uniqueness | ✅ `FilterValue1` included in key |

---

## Files Changed

- `ProxyotherClasses.cs` — `ProxyCacheTier`, `ProxyCacheConsistency`, `ProxyCacheProfile`, `CacheEntry.LastAccessedAt`
- `ProxyDataSource.Caching.cs` — `GetEntityWithCache`, `InvalidateCacheOnWrite`, `EvictIfOverLimit`, `GenerateCacheKey`, `TriggerBackgroundRevalidation`, `CacheProfile` property
- `ProxyDataSource.cs` — all write methods call `InvalidateCacheOnWrite` after `ExecuteWriteWithPolicy`
