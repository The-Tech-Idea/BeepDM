# Oracle Forms → FormsManager Mapping

This document maps every Oracle Forms concept that has a counterpart in `FormsManager` to the exact `FormsManager` method or `IBeepBuiltins` method that implements it. Where the engine does not yet have a counterpart, the gap is called out and routed to [`gaps.md`](gaps.md) for the engineering follow-up.

The mapping is structured by Oracle Forms concept family. Status is one of:

- ✅ **complete** — full semantic parity, no host-specific limitations.
- ⚠️ **partial** — concept is there but with limitations; see the notes.
- ❌ **missing** — concept is in scope for the engine but not yet implemented.
- 🚫 **out of scope** — concept is UI-specific and the engine deliberately does not own it (the host UI implements it).

If a concept is not in this document, it is **out of scope** (UI-specific) or **not yet documented**. If you find an undocumented concept, treat it as a gap and add it to this table.

---

## 1. Form lifecycle

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `WHEN-NEW-FORM-INSTANCE` | `FormsManager.OnFormOpen` event + `IBeepBuiltins.OpenForm(formName)` | ✅ | Fires before open completes; cancellable via `FormTriggerEventArgs.Cancel = true`. |
| `OPEN_FORM` (modeless) | `FormsManager.OpenFormAsync(formName, parameters)` | ✅ | Registry-backed. See [`functionality/multi-form.md`](functionality/multi-form.md). |
| `CALL_FORM` (modal) | `FormsManager.CallFormAsync(targetForm, parameters, FormCallMode.Modal)` | ✅ | Pushes current form onto call stack; `ReturnToCallerAsync` pops. |
| `NEW_FORM` | `FormsManager.NewFormAsync(formName)` | ✅ | Closes all open forms and opens the new one as the root. |
| `CLOSE_FORM` (modeless) | `FormsManager.CloseForm` via `IBuiltinHost.MultiFormCloseForm(formName)` | ✅ | Routed through the host so the host can decide cleanup. |
| `GO_FORM` | `FormsManager.CloseForm` + `FormsManager.OpenFormAsync` (or host equivalent) | ⚠️ | Engine-level `MultiFormGoForm` is a thin router; the host does the actual close + open. |
| `FORM-level WHEN-*` triggers | `OnFormOpen` / `OnFormClose` / `OnFormCommit` / `OnFormRollback` / `OnFormValidate` events | ✅ | 5 events, all cancellable. |
| Form-level configuration (visual attributes, window title, etc.) | ❌ not in engine | 🚫 | UI-specific. |
| `WHEN-NEW-FORM-INSTANCE` (per-form code) | Trigger system + `FormTriggerEventArgs` | ✅ | Registered via `TriggerManager`. |

## 2. Block lifecycle

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| Block creation | `FormsManager.RegisterBlock(...)` | ✅ | Multiple overloads (typed, non-typed, from source). |
| `GO_BLOCK` | `FormsManager.SwitchToBlockAsync(blockName)` / `IBeepBuiltins.GoBlock(blockName)` | ✅ | Validates unsaved changes first. |
| `NEXT_BLOCK` / `PREVIOUS_BLOCK` | `IBeepBuiltins.NextBlock()` / `PreviousBlock()` | ✅ | Resolved via block registration order. |
| `FIRST_BLOCK` / `LAST_BLOCK` | `IBeepBuiltins.FirstBlock()` / `LastBlock()` | ✅ | |
| `SET_BLOCK_PROPERTY` | `FormsManager.SetBlockProperty(blockName, BlockProperty, value)` | ✅ | Typed enum for ~20 properties (insert/update/delete/query allowed, default where, order by, etc.). |
| `GET_BLOCK_PROPERTY` | `FormsManager.GetBlockProperty(blockName, BlockProperty)` | ✅ | |
| Block-level security | `FormsManager.SetBlockSecurity(blockName, BlockSecurity)` / `IsBlockAllowed` | ✅ | |
| Block-level undo | `FormsManager.SetBlockUndoEnabled` / `UndoBlock` / `RedoBlock` | ✅ | Per-block undo stack with configurable depth. |
| Block savepoints | `FormsManager.CreateBlockSavepoint` / `RollbackToSavepointAsync` | ✅ | Snapshots current record by reflection. |
| `WHEN-NEW-BLOCK-INSTANCE` | `TriggerManager` (auto-fires on `SwitchToBlockAsync`) | ✅ | |
| `WHEN-REMOVE-RECORD` | ⚠️ fires on `DeleteCurrentRecordAsync` via `EventManager.OnBlockLeave` | ⚠️ | Naming differs slightly — confirm in your form code. |

