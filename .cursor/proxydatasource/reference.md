# ProxyDataSource Quick Reference

## Construction

```csharp
// Primary constructor: editor + list of registered datasource names + policy
var proxy = new ProxyDataSource(
    dmeEditor       : editor,
    dataSourceNames : new List<string> { "primary-db", "replica-db" },
    policy          : new ProxyPolicy
    {
        RoutingStrategy = ProxyRoutingStrategy.WeightedLatency,
        Resilience      = ProxyResilienceProfile.Balanced,
        Cache           = new ProxyCacheProfile { Enabled = true }
    });

// Backward-compat constructor (override individual knobs only)
var proxy2 = new ProxyDataSource(
    dmeEditor            : editor,
    dataSourceNames      : new List<string> { "primary-db", "replica-db" },
    maxRetries           : 3,
    retryDelay           : 500,
    healthCheckInterval  : 30_000);
```

## Policy

```csharp
// Apply a new policy at any time (re-initialises circuit breakers etc.)
proxy.ApplyPolicy(new ProxyPolicy
{
    RoutingStrategy = ProxyRoutingStrategy.WeightedLatency,
    Resilience      = new ProxyResilienceProfile
    {
        ProfileType             = ProxyResilienceProfileType.Custom,
        MaxRetries              = 3,
        RetryBaseDelayMs        = 200,
        RetryMaxDelayMs         = 30_000,
        UseExponentialBackoff   = true,
        FailureThreshold        = 5,
        CircuitResetTimeout     = TimeSpan.FromMinutes(5),
        ConsecutiveSuccessesToClose = 2
    },
    Cache = new ProxyCacheProfile
    {
        Enabled           = true,
        Tier              = ProxyCacheTier.ShortLived,
        DefaultExpiration = TimeSpan.FromMinutes(5),
        Consistency       = ProxyCacheConsistency.WriteThrough
    },
    EnableHedging      = false,
    HedgingThresholdMs = 50,
    MaxHedgeRequests   = 2
});

// Backward-compat shortcut properties (synced from policy on construction)
proxy.MaxRetries                      = 3;
proxy.RetryDelayMilliseconds          = 200;
proxy.HealthCheckIntervalMilliseconds = 30_000;
```

## ProxyRoutingStrategy values

```csharp
ProxyRoutingStrategy.WeightedLatency           // prefer low-latency, high-weight sources
ProxyRoutingStrategy.LeastOutstandingRequests  // fewest in-flight requests
ProxyRoutingStrategy.RoundRobin                // simple sequential selection
ProxyRoutingStrategy.HealthWeighted            // weight inversely proportional to failure rate
```

## Connection

```csharp
var state = proxy.Openconnection();   // ConnectionState.Open | Broken
proxy.Closeconnection();
proxy.Dispose();
```

## Read Operations (routed to healthy node)

```csharp
IEnumerable<object> rows = proxy.GetEntity("Orders", filters);
PagedResult         page  = proxy.GetEntity("Orders", filters, pageNumber: 1, pageSize: 50);
IEnumerable<object> rows2 = proxy.RunQuery("SELECT * FROM Products WHERE Active=1");
double              count  = proxy.GetScalar("SELECT COUNT(*) FROM Orders");

// Async
var rowsAsync = await proxy.GetEntityAsync("Orders", filters);
var scalar    = await proxy.GetScalarAsync("SELECT SUM(Total) FROM Orders");
```

## Write Operations (primary only)

```csharp
IErrorsInfo ei;
ei = proxy.InsertEntity("Orders",  new { CustomerId = 1, Total = 99.99 });
ei = proxy.UpdateEntity("Orders",  new { OrderId = 42, Total = 120.00 });
ei = proxy.DeleteEntity("Orders",  new { OrderId = 42 });
ei = proxy.UpdateEntities("Orders", dataTable, progressCallback);
ei = proxy.ExecuteSql("UPDATE Orders SET Status='Closed' WHERE ... ");
```

## Load-balanced execution

```csharp
// Read — any healthy node
var result = await proxy.ExecuteWithLoadBalancing(
    operation : ds => ds.GetEntityAsync("Products", null),
    isWrite   : false);

// Write — primary only
await proxy.ExecuteWithLoadBalancing(
    operation : ds => Task.FromResult(ds.InsertEntity("Products", record)),
    isWrite   : true);
```

## Fan-out Writes

```csharp
// Fan-out and quorum writes are configured via ProxyPolicy.WriteMode
proxy.ApplyPolicy(new ProxyPolicy
{
    WriteMode        = ProxyWriteMode.FanOut,    // all Primaries receive the write
    // or
    // WriteMode     = ProxyWriteMode.QuorumWrite,
    // WriteFanOutQuorum = 2,                   // at least 2 must ack
});

// InsertEntity/UpdateEntity/DeleteEntity then automatically fan out to all Primaries.
proxy.InsertEntity("Orders", new { CustomerId = 1, Total = 99.99 });

// Restore single-primary writes
proxy.ApplyPolicy(new ProxyPolicy { WriteMode = ProxyWriteMode.SinglePrimary });
```

