# Phase 2 - Compatibility and Safety Policy Engine

## Objective
Create policy-driven safety gates that classify and block risky migration operations before apply.

## Scope
- Risk scoring and policy checks for operations.
- Blocking/approval thresholds by environment tier.

## File Targets
- `Migration/MigrationManager.cs`
- `Migration/IMigrationManager.cs`

## Planned Enhancements
- Operation risk classes:
  - additive safe
  - potentially breaking
  - destructive/data-loss risk
- Policy rules:
  - block drop column/table in protected environments
  - require manual approval for type narrowing
  - enforce nullability transition checks
- Migration readiness extends with policy verdicts.

## Acceptance Criteria
- Policy engine emits pass/warn/block decisions per operation.
- High-risk operations require explicit override reason and approver.
- Unsafe plans are blocked in protected environments by default.