## 3. Record navigation

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `FIRST_RECORD` | `FormsManager.FirstRecordAsync(blockName)` / `IBeepBuiltins.FirstRecord()` | ✅ | |
| `LAST_RECORD` | `FormsManager.LastRecordAsync(blockName)` / `IBeepBuiltins.LastRecord()` | ✅ | |
| `NEXT_RECORD` | `FormsManager.NextRecordAsync(blockName)` / `IBeepBuiltins.NextRecord()` | ✅ | |
| `PREVIOUS_RECORD` | `FormsManager.PreviousRecordAsync(blockName)` / `IBeepBuiltins.PreviousRecord()` | ✅ | |
| `GO_RECORD(n)` (1-based) | `FormsManager.NavigateToRecordAsync(blockName, recordIndex)` / `IBeepBuiltins.GoRecord(oneBased)` | ⚠️ | Engine is 0-based; `IBeepBuiltins` adapts to 1-based. |
| Navigation history (back/forward) | `FormsManager.NavigateBackAsync` / `NavigateForwardAsync` | ✅ | Per-block history stack. |
| `SYSTEM.CURSOR_RECORD` | `FormsManager.GetCurrentRecordInfo(blockName)` | ✅ | |
| `SYSTEM.CURSOR_BLOCK` | `FormsManager.CurrentBlockName` | ✅ | |

## 4. Item navigation

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `GO_ITEM(item)` | `FormsManager.GoItemAsync(blockName, itemName)` / `IBeepBuiltins.GoItem(itemName)` | ✅ | |
| `NEXT_ITEM` / `PREVIOUS_ITEM` | `FormsManager.NextItemAsync` / `PreviousItemAsync` | ✅ | Optional `currentItemName` to disambiguate. |
| `SET_ITEM_PROPERTY` | `FormsManager.ItemProperties.SetItemProperty(...)` / `IBeepBuiltins.SetItemProperty(itemName, property, value)` | ✅ | ~20 properties. |
| `GET_ITEM_PROPERTY` | `FormsManager.ItemProperties.GetItemProperty(...)` | ✅ | |
| `SYSTEM.LAST_RECORD` | `FormsManager` system variables | ✅ | `:SYSTEM.LAST_RECORD`, `:SYSTEM.LAST_QUERY`, etc. — see [`functionality/system-variables.md`](functionality/system-variables.md). |
| `WHEN-NEW-ITEM-INSTANCE` | `EventManager.OnRecordEnter` (per-item) | ⚠️ | Fires at the record level; per-item event not split out. |

## 5. Query mode

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `ENTER_QUERY` | `FormsManager.EnterQueryModeAsync(blockName)` / `IBeepBuiltins.EnterQuery()` | ✅ | Validates unsaved changes first. |
| `EXECUTE_QUERY` | `FormsManager.ExecuteQueryAsync(blockName, filters)` / `FormsManager.ExecuteQueryAndEnterCrudModeAsync(...)` / `IBeepBuiltins.ExecuteQuery()` | ✅ | `ExecuteQueryAsync` runs query in current mode; `ExecuteQueryAndEnterCrudModeAsync` runs query and returns to CRUD mode. |
| `EXIT_QUERY` (cancel) | `IBeepBuiltins.ExitQuery()` | ✅ | |
| Query with `&` filter (column=value) | filter list passed to `ExecuteQueryAsync` | ✅ | `List<AppFilter> { new AppFilter { FieldName, Operator, FilterValue } }`. |
| `SYSTEM.LAST_QUERY` | `_systemVariablesManager` | ✅ | |
| `SYSTEM.CURSOR_RECORD` (after query) | post-query event | ✅ | |
| `ABORT_QUERY` | `RollbackToSavepointAsync` (for "back to before-ENTER_QUERY" semantics) | ⚠️ | No direct `ABORT_QUERY`; closest is the savepoint-rollback path. |

