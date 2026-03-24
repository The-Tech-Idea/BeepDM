# Remaining Gaps and Backlog

_Last updated: 2026-03-19_

Tracks all identified implementation gaps not yet addressed, ordered by priority.

---

## P0 — Must fix before production use

### GAP-001: `ProxyPolicy.Default` missing (build blocker)

**File:** `ProxyotherClasses.cs` → `ProxyPolicy` class  
**Impact:** Build fails — `CS0117`.  
**Fix:**
```csharp
public static ProxyPolicy Default => new ProxyPolicy();
```

---

### GAP-002: `ProxyDataSource.Transactions.cs` — double-commit risk

**File:** `ProxyDataSource.Transactions.cs`  
**Methods:** `BeginTransaction`, `EndTransaction`, `Commit`  
**Impact:** `Commit` is retried via the read-safe back-compat wrapper. A transient error during commit triggers a retry, which commits again → duplicate write.  
**Fix:**
```csharp
// Replace ExecuteWithRetry with ExecuteWriteWithPolicy + NonIdempotentWrite
return ExecuteWriteWithPolicy("Commit",
    ds => ds.CommitTransaction(),
    ProxyOperationSafety.NonIdempotentWrite);
```

---

## P1 — Important, implement before wide adoption

### GAP-003: No `OnRecovery` event

**WHY:** Callers (dashboards, alert bridges, auto-scaling hooks) need to know when a datasource comes back. Currently `OnFailover` fires on loss; nothing fires on recovery.  
**What to add:**
1. `event EventHandler<string> OnRecovery` in `IProxyDataSource`
2. Fire it in `ProxyDataSource.Routing.cs` → `PerformHealthCheck` when `_healthStatus[dsName]` transitions `false → true`
3. Include datasource name and recovery timestamp in event args

---

### GAP-004: No datasource role separation (Primary / Replica / Standby)

**WHY:** In a real multi-datasource setup (e.g., one primary SQL Server + two read replicas), writes must go only to the primary. Without role separation, `ExecuteWriteWithPolicy` can route a write to a read-only replica, causing a database error (not just a transient error — a `Persistent` error that will not retry and will cause data loss).

**What to add:**
```csharp
// ProxyotherClasses.cs
public enum ProxyDataSourceRole { Primary, Replica, Standby }

// ProxyDataSource
private readonly ConcurrentDictionary<string, ProxyDataSourceRole> _roles = new();

public void SetRole(string dsName, ProxyDataSourceRole role)
    => _roles[dsName] = role;
```

**Route writes to Primary only:**
```csharp
private IReadOnlyList<string> SelectWriteCandidates()
{
    var primaries = _dataSourceNames
        .Where(n => _roles.GetValueOrDefault(n, ProxyDataSourceRole.Primary) == ProxyDataSourceRole.Primary
                 && IsHealthy(n) && !IsCircuitOpen(n))
        .ToList();
    return primaries.Count > 0 ? primaries : _dataSourceNames.ToList(); // degraded fallback
}
```

`ExecuteWriteWithPolicy` uses `SelectWriteCandidates()` instead of `SelectCandidates()`.

---

## P2 — Quality improvements

### GAP-005: Health check blocks thread pool thread

**Location:** Timer callback in `ProxyDataSource.Routing.cs`  
**Problem:** Calling synchronous `Openconnection()` inside a `ThreadPool` timer callback is fine, but if `IsDataSourceHealthy` uses `Task.Run(...).Wait()` or `.Result` on an async method, it blocks a thread for the duration of the health check. Under aggressive health-check intervals (5 s, 3 sources) this can hold 3 threads continuously.  
**Fix:** Use a synchronous/low-level connection probe in the timer callback, or use `System.Threading.Timer` with a fully synchronous body.

---

### GAP-006: Cache hit ratio not tracked

**Location:** `ProxyDataSource.Caching.cs` + `ProxyDataSource.Observability.cs`  
**Problem:** `ProxySloSnapshot.CacheHitRatio` is always 0.  
**Fix:**
```csharp
// ProxyDataSource.cs or Caching.cs
private long _cacheHits;
private long _cacheMisses;

// In GetEntityWithCache:
if (cached != null && !cached.IsExpired)
    Interlocked.Increment(ref _cacheHits);
else
    Interlocked.Increment(ref _cacheMisses);

// In GetSloSnapshot:
long hits = Interlocked.Read(ref _cacheHits);
long misses = Interlocked.Read(ref _cacheMisses);
CacheHitRatio = hits + misses > 0 ? (double)hits / (hits + misses) : 0,
```

