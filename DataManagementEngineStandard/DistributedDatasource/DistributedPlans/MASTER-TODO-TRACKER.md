# MASTER TODO TRACKER - DistributedDataSource

Single source of truth for every actionable TODO across all phases. Each
checkbox mirrors a TODO in the per-phase doc; check both when complete.

Status legend:
- [ ] not started
- [~] in progress
- [x] done

## Phase 00 - Overview & Gap Matrix

- [x] Document gap matrix and reuse-vs-new inventory.
      Doc: [00-overview-distributeddatasource-gap-matrix.md](./00-overview-distributeddatasource-gap-matrix.md)

## Phase 01 - Core Contracts & Skeleton

Doc: [01-phase1-core-contracts-and-skeleton.md](./01-phase1-core-contracts-and-skeleton.md)

- [x] P01-01 Create `Distributed/` and `Distributed/Events/` folders.
- [x] P01-02 `IDistributedDataSource.cs`.
- [x] P01-03 `DistributedDataSource.cs` (partial root, fields, ctors).
- [x] P01-04 `DistributedDataSource.IDataSource.cs` (stubs).
- [x] P01-05 `DistributedDataSource.Events.cs`.
- [x] P01-06 `DistributedDataSource.Lifecycle.cs`.
- [x] P01-07 `DistributedDataSourceOptions.cs`.
- [x] P01-08 `DistributionMode.cs` enum.
- [x] P01-09 EventArgs files under `Events/`.
- [x] P01-10 Solution builds clean.
- [x] P01-11 `DistributionPlan.cs` Phase 02 stub (kept minimal so Phase 01 compiles ahead of Phase 02; expanded in P02-03).

## Phase 02 - Distribution Plan & Shard Catalog

Doc: [02-phase2-distribution-plan-and-shard-catalog.md](./02-phase2-distribution-plan-and-shard-catalog.md)

- [x] P02-01 `Shard.cs`, `IShardCatalog.cs`, `ShardCatalog.cs`.
- [x] P02-02 `ShardCatalog.Persistence.cs` (Save / Load) using
              `DriverName = "BeepDistributedShard"`.
- [x] P02-03 `DistributionPlan.cs` (replaces Phase 01 stub),
              `EntityPlacement.cs`, `PartitionFunctionRef.cs` + `PartitionKind` enum.
- [x] P02-04 `IDistributionPlanStore.cs`, `DistributionPlanStore.cs`
              using `DriverName = "BeepDistributionPlan"` and JSON-encoded `Params`.
- [x] P02-05 `DistributionPlanBuilder.cs` (Route/Shard/Replicate/Broadcast,
              `From(plan)` derivation, version auto-bump on `Build()`).
- [x] P02-06 `DistributedDataSource.Plan.cs` partial (moves
              `ApplyDistributionPlan`, adds richer per-placement validation,
              exposes `GetCurrentPlan`).
- [x] P02-07 Catalog and plan persist/load through `ConfigEditor`
              (mirrors `ProxyCluster.SaveNodesToConfig` pattern).
- [x] P02-08 Plan validation rejects unknown shard IDs with
              `OnPlacementViolation` (plus per-placement mode/quorum checks).

## Phase 03 - Entity Placement & Replication Modes

Doc: [03-phase3-entity-placement-and-replication-modes.md](./03-phase3-entity-placement-and-replication-modes.md)

- [x] P03-01 `PlacementResolution.cs`, `PlacementMatchKind.cs` (added dedicated `Broadcast` match kind).
- [x] P03-02 `EntityPlacementMap.cs` (exact + prefix buckets, prefix encoded by trailing `*`).
- [x] P03-03 `EntityPlacementResolver.cs` (exact + prefix + default + unmapped, live-shard supplier for Broadcast).
- [x] P03-04 `DistributedDataSource.Routing.cs` partial (`PlacementResolver`/`PlacementMap` accessors, `ResolvePlacement[ForWrite]`, per-shard `OnShardSelected`, violation events for unmapped/empty/missing-default).
- [x] P03-05 `DistributedExecutionContext.cs` (immutable correlation context with `WithTag` builder).
- [x] P03-06 `UnmappedEntityPolicy.cs` enum + `DistributedDataSourceOptions.UnmappedPolicy` / `DefaultShardIdForUnmapped` wired through ctor + `ApplyDistributionPlan`.

