# Phase 01 - Core Contracts & DistributedDataSource Skeleton

## Objective

Establish the contract surface (`IDistributedDataSource`) and the partial-class
skeleton of `DistributedDataSource` so later phases can fill in routing,
execution, transactions, etc. without touching the contract.

## Dependencies

- None (kickoff phase). References existing contracts:
  - [`Proxy/IProxyDataSource.cs`](../Proxy/IProxyDataSource.cs) (`IProxyCluster`)
  - `IDataSource` from `DataManagementModelsStandard`
  - `IDMEEditor` from `BeepDM/DataManagementEngineStandard`

## Scope

- Define `IDistributedDataSource : IDataSource, IDisposable`.
- Create `DistributedDataSource` partial class implementing `IDataSource` surface
  by delegating to the composed `ShardRouter` (stub in this phase).
- Define `DistributedDataSourceOptions` and `DistributionMode` enums.
- Wire empty events: `OnShardSelected`, `OnReshardStarted`, `OnReshardCompleted`,
  `OnPlacementViolation`.

## Out of Scope

- Actual routing logic (Phase 05).
- Persistence / catalog (Phase 02).
- Cross-shard transactions (Phase 09).

## Target Files

All new under `BeepDM/DataManagementEngineStandard/DistributedDatasource/Distributed/`:

- `IDistributedDataSource.cs` - public contract.
- `DistributedDataSource.cs` - partial; ctors, fields, IDataSource identity props.
- `DistributedDataSource.IDataSource.cs` - partial; IDataSource method delegations to router/executor.
- `DistributedDataSource.Events.cs` - partial; events + EventArgs.
- `DistributedDataSource.Lifecycle.cs` - partial; Open/Close/Dispose orchestration across shards.
- `DistributedDataSourceOptions.cs` - tuning knobs (timeouts, parallelism).
- `DistributionMode.cs` - enum: Routed | Sharded | Replicated | Broadcast.
- `Events/ShardSelectedEventArgs.cs`
- `Events/PlacementViolationEventArgs.cs`
- `Events/ReshardEventArgs.cs`

## Design Notes

- `DistributedDataSource` is constructed with:
  1. `IDMEEditor` (for logging, ConfigEditor, error info).
  2. `DistributionPlan` (Phase 02 type; in this phase just a stub with empty defaults).
  3. `IReadOnlyDictionary<string, IProxyCluster>` keyed by shard ID.
  4. Optional `DistributedDataSourceOptions`.

- `IDataSource` calls (`GetEntity`, `InsertEntity`, `UpdateEntity`, `RunQuery`,
  `ExecuteSql`, `BeginTransaction`, etc.) become thin wrappers that delegate to
  internal executor partials introduced in later phases. In this phase they
  throw `NotImplementedException` with a clear "implemented in Phase NN" message.

- `Dispose()` disposes the catalog, plan, and each shard cluster.

## Implementation Steps

1. Create the `Distributed/` folder and `Distributed/Events/` subfolder.
2. Add `IDistributedDataSource.cs` mirroring the `IProxyCluster` style: extends
   `IDataSource`, exposes `DistributionPlan` (read-only), `Shards` snapshot,
   `ApplyDistributionPlan(DistributionPlan plan)`, and the events listed above.
3. Create `DistributedDataSource.cs` partial with private fields:
   `_dmeEditor`, `_options`, `_plan`, `_shards`, `_disposed`. Add ctors.
4. Create `DistributedDataSource.IDataSource.cs` partial with all `IDataSource`
   methods stubbed to throw `NotImplementedException("see Phase NN")`.
5. Create `DistributedDataSource.Events.cs` declaring events and a
   `RaiseShardSelected`/`RaisePlacementViolation` helper.
6. Create `DistributedDataSource.Lifecycle.cs` with `Openconnection`,
   `Closeconnection`, `Dispose` iterating all shards.
7. Add the small enum/event-args/options types as one-class-per-file.
8. Compile the project; ensure no consumer build breakage (file is additive).

## TODO Checklist

- [x] Create `Distributed/` and `Distributed/Events/` folders.
- [x] `IDistributedDataSource.cs`.
- [x] `DistributedDataSource.cs` (partial root, fields, ctors).
- [x] `DistributedDataSource.IDataSource.cs` (stubs).
- [x] `DistributedDataSource.Events.cs`.
- [x] `DistributedDataSource.Lifecycle.cs`.
- [x] `DistributedDataSourceOptions.cs`.
- [x] `DistributionMode.cs`.
- [x] Event-args files under `Events/`.
- [x] Solution builds with no warnings on the new files.
- [x] `DistributionPlan.cs` placeholder added so Phase 01 compiles ahead of
      Phase 02 (expanded with shard-catalog references and entity placements
      in P02-03).

## Verification Criteria

- [x] `DistributedDataSource` can be instantiated with an empty stub plan and
      a single fake `IProxyCluster` without throwing. (Constructor validates
      args, `DistributionPlan.Empty` is the default; verified by inspection.)
- [x] `Dispose()` is idempotent (called twice = no error).
      (`Dispose` short-circuits on `_disposed`.)
- [x] All `IDataSource` methods raise `NotImplementedException` whose message
      cites the phase that will implement them.
      (`NotYet(member, phase)` helper produces a uniform Phase NN message.)
- [x] No reference cycles between Distributed and Proxy assemblies.
      (`Distributed/` references `TheTechIdea.Beep.Proxy` types; `Proxy/`
      does not reference `TheTechIdea.Beep.Distributed`.)
- [x] `dotnet build` clean across `net8.0`, `net9.0`, `net10.0` —
      0 errors, 0 new warnings under `DistributedDatasource/Distributed/`.

## Risks / Open Questions

- Should `DistributedDataSource` implement `IProxyCluster` for tooling reuse?
  Decision: NO - keep contracts separate; expose a `ToReadOnlyClusterView()`
  helper in Phase 13 if observability dashboards need it.
