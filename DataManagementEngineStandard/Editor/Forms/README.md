# FormsManager — Oracle Forms Runtime Engine

`FormsManager` is the BeepDM form-orchestration runtime in the `TheTechIdea.Beep.Editor.UOWManager`
namespace. It implements `IUnitofWorksManager` and coordinates block registration, navigation,
mode transitions, master/detail synchronization, triggers, LOVs, validation, auditing, security,
paging, multi-form communication, alerts, timers, sequences, savepoints, and undo/redo.

The architecture follows the **Oracle Forms runtime** model: FormsManager owns orchestration;
`IUnitofWork` owns persistence. UIs (WinForms, WPF, Blazor) call into FormsManager and provide an
`IBuiltinHost` adapter to receive the `IBeepBuiltins` built-in surface.

---

## 1. File Map — 28 Partial Classes

```
Editor/Forms/
├── FormsManager.Core.cs               Constructor + 24 DI helpers + lifetime
├── FormsManager.Properties.cs         28 readonly public properties
├── FormsManager.BlockRegistration.cs  Register / Unregister / SetupBlock / Savepoints
├── FormsManager.DataOperations.cs     Undo/Redo / Batch commit / Export/Import / Aggregates
├── FormsManager.BasicDataOps.cs       Insert / Delete / EnterQuery / ExecuteQuery
├── FormsManager.Navigation.cs         Record + block navigation / GO_BLOCK / GO_ITEM / NEXT_ITEM
├── FormsManager.ModeTransitions.cs    ENTER_QUERY → EXECUTE_QUERY → CRUD transitions
├── FormsManager.Relationships.cs      Master/detail registration + sync
├── FormsManager.FormOperations.cs     Open / Close / Commit / Rollback / Clear / Validate
├── FormsManager.GenericOperations.cs  Typed block wrappers + ShowLOVAsync
├── FormsManager.EnhancedOperations.cs Insert/update with audit defaults + query enhancement
├── FormsManager.FormsSimulation.cs    SetFieldValue / GetFieldValue / ExecuteSequence
├── FormsManager.DmlTriggers.cs        ON-INSERT / ON-UPDATE / ON-DELETE wrappers
├── FormsManager.KeyTriggers.cs        KEY- trigger registration + default actions
├── FormsManager.TriggerChaining.cs    Trigger execution DAG + execution log
├── FormsManager.Validation.cs         Field + block validation (event + rule-based)
├── FormsManager.Security.cs           Block/field security + row filtering + masking
├── FormsManager.Audit.cs              Audit trail configuration + query + export
├── FormsManager.Alerts.cs             MESSAGE / SHOW_ALERT / BELL equivalents
├── FormsManager.Timers.cs             CREATE_TIMER / DELETE_TIMER
├── FormsManager.Sequences.cs          Sequences + item defaults
├── FormsManager.InterFormComm.cs      :GLOBAL.* / parameters / message bus / shared blocks
├── FormsManager.MultiFormNavigation.cs CALL_FORM / OPEN_FORM / NEW_FORM / return-to-caller
├── FormsManager.BlockProperties.cs    SET_BLOCK_PROPERTY / GET_BLOCK_PROPERTY
├── FormsManager.Performance.cs        Paging / fetch-ahead / lazy load / cache
├── FormsManager.DirtyState.cs         Unsaved-changes detection + handling
├── FormsManager.Lifecycle.cs          Dispose
└── FormsManager.Helpers.cs            Private plumbing (SuppressSync, ResolveEntityType, etc.)
```

---

## 2. Helper Manager Composition (24 DI-injected managers)

FormsManager composes 24 helper managers, each exposed as a property and DI-injected:

