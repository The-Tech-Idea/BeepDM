# Phase 15 - DevEx, Testing, & Rollout Governance

## Objective

Make distribution safe to ship and easy to debug. Provide first-class fault
injection, simulated shard outages, partition-function fuzzing, contract
tests, and a documented rollout plan with KPI gates and shadow mode.

## Dependencies

- All prior phases.
- Existing `Proxy/ProxyCluster.FaultInjection.cs` patterns are reused for
  per-shard fault injection.

## Scope

- DevEx surface:
  - In-memory `FakeProxyCluster` for tests.
  - `IShardRoutingHook` test impl that pins requests for assertions.
  - `DistributedFaultInjector` controlling per-shard outages, latency, and
    quorum failure scenarios.
  - `PartitionFunctionFuzzer` to validate distribution uniformity over a key
    population.
  - Contract test suite per `IDataSource` method (read, write, transaction,
    DDL) with fixtures for each `DistributionMode`.
- Rollout governance:
  - Shadow mode: distributed datasource runs alongside legacy, mirrors reads,
    compares outputs, never returns the distributed result to the caller.
  - Dark-launch writes: writes go to legacy primary AND to distributed
    placement, distributed result discarded if mismatch detected; report drift.
  - KPI gates: success rate, p95 latency, drift count, in-doubt count -
    rollout halts when any KPI breaches.
- Documentation deliverables:
  - `DOCS/HOWTO_Add_Distributed_Datasource.md`.
  - `DOCS/Distributed_Rollout_Runbook.md`.
  - `DOCS/Capacity_Sizing_Guide.md`.

## Out of Scope

- A full e2e test harness with real backend instances (covered by separate
  CI infra).

## Target Files

Under `Distributed/Testing/`:

- `FakeProxyCluster.cs`.
- `RecordingShardRoutingHook.cs`.
- `DistributedFaultInjector.cs`.
- `PartitionFunctionFuzzer.cs`.
- `DistributedDataSourceTestKit.cs` (one-call setup helper).

Under `Distributed/Rollout/`:

- `IDistributedRolloutMode.cs`.
- `ShadowModeRunner.cs`, `DarkLaunchWriter.cs`.
- `KpiGate.cs` and `RolloutKpiSnapshot.cs`.

Under `Distributed/DOCS/`:

- `HOWTO_Add_Distributed_Datasource.md`.
- `Distributed_Rollout_Runbook.md`.
- `Capacity_Sizing_Guide.md`.

Update root README:

- Add a quickstart pointing to `HOWTO_Add_Distributed_Datasource.md`.

## Design Notes

- `FakeProxyCluster` implements `IProxyCluster` against an in-memory store so
  tests do not need real DBs. It records every call for assertion.
- `DistributedFaultInjector` lets a test toggle: shard down, slow shard,
  flaky shard, partial broadcast failure, in-doubt commit.
- `PartitionFunctionFuzzer` runs N synthetic keys through a function and
  reports per-shard distribution + max imbalance ratio; CI gate at 1.5x.
- Shadow mode wraps the legacy datasource and the distributed datasource
  inside a `ShadowModeRunner` that returns the legacy result, runs the
  distributed call asynchronously, and reports diffs to the audit sink.
- Dark-launch writes use the same wrapper pattern but with writes; failures
  are isolated and never affect the production write path.

## Implementation Steps

1. Create `Distributed/Testing/`, `Distributed/Rollout/`, `Distributed/DOCS/`.
2. Implement `FakeProxyCluster` (IProxyCluster compliance + recorder).
3. Implement `DistributedFaultInjector` and integrate hooks into executors
   gated by `DistributedDataSourceOptions.EnableFaultInjection` (off in prod).
4. Implement `PartitionFunctionFuzzer` (returns histogram + imbalance metric).
5. Implement contract tests per `IDataSource` method per `DistributionMode`.
6. Implement `ShadowModeRunner`, `DarkLaunchWriter`, and `KpiGate`.
7. Write `HOWTO_Add_Distributed_Datasource.md` (config -> register -> verify).
8. Write `Distributed_Rollout_Runbook.md` (shadow -> dark-launch -> cutover
   with KPI gates and rollback steps).
9. Write `Capacity_Sizing_Guide.md` (shard count, virtual slots, parallelism,
   throughput rules of thumb).

## TODO Checklist

- [ ] `FakeProxyCluster.cs`.
- [ ] `RecordingShardRoutingHook.cs`.
- [ ] `DistributedFaultInjector.cs`.
- [ ] `PartitionFunctionFuzzer.cs`.
- [ ] `DistributedDataSourceTestKit.cs`.
- [ ] `IDistributedRolloutMode.cs`, `ShadowModeRunner.cs`, `DarkLaunchWriter.cs`.
- [ ] `KpiGate.cs`, `RolloutKpiSnapshot.cs`.
- [ ] `HOWTO_Add_Distributed_Datasource.md`.
- [ ] `Distributed_Rollout_Runbook.md`.
- [ ] `Capacity_Sizing_Guide.md`.
- [ ] Contract tests per IDataSource method per mode.

## Verification Criteria

- [ ] Test suite covers all four `DistributionMode` values for read, write,
      transaction, and DDL paths.
- [ ] Fuzzer reports max imbalance < 1.5x for Hash function over 1M keys
      across 8 shards.
- [ ] Shadow mode catches an injected divergence between legacy and
      distributed and emits a drift audit event.
- [ ] `KpiGate` halts a synthetic rollout when error rate crosses the
      configured threshold.
- [ ] HOWTO walks a developer from zero to a working 2-shard distribution
      in under 30 minutes.

## Risks / Open Questions

- Shadow mode doubles read load; document the cost and recommend running it
  on a sampled fraction (`ShadowSampleRate` in `RolloutOptions`).
