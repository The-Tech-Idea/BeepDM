# ProxyCluster Quick Reference

Complete copy-paste API reference for `ProxyCluster`.
For policy/strategy values, see [`../proxydatasource/reference.md`](../proxydatasource/reference.md).

---

## Construction

```csharp
// 1. Create cluster  (editor first, then name)
var cluster = new ProxyCluster(editor, "cluster-name");

// 2. Create backing datasources (local)
var proxy = new ProxyDataSource(
    dmeEditor       : editor,
    dataSourceNames : new List<string> { "my-connection" });

// 3. Create backing datasources (remote via HTTP)
var transport = new HttpProxyTransport("http://worker:5100", TimeSpan.FromSeconds(10));
var remoteDs  = new RemoteProxyDataSource(transport, "remote-node-id", editor);

// 4. Build ProxyNode
var node = new ProxyNode(
    id     : "n1",
    source : proxy,            // or remoteDs
    weight : 5,                // relative traffic share (1-99)
    role   : ProxyDataSourceRole.Primary  // or Replica
);

// 5. Add to cluster
cluster.AddNode(node);
```

---

## Node Lifecycle

```csharp
// Add a node
cluster.AddNode(new ProxyNode("n2", proxy2, weight: 3, role: ProxyDataSourceRole.Replica));

// Remove a node (hard — no drain)
cluster.RemoveNode("n2");

// List current nodes
IReadOnlyList<IProxyNode> nodes = cluster.GetNodes();
foreach (var n in nodes)
    Console.WriteLine($"{n.Id}  weight={n.Weight}  role={n.Role}  healthy={n.IsHealthy}");

// Drain then remove (graceful)
cluster.DrainNode("n2", timeoutMs: 30_000);           // blocking
await cluster.DrainNodeAsync("n2", 30_000, ct);       // async variant
cluster.RemoveNode("n2");
```

---

## Connection

```csharp
cluster.Openconnection();                // open cluster (opens all nodes)
cluster.Closeconnection();               // close cluster
cluster.Dispose();                       // close + release all nodes + transports
```

---

## Cluster-Wide Policy Fan-Out

```csharp
// Push a ProxyPolicy to cluster AND all member nodes
cluster.ApplyClusterPolicy(new ProxyPolicy
{
    NodeRoutingStrategy  = ProxyNodeRoutingStrategy.WeightedRoundRobin,
    Resilience           = new ProxyResilienceProfile
    {
        MaxRetries         = 3,
        RetryBaseDelayMs   = 200,
        FailureThreshold   = 5
    },
    EnableHedging        = true,
    HedgingThresholdMs   = 80,
    MaxHedgeRequests     = 2
});
```

See `ProxyNodeRoutingStrategy` enum values: `LeastConnections`, `WeightedRoundRobin`, `LowestLatency`, `PrimaryWithStandby`, `ConsistentHash`.

---

## Entity Affinity

```csharp
// Build an EntityAffinityMap then apply via ProxyPolicy
var affinityMap = new EntityAffinityMap();
affinityMap.MapEntity("FinancialReports", "n1");
affinityMap.MapEntity("AuditLog",          "n2");
affinityMap.MapEntity("Orders",            "shard-east");
affinityMap.MapEntity("Invoices",          "shard-east");

cluster.ApplyClusterPolicy(new ProxyPolicy
{
    EntityAffinity   = affinityMap,
    AffinityFallback = EntityAffinityFallback.RouteToAny
});

// To clear affinity, re-apply a policy without EntityAffinity
cluster.ApplyClusterPolicy(new ProxyPolicy());
```

> Entity affinity is configured via `ProxyPolicy` — there are no runtime mutation methods (`SetEntityAffinity`, `ClearEntityAffinity`).

---

## Traffic Split (Canary / A/B)

```csharp
// Traffic splits are configured via ProxyPolicy — no runtime mutation methods.

// Send 10 % of traffic to canary node n3
cluster.ApplyClusterPolicy(new ProxyPolicy
{
    TrafficSplits = new[]
    {
        new TrafficSplitRule { TargetNodeId = "n3", WeightPercent = 10 }
    }
});

// Increase after validation
cluster.ApplyClusterPolicy(new ProxyPolicy
{
    TrafficSplits = new[]
    {
        new TrafficSplitRule { TargetNodeId = "n3", WeightPercent = 50 }
    }
});

// Remove split
cluster.ApplyClusterPolicy(new ProxyPolicy());
```

