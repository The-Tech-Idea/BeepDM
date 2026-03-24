# Phase 11 — Proxy Tier Scaling: Load Balancing & Failover for ProxyDataSource Itself

**Status:** Planning  
**Priority:** P2 (infrastructure-scale feature)  
**Root:** `DataManagementEngineStandard/Proxy/`

---

## Problem Statement

`ProxyDataSource` already load-balances and fails over **between backend datasources**.  
But `ProxyDataSource` itself is a single instance — a **single point of failure at the proxy tier**.

When you need to:
- Run multiple application servers each with their own `ProxyDataSource`
- Scale the proxy tier horizontally (e.g., in a web farm or microservice deployment)
- Fail over from one proxy instance to another when a host becomes unreachable

...there is currently no mechanism.  All circuit state, health status, metrics, and routing
decisions live in-process in a single `ProxyDataSource`.  Two proxy instances on different
servers will have divergent health views and independent circuit breakers — **split-brain**.

---

## Goals

| Goal | Description |
|------|-------------|
| **G1** | Scale `ProxyDataSource` instances horizontally: N instances route to the same set of backends |
| **G2** | Shared circuit/health state across proxy instances (no split-brain) |
| **G3** | Automatic failover when a proxy instance itself becomes unavailable |
| **G4** | Aggregate metrics/SLOs across all proxy instances |
| **G5** | Propagate `ProxyPolicy` changes to all proxy instances |
| **G6** | Sticky session affinity option (certain workloads must stay on one proxy) |
| **G7** | Zero-downtime rolling restart of proxy instances |

---

## Architectural Options

### Option A — Hierarchical Proxy (Proxy-of-Proxies) ⭐ Simplest path

A top-level `ProxyDataSource` (the **coordinator proxy**) treats other registered
`ProxyDataSource` instances as its "backend datasources" via `DMEEditor`.

```
Client
  └─ CoordinatorProxy (ProxyDataSource)
        ├─ ProxyNode-1  (ProxyDataSource → [DB-A, DB-B])
        ├─ ProxyNode-2  (ProxyDataSource → [DB-A, DB-B])
        └─ ProxyNode-3  (ProxyDataSource → [DB-A, DB-B])
```

**How it works today:** `ProxyDataSource` already implements `IDataSource`.
A registered proxy instance can be named `"proxy-node-1"` and can be returned
by `DMEEditor.GetDataSource("proxy-node-1")`.  So a coordinator proxy can
treat proxy nodes as opaque datasource backends — all existing load balancing,
health check, and failover logic reuses unchanged.

**Gaps to fill:**
- Nested health probes will double-count latency (proxy checks proxy checks backend)
- `ProxyDataSource.IsDataSourceHealthy()` uses `ProxyLivenessHelper.IsAlive()` which opens
  a real connection — on a proxy node this works fine since Openconnection() routes to its own backends
- Circuit state must be shared (see G2)

**Pros:** ~80 % reuse, ships fastest  
**Cons:** Health probe depth makes diagnosis non-obvious; two layers of retry/backoff

---

### Option B — `ProxyCluster` (dedicated new class) ⭐ Recommended

A new `ProxyCluster : IProxyDataSource` that owns a list of `IProxyDataSource` instances
(called **ProxyNodes**) and routes requests across them.

```
Client
  └─ ProxyCluster
        ├─ ProxyNode-1  (ProxyDataSource → [DB-A, DB-B])
        ├─ ProxyNode-2  (ProxyDataSource → [DB-A, DB-B])
        └─ ProxyNode-3  (ProxyDataSource → [DB-A, DB-B])
        (shared ICircuitStateStore, IProxyAuditSink, ProxyPolicy)
```

`ProxyCluster` gets its own routing, health probing, and failover logic tuned for
**proxy-to-proxy** latency rather than proxy-to-backend latency.  All nodes share a
single `ICircuitStateStore`, `ProxyPolicy`, and `IProxyAuditSink`.

**Pros:** Purpose-built, clean separation of concerns, can aggregate metrics  
**Cons:** New class with ~400 LOC, partially duplicates routing logic

---

### Option C — Distributed Mesh (P2P, no coordinator)

Each `ProxyDataSource` publishes health/circuit state to an external store (Redis, Consul,
etcd) via a pluggable `IProxyStatePublisher`.  Consumers ask the store for the current
healthy node list and route locally.

```
ProxyNode-1                ProxyNode-2
   ├─ reads/writes ──────────────────── Redis / etcd
   └─ backends                         └─ backends
```

**Pros:** True horizontal scale, no coordinator SPOF  
**Cons:** Requires external infrastructure, complex consistency model, large scope

---

## Recommended Approach: Option B with Option C as a future extension

Implement `ProxyCluster` (Option B) now.  Its `ICircuitStateStore` slot already accepts
a distributed backend (from Phase 8 / ICircuitStateStore design), so Option C arrives for
free when someone drops in a Redis-backed `ICircuitStateStore`.

---

## New Abstractions

### `IProxyNode` interface

```csharp
namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// Represents a single addressable proxy node within a <see cref="ProxyCluster"/>.
    /// Wraps an IProxyDataSource with cluster-level metadata.
    /// </summary>
    public interface IProxyNode
    {
        string          NodeId        { get; }
        IProxyDataSource Proxy        { get; }
        int             Weight        { get; set; }   // routing weight
        ProxyDataSourceRole NodeRole  { get; set; }   // Primary / Replica / Standby
        bool            IsAlive       { get; }        // last known liveness
        DateTime        LastProbeUtc  { get; }
        DataSourceMetrics Metrics     { get; }
    }
}
```