## 6. DML operations

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `INSERT_RECORD` | `FormsManager.InsertRecordAsync` / `IBeepBuiltins.Post()` (record-level commit) | ✅ | |
| `UPDATE_RECORD` | `FormsManager.UpdateCurrentRecordAsync` | ✅ | |
| `DELETE_RECORD` | `FormsManager.DeleteCurrentRecordAsync` | ✅ | |
| `COMMIT_FORM` | `FormsManager.CommitFormAsync` | ✅ | Validates first if `Configuration.ValidateBeforeCommit == true`. |
| `ROLLBACK_FORM` | `FormsManager.RollbackFormAsync` | ✅ | |
| `COMMIT` (record-level) | `IBeepBuiltins.Commit()` / `CommitAsync(ct)` | ✅ | |
| `POST` (record-level) | `IBeepBuiltins.Post()` / `PostAsync(ct)` | ✅ | |
| `POST-RECORD` (deprecated synonym) | `IBeepBuiltins.Post` | ✅ | |
| `POST-AND-COMMIT` (deprecated) | `Post` then `Commit` | ✅ | Two calls. |
| `POST-ALL` (deprecated) | `IBeepBuiltins.Post` per record + `Commit` | ✅ | |
| `VALIDATE_RECORD` | `FormsManager.ValidateBlock(blockName)` | ✅ | |
| `VALIDATE_FORM` | `FormsManager.ValidateForm()` | ✅ | |
| `WHEN-VALIDATE-RECORD` | `TriggerManager` + `ValidationManager.ValidateBlock` | ✅ | |
| `WHEN-VALIDATE-ITEM` | `ValidationManager.ValidateItem` (timing = OnChange) | ✅ | |
| `ON-ERROR` | `EventManager.OnError` | ✅ | |

## 7. Triggers

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `WHEN-NEW-FORM-INSTANCE` | `OnFormOpen` event | ✅ | |
| `WHEN-NEW-BLOCK-INSTANCE` | auto-fired on `SwitchToBlockAsync` | ✅ | |
| `WHEN-NEW-RECORD-INSTANCE` | `OnRecordEnter` event | ✅ | |
| `WHEN-VALIDATE-RECORD` | `OnValidateRecord` + `ValidationManager` | ✅ | |
| `WHEN-VALIDATE-ITEM` | `OnValidateField` + `ValidationManager.ValidateItem(..., OnChange)` | ✅ | |
| `WHEN-*-POST-QUERY` | `OnPostQuery` | ✅ | |
| `WHEN-*-PRE-INSERT` | `OnPreInsert` | ✅ | |
| `WHEN-*-POST-INSERT` | `OnPostInsert` | ✅ | |
| `WHEN-*-PRE-UPDATE` | `OnPreUpdate` | ✅ | |
| `WHEN-*-POST-UPDATE` | `OnPostUpdate` | ✅ | |
| `WHEN-*-PRE-DELETE` | `OnPreDelete` | ✅ | |
| `WHEN-*-POST-DELTE` (sic — Oracle has the typo) | `OnPostDelete` | ✅ | |
| `WHEN-*-PRE-COMMIT` | `OnPreCommit` | ✅ | |
| `WHEN-*-POST-COMMIT` | `OnPostCommit` | ✅ | |
| `WHEN-TIMER-EXPIRED` | `TimerManager.TimerFired` event + `TimerFiredEventArgs` | ✅ | |
| `KEY-` triggers (KEY-F1, KEY-EXIT, etc.) | `FormsManager.RegisterKeyTrigger` / `FireKeyTriggerAsync` | ✅ | |
| `WHEN-CUSTOM-ITEM-EVENT` | `EventManager.OnCustomItemEvent` event + `TriggerCustomItemEvent` method; `TriggerType.WhenCustomItemEvent = 178` | ✅ | First-class trigger type with dedicated event args (`CustomItemEventArgs`) carrying `EventType`, `BlockName`, `ItemName`, `Payload`, and `Properties` dictionary. |
| `RAISE_FORM_TRIGGER_FAILURE` | `IBeepBuiltins.RaiseFormTriggerFailure(failureCode, message)` | ✅ | Throws `BeepBuiltinException` with a Forms-style code. |
| Trigger chaining (sequencing, dependencies) | `TriggerManager` + `TriggerDependencyManager` | ✅ | The dependency manager handles ordering and cycle detection. |
| Trigger execution log | `FormsManager.TriggerLog` / `GetTriggerLog` | ✅ | |

