# Phase 05 - Shard Router & Key Extraction

## Objective

Combine the entity-placement resolver (Phase 03) with the partition functions
(Phase 04) into a single `ShardRouter` that turns any incoming `IDataSource`
call into a concrete `RoutingDecision`. Extract partition keys from filters,
PK lookups, and entity instances.

## Dependencies

- Phase 03 (`PlacementResolution`).
- Phase 04 (`IPartitionFunction`, `PartitionInput`).

## Scope

- `ShardRouter` orchestrator: resolves entity, then if `Sharded` mode applies
  the partition function using the extracted key.
- `RoutingDecision` record: target shards, write quorum, fan-out flag,
  partition key value (or null if scatter required).
- Key extraction from:
  - `List<AppFilter>` filters - look for filter where `FieldName` is in the
    plan's `KeyColumns` and operator is `=` or `IN`.
  - PK methods (`GetEntity(string entity, object[] keys)`) - positional match
    against `EntityStructure.PrimaryKey`.
  - Entity instance writes (`InsertEntity`, `UpdateEntity`, `DeleteEntity`) -
    reflection / `IDictionary<string,object>` lookup of key-column values.
- Scatter decision when no key value is supplied for a `Sharded` entity (read
  -> all shards; write -> error unless `AllowScatterWrite` is set).
- Pluggable `IShardRoutingHook` for custom override (forced shard, debug pin).

## Out of Scope

- Actual execution / scatter-merge (Phase 06).
- Cross-shard query planning (Phase 08).

## Target Files

Under `Distributed/Routing/`:

- `IShardRouter.cs`.
- `ShardRouter.cs` (partial root).
- `ShardRouter.KeyExtraction.cs` (partial; per-call-shape extractors).
- `RoutingDecision.cs` (record).
- `IShardRoutingHook.cs` + `NullShardRoutingHook.cs`.
- `ShardRoutingException.cs`.

Update partial:

- `DistributedDataSource.Routing.cs` (existing) - replace direct
  `EntityPlacementMap` calls with `ShardRouter` calls.

## Design Notes

- `ShardRouter` is constructed once per `DistributionPlan` version. Phase 11
  swaps it atomically when a plan changes.
- Key extraction is best-effort and explicit: if the call-site supplies no key
  for a `Sharded` entity, the router returns a `RoutingDecision` with
  `IsScatter = true` and an empty key, then the executor (Phase 06/07) decides.
- For `IN (a,b,c)` filters on the partition key, the router collapses to the
  union of shards; only those shards are targeted.
- Write quorum is taken from `EntityPlacement.WriteQuorum` for Replicated mode;
  for Sharded writes the quorum is per-shard (delegated to per-shard
  `IProxyCluster` `WriteMode`).

## Implementation Steps

1. Create `Distributed/Routing/` folder.
2. Implement `RoutingDecision` record (`ShardIds`, `IsScatter`, `IsFanOut`,
   `WriteQuorum`, `MatchKind`, `KeyValue`).
3. Implement `IShardRouter` and `ShardRouter` calling `EntityPlacementMap`.
4. Implement `ShardRouter.KeyExtraction.cs` with:
   - `TryExtractFromFilters(List<AppFilter>, IReadOnlyList<string> keyCols, out object key)`.
   - `TryExtractFromPositionalKeys(object[] keys, EntityStructure structure, IReadOnlyList<string> keyCols, out object key)`.
   - `TryExtractFromEntityInstance(object record, IReadOnlyList<string> keyCols, out object key)`.
5. For a `Sharded` placement: call the plan's `IPartitionFunction` with the
   extracted key; if no key, return scatter decision.
6. Implement `IShardRoutingHook` interface so tests can pin a request to a
   specific shard (used in DevEx phase).
7. Update `DistributedDataSource.Routing.cs` to call `ShardRouter` everywhere.

## TODO Checklist

- [x] `RoutingDecision.cs` (immutable record).
- [x] `IShardRouter.cs`, `ShardRouter.cs`, `ShardRouter.KeyExtraction.cs`
      (partition-function cache keyed by `EntityPlacement`).
- [x] `IShardRoutingHook.cs`, `NullShardRoutingHook.cs`,
      `ShardRoutingHookContext`.
- [x] `ShardRoutingException.cs` (serializable, carries `EntityName` and
      `Reason`).
- [x] `DistributedDataSource.Routing.cs` updated to delegate to router and
      emit `OnShardSelected` per chosen shard (see `Router` accessor,
      `SetRoutingHook`, `RebuildShardRouter`, four `Route*` convenience
      methods).
- [x] Key extractor handles `=`, `IN`, and positional PK arrays
      (type-safe coercion against `EntityStructure.Fields`, cached POCO
      reflection accessors, `IDictionary<string, object>` /
      non-generic `IDictionary` passthrough).

## Verification Criteria

- [x] Sharded entity + filter on partition key resolves to a single shard
      through `BuildShardedDecision`.
- [x] Sharded entity + `IN` filter collapses to the union of relevant
      shards through `TryGetMultiValues` + per-value dispatch.
- [x] Sharded entity + no partition-key filter returns
      `RoutingDecision { IsScatter = true }` with all live shards for
      reads; writes throw `ShardRoutingException` unless
      `DistributedDataSourceOptions.AllowScatterWrite = true`.
- [x] `IShardRoutingHook` override can rewrite the baseline decision
      (hook errors wrapped in `ShardRoutingException`).
- [x] Replicated / Broadcast modes return `IsFanOut = true` and carry the
      placement's `WriteQuorum` / `ReplicationFactor`.
- [x] `dotnet build DataManagementEngine.csproj` — 0 errors / 0 warnings
      (net8/9/10).

## Risks / Open Questions

- Reflection-based key extraction must support the existing `Beep` patterns
  (`IDictionary<string,object>`, anonymous objects, plain POCOs). Provide a
  cached `PropertyAccessor` to avoid reflection cost per call.
