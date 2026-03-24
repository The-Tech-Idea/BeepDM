# Known Issues and Build Errors

_Last updated: 2026-03-19_

---

## Critical — Blocks Build

### CS0117: `ProxyPolicy` does not contain a definition for `Default`

**Location:** `ProxyDataSource.cs` — constructor body  
**Line:** `_policy = policy ?? ProxyPolicy.Default;`  
**Root cause:** The back-compat constructor calls `ProxyPolicy.Default` as a fallback, but the `ProxyPolicy` class in `ProxyotherClasses.cs` has no `Default` static property.

**Fix — add one line inside `ProxyPolicy` in `ProxyotherClasses.cs`:**

```csharp
// Inside ProxyPolicy class body, after existing properties:
public static ProxyPolicy Default => new ProxyPolicy();
```

`new ProxyPolicy()` already uses field initializers that set `Resilience = new()`, `Cache = new()`, and `RoutingStrategy = WeightedLatency`, so `Default` is a fully usable instance.

---

## High Priority — Correctness Risks

### Double-commit risk in `ProxyDataSource.Transactions.cs`

**Location:** `ProxyDataSource.Transactions.cs` — `BeginTransaction`, `EndTransaction`, `Commit`  
**Risk:** These methods call `ExecuteWithRetry(...)` which is the back-compat wrapper routing to `ExecuteReadWithPolicy`. Transaction commit operations are **not** read-safe — retrying them can apply the same transaction twice.

**Fix — update each transaction method to use `ExecuteWriteWithPolicy` with `NonIdempotentWrite`:**

```csharp
// Before (WRONG — retries allowed):
return ExecuteWithRetry("Commit", ds => ds.CommitTransaction());

// After (CORRECT — single execute, no retry):
return ExecuteWriteWithPolicy("Commit",
    ds => ds.CommitTransaction(),
    ProxyOperationSafety.NonIdempotentWrite);
```

Apply the same pattern to `BeginTransaction` and `EndTransaction`.

---

## Medium Priority — Quality / Performance

### Health check blocks a thread pool thread

**Location:** `ProxyDataSource.Routing.cs` — `IsDataSourceHealthy(string dsName)` or the timer callback  
**Risk:** `Task.Run(...).Wait()` or similar sync-over-async patterns in health check code block a thread pool thread during every health check cycle. Under high health check frequency this can starve the thread pool.

**Fix:** Make the timer callback async, or use a dedicated low-priority background thread with direct synchronous connection test rather than wrapping async code.

---

## Low Priority — Feature Gaps

### DMEEditor connection ownership — proxy cannot force reconnect (GAP-007)

**Location:** `ProxyDataSource.Routing.cs` — `IsDataSourceHealthy` + all operation paths  
**Risk:** The proxy borrows connections via `_dmeEditor.GetDataSource(dsName)`. If DMEEditor's cached `IDataSource` instance has a broken socket that `ConnectionStatus` hasn't detected, `IsDataSourceHealthy` probes a fresh pooled connection (healthy), but actual operations use the stale DMEEditor instance (broken). Manifests as operations failing immediately after a clean health check.

**Fix:** In `IsDataSourceHealthy`, test the DMEEditor-owned instance directly and call `Openconnection()` to force reconnect if needed — not a pooled copy:
```csharp
var ds = _dmeEditor.GetDataSource(dsName);
if (ds?.ConnectionStatus != ConnectionState.Open)
    ds?.Openconnection();
return ds?.ConnectionStatus == ConnectionState.Open;
```

---

### No distributed circuit state (in-process memory only) (GAP-008)

**Location:** `CircuitBreaker.cs` + `ProxyDataSource.Routing.cs`  
**Risk:** Each app instance in a web farm maintains independent circuit breaker state. One instance can detect a backend failure and open its circuit while other instances continue flooding the failing backend until they each independently accumulate enough failures. Not a bug in single-process deployments, but a known architectural limit for web-farm scenarios.  
**Fix (deferred to Phase 8):** Introduce `ICircuitStateStore` interface with in-process default and optional Redis-backed implementation. See `impl-remaining-gaps.md` GAP-008 for the full design.

---

### No `OnRecovery` event

When a datasource transitions from unhealthy → healthy in `PerformHealthCheck`, only internal state is updated. Callers (dashboards, alerting) have no way to subscribe to recovery notifications.

**Fix:** Add `event EventHandler<string> OnRecovery` to `IProxyDataSource`, raise it in `PerformHealthCheck` when `_healthStatus[dsName]` flips from `false` to `true`.

### No datasource role separation (Primary / Replica / Standby)

All datasources are treated equally. Writes route according to the normal `SelectCandidates()` logic and can go to any source, including read-only replicas.

**Fix:** Add `ProxyDataSourceRole` enum and a per-source role dictionary. Route writes only to `Primary`-role sources. Route reads to `Replica`-role sources first, falling back to `Primary`.

### Cache hit ratio not tracked in SLO snapshot

`ProxySloSnapshot.CacheHitRatio` field exists but is always 0 because no cache hit/miss counters are maintained.

**Fix:** Add `_cacheHits` and `_cacheMisses` Interlocked counters; increment in `GetEntityWithCache`; expose ratio in `GetSloSnapshot`.

---

## Resolved Issues (for reference)

| Issue | Where fixed | How |
|-------|------------|-----|
| `new Random()` per call — biased distribution | `ProxyDataSource.ExecutionHelpers.cs` | `ThreadLocal<Random> _threadRandom` |
| `_healthStatus` plain Dictionary — thread-unsafe | `ProxyDataSource.Routing.cs` | `ConcurrentDictionary<string, bool>` |
| Double execution in retry wrappers | `ProxyDataSource.ExecutionHelpers.cs` | `ExecuteReadWithPolicy` returns result of single operation call |
| `_options` diverges from constructor knobs | `ProxyDataSource.cs` — `InitializeFromPolicy` | `_policy` is single source of truth |
| Cache key collision on `FilterValue1` | `ProxyDataSource.Caching.cs` — `GenerateCacheKey` | Key includes `~filterValue1` segment |
| Circuit breaker closes on first success from HalfOpen | `CircuitBreaker.cs` | `ConsecutiveSuccessesToClose` threshold (default 2) |