## 8. Validation

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| Built-in validation (`NOT NULL`, range, etc.) | `ValidationManager` + `ValidationRuleLibrary` | ✅ | Includes `Required`, `Range`, `Length`, `Regex`, `CompareField`, `Lookup`. |
| Custom validation rules | `ValidationRule` + `ValidationRuleBuilder` | ✅ | Fluent API: `manager.Validation.Rules.Add(ValidationRule.Required(...))`. |
| `WHEN-VALIDATE-ITEM` (timing = OnChange) | auto-fires on every field change | ✅ | |
| `WHEN-VALIDATE-RECORD` (timing = Pre-Commit) | runs before commit | ✅ | |
| Cross-block validation | `CrossBlockValidationManager` + `RegisterCrossBlockRule` | ✅ | Stops commit if any rule fails. |
| Severity levels (Error / Warning / Info) | `ValidationEnums` + `ValidationResult` | ✅ | |
| Validation timing (OnChange / OnValidate / Pre-Commit) | `ValidationTiming` enum | ✅ | |
| `SYSTEM.MODE` of validation | `ValidationManager.ValidationStarting` / `ValidationCompleted` events | ✅ | |

## 9. LOV (List of Values)

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `SHOW_LOV` | `FormsManager.ShowLOVAsync` / `IBeepBuiltins.ShowLov` | ✅ | Returns the selected value and writes it back to the bound field. |
| `LOV` (definition) | `LOVDefinition.CreateLookup(name, ds, entity, key, display)` | ✅ | Plus `MapField` for related-field mapping. |
| `LOV` properties (column display width) | `LOVColumn` width property | ⚠️ | Width is set; column reordering / hiding not exposed. |
| `LOV` validation on entry | `LOVManager.ValidateLOVValueAsync` (auto-fires on field change) | ✅ | |
| `LOV` cache | `LOVDefinition.CacheEnabled` + `LOVManager` | ✅ | |
| `POPUP_LOV` | `IBeepBuiltins.PopupLov` | ✅ | Returns the selected record. |
| `LIST_VALUES` | `IBeepBuiltins.ListValues` | ✅ | Returns records without selection. |

## 10. Item / block properties

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `SET_ITEM_PROPERTY` | `IBeepBuiltins.SetItemProperty` / `FormsManager.ItemProperties.SetItemProperty` | ✅ | ~20 properties: visible, enabled, required, default value, format mask, etc. |
| `GET_ITEM_PROPERTY` | `IBeepBuiltins.GetItemProperty` | ✅ | |
| Visual attributes (font, color) | ❌ not in engine | 🚫 | UI-specific. |
| Item-level `COPY_VALUE` from another item | `FormsManager.CopyFieldValue(sourceBlock, sourceField, targetBlock, targetField)` | ✅ | |
| Item default values (`:DEFAULT_VALUE` equivalent) | `FormsManager.SetItemDefault(blockName, itemName, () => value)` | ✅ | Lazy evaluation. |
| Item-level masking (security) | `FormsManager.SetFieldSecurity` + `GetMaskedFieldValue` | ✅ | |

