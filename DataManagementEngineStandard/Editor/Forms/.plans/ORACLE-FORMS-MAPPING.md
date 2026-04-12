# Oracle Forms to FormsManager Mapping

This document maps Oracle Forms runtime concepts to the current BeepDM `FormsManager` surface. It is focused on behavior and ownership, not UI layout. Canvases, tab pages, focus rendering, and LOV dialog presentation still belong to the host UI layer.

## Audit Status

- Audit date: 2026-04-09.
- Use this file as the runtime coverage baseline for FormsManager. Entries in the mapping tables below reflect APIs implemented in code unless a note explicitly says the concern remains UI-owned or documentation-only.
- `Help/formsmanager.html` was aligned with this mapping during the 2026-04-09 rewrite, so the help site and mapping document now describe the same audited runtime surface.

## Runtime ownership rules

- FormsManager owns orchestration: block registration, navigation, mode transitions, master/detail synchronization, triggers, LOV application, audit/security coordination, and multi-form plumbing.
- IUnitofWork owns persistence: query execution, loaded records, dirty-state truth, insert/update/delete execution, commit, rollback, and datasource-backed identity refresh.
- Sequence and identity allocation belong to create and insert flows only. Query, paging, navigation, and prefetch paths must not allocate keys.

## Built-in mapping

| Oracle Forms built-in or concept | FormsManager API | Notes |
| --- | --- | --- |
| `OPEN_FORM` (current form lifecycle) | `OpenFormAsync(string formName)` | Initializes the current form runtime |
| `CALL_FORM` | `CallFormAsync(string formName, Dictionary<string, object> parameters = null, FormCallMode mode = FormCallMode.Modal)` | Modal child-form semantics |
| `OPEN_FORM` (modeless child form) | `OpenFormAsync(string formName, Dictionary<string, object> parameters = null)` | Modeless overload backed by the registry |
| `NEW_FORM` | `NewFormAsync` | Replaces the current form in the registry |
| `RETURN` / exit to caller | `ReturnToCallerAsync` | Can pass return data back to caller |
| `ENTER_QUERY` | `EnterQueryModeAsync` | Per-block query-mode transition |
| `EXECUTE_QUERY` | `ExecuteQueryAndEnterCrudModeAsync`, `ExecuteQueryAsync` | Query execution with optional CRUD-mode return |
| `COMMIT_FORM` | `CommitFormAsync` | Coordinates validation, dirty-state save, audit, and lock cleanup |
| `ROLLBACK_FORM` | `RollbackFormAsync` | Rolls back dirty blocks and clears transient state |
| `CLEAR_FORM` | `ClearAllBlocksAsync` | Clears all registered blocks |
| `CLEAR_BLOCK` | `ClearBlockAsync` | Clears a single block |
| `GO_BLOCK` | `SwitchToBlockAsync` | Changes current block context |
| `GO_RECORD` | `NavigateToRecordAsync` | Direct record navigation |
| `FIRST_RECORD` | `FirstRecordAsync` | Record navigation |
| `NEXT_RECORD` | `NextRecordAsync` | Record navigation |
| `PREVIOUS_RECORD` | `PreviousRecordAsync` | Record navigation |
| `LAST_RECORD` | `LastRecordAsync` | Record navigation |
| `SHOW_LOV` | `ShowLOVAsync` plus `LOV.RegisterLOV` | Data loading and application are runtime concerns; dialog rendering is UI-owned |
| `SET_BLOCK_PROPERTY` | `SetBlockProperty`, `SetInsertAllowed`, `SetUpdateAllowed`, `SetDeleteAllowed`, `SetQueryAllowed` | Stored on `DataBlockInfo` and block-property helper |
| `GET_BLOCK_PROPERTY` | `GetBlockProperty`, `GetBlockProperty<T>` | Returns runtime block metadata |
| `DEFAULT_WHERE` | `SetDefaultWhere` | Appends a default query filter for the block |
| `ORDER_BY` | `SetOrderBy` | Sets default ordering for the block |
| `MESSAGE` | `SetMessage`, `ClearMessage` | UI status area remains caller-owned |
| `SHOW_ALERT` | `ShowAlertAsync`, `ConfirmAsync`, `ShowInfoAsync` | Backed by injected `IAlertProvider` |
| `COPY` | `CopyFieldValue` | Copies current field value across blocks |
| `DEFAULT_VALUE` | `SetItemDefault` | Applied during new-record flow |
| `:SEQUENCE.NEXTVAL` | `GetNextSequence`, `PeekNextSequence`, `ResetSequence`, `CreateSequence` | Prefer datasource or UoW sequence generation first |
| `CREATE_TIMER` | `CreateTimer` | Timer expiry raises `WHEN-TIMER-EXPIRED` |
| `DELETE_TIMER` | `DeleteTimer` | Removes a timer |
| `GET_TIMER` | `GetTimer`, `GetAllTimers` | Timer inspection |
| `:SYSTEM.*` | `SystemVariables` | System-variable emulation surface |
| `:GLOBAL.*` | `SetGlobalVariable`, `GetGlobalVariable` | Cross-form global values |
| Multi-form parameter lists | `CallFormAsync`, `OpenFormAsync` overload, `GetFormParameter`, `GetFormParameter<T>` | Parameter handoff to target form |
| Inter-form messaging | `PostMessage`, `SubscribeToMessage`, `UnsubscribeFromMessage` | Shared message bus |
| Shared data blocks | `CreateSharedBlock`, `SharedBlocks` | Cross-form block visibility |

