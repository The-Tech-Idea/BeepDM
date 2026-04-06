# FormsManager Enhancement — Todo Tracker

> Track progress across all enhancement phases.  
> Update status: `[ ]` = Not started, `[~]` = In progress, `[x]` = Complete

---

## Phase 1: Core Completion & Stabilization ─ Critical

### 1.1 Generic Block Registration
- [x] Add `RegisterBlock<T>()` generic overload
- [x] Store `Type` reference in `DataBlockInfo`
- [x] Add `GetBlock<T>()` typed accessor
- [x] Fix `CreateNewRecord` to use stored Type (no ExpandoObject fallback)
- [x] Add `InsertRecordAsync<T>()` generic CRUD overload

### 1.2 Complete Trigger Wiring
- [x] Wire `PreInsert`/`PostInsert` in `InsertRecordEnhancedAsync`
- [x] Wire `PreUpdate`/`PostUpdate` in update operations
- [x] Wire `PreDelete`/`PostDelete` in `DeleteCurrentRecordAsync`
- [x] Wire `PreQuery`/`PostQuery` in `ExecuteQueryEnhancedAsync`
- [x] Wire `PreCommit`/`PostCommit` in `CommitFormAsync`
- [x] Wire `WhenNewBlockInstance` in `SwitchToBlockAsync`
- [x] Wire `WhenCreateRecord` in `CreateNewRecord`
- [x] Wire `WhenRemoveRecord` in delete operations
- [x] Wire `OnPopulateDetails` in `SynchronizeDetailBlocksAsync`
- [x] Add trigger cancel handling (abort operation on `TriggerResult.Cancelled`)

### 1.3 Validation Integration
- [x] Auto-call `ValidateItem` on field change (`OnBlockFieldChanged`)
- [x] Auto-call `ValidateRecord` before per-record commit
- [x] Call `CrossBlockValidation.Validate` in `CommitFormAsync`
- [x] Integrate `ValidateBlock` in post-query transition
- [x] Add `ValidateBeforeNavigation` config option

### 1.4 LOV Full Integration
- [x] Wire LOV validation in `OnBlockFieldChanged`
- [x] Auto-populate related fields on LOV selection
- [x] Add `ShowLOVAsync(blockName, fieldName)` method
- [x] Add cascading LOV support
- [x] Implement LOV auto-complete via `FilterLOVData`

### 1.5 Missing Interface Implementations
- [x] Audit `IUnitofWorksManager` vs `FormsManager` for missing methods
- [x] Route `ValidateField`/`ValidateBlock`/`ValidateForm` to `ValidationManager`
- [x] Route interface `InsertRecordAsync`/`DeleteCurrentRecordAsync` properly
- [x] Implement `DuplicateCurrentRecordAsync` via `CloneItem`

**Phase 1 Progress: 30 / 30 tasks ✓**

---

## Phase 2: Oracle Forms Built-in Emulation ─ High

### 2.1 Block Property Built-ins
- [x] Add `SetBlockProperty(blockName, property, value)`
- [x] Add `GetBlockProperty(blockName, property)`
- [x] Support: `INSERT_ALLOWED`, `UPDATE_ALLOWED`, `DELETE_ALLOWED`, `QUERY_ALLOWED`, `DEFAULT_WHERE`, `ORDER_BY`
- [x] Wire property changes to `DataBlockInfo` fields
- [x] Add `IBlockPropertyManager` interface

### 2.2 Navigation Built-ins
- [x] Add `GoBlock(blockName)` 
- [x] Add `GoItem(blockName, itemName)`
- [x] Add `GoRecord(blockName, recordIndex)`
- [x] Add `NextBlock()` / `PreviousBlock()`
- [x] Add `NextItem()` / `PreviousItem()`
- [x] Wire `KEY-NEXT-ITEM`, `KEY-PREV-ITEM` trigger types

### 2.3 Message & Alert Built-ins
- [x] Add `SetMessage(text, level)` at FormsManager level
- [x] Add `ShowAlertAsync(title, message, style)` → `AlertResult`
- [x] Define `AlertStyle` enum (`Stop`, `Caution`, `Note`)
- [x] Define `AlertResult` enum (`Button1`, `Button2`, `Button3`)
- [x] Add `IAlertProvider` interface for UI injection
- [x] Add `ClearMessage()` 

### 2.4 Sequence & Default Value Built-ins
- [x] Add `GetNextSequence(sequenceName)`
- [x] Add `ISequenceProvider` interface
- [x] Add `SetItemDefault(blockName, itemName, defaultFactory)`
- [x] Add `CopyFieldValue(srcBlock, srcField, destBlock, destField)`
- [x] Add `PopulateGroupFromBlock(blockName, groupName)`

