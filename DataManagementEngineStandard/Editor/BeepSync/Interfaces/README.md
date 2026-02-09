# BeepSync Interfaces

## Purpose
This folder defines the synchronization helper contracts used by BeepSync workflows. Implementations in sibling folders must satisfy these contracts for mapping, validation, progress, and schema persistence.

## Key Interfaces
- `IDataSourceHelper`: Data-source interaction contract for sync routines.
- `IFieldMappingHelper`: Field correspondence and mapping rule resolution.
- `ISyncValidationHelper`: Pre-flight validation for sync compatibility.
- `ISyncProgressHelper`: Progress reporting and operational telemetry.
- `ISchemaPersistenceHelper`: Schema persistence and checkpoint behavior.

## Integration Notes
- Keep interfaces provider-agnostic so sync orchestrators remain reusable.
- Additive interface changes are preferred; breaking signature changes require broad downstream updates.
- Ensure cancellation and progress reporting remain available at orchestration boundaries.
