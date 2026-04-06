# FormsManager Enhancement Plan

> **Inspired by Oracle Forms** — phased roadmap to bring full Oracle Forms parity and modern data management capabilities to the Beep FormsManager system.

**Current Score:** 7.5/10  
**Target Score:** 9.5/10  
**Namespace:** `TheTechIdea.Beep.Editor.UOWManager`

---

## Architecture Overview (Current State)

```
FormsManager (IUnitofWorksManager)
├── Core (FormsManager.cs)                    ─ Block registration, DI, properties
├── Navigation (FormsManager.Navigation.cs)   ─ Record/block navigation
├── FormOperations (.FormOperations.cs)       ─ Open/Close/Commit/Rollback form
├── EnhancedOperations (.EnhancedOperations.cs) ─ Type-safe CRUD, insert/delete
├── ModeTransitions (.ModeTransitions.cs)     ─ ENTER_QUERY / EXECUTE_QUERY
├── DataOperations (.DataOperations.cs)       ─ Undo/Redo, aggregates, batch, export
│
├── 18 Helper Managers (DI-injected)
│   ├── RelationshipManager       ─ Master-detail relationships
│   ├── DirtyStateManager         ─ Change tracking
│   ├── EventManager              ─ UoW event subscriptions
│   ├── FormsSimulationHelper     ─ Oracle Forms built-in emulation
│   ├── PerformanceManager        ─ Caching / performance stats
│   ├── ConfigurationManager      ─ Form/block configuration
│   ├── SystemVariablesManager    ─ :SYSTEM.* variable emulation
│   ├── ValidationManager         ─ WHEN-VALIDATE-ITEM/RECORD
│   ├── LOVManager                ─ List of Values
│   ├── ItemPropertyManager       ─ SET/GET_ITEM_PROPERTY
│   ├── TriggerManager            ─ Oracle Forms triggers
│   ├── SavepointManager          ─ Named savepoints + rollback
│   ├── LockManager               ─ Client-side record locking
│   ├── QueryBuilderManager       ─ Filter/query building
│   ├── BlockErrorLog             ─ Per-block error log
│   ├── MessageQueueManager       ─ Platform-agnostic messaging
│   ├── BlockFactory              ─ UoW + EntityStructure from connection
│   └── CrossBlockValidationManager ─ Multi-block validation rules
│
├── Models (55+ classes)
├── Interfaces (IUnitofWorksManagerInterfaces.cs)
└── Configuration (Form/Nav/Perf/Validation configs)
```

---

## What's Working Well (Production-Ready)

- ✅ Core block registration with ConcurrentDictionary
- ✅ Master-detail relationship management
- ✅ Mode transitions (ENTER_QUERY → EXECUTE_QUERY → CRUD)
- ✅ Form operations (Open/Close/Commit/Rollback with trigger events)
- ✅ Navigation with validation, unsaved changes checking, nav history
- ✅ Dirty state tracking across blocks and relationships
- ✅ Trigger system architecture (form/block/item/global scope, sync+async)
- ✅ Validation framework (item/record/block/form levels, fluent builder)
- ✅ LOV manager structure (register/load/cache/validate)
- ✅ Item property manager (SET/GET_ITEM_PROPERTY equivalents)
- ✅ Savepoint manager (named snapshots + rollback)
- ✅ Record locking (auto-lock, modes, per-block)
- ✅ Query builder with templates
- ✅ Undo/Redo via IUndoable UOW capability
- ✅ Batch commit with progress
- ✅ Export/Import (JSON, CSV, DataTable)
- ✅ Block aggregates (Sum, Average, Count)
- ✅ System variables (:SYSTEM.* emulation)
- ✅ Performance caching and statistics
- ✅ Configuration system (form/navigation/performance/validation)

---

## Gaps and Improvement Areas