---

## Scatter-Gather (Parallel Reads)

```csharp
// Scatter-gather is enabled via ProxyPolicy.ReadMode — no ScatterGatherAsync() method.

// Enable: fan all reads out to ALL live nodes in parallel
cluster.ApplyClusterPolicy(new ProxyPolicy
{
    ReadMode = ProxyReadMode.ScatterGather
});

// Standard read calls now fan out automatically
var allRows = cluster.GetEntity("Logs", filters);
// or
var allRows = await cluster.GetEntityAsync("Logs", filters);

// Aggregate scalar across nodes manually
long totalCount = 0;
foreach (var node in cluster.GetNodes().Where(n => n.IsHealthy))
    totalCount += (long)(double)node.Proxy.GetScalar("SELECT COUNT(*) FROM Orders");

// Restore single-node reads
cluster.ApplyClusterPolicy(new ProxyPolicy { ReadMode = ProxyReadMode.SingleNode });
```

---

## Hedging

Hedging is configured via policy; no separate method is needed.

```csharp
cluster.ApplyClusterPolicy(new ProxyPolicy
{
    EnableHedging      = true,
    HedgingThresholdMs = 80,  // send to 2nd node if 1st hasn't replied in 80 ms
    MaxHedgeRequests   = 2    // max 2 in-flight copies including the original
});
// All IDataSource read calls through the cluster are now hedged automatically.
```

---

## Fault Injection (Chaos Testing)

**Test / staging environments only!**

```csharp
// Fault injection is configured via ProxyPolicy — no EnableFaultInjection() method.

// 20 % random errors + 150 ms artificial delay
cluster.ApplyClusterPolicy(new ProxyPolicy
{
    FaultInjection = new FaultInjectionPolicy
    {
        TargetNodeId = "n2",
        ErrorRate    = 0.20,  // 0.0–1.0  (20 %)
        DelayRate    = 1.0,   // 100 % of requests get delayed
        DelayMs      = 150
    }
});

// Disable — re-apply a policy with FaultInjection = null
cluster.ApplyClusterPolicy(new ProxyPolicy());
```

---

## Remote Nodes — Phase 12

### Coordinator side

```csharp
// Option A: timeout only
var transport = new HttpProxyTransport("http://worker-a:5100", TimeSpan.FromSeconds(10));

// Option B: timeout + API key
var transport = new HttpProxyTransport(
    baseUrl: "http://worker-a:5100",
    timeout: TimeSpan.FromSeconds(10),
    apiKey : "my-secret-key");             // sends X-Proxy-Api-Key header

// Option C: shared HttpClient (IHttpClientFactory pattern)
var transport = new HttpProxyTransport(
    baseUrl   : "http://worker-a:5100",
    httpClient: factory.CreateClient("proxy"),
    ownsClient: false);

// Wrap transport in a stub IDataSource
var remoteDs = new RemoteProxyDataSource(transport, "worker-a", editor);

// Use as any other node
cluster.AddNode(new ProxyNode("worker-a", remoteDs, weight: 5, role: ProxyDataSourceRole.Primary));
```

### Worker machine (ASP.NET Core Minimal API)

```csharp
// Program.cs on the worker machine
var builder = WebApplication.CreateBuilder(args);
var app     = builder.Build();

// Initialize BeepDM editor (singleton)
var editor     = /* ... your setup ... */;
var dispatcher = new ProxyRemoteRequestDispatcher(editor, "local-backing-connection");

// Endpoint
app.MapPost("/proxy/execute", async (HttpContext ctx, CancellationToken ct) =>
{
    // Auth check
    if (!ctx.Request.Headers.TryGetValue("X-Proxy-Api-Key", out var key) || key != "my-secret-key")
        { ctx.Response.StatusCode = 401; return; }

    var req  = await ctx.Request.ReadFromJsonAsync<ProxyRemoteRequest>(ct);
    var resp = await dispatcher.DispatchAsync(req!, ct);
    await ctx.Response.WriteAsJsonAsync(resp);
});

app.MapGet("/proxy/ping", () => Results.Ok("pong"));

app.Run("http://0.0.0.0:5100");
```