## 11. Alerts and messaging

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `MESSAGE(text, severity)` | `FormsManager.SetMessage(text, level)` / `IBeepBuiltins.Message(text, ack, severity)` | ✅ | Severity maps: 0=Hint, 5=Warning, 10=Error, 15=Stop. |
| `CLEAR_MESSAGE` | `FormsManager.ClearMessage()` / `IBeepBuiltins.ClearMessage()` | ✅ | |
| `SHOW_ALERT(title, message, style, button1, button2, button3)` | `FormsManager.ShowAlertAsync` / `IBeepBuiltins.AlertAsync` | ✅ | Returns the button index (1/2/3). |
| `ALERT_BUTTON1` / `ALERT_BUTTON2` / `ALERT_BUTTON3` | return value of `ShowAlertAsync` | ✅ | |
| `AlertStyle` (Info / Caution / Stop / Note) | `BeepBuiltinAlertStyle` enum | ✅ | |
| Two-button confirm | `FormsManager.ConfirmAsync(title, message)` | ✅ | Returns bool. |
| `SYSTEM.MESSAGE_LEVEL` (0-25) | `BeepBuiltinMessageSeverity` enum | ✅ | |

## 12. Sequences

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `:SEQUENCE.NEXTVAL` | `FormsManager.GetNextSequence(name)` | ✅ | In-memory `SequenceProvider`. |
| `:SEQUENCE.CURRVAL` | `FormsManager.PeekNextSequence(name)` | ✅ | |
| `CREATE_SEQUENCE` | `FormsManager.CreateSequence(name, startValue, incrementBy)` | ✅ | |
| `DROP_SEQUENCE` | (not yet; reset = `ResetSequence` to 0) | ❌ | No first-class drop; reset to 0 has the same effect on next call. |
| Datasource-backed sequences | upstream `IUnitofWork` / `IDataSource` | ✅ | FormsManager rules say: prefer datasource sequences first. |

## 13. Timers

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `CREATE_TIMER(name, interval, repeating)` | `FormsManager.CreateTimer(name, interval, repeating)` | ✅ | Returns `TimerDefinition`. |
| `DELETE_TIMER(name)` | `FormsManager.DeleteTimer(name)` | ✅ | |
| `GET_TIMER(name)` | `FormsManager.GetTimer(name)` | ✅ | |
| `WHEN-TIMER-EXPIRED` | `TimerManager.TimerFired` event | ✅ | Routed through the host or directly via `TimerFiredEventArgs`. |
| `:SYSTEM.TIMER` (current timer name) | `TimerFiredEventArgs.TimerName` | ✅ | |

## 14. Multi-form and globals

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `CALL_FORM(target, params, modal)` | `FormsManager.CallFormAsync(target, params, FormCallMode.Modal)` | ✅ | Pushes onto call stack. |
| `OPEN_FORM(target, params)` (modeless) | `FormsManager.OpenFormAsync(target, params)` | ✅ | |
| `NEW_FORM(target)` | `FormsManager.NewFormAsync(target)` | ✅ | Closes all open forms first. |
| `RETURN` (to caller) | `FormsManager.ReturnToCallerAsync(returnData)` | ✅ | Pops call stack. |
| `:GLOBAL.var` (read) | `FormsManager.GetGlobalVariable(name)` | ✅ | |
| `:GLOBAL.var` (write) | `FormsManager.SetGlobalVariable(name, value)` | ✅ | |
| `:PARAMETER.var` (form parameter) | `FormsManager.GetFormParameter(name)` | ✅ | Set via the `parameters` dictionary on `OpenFormAsync` / `CallFormAsync`. |
| Inter-form messaging | `FormsManager.PostMessage` / `SubscribeToMessage` / `BroadcastMessage` | ✅ | Via `FormMessageBus`. |
| Cross-form shared blocks | `FormsManager.CreateSharedBlock` / `GetSharedBlock` / `TryLockSharedBlock` | ✅ | With lock semantics. |
| `EXIT_FORM` (close all) | `FormsManager.CloseForm` on the root form | ✅ | Engine-side. |
| `SYSTEM.FORM_STATUS` (CHANGED / NEW / QUERY) | `_systemVariablesManager` | ✅ | |