### Critical Gaps
1. **Type safety in CRUD** — `CreateNewRecord` falls back to `ExpandoObject`; reflection-only field access
2. **No generic block registration** — `RegisterBlock<T>()` missing; everything goes through `object`
3. **Incomplete trigger integration** — Triggers defined but not wired into all CRUD/navigation paths
4. **No cross-form communication** — Forms cannot share data or trigger operations across forms
5. **No CALL_FORM / OPEN_FORM** — Oracle Forms multi-form navigation absent
6. **No Timer management** — Oracle Forms CREATE_TIMER / DELETE_TIMER missing
7. **No Audit Trail** — No automatic change logging with user/timestamp  
8. **Limited test coverage** — No unit or integration tests

### Partial Implementations
9. LOV — registered but cascade LOVs, auto-complete, multi-column search incomplete
10. Trigger — infrastructure solid but PRE/POST triggers not consistently fired in all operations
11. Validation — rules registered but async cross-block validation not fully integrated
12. Navigation — back/forward history exists but inter-form navigation absent

### Missing Oracle Forms Features
13. `SET_BLOCK_PROPERTY` / `GET_BLOCK_PROPERTY` built-ins
14. `GO_BLOCK` / `GO_ITEM` / `GO_RECORD` built-ins
15. Alert/Confirm dialogs (SHOW_ALERT)
16. Canvas/Tab page equivalent for block grouping
17. Key triggers (KEY-NEXT-ITEM, KEY-OTHERS, etc.)
18. Programmatic LOV display (SHOW_LOV)
19. Sequence/auto-number generation (GET_NEXT_SEQUENCE)
20. Transaction isolation (FORM-level vs. DB-level)

---

## Phase 1: Core Completion & Stabilization

**Goal:** Complete all interface methods, fix type safety, ensure all existing managers are properly wired.  
**Priority:** Critical  
**Estimated Scope:** ~30 tasks

### 1.1 Generic Block Registration
- [ ] Add `RegisterBlock<T>(string blockName, IUnitofWork<T> uow, IEntityStructure es, ...)` overload
- [ ] Store `Type` reference in `DataBlockInfo` for runtime type resolution
- [ ] Add `GetBlock<T>(string blockName)` typed accessor
- [ ] Update `CreateNewRecord` to use stored `Type` instead of `ExpandoObject` fallback
- [ ] Add `InsertRecordAsync<T>(string blockName, T record)` generic CRUD overload

### 1.2 Complete Trigger Wiring
- [ ] Wire `PreInsert`/`PostInsert` triggers in `InsertRecordEnhancedAsync`
- [ ] Wire `PreUpdate`/`PostUpdate` triggers in update operations
- [ ] Wire `PreDelete`/`PostDelete` triggers in `DeleteCurrentRecordAsync`
- [ ] Wire `PreQuery`/`PostQuery` triggers in `ExecuteQueryEnhancedAsync`
- [ ] Wire `PreCommit`/`PostCommit` triggers in `CommitFormAsync`
- [ ] Wire `WhenNewBlockInstance` trigger in `SwitchToBlockAsync`
- [ ] Wire `WhenCreateRecord` trigger in `CreateNewRecord`
- [ ] Wire `WhenRemoveRecord` trigger in delete operations
- [ ] Wire `OnPopulateDetails` trigger in `SynchronizeDetailBlocksAsync`
- [ ] Add trigger result handling: if `TriggerResult.Cancel`, abort the operation

### 1.3 Validation Integration
- [ ] Call `ValidationManager.ValidateItem` automatically on field change (in `OnBlockFieldChanged`)
- [ ] Call `ValidationManager.ValidateRecord` before commit per record
- [ ] Call `CrossBlockValidationManager.Validate` in `CommitFormAsync` before save
- [ ] Integrate `ValidationManager.ValidateBlock` in `ExecuteQueryAndEnterCrudModeAsync` post-query
- [ ] Add `ValidateBeforeNavigation` config option — validate current record before `NextRecord`/`PreviousRecord`

