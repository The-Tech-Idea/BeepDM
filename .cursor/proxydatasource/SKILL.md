---
name: proxydatasource
description: >
  Guidance for creating, configuring, and using ProxyDataSource in BeepDM.
  ProxyDataSource wraps one or more IDataSource backends with transparent
  failover, load balancing, caching, watchdog health checks, circuit breaking,
  audit logging, fan-out reads, and SLO tracking.
  Use when designing resilient single-machine data tiers or when a single
  ProxyDataSource suffices and a full cluster is not needed.
---

# ProxyDataSource Guide

`ProxyDataSource` implements `IProxyDataSource` and extends the standard
`IDataSource` contract with resilience and observability features.
It is the per-node unit of work; for multi-machine deployments see
[`proxycluster`](../proxycluster/SKILL.md).

## Use this skill when

- Creating a `ProxyDataSource` over one or more local `IDataSource` backends
- Configuring retry / circuit-breaker / cache policy via `ProxyPolicy`
- Implementing active-active reads (replicas) with primary-only writes
- Setting up watchdog auto-failover and role promotion
- Attaching an audit sink for compliance-level operation logging
- Collecting per-datasource metrics and SLO compliance data
- Wrapping distributed transactions across backends

## Do not use this skill when

- Managing a fleet of nodes across machines. Use [`proxycluster`](../proxycluster/SKILL.md).
- Configuring connection strings or driver resolution. Use [`connection`](../connection/SKILL.md).
- Implementing a brand-new IDataSource driver. Use the driver template.
- General BeepDM service registration. Use [`beepservice`](../beepservice/SKILL.md).

## Core Types

| Type | Role |
|------|------|
| `ProxyDataSource` | Main implementation — wraps N backends |
| `IProxyDataSource` | Contract (extends `IDataSource`) |
| `ProxyPolicy` | Single source-of-truth for all resilience settings |
| `ProxyRoutingStrategy` | Enum: `WeightedLatency`, `LeastOutstandingRequests`, `RoundRobin`, `HealthWeighted` |
| `ProxyDataSourceRole` | Enum: `Primary`, `Replica`, `ReadOnly`, `Standby` |
| `ProxyNode` | Backing-datasource wrapper with weight and role |
| `IProxyAuditSink` | Pluggable audit destination |
| `FileProxyAuditSink` | Built-in NDJSON file audit sink |
| `NullProxyAuditSink` | No-op (default) |
| `DataSourceMetrics` | Per-datasource latency/error counters |
| `ProxySloSnapshot` | Point-in-time SLO compliance snapshot |
| `WatchdogNodeStatus` | Health probe result per node |

## Typical Workflow

1. Register backing datasources in `ConfigEditor`.
2. Construct `ProxyDataSource` with the datasource names array.
3. Call `ApplyPolicy()` to set resilience and routing behaviour.
4. Optionally assign `AuditSink`, subscribe to events, start watchdog.
5. Call `Openconnection()` — proxy opens all backends.
6. Use `IDataSource` methods (`GetEntity`, `InsertEntity`, etc.) as normal.
7. Call `Dispose()` — proxy closes all backends and flushes audit sink.

## Key Methods

```csharp
// Construction — pass editor + list of registered datasource names (+ optional policy)
var proxy = new ProxyDataSource(
    dmeEditor       : editor,
    dataSourceNames : new List<string> { "orders-primary", "orders-replica" },
    policy          : new ProxyPolicy
    {
        RoutingStrategy = ProxyRoutingStrategy.WeightedLatency,
        Resilience      = new ProxyResilienceProfile
        {
            MaxRetries         = 3,
            RetryBaseDelayMs   = 200,
            FailureThreshold   = 5
        },
        Cache = new ProxyCacheProfile
        {
            Enabled           = true,
            DefaultExpiration = TimeSpan.FromMinutes(5)
        }
    });

// Re-apply policy at any time
proxy.ApplyPolicy(new ProxyPolicy
{
    RoutingStrategy = ProxyRoutingStrategy.LeastOutstandingRequests,
    Resilience      = ProxyResilienceProfile.Balanced
});

// Typed load-balanced read
var rows = await proxy.ExecuteWithLoadBalancing(
    operation : ds => ds.GetEntityAsync("Orders", filters),
    isWrite   : false);

// Typed load-balanced write (primary only)
await proxy.ExecuteWithLoadBalancing(
    operation : ds => Task.FromResult(ds.InsertEntity("Orders", record)),
    isWrite   : true);

// Cache
var data = proxy.GetEntityWithCache("Products", null, TimeSpan.FromMinutes(5));
proxy.InvalidateCache("Products");

// Audit
proxy.AuditSink = new FileProxyAuditSink("audit-output-dir");

// Watchdog
proxy.WatchdogIntervalMs        = 5_000;
proxy.WatchdogFailureThreshold  = 2;
proxy.WatchdogRecoveryThreshold = 3;
proxy.OnRolePromoted += (s, e) => Console.WriteLine($"{e.DataSourceName} promoted to {e.NewRole}");
proxy.OnRoleDemoted  += (s, e) => Console.WriteLine($"{e.DataSourceName} demoted  to {e.NewRole}");
proxy.StartWatchdog();

// Metrics
var metrics = proxy.GetMetrics();
// m.TotalRequests, m.SuccessfulRequests, m.FailedRequests, m.AverageResponseTime, m.CircuitBreaks
var slo     = proxy.GetSloSnapshot("orders-primary");
// slo.P50LatencyMs, slo.P95LatencyMs, slo.P99LatencyMs, slo.ErrorRatePercent, slo.CacheHitRatio

// Roles
proxy.SetRole("orders-replica", ProxyDataSourceRole.Primary);
proxy.SetRole("orders-primary", ProxyDataSourceRole.Replica);
```

