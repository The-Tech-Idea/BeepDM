# Phase 5 - Orchestration, CDC, and Dependencies

## Objective
Elevate scheduler behavior to enterprise orchestration with CDC-first incremental runs, dependency governance, and controlled replay/backfill.

## Enterprise Standards Mapped
- Snowflake streams/tasks and DAG patterns for incremental processing.
- Azure trigger and dependency-safe deployment behavior.
- Databricks multi-hop orchestration discipline between quality layers.

## Current-State Findings
- `SchedulerHost` supports queueing, retries, dependencies, and multiple schedulers.
- Dependency handling exists via `DependencyGraph`, but replay and backfill policies are not standardized.
- Trigger source and schedule status are tracked but not yet governed as lifecycle policy.

## Target-State Contract
- Incremental-first orchestration strategy with explicit full-refresh fallbacks.
- Dependency graph policy includes success/failure semantics and max-wait constraints.
- Replay and backfill procedures are deterministic and auditable.
- Schedule definitions include ownership, SLA, and change-control metadata.

## Required Workstreams and File Targets
- Scheduler governance and run semantics:
  - `Scheduling/SchedulerHost.cs`
  - `Scheduling/DependencyGraph.cs`
  - `Scheduling/PipelineRunQueue.cs`
  - `Scheduling/ScheduleStorage.cs`
- Event and manual trigger consistency:
  - `Scheduling/PipelineEventBus.cs`
  - `Engine/BuiltIn/Schedulers/*.cs`
- Pipeline run context enrichment:
  - `Engine/PipelineEngine.cs`

## Acceptance Criteria and KPIs
- Dependency deadlocks are detectable and surfaced as actionable alerts.
- Replay/backfill runs preserve source window boundaries and idempotency guarantees.
- Schedule changes are auditable with reason, approver, and effective date.
- Critical pipelines meet freshness SLO targets under normal and retry conditions.

## Risks and Mitigations
- Risk: dependency chains cause cascading failures.
  - Mitigation: circuit breakers, retry backoff tuning, and fail-fast propagation rules.
- Risk: backfill overload impacts live workloads.
  - Mitigation: queue class isolation and concurrency quotas by workload class.

## Test and Validation Plan
- DAG scenario tests for success, partial failure, timeout, and cancellation.
- Replay/backfill integration tests with duplicate-protection assertions.
- Stress tests for queue fairness and starvation prevention.