## 15. Security

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| Block-level security (`INSERT_ALLOWED`, `UPDATE_ALLOWED`, etc.) | `FormsManager.SetBlockSecurity(blockName, security)` | ✅ | |
| Item-level security | `FormsManager.SetFieldSecurity(blockName, itemName, security)` | ✅ | |
| Field masking | `FormsManager.GetMaskedFieldValue(blockName, itemName, rawValue)` | ✅ | |
| Role-based access | `SecurityContext.Roles` + `SecurityPermission` flags | ✅ | |
| Per-operation check | `FormsManager.IsBlockAllowed(blockName, SecurityPermission)` | ✅ | |
| `SYSTEM.FORM_STATUS` (security state) | `SecurityContext.CurrentContext` | ✅ | |
| Security violation event | `SecurityManager.OnSecurityViolation` | ✅ | |
| Encryption / hashing of sensitive data | ❌ not in engine | 🚫 | Host / application concern. |

## 16. Audit

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| Audit per-record changes | `AuditManager` + `ConfigureAudit` | ✅ | |
| Audit per-block | `AuditConfiguration.AuditedBlocks.Add(name)` | ✅ | |
| Audit per-field (granular) | `AuditFieldChange` | ✅ | |
| Audit log query | `FormsManager.GetAuditLog(blockName)` / `GetFieldHistory(blockName, fieldName)` | ✅ | |
| Audit log export | `FormsManager.ExportAuditToCsvAsync` / `ExportAuditToJsonAsync` | ✅ | |
| Audit log purge | `FormsManager.PurgeAudit(olderThanDays)` | ✅ | |
| Audit user | `FormsManager.SetAuditUser(userName)` | ✅ | |
| Audit store (file / in-memory) | `IFileAuditStore` / `IInMemoryAuditStore` (DI) | ✅ | Pluggable. |
| Audit on commit only vs. on every change | `AuditConfiguration` (default: on commit) | ✅ | |
| Field-level "who changed what" history | `GetFieldHistory` | ✅ | |

## 17. System variables (`:SYSTEM.*`)

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `:SYSTEM.CURSOR_BLOCK` | `SystemVariables.CursorBlock` | ✅ | |
| `:SYSTEM.CURSOR_RECORD` | `SystemVariables.CursorRecord` | ✅ | |
| `:SYSTEM.CURSOR_ITEM` | `SystemVariables.CursorItem` | ✅ | |
| `:SYSTEM.CURSOR_VALUE` | `SystemVariables.CursorValue` | ✅ | |
| `:SYSTEM.MODE` (Enter-Query / Normal) | `SystemVariables.Mode` | ✅ | |
| `:SYSTEM.BLOCK_STATUS` (CHANGED / NEW / QUERY) | `SystemVariables.BlockStatus` | ✅ | |
| `:SYSTEM.FORM_STATUS` (CHANGED / NEW / QUERY) | `SystemVariables.FormStatus` | ✅ | |
| `:SYSTEM.LAST_RECORD` | `SystemVariables.LastRecord` | ✅ | |
| `:SYSTEM.LAST_QUERY` | `SystemVariables.LastQuery` | ✅ | |
| `:SYSTEM.MESSAGE_LEVEL` (0-25) | `SystemVariables.MessageLevel` | ✅ | |
| `:SYSTEM.SUPPRESS_WORKING` | `SystemVariables.SuppressWorking` | ✅ | |
| `:SYSTEM.TIMER` (current timer) | `SystemVariables.Timer` | ✅ | |
| `:SYSTEM.TRIGGER_ITEM` / `:SYSTEM.TRIGGER_RECORD` / `:SYSTEM.TRIGGER_BLOCK` | `SystemVariables.Trigger*` | ✅ | |