## Phase 04 - Partition Functions (Row-Level)

Doc: [04-phase4-partition-functions-row-level.md](./04-phase4-partition-functions-row-level.md)

- [x] P04-01 `MurmurHash3Helper.cs` (extracted from Proxy ConsistentHashRouter; bit-for-bit identical, public `Hash(string|byte[], seed)` API).
- [x] P04-02 `IPartitionFunction.cs`, `PartitionInput.cs` (immutable input, case-insensitive `GetValue`, multi-shard friendly contract).
- [x] P04-03 `HashPartitionFunction.cs` (virtual-slot ring, default 150 slots, binary-search clockwise lookup, multi-column key joining via `\u001f`).
- [x] P04-04 `RangePartitionFunction.cs` (sorted half-open boundaries, optional open-ended terminal segment, `PartitionKeyCoercer`-driven comparisons).
- [x] P04-05 `ListPartitionFunction.cs` (explicit map + optional `DefaultShardId`, `PartitionKeyCoercer.AreEqual` lookups).
- [x] P04-06 `CompositePartitionFunction.cs` (deduplicated union of inner outputs, deduplicated union of inner key columns).
- [x] P04-07 `PartitionFunctionFactory.cs` (kind-switch factory; JSON parameter shapes for Range/List/Composite; `JsonElement` normaliser keeps coercer behaviour consistent).
- [x] P04-08 `PartitionKeyCoercer.cs` (invariant-culture `Stringify`, decimal/DateTime/Guid coercion, ordinal-IgnoreCase string fallback; never throws).
- [x] P04-09 `Proxy/ProxyCluster.NodeRouting.cs` now delegates to `MurmurHash3Helper.Hash` (no behavior change; duplicate 60-line impl removed).
- [x] P04-V Verification — `dotnet build DataManagementEngine.csproj` clean (0 errors / `net8.0`+`net9.0`+`net10.0`); `ReadLints` clean over all 9 new files + edited Proxy file.

## Phase 05 - Shard Router & Key Extraction

Doc: [05-phase5-shard-router-and-key-extraction.md](./05-phase5-shard-router-and-key-extraction.md)

- [x] P05-01 `RoutingDecision.cs` (immutable record; `EntityName`, `Mode`,
              `MatchKind`, `ShardIds`, `IsWrite`, `IsScatter`, `IsFanOut`,
              `WriteQuorum`, `ReplicationFactor`, `KeyValues`,
              `HookOverridden`, `Source`).
- [x] P05-02 `IShardRouter.cs`, `ShardRouter.cs`,
              `ShardRouter.KeyExtraction.cs` (filters / positional keys /
              entity instance; cached `EntityPlacement` -> `IPartitionFunction`
              dictionary; scatter read support; scatter write gated by
              `AllowScatterWrite`).
- [x] P05-03 `IShardRoutingHook.cs`, `NullShardRoutingHook.cs`,
              `ShardRoutingHookContext` (immutable context with
              `EntityName`, `IsWrite`, `KeyValues`,
              `DistributedExecutionContext`).
- [x] P05-04 `ShardRoutingException.cs` (serializable, carries
              `EntityName` + `Reason`; thrown from routing only).
- [x] P05-05 `DistributedDataSource.Routing.cs` delegates to router
              (`Router` accessor, `SetRoutingHook`, `RebuildShardRouter`,
              four `Route*` convenience methods that emit `OnShardSelected`).
- [x] P05-06 Key extractor handles `=`, `IN`, and positional PK arrays
              (type-safe coercion against `EntityStructure.Fields`,
              cached per-type POCO reflection accessors,
              `IDictionary<string,object>` / non-generic `IDictionary`
              passthrough).
- [x] P05-V Verification — `dotnet build DataManagementEngine.csproj`
              clean (0 errors / 0 warnings, net8/9/10).

