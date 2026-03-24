# Phase 5 - Rule-Based and Conditional Mapping

## Objective
Support enterprise rule-driven mapping logic per field/entity.

## Scope
- Conditional mapping rules and precedence.
- Business-rule transforms and fallback strategies.

## File Targets
- `Mapping/MappingManager.cs`
- `Mapping/MappingManager.Conventions.cs`

## Planned Enhancements
- Rule constructs:
  - `WHEN condition THEN map`
  - `IF source is null THEN default`
  - conditional transform branching
- Rule priorities:
  - explicit user rule
  - confidence auto-map
  - fallback default
- Conflict resolution strategy with deterministic precedence.

## Acceptance Criteria
- Conditional mapping rules execute in documented order.
- Rule conflicts are surfaced and explainable.
- Integration with DefaultsManager remains safe and predictable.
