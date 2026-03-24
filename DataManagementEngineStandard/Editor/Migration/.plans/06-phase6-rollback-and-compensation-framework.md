# Phase 6 - Rollback and Compensation Framework

## Objective
Define rollback-safe migration execution with compensation scripts and operational playbooks.

## Scope
- Rollback models for reversible and non-reversible operations.
- Compensation strategy when strict rollback is impossible.

## File Targets
- `Migration/MigrationManager.cs`
- `Migration/README.md`

## Planned Enhancements
- Rollback classes:
  - reversible DDL rollback
  - forward-fix only with compensation
- Pre-apply rollback readiness checks:
  - backup/snapshot confirmation
  - restore test evidence
- Compensation plan artifacts attached to each high-risk migration step.

## Acceptance Criteria
- High-risk migrations require rollback/compensation plan before apply.
- Rollback execution path is testable in pre-production.
- Failure summaries include rollback outcome details.