## Trigger mapping

| Oracle Forms trigger family | FormsManager surface | Notes |
| --- | --- | --- |
| PRE/POST DML triggers | `Triggers` manager and DML trigger partials | Used for insert, update, delete, query, and commit pipelines |
| KEY-* triggers | `RegisterKeyTrigger`, `RegisterKeyTriggerAsync`, `FireKeyTriggerAsync` | Includes default key actions such as commit and navigation |
| Trigger libraries | `TriggerLibrary` | Provides reusable audit, auto-number, cascade delete, and formatting triggers |
| Trigger chaining and diagnostics | `FormsManager.TriggerChaining.cs` plus `Triggers` manager | Execution log and dependency graph support |

## Validation, savepoints, and locks

| Oracle Forms concept | FormsManager surface | Notes |
| --- | --- | --- |
| Item, record, block, form validation | `Validation` manager, `ValidateField`, `ValidateBlock`, `ValidateForm` | Shared validation runtime |
| Cross-block validation | `RegisterCrossBlockRule`, `ValidateCrossBlock` | Commit-time orchestration across blocks |
| Savepoints | `Savepoints` manager | Named snapshots and rollback targets |
| Record locking | `Locking` manager | Client-side lock state and auto-lock behavior |

## Audit, security, and paging

| Oracle Forms-style concern | FormsManager surface | Notes |
| --- | --- | --- |
| Audit trail | `SetAuditUser`, `ConfigureAudit`, `GetAuditLog`, `ExportAuditToCsvAsync`, `AuditManager` | Uses field-change feed and pluggable stores |
| Block security | `SetSecurityContext`, `SetBlockSecurity`, `GetBlockSecurity`, `IsBlockAllowed` | Runtime enforcement plus UI flag propagation |
| Field security and masking | `SetFieldSecurity`, `GetFieldSecurity`, `GetMaskedFieldValue` | Visibility, editability, and masking rules |
| Paging | `SetBlockPageSize`, `SetTotalRecordCount`, `LoadPageAsync`, `SetFetchAheadDepth`, `Paging` | Tracks paging state; query loading remains caller-owned |
| Cache management | `InvalidateBlockCache`, `SetBlockCacheTtl`, `GetCacheStats`, `CheckCacheMemoryPressure` | Performance-manager integration |

## Key-generation notes

1. FormsManager preserves explicit keys supplied by caller, default, or trigger.
2. Datasource-managed identities should remain unset until insert or commit refresh completes.
3. Datasource or UoW sequences take priority over the in-memory sequence provider.
4. Use in-memory sequences for Oracle-style built-ins or non-database-backed test flows.
5. Composite keys must be handled field by field.
6. Master/detail synchronization must wait for stable parent keys when identities are generated only after insert.

## Ongoing Hardening Areas

- Reflection-heavy UoW update paths and remote cache invalidation remain hardening areas, not missing core Oracle Forms runtime capabilities.
- Programmatic `RaiseFormTriggerAsync` coverage should continue to be audited where explicit trigger-raise semantics are required.

## UI-layer boundaries

The following Oracle Forms concepts still belong primarily to the host UI layer rather than FormsManager itself:

- canvases and tab pages
- focus rendering and cursor movement between visual controls
- LOV dialog presentation and row selection UI
- keyboard routing from actual UI controls into key-trigger execution
- message-area rendering

FormsManager supplies the runtime state, orchestration, and data semantics that those UI layers consume.