| Property | Interface | Responsibility |
|----------|-----------|---------------|
| `DMEEditor` | `IDMEEditor` | Engine root — data sources, connections |
| `DirtyStateManager` | `IDirtyStateManager` | Unsaved change tracking |
| `PerformanceManager` | `IPerformanceManager` | Paging, caching, lazy load |
| `SystemVariables` | `ISystemVariablesManager` | `:SYSTEM.*` variable mirror |
| `Validation` | `IValidationManager` | Field/record/block validation |
| `LOV` | `ILOVManager` | LOV query + display |
| `ItemProperties` | `IItemPropertyManager` | Per-item get/set |
| `Triggers` | `ITriggerManager` | Trigger registration + firing |
| `Savepoints` | `ISavepointManager` | Named block savepoints |
| `Locking` | `ILockManager` | Record locking |
| `QueryBuilder` | `IQueryBuilderManager` | Dynamic query filtering |
| `ErrorLog` | `IBlockErrorLog` | Error logging |
| `Messages` | `IMessageQueueManager` | Per-block message queue |
| `BlockFactory` | `IBlockFactory` | UoW + entity structure resolution |
| `BlockProperties` | `IBlockPropertyManager` | `SET_BLOCK_PROPERTY` routing |
| `AlertProvider` | `IAlertProvider` | Alert dialog surface |
| `Sequences` | `ISequenceProvider` | Auto-increment sequences |
| `Timers` | `ITimerManager` | `CREATE_TIMER` / `DELETE_TIMER` |
| `Registry` | `IFormRegistry` | Multi-form registration |
| `MessageBus` | `IFormMessageBus` | Inter-form messaging |
| `SharedBlocks` | `ISharedBlockManager` | Cross-form shared blocks |
| `Security` | `ISecurityManager` | Block/field permissions |
| `Audit` | `IAuditManager` | Change audit trail |
| `Paging` | `IPagingManager` | Database-level paging |

Plus 4 internal managers:
- `_eventManager` (`IEventManager`) — TriggerBlockEnter/Leave, TriggerFieldValidation, etc.
- `_formsSimulationHelper` (`IFormsSimulationHelper`)
- `_masterDetailKeyResolver` (`MasterDetailKeyResolver`) — FK resolution for relationships
- `_configurationManager` (`IConfigurationManager`) — JSON config persistence

---

## 3. Public API (100+ methods)

### 3.1 Block Registration
| Method | Signature |
|--------|-----------|
| `RegisterBlock` | `(string name, IUnitofWork uow, string dsName, bool isMaster)` |
| `RegisterBlock` (overload) | `(string name, IUnitofWork uow, IEntityStructure es, string dsName, bool isMaster)` |
| `RegisterBlock<T>` (generic) | `(string name, IUnitofWork uow, string dsName, bool isMaster)` |
| `RegisterBlockFromSourceAsync` | `(string name, string conn, string entity, bool isMaster, CancellationToken)` |
| `SetupBlockAsync` | `(string name, string conn, string entity, bool isMaster, CancellationToken)` — single-call bootstrap |
| `UnregisterBlock` | `(string name)` — removes relationships, unsubscribes events |
| `GetBlock` / `GetBlock<T>` | `(string name)` — cached lookup |
| `GetUnitOfWork` | `(string name)` |
| `BlockExists` | `(string name)` |

### 3.2 Data Operations
| Method | Signature |
|--------|-----------|
| `InsertRecordAsync` | `(string name, object record = null)` |
| `DeleteCurrentRecordAsync` | `(string name)` |
| `UpdateCurrentRecordAsync` | `(string name)` |
| `ExecuteQueryAsync` | `(string name, List<AppFilter> filters = null)` |
| `EnterQueryModeAsync` | `(string name)` |
| `ExecuteQueryAndEnterCrudModeAsync` | `(string name, List<AppFilter> filters = null)` |
| `ExecuteQueryEnhancedAsync` | `(string name, List<AppFilter> filters = null)` — with audit |
| `InsertRecordEnhancedAsync` | `(string name, object record = null)` — with audit |
| `CreateNewRecord` | `(string name)` |
| `RefreshBlockAsync` | `(string name, List<AppFilter> filters, ConflictMode, CancellationToken)` |
| `RevertCurrentRecord` | `(string name)` |
| `RevertRecord` | `(string name, int index)` |

### 3.3 Navigation
| Method | Signature |
|--------|-----------|
| `FirstRecordAsync` / `LastRecordAsync` | `(string name)` |
| `NextRecordAsync` / `PreviousRecordAsync` | `(string name)` |
| `NavigateToRecordAsync` | `(string name, int recordIndex)` |
| `SwitchToBlockAsync` | `(string name)` |
| `GoBlockAsync` / `GoRecordAsync` / `GoItemAsync` | (Oracle Forms named equivalents) |
| `NextItemAsync` / `PreviousItemAsync` | `(string name, string currentItem = null)` |
| `NextBlockAsync` / `PreviousBlockAsync` | `()` |
| `NavigateBackAsync` / `NavigateForwardAsync` | `(string name)` (per-block history) |
| `CanNavigateBack` / `CanNavigateForward` | `(string name)` |
| `GetNavigationHistory` / `ClearNavigationHistory` | `(string name)` |
| `GetCurrentRecordInfo` | `(string name)` |
| `GetAllNavigationInfo` | `()` |

