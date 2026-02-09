# Forms Helpers

## Purpose
This folder provides helper components for form runtime behavior: event wiring, dirty tracking, relationships, performance optimization, and simulation.

## Key Files
- `DirtyStateManager.cs`: Detects unsaved changes and coordinates save/rollback choices.
- `EventManager.cs`: Subscribes to unit-of-work events and triggers block/field/record callbacks.
- `RelationshipManager.cs`: Maintains master/detail relations and synchronizes dependent blocks.
- `PerformanceManager.cs`: Caches block metadata and exposes cache/performance statistics.
- `FormsSimulationHelper.cs`: Emulates form variable, sequence, and field behaviors.

## Runtime Flow
1. Event manager subscribes to unit-of-work event streams.
2. Dirty state manager tracks changes across configured blocks.
3. Relationship manager propagates master changes to detail blocks.
4. Performance manager caches high-frequency block metadata.

## Extension Guidelines
- Keep block-name conventions stable across managers.
- Avoid direct UI dependencies in helper logic.
- Ensure save/rollback orchestration is cancellation-aware.

## Testing Focus
- Dirty-state propagation across related blocks.
- Event subscription and unsubscribe correctness.
- Master/detail synchronization under concurrent edits.
