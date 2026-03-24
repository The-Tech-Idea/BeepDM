# Phase 7 - Migration, DevEx, and Rollout

## Objective
Roll out DefaultsManager enhancements safely with developer guidance, migration tooling, and wave-based adoption.

## Scope
- Migration strategy from legacy rule forms to canonical forms.
- Developer examples and linting guidance.
- Controlled rollout waves and KPI gates.

## File Targets
- `Defaults/README.md`
- `Defaults/README_DefaultsManager.md`
- `Defaults/Examples/DefaultsManagerExamples.cs`
- `Defaults/Helpers/DefaultValueHelper.cs`

## Planned Enhancements
- Migration toolkit:
  - rule scanner,
  - compatibility report,
  - optional autofix suggestions.
- Rule style guide:
  - when to use static/default/query/expression operators,
  - naming and readability conventions.
- Rollout waves:
  - Wave 1: new rules in non-critical entities.
  - Wave 2: convert high-volume defaults.
  - Wave 3: enforce strict validation in production.

## Implementation Rules (Skill Constraints)
- Migration execution must preserve `IDMEEditor`-centric orchestration and avoid direct subsystem bypasses (`beepdm`).
- Any migrated defaults that touch datasource behavior must remain within `IDataSource` contract expectations and error handling norms (`idatasource`).
- All migration state, flags, and compatibility toggles should be persisted through `ConfigEditor` (`configeditor`).
- Migration artifacts/reports written to disk must use `EnvironmentService` folder conventions (`environmentservice`).
- Rollout instructions must assume shared, stable editor/service initialization patterns (`beepservice`), not per-screen initialization.

## Acceptance Criteria
- Existing defaults continue to work in compatibility mode.
- Migration report generated for all configured defaults.
- Docs/examples cover new query and dot-expression syntax.

## Risks and Mitigations
- Risk: migration churn in many existing rule strings.
  - Mitigation: automatic suggestions and phased enforcement.
- Risk: developers mix incompatible styles.
  - Mitigation: linter warnings and canonical formatting guidance.

## Test Plan
- Snapshot tests for legacy rules before and after migration tooling.
- Example compilation and runtime tests for published docs.
- Pilot rollout validation on selected datasources before global enablement.