---

### `ProxyCluster` class (new file: `ProxyCluster.cs`)

```csharp
namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// Routes operations across a horizontally-scaled set of <see cref="IProxyDataSource"/>
    /// nodes, with its own liveness probing, load balancing, and failover at the proxy tier.
    /// </summary>
    public class ProxyCluster : IProxyDataSource
    {
        // ── Node registry ──────────────────────────────────────────────
        private readonly ConcurrentDictionary<string, IProxyNode> _nodes = new();

        // ── Shared state (all nodes must use the same instance) ────────
        private ICircuitStateStore _circuitStateStore;   // inject shared Redis-backed store for G2
        private IProxyAuditSink    _auditSink;
        private ProxyPolicy        _clusterPolicy;

        // ── Scheduler ─────────────────────────────────────────────────
        private Timer              _nodeProbeTimer;

        // ── Construction ──────────────────────────────────────────────
        public ProxyCluster(
            ProxyPolicy           clusterPolicy,
            ICircuitStateStore    circuitStateStore = null,
            IProxyAuditSink       auditSink         = null)
        { ... }

        // ── Node management ────────────────────────────────────────────
        public void   AddNode(IProxyNode node);
        public void   RemoveNode(string nodeId);
        public IReadOnlyList<IProxyNode> GetNodes();

        // ── Policy propagation (G5) ────────────────────────────────────
        public void ApplyClusterPolicy(ProxyPolicy policy);  // fans out to all nodes

        // ── IProxyDataSource surface (delegates to selected node) ──────
        // ... all IDataSource + IProxyDataSource methods delegate via _selectedNode
    }
}
```

---

### `ProxyClusterPolicy` (new properties added to `ProxyPolicy`)

```csharp
// ── Phase 11: Cluster-tier routing ─────────────────────────────────────────

/// <summary>Strategy used to pick a proxy node for each operation.</summary>
public ProxyNodeRoutingStrategy NodeRoutingStrategy { get; init; }
    = ProxyNodeRoutingStrategy.LeastConnections;

/// <summary>How many consecutive node probe failures before a node is marked down.</summary>
public int NodeUnhealthyThreshold  { get; init; } = 2;

/// <summary>How many consecutive probe successes to restore a node.</summary>
public int NodeHealthyThreshold    { get; init; } = 1;

/// <summary>Interval (ms) between node liveness probes.</summary>
public int NodeProbeIntervalMs     { get; init; } = 5_000;

/// <summary>
/// When true, a client correlation ID (or user session ID) is used to pin
/// requests to the same node for the duration of a logical session.
/// </summary>
public bool EnableNodeAffinity     { get; init; } = false;

/// <summary>
/// How long an affinity binding is retained without traffic before expiry.
/// Only applies when EnableNodeAffinity = true.
/// </summary>
public int NodeAffinityTtlSeconds  { get; init; } = 300;
```

---

### `ProxyNodeRoutingStrategy` enum

```csharp
public enum ProxyNodeRoutingStrategy
{
    /// <summary>Each request goes to the node with fewest in-flight operations.</summary>
    LeastConnections,
    /// <summary>Weighted round-robin across nodes ordered by Weight property.</summary>
    WeightedRoundRobin,
    /// <summary>Node with the lowest rolling P50 latency gets the next request.</summary>
    LowestLatency,
    /// <summary>All requests go to the Primary node; Replicas are hot standby only.</summary>
    PrimaryWithStandby,
    /// <summary>Consistent hash on CorrelationId — naturally provides session affinity.</summary>
    ConsistentHash
}
```

---

## Component Responsibility Map

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ProxyCluster                                                           │
│  ┌─────────────┐  ┌───────────────────────────┐  ┌──────────────────┐  │
│  │ Node Probe  │  │ NodeRouter                │  │ Cluster Audit    │  │
│  │ Timer       │  │ (LeastConn / WRR / Hash)  │  │ (IProxyAuditSink)│  │
│  └─────────────┘  └───────────────────────────┘  └──────────────────┘  │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  IProxyNode  (N instances)                                        │  │
│  │                                                                   │  │
│  │  ┌─────────────────────────────────────────────────────────────┐ │  │
│  │  │  ProxyDataSource (existing, unchanged)                       │ │  │
│  │  │    ├─ Policy, Circuit store, Audit sink (shared refs)        │ │  │
│  │  │    ├─ Backend DS-A   Backend DS-B   Backend DS-C             │ │  │
│  │  │    └─ Health, Pool, Metrics, Watchdog (node-local)           │ │  │
│  │  └─────────────────────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  Shared across ALL nodes:                                               │
│    ICircuitStateStore  ─── optional Redis/etcd backend (G2)             │
│    ProxyPolicy         ─── single source of truth (G5)                  │
│    IProxyAuditSink     ─── single audit trail across cluster (G4)       │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## State Sharing Strategy (G2 — no split-brain)

| State | Scope | Sharing mechanism |
|-------|-------|-------------------|
| Circuit breaker open/closed | Per backend DS | `ICircuitStateStore` — inject same Redis-backed instance into all nodes |
| Node liveness (is ProxyNode up?) | Per node | `ProxyCluster` owns this — stored in `_nodes[].IsAlive`, probe runs on cluster coordinator |
| Backend health status | Per backend DS | Shared via `ICircuitStateStore` — already designed for this |
| Metrics | Per backend DS | Aggregated by `ProxyCluster.GetMetrics()` merging all node metrics |
| Policy | Cluster-wide | `ProxyCluster.ApplyClusterPolicy()` fans out to all nodes in one lock |
| Audit trail | Cluster-wide | Single `IProxyAuditSink` instance shared across all nodes |
| Affinity bindings | Per correlation/session | `ConcurrentDictionary<string, string>` on `ProxyCluster` — TTL expiry via background sweep |