## 18. Performance / paging

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| Block-level paging | `FormsManager.SetBlockPageSize` / `LoadPageAsync` | ✅ | Paging is tracked; total count and fetch-ahead are caller-owned. |
| Fetch-ahead | `FormsManager.SetFetchAheadDepth(blockName, depth)` | ✅ | |
| Lazy load | `FormsManager.SetLazyLoadMode(blockName, mode)` | ✅ | |
| Max records per fetch | `FormsManager.SetMaxRecordsPerFetch(blockName, max)` | ✅ | |
| Cache invalidation | `FormsManager.InvalidateBlockCache(blockName)` | ✅ | |
| Cache TTL | `FormsManager.SetBlockCacheTtl(blockName, ttl)` | ✅ | |
| Cache statistics | `FormsManager.GetCacheStats()` | ✅ | |
| Memory-pressure check | `FormsManager.CheckCacheMemoryPressure(thresholdMb)` | ✅ | |

## 19. Data operations (non-Oracle)

These are not Oracle Forms concepts but live alongside the Oracle surface:

| Concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| Block undo / redo | `FormsManager.UndoBlock` / `RedoBlock` | ✅ | |
| Block change summary | `FormsManager.GetBlockChangeSummary` | ✅ | |
| Refresh block | `FormsManager.RefreshBlockAsync` | ✅ | |
| Revert current record / specific record | `FormsManager.RevertCurrentRecord` / `RevertRecord` | ✅ | |
| Block query history | `FormsManager.GetBlockQueryHistory` | ✅ | |
| Block aggregate (sum, average, count) | `FormsManager.GetBlockSum` / `GetBlockAverage` / `GetBlockCount` | ✅ | |
| Block batch commit | `FormsManager.CommitFormBatchAsync` / `CommitBlockBatchAsync` | ✅ | |
| Block export (JSON / CSV / DataTable) | `FormsManager.ExportBlockToJsonAsync` / `ExportBlockToCsvAsync` / `GetBlockAsDataTable` | ✅ | |
| Block import (JSON / CSV) | `FormsManager.ImportBlockFromJsonAsync` / `ImportBlockFromCsvAsync` | ✅ | |
| Block group-by | `FormsManager.GetBlockGroups(blockName, fieldName)` | ✅ | |
| Form state snapshot / restore | `FormsManager.SaveFormState` / `RestoreFormStateAsync` | ✅ | |
| Clone block data | `FormsManager.CloneBlockDataAsync` | ✅ | |
| Duplicate current record | `FormsManager.DuplicateCurrentRecordAsync` | ✅ | |

## 20. Master/Detail

| Oracle Forms concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `CREATE_RELATION` (master/detail) | `FormsManager.CreateMasterDetailRelation(master, detail, masterKey, detailFK)` | ✅ | |
| Auto-detail synchronization on master current change | `unitOfWork.CurrentChanged` → `SynchronizeDetailBlocksAsync` | ✅ | |
| Detail block filter from master | automatic via `MasterDetailKeyResolver` | ✅ | |
| Master/detail coordinator (multi-level) | recursive `SynchronizeDetailBlocksAsync` | ✅ | |
| Detail block prevent-orphan check | `DependsOn` / `DependencyManager` | ✅ | |
| Manual detail refresh | `FormsManager.RefreshBlockAsync` on the detail block | ✅ | |

## 21. Configuration

| Concept | Counterpart | Status | Notes |
| --- | --- | --- | --- |
| `UnitofWorksManagerConfiguration` (top-level config DTO) | `FormsManager.Configuration` | ✅ | `Configuration/UnitofWorksManagerConfiguration.cs`. |
| `FormConfiguration` (per-form) | `Configuration/FormConfiguration.cs` | ✅ | |
| `NavigationConfiguration` (per-form navigation rules) | `Configuration/NavigationConfiguration.cs` | ✅ | |
| `PerformanceConfiguration` (per-form perf) | `Configuration/PerformanceConfiguration.cs` | ✅ | |
| `ValidationConfiguration` (per-form validation) | `Configuration/ValidationConfiguration.cs` | ✅ | |
| `ConfigurationManager` (DI helper) | `Configuration/ConfigurationManager.cs` | ✅ | |

