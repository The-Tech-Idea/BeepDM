# Phase 9 - Migration, Rollout, and KPI Governance

## Objective
Execute controlled migration from current ETL behavior to enterprise-target behavior with measurable adoption, reliability, and quality outcomes.

## Enterprise Standards Mapped
- Azure DataOps promotion controls and trigger-safe rollout.
- Snowflake/Databricks incremental migration and layered transition patterns.
- Informatica governance expectations for policy and audit continuity.

## Current-State Findings
- Backward-compatibility bridges exist in models and manager flows.
- No consolidated rollout wave model with KPI thresholds and rollback triggers.
- Operational success metrics are present but not unified as migration guardrails.

## Target-State Contract
- Wave-based migration:
  - Wave 1: non-critical pipelines.
  - Wave 2: standard production pipelines.
  - Wave 3: business-critical pipelines with strict SLO controls.
- Exit criteria and rollback triggers per wave.
- KPI governance board cadence for weekly migration health review.

## Required Workstreams and File Targets
- Migration and compatibility targets:
  - `Engine/PipelineManager.cs`
  - `Engine/PipelineDefinitionExtensions.cs`
  - `Scheduling/SchedulerHost.cs`
  - `Observability/PipelineDashboardApi.cs`
- Policy and metric linkage:
  - `Observability/MetricsEngine.cs`
  - `Observability/ObservabilityStore.cs`

## Acceptance Criteria and KPIs
- Migration completion rate by wave and pipeline tier.
- Reliability KPIs: success rate, retry inflation, timeout incidence.
- Quality KPIs: DQ reject ratio, lineage coverage, SLA freshness compliance.
- Business KPIs: reduced incident count and improved change lead time.

## Risks and Mitigations
- Risk: hidden legacy assumptions cause regressions.
  - Mitigation: compatibility mode and canary runs before wave progression.
- Risk: KPI drift goes unnoticed.
  - Mitigation: weekly governance reviews and hard stop thresholds.

## Test and Validation Plan
- Canary and shadow-run comparisons for migrated pipelines.
- Automated regression packs before wave promotion.
- Post-cutover health checks at 24h, 72h, and 7-day intervals.