### 2.5 Timer Management
- [x] Add `CreateTimer(timerName, interval, repeating)`
- [x] Add `DeleteTimer(timerName)`
- [x] Add `GetTimer(timerName)`
- [x] Add `WHEN-TIMER-EXPIRED` trigger type
- [x] Wire timer expiration to trigger execution
- [x] Add `ITimerManager` interface

**Phase 2 Progress: 25 / 25 tasks ✓**

---

## Phase 3: Multi-Form & Cross-Form Communication ─ High

### 3.1 Form Registry
- [x] Create `IFormRegistry` singleton interface
- [x] Add `RegisterForm` / `UnregisterForm`
- [x] Add `GetForm(formName)` → `IUnitofWorksManager`
- [x] Add `GetActiveFormNames()`
- [x] Add form lifecycle events

### 3.2 Multi-Form Navigation
- [x] Add `CallFormAsync(formName)` — suspends parent
- [x] Add `OpenFormAsync(formName)` — independent
- [x] Add `NewFormAsync(formName)` — closes current, opens new
- [x] Add `ReturnToCallerAsync()` with optional data
- [x] Add form modality (`Modal`/`Modeless`)
- [x] Add `FormCallStack` for nesting

### 3.3 Inter-Form Communication
- [x] Add `SetGlobalVariable` / `GetGlobalVariable` (:GLOBAL.*)
- [x] Add `SendParameterToForm(formName, paramName, value)`
- [x] Add `IFormMessageBus` pub/sub
- [x] Add `PostMessage(targetForm, messageType, payload)`
- [x] Add `OnFormMessage` event

### 3.4 Shared Data Blocks
- [x] Add `CreateSharedBlock(blockName, uow)`
- [x] Add `GetSharedBlock(blockName)`
- [x] Add lock coordination for shared blocks
- [x] Add cross-form change notifications

**Phase 3 Progress: 20 / 20 tasks ✓**

---

## Phase 4: Advanced Trigger System ─ Medium-High

### 4.1 Key Triggers
- [x] Add `KeyTriggerType` enum (`Models/TriggerEnums.cs`)
- [x] Add `RegisterKeyTrigger` / `RegisterKeyTriggerAsync` on FormsManager (`FormsManager.KeyTriggers.cs`)
- [x] Add `FireKeyTriggerAsync`
- [x] Wire key triggers into navigation/commit (default actions in `ExecuteKeyDefaultActionAsync`)

### 4.2 DML Triggers (Transactional)
- [x] Add `ON-INSERT` (=40) / `ON-UPDATE` (=41) / `ON-DELETE` (=42) to `TriggerType` enum
- [x] Wire DML triggers via `FireOnInsertAsync` / `FireOnUpdateAsync` / `FireOnDeleteAsync` (`FormsManager.DmlTriggers.cs`)
- [x] Add `RAISE_FORM_TRIGGER(triggerName)` programmatic fire via `RaiseFormTriggerAsync`

### 4.3 Trigger Chaining & Dependency
- [x] Add trigger dependency graph (`Helpers/TriggerDependencyManager.cs`, `ITriggerDependencyManager`)
- [x] Add circular dependency detection (DFS in `TriggerDependencyManager.FindCycle`)
- [x] Add `TriggerChainMode` enum (StopOnFailure / Continue / Rollback) in `Models/TriggerEnums.cs`
- [x] Add per-trigger `DependsOn` list and `ChainMode` to `TriggerDefinition`
- [x] Add trigger execution log with timing (`Helpers/TriggerExecutionLog.cs`, `ITriggerExecutionLog`)
- [x] Add `FireTriggersInOrderAsync` in `FormsManager.TriggerChaining.cs`
- [x] Subscribe to `ITriggerManager.TriggerExecuted` to auto-populate execution log

### 4.4 Trigger Templates & Libraries
- [x] `AutoNumberTrigger` factory in `TriggerLibrary`
- [x] `AuditStampTriggers` factory (PreInsert + PreUpdate pair) in `TriggerLibrary`
- [x] `CascadeDeleteTrigger` factory in `TriggerLibrary`
- [x] `FormatFieldTrigger` factory in `TriggerLibrary`

**Phase 4 Progress: 20 / 20 tasks ✅**

---

## Phase 5: Audit Trail & Change Tracking ─ Medium

