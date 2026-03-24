# Phase 4 - Object Graph and Nested Mapping

## Objective
Enable mapping of nested objects and collections with identity/cycle-safe behavior.

## Scope
- Deep mapping for complex object models.
- Collection mapping strategies and identity resolution.

## File Targets
- `Mapping/MappingManager.cs`
- `Mapping/Core/*` and `Mapping/Utilities/*` (as applicable)

## Planned Enhancements
- Nested path mapping:
  - `Address.City -> CustomerAddress.City`
  - collection item mapping by key selectors
- Object graph controls:
  - max depth
  - cycle detection
  - reference reuse policy
- Collection modes:
  - append
  - replace
  - merge-by-key

## Acceptance Criteria
- Nested object and collection mapping scenarios pass deterministic tests.
- Cycle-prone models do not cause recursion failures.
- Mapping output is predictable under merge policies.
