# Phase 02 - Distribution Plan & Shard Catalog

## Objective

Introduce the persistent, versioned topology: `Shard` (one per backend cluster),
`ShardCatalog` (the live inventory), and `DistributionPlan` (the per-entity
placement spec). All three persist via `IDMEEditor.ConfigEditor` using the same
pattern as `ProxyCluster.SaveNodesToConfig`.

## Dependencies

- Phase 01 (skeleton).
- Reuses `Proxy/ProxyCluster.cs` persistence patterns and `ConnectionProperties`.

## Scope

- Define `Shard` (immutable identity + reference to its `IProxyCluster`).
- Define `ShardCatalog` (thread-safe registry; add/remove/get; snapshot).
- Define `DistributionPlan`: per-entity `EntityPlacement` records (mode, shard
  set, partition function reference, replication factor, version).
- Persist via `ConfigEditor.DataConnections` with `DriverName =
  "BeepDistributedShard"` and `DriverName = "BeepDistributionPlan"`.
- Provide load/save methods analogous to `ProxyCluster.LoadNodesFromConfig`.

## Out of Scope

- Partition function implementations (Phase 04).
- Routing logic (Phase 05).
- Resharding mutations (Phase 11).

## Target Files

Under `Distributed/Catalog/`:

- `Shard.cs` - `ShardId`, `IProxyCluster Cluster`, `Weight`, `Tags`.
- `IShardCatalog.cs` - read/write contract.
- `ShardCatalog.cs` - thread-safe `ConcurrentDictionary<string, Shard>` impl.
- `ShardCatalog.Persistence.cs` - partial; `SaveToConfig`, `LoadFromConfig`.

Under `Distributed/Plan/`:

- `DistributionPlan.cs` - top-level plan with `Version`, `EntityPlacements`.
- `EntityPlacement.cs` - per-entity record: `EntityName`, `DistributionMode`,
  `ShardIds`, `PartitionFunctionRef`, `ReplicationFactor`, `WriteQuorum`.
- `PartitionFunctionRef.cs` - `Kind` (Hash/Range/List/Composite), `KeyColumns`,
  `Parameters` (string-keyed bag).
- `IDistributionPlanStore.cs` - load/save contract.
- `DistributionPlanStore.cs` - implementation against `IDMEEditor.ConfigEditor`.
- `DistributionPlanBuilder.cs` - fluent builder for tests / setup code.

Update partial:

- `DistributedDataSource.Plan.cs` - `ApplyDistributionPlan`, `GetCurrentPlan`,
  emits `OnPlacementViolation` when a referenced shard is missing from catalog.

## Design Notes

- `EntityPlacement` is a value record. Equality matters for plan diffs in
  Phase 11 resharding.
- Persistence schema (in `ConnectionProperties.ParameterList`):
  - For shards: `DistributionName`, `ShardId`, `ClusterName`, `Weight`, `Tags`.
  - For plans: `DistributionName`, `Version`, `EntityName`, `Mode`, `ShardIds`
    (CSV), `PartitionKind`, `KeyColumns` (CSV), `ReplicationFactor`,
    `WriteQuorum`, `Params` (JSON).
- Plans are immutable snapshots; mutations produce a new `DistributionPlan`
  with `Version = previous.Version + 1`. Phase 11 uses version comparison to
  drive dual-write windows.

## Implementation Steps

1. Create `Distributed/Catalog/` and `Distributed/Plan/` folders.
2. Implement `Shard` and `IShardCatalog` + `ShardCatalog` (concurrent, snapshot
   returns `IReadOnlyList<Shard>`).
3. Implement `ShardCatalog.Persistence.cs` mirroring
   `ProxyCluster.SaveNodesToConfig` but with `DriverName = "BeepDistributedShard"`.
4. Implement `EntityPlacement`, `PartitionFunctionRef`, `DistributionPlan`
   (with `WithEntity(...)` returning a new plan instance).
5. Implement `DistributionPlanStore` (round-trip JSON for `Params`).
6. Implement `DistributionPlanBuilder` exposing `RouteEntity`, `ShardEntity`,
   `ReplicateEntity`, `BroadcastEntity`, `Build()`.
7. Add `DistributedDataSource.Plan.cs` partial: stores the active plan, validates
   against the current catalog (every referenced shard must exist), and raises
   `OnPlacementViolation` for missing shards.
8. Add unit-style smoke test (in DevEx phase) for save/load round trip.

## TODO Checklist

- [x] `Shard.cs`, `IShardCatalog.cs`, `ShardCatalog.cs` (Catalog/ folder,
      `OrdinalIgnoreCase` keying, snapshot returns ordered list).
- [x] `ShardCatalog.Persistence.cs` (Save / Load) — `DriverName =
      "BeepDistributedShard"`, mirrors `ProxyCluster.SaveNodesToConfig` /
      `LoadLocalNodesFromConfig`. Tags persisted as escaped CSV (`k=v;k=v`).
- [x] `DistributionPlan.cs` (Plan/ folder, replaces Phase 01 stub),
      `EntityPlacement.cs`, `PartitionFunctionRef.cs` (+ `PartitionKind` enum).
- [x] `IDistributionPlanStore.cs`, `DistributionPlanStore.cs` —
      `DriverName = "BeepDistributionPlan"`, one record per
      `(plan, entity)`, `Params` round-tripped as JSON via `System.Text.Json`.
- [x] `DistributionPlanBuilder.cs` (`RouteEntity`/`ShardEntity`/
      `ReplicateEntity`/`BroadcastEntity`/`Place`, `From(plan)` derivation,
      `Build()` increments version).
- [x] `DistributedDataSource.Plan.cs` partial (moves `ApplyDistributionPlan`
      out of root partial, adds `GetCurrentPlan()` + richer per-placement
      validation, raises `OnPlacementViolation` with severity).
- [x] Catalog and plan persist/load through `ConfigEditor` (existing-record
      detection by `ConnectionName`; orphan placements removed on `Save`).
- [x] Plan validation rejects unknown shard IDs with `OnPlacementViolation`
      (plus mode-specific checks: Routed → exactly one shard, Sharded →
      partition function present, Replicated → RF ≤ shard count, quorum sane).

## Verification Criteria

- [x] Round-trip schema parity: every persisted record carries
      `DistributionName`, `Version`, `EntityName`, `Mode`, `ShardIds`,
      `PartitionKind`, `KeyColumns`, `ReplicationFactor`, `WriteQuorum`, `Params`.
      Loader reconstructs via `EntityPlacement` ctor; malformed records are
      skipped (logged) rather than failing the whole load.
- [x] Plan equality is value-style: same name + version + per-entity
      placement set compares equal (`DistributionPlan.Equals`,
      `EntityPlacement.Equals`, `PartitionFunctionRef.Equals` all implemented).
- [x] Plans are versioned monotonically: `DistributionPlan.WithEntity` /
      `WithoutEntity` and `DistributionPlanBuilder.Build` both bump `Version`.
- [x] Removing a shard from the catalog while a plan still references it
      raises a `PlacementViolationEventArgs` on next `ApplyDistributionPlan`
      (plan-level "unknown shard" violation + placement-level "no live
      shards" violation when no resolvable shards remain).
- [x] Build clean across `net8.0`, `net9.0`, `net10.0` with zero new lints
      in `Distributed/Catalog` and `Distributed/Plan`.

## Risks / Open Questions

- Should plan storage be a separate file under `BeepDataPath` instead of
  `ConfigEditor.DataConnections`? Decision: keep in `DataConnections` for v1
  (consistent with ProxyCluster). Revisit in Phase 13 if file-per-plan is
  needed for change tracking.