### 1.4 LOV Full Integration  
- [ ] Wire LOV validation in `OnBlockFieldChanged` when item has registered LOV
- [ ] Auto-populate related fields when LOV value selected (use `GetRelatedFieldValues`)
- [ ] Add `ShowLOVAsync(blockName, fieldName)` method on FormsManager (Oracle: SHOW_LOV)
- [ ] Add cascading LOV support — when parent LOV value changes, reload dependent LOV
- [ ] Implement LOV auto-complete — `FilterLOVData` on keystroke

### 1.5 Missing Interface Implementations
- [ ] Audit `IUnitofWorksManager` interface vs `FormsManager` — implement any missing methods
- [ ] Ensure `ValidateField`, `ValidateBlock`, `ValidateForm` delegate to `ValidationManager`
- [ ] Ensure `InsertRecordAsync`, `DeleteCurrentRecordAsync` (interface signatures) are properly routed
- [ ] Implement `DuplicateCurrentRecordAsync` using `IUnitofWorkHistory.CloneItem`

---

## Phase 2: Oracle Forms Built-in Emulation

**Goal:** Add the set of Oracle Forms built-in procedures/functions that developers expect.  
**Priority:** High  
**Estimated Scope:** ~25 tasks

### 2.1 Block Property Built-ins
- [ ] Add `SetBlockProperty(string blockName, string property, object value)` on FormsManager
- [ ] Add `GetBlockProperty(string blockName, string property)` on FormsManager
- [ ] Supported properties: `INSERT_ALLOWED`, `UPDATE_ALLOWED`, `DELETE_ALLOWED`, `QUERY_ALLOWED`, `DEFAULT_WHERE`, `ORDER_BY`, `RECORDS_DISPLAYED`, `CURRENT_RECORD`, `TOP_RECORD`
- [ ] Wire property changes to DataBlockInfo fields
- [ ] Add `IBlockPropertyManager` interface for extensibility

### 2.2 Navigation Built-ins
- [ ] Add `GoBlock(string blockName)` — switch focus to block (alias for `SwitchToBlockAsync`)
- [ ] Add `GoItem(string blockName, string itemName)` — set current item focus
- [ ] Add `GoRecord(string blockName, int recordIndex)` — navigate to specific record
- [ ] Add `NextBlock()` / `PreviousBlock()` — cycle through registered blocks
- [ ] Add `NextItem()` / `PreviousItem()` — cycle through items using `ItemPropertyManager` tab order
- [ ] Wire `KEY-NEXT-ITEM`, `KEY-PREV-ITEM` trigger types into item navigation

### 2.3 Message & Alert Built-ins
- [ ] Add `SetMessage(string text, MessageLevel level)` at FormsManager level (Oracle: MESSAGE)
- [ ] Add `ShowAlertAsync(string title, string message, AlertStyle style)` → returns `AlertResult` (Button1/Button2/Button3)
- [ ] Define `AlertStyle` enum: `Stop`, `Caution`, `Note`
- [ ] Define `AlertResult` enum: `Button1`, `Button2`, `Button3`
- [ ] Add `IAlertProvider` interface — inject UI-layer implementation for platform independence
- [ ] Add `ClearMessage()` built-in

### 2.4 Sequence & Default Value Built-ins
- [ ] Add `GetNextSequence(string sequenceName)` → returns next value from configured sequences
- [ ] Add `ISequenceProvider` interface — inject DB or in-memory sequence generator
- [ ] Add `SetItemDefault(string blockName, string itemName, Func<object> defaultFactory)` — dynamic defaults
- [ ] Add `CopyFieldValue(string srcBlock, string srcField, string destBlock, string destField)` built-in
- [ ] Add `PopulateGroupFromBlock(string blockName, string groupName)` — record group population

### 2.5 Timer Management
- [ ] Add `CreateTimer(string timerName, TimeSpan interval, bool repeating)` on FormsManager
- [ ] Add `DeleteTimer(string timerName)`
- [ ] Add `GetTimer(string timerName)` → `TimerInfo`
- [ ] Add `WHEN-TIMER-EXPIRED` trigger type
- [ ] Wire timer expiration to trigger execution
- [ ] Add `ITimerManager` interface with registration, lifecycle, and events

