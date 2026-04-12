# Forms Helpers

This folder contains the helper implementations that FormsManager composes into a UI-agnostic runtime. Most callers interact with them through FormsManager properties rather than constructing them directly.

## Caller-facing helper groups

### Core orchestration helpers

- `DirtyStateManager.cs`: checks unsaved changes, collects related dirty blocks, and coordinates save or rollback decisions.
- `EventManager.cs`: subscribes to UoW event streams and routes them into block, record, field, and error callbacks.
- `BlockFactory.cs`: creates UoW and `EntityStructure` pairs from configured connections.
- `BlockErrorLog.cs`: captures block-scoped runtime errors without taking a UI dependency.

### Relationship and data-shaping helpers

- `LOVManager.cs`: registers LOV definitions, loads datasource records, caches results, filters client-side, validates values, and extracts related-field values.
- `QueryBuilderManager.cs`: builds reusable filter and query definitions for blocks.
- `PagingManager.cs`: tracks page size, current page, total-record count, and fetch-ahead depth. It does not load data by itself.

### Validation, triggers, and interaction helpers

- `ValidationManager.cs`: item, record, block, and form validation rules with fluent registration support.
- `TriggerManager.cs`: trigger registration, ordering, async execution, execution statistics, and scope-aware lookup.
- `TriggerLibrary.cs`: common trigger factories such as audit stamping, auto-number, cascade delete, and field formatting.
- `ItemPropertyManager.cs`: item metadata such as enabled, visible, read-only, and tab-order behavior.
- `MessageQueueManager.cs`: status and informational message queue for UI consumers.
- `TimerManager.cs`: named timers that feed `WHEN-TIMER-EXPIRED` through FormsManager.

### Transactional state helpers

- `SavepointManager.cs`: named record snapshots and rollback targets.
- `LockManager.cs`: client-side current-record locking and auto-lock behavior.
- `CrossBlockValidationManager.cs`: commit-time rules spanning multiple blocks.

### Cross-form and platform helpers

- `FormRegistry.cs`: active-form registry used by modal and modeless multi-form navigation.
- `FormMessageBus.cs`: publish, subscribe, and broadcast form messages.
- `SharedBlockManager.cs`: exposes shared UoW-backed blocks across form instances.
- `DefaultAlertProvider.cs`: fallback alert implementation behind `ShowAlertAsync`.

### Audit and security helpers

- `AuditManager.cs`: captures field-level changes from the FormsManager change feed and flushes grouped audit entries.
- `InMemoryAuditStore.cs`: bounded in-memory audit storage.
- `FileAuditStore.cs`: JSON-backed audit storage for persisted session history.
- `SecurityManager.cs`: block security, field security, masking, and violation logging.

## Important behavior notes

### LOV return-value handling

`LOVManager.GetRelatedFieldValues(...)` uses an internal `__RETURN_VALUE__` key for the selected return field. Callers should prefer `FormsManager.ShowLOVAsync(...)` when applying a selected row, because FormsManager translates that sentinel back to the actual bound field name before setting values on the current record.

### Paging is state, not data loading

`PagingManager` computes page positions and stores total-record counts. It does not execute datasource queries. The typical flow is:

1. Set page size and total-record count.
2. Call `LoadPageAsync(...)` to move paging state and navigate the UoW cursor.
3. Re-execute the block query if server-side fetching is part of your UI flow.

### Security updates both metadata and enforcement

`SecurityManager` not only records rules, it also pushes effective flags into `DataBlockInfo` and `ItemPropertyManager` through FormsManager. CRUD entry points still perform runtime enforcement, so UI-layer disabling is not the only protection.

### Audit capture is tied to the change feed

Audit capture is driven from FormsManager's `OnBlockFieldChanged` event and then grouped into `AuditEntry` records on flush. If a change bypasses FormsManager field mutation paths, it may also bypass the audit helper unless the UoW or caller emits compatible notifications.

### Relationship ownership stays in FormsManager

The old standalone relationship helper abstraction is no longer the authoritative owner of master/detail behavior. Relationship metadata, key resolution, and detail synchronization are FormsManager concerns.

## Testing focus

- Dirty-state propagation across related blocks.
- Trigger ordering, async execution, and failure handling.
- Validation timing and cross-block commit blocking.
- LOV datasource-backed loads, cache reuse, and selected-record field population.
- Savepoint restore and lock cleanup behavior.
- Paging-state transitions without breaking current-record semantics.