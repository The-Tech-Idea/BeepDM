---
name: proxycluster
description: >
  Guidance for creating, configuring, and operating a ProxyCluster in BeepDM.
  ProxyCluster is the cluster-tier orchestrator: it manages a pool of ProxyNode
  objects (each backed by an IProxyDataSource), and adds node management,
  cluster-wide policy fan-out, entity affinity routing, traffic splits,
  hedging, scatter-gather parallel reads, fault injection for testing, and
  distributed remote nodes via HTTP transport (Phase 12).
  Use when you need more than one ProxyDataSource — especially for multi-machine
  deployments, sharded data, or advanced traffic management scenarios.
---

# ProxyCluster Guide

`ProxyCluster` implements `IProxyCluster`, which extends `IProxyDataSource`.
It acts as a single logical datasource backed by **N nodes**, where each node
wraps its own `IProxyDataSource` (local or remote).

For single-machine, single-backing-datasource resilience, use
[`proxydatasource`](../proxydatasource/SKILL.md) directly.
`ProxyCluster` is the right choice when:

- You have two or more physical database servers (replicas, shards, regions).
- You need rolling restarts with drain semantics.
- You want traffic splits (canary, A/B).
- You want parallel scatter-gather across shards.
- You need distributed remote nodes on separate machines.

## Use this skill when

- Creating a `ProxyCluster` over multiple `IProxyDataSource` nodes
- Adding / removing nodes at runtime (`AddNode`, `RemoveNode`)
- Draining a node before maintenance (`DrainNode`, `DrainNodeAsync`)
- Applying cluster-wide policies with `ApplyClusterPolicy`
- Pinning entity types to specific nodes with entity-affinity routing
- Setting traffic-split percentages for canary deployments
- Configuring hedged requests for P99 latency reduction
- Running scatter-gather queries across shards
- Injecting faults for chaos/resilience testing
- Wiring remote machines via `HttpProxyTransport` + `RemoteProxyDataSource`
- Collecting aggregate cluster metrics and SLO snapshots

## Do not use this skill when

- You have a single database server with one or two replicas and don't need cross-machine routing. Use [`proxydatasource`](../proxydatasource/SKILL.md).
- The main task is configuring the policy algorithms (retries, circuit breaker). See ProxyPolicy in [`proxydatasource`](../proxydatasource/SKILL.md).
- Setting up connection strings. Use [`connection`](../connection/SKILL.md).

## Core Types

| Type | Role |
|------|------|
| `ProxyCluster` | Main implementation — owns N nodes |
| `IProxyCluster` | Contract (extends `IProxyDataSource`) |
| `IProxyNode` | Node contract |
| `ProxyNode` | Concrete node: wraps an `IProxyDataSource` with weight + role |
| `EntityAffinityMap` | Maps entity names to specific node IDs |
| `ProxyCluster.RoutingRules` | Dynamic routing rule engine |
| `RemoteProxyDataSource` | Client stub for a remote worker over HTTP |
| `HttpProxyTransport` | HTTP/JSON transport (client side) |
| `ProxyRemoteRequestDispatcher` | Server-side dispatcher (worker machine) |
| `NodeStatusEventArgs` | Payload for `OnNodeDown` / `OnNodeRestored` |

## Typical Workflow

### Local cluster (single machine)

1. Obtain or create `IProxyDataSource` instances for each backend.
2. Construct `ProxyCluster` with a unique name and `IDMEEditor`.
3. Add nodes with `AddNode(new ProxyNode(id, backingDs, weight, role))`.
4. Call `ApplyClusterPolicy` if non-default settings are needed.
5. Optionally: set entity affinity, traffic splits, subscribe to events.
6. Call `Openconnection()`.
7. Use the cluster exactly as an `IDataSource` — routing is transparent.
8. Call `Dispose()`.

### Distributed cluster (multi-machine)

1. **On each worker machine**: host `ProxyRemoteRequestDispatcher` in an ASP.NET Core Minimal API (see example below).
2. **On the coordinator**: create one `HttpProxyTransport` + `RemoteProxyDataSource` per worker.
3. Pass each `RemoteProxyDataSource` as the backing datasource of a `ProxyNode`.
4. Proceed as local cluster from step 2.

## Key Methods