**Distributed option (G2 full):** Replace `InProcessCircuitStateStore` with a
`RedisCircuitStateStore` that writes to a shared Redis key namespace.  All `ProxyDataSource`
nodes on all hosts read/write the same circuit state.  No changes to `ProxyDataSource` — the
`ICircuitStateStore` abstraction already supports this.

---

## Failover Flow at Proxy Tier (G3)

```
1. ProxyCluster.NodeProbeTimer fires every NodeProbeIntervalMs
2. For each node: call ProxyLivenessHelper.IsAlive(node.Proxy, timeoutMs)
3. Consecutive failures >= NodeUnhealthyThreshold → node.IsAlive = false
                                                   → Raise OnNodeDown event
4. NodeRouter excludes down nodes from candidate list
5. If ALL Primary nodes are down and Replica nodes exist → promote best Replica
   (mirrors existing Watchdog promotion logic, reuses RoleChangeEventArgs)
6. When down node recovers (consecutive successes >= NodeHealthyThreshold):
   → node.IsAlive = true → Raise OnNodeRestored event
   → Demote back to Replica if it was promoted
```

---

## Sticky Affinity Design (G6)

```
Incoming operation carries CorrelationId (set on AsyncLocal<string> in ExecutionHelpers.cs).

ProxyCluster.SelectNode():
  if (policy.EnableNodeAffinity)
  {
      if (_affinityMap.TryGetValue(ctx.CorrelationId, out nodeId)
          && _nodes[nodeId].IsAlive)
          return _nodes[nodeId];

      // New binding: assign to best node by current strategy, then remember it
      var node = _nodeRouter.SelectBest(...);
      _affinityMap[ctx.CorrelationId] = node.NodeId;
      return node;
  }
  return _nodeRouter.SelectBest(candidates);
```

TTL expiry: a background `Timer` sweeps `_affinityMap` every 60 s and removes entries
older than `NodeAffinityTtlSeconds`.

---

## PolicyPropagation Design (G5)

```csharp
public void ApplyClusterPolicy(ProxyPolicy policy)
{
    _clusterPolicy = policy ?? throw new ArgumentNullException(nameof(policy));

    // Fan out to each node synchronously — nodes apply immediately
    foreach (var node in _nodes.Values)
        node.Proxy.ApplyPolicy(policy);   // existing ApplyPolicy on ProxyDataSource

    OnClusterPolicyChanged?.Invoke(this, new ClusterPolicyChangedEventArgs(policy));
}
```

For versioned rollout (canary): the cluster policy keeps a `RolloutPercentage` field
that causes `SelectNode` to split traffic — rolling percentage of nodes get the new policy
while the rest keep the old one.

---

## Metrics Aggregation (G4)

`ProxyCluster.GetMetrics()` merges `DataSourceMetrics` from all nodes:

```csharp
public IDictionary<string, DataSourceMetrics> GetClusterMetrics()
{
    var merged = new Dictionary<string, DataSourceMetrics>();
    foreach (var node in _nodes.Values)
    {
        foreach (var (dsName, m) in node.Proxy.GetMetrics())
        {
            if (!merged.TryGetValue(dsName, out var agg))
                merged[dsName] = agg = new DataSourceMetrics();
            agg.TotalRequests     += m.TotalRequests;
            agg.SuccessfulRequests += m.SuccessfulRequests;
            agg.FailedRequests    += m.FailedRequests;
            // P50/P99 latency: re-compute from merged histogram if available
            // otherwise: weighted average by TotalRequests
            agg.P50LatencyMs = WeightedAvg(agg, m);
        }
    }
    return merged;
}
```

---

## Zero-downtime Rolling Restart (G7)

Protocol for restarting a proxy node without dropping requests:

```
1. Signal node N to enter DRAINING mode
   → NodeDrainMode: new routing skips this node, in-flight ops complete
2. Wait for node.InFlightCount == 0  (or MaxDrainWaitMs timeout)
3. Restart / redeploy node N
4. Node N re-registers with ProxyCluster (or cluster probes and rediscovers it)
5. node.IsAlive flips true after NodeHealthyThreshold probes pass
```

New `ProxyNodeDrainMode` enum + `DrainNode(string nodeId, int drainTimeoutMs)` method
on `ProxyCluster`.

---

## New Events

| Event | Payload | Description |
|-------|---------|-------------|
| `OnNodeDown` | `NodeStatusEventArgs` | A proxy node failed liveness probe threshold |
| `OnNodeRestored` | `NodeStatusEventArgs` | A previously-down node is healthy again |
| `OnNodePromoted` | `RoleChangeEventArgs` | Replica node promoted to Primary at cluster tier |
| `OnNodeDemoted` | `RoleChangeEventArgs` | Promoted node demoted after original Primary recovers |
| `OnClusterPolicyChanged` | `ClusterPolicyChangedEventArgs` | Policy fanned out to all nodes |

---

## Implementation Phases

### Phase 11.1 — Core ProxyCluster skeleton (P2, ~1 day)