## 22. Built-ins surface (`IBeepBuiltins`)

The `IBeepBuiltins` interface is the canonical "give me the Oracle Forms surface" entry point. It is a thin routing layer that maps each method to the corresponding `FormsManager` method (or to a host-routed call via `IBuiltinHost` for UI-specific built-ins).

Categories of built-ins:

| Category | Methods | Status |
| --- | --- | --- |
| Identity | `CurrentBlock`, `CurrentItem`, `Host` | ✅ |
| Block navigation | `GoBlock`, `NextBlock`, `PreviousBlock`, `FirstBlock`, `LastBlock` | ✅ |
| Record navigation | `FirstRecord`, `LastRecord`, `NextRecord`, `PreviousRecord`, `GoRecord` | ✅ |
| Item navigation | `GoItem`, `NextItem`, `PreviousItem` | ✅ |
| Item / block properties | `SetItemProperty`, `GetItemProperty`, `SetBlockProperty`, `GetBlockProperty` | ✅ |
| LOV | `ShowLov` (with and without selected-value out-param), `PopupLov`, `ListValues` | ✅ |
| Transaction / lifecycle | `Commit` / `CommitAsync`, `Rollback` / `RollbackAsync`, `Post` / `PostAsync` | ✅ |
| Query mode | `EnterQuery`, `ExecuteQuery` / `ExecuteQueryAsync`, `ExitQuery` | ✅ |
| Clear / reset | `ClearBlock`, `ClearForm`, `ClearRecord` | ✅ |
| Mode introspection | `GetBlockMode`, `SetBlockMode` | ✅ |
| Messaging | `Message`, `ClearMessage`, `AlertAsync` | ✅ |
| Diagnostics | `GetAvailableBuiltins` | ✅ |
| Multi-form | `OpenForm`, `CloseForm`, `GoForm`, `SetGlobal`, `GetGlobal` | ✅ |
| Application / form property bag | `SetApplicationProperty`, `GetApplicationProperty`, `SetFormProperty`, `GetFormProperty` | ✅ |
| Trigger failure | `RaiseFormTriggerFailure` | ✅ |

## 23. Out-of-scope (host UI responsibilities)

These are deliberately owned by the host UI, not the engine:

- **Visual attributes** — font, color, background, foreground, prompt color.
- **Window properties** — title, position, size, icon, modal/non-modal window behavior (in the host sense, not the Oracle `MODAL` keyword).
- **Layout** — canvas position, tab order, item alignment.
- **Mouse / keyboard focus** — keystroke routing, focus loss / focus enter, accelerator keys.
- **Tab pages** — `TAB_PAGE`-style multi-page canvases.
- **Data blocks as visual elements** — `BeepBlock` / `BeepForms` are the WinForms host; the engine doesn't render them.
- **PL/SQL libraries** — Oracle's `PACKAGE` / `FUNCTION` semantics. The engine has no equivalent and does not attempt one.

## 24. Status legend

- ✅ **complete** — full semantic parity. The host UI can call the corresponding method and get the same behavior as Oracle Forms.
- ⚠️ **partial** — concept is implemented but with one or more documented limitations. See the notes column.
- ❌ **missing** — concept is in scope for the engine but not yet implemented. See [`gaps.md`](gaps.md).
- 🚫 **out of scope** — concept is owned by the host UI. The engine deliberately does not implement it.

## See also

- [`architecture.md`](architecture.md) — what the engine *is*.
- [`gaps.md`](gaps.md) — the implementation roadmap for ⚠️ and ❌ items.
- [`enhancements.md`](enhancements.md) — opportunities beyond just closing gaps.
- [`functionality/`](functionality/) — per-subsystem deep-dives.