### 5.1 Audit Log Infrastructure
- [x] Create `IAuditManager` interface (`Interfaces/IUnitofWorksManagerInterfaces.cs`)
- [x] Add `AuditEntry` model (`Models/AuditModels.cs`)
- [x] Add `AuditConfiguration` (per block, per field, retention) (`Models/AuditModels.cs`)
- [x] Add `GetAuditLog(blockName, filters)` (`FormsManager.Audit.cs`)
- [x] Add `GetFieldHistory(blockName, recordKey, fieldName)` (`FormsManager.Audit.cs`)

### 5.2 Automatic Audit Capture
- [x] Wire into `OnBlockFieldChanged` for field-level capture (`FormsManager.Audit.cs` → `InitializeAudit`)
- [x] Wire into `CommitFormAsync` for commit-level entries (`FormsManager.FormOperations.cs`)
- [x] Add `BeforeImage` / `AfterImage` snapshot fields on `AuditEntry`
- [x] Add configurable audit fields (CreatedBy, ModifiedBy, CreatedAt, ModifiedAt in `AuditConfiguration`)
- [x] Add `SetAuditUser(userName)` (`FormsManager.Audit.cs`)

### 5.3 Audit Persistence
- [x] Add `IAuditStore` pluggable interface (`Interfaces/IUnitofWorksManagerInterfaces.cs`)
- [x] Add `InMemoryAuditStore` default (`Helpers/InMemoryAuditStore.cs`)
- [x] Add `FileAuditStore` (JSON per session) (`Helpers/FileAuditStore.cs`)
- [x] Add audit export — `ExportAuditToCsvAsync` / `ExportAuditToJsonAsync` (`FormsManager.Audit.cs`)
- [x] Add retention policy + auto-purge — `PurgeAudit(days)`, `MaxRetentionDays` in config

**Phase 5 Progress: 15 / 15 tasks ✅**

---

## Phase 6: Security & Authorization ─ Medium

### 6.1 Security Manager
- [x] Create `ISecurityManager` interface
- [x] Add `SecurityContext` model (user, roles, permissions)
- [x] Add `SetSecurityContext()` on FormsManager
- [x] Add `BlockSecurity` model
- [x] Add `FieldSecurity` model

### 6.2 Block-Level Security
- [x] Add `SetBlockSecurity(blockName, security)`
- [x] Enforce in CRUD operations
- [x] Auto-set block allowed flags from security context
- [x] Add role-based query restrictions (WHERE clause)
- [x] Raise security violation events

### 6.3 Field-Level Security
- [x] Integrate with `ItemPropertyManager` for auto Enabled/Visible
- [x] Add field masking (PII, SSN, etc.)
- [x] Add `IFieldMaskProvider` interface
- [x] Add UI hints for secured fields
- [x] Log unauthorized access attempts

**Phase 6 Progress: 15 / 15 tasks ✅**

---

## Phase 7: Performance & Scalability ─ Medium

### 7.1 Virtual Scrolling / Paging
- [x] Add `IPagingManager` interface
- [x] Add `SetBlockPageSize(blockName, pageSize)`
- [x] Add `LoadPageAsync(blockName, pageNumber)`
- [x] Add `GetTotalRecordCount(blockName)`
- [x] Add configurable fetch-ahead

### 7.2 Lazy Loading
- [x] Add `LazyLoadMode` on DataBlockInfo
- [x] Optimize detail block loading on master navigation
- [x] Add field-level lazy loading for BLOB/CLOB
- [x] Add `MaxRecordsPerFetch` config
- [x] Add query result streaming support

### 7.3 Caching Improvements
- [x] Add LRU eviction policy
- [x] Add cache invalidation on external changes
- [x] Add block-level cache TTL
- [x] Add memory pressure monitoring + auto-evict
- [x] Add cache hit/miss ratio logging

**Phase 7 Progress: 15 / 15 tasks ✅**

---

## Phase 8: Testing & Documentation ─ High

### 8.1 Unit Tests
- [ ] `FormsManager.Core.Tests` — block registration
- [ ] `FormsManager.Navigation.Tests` — record navigation
- [ ] `FormsManager.FormOperations.Tests` — form lifecycle
- [ ] `FormsManager.ModeTransitions.Tests` — query/CRUD modes
- [ ] `TriggerManager.Tests` — trigger lifecycle
- [ ] `ValidationManager.Tests` — validation rules
- [ ] `LOVManager.Tests` — LOV operations
- [ ] `SavepointManager.Tests` — savepoint lifecycle
- [ ] `LockManager.Tests` — record locking

### 8.2 Integration Tests
- [ ] Master-detail cascade operations
- [ ] Full form lifecycle (Open → Query → Edit → Commit → Close)
- [ ] Multi-block validation with cross-block rules
- [ ] Concurrent block operations (thread safety)
- [ ] LOV with real data source
- [ ] Export → Import round-trip

