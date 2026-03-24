# Phase 3 - Provider Capability Matrix and Portability

## Objective
Operationalize provider differences with an explicit capability matrix and portability checks in planning.

## Scope
- Provider-specific constraints at plan-time.
- Portable migration strategy generation.

## File Targets
- `Migration/MigrationManager.cs`
- `Migration/README.md`

## Planned Enhancements
- Capability matrix dimensions:
  - alter column support
  - rename support
  - transactional DDL behavior
  - online/offline implications
- Plan annotations:
  - provider constraints
  - fallback strategy when unsupported
- Provider-specific recommendation bundling into plan outputs.

## Acceptance Criteria
- Plans clearly state provider capability assumptions.
- Unsupported operations produce explicit fallback tasks.
- Portability warnings are present when moving between providers.