## Phase 06 - Read Execution

Doc: [06-phase6-read-execution-single-and-scatter.md](./06-phase6-read-execution-single-and-scatter.md)

- [x] P06-01 `DistributedExecutionContext.cs` finalized — Phase 03
              shape reused (CorrelationId, OperationName, EntityName,
              IsWrite, Tags); deadlines flow through a per-call linked
              `CancellationTokenSource` inside the executor, not through
              context state.
- [x] P06-02 `IDistributedReadExecutor.cs` + `IShardInvoker.cs`, plus
              `DistributedReadExecutor` partials (root, `SingleShard`,
              `Scatter`, `Replicated`) under
              `Distributed/Execution/`. Executor wired into
              `DistributedDataSource` via a nested `ShardInvokerAdapter`.
- [x] P06-03 `IResultMerger.cs` + `BasicResultMerger.cs` (concat
              rows, sum scalars, sum + concat paged; singleton
              `BasicResultMerger.Instance`).
- [x] P06-04 `DistributedDataSource.Reads.cs` partial wiring
              `GetEntity`, `GetEntity(paged)`, `GetEntityAsync`,
              `RunQuery`, `GetScalar`, `GetScalarAsync` through
              routing + the read executor with the
              single-shard/scatter/replicated dispatch helpers.
- [x] P06-05 Options updates: `ScatterFailurePolicy` (BestEffort /
              FailFast / RequireAll, default BestEffort),
              `ReplicatedReadPolicy` (First / Random, default First).
              `MaxScatterParallelism` and `DefaultReadDeadlineMs`
              already existed from Phase 01 and are now honoured by
              the scatter partial's `SemaphoreSlim` + linked
              `CancellationTokenSource`.
- [x] P06-06 Read methods no longer throw `NotImplementedException`.
              Only `ExecuteSql` remains as a Phase 07 stub (it is a
              write-shape and belongs to the write executor).
- [x] P06-V  Verification — `dotnet build DataManagementEngine.csproj`
              clean (0 errors, only pre-existing warnings across
              net8/9/10).

## Phase 07 - Write Execution

Doc: [07-phase7-write-execution-sharded-replicated-broadcast.md](./07-phase7-write-execution-sharded-replicated-broadcast.md)

- [x] P07-01 `DistributedWriteOptions.cs` — per-call knobs (scatter
              opt-in, quorum override, AtLeastN, correlation id,
              ExecuteSql entity hint).
- [x] P07-02 `WriteOutcome.cs`, `WriteFanOutResult.cs`, `QuorumPolicy.cs`
              (All | Majority | AtLeastN).
- [x] P07-03 `IDistributedWriteExecutor.cs` +
              `DistributedWriteExecutor.cs` (root),
              `.SingleShard.cs`, `.FanOut.cs`, `.ScatterDelete.cs`
              partials with bounded parallelism on
              `MaxFanOutParallelism`.
- [x] P07-04 `DistributedDataSource.Writes.cs` partial implements
              `InsertEntity`, `UpdateEntity`, `UpdateEntities`,
              `DeleteEntity`, and `ExecuteSql`; stubs removed from
              `DistributedDataSource.IDataSource.cs`. `ExecuteSql`
              currently broadcasts under `QuorumPolicy.All` until the
              Phase 08 SQL parser lands.
- [x] P07-05 `OnPartialReplicationFailure` event added on
              `IDistributedDataSource` and wired in
              `DistributedDataSource.Events.cs`; raised by
              `OutcomeToErrors` whenever a fan-out quorum was met but
              at least one replica diverged.
- [x] P07-06 `MaxFanOutParallelism` (already present on
              `DistributedDataSourceOptions` from Phase 06) is now
              consumed by `DistributedWriteExecutor.FanOut.RunParallelLegs`;
              `AllowScatterWrite` similarly consumed by the scatter
              path.
- [x] P07-V  Verification — `dotnet build DataManagementEngine.csproj`
              clean (0 errors; no Phase 07 warnings).

## Phase 08 - Cross-Shard Query Planner & Merger

