# Phase 09 - Distributed Transactions (Single-Shard, 2PC, Saga)

## Objective

Provide a clear transaction story for `DistributedDataSource`. Single-shard
transactions are zero-overhead (delegated to one cluster). Multi-shard work
gets either two-phase commit (when every involved backend supports it) or a
compensating saga (when 2PC is not available or not desired).

## Dependencies

- Phase 07 (write executor).
- Phase 05 (router).
- `IDataSource.BeginTransaction` / `EndTransaction` semantics.

## Scope

- `IDistributedTransactionCoordinator` orchestrating begin / enlist / commit /
  rollback across N shards.
- Three execution strategies, selected by capability + policy:
  1. `SingleShardFastPath` - all touched entities map to the same shard;
     direct delegation, no overhead.
  2. `TwoPhaseCommit` - explicit prepare/commit phases; backends must support
     it (capability flag on `IProxyCluster`/`IDataSource`).
  3. `Saga` - sequence of forward operations + compensating operations the
     caller registers; the coordinator runs forward, then compensations on
     failure.
- `DistributedTransactionScope` - opaque token returned by `BeginTransaction`,
  threaded through writes so each write enlists in the same scope.
- Configurable timeouts, in-doubt resolution policy, and recovery log
  (in-process v1; durable in Phase 13).

## Out of Scope

- Strict serializable isolation across shards.
- Distributed deadlock detection.
- Cross-shard SAVEPOINTs (single-shard only in v1).

## Target Files

Under `Distributed/Transactions/`:

- `IDistributedTransactionCoordinator.cs`.
- `DistributedTransactionCoordinator.cs` (partial root).
- `DistributedTransactionCoordinator.SingleShard.cs` (partial).
- `DistributedTransactionCoordinator.TwoPhaseCommit.cs` (partial).
- `DistributedTransactionCoordinator.Saga.cs` (partial).
- `DistributedTransactionScope.cs`.
- `TransactionStrategy.cs` enum.
- `IDistributedTransactionLog.cs` + `InMemoryTransactionLog.cs` (v1) +
  `FileTransactionLog.cs` (v1.5).
- `SagaStep.cs`, `SagaCompensation.cs`.
- `TransactionDecisionResolver.cs` - chooses strategy based on involved
  shards' capabilities and policy.

Update partials:

- `DistributedDataSource.Transactions.cs` (new) - implements
  `BeginTransaction`, `EndTransaction`, `Commit`, `Rollback` returning a
  `DistributedTransactionScope` token.
- `DistributedDataSource.Writes.cs` - if a scope is active, every write
  enlists via the coordinator.

## Design Notes

- Capability flag: `IProxyCluster` exposes `SupportsTwoPhaseCommit` (added in
  this phase as an opt-in property, default false). It returns true only if
  the underlying `IDataSource` driver supports `IDbTransaction` + a
  prepare/commit pattern.
- Single-shard fast path detection: the coordinator inspects every enlisted
  entity's resolved shard set. If all = same shard, hand off to that
  cluster's transaction directly.
- 2PC log is required for crash recovery (Phase 13 makes it durable). v1 logs
  to memory but writes a clear warning.
- Saga: the caller declares each step as `(forward, compensate)`. Coordinator
  runs forward steps in order; on failure runs compensations in reverse.
- Read consistency in transactions is "read your own writes" within the
  scope; cross-shard snapshot is NOT guaranteed.

## Implementation Steps

1. Create `Distributed/Transactions/` folder.
2. Define `IDistributedTransactionCoordinator`,
   `DistributedTransactionScope`, `TransactionStrategy`,
   `IDistributedTransactionLog`, `InMemoryTransactionLog`.
3. Add `SupportsTwoPhaseCommit` to `IProxyCluster` (opt-in default false; no
   breaking change). Update `ProxyCluster` to return false unless every node
   supports it.
4. Implement `TransactionDecisionResolver` (single-shard / 2PC / saga
   selection based on capability + policy).
5. Implement `DistributedTransactionCoordinator.SingleShard.cs` partial.
6. Implement `TwoPhaseCommit.cs` partial (prepare phase across shards;
   global commit; in-doubt handling).
7. Implement `Saga.cs` partial (forward + compensation runner).
8. Implement `DistributedDataSource.Transactions.cs` partial.
9. Update `DistributedDataSource.Writes.cs` to enlist in the active scope
   when one is supplied.

## TODO Checklist

- [x] Add `SupportsTwoPhaseCommit` to `IProxyCluster` — added as a
      default interface member (`SupportsTwoPhaseCommit => false`) on
      `IProxyDataSource.cs`. Existing `ProxyCluster` implementations
      inherit the default, so no breaking change; concrete clusters
      opt-in by overriding once every node supports it.
- [x] `IDistributedTransactionCoordinator.cs` + partials —
      `IDistributedTransactionCoordinator` plus
      `DistributedTransactionCoordinator.cs` root (ctor, scope registry,
      shared helpers) and three strategy partials
      (`.SingleShard.cs`, `.TwoPhaseCommit.cs`, `.Saga.cs`). Each partial
      is single-responsibility and under 300 lines.