---

### GAP-007: DMEEditor owns connections — proxy cannot force reconnect

**WHY:** The proxy borrows connections via `_dmeEditor.GetDataSource(dsName)`. DMEEditor manages the underlying socket/connection lifecycle. If DMEEditor's cached instance enters a broken state (e.g., dropped socket after network blip that IDataSource.ConnectionStatus hasn't detected yet), the proxy has no path to force a fresh reconnect. `IsDataSourceHealthy` opens a *new* pooled connection for the health probe, but the stale DMEEditor-owned instance it then serves for actual operations may still be broken.  
**Scope:** Currently a single-process limitation; acceptable if DMEEditor reliably detects broken connections and reconnects on demand. Becomes critical if the underlying driver does NOT auto-reconnect.  
**Fix options:**
```csharp
// Option A — test the DMEEditor instance, not a pooled one
private bool IsDataSourceHealthy(string dsName)
{
    var ds = _dmeEditor.GetDataSource(dsName);
    if (ds == null) return false;
    try
    {
        if (ds.ConnectionStatus != ConnectionState.Open)
            ds.Openconnection();         // force reconnect on the DMEEditor instance
        return ds.ConnectionStatus == ConnectionState.Open;
    }
    catch { return false; }
}

// Option B — expose IDataSource.Reconnect() in the Beep contract
// and call it here instead of Openconnection()
```

---

### GAP-008: No distributed circuit state (in-process only)

**WHY:** All circuit breaker state (`_circuitBreakers`, `_healthStatus`, `_consecutiveUnhealthy`) lives in process memory. In a web-farm / multi-instance deployment, each application instance has an independent view of which backends are healthy. One instance can detect that DB2 is down and open its circuit; the other two instances continue hammering DB2 until they each independently accumulate enough failures. Under high traffic with a slow backend, this means `N_instances × FailureThreshold × RequestsPerCheck` extra failed calls leak through before the whole farm backs off.  
**Scope:** Not a bug for single-process deployments (CLI, desktop, single-server). Tracked as a known architectural limit for web-farm scenarios.  
**Fix (out-of-process circuit state):**
```csharp
// Interface for swappable circuit-state store
public interface ICircuitStateStore
{
    CircuitBreakerState GetState(string dsName);
    void RecordFailure(string dsName);
    void RecordSuccess(string dsName);
    void ForceOpen(string dsName);
    void ForceClose(string dsName);
}

// In-process default (current)
public class InProcessCircuitStateStore : ICircuitStateStore { ... }

// Redis-backed (for web farms)
public class RedisCircuitStateStore : ICircuitStateStore { ... }
```
Wire `ICircuitStateStore` into `ProxyPolicy` or inject via constructor. `CircuitBreaker.cs` delegates state reads/writes to the store instead of local fields.

---

## P3 — Future phases (not in scope yet)

| Phase | Gap description |
|-------|----------------|
| Phase 7 | Security: log redaction for query/filter values; datasource allowlist per policy |
| Phase 7 | Audit envelope: immutable route-decision record with policy version stamp |
| Phase 8 | Distributed circuit state via Redis (GAP-008) |
| Phase 8 | Connection ownership / reconnect contract (GAP-007) |
| Phase 8 | Adaptive throttling under saturation (`Saturation` error category detected) |
| Phase 8 | Capacity profiles (Small / Standard / HighThroughput) for pool sizing |
| Phase 8 | Stress-test benchmark suite |
| Phase 9 | Policy linting (`dotnet analyze` / custom Roslyn analyzer) |
| Phase 9 | Failover simulation test harness |
| Phase 9 | CI gate: reject PRs that change retry logic without test coverage |
| Phase 10 | KPI gate dashboard integration |
| Phase 10 | Wave rollout script (`Wave1 → Wave2 → Wave3 promote`) |
| Phase 10 | Hard-stop conditions: auto-revert when error rate > threshold |

---

## Gap Completion Checklist

```
[ ] GAP-001  ProxyPolicy.Default static property
[ ] GAP-002  Transactions.cs NonIdempotentWrite safety
[ ] GAP-003  OnRecovery event in IProxyDataSource + PerformHealthCheck
[ ] GAP-004  ProxyDataSourceRole enum + SelectWriteCandidates
[ ] GAP-005  Health check thread-pool safety
[ ] GAP-006  Cache hit ratio counters in SLO snapshot
```
