# UnitOfWork Helpers

## Purpose
This folder contains concrete helper implementations that the unit-of-work manager uses for state, validation, defaults, events, data conversion, and collection operations.

## Key Files
- `UnitofWorkStateHelper.cs`: Tracks and mutates per-entity state transitions.
- `UnitofWorkValidationHelper.cs`: Insert/update/delete validation rules.
- `UnitofWorkDefaultsHelper.cs`: Applies default rules during entity lifecycle operations.
- `UnitofWorkEventHelper.cs`: Creates event parameters and raises lifecycle events.
- `UnitofWorkDataHelper.cs`: Reflection-driven value extraction and cloning support.
- `UnitofWorkCollectionHelper.cs`: Sort/filter/page helpers for bound collections.

## Runtime Flow
1. Entity mutations enter the unit-of-work.
2. Defaults and validation execute before state transitions.
3. State helper marks changes and event helper emits pre/post hooks.
4. Collection helper supports downstream UI/query views.

## Extension Guidelines
- Keep helpers composable and side-effect scoped.
- Reuse cached reflection metadata for performance-critical paths.
- Never bypass validation when state transitions affect persistence.

## Testing Focus
- State transition correctness under add/edit/delete cycles.
- Validation behavior for missing keys and required fields.
- Event ordering and cancellation behavior.