### 3.4 Mode Transitions
| Method | Signature |
|--------|-----------|
| `EnterQueryModeAsync` | `(string name)` — validates unsaved changes first |
| `ExecuteQueryAndEnterCrudModeAsync` | `(string name, List<AppFilter> filters)` |
| `EnterCrudModeForNewRecordAsync` | `(string name)` |
| `CreateNewRecordInMasterBlockAsync` | `(string masterName)` |
| `GetBlockMode` / `TryGetBlockMode` | `(string name)` |
| `GetAllBlockModeInfo` | `()` |
| `IsFormReadyForModeTransitionAsync` | `()` |
| `ValidateAllBlocksForModeTransitionAsync` | `()` |

### 3.5 Form Operations
| Method | Signature |
|--------|-----------|
| `OpenFormAsync` | `(string formName)` |
| `CloseFormAsync` | `()` |
| `CommitFormAsync` | `()` — FK-aware topological ordered commit |
| `RollbackFormAsync` | `()` |
| `ClearAllBlocksAsync` / `ClearBlockAsync` | `(string name?)` |
| `ValidateForm` | `()` |

### 3.6 Relationships
| Method | Signature |
|--------|-----------|
| `CreateMasterDetailRelation` | `(string master, string detail, string masterKey, string detailFk, RelationshipType)` |
| `SynchronizeDetailBlocksAsync` | `(string master, CancellationToken)` |
| `GetDetailBlocks` | `(string master)` |
| `GetMasterBlock` | `(string detail)` |

### 3.7 Undo/Redo
| Method | Signature |
|--------|-----------|
| `SetBlockUndoEnabled` | `(string name, bool enable, int maxDepth)` |
| `UndoBlock` / `RedoBlock` | `(string name)` |
| `CanUndoBlock` / `CanRedoBlock` | `(string name)` |

### 3.8 Savepoints
| Method | Signature |
|--------|-----------|
| `CreateBlockSavepoint` | `(string name, string savepointName)` |
| `RollbackToSavepointAsync` | `(string name, string savepointName, CancellationToken)` |

### 3.9 Batch & Export
| Method | Signature |
|--------|-----------|
| `CommitFormBatchAsync` | `(int batchSize, IProgress<CommitBatchProgress>, Cancellation)` |
| `CommitBlockBatchAsync` | `(string name, int batchSize, IProgress, Cancellation)` |
| `ExportBlockToJsonAsync` / `ExportBlockToCsvAsync` | `(string name, Stream, ...)` |
| `ImportBlockFromJsonAsync` / `ImportBlockFromCsvAsync` | `(string name, Stream, bool clearFirst, ...)` |
| `GetBlockAsDataTable` | `(string name)` |
| `CloneBlockDataAsync` | `(string source, string dest, Cancellation)` |
| `DuplicateCurrentRecordAsync` | `(string name, Cancellation)` |

### 3.10 Aggregates
| Method | Signature |
|--------|-----------|
| `GetBlockSum` / `GetBlockAverage` | `(string name, string field)` |
| `GetBlockCount` | `(string name, Func<object,bool> predicate)` |
| `GetBlockGroups` | `(string name, string field)` |
| `GetBlockChangeSummary` | `(string name)` |
| `GetFormChangeSummary` | `()` |

### 3.11 Form State
| Method | Signature |
|--------|-----------|
| `SaveFormState` | `()` — returns `FormStateSnapshot` |
| `RestoreFormStateAsync` | `(FormStateSnapshot snapshot, Cancellation)` |

### 3.12 Cross-Block Validation
| Method | Signature |
|--------|-----------|
| `RegisterCrossBlockRule` / `UnregisterCrossBlockRule` | `(CrossBlockValidationRule)` / `(string name)` |
| `ValidateCrossBlock` | `()` |

### 3.13 Block Properties (Oracle `SET_BLOCK_PROPERTY`)
| Method | Signature |
|--------|-----------|
| `SetBlockProperty` / `GetBlockProperty` / `GetBlockProperty<T>` | `(string name, BlockProperty, object value)` |
| `SetInsertAllowed` / `SetUpdateAllowed` / `SetDeleteAllowed` / `SetQueryAllowed` | `(string name, bool)` |
| `SetDefaultWhere` / `SetOrderBy` | `(string name, string clause)` |

