# Phase 6 - Validation, Scoring, and Drift Detection

## Objective
Provide mapping quality gates with scoring, drift alerts, and pre-flight validation comparable to enterprise tooling.

## Scope
- Validation before execution.
- Schema drift detection and remediation guidance.

## File Targets
- `Mapping/MappingManager.Conventions.cs`
- `Mapping/MappingManager.cs`

## Planned Enhancements
- Validation dimensions:
  - missing source fields
  - duplicate assignments
  - type incompatibility risk
  - nullability mismatch
- Mapping quality score (0-100) and risk bands.
- Drift detection:
  - source schema change
  - destination schema change
  - mapping stale-field detection
- Suggested remediation actions and auto-fix candidates.

## Acceptance Criteria
- Validation output includes score + categorized issues.
- Drift detection catches added/removed/renamed field changes.
- Mapping execution can enforce score thresholds for production runs.
