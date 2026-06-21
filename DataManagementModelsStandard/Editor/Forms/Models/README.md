# Forms Models

## Purpose
This folder contains the typed model surface for form workflows: block metadata, relationships, trigger event args, save/rollback options, validation results, and performance snapshots.

## Model Groups
- Block and relationship metadata: `BlockConfiguration`, `DataBlockInfo`, `DataBlockRelationship`, `CachedBlockInfo`.
- Trigger payloads: `FormTriggerEventArgs`, `RecordTriggerEventArgs`, `BlockTriggerEventArgs`, `DMLTriggerEventArgs`, `ValidationTriggerEventArgs`, `NavigationTriggerEventArgs`, `ErrorTriggerEventArgs`.
- Validation and save models: `ValidationResult`, `SaveOptions`, `SaveResult`, `RollbackOptions`.
- Performance and navigation metrics: `PerformanceMetric`, `PerformanceStatistics`, `CacheEfficiencyMetrics`, `NavigationInfo`.
- Supporting enums: `DMLOperation`, `NavigationType`, `UnsavedChangesAction`, `SystemVariableType`, and related severity/state enums.

## Usage Notes
- These models are the canonical message and state contracts between managers and UI layers.
- Prefer extending models with additive fields to maintain wire compatibility.
- Keep enum changes synchronized with switch logic in helpers and managers.
