# Phase 2 - Engine Execution Semantics

## Objective
Harden runtime behavior so modeled execution policies are fully enforced and deterministic in normal, retry, cancel, and failure scenarios.

## Enterprise Standards Mapped
- Azure DataOps reliability expectations for predictable deployments and rollback safety.
- AWS Glue reliability and memory discipline for large jobs.
- Snowflake-style deterministic orchestration behavior for incremental pipelines.

## Current-State Findings
- `PipelineDefinition` and `PipelineStepDef` expose policy fields (`StopOnErrorCount`, `MaxParallelBatches`, `IsParallel`, `FilterExpression`, `TimeoutSeconds`) but execution coverage is incomplete.
- `PipelineEngine` uses retries and batching, but sink rollback behavior is not consistently applied on failures.
- `PipelineStepRunner` tracks outcomes but does not enforce full per-step policy envelope.

## Target-State Contract
- Runtime must enforce:
  - pipeline-level stop-on-error thresholds.
  - per-step timeout and retries.
  - optional per-step pre-filter behavior.
  - controlled parallel step execution where safe.
  - explicit sink rollback and error classification on terminal failure.

## Required Workstreams and File Targets
- Policy enforcement and lifecycle:
  - `Engine/PipelineEngine.cs`
  - `Engine/PipelineStepRunner.cs`
  - `Engine/PipelineRetryPolicy.cs`
- Model alignment:
  - `../../DataManagementModelsStandard/Pipelines/Models/PipelineDefinition.cs`
  - `../../DataManagementModelsStandard/Pipelines/Models/PipelineStepDef.cs`
- Plugin consistency:
  - `../../DataManagementModelsStandard/Pipelines/Interfaces/PipelineConstants.cs`

## Acceptance Criteria and KPIs
- 100% of modeled execution policy fields are either enforced or explicitly deprecated with migration notes.
- Failed runs call sink rollback path where sink supports rollback semantics.
- Deterministic run outcomes for repeated runs with same input and configuration.
- Retry metrics include attempt count and terminal failure reason per step.

## Risks and Mitigations
- Risk: policy enforcement changes behavior for existing pipelines.
  - Mitigation: feature flags and compatibility mode defaults for legacy runs.
- Risk: parallelization introduces order-sensitive regressions.
  - Mitigation: opt-in parallel policies plus deterministic ordering tests.

## Test and Validation Plan
- Unit tests for each policy field and conflict matrix.
- Integration tests for cancel/fail/retry/rollback paths.
- Soak tests on large input sets with threshold and timeout policies enabled.
