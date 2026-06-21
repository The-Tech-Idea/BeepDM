# Engine → UI Architecture

**Date:** 2026-06-18 — Simplified

---

## Architecture

```
Forms Engine (BeepDM)
  IUnitofWorksManager — full API (~285 methods)
    ▲                              ▲
    │ _formsManager (direct)       │ _formsHost.IBeepFormsHost (40 methods)
    │                              │
  BeepForms (Commands/Workflow)  BeepBlock (Navigation/Binding/Query)
    │                              │
    Form-level operations          Block-level operations
    RecordGroups, Params,          CRUD, Nav, Query, LOV,
    TEXT_IO, Sequences,            Validation, Data binding
    Timers, Lock, Dirty...
```

## IBeepFormsHost — Block contract only (40 methods)

| Category | Methods |
|---|---|
| Block lifecycle | RegisterBlock, TrySetActiveBlock, IsBlockRegistered, GetBlockInfo |
| Data | GetBlockFields, GetBlockData, GetCurrentBlockItem, GetDetailBlockNames |
| State | GetBlockRecordCount, SetBlockCurrentRecordIndex, GetBlockMode, IsBlockQueryAllowed, IsFieldQueryAllowed, IsBlockDirty |
| Navigation | MoveFirst/Prev/Next/LastAsync |
| CRUD | SaveBlockAsync, RollbackBlockAsync, InsertBlockRecordAsync, DeleteBlockCurrentRecordAsync, ExecuteQueryAsync, ClearBlockAsync, ClearRecordAsync, DuplicateCurrentRecordAsync |
| Query mode | EnterQueryModeAsync, ExitQueryModeAsync |
| LOV | HasLov, GetLov, LoadLovDataAsync, ShowLovAsync, GetLovRelatedFieldValues |
| Validation | ValidateBlockRecord |
| Messaging | ShowInfo, ShowWarning, ShowError |

## Form-level operations — use _formsManager directly

These are accessible through `_formsManager.*` in the Forms layer. NO need on IBeepFormsHost.

| Group | Methods |
|---|---|
| Record Groups | CreateRecordGroup, PopulateRecordGroupAsync, GetRecordGroup... (7) |
| Parameter Lists | CreateParameterList, AddParameter, GetParameter<T>... (11) |
| Lock Management | LockCurrentRecordAsync, UnlockCurrentRecord... (6) |
| Dirty State | HasUnsavedChanges, GetDirtyBlocks, SaveDirtyBlocksAsync... (4) |
| Inter-Form | PostMessage, BroadcastMessage, SubscribeToMessage... (5) |
| Multi-Form | CallFormAsync, NewFormAsync, ReturnToCallerAsync (3) |
| TEXT_IO | ReadTextFileAsync, WriteTextFileAsync... (6) |
| Bookmarks | SetBlockBookmark, GoToBlockBookmark... (4) |
| Computed | RegisterBlockComputed, GetBlockComputedValue... (5) |
| Search/Clone | FindBlockRecordAsync, CloneBlockRecordAsync... (3) |
| Nav History | NavigateBackAsync, GetNavigationHistory... (6) |
| Form State | SaveFormState, RestoreFormStateAsync (2) |
| Query History | GetBlockQueryHistory, ClearBlockQueryHistory (2) |
| Sequences | CreateSequence, GetNextSequence... (5) |
| Timers | CreateTimer, DeleteTimer... (6) |
| ShowAlert | ShowAlertAsync (1) |

## No gaps — architecture is correct

Blocks get everything they need through IBeepFormsHost (40 methods).
Forms layer has full engine access through _formsManager (~285 methods).
No duplication. No missing surface.