Files to create:
- `ProxyCluster.cs` — implements `IProxyDataSource`, holds `IProxyNode[]`
- `ProxyCluster.NodeManagement.cs` — `AddNode`, `RemoveNode`, `GetNodes`, drain logic
- `ProxyCluster.NodeProbing.cs` — probe timer, liveness, threshold counting
- `ProxyCluster.NodeRouting.cs` — `NodeRouter` (LeastConnections, WeightedRoundRobin, LowestLatency, PrimaryWithStandby, ConsistentHash)
- `ProxyNode.cs` — concrete `IProxyNode` implementation

Files to modify:
- `ProxyotherClasses.cs` — add `ProxyNodeRoutingStrategy` enum + cluster policy fields to `ProxyPolicy`
- `IProxyDataSource.cs` — add cluster-related members OR create `IProxyCluster : IProxyDataSource`

### Phase 11.2 — Shared state wiring (P2, ~0.5 day)

- `ProxyCluster` constructor enforces single shared `ICircuitStateStore` injected to all nodes
- `ProxyCluster` constructor enforces single shared `IProxyAuditSink` injected to all nodes
- `ApplyClusterPolicy` fan-out implemented

### Phase 11.3 — Affinity + drain (P2, ~0.5 day)

- `_affinityMap` with TTL sweep
- `DrainNode(string nodeId, int drainTimeoutMs)` method
- `InFlightCount` counter on each node (increment on enter execute, decrement on exit)

### Phase 11.4 — Metrics aggregation (P2, ~0.5 day)

- `GetClusterMetrics()` implemented with weighted average latency
- `GetClusterSloSnapshot()` combining all node SLO snapshots

### Phase 11.5 — Redis-backed circuit state (P3, infrastructure required)

- `RedisCircuitStateStore : ICircuitStateStore`
  - Uses `StackExchange.Redis`
  - Key format: `beep:proxy:circuit:{dsName}`
  - JSON-serialized `CircuitState` (`Open`, `HalfOpen`, `Closed`, counters, reset time)
  - Subscribe to Redis keyspace notifications to get instant state changes
- Inject into `ProxyCluster` constructor
- No changes to `ProxyDataSource` or `CircuitBreaker` — they use `ICircuitStateStore` already

---

## File/Type Summary

| New File | New Types | Purpose |
|----------|-----------|---------|
| `ProxyCluster.cs` | `ProxyCluster`, `IProxyCluster` | Cluster entry point |
| `ProxyCluster.NodeManagement.cs` | partial `ProxyCluster` | Add/Remove/Drain nodes |
| `ProxyCluster.NodeProbing.cs` | partial `ProxyCluster` | Liveness, threshold, events |
| `ProxyCluster.NodeRouting.cs` | partial `ProxyCluster`, `INodeRouter`, `LeastConnectionsRouter`, `WeightedRoundRobinRouter`,`LowestLatencyRouter`, `PrimaryWithStandbyRouter`, `ConsistentHashRouter` | Node selection strategies |
| `ProxyNode.cs` | `ProxyNode : IProxyNode` | Concrete node wrapper |
| `ProxyClusterEvents.cs` | `NodeStatusEventArgs`, `ClusterPolicyChangedEventArgs` | Event payloads |
| `RedisCircuitStateStore.cs` | `RedisCircuitStateStore : ICircuitStateStore` | Ph 11.5 distributed state |

Modify:
- `ProxyotherClasses.cs` — `ProxyNodeRoutingStrategy` enum + cluster policy fields
- `IProxyDataSource.cs` — optional: `IProxyCluster` extends `IProxyDataSource`

---

## Usage Example

```csharp
// --- Single-server (today) ---
var proxy = new ProxyDataSource(dme, new[] { "db-a", "db-b" }, policy);

// --- Scaled across 3 app servers (Phase 11) ---
var sharedCircuitStore = new RedisCircuitStateStore("redis:6379");   // Phase 11.5
var sharedAuditSink    = new FileProxyAuditSink("C:/logs/proxy-audit");

var clusterPolicy = new ProxyPolicy
{
    NodeRoutingStrategy  = ProxyNodeRoutingStrategy.LeastConnections,
    NodeProbeIntervalMs  = 5_000,
    EnableNodeAffinity   = false,
    WriteMode            = ProxyWriteMode.QuorumWrite,
    WriteFanOutQuorum    = 2
};

var cluster = new ProxyCluster(clusterPolicy, sharedCircuitStore, sharedAuditSink);

// Each node is a full ProxyDataSource pointing at same backends
// (could also be on separate hosts registered via DMEEditor)
foreach (var nodeId in new[] { "proxy-node-1", "proxy-node-2", "proxy-node-3" })
{
    var nodeProxy = new ProxyDataSource(dme, new[] { "db-a", "db-b" },
        clusterPolicy, sharedCircuitStore, sharedAuditSink);
    cluster.AddNode(new ProxyNode(nodeId, nodeProxy, weight: 1,
        role: nodeId == "proxy-node-1"
            ? ProxyDataSourceRole.Primary
            : ProxyDataSourceRole.Replica));
}

// Use cluster exactly like a single ProxyDataSource
var result = cluster.GetEntity("Product", filters);
```

---

## Risks

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Nested health probes cause cascade (cluster probes proxy which probes DB) | Medium | Separate `NodeProbeIntervalMs` from `HealthCheckIntervalMs`; use a lightweight TCP ping for node probes rather than a full DB query |
| Affinity map unbounded growth | Low | TTL sweep + max-map-size cap (evict LRU on overflow) |
| Policy fan-out partial failure (node N gets new policy, node N+1 crashes mid-fan-out) | Low | Each node's `ApplyPolicy` is idempotent; retry on re-registration |
| Redis unavailable takes down circuit state | Medium | `RedisCircuitStateStore` falls back to `InProcessCircuitStateStore` on connection failure |
| In-flight count leaks if Execute throws without decrement | Low | Use `Interlocked.Decrement` in a `finally` block |

