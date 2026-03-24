# Phase 4 - Dry-Run, Preflight, and Impact Analysis

## Objective
Add enterprise-grade preview behavior with dry-run scripts, preflight checks, and impact analysis.

## Scope
- Dry-run generation and simulation.
- Preflight validation before apply.

## File Targets
- `Migration/MigrationManager.cs`
- `Migration/IMigrationManager.cs`

## Planned Enhancements
- Dry-run output:
  - operation list
  - generated DDL preview
  - expected risk tags
- Preflight checks:
  - connection and permissions
  - locks/active sessions estimate
  - schema drift since plan creation
- Impact analysis:
  - entity/column usage hints
  - data volume sensitivity indicators

## Acceptance Criteria
- Dry-run can execute without changing schema.
- Preflight failures block apply and provide actionable diagnostics.
- Impact report is attached to plan artifact.