### 3.14 DML Triggers
| Method | Signature |
|--------|-----------|
| `FireOnInsertAsync` / `FireOnUpdateAsync` / `FireOnDeleteAsync` | `(string name, object record)` |
| `RaiseFormTriggerAsync` | `(string triggerName, string blockName)` |

### 3.15 Key Triggers
| Method | Signature |
|--------|-----------|
| `RegisterKeyTrigger` / `RegisterKeyTriggerAsync` | `(KeyTriggerType, string block, handler)` |
| `FireKeyTriggerAsync` | `(KeyTriggerType, string block)` |

### 3.16 Trigger Chaining
| Method | Signature |
|--------|-----------|
| `FireTriggersInOrderAsync` | `(IReadOnlyList<TriggerDefinition>, string block, Cancellation)` |
| `GetTriggerLog` / `ClearTriggerLog` | `()` / `()` |
| `TriggerLog` / `TriggerDependencies` | Properties |

### 3.17 Validation
| Method | Signature |
|--------|-----------|
| `ValidateField` | `(string name, string field, object value)` |
| `ValidateBlock` | `(string name)` |

### 3.18 Security
| Method | Signature |
|--------|-----------|
| `SetSecurityContext` | `(SecurityContext)` |
| `SetBlockSecurity` / `GetBlockSecurity` | `(string name, BlockSecurity)` / `(string name)` |
| `IsBlockAllowed` | `(string name, SecurityPermission)` |
| `SetFieldSecurity` / `GetFieldSecurity` | `(string name, string field, FieldSecurity)` / `(string name, string field)` |
| `GetMaskedFieldValue` | `(string name, string field, object raw)` |
| `GetSecurityViolations` | `()` |
| `SecurityContext` / `Security` | Properties |

### 3.19 Audit
| Method | Signature |
|--------|-----------|
| `SetAuditUser` / `ConfigureAudit` | `(string)` / `(Action<AuditConfiguration>)` |
| `GetAuditLog` | `(string block, AuditOperation?, DateTime?, DateTime?)` |
| `GetFieldHistory` | `(string block, string recordKey, string field)` |
| `ExportAuditToCsvAsync` / `ExportAuditToJsonAsync` | `(string path, string block)` / `(string path, string block)` |
| `PurgeAudit` / `ClearAudit` | `(int olderThanDays)` / `()` |
| `AuditManager` | Property |

### 3.20 Alerts & Messages
| Method | Signature |
|--------|-----------|
| `SetMessage` / `ClearMessage` | `(string text, MessageLevel)` / `()` |
| `ShowAlertAsync` | `(string title, string msg, AlertStyle, string btn1, string btn2, string btn3, Cancellation)` |
| `ShowInfoAsync` / `ConfirmAsync` | Shortcut wrappers |
| `CurrentMessage` | Property |

### 3.21 Forms Simulation
| Method | Signature |
|--------|-----------|
| `SetAuditDefaults` | `(object record, string user)` |
| `SetFieldValue` / `GetFieldValue` | `(object record, string field, object value)` / `(object record, string field)` |
| `ExecuteSequence` | `(string block, object record, string field, string seq)` |
| `SetSystemVariables` | `(object record, SystemVariableType, object value)` |

### 3.22 Inter-Form Communication
| Method | Signature |
|--------|-----------|
| `SetGlobalVariable` / `GetGlobalVariable` / `GetGlobalVariable<T>` | `(string name, object value)` / `(string name)` |
| `SendParameterToForm` | `(string form, string param, object value)` |
| `PostMessage` / `BroadcastMessage` | `(string target, string type, object payload)` / `(string type, object payload)` |
| `SubscribeToMessage` / `UnsubscribeFromMessage` | `(string type, Action<FormMessage>)` / `(string type)` |
| `CreateSharedBlock` / `GetSharedBlock` | `(string name, IUnitofWork)` / `(string name)` |
| `TryLockSharedBlock` / `ReleaseSharedBlockLock` | `(string name, TimeSpan)` / `(string name)` |