Doc: [08-phase8-cross-shard-query-planner-and-merger.md](./08-phase8-cross-shard-query-planner-and-merger.md)

- [x] P08-01 Query core types — added `MergeOperation.cs`,
              `AggregateKind.cs`, `OrderDirection.cs`, `AggregateSpec.cs`,
              `OrderBySpec.cs`, `QueryIntent.cs`, `MergeSpec.cs` (with
              `PartialAggregate` AVG-pair descriptor), and
              `QueryPlan.cs` under
              `Distributed/Query/`. All types are immutable snapshots
              suitable for plan-cache reuse.
- [x] P08-02 `IQueryPlanner.cs` + stateless `QueryPlanner.cs` — pushes
              filters/columns/order/top per shard, splits AVG into
              tagged SUM/COUNT pair, and chooses Union / TopN /
              SortMerge / GroupAggregate merge shapes. Single-shard
              decisions short-circuit to a trivial pass-through plan.
- [x] P08-03 `IQueryAwareResultMerger.cs` + `QueryAwareResultMerger.cs`
              (root) + `.Sorting.cs` (TopN/SortMerge collect+sort with
              deterministic shard-index tie-breaks + offset/limit) +
              `.Grouping.cs` (hash group-by, per-group
              `AggregateAccumulator`, AVG re-pair, post-aggregate
              order/top). Implements `IResultMerger` via a delegated
              `BasicResultMerger` so Phase 06 union semantics are
              preserved when no plan is present.
- [x] P08-04 `AggregateAccumulator.cs` — commutative/associative
              folder for Count/Sum/Min/Max with decimal→double upgrade
              on float input, ANSI SQL empty-aggregate semantics, and
              `DivideAverage` helper used by the merger's AVG rebuild.
              Also adds `RowValueExtractor.cs` so grouping/sorting can
              read values out of dictionary rows, `DataRow`s, and
              reflected POCOs uniformly.
- [x] P08-05 `BroadcastJoinRewriter.cs` — classifies sibling-entity
              joins via `EntityPlacementResolver` and returns a
              `BroadcastJoinDecision` (`NotApplicable` / `LocalJoin` /
              `RequiresDistributedJoin`). Rebuilt inside
              `RebuildPlacementResolver` so it always tracks the live
              plan.
- [x] P08-06 Kept `DistributedDataSource.Reads.cs` untouched for
              backward compatibility; new integration partial
              `DistributedDataSource.Query.cs` exposes `PlanQuery`,
              `ExecuteQueryIntent` (default shard executor over
              `IProxyCluster.GetEntity` plus a delegate overload for
              SQL-rendering callers), and `ExecutePlan`. The root
              `DistributedDataSource` constructor now installs
              `QueryAwareResultMerger` as the default merger so the
              Phase 06 IDataSource surface keeps union behaviour while
              richer intents flow through the planner. Both
              `_queryPlanner` and `_queryMerger` are hot-swappable via
              public properties.
- [x] P08-V  Verification — `dotnet build DataManagementEngine.csproj`
              clean (0 errors; no Phase 08 warnings).

## Phase 09 - Distributed Transactions

Doc: [09-phase9-distributed-transactions.md](./09-phase9-distributed-transactions.md)

- [x] P09-01 Added `SupportsTwoPhaseCommit` as a default interface
              member on `IProxyCluster` (`=> false`). Concrete clusters
              opt in by overriding once every node exposes a
              2PC-capable `IDataSource`; existing implementations pick
              up the safe default without changes.
- [x] P09-02 `IDistributedTransactionCoordinator.cs` plus coordinator
              split across four partials under
              `Distributed/Transactions/`:
              `DistributedTransactionCoordinator.cs` (root — ctor,
              scope registry, shared helpers), `.SingleShard.cs`,
              `.TwoPhaseCommit.cs`, `.Saga.cs` (each single-responsibility
              and well under 300 lines).
