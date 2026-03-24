# Phase 2 - Migration Summary and Readiness Reporting

## Objective
Upgrade readiness and summary reporting from operator-readable output to policy-grade artifacts suitable for automation and governance.

## Audited Hotspots
- `MigrationManager.ReadinessAndExplicit.cs`
  - `BuildReadinessReport(...)`
  - `CreateBaseReadinessReport(...)`
  - `LogReadinessReport(...)`
- `MigrationManager.Discovery.cs`
  - `GetMigrationSummary(...)`
  - `GetMigrationReadiness(...)`
  - `GetMigrationReadinessForTypes(...)`
- `MigrationManager.Capabilities.cs`
  - probe methods used by readiness capability context.

## Real Constraints to Address
- Readiness issues are rich but recommendations are free-text and hard to gate in CI.
- Summary output tracks create/update/uptodate lists but does not include stable decision codes per entity.
- Best-practice lines are mixed with issues in logging flow without explicit event type separation.

## Enhancements
- Add stable issue code taxonomy contracts for readiness and summary records.
- Add per-entity decision payload:
  - `Decision = Create | Update | NoChange | Error`
  - `DecisionReasonCode`
  - `CapabilityContextSnapshot`
- Add machine-facing report export contract (JSON serializable shape) for CI.
- Separate log channels:
  - `BestPractice`
  - `ReadinessIssue`
  - `MigrationDecision`
- Introduce report hash/fingerprint for diffing between runs.

## Deliverables
- Reporting schema document and serializer contract.
- CI integration example using readiness report for block/warn thresholds.
- Updated examples showing human summary + machine payload side by side.

## Acceptance Criteria
- CI pipeline can block on readiness errors without parsing message text.
- Repeated runs with same inputs produce stable report fingerprints.
- Report includes datasource capability snapshot used for each decision.