---

## Decision Points Before Implementation

1. **IProxyCluster or reuse IProxyDataSource?**  
   Option A: `ProxyCluster : IProxyDataSource` — drops in anywhere a datasource is expected.  
   Option B: `IProxyCluster : IProxyDataSource` — adds cluster-specific methods (drain, node management) to the contract.  
   **Recommendation:** Option B — gives typed access to cluster-only operations without casting.

2. **Node discovery: static list or dynamic registration?**  
   Start with static (`AddNode` call at startup). Dynamic registration (nodes self-announce via shared Redis key) is a Phase 11.5 extension.

3. **ProxyPolicy reuse vs ClusterPolicy subclass?**  
   Extend `ProxyPolicy` with opt-in cluster fields (current plan).  A subclass `ClusterProxyPolicy` is also viable but adds a type hierarchy.

---

## Transparent IDataSource Connection Model

### The Core Question

> *"How does a user who only knows `IDataSource` connect to the cluster?"*

They should not need to know whether they are talking to a single `ProxyDataSource`
or a `ProxyCluster` of 10 nodes.  The answer is **registration transparency**.

### Registration via ConfigEditor (same as any datasource)

```csharp
// ── SETUP (done once at startup, e.g. Program.cs or a DI bootstrapper) ──────

var clusterPolicy = new ProxyPolicy
{
    NodeRoutingStrategy = ProxyNodeRoutingStrategy.LeastConnections,
    WriteMode           = ProxyWriteMode.QuorumWrite,
    WriteFanOutQuorum   = 2
};

var cluster = new ProxyCluster(clusterPolicy, sharedCircuitStore, sharedAuditSink);
cluster.AddNode(new ProxyNode("node-1", proxy1, weight: 1));
cluster.AddNode(new ProxyNode("node-2", proxy2, weight: 1));
cluster.AddNode(new ProxyNode("node-3", proxy3, weight: 1));

// Register under a logical name — same as any IDataSource
dme.RegisterDataSource("orders-cluster", cluster);   // new extension method on IDMEEditor

// ── USAGE (application code — no awareness of cluster at all) ─────────────

var ds = dme.GetDataSource("orders-cluster");         // returns the ProxyCluster
var rows = ds.GetEntity("Orders", filters);           // routes transparently
```

The user's code is identical whether `"orders-cluster"` resolves to a bare `SQLiteDataSource`,
a single `ProxyDataSource`, or a 10-node `ProxyCluster`.

### ConfigEditor Connection Properties

`ProxyCluster` registers itself using a new `DatasourceType.ProxyCluster` value (or reuses
`DatasourceType.Proxy`).  The connection string carries a simple JSON descriptor:

```json
{
  "type": "ProxyCluster",
  "nodes": ["proxy-node-1", "proxy-node-2", "proxy-node-3"],
  "policy": "orders-cluster-policy"
}
```

`DMEEditor.GetDataSource("orders-cluster")` resolves the descriptor, looks up each node by
name (they must already be registered), builds the `ProxyCluster`, and returns it — lazy, on
first access.

### AddinAttribute Discovery

`ProxyCluster` carries `[AddinAttribute]` just like any driver.  This lets
`AssemblyHandler` auto-discover and load it:

```csharp
[AddinAttribute(
    Caption       = "BeepDM Proxy Cluster",
    Name          = "ProxyCluster",
    DatasourceType = DatasourceType.Proxy,
    Category      = DatasourceCategory.RDBMS)]
public class ProxyCluster : IProxyDataSource { ... }
```

### Connection Setup Matrix

| User code | Under the hood | User change required? |
|-----------|---------------|----------------------|
| `dme.GetDataSource("db-a")` | Direct `SQLiteDataSource` | none |
| `dme.GetDataSource("orders-proxy")` | Single `ProxyDataSource` → 2 backends | none |
| `dme.GetDataSource("orders-cluster")` | `ProxyCluster` → 3 proxy nodes → 2 backends each | none |
| Switch from single proxy to cluster | Update connection registration at startup only | startup only |

---

## Entity-Affinity Routing (Kafka-Style Partition Leadership)

### Concept

Kafka assigns each **topic-partition** to exactly one broker as **leader** — all reads and
writes for that partition go to the leader node.  We apply the same idea at the proxy tier:
certain **entities** (tables / collections) are pinned to specific proxy nodes.