- [x] P09-03 `DistributedTransactionScope.cs` (immutable correlation
              token — id, strategy, shard set, status,
              saga-step history, reason log, opened/closed timestamps,
              thread-safe status transitions) +
              `TransactionStrategy.cs` (`SingleShardFastPath`,
              `TwoPhaseCommit`, `Saga`) +
              `DistributedTransactionStatus.cs` lifecycle enum.
- [x] P09-04 `IDistributedTransactionLog.cs` (append/read/close/list),
              `TransactionLogEntry.cs` + `TransactionLogKind.cs`
              (Begin/PrepareSent/PrepareAck/PrepareNack/GlobalCommit/
              GlobalAbort/CommitAck/CommitFailed/RollbackAck/
              RollbackFailed/SagaForwardAck/SagaForwardFailed/
              SagaCompensationAck/SagaCompensationFailed/InDoubt/Closed),
              and `InMemoryTransactionLog.cs` v1 backed by a
              `ConcurrentDictionary<string, List<TransactionLogEntry>>`.
              Durable/file-backed log is deferred to Phase 13
              (observability + durability track).
- [x] P09-05 `SagaStep.cs` — immutable `(name, shardId, forward,
              compensation)` descriptor. A standalone
              `SagaCompensation.cs` wasn't needed: the compensation
              delegate is a first-class property of the step, which
              keeps the public surface focused and honors
              one-class-per-file.
- [x] P09-06 `TransactionDecisionResolver.cs` — stateless resolver,
              `(shardIds, shardCatalog, preferSaga) → TransactionStrategy`.
              Single-shard → fast path; multi-shard + every cluster
              `SupportsTwoPhaseCommit` → 2PC; otherwise saga.
              `preferSagaOverTwoPhaseCommit` forces saga for
              multi-shard cases.
- [x] P09-07 `DistributedDataSource.Transactions.cs` partial —
              implements `BeginTransaction` / `Commit` /
              `EndTransaction` against an implicit single-flight scope
              (routing the hint entity from `PassedArgs.CurrentEntity`
              through the shard router), and exposes the explicit
              distributed API (`BeginDistributedTransaction`,
              `CommitDistributedTransaction`,
              `RollbackDistributedTransaction`, `RunSaga`) plus a
              public hot-swap `TransactionCoordinator` property.
              Transaction coordinator is constructed inside
              `DistributedDataSource` with delegates for
              shard resolution and in-doubt event raising, and with
              the in-memory log wired per
              `DistributedDataSourceOptions.EnableInMemoryTransactionLog`.
              Added companion
              `TransactionInDoubtEventArgs.cs` + the
              `OnTransactionInDoubt` event on `IDistributedDataSource`
              (raised through the Phase 07 safe-invoke pattern in
              `DistributedDataSource.Events.cs`).
              Transaction stubs removed from
              `DistributedDataSource.IDataSource.cs`.
- [ ] P09-08 Writes enlist in active scope — deferred to Phase 13.
              The current write executor still performs each write as
              an isolated per-cluster operation ("auto-commit"), which
              is equivalent to pre-Phase-09 behaviour. Automatic
              enlistment needs thread/async context propagation that
              pairs naturally with durable-log work in Phase 13; the
              explicit saga API (`RunSaga`) is the recommended
              multi-shard pattern in v1.

## Phase 10 - Resilience & Shard-Down Policy

Doc: [10-phase10-resilience-and-shard-down-policy.md](./10-phase10-resilience-and-shard-down-policy.md)

- [x] P10-01 `IShardHealthMonitor.cs`, `ShardHealthMonitor.cs`,
              `ShardHealthSnapshot.cs` — background poller + hot-path
              counters, per-shard snapshots, and OnShardDown/OnShardRestored
              events wired through the datasource.
- [x] P10-02 `ShardDownPolicy.cs`, `ShardDownPolicyOptions.cs` — per-mode
              policy (FailFast / SkipShard / UseFailover / DegradeScatter)
              plus distributed circuit-breaker tuning knobs.
- [x] P10-03 `DistributedCircuitBreaker.cs` — distribution-tier wrapper
              over the existing Proxy `CircuitBreaker`, keyed by shard id.
