# Phase 03 - Entity Placement & Replication Modes

## Objective

Implement entity-level placement: `Routed` (one entity = one shard),
`Replicated` (entity duplicated across N shards), and `Broadcast` (read-any /
write-all reference table). This is the "split 500 tables to DB-A, 500 to DB-B"
and "duplicate the customers table to all shards" surface.

## Dependencies

- Phase 02 (`DistributionPlan`, `EntityPlacement`, `ShardCatalog`).

## Scope

- Implement `EntityPlacementMap` - the runtime, fast-lookup view of the plan
  that converts an entity name into a `PlacementResolution`.
- Implement `PlacementResolution` (which shards to target, in what mode, with
  what quorum).
- Implement `EntityPlacementResolver` for entity-name lookup with exact + prefix
  matching (reuses the algorithm from `Proxy/EntityAffinityMap.cs`).
- Wire `DistributedDataSource.Routing.cs` partial to delegate to the resolver
  and emit `OnShardSelected` for each call.

## Out of Scope

- Row-level partition (Phase 04 + 05).
- Actual read/write execution (Phase 06 / 07).

## Target Files

Under `Distributed/Placement/`:

- `EntityPlacementMap.cs` - thread-safe lookup built from a `DistributionPlan`.
- `EntityPlacementResolver.cs` - exact + prefix match (longest prefix wins).
- `PlacementResolution.cs` - record: `EntityName`, `Mode`, `TargetShardIds`,
  `WriteQuorum`, `ReplicationFactor`, `IsBroadcast`.
- `PlacementMatchKind.cs` - enum: Exact | Prefix | DefaultRoute | Unmapped.

Update partial:

- `DistributedDataSource.Routing.cs` - `ResolvePlacement(string entity)`,
  `ResolvePlacementForWrite(string entity)`, raises `OnShardSelected`.
- `DistributedDataSource.cs` (existing) - constructor builds `EntityPlacementMap`
  from the active plan and rebuilds it on `ApplyDistributionPlan`.

## Design Notes

- Mode semantics for entity-level (no row key):
  - `Routed`     -> exactly one shard. Fail (or fall back) if missing.
  - `Replicated` -> writes go to every shard in `TargetShardIds`; reads pick one
    via the underlying `IProxyCluster` policy. Quorum = `WriteQuorum`.
  - `Broadcast`  -> writes go to every shard in catalog (used for reference
    tables and DDL); reads pick any. Updates to the plan automatically include
    new shards in broadcast.
  - `Sharded`    -> deferred to Phase 04/05; this phase treats `Sharded` as
    "all shards listed" for the resolver layer.
- `Unmapped` policy is configurable in `DistributedDataSourceOptions`:
  `RejectUnmapped` (throw), `DefaultShardId` (route to a designated shard), or
  `BroadcastUnmapped` (all shards) - mirrors `EntityAffinityFallback`.

## Implementation Steps

1. Create `Distributed/Placement/` folder.
2. Implement `PlacementResolution` record + `PlacementMatchKind` enum.
3. Implement `EntityPlacementMap`: built from `DistributionPlan`, holds two
   `ConcurrentDictionary` (exact, prefix) plus a default rule.
4. Implement `EntityPlacementResolver` with the longest-prefix algorithm copied
   in spirit from `Proxy/EntityAffinityMap.Resolve`.
5. Add `Resolve(string entityName, bool isWrite)` returning a
   `PlacementResolution` and the `PlacementMatchKind`.
6. Update `DistributedDataSource.cs` ctor to build the map from the plan.
7. Add `DistributedDataSource.Routing.cs` partial exposing `ResolvePlacement`
   helpers and raising `OnShardSelected` (correlated by `ProxyExecutionContext`-
   style correlation id - reuse the `Proxy.ProxyExecutionContext` shape or
   introduce a tiny `DistributedExecutionContext`).
8. Add `OnPlacementViolation` raise when resolver returns `Unmapped` and policy
   is `RejectUnmapped`.

## TODO Checklist

