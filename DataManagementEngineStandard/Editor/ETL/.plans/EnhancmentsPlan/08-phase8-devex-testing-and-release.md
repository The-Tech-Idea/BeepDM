# Phase 8 - DevEx, Testing, and Release

## Objective
Define an engineering workflow that makes ETL changes safer and faster through repeatable local testing, CI quality gates, and evidence-based releases.

## Enterprise Standards Mapped
- Azure DataOps: branch strategy, PR gating, promotion pipeline, deployment sequencing.
- AWS Glue local-first testing recommendations.
- Google Dataflow emphasis on reliability and testability.

## Current-State Findings
- Existing ETL code has design docs but lacks a formal, enforced ETL-specific quality gate framework.
- Runtime behavior can evolve without standardized compatibility and non-functional tests.
- Release evidence and rollback readiness are not standardized templates.

## Target-State Contract
- Required release pipeline stages:
  - local validation and unit tests.
  - integration tests against representative source/sink profiles.
  - performance and reliability regression checks.
  - staged promotion with rollback proof.
- Standard artifacts: release notes, test evidence bundle, risk sign-off.

## Required Workstreams and File Targets
- Documentation and release governance in `.plans`.
- Runtime-focused test targets (future implementation):
  - `Engine/PipelineEngine.cs`
  - `Scheduling/SchedulerHost.cs`
  - `Observability/ObservabilityStore.cs`
  - `Engine/BuiltIn/Transformers/*.cs`
  - `Engine/BuiltIn/Validators/*.cs`

## Acceptance Criteria and KPIs
- CI enforces required ETL quality gates for protected branches.
- Release bundles include rollback test evidence for production-bound changes.
- Escaped defect rate and post-release incident rate trend downward.
- Test coverage for critical ETL runtime paths meets agreed threshold.

## Risks and Mitigations
- Risk: test matrix becomes too expensive.
  - Mitigation: risk-based suites (smoke, critical, full) and nightly deep runs.
- Risk: teams bypass gates under time pressure.
  - Mitigation: protected branch policies and exception approval trail.

## Test and Validation Plan
- Trial rollout on selected ETL modules before org-wide enforcement.
- CI observability to monitor gate duration, flake rate, and failure distribution.
- Quarterly review of gate effectiveness vs incident outcomes.