- [x] `DistributedTransactionScope.cs`, `TransactionStrategy.cs` —
      immutable scope token tracks correlation id, strategy, shard set,
      status, saga history, and labels; strategy enum covers
      `SingleShardFastPath`, `TwoPhaseCommit`, `Saga`.
      `DistributedTransactionStatus` enum captures the full lifecycle
      (`Active → Preparing/Committing/Compensating/Aborting → Committed/Aborted/InDoubt`).
- [x] `IDistributedTransactionLog.cs`, `InMemoryTransactionLog.cs` —
      contract + v1 in-process implementation backed by a
      `ConcurrentDictionary` keyed by correlation id;
      `TransactionLogEntry` record + `TransactionLogKind` enum cover
      every coordinator event (prepare sent/ack/nack, global commit/abort,
      commit ack/failed, rollback ack/failed, saga forward/compensation
      ack/failed, in-doubt, closed).
- [x] `SagaStep.cs` — immutable step descriptor pairing a forward
      delegate and an idempotent compensation per shard id;
      used by `DistributedTransactionCoordinator.Saga.RunSaga`.
      (A dedicated `SagaCompensation.cs` wasn't needed — the
      compensation delegate is a first-class property on `SagaStep`,
      keeping the descriptor focused and one class per file.)
- [x] `TransactionDecisionResolver.cs` — stateless resolver mapping
      `(shards, capabilities, preferSaga)` → `TransactionStrategy`.
      Single shard → fast path; multi-shard + every cluster
      `SupportsTwoPhaseCommit == true` → 2PC; otherwise saga.
- [x] `DistributedDataSource.Transactions.cs` partial — implements the
      `IDataSource` triple (`BeginTransaction` / `Commit` /
      `EndTransaction`) against an implicit single-flight scope, and
      exposes the explicit distributed API
      (`BeginDistributedTransaction`, `CommitDistributedTransaction`,
      `RollbackDistributedTransaction`, `RunSaga`) plus a public
      `TransactionCoordinator` property for hot-swapping the
      coordinator/log.
- [ ] Writes enlist in active scope — deferred to Phase 13. The current
      write executor fires each write as an isolated operation against
      its routed cluster, which is equivalent to "auto-commit" per
      write. Automatic enlistment requires thread/async context
      propagation that pairs naturally with the durable log work in
      Phase 13; v1 ships the explicit saga API as the recommended
      multi-shard pattern.

## Verification Criteria

- [x] All-single-shard transaction has zero added round-trips vs direct
      cluster transaction — verified by code review:
      `DistributedTransactionCoordinator.BeginSingleShard` /
      `CommitSingleShard` / `RollbackSingleShard` each invoke exactly
      one `cluster.BeginTransaction` / `cluster.Commit` /
      `cluster.EndTransaction` call with no extra proxy hops. The
      overhead is a log append + one status transition.
- [x] 2PC across 2 shards commits atomically in the success case and
      rolls back atomically when one shard fails the prepare — logic
      verified by code review:
      `DistributedTransactionCoordinator.TwoPhaseCommit.cs`
      (`BeginTwoPhaseCommit` / `CommitTwoPhaseCommit`) issues prepare
      per shard, rolls back every prepared shard on the first prepare
      failure via `AbortPreparedShards`, and only enters the commit
      phase after every shard votes ok. Runtime test lands with the
      Phase 14 integration harness.
- [x] In-doubt outcome surfaced via `OnTransactionInDoubt` event —
      verified by code review: when the commit round produces a mixed
      ack/fail set after a successful prepare, the coordinator builds a
      `TransactionInDoubtEventArgs`, appends an `InDoubt` log entry,
      and invokes the `_raiseInDoubt` callback (wired to
      `DistributedDataSource.RaiseTransactionInDoubt`). The event is
      declared on `IDistributedDataSource` and raised through the
      existing safe-invoke pattern in `DistributedDataSource.Events.cs`.
- [x] Saga rolls back compensations in reverse order on failure —
      verified by code review:
      `DistributedTransactionCoordinator.Saga.RunSaga` records every
      successful forward step, and `RollbackCompensations` iterates
      that list from the last index to index 0, invoking each step's
      `Compensation` delegate. Compensation failures are logged
      (`SagaCompensationFailed`) but never propagate so the scope
      always reaches a terminal state.
- [x] Strategy selection matches `TransactionDecisionResolver` table —
      verified by code review: single shard always returns
      `SingleShardFastPath`; `preferSagaOverTwoPhaseCommit` always
      returns `Saga`; multi-shard with every cluster
      `SupportsTwoPhaseCommit == true` returns `TwoPhaseCommit`; any
      missing or non-capable cluster falls back to `Saga`.

## Risks / Open Questions

- 2PC requires durable log to be production-safe. v1 ships in-memory with a
  startup warning; Phase 13 introduces `FileTransactionLog`.
- Some Beep `IDataSource` drivers do not expose explicit prepare. For these,
  fall back to saga and document the loss of atomicity.