## Validation and Safety

- Pass the policy in the constructor or call `ApplyPolicy()` before `Openconnection()` if custom settings are needed.
- Write operations (`InsertEntity`, `UpdateEntity`, `DeleteEntity`, `ExecuteSql`) are automatically routed to Primary-role backends only.
- The circuit breaker opens after `ProxyResilienceProfile.FailureThreshold` failures; requests fail-fast until `CircuitResetTimeout` elapses.
- `FileProxyAuditSink` is not thread-safe for concurrent writers — one proxy instance per file path.
- `GetEntityWithCache` caches in-process; do not use for data with strict consistency requirements.
- Call `StopWatchdog()` before `Dispose()` to avoid a race on shutdown.
- `ErrorsInfo.Flag` must be checked — methods return `IErrorsInfo`, not exceptions, for expected failures.

## Pitfalls

- The first datasource name in `dataSourceNames` becomes the write target if no Primary role is assigned; always set roles explicitly.
- Enabling cache (`ProxyCacheProfile.Enabled = true`) without invalidating after writes leads to stale reads.
- Setting `ProxyResilienceProfile.CircuitResetTimeout = TimeSpan.Zero` effectively disables the circuit breaker.
- Using `ExecuteWithLoadBalancing` with `isWrite: false` for mutation operations bypasses write safety.
- `WatchdogIntervalMs` too low can flood the database with health probes.

## File Locations

| File | Purpose |
|------|---------|
| `DataManagementEngineStandard/Proxy/ProxyDataSource.cs` | Core class + constructor |
| `DataManagementEngineStandard/Proxy/ProxyDataSource.Routing.cs` | Read/write dispatch and retry logic |
| `DataManagementEngineStandard/Proxy/ProxyDataSource.Caching.cs` | Local in-process cache |
| `DataManagementEngineStandard/Proxy/ProxyDataSource.Watchdog.cs` | Health probe + auto-promotion |
| `DataManagementEngineStandard/Proxy/ProxyDataSource.Audit.cs` | Audit sink integration |
| `DataManagementEngineStandard/Proxy/ProxyDataSource.Transactions.cs` | BeginTransaction / Commit / EndTransaction |
| `DataManagementEngineStandard/Proxy/ProxyDataSource.Observability.cs` | Metrics & SLO tracking |
| `DataManagementEngineStandard/Proxy/ProxyDataSource.FanOut.cs` | Fan-out parallel reads |
| `DataManagementEngineStandard/Proxy/ProxyDataSource.CircuitState.cs` | Circuit breaker state |
| `DataManagementEngineStandard/Proxy/ProxyDataSource.RedisCircuitStateStore.cs` | Redis-backed shared circuit state |
| `DataManagementEngineStandard/Proxy/IProxyDataSource.cs` | Interface definition |
| `DataManagementEngineStandard/Proxy/ProxyotherClasses.cs` | Policy, events, metrics, watchdog DTOs |
| `DataManagementEngineStandard/Proxy/Examples/ProxyDataSourceExamples.cs` | Runnable examples |

## Example

```csharp
// Minimum viable setup
var proxy = new ProxyDataSource(
    dmeEditor       : editor,
    dataSourceNames : new List<string> { "sales-primary", "sales-replica" },
    policy          : new ProxyPolicy
    {
        Resilience = new ProxyResilienceProfile { MaxRetries = 2 },
        Cache      = new ProxyCacheProfile { Enabled = true }
    });

proxy.AuditSink = new FileProxyAuditSink(".");  // current directory
proxy.StartWatchdog();
proxy.Openconnection();

// Read (may hit replica)
var orders = proxy.GetEntity("Orders", new List<AppFilter>
{
    new AppFilter { FieldName = "CustomerId", Operator = "=", FilterValue = "42" }
});

// Write (always goes to primary)
proxy.InsertEntity("Orders", new { CustomerId = 42, Total = 99.99, Status = "New" });

// Metrics
var m = proxy.GetMetrics()["sales-primary"];
Console.WriteLine($"Avg latency: {m.AverageResponseTime:F1} ms  errors={m.FailedRequests}");

proxy.StopWatchdog();
proxy.Dispose();
```

## Related Skills

- [`proxycluster`](../proxycluster/SKILL.md) — multi-node cluster tier
- [`connection`](../connection/SKILL.md) — driver resolution and connection strings
- [`beepdm`](../beepdm/SKILL.md) — DMEEditor and overall orchestration
- [`configeditor`](../configeditor/SKILL.md) — registering datasources

## Detailed Reference

See [`reference.md`](./reference.md) for a complete, copy-paste API quick reference.