```csharp
// Node management
cluster.AddNode(new ProxyNode("n1", proxyDs, weight: 5, role: ProxyDataSourceRole.Primary));
cluster.RemoveNode("n1");
IReadOnlyList<IProxyNode> nodes = cluster.GetNodes();

// Drain for maintenance
cluster.DrainNode("n2", timeoutMs: 30_000);
await cluster.DrainNodeAsync("n2", timeoutMs: 30_000, cancellationToken);

// Cluster-wide policy (applies to cluster AND all nodes)
cluster.ApplyClusterPolicy(new ProxyPolicy
{
    NodeRoutingStrategy = ProxyNodeRoutingStrategy.WeightedRoundRobin,
    Resilience          = new ProxyResilienceProfile { MaxRetries = 3 }
});

// Entity affinity — via ProxyPolicy, not runtime mutation methods
var affinityMap = new EntityAffinityMap();
affinityMap.MapEntity("Invoices", "n1");
affinityMap.MapEntity("Products", "n2");
cluster.ApplyClusterPolicy(new ProxyPolicy { EntityAffinity = affinityMap });

// Traffic split (canary) — via ProxyPolicy
cluster.ApplyClusterPolicy(new ProxyPolicy
{
    TrafficSplits = new[] { new TrafficSplitRule { TargetNodeId = "n3", WeightPercent = 10 } }
});

// Scatter-gather reads — via ProxyPolicy.ReadMode
cluster.ApplyClusterPolicy(new ProxyPolicy { ReadMode = ProxyReadMode.ScatterGather });
var merged = cluster.GetEntity("AuditLog", filters); // fans out to all nodes

// Fault injection (test/staging only!) — via ProxyPolicy
cluster.ApplyClusterPolicy(new ProxyPolicy
{
    FaultInjection = new FaultInjectionPolicy { TargetNodeId = "n2", ErrorRate = 0.20, DelayMs = 150 }
});
// Disable: re-apply policy with FaultInjection = null
cluster.ApplyClusterPolicy(new ProxyPolicy());

// Aggregate observability
IDictionary<string, DataSourceMetrics> metrics = cluster.GetClusterMetrics();
IReadOnlyList<ProxySloSnapshot>        slos    = cluster.GetClusterSloSnapshots();
```

## Distributed Remote Nodes (Phase 12)

### Worker machine setup (ASP.NET Core Minimal API)

```csharp
// Program.cs — worker machine
var dispatcher = new ProxyRemoteRequestDispatcher(editor, "local-db");

app.MapPost("/proxy/execute", async (HttpContext ctx, CancellationToken ct) =>
{
    var req  = await ctx.Request.ReadFromJsonAsync<ProxyRemoteRequest>(ct);
    var resp = await dispatcher.DispatchAsync(req, ct);
    await ctx.Response.WriteAsJsonAsync(resp);
});

app.MapGet("/proxy/ping", () => Results.Ok("pong"));
```

### Coordinator setup

```csharp
// Create one transport + stub per worker machine
var transportA = new HttpProxyTransport("http://worker-a:5100", TimeSpan.FromSeconds(10));
var remoteA    = new RemoteProxyDataSource(transportA, "worker-a", editor);

var transportB = new HttpProxyTransport("http://worker-b:5100", TimeSpan.FromSeconds(10));
var remoteB    = new RemoteProxyDataSource(transportB, "worker-b", editor);

var cluster = new ProxyCluster(editor, "global-cluster");
cluster.AddNode(new ProxyNode("worker-a", remoteA, weight: 5, role: ProxyDataSourceRole.Primary));
cluster.AddNode(new ProxyNode("worker-b", remoteB, weight: 3, role: ProxyDataSourceRole.Replica));

cluster.Openconnection();
var rows = cluster.GetEntity("Customers", null);
cluster.Dispose();
```

### HttpProxyTransport options

```csharp
// With timeout + API key
var transport = new HttpProxyTransport(
    baseUrl: "http://worker-a:5100",
    timeout: TimeSpan.FromSeconds(10),
    apiKey : "secret-key-abc");          // sent as X-Proxy-Api-Key header

// With external HttpClient (e.g. from IHttpClientFactory)
var transport = new HttpProxyTransport(
    baseUrl   : "http://worker-a:5100",
    httpClient: factory.CreateClient("proxy"),
    ownsClient: false);
```

## Validation and Safety

- `ApplyClusterPolicy` propagates to all nodes synchronously — call before `Openconnection` if possible.
- `DrainNode` blocks until all in-flight requests on that node finish or `timeoutMs` elapses; always await the async variant in web/service contexts.
- Fault injection is configured via `ProxyPolicy.FaultInjection` — for **test and staging only**, never set in production.
- Traffic splits are configured via `ProxyPolicy.TrafficSplits`; all split percentages apply to the shared routing pool.
- Scatter-gather is enabled via `ProxyPolicy.ReadMode = ProxyReadMode.ScatterGather`; all nodes receive the read in parallel.
- Entity affinity is configured via `ProxyPolicy.EntityAffinity` (an `EntityAffinityMap` instance); re-apply the policy to change affinity.
- `RemoteProxyDataSource.Dispose()` closes its transport (HTTP connections are returned to the pool).
- Security: protect the `/proxy/execute` endpoint with TLS + the `X-Proxy-Api-Key` header; never expose it on a public interface without authentication.
- The `ProxyRemoteRequestDispatcher` deserializes type-hinted records using `Type.GetType(assemblyQualifiedName)` — only accept requests from trusted coordinators.

## Pitfalls