### 3.23 Performance & Paging
| Method | Signature |
|--------|-----------|
| `SetBlockPageSize` / `LoadPageAsync` | `(string name, int)` / `(string name, int page, Cancellation)` |
| `GetTotalRecordCount` / `SetTotalRecordCount` | `(string name)` / `(string name, long)` |
| `SetFetchAheadDepth` | `(string name, int depth)` |
| `SetLazyLoadMode` / `GetLazyLoadMode` | `(string name, LazyLoadMode)` / `(string name)` |
| `SetMaxRecordsPerFetch` | `(string name, int max)` |
| `InvalidateBlockCache` / `SetBlockCacheTtl` | `(string name)` / `(string name, TimeSpan)` |
| `GetCacheStats` / `CheckCacheMemoryPressure` | `()` / `(long thresholdMb)` |
| `Paging` | Property |

### 3.24 Dirty State
| Method | Signature |
|--------|-----------|
| `CheckAndHandleUnsavedChangesAsync` | `(string name)` |
| `HasUnsavedChanges` / `GetDirtyBlocks` | `()` / `()` |

---

## 4. Model Catalog (80+ classes)

### 4.1 Trigger Models (`Models/TriggerEnums.cs`, `TriggerDefinition.cs`, `TriggerContext.cs`, `TriggerEventArgs.cs`)
- **200+ Oracle Forms trigger types** across 10 categories: FormLifecycle, BlockLifecycle, RecordLifecycle, ItemLifecycle, DataManipulation, Query, Validation, Navigation, KeyAction, Mouse, Timer, ErrorHandling, MasterDetail, Custom
- `TriggerDefinition` — full trigger config with sync/async handlers, dependencies, priority, chain mode
- `TriggerContext` — 40+ properties for handler context
- `TriggerResult` — Success/Failure/Cancelled/Skipped/Timeout/Exception

### 4.2 Data Models (`DataBlockInfo.cs`, `DataBlockRelationship.cs`, `DataBlockMode.cs`)
- `DataBlockInfo` — registered block metadata (UoW, entity structure, mode, security flags, FK fields)
- `DataBlockRelationship` — master/detail relationship definition
- `DataBlockMode` — Normal/EnterQuery/Query/CRUD/ReadOnly/Insert

### 4.3 Validation Models (`ValidationRule.cs`, `ValidationResult.cs`, `ValidationEnums.cs`, `CrossBlockValidationRule.cs`)
- `ValidationRule` — single field rule with 16 validation types, custom validator, conditions
- `ItemValidationResult` / `RecordValidationResult` / `BlockValidationResult` / `FormValidationResult` — hierarchical results
- `ValidationTiming` — OnBlur/OnChange/OnRecordChange/OnCommit/Manual

### 4.4 Oracle Forms System Variables (`SystemVariables.cs`, `SystemVariableType.cs`, `RecordStatus.cs`)
- 20+ `:SYSTEM.*` mirror properties (CURRENT_BLOCK, CURSOR_RECORD, LAST_QUERY, FORM_STATUS, etc.)
- `BeepRecordStatus` — Query/New/Insert/Changed/QueryCriteria

### 4.5 LOV Models (`LOVDefinition.cs`, `LOVColumn.cs`, `LOVResult.cs`, `LOVEnums.cs`)
- `LOVDefinition` — full LOV config (display/return fields, columns, filters, cache, validation type)
- `LOVResult` / `LOVSelectionResult` / `LOVValidationResult`
- `LOVSearchMode` / `LOVValidationType` — Contains/StartsWith/EndsWith/Exact; ListOnly/Unrestricted/Validated

### 4.6 Security Models (`SecurityModels.cs`, `SecurityPermission.cs`)
- `SecurityContext` / `BlockSecurity` / `FieldSecurity` — user identity, per-block filtering, per-field masking
- `SecurityRole` / `SecurityViolationEventArgs`

### 4.7 Audit Models (`AuditModels.cs`, `AuditConfiguration.cs`)
- `AuditEntry` — full before/after image with field-level change tracking
- `AuditConfiguration` — enabled blocks, excluded fields, auto-generated columns, retention

### 4.8 Performance Models (`PerformanceModels.cs`, `CacheEfficiencyMetrics.cs`, `CachedBlockInfo.cs`, `DirtyBlockInfo.cs`)
- `PageInfo` / `CacheStats` / `CacheEfficiencyMetrics` / `PerformanceMetric` / `PerformanceStatistics`
- `LazyLoadMode` / `CachedBlockInfo` / `CachePriority`

### 4.9 Navigation Models (`NavigationInfo.cs`, `NavigationHistoryEntry.cs`, `NavigationTriggerEventArgs.cs`, `TriggerStatisticsInfo.cs`)
- `NavigationInfo` — current index, total records, has previous/next, metrics
- `NavigationTriggerEventArgs` — cancelable navigation event with context

