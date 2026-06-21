# FormsManager — Functional Matrix

This document is a tabular reference of every public type and capability in `FormsManager`. It is the "what can I call and where does it live" companion to the README's quick-start examples.

The matrix has four parts:

1. **[`FormsManager` public surface by concern](#1-formsmanger-public-surface-by-concern)** — every public method/property/event, grouped by what it does.
2. **[Helper manager property index](#2-helper-manager-property-index)** — the 18 helper properties exposed on `FormsManager`.
3. **[Event index](#3-event-index)** — every event the engine raises, grouped by which helper raises it.
4. **[Helper-to-partial map](#4-helper-to-partial-map)** — which partial class wires up which helper.

## 1. `FormsManager` public surface by concern

### Block registration and lifecycle

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `RegisterBlock(string, IUnitofWork, string?, bool)` | BlockRegistration | no | void |
| `RegisterBlock(string, IUnitofWork, IEntityStructure, string?, bool)` | BlockRegistration | no | void |
| `RegisterBlockFromSourceAsync(string, string, string, bool, CancellationToken)` | BlockRegistration | yes | `Task<bool>` |
| `SetupBlockAsync(...)` (alias of above) | BlockRegistration | yes | `Task<bool>` |
| `UnregisterBlock(string)` | BlockRegistration | no | bool |
| `GetBlock(string)` | BlockRegistration | no | `DataBlockInfo` |
| `GetBlock<T>(string)` | GenericOperations | no | `DataBlockInfo` |
| `GetUnitOfWork(string)` | BlockRegistration | no | `IUnitofWork` |
| `BlockExists(string)` | BlockRegistration | no | bool |
| `BlockCount` (property) | Properties | — | int |

### Form lifecycle

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `OpenFormAsync(string)` | FormOperations | yes | `Task<bool>` |
| `CloseFormAsync()` | FormOperations | yes | `Task<bool>` |
| `CallFormAsync(string, Dictionary, FormCallMode)` | MultiFormNavigation | yes | `Task<bool>` |
| `OpenFormAsync(string, Dictionary)` | MultiFormNavigation | yes | `Task<bool>` (modeless) |
| `NewFormAsync(string)` | MultiFormNavigation | yes | `Task<bool>` |
| `ReturnToCallerAsync(object?)` | MultiFormNavigation | yes | `Task<bool>` |
| `CommitFormAsync()` | FormOperations | yes | `Task<IErrorsInfo>` |
| `RollbackFormAsync()` | FormOperations | yes | `Task<IErrorsInfo>` |
| `ClearAllBlocksAsync()` | FormOperations | yes | `Task` |
| `ClearBlockAsync(string)` | FormOperations | yes | `Task<bool>` |
| `ValidateForm()` | FormOperations | no | bool |
| `IsFormReadyForModeTransitionAsync()` | ModeTransitions | yes | `Task<bool>` |

### Block navigation

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `SwitchToBlockAsync(string)` | Navigation | yes | `Task<bool>` |
| `GoBlockAsync(string)` (alias) | Navigation | yes | `Task<bool>` |
| `FirstBlock` / `LastBlock` (via built-ins) | — | — | — |

### Record navigation

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `FirstRecordAsync(string)` | Navigation | yes | `Task<bool>` |
| `NextRecordAsync(string)` | Navigation | yes | `Task<bool>` |
| `PreviousRecordAsync(string)` | Navigation | yes | `Task<bool>` |
| `LastRecordAsync(string)` | Navigation | yes | `Task<bool>` |
| `NavigateToRecordAsync(string, int)` | Navigation | yes | `Task<bool>` |
| `GoRecordAsync(string, int)` (alias) | Navigation | yes | `Task<bool>` |
| `NavigateBackAsync(string)` | DataOperations | yes | `Task<bool>` |
| `NavigateForwardAsync(string)` | DataOperations | yes | `Task<bool>` |
| `CanNavigateBack(string)` | DataOperations | no | bool |
| `CanNavigateForward(string)` | DataOperations | no | bool |
| `GetNavigationHistory(string)` | DataOperations | no | `IReadOnlyList<NavigationHistoryEntry>` |
| `ClearNavigationHistory(string)` | DataOperations | no | void |
| `GetCurrentRecordInfo(string)` | Navigation | no | `NavigationInfo` |
| `GetAllNavigationInfo()` | Navigation | no | `Dictionary<string, NavigationInfo>` |
| `GetCurrentRecord(string)` | EnhancedOperations | no | object |
| `GetRecordCount(string)` | EnhancedOperations | no | int |

### Item navigation

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `GoItemAsync(string, string)` | Navigation | yes | `Task<bool>` |
| `NextItemAsync(string, string?)` | Navigation | yes | `Task<bool>` |
| `PreviousItemAsync(string, string?)` | Navigation | yes | `Task<bool>` |
| `NextBlockAsync()` | Navigation | yes | `Task<bool>` |
| `PreviousBlockAsync()` | Navigation | yes | `Task<bool>` |

### Mode transitions

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `EnterQueryModeAsync(string)` | ModeTransitions | yes | `Task<IErrorsInfo>` |
| `EnterQueryAsync(string)` (alias) | BasicDataOps | yes | `Task<bool>` |
| `ExecuteQueryAsync(string, List<AppFilter>?)` | BasicDataOps | yes | `Task<bool>` |
| `ExecuteQueryAndEnterCrudModeAsync(string, List<AppFilter>?)` | ModeTransitions | yes | `Task<IErrorsInfo>` |
| `ExecuteQueryEnhancedAsync(string, List<AppFilter>?)` | EnhancedOperations | yes | `Task<IErrorsInfo>` |
| `EnterCrudModeForNewRecordAsync(string)` | ModeTransitions | yes | `Task<IErrorsInfo>` |
| `CreateNewRecordInMasterBlockAsync(string)` | ModeTransitions | yes | `Task<IErrorsInfo>` |
| `ValidateAllBlocksForModeTransitionAsync()` | ModeTransitions | yes | `Task<IErrorsInfo>` |
| `GetBlockMode(string)` | ModeTransitions | no | `DataBlockMode` |
| `GetAllBlockModeInfo()` | ModeTransitions | no | `Dictionary<string, BlockModeInfo>` |

### DML

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `InsertRecordAsync(string, object?)` | BasicDataOps | yes | `Task<bool>` |
| `InsertRecordAsync<T>(string, T)` | GenericOperations | yes | `Task<bool>` |
| `InsertRecordEnhancedAsync(string, object?)` | EnhancedOperations | yes | `Task<IErrorsInfo>` |
| `UpdateCurrentRecordAsync(string)` | EnhancedOperations | yes | `Task<IErrorsInfo>` |
| `DeleteCurrentRecordAsync(string)` | BasicDataOps | yes | `Task<bool>` |
| `CreateNewRecord(string)` | EnhancedOperations | no | object |
| `DuplicateCurrentRecordAsync(string, CancellationToken)` | DataOperations | yes | `Task<bool>` |
| `CopyFields(object, object, params string[])` | EnhancedOperations | no | bool |
| `CloneBlockDataAsync(string, string, CancellationToken)` | DataOperations | yes | `Task<bool>` |
| `ApplyAuditDefaults(object, string?)` | EnhancedOperations | no | void |
| `SetFieldValue(object, string, object)` | FormsSimulation | no | bool |
| `GetFieldValue(object, string)` | FormsSimulation | no | object |

### Triggers

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `RegisterKeyTrigger(KeyTriggerDescriptor)` | KeyTriggers | no | void |
| `RegisterKeyTriggerAsync(KeyTriggerDescriptor)` | KeyTriggers | no | void |
| `FireKeyTriggerAsync(string, string?, IDictionary?)` | KeyTriggers | yes | `Task<bool>` |
| `FireOnInsertAsync(string, object)` | DmlTriggers | yes | `Task<bool?>` |
| `FireOnUpdateAsync(string, object)` | DmlTriggers | yes | `Task<bool?>` |
| `FireOnDeleteAsync(string, object)` | DmlTriggers | yes | `Task<bool?>` |
| `RaiseFormTriggerAsync(string, string?)` | DmlTriggers | yes | `Task<TriggerResult>` |
| `FireTriggersInOrderAsync(string, List<TriggerDefinition>, IDictionary?)` | TriggerChaining | yes | `Task<IReadOnlyList<TriggerResult>>` |
| `GetTriggerLog()` | TriggerChaining | no | `IReadOnlyList<TriggerExecutionLogEntry>` |
| `ClearTriggerLog()` | TriggerChaining | no | void |

### Validation

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `ValidateField(string, string, object)` | Validation | no | bool |
| `ValidateBlock(string)` | Validation | no | bool |
| `RegisterCrossBlockRule(CrossBlockValidationRule)` | DataOperations | no | void |
| `UnregisterCrossBlockRule(string)` | DataOperations | no | bool |
| `ValidateCrossBlock()` | DataOperations | no | `IReadOnlyList<string>` |

### LOV

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `ShowLOVAsync(string, string, string?, object?)` | GenericOperations | yes | `Task<LOVResult>` |

### Block properties

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `SetBlockProperty(string, BlockProperty, object)` | BlockProperties | no | void |
| `GetBlockProperty(string, BlockProperty)` | BlockProperties | no | object |
| `SetInsertAllowed(string, bool)` | BlockProperties | no | void |
| `SetUpdateAllowed(string, bool)` | BlockProperties | no | void |
| `SetDeleteAllowed(string, bool)` | BlockProperties | no | void |
| `SetQueryAllowed(string, bool)` | BlockProperties | no | void |
| `SetDefaultWhere(string, string)` | BlockProperties | no | void |
| `SetOrderBy(string, string)` | BlockProperties | no | void |

### Item properties (via `FormsManager.ItemProperties`)

See [`functionality/item-properties.md`](functionality/item-properties.md) for the full surface of `IItemPropertyManager`. Exposed property:

- `FormsManager.ItemProperties` → `IItemPropertyManager`

### Alerts and messaging

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `SetMessage(string, MessageLevel)` | Alerts | no | void |
| `ClearMessage()` | Alerts | no | void |
| `ShowAlertAsync(string, string, BeepBuiltinAlertStyle, string, string?, string?, CancellationToken)` | Alerts | yes | `Task<AlertResult>` |
| `ShowInfoAsync(string, string, CancellationToken)` | Alerts | yes | `Task<AlertResult>` |
| `ConfirmAsync(string, string, CancellationToken)` | Alerts | yes | `Task<bool>` |
| `CurrentMessage` (property) | Alerts | — | `StatusMessage` |

### Sequences

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `GetNextSequence(string)` | Sequences | no | long |
| `PeekNextSequence(string)` | Sequences | no | long |
| `ResetSequence(string, long)` | Sequences | no | void |
| `CreateSequence(string, long, long)` | Sequences | no | void |
| `SetItemDefault(string, string, Func<object>)` | Sequences | no | void |
| `ClearItemDefault(string, string)` | Sequences | no | void |
| `ApplyItemDefaults(string, object)` | Sequences | no | void |
| `CopyFieldValue(string, string, string, string)` | Sequences | no | void |
| `PopulateGroupFromBlock(string)` | Sequences | no | `Dictionary<string, object>` |

### Timers

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `CreateTimer(string, TimeSpan, bool)` | Timers | no | `TimerDefinition` |
| `DeleteTimer(string)` | Timers | no | bool |
| `GetTimer(string)` | Timers | no | `TimerDefinition` |
| `GetAllTimers()` | Timers | no | `IReadOnlyList<TimerDefinition>` |
| `TimerExists(string)` | Timers | no | bool |

### Multi-form and globals

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `SetGlobalVariable(string, object)` | InterFormComm | no | void |
| `GetGlobalVariable(string)` | InterFormComm | no | object |
| `SendParameterToForm(string, string, object)` | InterFormComm | no | bool |
| `PostMessage(string, string, object?)` | InterFormComm | no | void |
| `BroadcastMessage(string, object?)` | InterFormComm | no | void |
| `SubscribeToMessage(string, Action<FormMessage>)` | InterFormComm | no | void |
| `UnsubscribeFromMessage(string)` | InterFormComm | no | void |
| `CreateSharedBlock(string, IUnitofWork)` | InterFormComm | no | bool |
| `GetSharedBlock(string)` | InterFormComm | no | `IUnitofWork` |
| `TryLockSharedBlock(string, TimeSpan)` | InterFormComm | no | bool |
| `ReleaseSharedBlockLock(string)` | InterFormComm | no | void |
| `GetCallStack()` | MultiFormNavigation | no | `IReadOnlyList<FormCallStackEntry>` |
| `GetFormParameter(string)` | MultiFormNavigation | no | object |

### Master/Detail

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `CreateMasterDetailRelation(string, string, string, string)` | Relationships | no | void |
| `SynchronizeDetailBlocksAsync(string)` | Relationships | yes | `Task` |
| `GetDetailBlocks(string)` | Relationships | no | `List<string>` |
| `GetMasterBlock(string)` | Relationships | no | string |

### Security

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `SetSecurityContext(SecurityContext)` | Security | no | void |
| `SetBlockSecurity(string, BlockSecurity)` | Security | no | void |
| `GetBlockSecurity(string)` | Security | no | `BlockSecurity` |
| `IsBlockAllowed(string, SecurityPermission)` | Security | no | bool |
| `SetFieldSecurity(string, string, FieldSecurity)` | Security | no | void |
| `GetFieldSecurity(string, string)` | Security | no | `FieldSecurity` |
| `GetMaskedFieldValue(string, string, object)` | Security | no | object |
| `GetSecurityViolations()` | Security | no | `IReadOnlyList<SecurityViolationEventArgs>` |
| `SecurityContext` (property) | Security | — | `SecurityContext` |

### Audit

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `SetAuditUser(string)` | Audit | no | void |
| `ConfigureAudit(Action<AuditConfiguration>)` | Audit | no | void |
| `GetAuditLog(string?)` | Audit | no | `IReadOnlyList<AuditEntry>` |
| `GetFieldHistory(string, string)` | Audit | no | `IReadOnlyList<AuditFieldChange>` |
| `ExportAuditToCsvAsync(string, string?)` | Audit | yes | `Task` |
| `ExportAuditToJsonAsync(string, string?)` | Audit | yes | `Task` |
| `PurgeAudit(int)` | Audit | no | void |
| `ClearAudit()` | Audit | no | void |
| `AuditManager` (property) | Audit | — | `IAuditManager` |

### Performance / paging

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `SetBlockPageSize(string, int)` | Performance | no | void |
| `LoadPageAsync(string, int, CancellationToken)` | Performance | yes | `Task<PageInfo>` |
| `GetTotalRecordCount(string)` | Performance | no | long |
| `SetTotalRecordCount(string, long)` | Performance | no | void |
| `SetFetchAheadDepth(string, int)` | Performance | no | void |
| `SetLazyLoadMode(string, LazyLoadMode)` | Performance | no | void |
| `GetLazyLoadMode(string)` | Performance | no | `LazyLoadMode` |
| `SetMaxRecordsPerFetch(string, int)` | Performance | no | void |
| `InvalidateBlockCache(string)` | Performance | no | void |
| `SetBlockCacheTtl(string, TimeSpan)` | Performance | no | void |
| `GetCacheStats()` | Performance | no | `CacheStats` |
| `CheckCacheMemoryPressure(long)` | Performance | no | void |

### Undo/redo / data ops

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `SetBlockUndoEnabled(string, bool, int)` | DataOperations | no | void |
| `UndoBlock(string)` | DataOperations | no | bool |
| `RedoBlock(string)` | DataOperations | no | bool |
| `CanUndoBlock(string)` | DataOperations | no | bool |
| `CanRedoBlock(string)` | DataOperations | no | bool |
| `GetBlockChangeSummary(string)` | DataOperations | no | `ChangeSummary` |
| `GetFormChangeSummary()` | DataOperations | no | `IReadOnlyDictionary<string, ChangeSummary>` |
| `RefreshBlockAsync(string, bool, CancellationToken)` | DataOperations | yes | `Task<bool>` |
| `RevertCurrentRecord(string)` | DataOperations | no | bool |
| `RevertRecord(string, int)` | DataOperations | no | bool |
| `GetBlockQueryHistory(string)` | DataOperations | no | `IReadOnlyList<QueryHistoryEntry>` |
| `ClearBlockQueryHistory(string)` | DataOperations | no | void |
| `GetBlockSum(string, string)` | DataOperations | no | decimal |
| `GetBlockAverage(string, string)` | DataOperations | no | decimal |
| `GetBlockCount(string, Func<object,bool>?)` | DataOperations | no | int |
| `GetBlockGroups(string, string)` | DataOperations | no | `IReadOnlyList<ItemGroup<object>>` |
| `CommitFormBatchAsync(string?, CancellationToken)` | DataOperations | yes | `Task<CommitBatchResult>` |
| `CommitBlockBatchAsync(string, CancellationToken)` | DataOperations | yes | `Task<CommitBatchResult>` |
| `ExportBlockToJsonAsync(string, Stream, CancellationToken)` | DataOperations | yes | `Task` |
| `ExportBlockToCsvAsync(string, Stream, CancellationToken)` | DataOperations | yes | `Task` |
| `GetBlockAsDataTable(string)` | DataOperations | no | `DataTable` |
| `ImportBlockFromJsonAsync(string, Stream, bool, CancellationToken)` | DataOperations | yes | `Task<int>` |
| `ImportBlockFromCsvAsync(string, Stream, bool, CancellationToken)` | DataOperations | yes | `Task<int>` |

### Savepoints and state

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `CreateBlockSavepoint(string, string?)` | BlockRegistration | no | string |
| `RollbackToSavepointAsync(string, string, CancellationToken)` | BlockRegistration | yes | `Task<bool>` |
| `SaveFormState()` | DataOperations | no | `FormStateSnapshot` |
| `RestoreFormStateAsync(FormStateSnapshot, CancellationToken)` | DataOperations | yes | `Task<bool>` |

### Dirty state

| Method | File | Async? | Returns |
| --- | --- | --- | --- |
| `CheckAndHandleUnsavedChangesAsync(string)` | DirtyState | yes | `Task<bool>` |
| `HasUnsavedChanges()` | DirtyState | no | bool |
| `GetDirtyBlocks()` | DirtyState | no | `List<string>` |

### System variables (via `FormsManager.SystemVariables`)

See [`functionality/system-variables.md`](functionality/system-variables.md). Exposed property:

- `FormsManager.SystemVariables` → `ISystemVariablesManager`

## 2. Helper manager property index

These are the 18 helper properties exposed on `FormsManager`. Each one is a manager for one concern, all of which are DI-injectable.

| Property | Type | Owns |
| --- | --- | --- |
| `DirtyStateManager` | `IDirtyStateManager` | Per-block dirty tracking |
| `PerformanceManager` | `IPerformanceManager` | Caching + metrics |
| `SystemVariables` | `ISystemVariablesManager` | `:SYSTEM.*` emulation |
| `Validation` | `IValidationManager` | Field/record/cross-block validation |
| `LOV` | `ILOVManager` | List-of-values orchestration |
| `ItemProperties` | `IItemPropertyManager` | `SET_ITEM_PROPERTY` family |
| `Triggers` | `ITriggerManager` | Trigger engine |
| `Savepoints` | `ISavepointManager` | Block savepoints |
| `Locking` | `ILockManager` | Per-block locks |
| `QueryBuilder` | `IQueryBuilderManager` | Filter composition |
| `ErrorLog` | `IBlockErrorLog` | Per-block error log |
| `Messages` | `IMessageQueueManager` | Per-block message queue |
| `BlockFactory` | `IBlockFactory` | Block-from-source factory |
| `BlockProperties` | `IBlockPropertyManager` | Block-level property bag |
| `AlertProvider` | `IAlertProvider` | Alert rendering (pluggable) |
| `Sequences` | `ISequenceProvider` | In-memory sequences |
| `Timers` | `ITimerManager` | Timers + WHEN-TIMER-EXPIRED |
| `Registry` | `IFormRegistry` | Multi-form registry |
| `MessageBus` | `IFormMessageBus` | Inter-form messaging |
| `SharedBlocks` | `ISharedBlockManager` | Cross-form shared blocks |
| `Security` | `ISecurityManager` | Block/field security + masking |
| `Paging` | `IPagingManager` | Page state |
| `TriggerLog` | `ITriggerExecutionLog` | Trigger execution log |
| `TriggerDependencies` | `ITriggerDependencyManager` | Trigger ordering + cycle detection |
| `AuditManager` | `IAuditManager` | Audit capture + storage |
| `Configuration` | `UnitofWorksManagerConfiguration` | Top-level config DTO |

## 2.5 Cross-cutting infrastructure (static helpers)

These are static helpers used by the engine internally. They are not DI-injectable and not exposed as `FormsManager` properties.

| Helper | Where | Purpose |
| --- | --- | --- |
| `RecordPropertyAccessor` | `Helpers/RecordPropertyAccessor.cs` | Centralized reflection-based property accessor for `DataBlockField.FieldName` lookups. Process-wide, type-keyed `PropertyInfo` cache + negative cache + throttled diagnostic logging. Replaces ~12 ad-hoc `record.GetType().GetProperty(...)` sites across the engine. Used by `FormsManager.Helpers.cs`, `FormsManager.BlockRegistration.cs`, `FormsManager.DataOperations.cs`, `FormsManager.Sequences.cs`, `FormsManager.Validation.cs`, and `Helpers/FormsSimulationHelper.cs`. |

## 3. Event index

54 events total across the folder. Orchestrator-level events are on `FormsManager` itself; helper-level events are on the helper properties.

### Orchestrator events

| Event | Args | Raised when |
| --- | --- | --- |
| `OnFormOpen` | `FormTriggerEventArgs` | Before form open completes (cancellable). |
| `OnFormClose` | `FormTriggerEventArgs` | Around form close, including unsaved-change handling. |
| `OnFormCommit` | `FormTriggerEventArgs` | Around form commit processing. |
| `OnFormRollback` | `FormTriggerEventArgs` | Around form rollback processing. |
| `OnFormValidate` | `FormTriggerEventArgs` | When form-level validation is requested. |
| `OnNavigate` | `NavigationTriggerEventArgs` | Before a navigation operation. |
| `OnCurrentChanged` | `NavigationTriggerEventArgs` | After the current record changes. |
| `OnBlockFieldChanged` | `BlockFieldChangedEventArgs` | Per-field change feed. |

### Helper events

| Helper | Event | Args |
| --- | --- | --- |
| `EventManager` | `OnBlockEnter` | `BlockTriggerEventArgs` |
| `EventManager` | `OnBlockLeave` | `BlockTriggerEventArgs` |
| `EventManager` | `OnBlockClear` | `BlockTriggerEventArgs` |
| `EventManager` | `OnBlockValidate` | `BlockTriggerEventArgs` |
| `EventManager` | `OnRecordEnter` | `RecordTriggerEventArgs` |
| `EventManager` | `OnRecordLeave` | `RecordTriggerEventArgs` |
| `EventManager` | `OnRecordValidate` | `RecordTriggerEventArgs` |
| `EventManager` | `OnPreQuery` / `OnPostQuery` | `DMLTriggerEventArgs` |
| `EventManager` | `OnPreInsert` / `OnPostInsert` | `DMLTriggerEventArgs` |
| `EventManager` | `OnPreUpdate` / `OnPostUpdate` | `DMLTriggerEventArgs` |
| `EventManager` | `OnPreDelete` / `OnPostDelete` | `DMLTriggerEventArgs` |
| `EventManager` | `OnPreCommit` / `OnPostCommit` | `DMLTriggerEventArgs` |
| `EventManager` | `OnValidateField` / `OnValidateRecord` / `OnValidateForm` | `ValidationTriggerEventArgs` |
| `EventManager` | `OnError` | `ErrorTriggerEventArgs` |
| `ValidationManager` | `ValidationFailed` | `ValidationFailedEventArgs` |
| `ValidationManager` | `ValidationStarting` | `ValidationStartingEventArgs` |
| `ValidationManager` | `ValidationCompleted` | `ValidationCompletedEventArgs` |
| `TriggerManager` | `TriggerExecuting` | `TriggerExecutingEventArgs` |
| `TriggerManager` | `TriggerExecuted` | `TriggerExecutedEventArgs` |
| `TriggerManager` | `TriggerRegistered` / `TriggerUnregistered` | — |
| `TriggerManager` | `TriggerChainCompleted` | `TriggerChainCompletedEventArgs` |
| `LOVManager` | `LOVDataLoaded` | `LOVDataLoadedEventArgs` |
| `LOVManager` | `LOVValidationFailed` | `LOVValidationEventArgs` |
| `ItemPropertyManager` | `ItemPropertyChanged` | `ItemPropertyChangedEventArgs` |
| `ItemPropertyManager` | `ItemValueChanged` | `ItemValueChangedEventArgs` |
| `ItemPropertyManager` | `ItemErrorChanged` | `ItemErrorEventArgs` |
| `DirtyStateManager` | `OnUnsavedChanges` | `UnsavedChangesEventArgs` |
| `BlockErrorLog` | `OnError` / `OnWarning` | `BlockErrorEventArgs` |
| `TimerManager` | `TimerFired` | `TimerFiredEventArgs` |
| `MessageQueueManager` | `OnMessage` / `OnMessageCleared` | `BlockMessageEventArgs` |
| `FormRegistry` | `FormLifecycleChanged` | `FormLifecycleEventArgs` |
| `FormMessageBus` | `OnFormMessage` | `FormMessageEventArgs` |
| `SharedBlockManager` | `SharedBlockChanged` | `SharedBlockChangedEventArgs` |
| `SecurityManager` | `OnSecurityViolation` | `SecurityViolationEventArgs` |

## 4. Helper-to-partial map

Which partial class wires up which helper (i.e. exposes the property on `FormsManager`).

| Partial | Helpers exposed |
| --- | --- |
| `Properties.cs` | 16 helpers (DirtyStateManager, PerformanceManager, SystemVariables, Validation, LOV, ItemProperties, Triggers, Savepoints, Locking, QueryBuilder, ErrorLog, Messages, BlockFactory, BlockProperties, AlertProvider, Sequences, Timers, Registry, MessageBus, SharedBlocks, Configuration) + readonly state (DMEEditor, Blocks, IsDirty, Status, BlockCount) |
| `Performance.cs` | `Paging` |
| `Security.cs` | `Security`, `SecurityContext` |
| `Audit.cs` | `AuditManager` |
| `TriggerChaining.cs` | `TriggerLog`, `TriggerDependencies` |
| `Alerts.cs` | `CurrentMessage` |
| `Locks.cs` (n/a) | — |

The Core partial (`FormsManager.Core.cs`) holds the field declarations and the constructor that wires all of them up.
