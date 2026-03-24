# Phase 1 - Contract and Plan Artifact Foundation

## Objective
Introduce a first-class migration plan artifact and execution contract that separates planning from applying.

## Scope
- Plan model, immutable plan identity/hash, and lifecycle states.
- Non-breaking integration with existing `IMigrationManager` APIs.

## File Targets
- `Migration/IMigrationManager.cs`
- `Migration/MigrationManager.cs`

## Planned Enhancements
- Add migration plan model:
  - entities/operations
  - risk classification
  - provider assumptions
  - checksum/hash
- Add lifecycle:
  - draft
  - reviewed
  - approved
  - applied
  - verified
- Preserve existing method signatures with extension methods/wrapper services.

## Implementation Rules (Skill Constraints)
- Keep `IDMEEditor` as orchestration boundary (`beepdm`).
- Use `IDataSource` and `IDataSourceHelper` contracts for provider-agnostic behavior (`idatasource` + `migration`).
- Persist plan metadata through `ConfigEditor`-managed stores (`configeditor`).

## Acceptance Criteria
- Plans can be generated without applying changes.
- Plan hash changes when operation set changes.
- Existing migration calls remain usable in compatibility mode.