### 4.10 Item Models (`ItemInfo.cs`, `ItemPropertyEventArgs.cs`, `BlockFieldMetadata.cs`, `BlockFieldChangedEventArgs.cs`, `FieldConstraints.cs`)
- `ItemInfo` — 30 properties (bound property, tab order, validation, error state, navigation links)
- `ItemValueChangedEventArgs` / `ItemErrorEventArgs` / `ItemNavigationEventArgs`

### 4.11 DML & Trigger Event Args (`DMLTriggerEventArgs.cs`, `TriggerEventArgs.cs`, `ErrorTriggerEventArgs.cs`, `ValidationTriggerEventArgs.cs`, `FormTriggerEventArgs.cs`, `RecordTriggerEventArgs.cs`, `BlockTriggerEventArgs.cs`)
- `DMLTriggerEventArgs` — context for ON-INSERT/UPDATE/DELETE with helper methods
- `FormTriggerEventArgs` — cancelable form lifecycle event

### 4.12 Save/Undo/Dirty Models (`SaveResult.cs`, `SaveOptions.cs`, `RollbackOptions.cs`, `SavepointInfo.cs`, `UnsavedChangesEventArgs.cs`, `DirtyBlockInfo.cs`, `LockMode.cs`, `RecordLockInfo.cs`, `BlockErrorInfo.cs`, `BlockMessage.cs`)
- `SavepointInfo` — timestamped block state snapshot with record index, dirty flag, snapshot dict
- `UnsavedChangesEventArgs` — user-actionable dialog type

### 4.13 Multi-Form Models (`FormRegistryModels.cs`, `FormCallStackEntry.cs`, `FormMessage.cs`)
- `FormCallStackEntry` — nested form stack with call mode, parameters, completion task
- `FormMessage` / `FormMessageEventArgs` — inter-form message bus

### 4.14 Configuration Models (`Configuration/*`)
- `UnitofWorksManagerConfiguration` — top-level aggregation
- `NavigationConfiguration` / `ValidationConfiguration` / `PerformanceConfiguration` / `FormConfiguration`
- `ConfigurationManager` — JSON persistence

---

## 5. Events (10+ event streams)

| Event | Class | Fires When |
|-------|-------|-----------|
| `OnBlockFieldChanged` | `FormsManager` | Any field value changes (also feeds audit) |
| `OnNavigate` | `Navigation` | Before record navigation (cancellable) |
| `OnCurrentChanged` | `Navigation` | After navigation completes |
| `OnFormOpen` / `OnFormClose` | `FormOperations` | Form lifecycle |
| `OnFormCommit` / `OnFormRollback` | `FormOperations` | Commit/rollback lifecycle |
| `OnFormValidate` | `FormOperations` | Form validation |
| `OnFormMessage` | `InterFormComm` | Inter-form message received |

---

## 6. Quick Start

```csharp
using TheTechIdea.Beep.Editor.UOWManager;

var manager = new FormsManager(dmeEditor) { CurrentFormName = "CUSTOMER_ORDERS" };

// Register blocks
manager.RegisterBlock<CustomerDto>("CUSTOMERS", customerUow, customerEntity, "Northwind", isMaster: true);
manager.RegisterBlock<OrderDto>("ORDERS", orderUow, orderEntity, "Northwind");

// Master-detail
manager.CreateMasterDetailRelation("CUSTOMERS", "ORDERS", "CustomerId", "CustomerId");

// Query
await manager.ExecuteQueryAsync("CUSTOMERS");

// Navigate
await manager.FirstRecordAsync("CUSTOMERS");
await manager.SynchronizeDetailBlocksAsync("CUSTOMERS");

// Commit
await manager.CommitFormAsync();
```

---

## 7. Documentation Map

| Document | Purpose |
|----------|---------|
| **[`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md)** | Every Oracle Forms concept → FormsManager method |
| **[`gaps.md`](gaps.md)** | What the engine does not yet implement |
| **[`enhancements.md`](enhancements.md)** | Prioritized gap-closure roadmap |
| **[`architecture.md`](architecture.md)** | Subsystems, layering, host/orchestrator/helper model |
| **[`Models/README.md`](Models/README.md)** | Model class catalog |
| **[`Configuration/README.md`](Configuration/README.md)** | Configuration DTOs |
