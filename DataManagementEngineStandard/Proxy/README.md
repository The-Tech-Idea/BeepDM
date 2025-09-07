# Proxy Data Source Layer

`ProxyDataSource` is an advanced adaptive wrapper around multiple concrete `IDataSource` instances that adds:

- Automatic failover
- Circuit breaker protection
- Health checking (timer driven)
- Weighted + latency‑aware load balancing
- Connection pooling (per underlying source)
- Retry with (configurable) incremental / exponential backoff
- Transparent cascading of the full `IDataSource` API
- Per entity query result caching (opt‑in)
- Metrics collection (counts, success/fail ratio, average latency, last activity)

It enables resilient multi‑backend execution: if a provider becomes unhealthy or slow, traffic shifts to healthier peers.

## High Level Architecture

```
                +--------------------+
 Client Calls → |   ProxyDataSource  | → Selects Healthy Target (LB + Circuit + Health)
                +----------+---------+
                           |
          +----------------+------------------+
          |                |                  |
      IDataSource A    IDataSource B     IDataSource C
          |                |                  |
      (RDBSource)      (RDBSource)        (Other)
```

## Core Capabilities

| Capability | Mechanism |
|------------|-----------|
| Failover | Iterates candidates; skips unhealthy / open circuit; raises `OnFailover` |
| Circuit Breaking | `CircuitBreaker` per data source (Closed → Open → HalfOpen) |
| Health Check | Timer invoking `PerformHealthCheck` with 5s bounded probe |
| Load Balancing | Weighted random + tie break by lower `AverageResponseTime` + fewer `TotalRequests` |
| Retry Policy | `RetryPolicy(Func<Task<bool>>)` + exponential backoff in `ExecuteWithPolicy` |
| Caching | Concurrent dictionary of `CacheEntry` keyed by entity + filter signature |
| Metrics | `DataSourceMetrics` (atomic counters + rolling average latency) |
| Connection Pooling | Bounded queue (`MaxPoolSize`) per data source + idle expiry |

## Key Classes

| Class | Role |
|-------|------|
| `ProxyDataSource` | Orchestrator & facade implementing `IDataSource` |
| `CircuitBreaker` | Failure threshold tracking with reset timeout |
| `DataSourceMetrics` | Thread‑safe counters & timing stats |
| `PooledConnection` | Wrapper for pooled `IDataSource` instance |
| `ProxyDataSourceOptions` | Tuning & feature flags |
| `CacheEntry` | Cached entity result container |

## Lifecycle
1. Construct with list of underlying data source names already registered in `IDMEEditor`.
2. Timer starts periodic health checks → updates `_healthStatus` & resets/marks failures.
3. All public `IDataSource` calls delegate through retry + failover logic.
4. On repeated transient failures for a source → circuit opens → excluded from selection until half‑open probe succeeds.

## Selection Algorithm (Simplified)
```
Healthy + !CircuitOpen → Order by Weight desc, then AvgLatency asc, then TotalRequests asc
Fallback: all sources if none healthy
```

## Caching
Use `GetEntityWithCache(entity, filters, expiration)` or normal `GetEntity` (no cache). Invalidate with `InvalidateCache("EntityName")` or all with `InvalidateCache()`.

## Metrics Snapshot
`GetMetrics()` returns immutable map: name → `DataSourceMetrics` (Totals / Success / Fail / AverageResponseTime / LastSuccessful etc.).

## Typical Usage
```csharp
var proxy = new ProxyDataSource(editor, new List<string>{"PrimaryDB","Replica1","Replica2"});
var list = proxy.GetEntity("Customers", new List<AppFilter>{ new AppFilter{ FieldName="Country", Operator="=", FilterValue="'USA'"}});
var cached = proxy.GetEntityWithCache("Orders", null, TimeSpan.FromMinutes(2));
var metrics = proxy.GetMetrics();
```

## Failover Event
Subscribe to `OnFailover` for observability:
```csharp
proxy.OnFailover += (s,e)=> logger.LogInformation($"Failover {e.FromDataSource} -> {e.ToDataSource}");
```

## Extending
Override / fork for:
- Custom load distribution (e.g. EWMA, p95 latency weighting)
- Different cache provider (Redis / MemoryCache)
- Additional transient exception types
- Adaptive retry backoff strategy

## Tuning Options (`ProxyDataSourceOptions`)
- `MaxRetries`
- `RetryDelayMilliseconds`
- `HealthCheckIntervalMilliseconds`
- `FailureThreshold`
- `CircuitResetTimeout`
- `EnableCaching`
- `DefaultCacheExpiration`
- `EnableLoadBalancing`

## Thread Safety
All shared mutable state uses `ConcurrentDictionary`, `Interlocked`, or local locks. Connection pool cleans expired objects defensively.

## Error Handling Strategy
- Transient exceptions (Timeout/IO) trigger retry
- Persistent exceptions trigger immediate failover attempt
- Logged via `IDMEEditor.AddLogMessage` and metrics updated

## Disposal
`Dispose()` stops timer, closes & disposes underlying connections, clears pools.

---
This proxy enables multi‑host resilience without changing existing `IDataSource` consumers.