---

## Phase 3: Multi-Form & Cross-Form Communication

**Goal:** Enable Oracle Forms multi-form patterns (CALL_FORM, OPEN_FORM, NEW_FORM) and inter-form data sharing.  
**Priority:** High  
**Estimated Scope:** ~20 tasks

### 3.1 Form Registry
- [ ] Create `IFormRegistry` — singleton registry of all active FormsManager instances
- [ ] Add `RegisterForm(string formName, IUnitofWorksManager form)` / `UnregisterForm`
- [ ] Add `GetForm(string formName)` → `IUnitofWorksManager`
- [ ] Add `GetActiveFormNames()` → `IReadOnlyList<string>`
- [ ] Add form lifecycle events: `OnFormRegistered`, `OnFormUnregistered`

### 3.2 Multi-Form Navigation
- [ ] Add `CallFormAsync(string formName, ...)` — Oracle CALL_FORM: opens child form, suspends parent
- [ ] Add `OpenFormAsync(string formName, ...)` — Oracle OPEN_FORM: opens independent form
- [ ] Add `NewFormAsync(string formName, ...)` — Oracle NEW_FORM: closes current, opens new
- [ ] Add `ReturnToCallerAsync()` — return from CALL_FORM with optional data
- [ ] Add form modality: `Modal`, `Modeless`
- [ ] Add `FormCallStack` for CALL_FORM nesting

### 3.3 Inter-Form Communication
- [ ] Add `SetGlobalVariable(string name, object value)` / `GetGlobalVariable(string name)` — Oracle :GLOBAL.*
- [ ] Add `SendParameterToForm(string formName, string paramName, object value)` — Oracle parameter lists
- [ ] Add `IFormMessageBus` — publish/subscribe between forms
- [ ] Add `PostMessage(string targetForm, string messageType, object payload)`
- [ ] Add `OnFormMessage` event on FormsManager for receiving messages

### 3.4 Shared Data Blocks
- [ ] Add `CreateSharedBlock(string blockName, IUnitofWork uow)` — block visible across forms
- [ ] Add `GetSharedBlock(string blockName)` from any form
- [ ] Add lock coordination for shared blocks
- [ ] Add change notification across forms when shared block is modified

---

## Phase 4: Advanced Trigger System

**Goal:** Complete Oracle Forms trigger parity and add modern extensions.  
**Priority:** Medium-High  
**Estimated Scope:** ~20 tasks

### 4.1 Key Triggers
- [ ] Add `KeyTriggerType` enum: `KEY_NEXT_ITEM`, `KEY_PREV_ITEM`, `KEY_NEXT_RECORD`, `KEY_PREV_RECORD`, `KEY_COMMIT`, `KEY_EXIT`, `KEY_OTHERS`, `KEY_F1` through `KEY_F12`
- [ ] Add `RegisterKeyTrigger(KeyTriggerType type, string blockName, ...)` on `ITriggerManager`
- [ ] Add `FireKeyTrigger(KeyTriggerType type, string blockName, ...)` — invokable by UI layer
- [ ] Wire key triggers into navigation and commit operations

### 4.2 DML Triggers (Transactional)
- [ ] Add `ON-INSERT`, `ON-UPDATE`, `ON-DELETE`, `ON-LOCK` transactional triggers
- [ ] These replace default DML — give full control to the developer
- [ ] Wire into `CommitFormAsync` flow: if `ON-INSERT` trigger exists, call it instead of UoW default
- [ ] Add `RAISE_FORM_TRIGGER(triggerName)` — programmatically fire a named trigger

### 4.3 Trigger Chaining & Dependency
- [ ] Add trigger dependency graph: trigger A fires trigger B
- [ ] Add circular dependency detection
- [ ] Add `TriggerChainMode`: `StopOnFailure`, `ContinueOnFailure`, `RollbackOnFailure`
- [ ] Add execution timeout per trigger with configurable `TriggerTimeout`
- [ ] Add trigger execution log with timing for diagnostics