---

## Cluster Metrics & SLO

```csharp
// Per-node metrics aggregate
IDictionary<string, DataSourceMetrics> metrics = cluster.GetClusterMetrics();
foreach (var (nodeId, m) in metrics)
{
    Console.WriteLine($"{nodeId}:");
    Console.WriteLine($"  Avg   : {m.AverageResponseTime:F1} ms");
    Console.WriteLine($"  Total : {m.TotalRequests}");
    Console.WriteLine($"  OK    : {m.SuccessfulRequests}");
    Console.WriteLine($"  Errors: {m.FailedRequests}");
    Console.WriteLine($"  CB breaks: {m.CircuitBreaks}");
}

// Per-node SLO snapshots
IReadOnlyList<ProxySloSnapshot> slos = cluster.GetClusterSloSnapshots();
foreach (var s in slos)
{
    Console.WriteLine($"{s.DataSourceName}: " +
                      $"p50={s.P50LatencyMs:F1}ms  " +
                      $"p95={s.P95LatencyMs:F1}ms  " +
                      $"p99={s.P99LatencyMs:F1}ms  " +
                      $"errors={s.ErrorRatePercent:F2}%  " +
                      $"cache={s.CacheHitRatio:P1}");
}
```

---

## Cluster Events

```csharp
cluster.OnNodeDown     += (s, e) =>
    Console.Error.WriteLine($"[HA] Node DOWN: {e.NodeId}  alive={e.IsAlive}  reason={e.Reason}");

cluster.OnNodeRestored += (s, e) =>
    Console.WriteLine($"[HA] Node RESTORED: {e.NodeId}  at={e.OccurredAt}");

cluster.OnNodePromoted += (s, e) =>
    Console.WriteLine($"[HA] {e.DataSourceName}: {e.OldRole} → {e.NewRole}  reason={e.Reason}");

cluster.OnNodeDemoted  += (s, e) =>
    Console.WriteLine($"[HA] {e.DataSourceName}: {e.OldRole} → {e.NewRole}  reason={e.Reason}");

cluster.OnFailover     += (s, e) =>
    Console.WriteLine($"[HA] Failover: {e.FromDataSource} → {e.ToDataSource}  reason={e.Reason}");
```

`NodeStatusEventArgs` properties:

| Property | Type | Description |
|----------|------|-------------|
| `NodeId` | `string` | ID of the affected node |
| `IsAlive` | `bool` | `false` on `OnNodeDown`, `true` on `OnNodeRestored` |
| `Reason` | `string?` | Human-readable reason string |
| `OccurredAt` | `DateTime` | UTC timestamp of the event |

`FailoverEventArgs` properties: `FromDataSource`, `ToDataSource`, `Reason`.
`RoleChangeEventArgs` properties: `DataSourceName`, `OldRole`, `NewRole`, `Reason`.

---

## Rolling Restart Pattern

```csharp
var ids = cluster.GetNodes().Select(n => n.Id).ToList();
foreach (var id in ids)
{
    await cluster.DrainNodeAsync(id, timeoutMs: 30_000, ct);  // quiesce
    cluster.RemoveNode(id);
    // ... restart the backing process out-of-band ...
    await Task.Delay(5_000, ct);                               // wait for startup
    cluster.AddNode(new ProxyNode(id, rebuildProxy(id), weight: 5, ProxyDataSourceRole.Primary));
}
```

---

## Inherits Full IProxyDataSource Surface

`ProxyCluster` implements every method on `IProxyDataSource`, routing calls to the
selected node according to the active routing policy.
For the complete list of read / write / metadata / caching / watchdog / roles / audit methods,
see [`../proxydatasource/reference.md`](../proxydatasource/reference.md).

```csharp
// Any IDataSource call works transparently through the cluster
IEnumerable<object> rows  = cluster.GetEntity("Orders", null);
IErrorsInfo         ins   = cluster.InsertEntity("Orders", record);
cluster.ApplyPolicy(new ProxyPolicy { Strategy = ProxyStrategy.PrimaryOnly });
cluster.SetSloTarget(99.5, targetLatencyMs: 50);
cluster.StartWatchdog(intervalMs: 15_000);
```