- [x] P10-04 EventArgs (`ShardDown`, `ShardRestored`, `DegradedMode`,
              `PartialBroadcast`) declared on `IDistributedDataSource`
              and raised through safe-invoke helpers.
- [x] P10-05 `DistributedDataSource.Resilience.cs` partial owning the
              monitor + breaker, the `ApplyResilienceOptions` hot-swap
              API, and hot-path helpers (`IsShardHealthyForDispatch`,
              `FilterHealthyShards`, `PassesScatterGate`,
              `EvaluateScatterGate`, `NotifyShardCallSucceeded/Failed`,
              `ReportPartialBroadcast`).
- [x] P10-06 Read/Write executors honor the health filter and per-mode
              policy via the extended `IShardInvoker` (default-interface
              members `IsShardHealthy`, `FilterHealthyShards`,
              `EvaluateScatterGate`, `NotifyShardSuccess`,
              `NotifyPartialBroadcast`) overridden on
              `ShardInvokerAdapter`. Replicated reads promote healthy
              replicas to the front; scatter reads dispatch only to the
              healthy subset and record per-leg latency; broadcast writes
              synthesize skipped-shard `WriteFanOutResult` entries so
              quorum evaluation stays honest.
- [x] P10-07 `MinimumHealthyShardRatio` enforced at scatter entry via
              `EvaluateScatterGate`, which raises `OnDegradedMode` and
              throws `DegradedShardSetException` before any per-shard
              work starts. Applied to scatter reads, replicated / broadcast
              fan-out writes, and opt-in scatter-delete writes.

## Phase 11 - Resharding & Rebalancing

Doc: [11-phase11-resharding-and-rebalancing.md](./11-phase11-resharding-and-rebalancing.md)

- [x] P11-01 `DualWriteWindow.cs`, `DualWriteState.cs`, `IDualWriteCoordinator.cs`,
              `DualWriteCoordinator.cs` under `Distributed/Resharding/`.
- [x] P11-02 `IEntityCopyService.cs`, `EntityCopyService.cs`,
              `CopyCheckpoint.cs`, `IEntityCopyCheckpointStore.cs`,
              `InMemoryCopyCheckpointStore.cs`, `EntityCopyOptions.cs`,
              `CopyResult.cs`.
- [x] P11-03 `IReshardingService.cs` plus partials
              (`ReshardingService.cs` root, `.AddShard.cs`,
              `.RemoveShard.cs`, `.MoveEntity.cs`, `.Repartition.cs`),
              with `ShardSpec.cs`, `RemoveShardPlan.cs`, `ReshardOutcome.cs`.
- [x] P11-04 `PlanDiff.cs`, `PlanDiffEntry.cs`, `PlanDiffKind.cs`.
- [x] P11-05 `Events/ReshardProgressEventArgs.cs` + `RaiseReshardProgress`
              helper and `OnReshardProgress` event on `IDistributedDataSource`.
- [x] P11-06 `ShardRouter.DualWrite.cs` partial consults
              `IDualWriteCoordinator` and fans write decisions out to
              source + target shards during DualWrite / Cutover.
- [x] P11-07 `DistributedDataSource.Resharding.cs` partial exposes
              `Resharder`, `AddShard/RemoveShard/MoveEntity/Repartition`
              async wrappers, and `ApplyDistributionPlanAsync` which
              routes non-empty `PlanDiff`s through the resharding pipeline.

## Phase 12 - Schema Management & DDL Broadcast

Doc: [12-phase12-schema-management-and-ddl-broadcast.md](./12-phase12-schema-management-and-ddl-broadcast.md)

- [x] P12-01 `IDistributedSchemaService.cs` and partials
              (`DistributedSchemaService.cs` root +
              `.Create` / `.Alter` / `.Drop` / `.DriftDetection`
              partials, with delegates for plan/shard lookup and
              audit raising so the service stays decoupled from
              `DistributedDataSource`).
- [x] P12-02 `SchemaDriftReport.cs`, `SchemaDriftEntry.cs`,
              `SchemaDriftKind.cs`. `DetectSchemaDriftAsync`
              samples every target shard via
              `IProxyCluster.GetEntityStructure`, picks a
              deterministic reference, and classifies drift
              (missing entity, extra/missing columns, type /
              nullability / identity / PK mismatches, index
              drift).