### 4.4 Trigger Templates & Libraries
- [ ] Create `TriggerLibrary` — pre-built common triggers (audit stamping, cascading deletes, field formatting)
- [ ] Add `AutoNumberTrigger(blockName, fieldName, sequenceName)` — auto-generate IDs
- [ ] Add `AuditStampTrigger(blockName)` — set CreatedBy/ModifiedBy/timestamps
- [ ] Add `CascadeDeleteTrigger(masterBlock, detailBlock)` — delete details when master deleted
- [ ] Add `FormatFieldTrigger(blockName, fieldName, format)` — auto-format on field change

---

## Phase 5: Audit Trail & Change Tracking

**Goal:** Enterprise-grade change tracking with full audit capabilities.  
**Priority:** Medium  
**Estimated Scope:** ~15 tasks

### 5.1 Audit Log Infrastructure
- [ ] Create `IAuditManager` interface
- [ ] Add `AuditEntry` model: `Timestamp`, `User`, `BlockName`, `RecordKey`, `FieldName`, `OldValue`, `NewValue`, `OperationType`
- [ ] Add `AuditConfiguration`: enable/disable per block, per field, retention policy
- [ ] Add `GetAuditLog(blockName, filters)` → `IReadOnlyList<AuditEntry>`
- [ ] Add `GetFieldHistory(blockName, recordKey, fieldName)` → field value timeline

### 5.2 Automatic Audit Capture
- [ ] Wire into `OnBlockFieldChanged` to capture field-level changes
- [ ] Wire into `CommitFormAsync` to capture commit-level audit entries
- [ ] Add `BeforeImage` / `AfterImage` record snapshotting per commit
- [ ] Add configurable audit fields: `CreatedBy`, `CreatedDate`, `ModifiedBy`, `ModifiedDate`
- [ ] Add `SetAuditUser(string userName)` — set current user for audit stamping

### 5.3 Audit Persistence
- [ ] Add `IAuditStore` interface — pluggable storage (in-memory, file, database)
- [ ] Add `InMemoryAuditStore` default implementation
- [ ] Add `FileAuditStore` — JSON file per form session
- [ ] Add audit export to CSV/JSON
- [ ] Add audit retention policy with auto-purge

---

## Phase 6: Security & Authorization

**Goal:** Row-level and field-level security with role-based access control.  
**Priority:** Medium  
**Estimated Scope:** ~15 tasks

### 6.1 Security Manager
- [ ] Create `ISecurityManager` interface
- [ ] Add `SecurityContext` model: current user, roles, permissions
- [ ] Add `SetSecurityContext(SecurityContext ctx)` on FormsManager
- [ ] Add `BlockSecurity` model: `CanQuery`, `CanInsert`, `CanUpdate`, `CanDelete` per role
- [ ] Add `FieldSecurity` model: `CanView`, `CanEdit` per role per field

### 6.2 Block-Level Security
- [ ] Add `SetBlockSecurity(string blockName, BlockSecurity security)`
- [ ] Enforce block security in `InsertRecordEnhancedAsync`, `DeleteCurrentRecordAsync`, etc.
- [ ] Auto-set `InsertAllowed`/`UpdateAllowed`/`DeleteAllowed` from security context
- [ ] Block query restrictions per role (appended WHERE clause)
- [ ] Raise security violation events

### 6.3 Field-Level Security
- [ ] Integrate with `ItemPropertyManager` — auto-set Enabled/Visible from security
- [ ] Add field masking for sensitive data (PII, SSN, credit card)
- [ ] Add `IFieldMaskProvider` interface for custom masking
- [ ] Add UI hints for read-only secured fields
- [ ] Log unauthorized access attempts

---

## Phase 7: Performance & Scalability

**Goal:** Handle large datasets, virtual scrolling support, and optimized data loading.  
**Priority:** Medium  
**Estimated Scope:** ~15 tasks

