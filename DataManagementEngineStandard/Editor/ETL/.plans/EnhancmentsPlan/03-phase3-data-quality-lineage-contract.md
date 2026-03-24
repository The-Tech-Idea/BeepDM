# Phase 3 - Data Quality and Lineage Contract

## Objective
Define a formal DQ and lineage contract so every run produces auditable quality evidence and complete transformation traceability.

## Enterprise Standards Mapped
- Databricks silver-layer quality enforcement and dedup expectations.
- Informatica lineage and impact analysis principles.
- Microsoft Purview-aligned lineage mindset for downstream governance.

## Current-State Findings
- Validators and lineage tracking exist, but consolidated DQ report contracts are not consistently attached to run outcomes.
- Error-sink handling captures rejected records but quality dimensions are not standardized as KPIs.
- Some transform components are scale-sensitive for high-cardinality workloads.

## Target-State Contract
- Standard DQ dimensions per run: completeness, validity, uniqueness, consistency, timeliness.
- Row-level rejected/warned evidence policy with retention windows.
- Lineage completeness contract: source field -> transform rule -> destination field.
- Standard DQ report object attached to each run result and observability persistence.

## Required Workstreams and File Targets
- DQ outcome modeling and run integration:
  - `Engine/PipelineStepRunner.cs`
  - `Engine/PipelineEngine.cs`
  - `Observability/ObservabilityStore.cs`
- Transformer/validator hardening:
  - `Engine/BuiltIn/Validators/*.cs`
  - `Engine/BuiltIn/Transformers/DeDuplicateTransformer.cs`
  - `Engine/BuiltIn/Transformers/AggregateTransformer.cs`
  - `Engine/BuiltIn/Transformers/ScriptTransformer.cs`
- Lineage persistence and query contracts:
  - `Engine/PipelineLineageTracker.cs`
  - `Observability/PipelineDashboardApi.cs`

## Acceptance Criteria and KPIs
- Every run emits a DQ report and lineage completeness percentage.
- >= 99% lineage coverage for transformed destination fields in production pipelines.
- Rejected and warned records are attributable to rule IDs and step IDs.
- DQ trend queries available by pipeline, step, and period.

## Risks and Mitigations
- Risk: DQ overhead impacts throughput.
  - Mitigation: configurable sampling and tiered verbosity levels.
- Risk: lineage volume growth increases storage pressure.
  - Mitigation: retention policy, compaction, and archive strategy.

## Test and Validation Plan
- Rule-level unit tests for pass/warn/reject outcomes.
- End-to-end lineage trace tests across multi-step transforms.
- Regression tests for DQ score reproducibility on static datasets.