### 8.3 Documentation
- [ ] Update `README.md` with new APIs
- [ ] Create `MIGRATION-GUIDE.md`
- [ ] Create `ORACLE-FORMS-MAPPING.md` reference
- [ ] Create per-helper READMEs
- [ ] 100% XML doc coverage on public API

**Phase 8 Progress: 0 / 20 tasks**

---

## Phase 9: Help Documentation Update — After Phase 8

**Goal:** Update `Help/formsmanager.html` once all implementation phases are complete, reflecting the full final API surface.  
**Depends on:** Phases 1–8 complete.

### 9.1 Baseline (Full current API)
- [ ] Rewrite `formsmanager.html` with full API (all partials, all helpers, all events)
- [ ] Document all 18 helper managers with interface signatures + usage examples
- [ ] Document mode transitions (ENTER_QUERY / EXECUTE_QUERY / CRUD)
- [ ] Document form lifecycle (Open / Close / Commit / Rollback)
- [ ] Document navigation built-ins (First/Next/Previous/Last/NavigateTo/SwitchBlock)
- [ ] Document data operations (Undo/Redo, batch commit, export/import, aggregates)
- [ ] Document trigger system (registration, firing, scope hierarchy)
- [ ] Document validation system (rules, fluent builder, timing)
- [ ] Document LOV system (register, load, cache, validate)
- [ ] Document savepoint and lock managers
- [ ] Add Oracle Forms → FormsManager mapping table
- [ ] Add complete C# code examples for each section

### 9.2 Phase 1 Additions
- [ ] Document generic `RegisterBlock<T>()` and `InsertRecordAsync<T>()`
- [ ] Document `GetBlock<T>()` typed accessor
- [ ] Document auto-trigger wiring (PRE/POST insert/update/delete/query/commit)
- [ ] Document auto-validation on field change and before commit
- [ ] Document `ShowLOVAsync` and cascading LOV
- [ ] Document `DuplicateCurrentRecordAsync`

### 9.3 Phase 2 Additions
- [ ] Document `SetBlockProperty` / `GetBlockProperty` built-ins
- [ ] Document `GoBlock` / `GoItem` / `GoRecord` / `NextBlock` / `PreviousBlock`
- [ ] Document `ShowAlertAsync` with AlertStyle / AlertResult
- [ ] Document `GetNextSequence` and `ISequenceProvider`
- [ ] Document Timer management (`CreateTimer`, `DeleteTimer`, `WHEN-TIMER-EXPIRED`)

### 9.4 Phase 3 Additions
- [ ] Document `IFormRegistry` and multi-form lifecycle
- [ ] Document `CallFormAsync` / `OpenFormAsync` / `NewFormAsync` / `ReturnToCallerAsync`
- [ ] Document `:GLOBAL.*` variables (`SetGlobalVariable` / `GetGlobalVariable`)
- [ ] Document `IFormMessageBus` and inter-form messaging
- [ ] Document shared blocks

### 9.5 Phase 4 Additions
- [ ] Document key triggers (`KeyTriggerType`, `RegisterKeyTrigger`, `FireKeyTrigger`)
- [ ] Document DML triggers (`ON-INSERT` / `ON-UPDATE` / `ON-DELETE`)
- [ ] Document trigger chaining and dependency graph
- [ ] Document built-in `TriggerLibrary` entries

### 9.6 Phases 5–7 Additions
- [ ] Document `IAuditManager`, `AuditEntry`, audit configuration and stores
- [ ] Document `ISecurityManager`, `BlockSecurity`, `FieldSecurity`
- [ ] Document `IPagingManager`, lazy loading, LRU cache settings

**Phase 9 Progress: 0 / 30 tasks**

---

## Overall Progress

| Phase | Status | Tasks Done | Total | % |
|-------|--------|-----------|-------|---|
| 1 — Core Completion | Not Started | 0 | 30 | 0% |
| 2 — Oracle Built-ins | Not Started | 0 | 25 | 0% |
| 3 — Multi-Form | Not Started | 0 | 20 | 0% |
| 4 — Advanced Triggers | Not Started | 0 | 20 | 0% |
| 5 — Audit Trail | Not Started | 0 | 15 | 0% |
| 6 — Security | Not Started | 0 | 15 | 0% |
| 7 — Performance | Not Started | 0 | 15 | 0% |
| 8 — Testing & Docs | Not Started | 0 | 20 | 0% |
| 9 — Help Documentation | Not Started | 0 | 30 | 0% |
| **TOTAL** | | **0** | **190** | **0%** |

---

*Last updated: $(date)*