- Adding a `RemoteProxyDataSource` as a node of the **same** cluster on the **same** machine creates a loopback cycle.
- After disabling fault injection, re-apply a clean `ProxyPolicy()` — orphaned policy objects do not self-clear.
- `EntityAffinityMap` entries are matched by entity name; if the pinned node ID is not in the cluster, routing silently falls back to the default strategy.
- Not draining before removing a node can cut off in-flight requests.
- `GetClusterMetrics()` aggregates per-node metrics — not a substitute for a real observability platform (Prometheus/Grafana) for production use.

## File Locations

| File | Purpose |
|------|---------|
| `DataManagementEngineStandard/Proxy/ProxyCluster.cs` | Core class + constructor |
| `DataManagementEngineStandard/Proxy/ProxyCluster.NodeManagement.cs` | AddNode, RemoveNode, GetNodes |
| `DataManagementEngineStandard/Proxy/ProxyCluster.NodeRouting.cs` | Request routing logic |
| `DataManagementEngineStandard/Proxy/ProxyCluster.NodeProbing.cs` | Health probing, outlier detection, replica lag |
| `DataManagementEngineStandard/Proxy/ProxyCluster.RoutingRules.cs` | Dynamic routing rules engine |
| `DataManagementEngineStandard/Proxy/ProxyCluster.Hedging.cs` | Hedged request implementation |
| `DataManagementEngineStandard/Proxy/ProxyCluster.ScatterGather.cs` | Parallel scatter-gather reads |
| `DataManagementEngineStandard/Proxy/ProxyCluster.FaultInjection.cs` | Chaos testing: error rate + delay injection |
| `DataManagementEngineStandard/Proxy/EntityAffinityMap.cs` | Entity→NodeId affinity registry |
| `DataManagementEngineStandard/Proxy/ProxyClusterEvents.cs` | Cluster event argument types |
| `DataManagementEngineStandard/Proxy/IProxyDataSource.cs` | `IProxyCluster` interface definition |
| `DataManagementEngineStandard/Proxy/Remote/HttpProxyTransport.cs` | HTTP transport (client) |
| `DataManagementEngineStandard/Proxy/Remote/RemoteProxyDataSource.cs` | Remote node stub |
| `DataManagementEngineStandard/Proxy/Remote/ProxyRemoteRequestDispatcher.cs` | Worker dispatcher (server) |
| `DataManagementEngineStandard/Proxy/Remote/ProxyRemoteProtocol.cs` | Shared wire DTOs |
| `DataManagementEngineStandard/Proxy/Examples/ProxyClusterExamples.cs` | Runnable cluster examples |

## Example

```csharp
// Full distributed cluster setup
var cluster = new ProxyCluster(editor, "regional-cluster");  // editor first!

// Local primary node
var localProxy = new ProxyDataSource(
    dmeEditor       : editor,
    dataSourceNames : new List<string> { "local-db" });
cluster.AddNode(new ProxyNode("local", localProxy, weight: 5, role: ProxyDataSourceRole.Primary));

// Remote replica on another machine
var transport = new HttpProxyTransport("http://replica-server:5100", TimeSpan.FromSeconds(8));
var remote    = new RemoteProxyDataSource(transport, "remote-replica", editor);
cluster.AddNode(new ProxyNode("remote", remote, weight: 3, role: ProxyDataSourceRole.Replica));

// Policy + entity affinity via ProxyPolicy
var affinityMap = new EntityAffinityMap();
affinityMap.MapEntity("FinancialReports", "local");

cluster.ApplyClusterPolicy(new ProxyPolicy
{
    NodeRoutingStrategy = ProxyNodeRoutingStrategy.WeightedRoundRobin,
    Resilience          = new ProxyResilienceProfile { MaxRetries = 2 },
    EnableHedging       = true,
    HedgingThresholdMs  = 80,
    MaxHedgeRequests    = 2,
    EntityAffinity      = affinityMap
});

// Events
cluster.OnNodeDown  += (s, e) => AlertOps($"Node down: {e.NodeId}  reason={e.Reason}");
cluster.OnFailover  += (s, e) => AlertOps($"Failover: {e.FromDataSource} → {e.ToDataSource}");

cluster.Openconnection();

// Reads spread across both nodes (weighted round-robin)
var orders = cluster.GetEntity("Orders", null);

// Scatter-gather: enable via policy, then use normal read methods
cluster.ApplyClusterPolicy(new ProxyPolicy { ReadMode = ProxyReadMode.ScatterGather });
var allLogs = cluster.GetEntity("AuditLog", null); // fans out to all live nodes

// Aggregate metrics
foreach (var (id, m) in cluster.GetClusterMetrics())
    Console.WriteLine($"{id}: avg={m.AverageResponseTime:F1}ms errors={m.FailedRequests}");

cluster.Dispose();
```

## Related Skills

- [`proxydatasource`](../proxydatasource/SKILL.md) — per-node resilience and single-machine setup
- [`connection`](../connection/SKILL.md) — driver resolution and connection strings
- [`beepdm`](../beepdm/SKILL.md) — DMEEditor orchestration
- [`configeditor`](../configeditor/SKILL.md) — registering datasource connections

## Detailed Reference

See [`reference.md`](./reference.md) for a complete, copy-paste API quick reference.
