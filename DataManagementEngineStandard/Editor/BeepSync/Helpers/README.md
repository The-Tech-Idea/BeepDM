# BeepSync Helpers

## Purpose
This folder provides concrete helper logic that powers BeepSync data and schema synchronization.

## Key Files
- `FieldMappingHelper.cs`: Field pairing, mapping normalization, and mapping validation support.
- `SyncValidationHelper.cs`: Compatibility checks before synchronization starts.
- `SyncProgressHelper.cs`: Progress messaging and status aggregation.
- `SchemaPersistenceHelper.cs`: Persistence of schema snapshots/checkpoints.

## Runtime Flow
1. Build or load source-to-target field mappings.
2. Validate mapping and schema compatibility.
3. Execute sync operations while emitting progress updates.
4. Persist resulting schema state and metadata snapshots.

## Extension Guidelines
- Keep validation side-effect free so checks can run multiple times.
- Use stable mapping keys to preserve incremental sync behavior.
- Persist schema updates atomically when possible.