## Transactions

```csharp
var args = new PassedArgs();
proxy.BeginTransaction(args);
proxy.InsertEntity("Ledger", row1);
proxy.InsertEntity("Ledger", row2);
proxy.Commit(args);         // ok path
proxy.EndTransaction(args); // rollback path
```

## Metadata

```csharp
IEnumerable<string>      names     = proxy.GetEntitesList();
EntityStructure          structure = proxy.GetEntityStructure("Orders", refresh: false);
EntityStructure          structure2= proxy.GetEntityStructure(fnd, refresh: true);
bool                     exists    = proxy.CheckEntityExist("Orders");
int                      idx       = proxy.GetEntityIdx("Orders");
Type                     type      = proxy.GetEntityType("Orders");
IEnumerable<ChildRelation>    children = proxy.GetChildTablesList("Orders", "dbo", "");
IEnumerable<RelationShipKeys> fks      = proxy.GetEntityforeignkeys("Orders", "dbo");
IEnumerable<ETLScriptDet>     scripts  = proxy.GetCreateEntityScript(entities);
IErrorsInfo                   ei       = proxy.CreateEntities(entities);
bool                          created  = proxy.CreateEntityAs(entityStructure);
IErrorsInfo                   run      = proxy.RunScript(scriptDet);
```

## Caching

```csharp
// Get with TTL override
var data = proxy.GetEntityWithCache("Products", null, TimeSpan.FromMinutes(5));

// Invalidate a specific entity
proxy.InvalidateCache("Products");

// Invalidate all
proxy.InvalidateCache();
```

## Watchdog

```csharp
proxy.WatchdogIntervalMs        = 5_000;   // probe every 5 s
proxy.WatchdogProbeTimeoutMs    = 2_000;   // each probe times out after 2 s
proxy.WatchdogFailureThreshold  = 2;       // 2 consecutive failures → unhealthy
proxy.WatchdogRecoveryThreshold = 3;       // 3 consecutive successes → healthy

proxy.OnRolePromoted += (s, e) => Alert($"{e.DataSourceName} promoted to {e.NewRole}");
proxy.OnRoleDemoted  += (s, e) => Alert($"{e.DataSourceName} demoted  to {e.NewRole}");

proxy.StartWatchdog();
// ... app runs ...
proxy.StopWatchdog();

IReadOnlyList<WatchdogNodeStatus> statuses = proxy.GetWatchdogStatus();
```

## Roles

```csharp
proxy.SetRole("replica-db",  ProxyDataSourceRole.Primary);
proxy.SetRole("primary-db",  ProxyDataSourceRole.Replica);
// Roles: Primary, Replica, ReadOnly, Standby
```

## Audit

```csharp
proxy.AuditSink = new FileProxyAuditSink("operations.jsonl");
proxy.AuditSink = NullProxyAuditSink.Instance;  // disable
```

## Metrics & SLO

```csharp
IDictionary<string, DataSourceMetrics> all = proxy.GetMetrics();
DataSourceMetrics m = all["primary-db"];
Console.WriteLine($"total={m.TotalRequests}  ok={m.SuccessfulRequests}  errors={m.FailedRequests}  avg={m.AverageResponseTime:F1}ms  cb_breaks={m.CircuitBreaks}");

// SLO snapshot for one datasource
ProxySloSnapshot slo = proxy.GetSloSnapshot("primary-db");
Console.WriteLine($"p50={slo.P50LatencyMs:F1}ms  p95={slo.P95LatencyMs:F1}ms  p99={slo.P99LatencyMs:F1}ms  errors={slo.ErrorRatePercent:F2}%  cache={slo.CacheHitRatio:P1}");

// All datasources
IReadOnlyList<ProxySloSnapshot> all_slos = proxy.GetAllSloSnapshots();
```

## Events

```csharp
proxy.PassEvent   += (s, e) => Console.WriteLine(e.Messege);
proxy.OnFailover  += (s, e) => Alert($"Failover: {e.FromDataSource} → {e.ToDataSource}  reason={e.Reason}");
proxy.OnRecovery  += (s, e) => Alert($"Recovered: {e.DataSourceName}");
proxy.OnRolePromoted += (s, e) => Alert($"{e.DataSourceName} promoted: {e.OldRole} → {e.NewRole}");
proxy.OnRoleDemoted  += (s, e) => Alert($"{e.DataSourceName} demoted: {e.OldRole} → {e.NewRole}");
```

| Event | Args type | Key properties |
|-------|-----------|----------------|
| `OnFailover` | `FailoverEventArgs` | `FromDataSource`, `ToDataSource`, `Reason` |
| `OnRecovery` | `RecoveryEventArgs` | `DataSourceName`, `RecoveredAt` |
| `OnRolePromoted` / `OnRoleDemoted` | `RoleChangeEventArgs` | `DataSourceName`, `OldRole`, `NewRole`, `Reason` |

## Connection pool

```csharp
IDataSource conn = proxy.GetPooledConnection("primary-db");
proxy.ReturnConnection("primary-db", conn);
IDataSource direct = proxy.GetConnection("primary-db");
```