- [x] P12-03 `AlterEntityChange.cs` + `AlterEntityChangeKind.cs`
              (discriminated record with static factories for
              `AddColumn` / `DropColumn` / `AlterColumn` /
              `AddIndex` / `DropIndex`). `DistributedSchemaService.Alter`
              renders each change into an `ETLScriptDet` of
              kind `DDLScriptType.AlterFor` and fans it out.
- [x] P12-04 `IDistributedSequenceProvider.cs`,
              `SnowflakeSequenceProvider.cs`, and
              `HiLoSequenceProvider.cs` (with `HiLoBlock` /
              `HiLoBlockAllocator`). Pluggable, thread-safe
              id allocators for Phase 07 sharded inserts.
- [x] P12-05 `IdentityColumnPolicy.cs` (`WarnOnly` /
              `RejectShardedIdentity`) + option wiring in
              `DistributedDataSourceOptions.IdentityColumnPolicy`
              and `DistributedDataSourceOptions.SequenceProvider`.
              Creating a `Sharded` entity with an auto-identity
              column under `RejectShardedIdentity` returns a
              populated `SchemaOperationOutcome.TerminalError`.
- [x] P12-06 `DistributedDataSource.Schema.cs` partial -
              lazy `SchemaService` accessor, `IDataSource`
              DDL members (`CreateEntityAs`, `CreateEntities`,
              `GetCreateEntityScript`, `RunScript`) that
              block on the async schema service, and typed
              async wrappers
              (`CreateEntityDistributedAsync`,
              `AlterEntityDistributedAsync`,
              `DropEntityDistributedAsync`,
              `DetectSchemaDriftAsync`).
- [x] P12-07 DDL paths emit audit events: placement
              violations flow through `RaisePlacementViolation`,
              per-shard fan-out exceptions go through
              `RaisePassEvent`, and every operation returns a
              `SchemaOperationOutcome` capturing targeted vs.
              succeeded shards plus per-shard errors (and a
              terminal error when validation blocks execution).
- [x] P12-BUILD Clean `DataManagementEngineStandard` build
              across net8.0 / net9.0 / net10.0 with 0 errors
              (2026-04-19).

## Phase 13 - Observability, Security, Audit

Doc: [13-phase13-observability-security-audit.md](./13-phase13-observability-security-audit.md)

- [x] P13-01 `IDistributedMetricsAggregator.cs`,
              `DistributedMetricsAggregator.cs`,
              `DistributionMetricsSnapshot.cs`, `HotShardDetector.cs`.
- [x] P13-02 `DistributedActivitySource.cs`.
- [x] P13-03 `IDistributedAuditSink.cs` + sinks
              (`NullDistributedAuditSink`, `FileDistributedAuditSink`) +
              `DistributedAuditEvent` record +
              `DistributedAuditEventKind` enum.
- [x] P13-04 `FileTransactionLog.cs` (durable JSON-lines append-only
              log; wired into the coordinator when
              `DistributedDataSourceOptions.DurableTransactionLogDirectory`
              is set, with graceful fallback to in-memory log).
- [x] P13-05 `IDistributedAccessPolicy.cs` + 2 implementations
              (`AllowAllAccessPolicy`, `EntityAclAccessPolicy`) +
              `DistributedAccessKind` enum + `DistributedSecurityException`.
- [x] P13-06 `DistributedDataSource.Observability.cs`,
              `DistributedDataSource.Audit.cs` partials with full
              wiring: access checks on every read/write/DDL hop,
              placement/scatter/fan-out/DDL/reshard/transaction audit
              events, metrics recording from resilience callbacks, and
              hot-shard/hot-entity detection event forwarding.
- [x] P13-BUILD Clean `DataManagementEngineStandard` build
              across net8.0 / net9.0 / net10.0 with 0 errors
              (2026-04-19).

## Phase 14 - Performance & Capacity Engineering

