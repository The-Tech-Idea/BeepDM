# Phase 10 - Rollout Governance and KPI Gates

## Balanced Alignment
This phase is aligned with [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md).

## Objective
Roll out proxy enhancements using controlled waves and KPI-based progression.

## Scope
- Wave strategy and hard-stop rules.
- KPI governance and approval cadence.

## File Targets
- `Proxy/README.md`
- `Proxy/ProxyDataSource.cs`

## Planned Enhancements
- Rollout waves:
  - Wave 1: non-critical workloads
  - Wave 2: standard production workloads
  - Wave 3: critical workloads
- KPI gates:
  - failover rate
  - circuit-open duration
  - error rate
  - p95 latency
  - cache hit ratio
- Hard-stop conditions for automatic pause/revert.

## Audited Hotspots
- mutation wrappers currently at risk of duplicate invocation under retries
- health/routing state concurrency behavior under timer + live traffic
- transaction wrapper result/exception propagation

## Real Constraints to Address
- Rollout must explicitly guard against duplicate-write regressions.
- KPI gates should include "duplicate side-effect incident count" and "retry-without-replay violations".
- Promotion requires deterministic policy consistency between configured and effective runtime settings.

## Acceptance Criteria
- Promotion requires KPI threshold pass.
- Rollout decisions are recorded and auditable.
