# Phase 1 - Core Contracts and Architecture

## Objective
Create the foundational architecture for `BalancedDataSource` as a full `IDataSource` implementation.

## Scope
- Core class design and separation of concerns.
- Contract compatibility with existing `IDMEEditor` and `IDataSource` consumers.

## File Targets (planned)
- `Balanced/BalancedDataSource.cs` (new)
- `Balanced/BalancedDataSource.Options.cs` (new)
- `Balanced/BalancedDataSource.Interfaces.cs` (new)

## Planned Enhancements
- Implement full `IDataSource` surface.
- Add internal strategy interfaces:
  - route selector
  - health evaluator
  - failover policy
  - retry policy
- Keep compatibility with existing orchestration paths in `IDMEEditor`.

## Implementation Rules (Skill Constraints)
- Preserve full `IDataSource` contract semantics (`idatasource`).
- Use `IDMEEditor` as orchestration boundary (`beepdm`).
- Persist config/policy through `ConfigEditor` where applicable (`configeditor`).

## Acceptance Criteria
- Contract coverage checklist completed for all `IDataSource` members.
- Existing consumer code can substitute `BalancedDataSource` without signature changes.