### 7.1 Virtual Scrolling / Paging
- [ ] Add `IPagingManager` interface on FormsManager
- [ ] Add `SetBlockPageSize(string blockName, int pageSize)` 
- [ ] Add `LoadPageAsync(string blockName, int pageNumber)` — server-side paging
- [ ] Add `GetTotalRecordCount(string blockName)` — count without loading all records
- [ ] Add configurable fetch-ahead: load next page when user approaches end

### 7.2 Lazy Loading
- [ ] Add `LazyLoadMode` on DataBlockInfo: `None`, `OnDemand`, `OnScroll`
- [ ] Detail blocks load on master record navigation (already done, optimize)
- [ ] Add field-level lazy loading for BLOB/CLOB fields
- [ ] Add `MaxRecordsPerFetch` config to prevent loading entire tables
- [ ] Add query result set streaming support

### 7.3 Caching Improvements
- [ ] Add LRU eviction policy for block data cache in PerformanceManager
- [ ] Add cache invalidation on external data changes (polling or push)
- [ ] Add block-level cache TTL configuration
- [ ] Add memory pressure monitoring — auto-evict when threshold exceeded
- [ ] Add cache hit/miss ratio logging to PerformanceStatistics

---

## Phase 8: Testing & Documentation

**Goal:** Comprehensive test coverage and developer documentation.  
**Priority:** High (runs parallel to all phases)  
**Estimated Scope:** ~20 tasks

### 8.1 Unit Tests
- [ ] Create `FormsManager.Core.Tests` — RegisterBlock, UnregisterBlock, GetBlock, BlockExists
- [ ] Create `FormsManager.Navigation.Tests` — First/Next/Previous/Last, boundary conditions
- [ ] Create `FormsManager.FormOperations.Tests` — Open/Close/Commit/Rollback, unsaved changes
- [ ] Create `FormsManager.ModeTransitions.Tests` — EnterQuery/ExecuteQuery, state validation
- [ ] Create `TriggerManager.Tests` — Register/Fire/Chain/Suspend triggers
- [ ] Create `ValidationManager.Tests` — Rule registration, item/record/block validation
- [ ] Create `LOVManager.Tests` — Register/Load/Cache/Validate LOVs
- [ ] Create `SavepointManager.Tests` — Create/Rollback/Release savepoints
- [ ] Create `LockManager.Tests` — Lock/Unlock/AutoLock, mode transitions

### 8.2 Integration Tests
- [ ] Master-detail cascade operations (insert master → auto-sync details)
- [ ] Full form lifecycle: Open → RegisterBlocks → EnterQuery → ExecuteQuery → Edit → Commit → Close
- [ ] Multi-block validation with cross-block rules
- [ ] Concurrent block operations (thread safety verification)
- [ ] LOV with real data source loading
- [ ] Export → Import round-trip (JSON, CSV)

### 8.3 Documentation
- [ ] Update `README.md` with Phase 1-7 API additions
- [ ] Create `MIGRATION-GUIDE.md` for upgrading from earlier versions
- [ ] Create `ORACLE-FORMS-MAPPING.md` — side-by-side Oracle Forms ↔ FormsManager reference
- [ ] Create per-helper `README.md` (TriggerManager, ValidationManager, LOVManager, etc.)
- [ ] Add XML doc coverage to 100% for public API surface

---

## Phase Summary

| Phase | Name | Priority | Tasks | Dependencies |
|-------|------|----------|-------|-------------|
| 1 | Core Completion & Stabilization | Critical | ~30 | None |
| 2 | Oracle Forms Built-in Emulation | High | ~25 | Phase 1 |
| 3 | Multi-Form & Cross-Form Communication | High | ~20 | Phase 1 |
| 4 | Advanced Trigger System | Medium-High | ~20 | Phase 1, 2 |
| 5 | Audit Trail & Change Tracking | Medium | ~15 | Phase 1 |
| 6 | Security & Authorization | Medium | ~15 | Phase 1, 3 |
| 7 | Performance & Scalability | Medium | ~15 | Phase 1 |
| 8 | Testing & Documentation | High | ~20 | Parallel |
| **Total** | | | **~160** | |