**Why:**
- Improves cache locality — node N always serves `Orders`, its in-process cache stays warm
- Enables per-entity throughput limits (node N's capacity = Orders capacity)
- Allows entity-specific policy (separate resilience profile for Orders vs. Products)
- Mirrors Vitess's **VSchema** (entity→shard mapping) and ProxySQL's **query rules**

### `EntityAffinityMap` (new type)

```csharp
/// <summary>
/// Maps entity names to the proxy node that should service them.
/// Supports wildcard prefix matching: "Order*" → "node-1".
/// </summary>
public class EntityAffinityMap
{
    // exact-match rules (highest priority)
    private readonly Dictionary<string, string> _exact  = new(StringComparer.OrdinalIgnoreCase);
    // prefix rules (e.g., "Order" matches "Orders", "OrderLines")
    private readonly List<(string Prefix, string NodeId)> _prefixes = new();
    // fallback node (null = use normal cluster routing)
    public string FallbackNodeId { get; set; }

    public void MapEntity(string entityName, string nodeId);
    public void MapPrefix(string prefix,     string nodeId);

    /// <summary>Returns the designated node ID, or null to use cluster routing.</summary>
    public string Resolve(string entityName);
}
```

### Integration into `ProxyCluster.SelectNode()`

```csharp
private IProxyNode SelectNode(string operationName, string entityName, ProxyExecutionContext ctx)
{
    // 1. Entity affinity — hard-pinned entities always go to designated node
    if (_entityAffinity != null && entityName != null)
    {
        var affinityNodeId = _entityAffinity.Resolve(entityName);
        if (affinityNodeId != null
            && _nodes.TryGetValue(affinityNodeId, out var affinityNode)
            && affinityNode.IsAlive)
            return affinityNode;
        // designated node is down → fall through to normal routing (configurable: hard-fail or graceful)
    }

    // 2. Session affinity
    if (_clusterPolicy.EnableNodeAffinity && ctx != null)
    { ... }

    // 3. Normal routing strategy
    return _nodeRouter.SelectBest(LiveNodes, ctx);
}
```

### `EntityAffinityPolicy` in `ProxyPolicy`

```csharp
/// <summary>Entity → node affinity rules. Null = no entity pinning.</summary>
public EntityAffinityMap EntityAffinity { get; init; } = null;

/// <summary>
/// When the designated node for an entity is down, whether to hard-fail
/// (throw) or gracefully fall back to normal cluster routing.
/// </summary>
public EntityAffinityFallback AffinityFallback { get; init; }
    = EntityAffinityFallback.UseClusterRouting;

public enum EntityAffinityFallback { UseClusterRouting, Throw }
```

### Usage Example

```csharp
var affinity = new EntityAffinityMap();
affinity.MapEntity("Orders",       "node-1");   // Orders → always node-1
affinity.MapEntity("OrderLines",   "node-1");   // OrderLines → always node-1
affinity.MapPrefix("Inventory",    "node-2");   // Inventory*, InventoryLog, etc. → node-2
affinity.MapEntity("Products",     "node-3");
affinity.FallbackNodeId = null;                 // other entities → normal routing

var policy = new ProxyPolicy
{
    EntityAffinity   = affinity,
    AffinityFallback = EntityAffinityFallback.UseClusterRouting
};
```

### Rebalancing (Kafka-style Leader Election)

When a node is added or removed, `ProxyCluster.RebalanceAffinity()` re-assigns orphaned entity
bins to remaining nodes.

```
Algorithm (simple consistent hash):
  entityBins = EntityAffinityMap.AllRules.GroupBy(nodeId)
  for each bin whose nodeId is no longer alive:
      best = LiveNodes.OrderBy(n => n.EntityBinCount).First()
      Reassign bin → best.NodeId
  Raise OnAffinityRebalanced event
```

---

## Feature Borrowings from Well-Known Systems

### From Envoy / Istio — Request Hedging

> *"Send the same request to a backup node if the primary doesn't respond within a
> threshold. Use whichever response arrives first."*

Reduces P99 tail latency at the cost of extra load (~2× for the timeout window).

```csharp
// New policy fields
public bool EnableHedging        { get; init; } = false;
public int  HedgingThresholdMs   { get; init; } = 200;   // hedge if no response within 200 ms
public int  MaxHedgeRequests     { get; init; } = 1;     // at most 1 extra in-flight copy
```

`ProxyCluster` starts a second request to the next-best node after `HedgingThresholdMs`,
cancels the slower one on first response.  Only applied to read-safe operations.

---

### From ProxySQL / MaxScale — Query Routing Rules

> *"Route specific operations to specific nodes based on operation name regex or entity name
> pattern, not just entity affinity."*

More expressive than `EntityAffinityMap` — rules can match on operation name, entity name,
and safety class simultaneously.

```csharp
public class ProxyRoutingRule
{
    public Regex   OperationPattern { get; init; }   // e.g., new Regex("^Report")
    public Regex   EntityPattern    { get; init; }   // e.g., new Regex("^Archive")
    public string  TargetNodeId     { get; init; }   // hard node, or null = use strategy
    public ProxyNodeRoutingStrategy? OverrideStrategy { get; init; }
    public int     Priority         { get; init; } = 0;  // higher wins
}

// In ProxyPolicy:
public IReadOnlyList<ProxyRoutingRule> RoutingRules { get; init; } = Array.Empty<ProxyRoutingRule>();
```

Rules are evaluated in descending priority order before entity affinity.
Example:
```csharp
new ProxyRoutingRule
{
    OperationPattern = new Regex("^(RunReport|ExportCsv)", RegexOptions.IgnoreCase),
    TargetNodeId     = "node-analytics",   // heavy read-only ops go to the analytics node
    Priority         = 100
},
new ProxyRoutingRule
{
    EntityPattern = new Regex("^Archive"),
    OverrideStrategy = ProxyNodeRoutingStrategy.PrimaryWithStandby,
    Priority = 50
}
```

---

### From Vitess (YouTube's MySQL scaler) — Scatter-Gather Reads

> *"Fan out a read to ALL nodes, then merge results.  Used for aggregations or global
> entity searches where no single node has the full dataset."*

```csharp
// New write mode sibling for reads
public enum ProxyReadMode
{
    SingleNode,      // default — pick best node, execute once
    ScatterGather    // fan out to all live primary nodes, merge results
}

// New policy field
public ProxyReadMode ReadMode { get; init; } = ProxyReadMode.SingleNode;
```

`ProxyCluster.ExecuteScatterGatherAsync<T>` fans out to all primary nodes, collects
`IEnumerable<T>` results, and merges:

```csharp
private async Task<IEnumerable<T>> ExecuteScatterGatherAsync<T>(
    Func<IDataSource, IEnumerable<T>> operation,
    Func<IEnumerable<T>, IEnumerable<T>> merge = null)  // default: Concat + DistinctBy PK
```

Triggered automatically when `ReadMode == ScatterGather` and the operation returns a collection.

---

### From HAProxy — Slow-Start for New Nodes

> *"When a node is added or recovers, don't send it full traffic immediately.
> Ramp up gradually over `SlowStartDurationMs`."*

Protects a cold node (empty cache, no JIT warmup) from being overwhelmed.

```csharp
public int SlowStartDurationMs { get; init; } = 30_000;   // 0 = disabled
```

`NodeRouter.GetEffectiveWeight(node)` returns a capped weight during slow-start:

```
elapsed    = (UtcNow - node.AddedAtUtc).TotalMs
rampFactor = Math.Min(1.0, elapsed / SlowStartDurationMs)
effectiveWeight = (int)(node.Weight * rampFactor)   // starts near 0, reaches full weight over 30 s
```

---

### From Envoy — Outlier Detection (Automatic Backend Ejection)

> *"Automatically eject a backend from the load-balancing pool when its error rate or latency
> exceeds a threshold — without waiting for the circuit breaker."*

Complements the existing circuit breaker: circuit breaker reacts to cascading failures;
outlier detection proactively removes degraded nodes sooner.

```csharp
public class OutlierDetectionPolicy
{
    public bool   Enabled                   { get; init; } = false;
    public double ConsecutiveErrorThreshold { get; init; } = 5;      // eject after 5 errors
    public double ErrorRateThreshold        { get; init; } = 0.50;   // or 50 % error rate
    public int    IntervalMs                { get; init; } = 10_000; // evaluated every 10 s
    public int    BaseEjectionTimeMs        { get; init; } = 30_000; // first eject: 30 s
    public int    MaxEjectionTimeMs         { get; init; } = 300_000; // max: 5 min
    public double MaxEjectionPercent        { get; init; } = 0.50;   // never eject >50% of nodes
}

// In ProxyPolicy:
public OutlierDetectionPolicy OutlierDetection { get; init; } = new();
```

---

### From Chaos Engineering (Netflix Chaos Monkey) — Fault Injection

> *"Deliberately inject failures into the proxy for testing resilience without touching backends."*

Off by default.  Only activates when `FaultInjection.Enabled = true` (dev/test environments only).

```csharp
public class FaultInjectionPolicy
{
    public bool   Enabled          { get; init; } = false;
    public double ErrorRate        { get; init; } = 0.0;     // 0.05 = 5 % of calls throw
    public double DelayRate        { get; init; } = 0.0;     // 0.10 = 10 % of calls get delayed
    public int    DelayMs          { get; init; } = 500;
    public string TargetNodeId     { get; init; } = null;    // null = all nodes
    public string TargetEntity     { get; init; } = null;    // null = all entities
}
```

---

### From AWS Aurora / Galera — Replica Lag Awareness

> *"Don't route reads to a replica that is lagging more than N milliseconds behind the primary."*

```csharp
public int MaxReplicaLagMs { get; init; } = 5_000;   // 0 = no lag check
```

Each node exposes `IProxyNode.ReplicaLagMs` (polled by a background query: `SELECT lag_ms FROM
proxy_replication_status` or equivalent).  Nodes with `ReplicaLagMs > MaxReplicaLagMs` are
temporarily demoted to `Standby` for read routing.

---

### From Kubernetes / Istio — Traffic Splitting (Canary Deploys)

> *"Send X % of traffic to a NEW policy/node version while the rest stays on stable."*

Already partially sketched as a policy fan-out feature.  Full design:

```csharp
public class TrafficSplitRule
{
    public string TargetNodeId   { get; init; }   // the canary node
    public int    WeightPercent  { get; init; }   // e.g., 10 = send 10% here
    public string OperationScope { get; init; }   // null = all; "Read" = reads only
}

// In ProxyPolicy:
public IReadOnlyList<TrafficSplitRule> TrafficSplits { get; init; } = Array.Empty<TrafficSplitRule>();
```

`NodeRouter` incorporates traffic-split weights into its candidate selection before applying
the normal routing strategy.

---

### From Nginx / HAProxy — Rate Limiting per Node

> *"Cap the number of requests per second routed to a specific node."*

```csharp
public class NodeRateLimit
{
    public string NodeId        { get; init; }
    public int    MaxRps        { get; init; }   // max requests per second
    public RateLimitAction Action { get; init; } = RateLimitAction.RouteElsewhere;
}

public enum RateLimitAction
{
    RouteElsewhere,   // skip node and pick next best
    Queue,            // hold request in bounded queue
    Reject            // throw immediately
}

// In ProxyPolicy:
public IReadOnlyList<NodeRateLimit> NodeRateLimits { get; init; } = Array.Empty<NodeRateLimit>();
```

Implemented via a `SemaphoreSlim` or token-bucket per node inside `NodeRouter`.

---

### From Redis Cluster / DynamoDB — Virtual Slots / Consistent Hashing

The `ConsistentHash` routing strategy (already in the `ProxyNodeRoutingStrategy` enum) gets
a proper implementation:

```
Ring positions: each node gets K virtual slots (K = 150 by default — matches Redis Cluster)
Hash function: MurmurHash3(entityName + correlationId) → uint → ring position lookup
Benefit: when a node is added/removed, only 1/N of keys re-map
```

`ProxyCluster.RebalanceAffinity()` uses the same ring logic when entity affinity bins
are reassigned.

---

### From Pgpool-II / RDS Proxy — Connection Multiplexing

> *"Many application-level logical connections share a smaller pool of real backend connections."*

Today: each `ProxyDataSource` has its own connection pool.  At cluster level:

```
ProxyCluster manages a shared connection quota:
  TotalMaxBackendConnections = sum(node.MaxPoolSize × node.BackendCount)
  When quota is reached: queue new requests (ClusterMaxQueueDepth, ClusterQueueTimeoutMs)
```

New policy fields:

```csharp
public int ClusterMaxBackendConnections { get; init; } = 0;       // 0 = uncapped
public int ClusterMaxQueueDepth         { get; init; } = 1_000;
public int ClusterQueueTimeoutMs        { get; init; } = 5_000;
```

---

## Full Extended Goals Table

| ID | Goal | Source inspiration |
|----|------|--------------------|
| G1 | Horizontal proxy scaling | HAProxy, Envoy |
| G2 | Shared circuit state (no split-brain) | Vitess, Redis Cluster |
| G3 | Proxy-tier failover | HAProxy, Pgpool-II |
| G4 | Aggregate metrics | Envoy, Istio |
| G5 | Policy fan-out | Kubernetes ConfigMap rollout |
| G6 | Sticky session affinity | HAProxy sticky tables |
| G7 | Zero-downtime rolling restart | Kubernetes rolling deploy |
| **G8** | **Entity-affinity routing (partition leadership)** | **Kafka topic leadership, Vitess VSchema** |
| **G9** | **Request hedging** | **Envoy, Google gRPC** |
| **G10** | **Query routing rules** | **ProxySQL, MaxScale** |
| **G11** | **Scatter-gather reads** | **Vitess scatter queries** |
| **G12** | **Slow-start for new nodes** | **HAProxy slow-start** |
| **G13** | **Outlier detection** | **Envoy outlier_detection** |
| **G14** | **Fault injection** | **Netflix Chaos Monkey, Istio** |
| **G15** | **Replica lag awareness** | **AWS Aurora read replicas** |
| **G16** | **Traffic splitting / canary** | **Istio VirtualService, Argo Rollouts** |
| **G17** | **Per-node rate limiting** | **Nginx, HAProxy** |
| **G18** | **Consistent hash routing** | **Redis Cluster, DynamoDB** |
| **G19** | **Connection multiplexing quota** | **Pgpool-II, RDS Proxy** |

---

## Updated Phase Plan

| Sub-phase | Scope | Priority |
|-----------|-------|----------|
| 11.1 | `ProxyCluster` skeleton + `IProxyNode` + basic `LeastConnections` routing | P2 |
| 11.2 | Shared circuit state wiring + policy fan-out | P2 |
| 11.3 | Session affinity + drain + in-flight counter | P2 |
| 11.4 | Aggregate metrics + SLO | P2 |
| 11.5 | `RedisCircuitStateStore` | P3 |
| **11.6** | **Entity-affinity map + rebalancing (G8)** | **P2** |
| **11.7** | **Query routing rules (G10) + traffic splitting (G16)** | **P2** |
| **11.8** | **Request hedging (G9) + outlier detection (G13) + slow-start (G12)** | **P3** |
| **11.9** | **Scatter-gather reads (G11) + replica lag (G15)** | **P3** |
| **11.10** | **Fault injection (G14) + rate limiting (G17)** | **P3 (dev/test only)** |
| **11.11** | **Consistent hashing (G18) + connection multiplexing (G19)** | **P3** |

---

## Full New Type / File Summary

| New File | New Types |
|----------|-----------|
| `ProxyCluster.cs` | `ProxyCluster`, `IProxyCluster` |
| `ProxyCluster.NodeManagement.cs` | partial + `IProxyNode`, `ProxyNode` |
| `ProxyCluster.NodeProbing.cs` | partial + outlier detection, slow-start |
| `ProxyCluster.NodeRouting.cs` | partial + `INodeRouter`, all strategy impls, consistent hash ring |
| `ProxyCluster.EntityAffinity.cs` | partial + `EntityAffinityMap`, `ProxyRoutingRule` |
| `ProxyCluster.ScatterGather.cs` | partial — scatter-gather read fan-out |
| `ProxyCluster.Hedging.cs` | partial — request hedging |
| `ProxyCluster.TrafficControl.cs` | partial — rate limiting, traffic splits, fault injection |
| `ProxyClusterEvents.cs` | `NodeStatusEventArgs`, `ClusterPolicyChangedEventArgs`, `AffinityRebalancedEventArgs` |
| `RedisCircuitStateStore.cs` | `RedisCircuitStateStore : ICircuitStateStore` |
| Modify: `ProxyotherClasses.cs` | `ProxyNodeRoutingStrategy`, `ProxyReadMode`, `EntityAffinityFallback`, `NodeRateLimit`, `TrafficSplitRule`, `OutlierDetectionPolicy`, `FaultInjectionPolicy` + new `ProxyPolicy` fields |
| Modify: `IProxyDataSource.cs` | `IProxyCluster extends IProxyDataSource` |