- [x] `PlacementResolution.cs`, `PlacementMatchKind.cs` — both shipped under `Distributed/Placement/`. `PlacementMatchKind` adds a dedicated `Broadcast` value alongside `Exact`/`Prefix`/`DefaultRoute`/`Unmapped` so fallback expansions are distinguishable in diagnostics. `PlacementResolution` is fully immutable, surfaces `Source` (originating placement) and convenience flags (`IsBroadcast`, `IsUnmapped`).
- [x] `EntityPlacementMap.cs` (built from plan, rebuilt on `ApplyDistributionPlan`) — exact and prefix `ConcurrentDictionary` buckets, prefix patterns encoded by trailing `*`, longest-prefix-wins resolver. `Empty` singleton + `FromPlan` factory; `SourcePlan` retained for diagnostics.
- [x] `EntityPlacementResolver.cs` (exact + prefix + default + unmapped policy) — wraps the map, calls a `Func<IReadOnlyList<string>>` live-shard supplier so Broadcast resolutions auto-include late-added shards, filters dead shards from `Replicated`/`Sharded` placements, and maps the three `UnmappedEntityPolicy` values to dedicated `PlacementMatchKind`s.
- [x] `DistributedDataSource.Routing.cs` partial — exposes `PlacementResolver` / `PlacementMap` accessors, `ResolvePlacement` overloads, and the internal `RebuildPlacementResolver` helper. Emits one `OnShardSelected` per targeted shard (with correlation id from `DistributedExecutionContext`) and raises `OnPlacementViolation` for unmapped/empty/missing-default cases before throwing `InvalidOperationException`.
- [x] `DistributedExecutionContext.cs` (tiny per-call context with correlation id) — immutable POCO with `CorrelationId`, `OperationName`, `EntityName`, `IsWrite`, `StartedUtc`, and a derived `WithTag` builder. Auto-generated correlation ids via `New(...)`; explicit propagation via `FromCorrelation(...)`.
- [x] Unmapped policy enum added to `DistributedDataSourceOptions` — `UnmappedPolicy` (default `RejectUnmapped`) + `DefaultShardIdForUnmapped`. The resolver constructor validates that `DefaultShardId` policy ships with a non-empty default.

## Verification Criteria

- [x] Routed entity resolves to exactly one shard — `EntityPlacement` constructor enforces `Routed` + exactly one `ShardIds` entry; `EntityPlacementResolver.BuildResolutionFromPlacement` filters against the live catalog so a stale single-shard reference becomes a violation rather than a silent miss.
- [x] Replicated entity with `TargetShardIds = [s1, s2, s3]` returns all three in `PlacementResolution.TargetShardIds` — `BuildResolutionFromPlacement` returns the full intersection of placement shards and the live snapshot for non-Broadcast modes.
- [x] Broadcast entity returns every shard currently in the catalog — Broadcast branch ignores the persisted shard list and uses `SafeLiveShards()` (sourced from `DistributedDataSource.SnapshotLiveShardIds`) so late-added shards are picked up automatically.
- [x] Prefix mapping `Audit_*` -> `s1` resolves `Audit_Login` to `s1` — `EntityPlacementMap.FromPlan` strips the trailing `*` and stores `"Audit_"` in the prefix bucket; `Match` uses longest-prefix-wins lookup (`StringComparison.OrdinalIgnoreCase`).
- [x] Unmapped + `RejectUnmapped` policy throws and raises `OnPlacementViolation` — `DistributedDataSource.ResolvePlacement` calls `RaisePlacementViolation` (Error severity) with a "no placement matches…" reason and then throws `InvalidOperationException`. Default-route + missing fallback shard and zero-live-shard placements raise an additional violation event without crashing the resolver itself.

## Implementation Notes

- The placement map and resolver are rebuilt under `_planSwapLock` in both the constructor and `ApplyDistributionPlan`, so callers always observe a consistent (plan, map, resolver) triple.
- `EntityPlacementResolver` defensively swallows exceptions from the live-shard supplier so a misbehaving catalog cannot crash routing on the hot path; the empty list flows through as a "no live shards" violation.
- `OnShardSelected` is raised one event per resolved shard — Phase 06 / 07 executors can join on `(EntityName, CorrelationId)` to reconstruct the full fan-out plan from the event stream alone.
- All new files are partial-friendly and clean-code: one type per file, immutable records, value-style equality on the existing Phase 02 types, no narrating comments.

## Build / Lint

- `dotnet build DataManagementEngine.csproj` — **0 errors** across `net8.0`, `net9.0`, `net10.0`. Pre-existing CS1591/CA1416 warnings only (no new ones).
- `ReadLints` over the eight new/edited files — **No linter errors found**.

## Risks / Open Questions

- For Broadcast tables, late-added shards must be included automatically. The
  resolver should query `IShardCatalog.Snapshot()` per call rather than caching.
  Trade-off: ~O(shards) per resolve; acceptable since shard count is small.