---

## Oracle Forms Feature Parity Matrix

| Oracle Forms Feature | FormsManager Equivalent | Status |
|---|---|---|
| ENTER_QUERY | `EnterQueryModeAsync` | ✅ Complete |
| EXECUTE_QUERY | `ExecuteQueryAndEnterCrudModeAsync` | ✅ Complete |
| COMMIT_FORM | `CommitFormAsync` | ✅ Complete |
| ROLLBACK | `RollbackFormAsync` | ✅ Complete |
| CLEAR_BLOCK | `ClearBlockAsync` | ✅ Complete |
| FIRST_RECORD | `FirstRecordAsync` | ✅ Complete |
| NEXT_RECORD | `NextRecordAsync` | ✅ Complete |
| PREVIOUS_RECORD | `PreviousRecordAsync` | ✅ Complete |
| LAST_RECORD | `LastRecordAsync` | ✅ Complete |
| CREATE_RECORD | `CreateNewRecord` / `InsertRecordEnhancedAsync` | ⚠️ Needs generic |
| DELETE_RECORD | `DeleteCurrentRecordAsync` | ⚠️ Needs trigger wiring |
| SET_ITEM_PROPERTY | `ItemProperties.SetItemProperty` | ✅ Complete |
| GET_ITEM_PROPERTY | `ItemProperties.GetItemProperty` | ✅ Complete |
| SET_BLOCK_PROPERTY | — | ❌ Phase 2 |
| GET_BLOCK_PROPERTY | — | ❌ Phase 2 |
| GO_BLOCK | — | ❌ Phase 2 |
| GO_ITEM | — | ❌ Phase 2 |
| GO_RECORD | — | ❌ Phase 2 |
| SHOW_LOV | — | ❌ Phase 1 |
| MESSAGE | `Messages.SetMessage` | ✅ Complete |
| SHOW_ALERT | — | ❌ Phase 2 |
| CALL_FORM | — | ❌ Phase 3 |
| OPEN_FORM | — | ❌ Phase 3 |
| NEW_FORM | — | ❌ Phase 3 |
| CREATE_TIMER | — | ❌ Phase 2 |
| WHEN-VALIDATE-ITEM | `Validation.ValidateItem` | ⚠️ Needs auto-wiring |
| PRE-INSERT / POST-INSERT | `TriggerType.PreInsert` / `PostInsert` | ⚠️ Needs wiring |
| PRE-QUERY / POST-QUERY | `TriggerType.PreQuery` / `PostQuery` | ⚠️ Needs wiring |
| ON-ERROR | `TriggerType.OnError` | ✅ Defined |
| ON-MESSAGE | `TriggerType.OnMessage` | ✅ Defined |
| :SYSTEM.* variables | `SystemVariables` | ✅ Complete |
| Record Group | — | ❌ Phase 2 |
| LOV | `LOVManager` | ⚠️ Partial |
| Master-Detail | `RelationshipManager` | ✅ Complete |
| Savepoint | `SavepointManager` | ✅ Complete |
| Record Locking | `LockManager` | ✅ Complete |
| KEY-* triggers | — | ❌ Phase 4 |
| ON-INSERT / ON-UPDATE / ON-DELETE | — | ❌ Phase 4 |
| :GLOBAL.* variables | — | ❌ Phase 3 |

---

## Recommended Implementation Order

1. **Phase 1** (Core) — Complete first, everything depends on it
2. **Phase 8** (Testing) — Start immediately, write tests as each phase completes
3. **Phase 2** (Built-ins) — Most user-visible improvements
4. **Phase 3** (Multi-Form) — Needed for real applications
5. **Phase 4** (Triggers) — Builds on Phase 1+2
6. **Phase 5** (Audit) — Independent, can run parallel to Phase 4
7. **Phase 6** (Security) — Depends on Phase 3 (multi-form)
8. **Phase 7** (Performance) — Final optimization pass