Doc: [14-phase14-performance-and-capacity-engineering.md](./14-phase14-performance-and-capacity-engineering.md)

- [x] P14-01 `IDistributedRateLimiter.cs`, `TokenBucketRateLimiter.cs`
              (per-shard token buckets, configurable rate + burst,
              sync `TryAcquire` and async `AcquireAsync` with retry-after
              `BackpressureException`).
- [x] P14-02 `DistributedConcurrencyGate.cs` - global
              (`MaxConcurrentDistributedCalls`) + per-shard
              (`MaxConcurrentCallsPerShard`) semaphores with
              `IDisposable` permits. Throws `BackpressureException`
              when the cap is reached.
- [x] P14-03 `AdaptiveTimeoutCalculator.cs` consumes the Phase 13
              `DistributionMetricsSnapshot` per-shard p95 and
              computes `clamp(factor * p95, minDeadline, maxDeadline)`
              with the caller's per-shard timeout as a floor.
- [x] P14-04 `HotShardMitigator.cs` subscribes to
              `IDistributedMetricsAggregator.OnHotShardDetected` and
              maintains a per-shard cooldown. Replicated / broadcast
              reads skip hot shards when another replica is available;
              single-shard reads are never shed.
- [x] P14-05 `BackpressureException.cs` + `BackpressureEventArgs.cs`
              (serializable exception with `GateName` + `RetryAfter`
              and an `OnBackpressure` event on `DistributedDataSource`
              so operators can instrument it without catching the
              exception upstream).
- [x] P14-06 `PerformanceOptions.cs` + wiring in
              `DistributedDataSourceOptions.Performance` /
              `DistributedDataSourceOptions.RateLimiter`. Default
              caps preserve existing Phase 06 / 07 behaviour.
- [x] P14-07 `DistributedDataSource.Performance.cs` partial +
              Phase 14 hooks on `IShardInvoker`
              (`AcquireDistributedCallPermit`,
              `AcquireShardCallPermit`, `ShedHotShards`,
              `ComputeShardDeadlineMs`). `ShardInvokerAdapter`
              overrides forward to the datasource. Read + write
              executors (scatter, replicated, single-shard, fan-out,
              scatter-delete) acquire permits, apply adaptive
              per-leg deadlines, and surface `BackpressureException`
              cleanly. `Dispose()` tears down the gate and detaches
              the mitigator from the aggregator.
- [x] P14-BUILD Clean `DataManagementEngineStandard` build
              across net8.0 / net9.0 / net10.0 with 0 errors
              (2026-04-19).

## Phase 15 - DevEx, Testing, Rollout Governance

Doc: [15-phase15-devex-testing-and-rollout-governance.md](./15-phase15-devex-testing-and-rollout-governance.md)

- [ ] P15-01 `FakeProxyCluster.cs`.
- [ ] P15-02 `RecordingShardRoutingHook.cs`.
- [ ] P15-03 `DistributedFaultInjector.cs`.
- [ ] P15-04 `PartitionFunctionFuzzer.cs`.
- [ ] P15-05 `DistributedDataSourceTestKit.cs`.
- [ ] P15-06 `IDistributedRolloutMode.cs`, `ShadowModeRunner.cs`,
              `DarkLaunchWriter.cs`.
- [ ] P15-07 `KpiGate.cs`, `RolloutKpiSnapshot.cs`.
- [ ] P15-08 `HOWTO_Add_Distributed_Datasource.md`.
- [ ] P15-09 `Distributed_Rollout_Runbook.md`.
- [ ] P15-10 `Capacity_Sizing_Guide.md`.
- [ ] P15-11 Contract tests per IDataSource method per mode.

## Cross-Phase Cleanups & Notes

- [ ] Verify no Proxy assembly takes a dependency on Distributed assembly.
- [ ] Verify `Distributed/` folder remains additive (no breaking changes to
      existing `Proxy/` or `BeepDM` consumers).
- [ ] Tag all new public types with XML doc comments and `since` notes.
- [ ] Confirm partial-class file-per-responsibility convention is consistent
      with `Proxy/ProxyCluster.*.cs` style.
