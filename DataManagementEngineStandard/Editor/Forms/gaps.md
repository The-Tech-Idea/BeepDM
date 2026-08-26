# FormsManager — Gaps (CRUD & Data Management Focus)

This document lists the gaps between the current engine and full Oracle Forms
emulation, **scoped to CRUD, data management, and database data entry**. UI
rendering concerns (windows, menus, images, visual attributes, reports, OLE)
are deliberately excluded — they are the host UI's responsibility.

Items marked ⚠️ partial or ❌ missing in [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md).

**Audit date:** 2026-06-17 — All 37 P0-P3 gaps resolved (32 fixed/enhanced, 5 deferred). 
Second pass closed 8 additional code-quality issues (duplication, DI bypass, stubs, naming).

**2026-08-22 pass** — sourced from an independent Oracle-Forms-parity catalog built from
the sibling `Beep.Forms` repo's IDE/host code, cross-checked against this file and
`ORACLE-FORMS-MAPPING.md` before anything was changed (several items the catalog first
flagged as gaps turned out to already be implemented here — inter-form globals, the full
QBE operator enum, query-only blocks, validate-from-list — see the catalog document for
the corrected picture). What follows is genuinely new. All items have a regression test in
`FormsManager.Tests` under `#region DML Trigger Wiring (2026-08-22)`; each was confirmed to
fail against the pre-fix code before the fix was applied (not merely written after).

### G0.15: `ON-INSERT`/`ON-UPDATE`/`ON-DELETE` triggers never fired (FIXED 2026-08-22)

**What:** `FireOnInsertAsync`/`FireOnUpdateAsync`/`FireOnDeleteAsync`
(`FormsManager.DmlTriggers.cs`, "Phase 4.2") were fully implemented — correct
handled/not-registered/cancelled semantics — with **zero call sites anywhere in the
engine**. A form that registered an ON-INSERT/ON-UPDATE/ON-DELETE trigger, expecting
Oracle's "this replaces the default DML," got nothing: no error, no log, the trigger
simply never ran.

**Fix:** Wired `FireOnUpdateAsync`/`FireOnDeleteAsync` into
`UpdateCurrentRecordAsync`/`DeleteCurrentRecordAsync` directly — both write to the
datasource immediately, so "handled → skip the default `UpdateAsync`/`DeleteAsync` call"
is exact, no double-write is possible. `FireOnInsertAsync` is wired into the commit path
(`TryCrossFormTransactionCommitAsync`, both the single-form and cross-form branches), since
Oracle Forms' `CREATE_RECORD` only stages a record — the actual `INSERT` happens at
`COMMIT_FORM`, deep inside `DirtyStateManager.SaveDirtyBlocksAsync` → `UnitofWork.Commit()`
→ OBL's `CommitAllAsync`, which commits an entire batch of records in one call with no
per-record interception point this pass investigated safely.

**Known, deliberate limitation on ON-INSERT specifically:** unlike UPDATE/DELETE, a
registered ON-INSERT handler today **fires and can cancel the commit**, but does **not**
yet exclude its record from the batched default insert that follows — full Oracle
semantics ("the trigger's write is the only write") needs per-record exclusion inside
`CommitAllAsync`, which is a separate, larger change into OBL. Documented in code
(`FormsManager.DmlTriggers.cs`, `FireOnInsertForDirtyBlocksAsync`'s remarks) so nobody
builds an ON-INSERT handler assuming the default write is skipped until that lands.

**Where:** `FormsManager.DmlTriggers.cs` (`FireOnInsertForDirtyBlocksAsync`, new),
`FormsManager.EnhancedOperations.cs` (`UpdateCurrentRecordAsync`),
`FormsManager.BasicDataOps.cs` (`DeleteCurrentRecordAsync`),
`FormsManager.FormOperations.cs` (`TryCrossFormTransactionCommitAsync`).

**Risk of fix:** Low for UPDATE/DELETE (additive, gated on a trigger actually being
registered — zero behaviour change for every form that doesn't use ON-UPDATE/ON-DELETE).
Low for the partial ON-INSERT fix too, for the same reason, but read the limitation above
before relying on "replaces" semantics for INSERT specifically.

### G0.16: `ON-LOCK`/`ON-ROLLBACK` triggers never fired (FIXED 2026-08-22)

**Fix:** Added `FireOnLockAsync`/`FireOnRollbackAsync` (same shape as G0.15's helpers).
ON-LOCK is wired ahead of the two `_lockManager.AutoLockIfNeededAsync` call sites
(`DeleteCurrentRecordAsync`, `UpdateCurrentRecordAsync`) — a registered handler skips the
default client-side lock, full replace semantics (locking is synchronous/immediate, no
OBL batching involved). ON-ROLLBACK is wired into `RollbackFormAsync`, fired per dirty
block before the batched `DirtyStateManager.RollbackDirtyBlocksAsync` call; a block whose
ON-ROLLBACK handler ran is excluded from that batch, so it is not also rolled back by the
default path.

**Where:** `FormsManager.DmlTriggers.cs`, `FormsManager.BasicDataOps.cs`,
`FormsManager.EnhancedOperations.cs`, `FormsManager.FormOperations.cs`.

**Risk of fix:** Low — same additive/gated shape as G0.15.

### G0.17: `WHEN-LOV-VALIDATE` — spelling mismatch against Oracle's canonical name (FIXED 2026-08-22)

**What:** The engine member was `WhenLOVValidate` (missing Oracle's "-ion" suffix). It DID
fire correctly (`FormsManager.GenericOperations.cs`, `ShowLOVAsync`) — this was never a
"doesn't fire" bug like G0.15/G0.16 — but `TriggerTypeNames.TryToMember` in the Beep.Forms
IDE resolves an Oracle-style name via `Enum.TryParse`, and "When-LOV-Validation" (Oracle's
actual name) never matched "WhenLOVValidate", so the IDE could never author a registration
for it even though the engine fully supported one.

**Fix:** Renamed the enum member to `WhenLOVValidation`. Two production call sites (the
declaration and the one firing site) — see the doc comment left in place on the member for
anyone who finds `WhenLOVValidate` in an old note or commit message.

**Where:** `Models/TriggerEnums.cs`, `FormsManager.GenericOperations.cs`.

**Risk of fix:** Low. Enum member renames are a breaking change for anyone who already
compiled against the old name — but grep across both this repo and the sibling Beep.Forms
repo found no other reference, and this engine has not shipped a NuGet release since the
member was added, so no external consumer can exist yet.

### G0.18: `WHEN-VALIDATE-FORM` — no engine trigger existed at all (FIXED 2026-08-22)

**What:** `ValidateForm()` looped every block's `ValidateBlock()` and raised a plain .NET
`OnFormValidate` event, but never fired a `TriggerType` — because none existed. A form
could not register whole-form validation logic the way Oracle Forms' `WHEN-VALIDATE-FORM`
does; the closest existing triggers (`WhenValidateItem`, `WhenValidateRecord`) are scoped
below the form level.

**Fix:** Added `TriggerType.WhenValidateForm = 12` (the reserved 12-19 form-level range).
Fired synchronously in `ValidateForm()`, in the same place `OnFormValidate` already fires,
mirroring `PreCommit`'s cancellation convention — `TriggerResult.Cancelled` stops
validation before any block is checked.

**Where:** `Models/TriggerEnums.cs`, `FormsManager.FormOperations.cs`.

**Risk of fix:** Low — purely additive (new enum member, new fire point that only matters
to a form that registers a `WhenValidateForm` handler).

### G0.19: `AppFilter` `LIKE`/`NOT LIKE` operator threw `ArgumentException` (FIXED 2026-08-22)

**What:** `QueryBuilderManager.OperatorToString` correctly maps `QueryOperator.Like`/
`NotLike` (Oracle's generic QBE pattern-match operator) to the strings `"like"`/`"not
like"` — but the two general-purpose filter appliers that actually evaluate an `AppFilter`
against data, `Utils/Util.cs` (`GenerateFilterExpression`, both the SQL-string and the
in-memory boolean overloads) and `Json/JsonExtensions.cs` (an exact, independently
maintained duplicate of the same two methods), had no `case` for either string and fell
through to `default: throw new ArgumentException(...)`. Any query-by-example using a raw
LIKE pattern (as opposed to the `contains`/`startswith`/`endswith` convenience operators,
which were unaffected) threw instead of filtering.

**Fix:** Added `"like"`/`"not like"` cases to all four switches (two per file). The
in-memory boolean overloads needed an actual SQL-LIKE-pattern matcher (`%`/`_` wildcards)
since .NET has no built-in one; added `Util.IsSqlLikeMatch` (public static) and had
`JsonExtensions` call it rather than duplicating the regex a third time.

**Where:** `Utils/Util.cs`, `Json/JsonExtensions.cs`.

**Risk of fix:** Low — additive `case` labels; every previously-working operator is
untouched. Worth noting for whoever next touches either file: `Util.cs` and
`JsonExtensions.cs` carry two independently-maintained, near-identical copies of this
filter-evaluation logic (predates this pass) — a real duplication (this repo's own rule
against exactly this shape), not fixed here since it's a larger refactor than this pass's
scope, but flagged so the next bug found in one of them gets checked in both.

### G0.20: `COUNT_QUERY` — no count-without-fetch existed (FIXED 2026-08-22)

**What:** `IBeepBuiltins`/`FormsManager.GetBlockRecordCount` counts already-loaded rows;
nothing asked the datasource for a count of records matching the current query criteria
*without* fetching them, which is what Oracle Forms' `COUNT_QUERY` does (so the user can
see "this query will return N records" before committing to a page-through).

**Fix:** Added `FormsManager.CountQueryAsync(blockName, filters, ct)`. Merges the block's
default WHERE clause with the caller's filters exactly like `ExecuteQueryAsync` (so the two
always agree on what "matches"), then asks the datasource directly via the formally-declared
`IDataSource.GetScalarAsync(string)` — no reflection needed, unlike the older
`GetBlockAggregateScalarAsync` (G3.10), which reflects on `GetScalarAsync` defensively; this
one is on the actual interface. Returns -1 (not 0) when the block, entity, or datasource
can't be resolved, or the datasource throws — 0 would misread as "no matching records."
Deliberately does **not** fall back to fetch-then-count for datasources that can't answer a
COUNT query: a fallback that fetches would disturb the block's currently loaded records,
which breaks Oracle's own COUNT_QUERY contract ("counts without changing what's in the
block") — returning -1 and logging why is more honest than a fallback that quietly violates
that contract.

**Where:** `FormsManager.BasicDataOps.cs` (new method, next to `ExecuteQueryAsync`),
`Interfaces/IUnitofWorksManager.cs` (new interface member).

**Risk of fix:** Low — new, additive method; nothing existing changed.

### G0.21: Master-detail delete behavior (isolated/non-isolated/cascading) — no distinction existed (FIXED 2026-08-22)

**What:** Deleting a master record never checked whether it had detail records at all —
`DeleteCurrentRecordAsync`'s only detail-block interaction was an unsaved-changes check, not
an existence check. No isolated/non-isolated/cascading distinction, and no
`ON-CHECK-DELETE-MASTER` firing, existed anywhere. (A previous attempt at this — a bare
`CascadeDelete` bool on `DataBlockRelationship` — was removed 2026-06 as an unwired
placeholder; see that class's remarks. This fix re-adds the capability and finishes wiring
it, rather than repeating the same mistake.)

**Fix:** `DataBlockRelationship.DeleteBehavior` (`MasterDeleteBehavior`: `NonIsolated`
[Oracle's default — block the delete while detail records exist], `Isolated` [allow it,
orphans permitted], `Cascading` [delete every detail record first, through its own full
`DeleteCurrentRecordAsync` pipeline — so the detail's own triggers, and any further
Cascading relationship *it* is a master of, fire normally]). `ON-CHECK-DELETE-MASTER` fires
per relationship before the default check; a registered handler replaces the default
check entirely (same "replaces the default" shape as G0.15/G0.16). `ON-CLEAR-DETAILS` fires
after a successful cascade. The cascade loop (`CascadeDeleteDetailRecordsAsync`) is bounded
by the starting record count and re-checks the count actually decreased after each delete,
so a UoW whose current-record pointer doesn't advance after delete fails loudly instead of
looping forever.

**Where:** `Models/DataBlockRelationship.cs` (new enums + properties),
`FormsManager.DmlTriggers.cs` (`FireOnCheckDeleteMasterAsync`),
`FormsManager.BasicDataOps.cs` (`DeleteCurrentRecordAsync`, `CascadeDeleteDetailRecordsAsync`).

**Risk of fix:** Medium for existing users with master-detail relationships and detail
records present at delete time: `NonIsolated` is the new default and Oracle's own default,
but the engine's *previous* behavior was "no check at all" (equivalent to `Isolated`) — a
form that already relies on deleting a master with existing, un-isolated detail rows will
now be blocked unless it explicitly sets `DeleteBehavior = Isolated` on the relationship.
This is a deliberate correctness fix (matching Oracle, not a silent behavior change for its
own sake), but worth flagging for anyone auditing behavior changes around this date.

### G0.22: Master-detail deferred coordination — only Immediate existed (FIXED 2026-08-22)

**What:** `SynchronizeDetailBlocksAsync`/`SynchronizeDetailHierarchyAsync` always re-queried
a detail block the instant its master's current record changed. Oracle Forms' `Deferred`
coordination property (don't re-query until something explicitly asks) had no equivalent.

**Fix:** `DataBlockRelationship.Coordination` (`DetailCoordination`: `Immediate` [Oracle's
default, this engine's only previous behavior] / `Deferred`). A Deferred relationship is
skipped entirely inside `SynchronizeDetailHierarchyAsync`'s loop (neither re-queried nor
cleared) and marked pending via a new `_pendingDeferredSync` set. New
`FormsManager.SynchronizeDeferredDetailAsync(masterBlockName, detailBlockName, ct)` forces
one specific deferred relationship current on demand (e.g. right before a host shows/enters
that detail block) by reusing `SynchronizeDetailHierarchyAsync`'s already-hardened
per-relationship logic rather than a second copy of it. New
`HasPendingDeferredSync(detailBlockName)` lets a host check before deciding whether to force
the sync.

**Where:** `Models/DataBlockRelationship.cs`, `FormsManager.Core.cs` (`_pendingDeferredSync`),
`FormsManager.Helpers.cs` (`SynchronizeDetailHierarchyAsync`'s loop),
`FormsManager.Relationships.cs` (`SynchronizeDeferredDetailAsync`, `HasPendingDeferredSync`,
and `GetActiveRelationships` made `public` — was `internal`, used only inside this class;
needed so a host or a relationship-configuring caller can read/adjust
`DeleteBehavior`/`Coordination` after `CreateMasterDetailRelation`).

**Risk of fix:** Low — `Coordination` defaults to `Immediate`, so every existing relationship
keeps today's only behavior unless a caller explicitly opts a relationship into `Deferred`.

### G0.23: Property Class, DEFAULT_VALUE, and "Copy Value from Item" — authored but never reached the runtime item store (FIXED 2026-08-22)

**What:** Three separate but related gaps, found together while implementing Property Class:
1. `BlockFieldDefinition` — the IDE's own per-field authoring model — carried `IsRequired`,
   `IsEnabled`, etc., but had no way to author FORMAT_MASK, DEFAULT_VALUE, "Copy Value from
   Item", or the finer per-operation QUERY/INSERT/UPDATE_ALLOWED flags at all. Worse: even the
   fields it *did* carry never reached the runtime `ItemInfo` store —
   `RegisterItemsFromEntityStructure` (called from `RegisterBlock`) only ever read the
   datasource's own column metadata (nullability, auto-increment, key); the designer's
   overrides on `BlockDefinition.EntityDefinition.Fields` were captured and then silently
   discarded. A field marked read-only for insert in the designer stayed insertable at
   runtime.
2. `ItemInfo.DefaultValue` and `ItemPropertyManager.ApplyDefaultValues(blockName, record)`
   already existed (SET_ITEM_PROPERTY / GET_ITEM_PROPERTY plumbing was complete) — but nothing
   called `ApplyDefaultValues` when a new record was actually created. DEFAULT_VALUE could be
   set and read back, but a new record never received it.
3. `FormsManager.Sequences.cs`'s `SetItemDefault`/`ApplyItemDefaults` (a registered-factory
   default mechanism, more general than a static DEFAULT_VALUE) was documented in its own
   XML comment as *"Called internally from CreateNewRecord after the record is constructed"* —
   but had zero callers anywhere in the engine. The comment described an intention that was
   never implemented.
4. Property Class (the Oracle Forms named-bundle inheritance mechanism) and "Copy Value from
   Item" did not exist in any form.

**Fix:**
- New `PropertyClass` model + `IPropertyClassManager`/`PropertyClassManager` (mirrors
  `VisualAttribute`/`IVisualAttributeManager` exactly), exposed as `FormsManager.PropertyClasses`
  / `IUnitofWorksManager.PropertyClasses`. Every field on `PropertyClass` is nullable —
  "not part of this class" — and `ApplyToItem(item, fieldDefinition)` resolves with a fixed
  precedence: **the field's own authored value wins → the property class fills whatever the
  field left unauthored → anything neither says keeps the item's existing (entity-structure-derived)
  value.** This is unambiguous by construction because it operates on `BlockFieldDefinition`'s
  new *nullable* authoring fields, not on `ItemInfo`'s own non-nullable runtime fields — there
  is never a question of whether `false` means "explicitly authored false" or "still at the
  engine default," because only the field/class layer is consulted for "was this authored."
- `BlockFieldDefinition` gained `PropertyClassName`, `FormatMask`, `HasDefaultValue` +
  `DefaultValue`, `CopyValueFromItem`, and nullable `QueryAllowed`/`InsertAllowed`/`UpdateAllowed`.
- `DefinitionBlockRegistrar.TryRegister` now calls a new `ApplyAuthoredFieldProperties` step
  right after `RegisterBlock` (which seeds `ItemInfo` from the entity structure): for every
  authored field with a matching `ItemInfo`, it calls `PropertyClasses.ApplyToItem`. This is
  the one place design-time authoring becomes runtime behavior for these properties — the same
  role `ApplyAuthoredKeys` already played for `IsPrimaryKey`.
- `ItemInfo` gained `CopyValueFromItem` ("BlockName.ItemName").
- `FormsManager.CreateNewRecord` now calls, in order, after the CLR instance is constructed and
  before WHEN-CREATE-RECORD fires: `ItemPropertyManager.ApplyDefaultValues` (static DEFAULT_VALUE),
  the new private `ApplyCopyValueFromItem` (reads the source item's current value from the item
  store and reflects it onto the new record's bound property), then `ApplyItemDefaults`
  (registered factories — most specific, so it can override either of the above for the same
  field). WHEN-CREATE-RECORD still fires last, so trigger logic can see and further override
  everything.

**Where:** `Models/PropertyClass.cs`, `Interfaces/IPropertyClassManager.cs` (new),
`Helpers/PropertyClassManager.cs` (new), `Models/BlockDefinition.cs` (`BlockFieldDefinition`),
`Models/ItemInfo.cs` (`CopyValueFromItem`), `Interfaces/IUnitofWorksManager.cs`
(`PropertyClasses`), `FormsManager.Core.cs` / `FormsManager.Properties.cs` (wiring),
`Helpers/DefinitionBlockRegistrar.cs` (`ApplyAuthoredFieldProperties`),
`FormsManager.EnhancedOperations.cs` (`CreateNewRecord`, `ApplyCopyValueFromItem`).

**Risk of fix:** Medium for `InsertAllowed`/`UpdateAllowed`/`QueryAllowed` specifically, for the
same reason as G0.21: a field the designer already marked read-only in one operation, that
previously had no runtime effect, now actually enforces it. Low everywhere else — `DefaultValue`,
`CopyValueFromItem`, and `FormatMask` are purely additive (nothing populated them before, so
nothing regresses), and `ApplyItemDefaults` had no prior callers to conflict with.

### G0.24: Two-phase/distributed commit — no transaction was ever opened, on any commit (FIXED 2026-08-22)

**What:** `CommitFormAsync`'s cross-form path (`TryCrossFormTransactionCommitAsync`) had a doc
comment claiming it "optionally wraps [the commit] in a single source-level transaction if every
participating form's data source supports transactions" and, on failure, "rolls back all
committed forms." Neither was implemented: no `IDataSource.BeginTransaction` call existed
anywhere in the method. Each form's dirty blocks were saved sequentially via
`UnitOfWork.Commit()`, which persists immediately and independently per block/record. The
"rollback" on failure called `RollbackDirtyBlocksAsync` on forms already reported as
successfully committed — by that point their blocks were typically no longer dirty, so the call
discarded in-memory state that no longer represented anything; it never issued a compensating
undo against the datasource, and could not have, since a plain sequential commit gives it nothing
to undo through. The single-form case — the far more common one, and the one most forms actually
hit — had **no coordination at all**: several dirty blocks on one or more datasources in one
form committed one at a time with nothing tying them together, and this path wasn't even inside
the (non-functional) cross-form wrapper.

**Fix:** Rewrote `TryCrossFormTransactionCommitAsync` (moved to the new
`FormsManager.TransactionCoordination.cs`) to group every dirty block in commit scope — across
however many forms are participating, including the single-form case, which no longer gets a
separate no-coordination fast path — by its **owning `IDataSource` instance**, not by form (one
form's blocks can span several datasources; several forms can share one). For each distinct
datasource: **prepare** by calling the already-declared `IDataSource.BeginTransaction` before any
block on it saves, then run every form's normal ON-INSERT + `SaveDirtyBlocksAsync` path exactly
as before (unchanged, so master-key propagation and commit ordering are untouched) — nothing is
durable yet on a transaction-capable datasource, so a prepare failure is now a true, clean
`EndTransaction` abort on every datasource that opened one, not a doomed attempt to undo an
already-persisted write. **Commit** by calling `IDataSource.Commit` on every opened transaction
only after every block's save succeeded. A datasource whose provider doesn't implement the
triple (`JsonDataSource`, `CSVDataSource`, and other file-backed sources throw
`NotImplementedException`) has no ACID mechanism to open — its blocks keep the previous
immediately-durable behavior, and a block that lands there before a later prepare-phase failure
on a *different* datasource is logged as a named, un-rollback-able partial commit rather than
silently folded into "commit failed." A failure between prepare and commit succeeding on some but
not all datasources — the one outcome no software-only coordinator across independent database
engines can prevent without a real distributed transaction coordinator (MS DTC or equivalent) —
is also logged by name rather than misreported as full success or a completed rollback.

Deliberately does not duplicate `IDistributedTransactionCoordinator`
(`DistributedDatasource/Distributed/DistributedDataSource.Transactions.cs`), which already
implements full 2PC/saga coordination — for datasources that are shards under one
`DistributedDataSource`. That is a different, narrower population (an application that has
explicitly adopted sharding) than the case this fix closes: several ordinary,
independently-configured transaction-capable datasources (e.g. two separate SQL Server
connections) committed together from one form or one call-stack of forms, which is the situation
`CommitFormAsync` actually faces and the one its own doc comment already claimed to handle.

**Where:** `FormsManager.TransactionCoordination.cs` (new — `TryCrossFormTransactionCommitAsync`
moved here from `FormsManager.FormOperations.cs`, `AbortOpenedTransactions` helper).

**Risk of fix:** Medium. A commit that previously "succeeded" by writing each block immediately,
uncoordinated, now genuinely opens a transaction per transactional datasource first — a datasource
whose `BeginTransaction` is implemented but flaky, slow, or contends under load surfaces that
failure as a whole-commit failure where before it was never exercised at all. This is a
correctness fix (the behavior now matches what was always documented and what Oracle Forms
commit semantics require), but any environment relying on the previous no-coordination behavior
for performance reasons should be re-tested.

### G0.25: WHEN-LOV-VALIDATION never fired on a typed value; its result was discarded; item error state was never set by any validation path (FIXED 2026-08-22)

**What:** Three compounding gaps found together while re-checking the WHEN-LOV-VALIDATION rename
from G0.17:
1. The `WhenLOVValidation` trigger (renamed from the misspelled `WhenLOVValidate` earlier in this
   pass) only fired from `ShowLOVAsync` — explicit LOV invocation (Oracle's SHOW_LOV). It never
   fired from the far more common case: a user types a value directly into a field that has an
   attached LOV, and the engine validates it against that LOV. A form author registering a
   WHEN-LOV-VALIDATION handler to enforce custom LOV logic (Oracle's primary documented use for
   this trigger) found it silently never ran for typed input.
2. That typed-value path (`ItemPropertyManager`'s `ItemChanged` handler, in `RegisterBlock`)
   called `LOVManager.ValidateLOVValueAsync` as `_ = _lovManager.ValidateLOVValueAsync(...)` —
   fire-and-forget on an `async Task`-returning method, inside a plain synchronous event handler.
   The `LOVValidationFailed` .NET event still fired as a side effect (so a host directly
   subscribed to it was unaffected), but nothing awaited the call, so an exception thrown inside
   it — e.g. the LOV's own datasource erroring — became an unobserved task exception: silently
   dropped, never reaching `_eventManager.TriggerError` the way every other exception in this
   class is required to.
3. `ItemPropertyManager.SetItemError`/`ClearItemError` — and the `HasItemError`/
   `GetItemErrorMessage`/`GetItemsWithErrors`/`ItemErrorChanged` surface they back — had **zero
   callers anywhere in the engine**, from any validation path, not just LOV. A host checking
   whether an item is currently invalid could never get `true` back no matter what actually
   failed.

**Fix:** The `ItemChanged` handler is now `async` (matching the established pattern immediately
below it in the same method, `mdHandler` for `CurrentChanged`/`SynchronizeDetailBlocksAsync`,
including its same reason: an unhandled exception from an async-void event handler is
unobservable, so the whole handler body is wrapped in try/catch routing to
`_eventManager.TriggerError`). For a field with an attached LOV, it now: fires
`WhenLOVValidation` first and awaits it — a handler that returns `Cancelled` rejects the value
outright (`SetItemError`, default LOV check skipped entirely, matching the "replaces default"
shape used elsewhere in this engine); otherwise awaits `ValidateLOVValueAsync` as before, but now
actually reads the result — `SetItemError` on failure, `ClearItemError` on success. This is
deliberately scoped to the LOV path only: `SetItemError`/`ClearItemError` having no callers is a
real, separate, and larger gap that also affects the plain field/record rule-based validation
path (`ValidationManager`'s `ValidationFailed`/`ValidationCompleted` .NET events fire correctly
but likewise never reach the per-item error store) — flagged here, not fixed, since wiring every
validation path through it is a materially bigger change than this pass's scope.

**Where:** `FormsManager.BlockRegistration.cs` (`RegisterBlock`'s `ItemChanged` handler).

**Risk of fix:** Low-Medium. A field with an attached LOV and a registered WHEN-LOV-VALIDATION
handler now actually has that handler run on every keystroke-driven change, not only on explicit
LOV invocation — a handler written expecting the old (silent) behavior now executes where it
didn't before. `SetItemError`/`ClearItemError` for LOV validation are purely additive (nothing
read `HasItemError` meaningfully before, since it could never become true).

### G0.26: `ReturnToCallerAsync` could never succeed for a real multi-form call (FIXED 2026-08-22)

**What:** Found while adding regression tests for the (otherwise correctly implemented)
CALL_FORM/OPEN_FORM/NEW_FORM surface. `CallFormAsync` pushes its `FormCallStackEntry` onto the
**caller's** own `_callStack` (`this` inside `CallFormAsync` is the caller). Every other consumer
of `_callStack` — `TryReleaseCallEntryFor`, and through it `ReleaseSuspendedCallerFor` — already
knew and handled this correctly: `ReleaseSuspendedCallerFor`'s own remarks say so explicitly
("The call-stack entry lives on the CALLER's manager, not the callee's... The form registry is
shared... which is what makes the lookup possible") and it searches every manager the registry
knows about when its own local stack doesn't have the entry. `ReturnToCallerAsync` — the public
method a callee calls to voluntarily hand control back, the normal (non-crash) return path — did
not: it only ever checked `this._callStack`, which is empty for a genuine callee running as its
own `FormsManager` instance (one instance per open form, registered by name — the architecture
this engine's own multi-form design otherwise assumes throughout, including in
`ReleaseSuspendedCallerFor` itself). The result: a callee could open (`CallFormAsync`, e.g. a
lookup dialog) and the caller would genuinely suspend as designed, but the callee's own
`ReturnToCallerAsync("selected value")` always failed — `_callStack.Count == 0` — so the caller
was left suspended forever unless the callee instead closed itself outright (hitting
`ReleaseSuspendedCallerFor`'s already-correct path — this is why the *crash/close* recovery case
worked, in a 2026-08-03 fix, while the *normal return* case, arguably the more common one, stayed
broken). A single-instance self-call (one `FormsManager` acting as both caller and callee) masked
this, since `this._callStack` happened to be the right stack in that shape — not the shape this
engine's own registry-based design otherwise targets.

**Fix:** `ReturnToCallerAsync` now uses the same fast-path-then-registry-search pattern
`ReleaseSuspendedCallerFor` already established: try releasing the entry from this manager's own
stack first, then search every other manager `IFormRegistry.GetActiveFormNames()` knows about.
`TryReleaseCallEntryFor` gained an `out FormCallStackEntry releasedEntry` overload (the original
2-arg signature kept, delegating to it) so `ReturnToCallerAsync` can still attach `returnData` to
`RETURN_VALUE` on the caller once the entry — and therefore `entry.CallerFormName` — is found,
regardless of which manager's stack it came from. The same foreign-entry protection is preserved:
`TryReleaseCallEntryFor` only pops when the stack's top entry actually names the expected callee.

**Where:** `FormsManager.MultiFormNavigation.cs` (`ReturnToCallerAsync`, `TryReleaseCallEntryFor`).

**Risk of fix:** Low. The previous behavior was a hang (suspended caller, no way to unblock it via
the intended return path) for the exact multi-instance shape this engine's registry is built
around — there is no working prior behavior for that shape to regress. A caller relying on the
single-instance self-call shape (where the bug was masked) keeps working: the fast local path is
tried first and still succeeds there.

### G0.27: Row-level security filter never enforced on any query — access-control bypass, not just a missing feature (FIXED 2026-08-22)

**What:** `BlockSecurity.RowFilterClause`/`.RowFilterValues` (e.g. `"TenantId = :TenantId"`, to
restrict a user to their own tenant's rows) and `ISecurityManager.GetBlockRowFilter`/
`GetBlockSecurity` existed with **zero callers anywhere in the engine**. `ExecuteQueryAsync` only
ever checked the coarse per-operation allow/deny flags via `EnforceBlockSecurity` ("can this user
query this block at all") and merged `block.DefaultWhereClause` (an unrelated, non-security
concept) into the query — the row filter itself was never merged in. Concretely: a form
configured with row-level security showed **every row to every permitted user**, not just the
rows that user's filter allows — a silent access-control bypass, not a missing convenience. The
newly-added `CountQueryAsync` (G0.20, same 2026-08-22 pass) had the identical gap on the same
code shape — it would have reported the true, unfiltered row count to a user who is not permitted
to see all of them, a second leak of the same restricted information via a different built-in.
`QueryBuilderManager.ParseCondition` already anticipated exactly this consumer — its own comment
reads *"Handle parameterized placeholders: Field = :1 or Field = :name — These are preserved
as-is so the caller can resolve them"* — the parser was built for this and nothing had ever been
the caller.

**Fix:** New `FormsManager.BuildSecurityRowFilters(blockName)`: reads `GetBlockSecurity(blockName)`,
parses `RowFilterClause` via the existing `QueryBuilderManager.ParseWhereClause` (same mechanism
`DefaultWhereClause` already used), then resolves any `:Name` placeholder `AppFilter` value
against `RowFilterValues`. Called from both `ExecuteQueryAsync` and `CountQueryAsync`, ANDed into
`finalFilters` alongside `DefaultWhereClause`, so a query and its count always agree on both what
"default" and what "permitted" mean. Scoped to these two entry points specifically — other places
a datasource is read directly (LOV loading, master-detail sync) are not covered by this fix and
are not claimed to be; `ExecuteQueryAsync`/`CountQueryAsync` are the two Oracle Forms built-ins
(EXECUTE_QUERY/COUNT_QUERY) this restriction is documented against.

**Where:** `FormsManager.BasicDataOps.cs` (`BuildSecurityRowFilters`, `ExecuteQueryAsync`,
`CountQueryAsync`).

**Risk of fix:** This closes a real information-disclosure gap; the "risk" is entirely on the side
of any deployment that configured `BlockSecurity.RowFilterClause` believing it was already
enforced (per the property's own doc comment, which describes exactly this behavior) — for that
deployment, rows that should always have been hidden start being hidden now, which is the correct
behavior, not a regression. No deployment could have been relying on the filter being ignored as
a feature.

### G0.28: WHEN-TIMER-EXPIRED — same fire-and-forget exception hazard as G0.25, fixed the same way (FIXED 2026-08-22)

**What:** `OnTimerManagerFired` (`FormsManager.Lifecycle.cs`, the handler for
`ITimerManager.TimerFired`) called `_ = _triggerManager.FireFormTriggerAsync(...)` —
fire-and-forget on an async `Task`, inside a synchronous event handler wrapped in a `try/catch`.
The catch only ever observed a *synchronous* throw (e.g. from building the `TriggerContext`); an
exception from the trigger's own execution — a registered WHEN-TIMER-EXPIRED handler throwing —
became an unobserved task exception, silently dropped instead of reaching `LogError`. Same defect
shape as G0.25's `ItemChanged` handler, found on a second pass specifically looking for this
pattern elsewhere in the class.

**Fix:** `OnTimerManagerFired` is now `async void` (matching the class's own established pattern
for this exact hazard — `mdHandler` for `CurrentChanged`, and the `ItemChanged` handler after
G0.25) and awaits `FireFormTriggerAsync` inside the same try/catch, so an exception during trigger
execution is now actually caught and logged.

**Where:** `FormsManager.Lifecycle.cs` (`OnTimerManagerFired`).

**Risk of fix:** Low. Purely a visibility fix — the trigger already ran either way; only whether a
failure inside it was reported changes. Not independently revert-tested (unlike this pass's other
fixes): the specific property under test — whether an exception is *observed* rather than silently
dropped — isn't reliably distinguishable from outside without hooking
`TaskScheduler.UnobservedTaskException`, so the regression test proves the trigger fires correctly
through the now-awaited path instead, which a fire-and-forget version would have passed too.

### G0.29: `SetItemError`/`ClearItemError` had no caller for field/record rule-based validation — completing the gap G0.25 deliberately left open (FIXED 2026-08-22)

**What:** G0.25 wired `SetItemError`/`ClearItemError` for the LOV validation path but explicitly
flagged, not fixed, the same gap for `ValidationManager`'s ordinary rule-based path
(`ValidateItem`/`ValidateRecord`, backing `ValidateField`/`ValidateBlock`/the `ItemChanged`
handler's own rule check). `ValidationManager.ValidationFailed`/`ValidationCompleted` fired
correctly as .NET events the entire time — a host subscribed directly to those was never
affected — but no code anywhere read an `ItemValidationResult`/`RecordValidationResult` and
pushed it into the per-item error store. A form with a registered `ValidationRule` (Required,
Range, Pattern, …) had `ItemPropertyManager.HasItemError`/`GetItemErrorMessage`/
`GetItemsWithErrors` never report the failure, no matter how many rules failed.

**Fix:** `ValidateField`, `ValidateBlock` (per-field, from `RecordValidationResult.ItemResults`),
and the `ItemChanged` handler's own rule check (`FormsManager.BlockRegistration.cs`) now read
their `ItemValidationResult`/`RecordValidationResult` and call `SetItemError`/`ClearItemError`.
The one real design question this raised: `ItemValidationResult.IsValid` is vacuously `true` when
zero rules are registered for a field (`!RuleResults.Any(...)` over an empty list) — naively
clearing on every "valid" result would let a record-level revalidation with no rules for a field
silently wipe out a real error a *different* check (LOV, or the same field's own per-keystroke
check) had already set on it. Fixed by only touching item error state when
`RuleResults.Count > 0` — something was actually evaluated — everywhere except the `ItemChanged`
handler, which composes the rule-check and the LOV-check within one atomic pass instead (only
clears when *both* agree the value is good), so the same false-clear can't occur there by
construction.

**Where:** `FormsManager.Validation.cs` (`ValidateField`, `ValidateBlock`),
`FormsManager.BlockRegistration.cs` (`ItemChanged` handler's rule-check/LOV composition).

**Risk of fix:** Low — purely additive item-error-state writes; nothing previously read
`HasItemError`/`GetItemErrorMessage` meaningfully, since they could never report a rule failure
before this fix.

### G0.30: Four Oracle Forms triggers (WHEN-CLOSE-FORM, WHEN-CLEAR-BLOCK, WHEN-FORM-NOTIFICATION,
WHEN-DATABASE-RECORD) and eight KEY-* names had no matching `TriggerType`/`KeyTriggerType`
member — closing out most of the remaining rows in the Beep.Forms
`ORACLE-FORMS-FEATURE-CATALOG.md` §13 "still open" list (FIXED 2026-08-24)

**What:** The Beep.Forms IDE's Add Trigger picker (`TriggerTypeNames.IsAuthorable`) offers the full
69-event Oracle Forms 12c catalog by parsing each display name and live-`Enum.TryParse`-ing it
against `TriggerType` — so an event with no matching member was listed but silently unauthorable.
Cross-referencing the catalog against `TriggerEnums.cs` found four genuine engine gaps and one
simple name mismatch:
- `WhenCloseForm`, `WhenFormNotification` (form-level), `WhenDatabaseRecord`, `WhenClearBlock`
  (block-level) — no engine member existed at all.
- `KeyListValues` existed but under a name that doesn't match the stripped Oracle display string
  `"Key-ListVal"` (same defect shape as G0.17's `WhenLOVValidate`→`WhenLOVValidation` rename) —
  renamed to `KeyListVal`; zero callers anywhere in either repo referenced the old name.
- Seven more `KeyTriggerType`/`TriggerType` members (`KeyPageUp`, `KeyPageDown`, `KeyTab`,
  `KeyDelete`, `KeyInsert`, `KeyClear`, `KeyForm`) had no member either — added into the
  already-documented "Reserved" numeric slots.

**Fix:**
- `TriggerEnums.cs`: added `WhenCloseForm = 13`, `WhenFormNotification = 14`,
  `WhenDatabaseRecord = 43`, `WhenClearBlock = 44`; renamed `KeyListValues` → `KeyListVal`; added
  `KeyPageUp/KeyPageDown/KeyTab/KeyDelete/KeyInsert/KeyClear/KeyForm = 162..168` to both
  `TriggerType` and the companion `KeyTriggerType` (same numeric values, since
  `FormsManager.KeyTriggers.cs` casts `(TriggerType)(int)key`).
- `CloseFormAsync` (`FormsManager.FormOperations.cs`) fires `WhenCloseForm` right before its
  existing cleanup step; a registered handler can still veto the close (`TriggerResult.Cancelled`
  → `CloseFormAsync` returns `false`, form stays open) — Oracle documents WHEN-CLOSE-FORM as
  restricted/cancellable.
- `ClearBlockAsync` (same file) fires `WhenClearBlock` as a notification, after `UnitOfWork.Clear()`
  — Oracle documents it unrestricted, so no cancellation gate.
- `OnMessageBusFormMessage` (`FormsManager.Lifecycle.cs`) fires `WhenFormNotification` alongside the
  existing plain `.NET` `OnFormMessage` event, carrying `SenderForm`/`MessageType`/`Payload` via
  `ctx.Parameters`; rewritten `async void` + `try/catch` (same fix shape as G0.28) since the fire is
  now genuinely async inside a `+=`-subscribed handler.
- `InsertRecordEnhancedAsync` (`FormsManager.EnhancedOperations.cs`) fires `WhenDatabaseRecord` as a
  notification, immediately *before* the existing `PreInsert` fire — Oracle's real firing order has
  WHEN-DATABASE-RECORD (announcing the record's transition into a database record) precede
  PRE-INSERT (the "about to insert" gate). This engine's own `PreInsert` already fires at
  add-to-block time rather than the true physical-commit moment (CREATE_RECORD stages the record;
  the actual INSERT happens later via `SaveDirtyBlocksAsync`/`UnitOfWork.Commit()`) — firing
  WHEN-DATABASE-RECORD immediately ahead of it is the closest honest match this engine's pipeline
  supports without a second firing pass over the commit path.
- `ExecuteKeyDefaultActionAsync` (`FormsManager.KeyTriggers.cs`) needed no change: its `default:
  return true` ("consumed silently") already covers the large majority of `KeyTriggerType` members
  (only `Enter/Commit`, `Exit`, `ExecuteQuery`, `NextRecord/PreviousRecord`, `NextBlock/PreviousBlock`
  get explicit handling), and the seven new keys are item-focus/UI-level concerns (grid paging,
  caret editing, focus movement) this engine layer doesn't own — inventing a default action for them
  would be fabricated behavior, not a fix.

**Where:** `TriggerEnums.cs`; `FormsManager.FormOperations.cs` (`CloseFormAsync`,
`ClearBlockAsync`); `FormsManager.Lifecycle.cs` (`OnMessageBusFormMessage`);
`FormsManager.EnhancedOperations.cs` (`InsertRecordEnhancedAsync`).

**Risk of fix:** Low for the three notification-style fires (`WhenClearBlock`,
`WhenFormNotification`, `WhenDatabaseRecord`) — additive, no existing caller could have depended on
a trigger type that didn't exist. Slightly higher for `WhenCloseForm`: it's cancellable, so a form
that registers a handler for it can now veto a close that previously always succeeded — this is the
intended, documented Oracle behavior, not a regression, but it is new observable behavior for any
form author who adds one. The `KeyListValues`→`KeyListVal` rename is a breaking source change for
any external caller of that literal enum member name; confirmed zero such callers exist in either
repo before renaming.

**Not fixed, out of scope for this pass:** Object Groups, the Editor (large-text popup) object, and
the three IDE authoring dialogs (Record Groups, Parameter Lists, Alerts) remain open — these are
substantially larger surfaces (new capabilities, not missing trigger members) and were not
investigated this pass. Also identified but **not fixed** — Beep.Forms-repo catalog data-quality
issues, not BeepDM engine gaps: `OracleFormsTriggerEvents.cs`'s `OracleFormsTriggerEvent.Commit`/
`.Rollback` bare entries (display strings `"Commit"`/`"Rollback"`) duplicate the catalog's own
already-correct `KeyCommit`/`KeyRollback` (`"Key-Commit"`/`"Key-Rollback"`); `.WhenTimer`'s display
string `"When-Timer"` likely should read `"When-Timer-Expired"` to match Oracle's real canonical
name and this engine's already-firing `TriggerType.WhenTimerExpired` (G0.28). Both are Beep.Forms
IDE catalog edits, not engine changes — flagged for a decision there rather than silently applied
here.

### G0.31: Alerts had no persisted, named ALERT object — `ShowAlertAsync` only ever took its
content literally, so there was nothing an IDE authoring dialog could register (FIXED 2026-08-25)

**What:** Oracle Forms' Alert is a design-time object: authored once (name, title, message, style,
button labels) and invoked from any trigger by name via `SHOW_ALERT('alert_name')`. This engine's
`ShowAlertAsync(title, message, style, ...)` (`FormsManager.Alerts.cs`) took all of that content as
literal parameters on every call — there was no persisted definition at all, unlike the already-built
Record Group (`IRecordGroupRegistry`) and Parameter List (`IParameterListManager`) registries this
same catalog entry was originally grouped with. A Beep.Forms.IDE authoring dialog for "Alerts" would
have had nothing to register against.

**Fix:** New `AlertDefinition` model (`AlertModels.cs`) — `Name`, `Title`, `Message`, `Style`,
`Button1Text`/`Button2Text`/`Button3Text`, `CreatedAt` — and a new `IAlertRegistry` interface
(`CreateAlert`, `GetAlert`, `GetAllAlerts`, `RemoveAlert`, `ClearAllAlerts`, `AlertExists`,
`ShowAlertByNameAsync`), implemented in `FormsManager.Alerts.cs` backed by a
`ConcurrentDictionary<string, AlertDefinition>`, matching the exact shape of the Record
Group/Parameter List registries in the same file family. `ShowAlertByNameAsync` resolves the named
definition and renders it through the *same* `ShowAlertAsync`/`IAlertProvider` path the ad-hoc
overload already uses, so there is exactly one rendering path, not two. Exposed flatly on
`IUnitofWorksManager`, matching how Record Groups/Parameter Lists are exposed (no `.Alerts`
sub-manager property, consistent with the existing convention for these two).

**Deliberately not implemented:** Oracle Forms' Alert also has a "Default Alert Button" property
(which button gets Enter-key focus). Wiring it through would have meant extending `IAlertProvider`'s
signature and touching `DefaultAlertProvider` plus both runtime UI implementations
(`WinFormAlertProvider`, `BeepWpfAlertProvider`/`WpfAlertDialog` — the latter already hardcodes
Button1 as the WPF default, so the property has *some* real behavior already for the common case).
Given the scope of this pass, `DefaultButton` was **not added** to `AlertDefinition` — storing a
property that nothing downstream reads is exactly the "accepted-then-ignored" defect this codebase's
own rules warn against, and adding it honestly (wired all the way through 4 files across 2 runtime UI
projects) was judged out of proportion to a single-button-focus nicety. Flagged here rather than
silently dropped.

**Where:** `AlertModels.cs` (new `AlertDefinition`), `IAlertRegistry.cs` (new file),
`FormsManager.Alerts.cs`, `IUnitofWorksManager.cs`.

**Risk of fix:** Low — purely additive; nothing previously called a named-alert API because none
existed. Six new regression tests in `FormsManagerTests.cs`; the "renders through the provider with
the definition's own fields" test proven via revert (reverting `ShowAlertByNameAsync` to a hardcoded
`AlertResult.None` failed exactly that test, and only that test, as expected).

### G0.32: Editor object (large-text popup, EDIT_TEXTITEM) did not exist at all — no model, no
provider abstraction, no runtime UI in either host (FIXED 2026-08-25)

**What:** Oracle Forms' Editor object is a named, reusable large-text popup a text item's
`EDITOR_NAME` property attaches to, invoked via the `EDIT_TEXTITEM` built-in. Nothing in this
engine or either runtime UI project (`TheTechIdea.Beep.Forms.WinForms`, `.Wpf`) implemented any part
of it — the only pre-existing "Editor" hit anywhere in the model layer was
`BlockDefinition`'s `EditorKey` (on `BlockEntityDefinition`/`BlockFieldDefinition`), an unrelated
control-selection string the platform field-presenter registry uses to pick which WinForms/WPF
control to instantiate for a field. Genuinely a from-scratch capability, not a name mismatch or a
missing wire-up of something already there.

**Fix, following the same shape as the Alert Provider/Registry split (`IAlertProvider` +
`IAlertRegistry`, G0.31) and the LOV attachment pattern (`ItemInfo.LOVName` +
`SetItemLOV`/`GetItemLOV`):**
- `EditorDefinition`/`EditorResult` (new file `EditorModels.cs`) — a named popup definition
  (Title, Width, Height, WrapText, ShowScrollBar) and the outcome of showing one
  (Committed + edited Value). `EditorDefinition.SystemDefault()` is what `EDIT_TEXTITEM` uses for
  an item with no Editor object attached, matching Oracle's own EDIT_TEXTITEM (works with or
  without a named Editor).
- `IEditorProvider` (new, in `IProviders.cs` next to `IAlertProvider`) — the pluggable UI
  abstraction; `DefaultEditorProvider` is the engine-side fallback and, unlike
  `DefaultAlertProvider`'s auto-accept, always returns Cancel rather than fabricate an edit with no
  real UI behind it (there is no meaningful "OK" for a text edit nothing can actually show).
- `IEditorRegistry` (new file `IEditorRegistry.cs`) — `CreateEditor`/`GetEditor`/`GetAllEditors`/
  `RemoveEditor`/`ClearAllEditors`/`EditorExists`, implemented in new file `FormsManager.Editor.cs`
  the same shape as `IAlertRegistry`.
- `ItemInfo.EditorName` (mirrors `LOVName`) plus `SetItemEditor`/`GetItemEditor` on
  `IItemPropertyManager`/`ItemPropertyManager` — also added to `ItemInfo.Clone()`, which silently
  drops any property not explicitly copied there.
- `FormsManager.ShowEditorAsync(blockName, itemName, ct)` — resolves the item's attached editor (or
  the system default), reads the current value, shows the popup, and on commit writes the edited
  value onto `blockInfo.UnitOfWork.CurrentItem` via `SetFieldValue` — the exact same
  reflection-based write `ShowLOVAsync` already uses to apply a selected LOV record's related field
  values onto the current record, so the edit flows into the same commit path a normal item edit
  uses. Does not itself fire `WHEN-VALIDATE-ITEM`: neither does Oracle's EDIT_TEXTITEM — validation
  fires on navigation away from the item, same as any other edit, once the new value is in place.
- Both runtime hosts: `WinFormEditorProvider` (`TheTechIdea.Beep.Forms.WinForms`) and
  `BeepWpfEditorProvider` + `WpfEditorDialog` (`TheTechIdea.Beep.Forms.Wpf`) — real popup UI, same
  construction/wiring pattern as the existing `WinFormAlertProvider`/`BeepWpfAlertProvider`
  (constructor-injected owner-window provider, not auto-wired — the host application opts in the
  same way it already does for alerts). `IBeepFormsHost` gained the full Editor Registry surface
  plus `ShowEditorAsync`, implemented in both `WinFormFormHost` and `BeepWpfForms` — and, since this
  pass touched that exact interface, its **Alert Registry surface from G0.31 was also backfilled**
  there (it had been added to `IUnitofWorksManager` but not to `IBeepFormsHost`, leaving that host
  contract incomplete relative to its own established pattern of a pass-through for every named
  registry).

**Deliberately not implemented:** a `WinFormEditorProvider`/`BeepWpfEditorProvider` "feature panel"
(the runtime debug/inspector panels that exist for Record Groups and Parameter Lists,
`WinFormRecordGroupPanel`/`WinFormParameterListPanel`) — the popup itself is the whole user-facing
surface for this object; a separate inspector panel wasn't judged to add anything a form author
needs. Also not implemented: `ItemInfo.EditorName` is not yet surfaced through any IDE authoring UI
(no Beep.Forms.IDE dialog sets it) — that's the same "engine capability exists, IDE authoring
doesn't yet" shape as Record Groups/Parameter Lists/Alerts themselves, still open.

**Where:** `EditorModels.cs`, `IProviders.cs`, `IEditorRegistry.cs` (all new),
`FormsManager.Editor.cs` (new), `FormsManager.Core.cs`/`.Properties.cs`, `IUnitofWorksManager.cs`,
`ItemInfo.cs`, `ItemPropertyManager.cs`, `IValidationAndLov.cs`, `IBeepFormsHost.cs`;
`WinFormEditorProvider.cs`, `WinFormFormHost.RuntimeObjects.cs` (WinForms);
`WpfEditorDialog.xaml`/`.xaml.cs`, `BeepWpfEditorProvider.cs`, `BeepWpfForms.RuntimeObjects.cs`
(WPF).

**Risk of fix:** Low — purely additive across all three projects; nothing previously called any of
this because none of it existed. Caught one real bug during testing:
`EditorDefinition`'s constructor rejected a null `name`, which broke `SystemDefault()` (the
system-default definition is intentionally unnamed) — fixed by moving the "name required" guard to
the registry (`CreateEditor`), where it belongs, since the model itself has a legitimate unnamed
state the registry does not. Ten new BeepDM regression tests in `FormsManagerTests.cs`; the
commit-to-record write proven via revert (reverting it left the "committed" test failing while
cancel/no-record/attached-editor tests kept passing, as expected).

### G0.33: `BlockPropertyManager` (SET_BLOCK_PROPERTY/GET_BLOCK_PROPERTY) fully implemented and wired
into `FormsManager`, but absent from `IUnitofWorksManager` — unreachable through the documented
contract every consumer actually types against (FIXED 2026-08-25)

**What:** `IBlockPropertyManager`/`BlockPropertyManager` (`SetBlockProperty`/`GetBlockProperty`/
`GetBlockProperty<T>`) were fully implemented, and `FormsManager` had a `BlockProperties` property
(`FormsManager.Properties.cs`) exposing it, since before this fix — but `IUnitofWorksManager` never
declared that property. Every consumer that follows this project's own house rule ("reach the engine
only through `ExtensionServices`/the documented interface, never a second path") — the Beep.Forms.IDE
extension, and both `IBeepFormsHost` implementations — typed against `IUnitofWorksManager`, so
`BlockProperties` was reachable only through an unsupported downcast to the concrete `FormsManager`
class. `SET_BLOCK_PROPERTY`/`GET_BLOCK_PROPERTY` (Oracle Forms built-ins) were accepted-then-ignored
in exactly the shape this codebase's own rules exist to hunt: a capability that works when called
directly on the concrete type and silently isn't there for anyone going through the interface.

**Fix:** Added `IBlockPropertyManager BlockProperties { get; }` to `IUnitofWorksManager`, matching
exactly how `Triggers`/`LOV`/`Validation`/`ItemProperties` are already exposed there (a sub-manager
property, not flat methods — `FormsManager`'s own flat `SetBlockProperty`/`GetBlockProperty`
convenience methods are FormsManager-specific sugar, not part of the documented contract, the same
way `RegisterItemTrigger` isn't flat-exposed on the interface either). Added
`SetBlockProperty(blockName, BlockProperty, value)`/`GetBlockProperty(blockName, BlockProperty)` to
`IBeepFormsHost`, typed with the engine's own `BlockProperty` enum rather than the string-based
convention `SetItemProperty`/`GetItemProperty` use on the same interface (block properties are a
small, well-defined enum — translating to strings would have been an unrequested indirection).
Implemented in both `WinFormFormHost` and `BeepWpfForms`, following the exact
`RequireManager()`/`TryReadManager()`/`NormalizeBlockName()`/`RefreshBlockAndDetails()` pattern their
own `SetItemProperty`/`GetItemProperty` already use in the same file family.

**Where:** `IUnitofWorksManager.cs`, `IBeepFormsHost.cs`; `WinFormFormHost.BlockProperties.cs` (new),
`BeepWpfForms.BlockProperties.cs` (new).

**Risk of fix:** Low — purely additive; `FormsManager` already implemented `BlockProperties`, so no
engine behavior changed, only reachability. One new regression test in `FormsHostContractTests.cs`
extending the existing `ManagerContract_ExposesEngineOwnedRuntimeProviders` (which already asserted
`Timers`/`Sequences` the same way) plus a new `FormsHost_ExposesBlockPropertyBuiltins`; proven via
revert (removing the interface property failed the extended test, as expected).

### G0.34: Designer-authored block-level QUERY_ALLOWED/INSERT_ALLOWED/UPDATE_ALLOWED/DELETE_ALLOWED
and DEFAULT_WHERE never reached the live block (FIXED 2026-08-25)

**What:** `BlockPropertyManager` (G0.33, same day) and the gating checks it backs are real and
wired — `FormsManager.BasicDataOps.cs` refuses a delete when `blockInfo.DeleteAllowed` is false,
refuses a query when `block.QueryAllowed` is false and merges `block.DefaultWhereClause` into every
query; `FormsManager.EnhancedOperations.cs` gates insert/update the same way. But
`DefinitionBlockRegistrar` — the one place a design-time `BlockDefinition` becomes a live block —
never called any of it: grepping `DefinitionBlockRegistrar.cs` for `QueryAllowed`/`InsertAllowed`/
`UpdateAllowed`/`DeleteAllowed`/`DefaultWhereClause` returned nothing. There was also no way to author
any of this in the first place — `BlockDefinition` had no `QueryAllowed`/`InsertAllowed`/
`UpdateAllowed`/`DeleteAllowed` properties at all, and its existing `QueryString` property (doc
comment: "Optional WHERE clause applied when the block queries") had **zero consumers anywhere** —
grepping the whole engine for `.QueryString` outside `BlockDefinition` itself found only two
unrelated local variables of the same name in the legacy `Report` module. So this was a gap at both
ends: nothing to author, and nothing that would have read it if there had been.

**Fix:** Added `bool? QueryAllowed`/`InsertAllowed`/`UpdateAllowed`/`DeleteAllowed` to
`BlockDefinition` (null = not authored, same nullable-tri-state convention as
`BlockFieldDefinition`'s per-field cluster) and reused the existing `QueryString` property for
DEFAULT_WHERE rather than adding a second one. `DefinitionBlockRegistrar.TryRegister` now calls a new
`ApplyAuthoredBlockProperties(manager, block)` right after `RegisterBlock`, which maps each authored
value onto the live block through `manager.BlockProperties.SetBlockProperty(...)` — the first real
caller of the `IUnitofWorksManager.BlockProperties` surface G0.33 exposed the same day. IDE-side
authoring (emitter, read-back, `BlockMetadataEditorDialog` UI) lands in the same Beep.Forms commit.

**Where:** `BlockDefinition.cs` (new properties, `QueryString` doc comment corrected),
`DefinitionBlockRegistrar.cs` (`ApplyAuthoredBlockProperties`, new).

**Risk of fix:** Low — purely additive, and every new call is conditional on the property actually
being authored (`HasValue`/non-blank), so a block that sets none of this behaves exactly as before.
**Known gap, not hidden:** `ApplyAuthoredBlockProperties` has no direct unit test — `TryRegister`
needs a live `IDataSource`/`IDMEEditor` chain to reach it (entity structure resolution, row-type
generation, `UnitOfWorkFactory.CreateUnitOfWork`), and its sibling `ApplyAuthoredFieldProperties`
(G0.23) has the same gap, unaddressed since it was added. Verified instead through
`DesignerCompileCheck`'s emitter/read-back/Roslyn-compile round-trip (Beep.Forms) plus code review;
building real `DefinitionBlockRegistrar`-level test infrastructure is a separate, larger piece of
work this fix does not attempt.

### G0.35: `BlockFieldDefinition.IsEnabled`/`IsVisible` never reached `ItemInfo.Enabled`/`.Visible`
(FIXED 2026-08-25)

**What:** `ItemInfo.Enabled` genuinely gates editability — `IsEditable(mode)` checks it before the
per-mode Query/Insert/Update flags, and `ItemPropertyManager` reads `item.Enabled`/`item.Visible`
together when building visible/editable item lists. Both runtime hosts (`WinFormBlockHost.cs`,
`BeepWpfBlock.cs`) read `item.Visible` at refresh time to show/hide a field's control. But
`PropertyClassManager.ApplyToItem` — the one place a designer-authored `BlockFieldDefinition` is
overlaid onto the live `ItemInfo` — never touched either property, and `RegisterItemsFromEntityStructure`
(which builds `ItemInfo` from the datasource's own column metadata) has no `BlockDefinition` input at
all to have set them from in the first place. So a field authored `IsEnabled = false` or
`IsVisible = false` compiled, round-tripped through its own editor correctly, and did nothing at
runtime — the exact accepted-then-ignored shape as G0.34's `QueryString`, found while auditing the
same `ApplyToItem` method for the same class of gap right after fixing it.

**Fix:** `PropertyClassManager.ApplyToItem` now sets `item.Enabled = fieldDefinition.IsEnabled;` and
`item.Visible = fieldDefinition.IsVisible;` directly — unconditionally, not through the
field-then-class-then-existing fallback chain the nullable cluster uses, because neither property is
part of the Property Class model (`PropertyClass` has no `Enabled`/`Visible` member) and both default
to `true` on `BlockFieldDefinition`, matching `ItemInfo`'s own defaults, so an unauthored field is a
no-op. `IsVisible` already had full IDE authoring (emit/read-back/dialog UI); `IsEnabled` had none —
added alongside it in the same Beep.Forms commit, mirroring `IsReadOnly`'s existing emit/read shape
exactly (`if (!f.IsEnabled) Line(...)` / `FB("IsEnabled", fallback: true)`).

**Where:** `PropertyClassManager.cs` (`ApplyToItem`).

**Risk of fix:** Low — a field that authors neither is unaffected (both flags already default to
`true`, matching every existing item's current behaviour). One new direct unit test,
`PropertyClassApplyToItem_EnabledAndVisible_OverlayDirectlyFromField` in `FormsManagerTests.cs`
(unlike G0.34, `ApplyToItem` is a plain, easily-mockable method with existing test precedent right
above it — no `IDataSource`/`IDMEEditor` chain needed); proven via revert (removing the two
assignments failed exactly that test).

### G0.36: `SystemVariablesManager` is reachable, but most of its `Update*`/`Set*` methods have no
caller — most `:SYSTEM.*`-equivalent fields are permanently stuck at their constructor default
(FOUND 2026-08-25; **trigger-context, block-switch, current-form, mode, last-error, last-query, the
block/record-status "CHANGED"/"QUERY"/"NEW" transitions, the post-commit/post-rollback reset back to
"QUERY", SetFormStatus's first direct call site, and ordinary-navigation record-position tracking all
FIXED across 2026-08-25/26** — `SetTriggerContext`/`ClearTriggerContext`, `UpdateForBlockChange`,
`SetCurrentForm`, `SetMode`, `SetLastError`, `SetLastQuery`, `SetFormStatus`, `SetBlockStatus`/
`SetRecordStatus` (for `"CHANGED"`, `"QUERY"`, and `"NEW"`), and `UpdateForRecordChange` (for ordinary
First/Next/Previous/Last/GoRecord navigation, wired 2026-08-26) now wired; `UpdateForItemChange`
turned out to be already wired, pre-dating this finding — the first pass's grep missed calls through
the private field; the sole remaining unwired piece in the whole `SystemVariablesManager` surface is
the `"INSERT"` value of `BLOCK_STATUS`/`RECORD_STATUS`, see "Still open" below)

**What:** Looking into this session's "`:SYSTEM.*` variables are ~4/90 implemented" framing (repeated
across three rounds of scoping) to see whether it was a well-scoped next target turned up that the
framing itself was wrong: it was based on `SystemVariableType`/`FormsSimulationHelper.SetSystemVariables`,
a real but unrelated, intentionally-narrow feature (stamps `SYSTEM_DATE`/`SYSTEM_DATETIME`/
`SYSTEM_USER`/`RECORD_STATUS` onto a **data record**, e.g. for audit columns — nothing to do with
`:SYSTEM.*` trigger-context variables). The actual `:SYSTEM.*` emulation is `SystemVariablesManager`/
`SystemVariables` (`Helpers/SystemVariablesManager.cs`, `Models/SystemVariables.cs`), which nobody had
looked at: 23 real fields (`CURRENT_BLOCK`, `CURSOR_RECORD`, `CURSOR_ITEM`, `CURSOR_VALUE`, `MODE`,
`BLOCK_STATUS`, `FORM_STATUS`, `RECORD_STATUS`, `MASTER_BLOCK`, five `TRIGGER_*` fields, `LAST_QUERY`,
`LAST_ERROR`(`_CODE`), `LAST_OPERATION_TIME`, …), exposed on `IUnitofWorksManager.SystemVariables` and
on `TriggerContext.SystemVariables` for handlers to read.

The `functionality/system-variables.md` doc describing this manager was itself almost entirely
fictional — a different, PascalCase-property API (`manager.SystemVariables.CursorBlock`, `.Mode`,
`.Timer`, `SetMaskSensitiveColumns(...)`) that matches no code anywhere in this repo, plus a trigger
example using a lambda `Callback = (ctx) => ...` / `TriggerResult.Ok()` shape that doesn't match the
real `Func<TriggerContext, TriggerResult>` handler signature `DesignerHandlerScaffolder` actually
emits. Corrected to describe the real implementation, verified field-by-field and method-by-method
against source.

**The gap that actually matters, found while correcting the doc — corrected once more, below, after an
initial grep mistake:** the first pass at this claimed *every* `SystemVariablesManager` update method
had zero callers, based on grepping `SystemVariables.<MethodName>(` — which only matches calls through
the public property syntax. `FormsManager`'s own code calls the manager through its **private field**,
`_systemVariablesManager.<MethodName>(...)`, which that grep never matched. Re-grepped correctly
(`_systemVariablesManager\.` and a bare `.<MethodName>(` sweep) and found two real, pre-existing
exceptions: `GoItemAsync` (`FormsManager.Navigation.cs:406`) already calls `UpdateForItemChange` on
every item-focus change, and `TryUpdateSavepointSystemVariables` (`FormsManager.BlockRegistration.cs:648`)
already calls `UpdateForRecordChange` — but only after a savepoint rollback, not on ordinary record
navigation (`NextRecordAsync` etc. do not call it). Both predate this session; they were simply missed,
not fixed here. Corrected the count: **six** methods have genuinely zero callers —
`UpdateForBlockChange`, `SetMode`, `SetBlockStatus`, `SetFormStatus`, `SetRecordStatus`,
`SetLastError`/`ClearLastError`, `SetLastQuery`, `SetCurrentForm` (eight names, still zero calls
between them) — plus `UpdateForRecordChange`'s coverage is real but narrow (savepoint-restore only).
`SetTriggerContext`/`ClearTriggerContext` are wired as of this same pass (see above). `FormsManager`'s
block-switch, mode-transition, DML, and query-execution code still never calls into this manager for
those eight names. The class is real, well-designed, reachable, and mostly-but-not-fully inert: a
trigger handler can write `context.SystemVariables.GetFormSystemVariables().CURRENT_BLOCK` today, it
will compile and
return a value, and that value will be `string.Empty` forever regardless of what the form actually
does, because nothing keeps it current.

**Fixed, same day: the trigger-context slice.** Unlike the other nine `Update*`/`Set*` methods, `SetTriggerContext`/
`ClearTriggerContext` had exactly ONE natural choke point rather than ~30 scattered call sites:
`TriggerManager.ExecuteTriggerChain`/`ExecuteTriggerChainAsync` are the two internal methods every one
of the ten public `Fire*Trigger(Async)` variants (Form/Block/Item/Global × sync/async) funnels
through, so hooking there — not each Fire* method, and not each of the ~30 `FormsManager.*.cs` call
sites that build a `TriggerContext` and hand it to one of those Fire* methods — covers every trigger
firing in one place. Added `ITriggerManager.SystemVariables { get; set; }` (settable, not a
constructor parameter, since `FormsManager` already constructs its `SystemVariablesManager` before its
`TriggerManager` and just assigns it across), wired in `FormsManager.Core.cs` right after
`_triggerManager` is built, and a private `ApplyTriggerContextToSystemVariables` helper that both
populates `context.SystemVariables` (previously always null — a handler reading it would have NRE'd)
and calls `SetTriggerContext(context.TriggerType.ToString(), context.BlockName, context.ItemName,
context.RecordIndex)` before the chain runs; `ClearTriggerContext()` runs once after. Both null-safe
(`SystemVariables` unset is a documented, tested no-op, not a throw) — a hand-constructed
`TriggerManager` in a test or elsewhere that never gets it wired keeps working exactly as before.

Three new direct tests against the real `TriggerManager` class (not mocked, the first in this file to
test `TriggerManager`'s own behavior rather than `FormsManager`'s dispatch through a mocked one):
firing a block trigger sets-then-clears the context and hands the real store through to the handler;
firing an item trigger (async) does the same; firing with `SystemVariables` unset doesn't throw.
Proven via revert — commenting out all four call sites failed the first two tests with exactly the
predicted symptoms (`context.SystemVariables` null inside the handler; the mock's `VerifyAll()`
reporting the setups were never matched). Fixing this also required updating two **pre-existing**
Strict-mock tests (`GoItem_ValidItemUpdatesCursorAndFiresNewItemTrigger`,
`GoItem_UnknownItemReturnsFalseWithoutTrigger`) to stub the new `ITriggerManager.SystemVariables`
setter — a `MockBehavior.Strict` mock throws on any unconfigured member access, and
`FormsManager`'s constructor now genuinely calls that setter on whatever `ITriggerManager` it's given.

**Already wired, pre-existing, missed by the first pass's flawed grep:** `UpdateForItemChange` (called
from `GoItemAsync` on every item-focus change) and `UpdateForRecordChange` (called from
`TryUpdateSavepointSystemVariables`, but only after a savepoint rollback — ordinary record navigation
such as `NextRecordAsync` does not call it, so this one is real but narrow).

**Fixed, same day: `UpdateForBlockChange`.** Same shape as `UpdateForItemChange` turned out to be —
one natural choke point, not a scattered set of call sites. `SwitchToBlockAsync` is the single method
every block switch goes through (`GoBlockAsync` is a pure delegation to it: `=> SwitchToBlockAsync(blockName)`),
so `_systemVariablesManager?.UpdateForBlockChange(blockName)` now runs there, right after
`_currentBlockName` is committed to the new block and before the block-enter event/side effects. Two
new tests (`SwitchToBlockAsync_UpdatesSystemVariablesCurrentBlock`,
`GoBlockAsync_DelegatesToSwitchToBlockAsync_UpdatesSystemVariables` — the second pins the delegation
itself, not just the method it forwards to), proven via revert.

**Fixed, same day: `SetCurrentForm`.** `CurrentFormName` (the public property, `FormsManager.Properties.cs`)
has exactly three writers — its own setter, and two direct `_currentFormName` field assignments in
`OpenFormAsync`/`CloseFormAsync` (`FormsManager.FormOperations.cs`) that bypass the property (a
class's own code never invokes its own property setter implicitly). All three now also call
`_systemVariablesManager?.SetCurrentForm(...)` — the property setter for external callers, and both
`FormOperations.cs` sites for the engine's own form open/close lifecycle (close passes `null`,
mirroring the existing `_currentFormName = null` reset). One new test
(`CurrentFormName_Set_UpdatesSystemVariablesCurrentForm`) covers the property-setter path directly;
proven via revert. The other two sites use the identical one-line call and were not given their own
tests — proportionate to how small and visually-verifiable a one-line addition next to an existing
assignment is, versus building out `OpenFormAsync`/`CloseFormAsync` test scaffolding that doesn't
exist yet in this file for any other purpose.

**Fixed, same day: `SetMode`.** Re-checked rather than left as "no single choke point" — confirmed
`blockInfo.Mode` is assigned directly at **four** sites, not three: the three already found in
`FormsManager.ModeTransitions.cs` (`EnterQueryModeAsync`, `EnterCrudModeForNewRecordAsync`, and the
child-block coordination inside `CreateNewRecordInMasterBlockAsync`), plus a fourth in
`FormsManager.EnhancedOperations.cs`'s `ExecuteQueryEnhancedAsync` (the real implementation behind
`ExecuteQueryAsync`/`ExecuteQueryAndEnterCrudModeAsync`, both of which delegate to it) that the
original count missed. Four sites with no shared setter is not the same as "unwireable" — it is the
same shape as `SetCurrentForm`'s three writers, just one site larger. Added a private
`ToSystemVariableMode(DataBlockMode)` helper mapping the engine's five-value `DataBlockMode` enum onto
Oracle's real two-value `:SYSTEM.MODE` vocabulary (`NORMAL`/`ENTER-QUERY` — Oracle Forms does not
publish a third value for this variable, unlike `:SYSTEM.BLOCK_STATUS`, which does have `QUERY`), and
called `_systemVariablesManager?.SetMode(ToSystemVariableMode(...))` at all four sites, right next to
each direct `.Mode =` assignment. One new test (`EnterQueryModeAsync_SetsSystemVariablesModeToEnterQuery`,
covering the simplest of the four); the other three share the identical one-line pattern and were
verified by the full 122-test run rather than each getting a dedicated test, the same proportionality
call made for `SetCurrentForm`'s `OpenFormAsync`/`CloseFormAsync` sites. Proven via revert.

**Fixed, same day: `SetLastError`.** Unlike `SetMode`, this one turned out to have a genuine single
choke point despite 114+ separate `catch` blocks scattered across `FormsManager.*.cs`: every one of
them already reports through the shared `protected void LogError(string message, Exception ex = null,
string blockName = null)` helper (`FormsManager.Helpers.cs`), which mirrors the failure into the
per-block `IBlockErrorLog` when a block context is given. Hooking `LogError` itself — not each catch
site — covers every failure this manager ever logs, the same shape Oracle Forms' own
`:SYSTEM.LAST_ERROR` has: it reflects whatever runtime error the form most recently hit, from any
operation, not just DML. Added `_systemVariablesManager?.SetLastError(message, ex?.HResult ?? 0)`
right after `LogError`'s existing `_errorLog?.LogError(...)` mirror. There is no Oracle-style
`ORA-`/`FRM-` error number available from a .NET exception; `ex.HResult` is the closest native analog
to a numeric code (0 when there is no exception object). `ClearLastError()` was deliberately left
unwired — real Oracle Forms has no "clear" semantic for `:SYSTEM.LAST_ERROR` either; it just persists
until overwritten by the next error, which `SetLastError` already provides. One new test
(`LogError_SetsSystemVariablesLastError`, forcing `IUnitofWork.Get()` to throw inside
`ExecuteQueryEnhancedAsync` and asserting the mock's `SetLastError` was invoked with a message
containing the block name), proven via revert.

**Still open — one value.** `SetFormStatus` as a direct call is **no longer** on this list — see
the `CommitFormAsync`/`RollbackFormAsync` entry below — and neither is `UpdateForRecordChange`'s
general (non-savepoint) case — see below.

**`BLOCK_STATUS`/`RECORD_STATUS`'s `"INSERT"` value is also still open, but for a different reason
than "scattered call sites."** Unlike `"NEW"`/`"QUERY"` (see below — both landed same day once
re-checked), `"INSERT"` needs the engine to know, at edit time, whether the record being edited was
ever fetched by a query or was created blank — a genuinely per-record distinction the current
`SystemVariables` snapshot (keyed per-block, one "current" value, not per-row) does not carry.
Real Oracle Forms uses `"INSERT"` for an edited `"NEW"` record and reserves `"CHANGED"` for an
edited `"QUERY"` record; this session's `ItemChanged` handler wiring (see below) does not make that
distinction — it always sets `"CHANGED"`, regardless of whether the record was new or queried. This
is a real, deliberate scope decision, not an oversight: making it correct needs either a per-record
status field wired into whatever tracks the "current record" (a bigger design question) or accepting
a per-block approximation and documenting its inaccuracy — left for its own pass.

**`SetLastQuery`: the "no existing serializer" blocker turned out to be a search gap, not a real
blocker — found and fixed same day.** `SetLastQuery` was checked directly: `ExecuteQueryEnhancedAsync`
has a single natural landing spot right where it calls `blockInfo.UnitOfWork.Get(filters)`, the same
shape that made `SetMode` and `SetLastError` tractable, but the original check concluded it was
blocked on something those two weren't — there being no existing string form of "the query that ran"
to hand it, since `ExecuteQueryEnhancedAsync` receives a `List<AppFilter>`, not a WHERE-clause string.
A follow-up re-check found that conclusion itself was based on an incomplete grep:
`DataManagementModelsStandard/Extensions/DataSourceAppFilterExtensions.cs`'s
`BuildSelectQueryDefinition(this IDataSource, entityNameOrSelect, filters, selectedColumns)` already
builds a full `"SELECT ... FROM ... WHERE ..."` string (plus a parameter dictionary) from exactly an
`AppFilter` list — a real, existing, general-purpose capability, itself with zero callers anywhere in
the engine before this pass (not Forms-specific, so not itself an instance of this gap's shape, but a
sibling one worth noting: built, reachable, unused). `ExecuteQueryEnhancedAsync` now resolves the
block's `IDataSource` via `_dmeEditor.GetDataSource(blockInfo.DataSourceName)` (the same pattern
`FormsManager.Validation.cs` already uses for LOV validation) and calls
`SetLastQuery(queryDefinition.QueryText)` right after the query succeeds — best-effort: an
unresolvable data source name leaves `LAST_QUERY` at its prior value rather than failing a query that
already succeeded. Two new tests
(`ExecuteQueryEnhancedAsync_OnSuccess_SetsSystemVariablesLastQuery`,
`ExecuteQueryEnhancedAsync_UnresolvableDataSource_DoesNotSetLastQuery`), proven via revert; full
`FormsManager.Tests` suite (168/168) green across a full engine rebuild.

**`SetBlockStatus`/`SetRecordStatus`'s `"CHANGED"` transition: found, reverted on an unexplained test
interaction, re-attempted and landed same day.** The block-registration `ItemChanged` handler
(`FormsManager.BlockRegistration.cs`) is the one place every genuine user-driven field edit on every
block passes through, and it is safe against false positives from query population: confirmed by
reading `UnitofWork.CRUD.cs`'s `Get()`/`Get(filters)` and `UnitofWork.Core.Utilities.cs`'s
`GetDataInUnits` directly — the latter builds each row fully in a plain `List<T>` and only wraps the
*already-populated* list in a new `ObservableBindingList<T>` afterward, so `ItemChanged` genuinely
never fires from a query, only from a real edit.

The *first* attempt at wiring `_systemVariablesManager?.SetBlockStatus(blockName, "CHANGED")` /
`SetRecordStatus(blockName, "CHANGED")` into that handler (`SetBlockStatus`'s own `"CHANGED"` value
already cascades `FORM_STATUS` too, so this closes three fields' `"CHANGED"` transition in one hook)
compiled clean and passed its own new test in isolation, but made
`ItemChanged_FieldHasLOV_FiresWhenLOVValidationTrigger` (a pre-existing test, unrelated block/LOV
setup, no shared object with the new test) fail consistently (3/3) whenever both tests ran in the same
suite — while a trivial no-op `[Fact]` added in the new test's place did not reproduce it (3/3 clean),
ruling out "the suite just got bigger" as the cause. The failure was `ctx.NewValue` arriving `null`
while `ctx.ItemName` arrived correct in the same synchronous trigger-callback statement pair — not
something a `TriggerManager`/`LOVManager` review turned up a static/shared-state explanation for (both
checked directly: no `static` collections in either). Per house rule 8 (verify before claiming, prove
the guard can fail on the thing it guards) that attempt was reverted cleanly (verified `git status`
clean, rebuilt, 123/123 green) rather than shipped with an unexplained red test.

**The re-attempt used the identical wiring and the identical choke point**, plus one new direct test
(`ItemChanged_NoLov_SetsBlockAndRecordStatusToChanged` in `FormsManagerTests.cs`), deliberately
exercising the no-LOV branch that the three `WHEN-LOV-VALIDATION` tests do not — a different code path
through the same handler, rather than a copy of the test that had shown the interaction. This did
**not** reproduce the earlier failure: 25 consecutive full-suite runs (166/166) were green with the
wiring in place — including runs that follow the same "both tests present" shape the first attempt's
3/3 reproduction required — versus one confirmed red run with the two `SetBlockStatus`/`SetRecordStatus`
lines commented out (`// TEMP-REVERTED-FOR-PROOF:`), where the new test failed exactly as predicted
("CHANGED" vs "NEW"). The original 3/3 reproduction was real but its exact mechanism was never
identified and could not be reproduced again under the same wiring plus a comparable new test; it is
recorded here rather than erased, in case a future session hits the same symptom and needs the history.
The earlier hypothesis — a pre-existing fragility in `WaitUntilAsync`'s polling synchronization around
an async-void event handler, made visible by one more concurrently-scheduled async `ItemChanged`
handler — was not tested directly (the polling helper itself was left unchanged), so it remains
unconfirmed rather than ruled out; it just did not manifest this time.

A related dead-end, ruled out rather than pursued: a second, entirely separate per-block snapshot
store — `UpdateBlockVariables`/`GetBlockVariables`/`_blockVars` in `SystemVariablesManager.cs`
("Per-Block Snapshot (Phase 8)" region) — exists with zero callers on *both* the write and read side,
and its own doc comment says it exists so a class called `BeepDataBlock` "can read system variables
without calling FormsManager directly." `BeepDataBlock` is the **legacy** pre-extraction WinForms
control (see `WinFormsScanner.cs`/`CodeGenConstants.cs` in Beep.Forms, both of which call it "legacy"
by name) that Beep.Forms' extraction deliberately left behind — its replacement, `WinFormBlockHost`,
does not use this snapshot mechanism. Building this out would mean maintaining a second, redundant
per-block dictionary alongside the one `GetSystemVariables(blockName)` already serves (house rule 3:
two stores for one domain), for a consumer that no longer exists in this repo. Left as-is, not wired
and not deleted (removal is a decision for Fahad, not made here) — documented so a future pass does
not mistake it for a live, missing-caller gap of the same shape as the four above.

**`BLOCK_STATUS`/`RECORD_STATUS`'s `"QUERY"` and `"NEW"` transitions: re-checked against the earlier
"genuinely larger, scoped-per-call-site work" characterization and found tractable at the existing
`SetMode`/`SetLastQuery` choke points — found and fixed same day.** After the `"CHANGED"` transition
landed, the remaining `"NEW"`/`"QUERY"`/`"INSERT"` values were re-examined individually rather than
left under the blanket "scattered call sites" label. Two of the three needed no new investigation:
`"QUERY"` — a record just fetched and not yet touched — shares `ExecuteQueryEnhancedAsync`'s existing
hook, the exact site `SetMode` and `SetLastQuery` already call from, set unconditionally on a
successful `Get`/`Get(filters)` regardless of row count (the same simplification `SetMode` already
makes there). `"NEW"` — a blank record just created — shares `EnterCrudModeForNewRecordAsync`'s
existing hook, right after `CreateNewRecord` succeeds; both the direct single-block path and
`CreateNewRecordInMasterBlockAsync`'s master-block delegation funnel through it, so one hook covers
both callers (`CoordinateChildBlocksForNewMasterRecord`'s own separate detail-block `Mode = CRUD`
assignment was deliberately **not** given the same treatment — it clears a detail block and sets its
mode without actually creating a record there, so there is no "current record" yet to legitimately
call `"NEW"`; wiring it would have meant guessing at semantics rather than following an established
pattern). `"INSERT"` remains open — see above for why it is a different, bigger kind of blocker than
the other two. Two new tests
(`ExecuteQueryEnhancedAsync_OnSuccess_SetsSystemVariablesQueryStatus`,
`EnterCrudModeForNewRecordAsync_OnSuccess_SetsSystemVariablesNewStatus`), each proven via revert;
full engine build plus `FormsManager.Tests` (170/170) green across 5 consecutive runs.

**`CommitFormAsync`/`RollbackFormAsync`: found while landing "QUERY"/"NEW" — nothing reset
`BLOCK_STATUS`/`RECORD_STATUS`/`FORM_STATUS` back off `"CHANGED"`, so a saved or discarded edit
stayed permanently `"CHANGED"` — closed same day, and gave `SetFormStatus` its first direct call
site.** Wiring `"CHANGED"` two entries back made this visible: once a block could reach `"CHANGED"`,
nothing in either commit or rollback ever moved it back off that value, so a form that had ever had
one edit would report `BLOCK_STATUS = "CHANGED"` forever afterward, saved or not — arguably a worse
symptom than the field being permanently unset, since it actively lies about the block's state.
`CommitFormAsync` captures each form's dirty-block list into a `dirtyBlocksByForm` map *before* the
commit actually runs (`GetDirtyBlocks()` called again after a successful commit would already be
empty, `IsDirty` having just been cleared), then on success calls `SetBlockStatus`/
`SetRecordStatus(_, "QUERY")` for every block that map recorded and, once all of a form's committed
blocks are reset, `SetFormStatus("QUERY")` for that form — safe because `SetBlockStatus`'s only
source of `"CHANGED"` is the `ItemChanged` handler wired above, and every block that could be
`"CHANGED"` was, by construction, one of the blocks just committed. Cross-form-safe: `formsToCommit`
can include peer `FormsManager` instances, and their own `_systemVariablesManager` is reached the
same way the surrounding method already reaches their `_auditManager`/`_lockManager`/`_blocks` —
private-field access across instances of the same class, an established pattern in this method, not
a new one. `RollbackFormAsync` mirrors this on a successful rollback, with one deliberate difference:
it scopes the reset to `blocksForDefaultRollback`, not the full dirty-blocks list, and only calls
`SetFormStatus("QUERY")` when every dirty block took that default path. A block with a registered
`ON-ROLLBACK` handler replaces the default rollback entirely and may have written its own outcome
to `TriggerContext.SystemVariables` during that trigger (the trigger-context wiring from earlier in
this same G0.36 pass makes that field non-null); force-resetting such a block to `"QUERY"` regardless
would silently discard whatever the form author's own handler decided. Four new tests
(`CommitFormAsync_OnSuccess_ResetsBlockRecordAndFormStatusToQuery`,
`CommitFormAsync_BlockCommitFails_DoesNotResetStatusToQuery`,
`RollbackFormAsync_OnSuccess_ResetsBlockRecordAndFormStatusToQuery`,
`RollbackFormAsync_OnRollbackRegistered_DoesNotOverrideThatBlocksStatus`), each proven via revert;
full engine build plus `FormsManager.Tests` (174/174) green across 5 consecutive runs.

**`UpdateForRecordChange`'s "only from savepoint rollback" limitation: re-checked and closed
(2026-08-26).** The last item this pass's own earlier account had left open on the strength of the
original grep, not individually re-checked. Every record-navigation entry point —
`FirstRecordAsync`/`NextRecordAsync`/`PreviousRecordAsync`/`LastRecordAsync` (via
`NavigateWithValidationAsync`) and `NavigateToRecordAsync`/`GoRecordAsync` — funnels through exactly
two private methods, `NavigateAsync` and `NavigateToRecordInternalAsync`, both of which already
compute the post-navigation `currentIndex` for their own record-history bookkeeping. Wiring
`_systemVariablesManager?.UpdateForRecordChange(blockName, currentIndex, blockInfo.UnitOfWork.TotalItemCount)`
into each success branch needed no new state — the same "two choke points, not one, but still a
small enumerable set" shape `SetMode`/`SetCurrentForm` already had. Two new tests
(`NextRecordAsync_OnSuccess_UpdatesSystemVariablesRecordPosition`,
`NavigateToRecordAsync_OnSuccess_UpdatesSystemVariablesRecordPosition`), each choke point proven via
revert independently.

Getting the second test to actually exercise a *successful* navigation (rather than the pre-existing
test's own caution of not asserting a return value at all) surfaced a real testing-infrastructure
trap, not a production defect: `PerformRecordNavigation` (unlike `PerformNavigation`, which
First/Next/Previous/Last use) dynamic-dispatches `SetCurrentIndex`/`GetTotalRecords` directly against
`Units`. A bare `ICollection` mock has no `CurrentIndex` to dispatch to, so a first attempt used a
real `ObservableBindingList<T>` closed over a `private` nested test class — which still failed,
`GetTotalRecords` silently returning `0` instead of the real count. The cause: the C# dynamic binder
(unlike plain reflection, which this engine's own doc comments call out as the reason `dynamic` was
chosen over `GetProperty` reflection in the first place) enforces accessibility, and `FormsManager`'s
dynamic-dispatch code runs in a different assembly than the test — so a `RuntimeBinderException` was
being thrown and silently caught by `GetTotalRecords`'s existing `Debug.WriteLine`-only diagnostic
(itself deliberate, documented "B2" behaviour, not something to change). Using a public entity type
(`TheTechIdea.Beep.Editor.Entity`) as `Units`'s generic argument instead of the private test fixture
fixed the test; nothing in `FormsManager` or `ObservableBindingList` needed to change.

**Lesson carried forward, and now applied eight times: before assuming "no single choke point" (or
"no existing capability to build on"), actually check.** `UpdateForItemChange`, `UpdateForBlockChange`,
`SetCurrentForm`, `SetMode`, `SetLastError`, `SetLastQuery`, and `SetBlockStatus`/`SetRecordStatus` all
turned out to have a small, fixed, enumerable set of writers — or, for `SetLastError`, an actual single
shared helper hiding behind 114 scattered `catch` blocks — rather than truly scattered call sites, and
were only missed the first time by a grep that didn't account for the private-field call shape (or, for
`SetMode`, by not checking a second file; or, for `SetLastQuery`, by not searching outside the Forms
subsystem's own folder for a general-purpose helper that happened to live in
`DataManagementModelsStandard/Extensions` instead). `SetBlockStatus`/`SetRecordStatus` add a second
lesson on top of the first: finding the right choke point is necessary but was not, on the first
attempt, sufficient — landing the wiring surfaced what looked like an unrelated test's fragility, and
the honest response per house rule 8 was to revert rather than ship a plausible-but-unproven
explanation. The follow-up re-attempt (same wiring, same choke point, a differently-shaped new test) is
what actually confirmed the wiring itself was safe; "could not reproduce the earlier failure after a
serious attempt" is real evidence, but the original failure's mechanism is still not identified, which
is why both attempts are recorded rather than only the one that landed. `SetFormStatus` and the
`NEW`/`QUERY`/`INSERT` values of `BLOCK_STATUS`/`RECORD_STATUS` remain open on the strength of the
original grep plus the doc's ordering constraints, not yet individually re-checked at this depth.

**Where:** `Editor/Forms/Helpers/SystemVariablesManager.cs`, `Editor/Forms/Models/SystemVariables.cs`,
`Editor/Forms/Interfaces/ICoreHelpers.cs` (`ISystemVariablesManager`),
`Editor/Forms/functionality/system-variables.md` (corrected); `Editor/Forms/Helpers/TriggerManager.cs`
(`SystemVariables` property, `ApplyTriggerContextToSystemVariables`, the two `ExecuteTriggerChain*`
hooks), `Interfaces/ITriggerSystem.cs` (`ITriggerManager.SystemVariables`), `FormsManager.Core.cs`
(trigger-context wiring); `FormsManager.Navigation.cs` (`SwitchToBlockAsync`'s
`UpdateForBlockChange` call); `FormsManager.Properties.cs` (`CurrentFormName` setter),
`FormsManager.FormOperations.cs` (`OpenFormAsync`/`CloseFormAsync`);
`FormsManager.ModeTransitions.cs` (`ToSystemVariableMode` helper, three of the four `SetMode` call
sites, plus `EnterCrudModeForNewRecordAsync`'s `SetBlockStatus`/`SetRecordStatus("NEW")` calls),
`FormsManager.EnhancedOperations.cs` (the fourth `SetMode` call site, the `SetLastQuery` call, and
the `SetBlockStatus`/`SetRecordStatus("QUERY")` calls, all in `ExecuteQueryEnhancedAsync`);
`FormsManager.Helpers.cs` (`LogError`'s `SetLastError` call); `FormsManager.BlockRegistration.cs`
(the `ItemChanged` handler's `SetBlockStatus`/`SetRecordStatus("CHANGED")` calls);
`FormsManager.FormOperations.cs` (`CommitFormAsync`'s `dirtyBlocksByForm` capture and post-commit
`SetBlockStatus`/`SetRecordStatus`/`SetFormStatus("QUERY")` reset; `RollbackFormAsync`'s matching
post-rollback reset, scoped to `blocksForDefaultRollback`); `FormsManager.Navigation.cs`
(`NavigateAsync`'s and `NavigateToRecordInternalAsync`'s `UpdateForRecordChange` calls);
`Editor/Forms.Tests/FormsManagerTests.cs` (nineteen new tests total, two updated Strict mocks).

### G0.37: `BlockFieldDefinition.Label` never reached `ItemInfo.PromptText` (FIXED 2026-08-25)

**What:** Found while sweeping the remaining `BlockFieldDefinition` properties for the same
accepted-then-ignored shape as G0.35 right after fixing it. `ItemInfo.Create()` defaults
`PromptText` to the raw field name (`ItemInfo.cs:318`), and both runtime hosts
(`WinFormBlockHost.cs`/`BeepWpfBlock.cs`, plus their `*.GridMode.cs` grid-column-caption paths)
already read `item.PromptText` as the visible field label — the entire rendering pipeline was
built and working. The IDE has always emitted an authored `Label` onto `BlockFieldDefinition`
(`DesignerBlockGenerator.cs`), but `PropertyClassManager.ApplyToItem` — the same overlay method
G0.35 just extended for `Enabled`/`Visible` — never touched it, so every authored caption
("Order ID") was silently discarded and every field showed its raw column name ("OrderId")
at runtime instead.

**Fix:** `ApplyToItem` now sets `item.PromptText = fieldDefinition.Label;` when a `Label` is
authored (non-blank), left untouched otherwise. `PropertyClass` has no `Label` member, so this
is a direct field-only override, the same shape as `Enabled`/`Visible`.

**Where:** `PropertyClassManager.cs` (`ApplyToItem`).

**Risk of fix:** Low — a field that authors no `Label` is unaffected (`PromptText` keeps its
existing/default value). Two new direct unit tests,
`PropertyClassApplyToItem_Label_OverlaysPromptTextDirectlyFromField` and
`PropertyClassApplyToItem_NoAuthoredLabel_KeepsExistingPromptText`, in `FormsManagerTests.cs`;
proven via revert (commenting out the assignment failed the first test with the predicted
`"OrderId"` vs `"Order ID"` mismatch, full 125-test suite green before and after).

### G0.38: `BlockFieldDefinition.FormatMask` reached `ItemInfo.FormatMask` but no host ever read it
(FIXED 2026-08-25; not given its own entry at the time — added retroactively when G0.39 below
touched this section again)

**What:** `PropertyClassManager.ApplyToItem` has overlaid `fieldDefinition.FormatMask` onto
`item.FormatMask` since G0.23, but neither runtime host's date/numeric presenters nor the grid
column builder ever read `item.FormatMask` — an authored `"MM/DD/YYYY"` or `"999,999.99"` had no
effect on what was displayed. The blocker was that Oracle's format-mask vocabulary is not .NET's:
passing an authored mask straight into `DateTime.ToString` throws (uppercase `D` is not a
recognised .NET custom date specifier) or renders wrong (Oracle's `/`/`:` are literals; .NET's
are culture-dependent separators unless escaped).

**Fix:** New `OracleFormatMaskTranslator` (`Helpers/OracleFormatMaskTranslator.cs`),
`TryTranslateDate`/`TryTranslateNumeric`, translates the commonly-authored token subset for both
and refuses (rather than guesses) on anything outside it (Oracle's `MI`/`PR`/`FM`/`V`/`SSSSS`/
`IYYY`/`RN` and similar). WinForms wiring (Beep.Forms, `WinFormDateFieldPresenter`/
`WinFormNumericFieldPresenter`.`ApplyFormatMask`, `WinFormBlockHost.GridMode.cs`) is the actual
consumer; this repo change is the translator alone.

**Where:** `Helpers/OracleFormatMaskTranslator.cs` (new).

**Risk of fix:** Low, purely additive — no existing caller of anything in this file. 25 new unit
tests (`OracleFormatMaskTranslatorTests.cs`) cover known-good masks (including a real
`DateTime`/`decimal` round-trip through the produced format string) and confirm every unsupported
case is refused.

### G0.39: `BlockFieldDefinition.Width` never reached `ItemInfo.Width` — `ItemInfo` had no such
property at all (FIXED 2026-08-25)

**What:** Found sweeping the same `BlockFieldDefinition` properties G0.37/G0.38 came from. The IDE
has authored, emitted, and read back `Width` (display width in pixels, 0 = auto) since the property
was added; `ItemInfo` never had a `Width` member to carry it to either runtime host, so no control
was ever sized from it — every field showed whatever its container's own default layout produced,
authored width or not.

**Fix:** New `ItemInfo.Width` (`int`, 0 = auto), wired into `Clone()`; `PropertyClassManager
.ApplyToItem` overlays it directly from `fieldDefinition.Width` when authored (> 0) — like Label,
`PropertyClass` has no `Width` member, so there is no class-fallback step. Host-side consumption
(WinForms sizing a field's control or grid column) lands in the paired Beep.Forms commit.

**Where:** `Models/ItemInfo.cs` (`Width`, `Clone()`), `Helpers/PropertyClassManager.cs`
(`ApplyToItem`).

**Risk of fix:** Low — a field that authors no `Width` is unaffected (`ItemInfo.Width` defaults to
0, matching `BlockFieldDefinition.Width`'s own default). Two new direct unit tests
(`PropertyClassApplyToItem_Width_OverlaysItemWidthDirectlyFromField` /
`PropertyClassApplyToItem_NoAuthoredWidth_KeepsExistingWidth`); proven via revert (commenting out
the assignment failed the first test with the predicted `220` vs `0` mismatch), full 152-test
suite green before and after.

### G0.40: `BlockFieldDefinition.IsRequired` never reached `ItemInfo.Required` through
`PropertyClassManager.ApplyToItem` (FIXED 2026-08-25)

**What:** Found sweeping the same properties G0.37/G0.38/G0.39 came from. Both runtime hosts
already fully consume `item.Required` (`presenter.IsRequired = item?.Required ?? presenter
.IsRequired;`, `WinFormBlockHost.cs`/`BeepWpfBlock.cs`) — the pipeline downstream of `ItemInfo` was
complete. `RegisterItemsFromEntityStructure` sets `item.Required` from the live datasource's
nullability metadata when a block registers, but `ApplyToItem` never once touched it afterward, so
an author who explicitly checked *Required* on a field in the IDE — a business rule the database
schema itself does not enforce — saw it compile and round-trip and had it silently discarded at
runtime: the field stayed exactly as required (or not) as the raw column happened to be.

**Fix, deliberately one-directional, not the `Enabled`/`Visible` shape:** `BlockFieldDefinition
.IsRequired` is a plain `bool` with no "not authored" state distinct from `false` — unlike the
`QueryAllowed`/`InsertAllowed`/`UpdateAllowed` cluster, and unlike `IsEnabled`/`IsVisible`, whose
unconditional overlay is safe only because *both* sides default to `true`. Here the defaults do not
coincide: an unauthored field's `IsRequired` is `false` by the type's own default, while
`item.Required`'s meaningful default (from the live schema) can very much be `true` for a NOT NULL
column. An unconditional overlay would have forced every schema-required field optional the moment
its author left this one field untouched — a regression, not a fix, and the exact defect class this
file exists to catch, this time self-inflicted. `ApplyToItem` now only ever sets
`item.Required = true` when `fieldDefinition.IsRequired` is `true`; an unauthored field keeps
whatever the schema already determined. Known, accepted limitation: an author cannot use this field
to force a NOT NULL column optional at the UI level — that would need `IsRequired` to become
`bool?`, a breaking model change out of scope here.

**Where:** `Helpers/PropertyClassManager.cs` (`ApplyToItem`).

**Risk of fix:** Low in the direction that matters (adds required-ness, never removes it). Two new
direct unit tests — `PropertyClassApplyToItem_AuthoredIsRequiredTrue_SetsItemRequired` and
`PropertyClassApplyToItem_UnauthoredIsRequired_KeepsSchemaDerivedTrue` (the latter specifically
proving the no-regression case: an unauthored field with a schema-derived `item.Required = true`
stays `true`) — proven via revert (commenting out the assignment failed the authored-true test with
the predicted `True` vs `False` mismatch), full 154-test suite green before and after. No
Beep.Forms code change needed — both hosts were already consumers of `item.Required`.

### G0.41: `BlockFieldDefinition.EditorKey` never reached either runtime presenter registry
(FIXED 2026-08-25)

**What:** The "Generate/Sync Field Controls" IDE workflow (`BlockItemWorkflowCoordinator
.FieldControls.cs`) auto-derives `EditorKey` from `FieldTypeMapper.GetCanonicalFieldType`, but a
user can freely type a different value into the same field-editor text box, and
`preserveExplicit` deliberately protects that choice from being silently overwritten on the next
sync — a real, deliberate authoring override, not just an auto-synced mirror (confirmed by reading
the workflow directly, not inferred from the doc comment alone). Neither
`WinFormFieldPresenterRegistry.Create`/`.ResolveColumnType` nor `WpfFieldPresenterRegistry.Create`
ever consulted it — every field always rendered whatever its raw data type inferred to, regardless
of what an author explicitly picked.

**Fix:** New `FieldTypeMapper.TryNormalizeEditorKey(string?, out string?)` — the single, shared
place that recognises an authored `EditorKey` against the exact canonical categories
`GetCanonicalFieldType` itself returns ("Numeric"/"Date"/"Boolean"/"Checkbox"/"ReadOnly"/"Text"),
case-insensitively. The field-editor's `EditorKey` box is free text, not a constrained dropdown, so
an unrecognised value (a typo, or a platform-specific control class name the IDE's own scanner
separately understands, e.g. "BeepComboBox") is refused rather than guessed at.
`PropertyClassManager.ApplyToItem` overlays the normalised result onto `ItemInfo.EditorKey` (new
property) only when recognised; an unrecognised or unauthored value leaves it null, which both
runtime registries then treat identically — falling back to the field's own inferred type, exactly
the behaviour before `EditorKey` existed.

**Where:** `Helpers/FieldTypeMapper.cs` (`TryNormalizeEditorKey`, new), `Models/ItemInfo.cs`
(`EditorKey`, wired into `Clone()`), `Helpers/PropertyClassManager.cs` (`ApplyToItem`).

**Risk of fix:** Low — an unauthored or unrecognised `EditorKey` is a no-op (`item.EditorKey` stays
null, registries fall back to today's inference). 11 new tests
(`PropertyClassApplyToItem_RecognisedEditorKey_OverlaysCanonicalCategory` — a `[Theory]` over all
six canonical values, case-insensitively — and `PropertyClassApplyToItem_UnrecognisedEditorKey
_LeavesItemEditorKeyNull` covering null/blank/whitespace/a typo/a platform-specific control name);
proven via revert (disabling the overlay failed all six recognised-value cases with the predicted
mismatch, while the five unrecognised-value cases correctly kept passing — proving the revert
target was neither too broad nor too narrow), full 165-test suite green before and after. Host-side
consumption (both runtime presenter registries) lands in the paired Beep.Forms commit.

---

### G0.42: `ViewStateSyncer`/`BeepViewState`/`IFormsNotificationService` — checked and found to be
a superseded duplicate, not a missing implementation (INVESTIGATED, NOT FIXED, 2026-08-26)

**What:** A research pass surveyed BeepDM's other `Helpers/*.cs` classes for the same
"built, reachable, zero real callers" shape `SystemVariablesManager` had (G0.36). `ViewStateSyncer`
(`Helpers/ViewStateSyncer.cs`) is a genuine candidate by that test: its own doc comment says
"Syncs `BeepViewState` from `IUnitofWorksManager`. Shared by WPF and WinForms," and grepping the
whole `Beep.Forms` tree found zero callers of `Attach`/`Sync`/`SyncBlock`/`TryGetCurrentMessage`
anywhere in either host. `WinFormBlockHost.cs`/`BeepWpfBlock.cs` each declare
`object ViewState { get; } = new BeepViewState();` (satisfying `IBlockView.ViewState`) but never
populate it via `ViewStateSyncer` or anything else. `IFormsNotificationService`
(`Hosts/IFormsNotificationService.cs`) — "Publishes messages into a `BeepViewState` for UI
consumption" — has **zero implementations and zero callers anywhere**, not even a stub.

**Why this is not the same shape as G0.36's fixes, despite looking identical on first grep:**
`ViewStateSyncer.Sync(BeepViewState)` computes exactly six fields — `IsDirty`, `StatusText`,
`ActiveBlockName`, `RecordPositionText`, `CurrentMessage`, `MessageSeverity` — reading from
`IUnitofWorksManager.IsDirty`/`.Status`/`.CurrentBlockName`/`.GetBlock(...).UnitOfWork`/
`.Messages.GetCurrentMessage(...)`. `WinFormFormStatusBar.cs` (and its WPF mirror) already show
every one of those six, computed a **different**, already-shipped way: `IBeepFormsHost
.GetBlockStatus(block)` for position/mode/dirty, and `IBeepFormsHost.MessageRaised`/
`MessageCleared`/`ActiveBlockChanged` events for messages and the active block — and
`WinFormFormStatusBar`'s own doc comment names this explicitly as an earlier fix of the identical
"built, no consumer" shape: *"The engine end of this was already complete and had no consumer...
until 2026-08-01 no UI layer did."* Wiring `ViewStateSyncer` into the hosts now would not add any
user-visible capability — it would add a **second, parallel path** computing the same six facts a
different way, which is exactly the "two owners of one fact" defect house rule 3 exists to prevent,
not a gap to close.

**`BeepViewState`'s richer, unpopulated fields are a different, deeper story — but not one to build
either.** The model (`Models/BeepViewState.cs`) also declares `CoordinationText`, `WorkflowText`/
`WorkflowHistory`, `SavepointText`, `AlertText`, `ErrorCount` + location, `AggregateText`,
`ConnectionName`. None of these are ever set by `ViewStateSyncer.Sync` itself, let alone anything
else — so this is not "wire an existing computation into the UI," it is "design and implement a
computation that has never existed," a genuinely open-ended feature addition (what should a
form-level "coordination" or "workflow history" status line even show?) rather than a mechanical
fix matching this session's scope.

**Not fixed, and deliberately not deleted.** `ViewStateSyncer`/`BeepViewState`/
`IFormsNotificationService` together read as an earlier, abandoned design for a richer status
surface, superseded by the `GetBlockStatus`/message-event mechanism that actually shipped —
resolvable under house rule 3 (standardise on the one that is actually enforced) rather than a
missing implementation under house rule 6. But `BeepViewState` is part of `IBlockView.ViewState`'s
public contract (both hosts' property declarations reference it), so removing it is a larger,
API-surface decision than deleting a single dead method — left for Fahad, the same disposition
already used for the `UpdateBlockVariables`/`_blockVars` per-block snapshot dead-end found during
G0.36 (see above): documented here so a future pass does not mistake this for a live,
missing-caller gap of the same shape as `SystemVariablesManager`'s.

**Where:** `Helpers/ViewStateSyncer.cs`, `Models/BeepViewState.cs`, `Hosts/IFormsNotificationService.cs`
(all BeepDM, unchanged); `WinFormBlockHost.cs`, `BeepWpfBlock.cs` (Beep.Forms, unchanged — their
`ViewState` property declarations are the only Beep.Forms-side reference).

---

### G0.43: `SharedBlockManager.NotifySharedBlockChanged`/`SharedBlockExists`/`RemoveSharedBlock`
had no `FormsManager`-level caller (FIXED 2026-08-26)

**What:** The same Helpers-directory sweep that found G0.42 also checked
`Helpers/SharedBlockManager.cs`. `CreateSharedBlock`/`GetSharedBlock`/`TryLockSharedBlock`/
`ReleaseSharedBlockLock` were already wired through `FormsManager.InterFormComm.cs` — the
read/write/lock half of Oracle Forms' cross-form shared-block coordination was live. But
`SharedBlockExists`/`RemoveSharedBlock` had no `FormsManager` wrapper at all despite being on
both the concrete class and `ISharedBlockManager`, and `NotifySharedBlockChanged` — the method
that raises the `SharedBlockChanged` event `ISharedBlockManager` already declared — existed only
on the **concrete** `SharedBlockManager` class, not the interface itself, so no caller typed
against the interface (which is how `FormsManager` holds `_sharedBlockManager`) could ever raise
it. A form that published a block via `CreateSharedBlock` and another form that fetched it via
`GetSharedBlock` had no way to learn the first form committed a change to it.

**Fix:**
1. Added `void NotifySharedBlockChanged(string blockName, string changedBy, object changedRecord = null);`
   to `ISharedBlockManager` (`IMultiForm.cs`) — `SharedBlockManager` was confirmed (grep) to be
   the sole implementer, so no cascading breakage.
2. Added `FormsManager.SharedBlockExists(string)` / `RemoveSharedBlock(string)` thin wrappers to
   `FormsManager.InterFormComm.cs`, matching the existing `CreateSharedBlock`/`GetSharedBlock`
   wrapper pattern.
3. Wired the actual notification call into `CommitFormAsync`'s existing post-commit reset loop
   (the same loop G0.36 added to reset `BLOCK_STATUS`/`RECORD_STATUS` to `"QUERY"`): for each
   block just committed, if that block name is also a published shared block, call
   `NotifySharedBlockChanged(blockName, fm._currentFormName ?? "anonymous")`. This is exactly the
   "changes to a shared block were just committed" moment the method's own doc comment already
   described — `CommitFormAsync` was simply never the caller.

**Where:** `IMultiForm.cs` (interface addition), `FormsManager.InterFormComm.cs` (two new
wrappers), `FormsManager.FormOperations.cs` (notify call inside `CommitFormAsync`'s per-form
post-commit loop).

**Tests:** `FormsManagerTests.cs` — `SharedBlockExists_AfterCreateSharedBlock_ReturnsTrue`,
`SharedBlockExists_UnknownBlock_ReturnsFalse`, `RemoveSharedBlock_RemovesIt_SharedBlockExistsThenReturnsFalse`,
`RemoveSharedBlock_UnknownBlock_ReturnsFalse`, `CommitFormAsync_CommittedBlockIsSharedBlock_NotifiesSharedBlockChanged`,
`CommitFormAsync_CommittedBlockIsNotSharedBlock_DoesNotNotify`. Each of the wrapper/notify fixes
was proven by temporarily reverting it and confirming its specific test fails, then restoring it.

**Risk of fix:** Low. Purely additive — one new interface method (sole implementer already
had it), two new thin wrappers, and one new conditional call inside an existing success path
that only fires when a committed block is also a published shared block (a feature with no
existing callers until now, so no existing behavior changes for anyone not using it).

**Not done in this pass, deliberately:** `PagingManager`'s missing getter wrappers
(`GetPageSize`/`GetCurrentPage`/`GetFetchAheadDepth`/`ResetPaging` have no `FormsManager`-level
wrapper) were investigated alongside this fix and found to be a lower-priority case of the same
shape — but unlike `NotifySharedBlockChanged`, they are already reachable through the pre-existing
`public IPagingManager Paging => _pagingManager;` escape-hatch property, so "zero direct
`FormsManager`-level wrapper" does not mean "zero real callers reachable" here. Adding redundant
wrappers for state already reachable through `Paging` would grow the public surface without
closing any actual gap (house rule: don't add features beyond what's needed) — left as-is rather
than built.

---

### G0.44: `Helpers/TypeBridgeAdapters.cs` (`ValidationBridge`/`TriggerBridge`/`LOVBridge`) —
checked and found to be scoped to a different, excluded product, not a Beep.Forms gap
(INVESTIGATED, NOT FIXED, 2026-08-26)

**What:** Finishing the Helpers-directory sweep (G0.36/G0.42/G0.43) reached
`TypeBridgeAdapters.cs` last: three static classes — `ValidationBridge.ToBeepDMRule`,
`TriggerBridge.MapTriggerType`/`ToBeepDMTrigger`, `LOVBridge.ToBeepDMLOV` — plus a
`LOVColumnInfo` DTO, none referenced anywhere in either `BeepDM` or `Beep.Forms`
(confirmed by grep across both repos). On the surface this is the same "built, zero
callers" shape as G0.42/G0.43.

**Why this one is out of scope rather than a gap to close.** Every method's own doc
comment names its intended caller: `ValidationBridge`'s says *"Call from BeepDataBlock
when IsCoordinated..."*; `TriggerBridge.MapTriggerType`'s hard-coded integer ranges
(`Form=1-6, Block=100-109, Record=200-214, Item=300-311, ...`) only correspond to a
local, WinForms-specific enum that predates BeepDM's own `TriggerType`; `LOVColumnInfo`
is explicitly *"used by LOVBridge to avoid referencing WinForms LOVColumn type"*.
`BeepDataBlock` is a type from `Beep.Winform.Data.Integrated` — the pre-split ancestor
project this repo's own `CLAUDE.md` states, as a deliberate, load-bearing boundary,
that **"Beep.Forms takes no dependency on the Integrated projects... directly or
transitively."** `WinFormBlockHost`/`BeepWpfBlock` (Beep.Forms' actual runtime hosts)
are thin hosts with no local, pre-engine validation/trigger/LOV representation to
bridge from — validation, triggers and LOVs are engine-owned from the start in this
product's architecture, so there is no "local" registration on this side of the
boundary for these adapters to ever convert. `BeepDM` is the shared engine behind more
than one product; this file most plausibly still serves `Beep.Winform.Data.Integrated`
directly (a sibling repo entirely outside this session's two-repo scope), which is
exactly the same "caller lives outside the repo I can grep" shape the `FormMenuManager`
and `OracleFormatMaskTranslator` false leads turned out to have — except here the real
caller (if any) is in a repo Beep.Forms is contractually forbidden from depending on,
not merely one this session happens not to have open.

**Not fixed, and not investigated further.** Building a caller inside Beep.Forms would
mean inventing a "local rule" concept in the runtime hosts that the thin-host
architecture deliberately does not have — moving behavior into the host is the opposite
of this product's engine-owns-everything design, not a missing implementation of it.
Chasing the real caller down means opening `Beep.Winform.Data.Integrated`, which is
outside this repo's boundary by explicit rule, not merely unswept. Recorded here so a
future Helpers-directory sweep in this repo does not re-flag it as a live Beep.Forms
gap — the "zero callers in the two repos I searched" finding is real; the conclusion
that it is a defect in *this* product is not.

**Where:** `Helpers/TypeBridgeAdapters.cs` (BeepDM, unchanged).

---

### G0.45: `BlockFieldDefinition.IsReadOnly` never reached `ItemInfo.InsertAllowed`/
`UpdateAllowed` through `PropertyClassManager.ApplyToItem` (FIXED 2026-08-26)

**What:** Pivoting the survey away from the now-closed Helpers-directory sweep (G0.36–G0.44) to
`Editor/Forms/Models/*.cs` for the same "accepted-then-ignored" shape G0.40/G0.41 already found
twice on `BlockFieldDefinition`. `IsReadOnly` has a complete, working IDE authoring surface —
`BlockFieldsEditorDialogData` both loads (`IsReadOnly = f.IsReadOnly`) and saves
(`f.IsReadOnly = r.IsReadOnly`) it, and `DesignerBlockGenerator` emits
`Ord.Fields[...].IsReadOnly = true;` into the user's generated `.Designer.cs` — but
`PropertyClassManager.ApplyToItem` never once read it. Both runtime hosts already fully consume
the two `ItemInfo` flags that would need to carry it: `WinFormBlockHost.cs`/`BeepWpfBlock.cs`
compute `presenter.IsReadOnly` from `!item.InsertAllowed`/`!item.UpdateAllowed` depending on the
current block mode (confirmed live in both single-record presenters and both grid-mode column
configs), so the pipeline downstream of `ItemInfo` was complete — same as G0.40's `Required`
pipeline. An author who checked "Is Read Only" on a field in the Block Fields editor — the
functional equivalent of Oracle Forms' Insert/Update Allowed = No on an item — saw it compile and
round-trip perfectly and had it silently discarded at runtime: the field stayed exactly as
editable as `InsertAllowed`/`UpdateAllowed`/the Property Class already said, i.e. usually fully
editable. Worse than most of this series' findings in one respect: an author relying on this to
protect a computed or system-managed field (an order total, a generated key) got a false sense
of protection, not just a missing convenience.

**Fix, same one-directional shape as G0.40's `IsRequired`, deliberately not touching `Enabled`:**
`BlockFieldDefinition.IsReadOnly` is a plain `bool` with no "not authored" state distinct from
`false` and no `PropertyClass` member, so `ApplyToItem` only ever forces
`item.InsertAllowed = false; item.UpdateAllowed = false;` when `fieldDefinition.IsReadOnly` is
`true` — applied after the `QueryAllowed`/`InsertAllowed`/`UpdateAllowed` cluster so it wins even
over a contradictory explicit authoring of those three. An unauthored field (the common case)
leaves whatever `InsertAllowed`/`UpdateAllowed` already resolved to untouched. Deliberately does
**not** also set `item.Enabled = false`: `IsEnabled` is `BlockFieldDefinition`'s own independent,
already-wired flag (G0.38 or earlier) driving a *different* runtime concept — a fully
disabled/greyed-out control (`presenter.IsEnabled`) — and Oracle Forms itself keeps Enabled and
Insert/Update Allowed as separate item properties an author sets independently; conflating them
would remove that independence rather than fix a gap. `QueryAllowed` is untouched for the same
reason: a read-only display field must still work as an Enter-Query search criterion unless an
author separately restricts that.

**Where:** `Helpers/PropertyClassManager.cs` (`ApplyToItem`).

**Risk of fix:** Low in the direction that matters (adds a restriction, never removes one).
Three new direct unit tests —
`PropertyClassApplyToItem_AuthoredIsReadOnlyTrue_ForcesInsertAndUpdateNotAllowed`,
`PropertyClassApplyToItem_UnauthoredIsReadOnly_KeepsExistingInsertUpdateAllowed` (the
no-regression case), and `PropertyClassApplyToItem_AuthoredIsReadOnlyTrue_OverridesExplicit
InsertUpdateAllowedTrue` (proves ordering: `IsReadOnly` wins even over an explicit contradictory
`InsertAllowed`/`UpdateAllowed = true`) — each proven via revert (commenting out the new
conditional failed both of the first two authored-true assertions with the predicted `True` vs
`False` mismatch), full 187-test suite green across 5 consecutive runs before and after, full
engine rebuild clean. No Beep.Forms code change needed — both hosts were already consumers of
`item.InsertAllowed`/`item.UpdateAllowed`.

---

### G0.46: `BlockFieldDefinition.Order` never reached `ItemInfo.TabIndex` (FIXED 2026-08-26)

**What:** Fourth instance of the `BlockFieldDefinition` "IDE round-trips perfectly, runtime never
reads it" shape in this series (after `IsRequired` G0.40, `EditorKey` G0.41, `IsReadOnly` G0.45).
`Order` has a complete authoring surface — the Block Fields editor's `BlockFieldsEditorDialogData`
renumbers every row to its current position on every save
(`for (var i = 0; i < FieldRows.Count; i++) FieldRows[i].Order = i;`), `DesignerBlockGenerator`
emits it unconditionally for every field (`Ord.Fields[...].Order = N;`), and
`FromEntityStructure` (the fresh-scaffold path) seeds it from column position at creation time —
but `PropertyClassManager.ApplyToItem` never read it, and `ItemPropertyManager
.RegisterItemsFromEntityStructure` sets every `item.TabIndex` purely from the raw datasource
column iteration order (`TabIndex = tabIndex++` inside the `structure.Fields` loop), independent
of anything the designer authored. `WinFormBlockHost.cs`/`BeepWpfBlock.cs` both already read
`item.TabIndex` every refresh (`control.TabIndex = item.TabIndex;`) to drive the actual WinForms/
WPF Tab-key navigation order. So an author who drag-reordered a block's fields in the Block
Fields editor saw that new order everywhere the designer shows it (the field list, the emitted
`Fields` collection) and nowhere the runtime's keyboard navigation did — Tab still walked fields
in their original schema/column order.

**Fix, and why it ranks rather than copies the raw value:** A naive `item.TabIndex =
fieldDefinition.Order;` overlay (the `Width`/`Enabled`/`Visible` shape) has a real regression risk
this field doesn't share with those: `Order` is a plain `int` with no "unauthored" state distinct
from its default (`0`), and unlike `Enabled`/`Visible` (where the coincident default is `true` on
both sides, a safe no-op), a **legacy or hand-written block that never went through the Block
Fields editor** can have every field's `Order` sitting at the same unauthored `0` — copying that
verbatim would collapse every item in the block onto a single duplicate `TabIndex`, discarding the
unique, deterministic sequence `RegisterItemsFromEntityStructure` already gave it. New
`DefinitionBlockRegistrar.AssignTabIndexFromAuthoredOrder` instead ranks the block's fields by a
**stable sort** on `Order` and assigns sequential `TabIndex` values from that rank: a genuinely
reordered block gets its new order; a legacy all-zero block gets the *same* unique sequence it
already had, because `OrderBy` is stable and ties resolve to original list position. Extracted as
its own `public static` method (previously inlined in `ApplyAuthoredFieldProperties`) specifically
so the ranking algorithm is unit-testable without standing up the full datasource-backed
`DefinitionBlockRegistrar.RegisterAll` integration path.

**Where:** `Helpers/DefinitionBlockRegistrar.cs` (`ApplyAuthoredFieldProperties`, new
`AssignTabIndexFromAuthoredOrder`).

**Risk of fix:** Low. Three new direct unit tests —
`AssignTabIndexFromAuthoredOrder_RanksByAuthoredOrder_NotOriginalListPosition` (a field authored
last in the list but with the lowest `Order` ranks first), `AssignTabIndexFromAuthoredOrder
_AllFieldsShareUnauthoredDefaultOrder_StillAssignsUniqueSequentialTabIndex` (the no-regression
case — three fields all at `Order = 0` still get unique, list-order-preserving `TabIndex` values,
never a duplicate), and a trivial null-list no-op guard — the first two proven via revert
(commenting out the method body failed both with the predicted `1` vs `0` mismatch). Full
190-test suite green across 5 consecutive runs before and after, full engine rebuild clean. No
Beep.Forms code change needed — both hosts were already consumers of `item.TabIndex`.

---

### G0.47: Remaining `BlockFieldDefinition` properties surveyed — sweep closed, no further
fixes found (INVESTIGATED, NOT FIXED, 2026-08-26)

**What:** Continuing the property-by-property sweep that produced G0.40/G0.41/G0.45/G0.46, every
remaining `BlockFieldDefinition` property was checked for the same "IDE round-trips it, nothing
reads it" shape. None of them are.

**`IsCheck`/`IsUnique`/`IsIndexed` — schema-constraint metadata, not a behavioral flag.**
`IsCheck`'s own doc comment says "Carries a check constraint" — this cluster (alongside
`AllowDBNull`, and unlike `IsPrimaryKey`) describes what the *database* already enforces, not
something the engine's own CRUD logic needs to act on. Confirmed by contrast with the one sibling
that genuinely is consumed: `IsPrimaryKey` feeds `DefinitionBlockRegistrar.ApplyAuthoredKeys`,
because a schemaless datasource (`.json`/`.csv`) cannot derive its own key and `UnitofWork`
needs one to build WHERE clauses — a real functional need. A CHECK/UNIQUE/INDEX constraint has no
analogous functional need at the UI layer; the database already enforces it. Zero consumers in
either repo, and no reason to invent runtime behavior for them.

**`Category` — weaker evidence than the four fixed properties; not force-fixed.** Editable
(`BlockEntityEditorDialog.xaml`'s `CategoryName` TextBox is `TwoWay`-bound) and round-trips, but
unlike `IsReadOnly`/`Order`, there is no already-existing, already-consumed runtime property
sitting unfed and waiting — `FieldTypeMapper.GetCanonicalFieldType`/`ResolveCategory` (the
engine's actual, working type-classification machinery, which both hosts' presenter registries
and grid-mode column config already switch on) derive their answer from `DataType`/`Fieldtype`
directly, never from the designer-authored `Category` string. Wiring this would mean *changing*
that resolution logic to prefer an authored override, not feeding an existing pipeline — a
larger, more speculative change than this series' other fixes, and not attempted without
stronger evidence of an actual gap in behavior a user has hit.

**`ControlType` — a likely-superseded duplicate of `EditorKey`, not a missing implementation.**
Also editable (`BlockFieldsEditorDialog.xaml`'s `ControlType` TextBox, `TwoWay`) and round-trips
identically to `EditorKey` (same load/save/emit shape), but both properties describe the same
thing — "Platform control type hint" per its own doc comment — and only `EditorKey` is wired
(`FieldTypeMapper.TryNormalizeEditorKey`, G0.41). Building a second control-selection path for
`ControlType` would be exactly the "two owners of one fact" defect house rule 3 exists to
prevent, not a gap to close. Left as a likely-vestigial duplicate; not deleted — a Block Fields
editor UI change and a public model property are a bigger decision than this series' usual
single-method removals, left for Fahad the same way `ViewStateSyncer` (G0.42) was.

**`BindingProperty`/`DataSourceId`/`Description`/`Size` — schema/display metadata, same
non-issue shape as `DataType`.** Zero consumers anywhere, no doc-comment evidence of an intended
runtime role, and no confirmed downstream property already reading a related concept the way
`InsertAllowed`/`UpdateAllowed`/`TabIndex` were for the fixed properties. Not chased further
without a concrete lead.

**`BlockNavigationDefinition`/`BlockNavigationCommand` — already fully wired, closed 2026-08-25,
one day before this sweep reached it.** Both hosts' navigation bars (`WinFormBlockNavigationBar`,
`BeepWpfBlockNavigationBar`) already consume this model; see each host's own
`ENGINE-GAP-ANALYSIS.md` for the fix history ("Navigation bar auto-discovery", "gained its six
missing command buttons"). Nothing to do here.

**The one genuinely open item found this pass — deliberately not built, and not mine to
settle:** `IBlockNavigationBar.BlockName` (added for the WinForms auto-discovery fix, 2026-08-25)
has no consumer on the WPF side — `BeepWpfForms`'s own `ENGINE-GAP-ANALYSIS.md` already documents
why: `BeepWpfForms` owns block placement as a single-active-block "content canvas," and
"auto-discovering blocks or navigation bars the way WinForms does would mean first deciding
whether that placement model changes to admit something dropped independently elsewhere in the
tree, which is a design question this pass did not settle unilaterally." That reasoning still
holds; re-litigating an architectural decision already deliberately deferred is not this sweep's
call to make either.

**This closes the `BlockFieldDefinition` property sweep this session's G0.40/G0.41/G0.45/G0.46
passes opened.** BeepDM only (docs-only, no code change — every candidate this pass checked
needed no fix).

---

### G0.48: `BlockConfiguration.MaxRecords` (per-block) never reached
`ValidateQueryResultsForModeTransition`, plus a warning-message-clobbering bug found proving it
(FIXED 2026-08-26)

**What:** Pivoting off the closed `BlockFieldDefinition` sweep to `Models/*.cs` more broadly,
`BlockConfiguration` — a rich, developer-facing per-block settings object reachable via
`Configuration.BlockConfigurations[blockName] = new BlockConfiguration{...}` — turned out to have
**nine** properties (`EnableCaching`, `EnableValidation`, `EnableAuditTrail`, `QueryTimeout`,
`MaxRecords`, `EnableOptimisticLocking`, `EnableBatchOperations`, `BatchSize`,
`EnableChangeTracking`) with zero consumers anywhere in the engine, and five more (`PageSize`,
`FetchAheadDepth`, `MaxRecordsPerFetch`, `EnableLazyLoad`, `CacheTtlMinutes`) that are write-only
mirrors of `PagingManager`'s own, separately-consumed state (`FormsManager.SetBlockPageSize`
writes to both `_pagingManager` — the real, read side — and `block.Configuration.PageSize`, which
nothing reads back). `ApplyBlockConfiguration`'s own comment (`// Apply any specific
configuration settings`) does nothing beyond the bare assignment that precedes it — a stub that
never got filled in.

Of the nine fully-dead properties, only `MaxRecords` had a genuinely tractable fix: sibling
top-level settings on the containing `UnitofWorksManagerConfiguration` — `ValidateBeforeCommit`,
`ConfirmBeforeClear`, `StopValidationOnFirstError`, `ClearCacheOnFormClose`, and critically
**`MaxRecordsPerBlock`** (the manager-*wide* default) — are all genuinely wired (confirmed after
correcting an initial grep that missed `Configuration?.` null-conditional chains, the same mistake
the `CrossBlockValidationManager` false-positive taught this session to watch for). Since
`Configuration?.MaxRecordsPerBlock ?? 10000` (`FormsManager.ModeTransitions.cs`) was already the
real, live "cap query results" mechanism, the per-block `BlockConfiguration.MaxRecords` override
sitting right next to it was the accepted-then-ignored gap — a developer who registered a tighter
limit for one specific block via `Configuration.BlockConfigurations["Ord"]` got no effect; every
block was silently governed by the same manager-wide default regardless.

**Fix, and why it checks `TryGetValue` rather than `GetBlockConfiguration`:**
`ValidateQueryResultsForModeTransition` now prefers the block's own `MaxRecords` when the block
is genuinely present in `Configuration.BlockConfigurations` (`TryGetValue`), falling back to
`MaxRecordsPerBlock ?? 10000` exactly as before for every block that never registered one.
Deliberately not `GetBlockConfiguration(blockName)` (which returns a *fresh* `new
BlockConfiguration()` — `MaxRecords = 1000` by its own compile-time default — for any
unregistered block): since `1000` and `10000` do not coincide, treating "never configured" as "an
authored 1000" would have silently *tightened* the effective limit for every block in the
product that never touched this API, the exact self-inflicted-regression shape G0.40's
`IsRequired` fix was written to avoid.

**A second, unrelated bug found proving the first one, and fixed alongside it:**
`ExecuteQueryAndEnterCrudModeAsync` sets `result.Message = "Query executed but with warnings:
..."` (and correctly leaves `result.Flag = Errors.Warning`) the moment
`ValidateQueryResultsForModeTransition` returns `IsValid = false` — then, a few lines later,
**unconditionally overwrites `result.Message`** with the generic `"Query executed successfully.
N records found..."` text regardless of whether a warning was just set. `Flag` correctly reports
`Warning`, but the *reason* was silently discarded — for any validation warning this method could
ever raise, not only the newly-wired `MaxRecords` one. First-drafted tests for the fix above
failed on exactly this (message asserted the generic success text, not the limit detail), which
is how it surfaced. Fixed by only reassigning `result.Message` with the generic success text when
`validationResult.IsValid` is true; `FirstRecordAsync` still runs regardless of the warning,
unchanged from before.

**Where:** `FormsManager.ModeTransitions.cs` (`ValidateQueryResultsForModeTransition`,
`ExecuteQueryAndEnterCrudModeAsync`).

**Risk of fix:** Low. Two new tests —
`ExecuteQueryAndEnterCrudModeAsync_RecordCountExceedsBlockSpecificMaxRecords_ReturnsWarningWithBlockLimit`
and `..._NoBlockSpecificConfiguration_FallsBackToManagerWideMaxRecordsPerBlock` (the
no-regression case) — each proven via revert independently: reverting the `MaxRecords` lookup
alone failed the first test only (`Flag` came back `Ok` instead of `Warning`); reverting the
message-preservation guard alone failed both tests identically to how they first failed before
any fix (message asserted the clobbered generic text). Full 192-test suite green across 5
consecutive runs before and after; full engine rebuild clean.

**Not attempted in this pass, deliberately — a much larger, genuinely separate body of work:**
the remaining eight dead `BlockConfiguration` properties (`EnableCaching` through
`EnableChangeTracking`), the five `PagingManager`-mirroring properties, and the top-level
`UnitofWorksManagerConfiguration.EnableLogging`/`DefaultSaveOptions`/`DefaultRollbackOptions`.
Unlike `MaxRecords`, none of these have an already-existing, already-consumed sibling mechanism to
redirect — `EnableValidation` would need a correctly-scoped guard added across (at least) three
separate `_validationManager.ValidateItem`/`ValidateRecord` call sites; `QueryTimeout`/caching/
optimistic-locking/batch-operations would each need genuinely new behavior built against
`IUnitofWork`/`IDataSource`, which have no timeout, cache, or batch parameter today. That is real,
separately-scoped design work, not a same-pass mechanical wire-up — left as a named, evidenced
future work item rather than attempted piecemeal.

---

### G0.49: `LOVColumn`'s per-column display config (`Width`/`Visible`/`Searchable`/`Format`/
`Alignment`/`SortOrder`/`SortAscending`) is honored by the WPF LOV picker and not by
`WinFormLovDialog` — real gap, needs a grid control WinForms doesn't use here yet
(INVESTIGATED, NOT FIXED, 2026-08-26)

**What:** Surveying `LOVDefinition`/`LOVColumn` for the same shape this session's other passes
found. Most of `LOVDefinition`'s properties are genuinely wired — `LOVName`, `Title`,
`DataSourceName`, `EntityName`, `DisplayField`, `ReturnField`, `Columns`, `Filters`,
`AllowSearch`, `SearchMode`, `Width`, `Height`, `AllowMultiSelect`, `AutoPopulateRelatedFields`,
`RelatedFieldMappings`, `UseCache`, `ValidationType` are each consumed somewhere between
`LOVManager` (BeepDM) and the two runtime hosts' LOV dialogs. A cluster with no reader anywhere
(`WhereClause`, `OrderByClause`, `ShowRowNumbers`, `AutoSizeColumns`, `AutoRefresh`,
`AutoDisplay`, `AutoDisplayMinChars`, `CacheDurationMinutes`) also has **no IDE authoring
surface at all** — `LOVEditorDialogData.cs` never loads or saves any of them — so unlike
`IsReadOnly`/`Order`/`MaxRecords`, there is no live round-trip promise being silently broken;
these are developer-facing-only properties nothing has wired a consumer for, closer in shape to
`ValidationRuleLibrary` (G0.… investigated in an earlier pass) than to a defect. Not chased
further without a concrete case of code actually setting one and expecting an effect.

**`LOVColumn` is where a real, confirmed asymmetry showed up.** `WpfLovPickerDialog.xaml.cs`
consumes every one of `LOVColumn`'s display properties — `FieldName`, `DisplayName`, `Width`,
`Visible`, `Searchable`, `Format`, `Alignment`, `SortOrder`, `SortAscending` — building a real
multi-column grid. `WinFormLovDialog.cs` consumes only `Width`/`Height`/`Title` off the parent
`LOVDefinition` and builds its list from a plain `BeepListBox`: one row per record, one column of
text, computed by `GetDisplayText` from `LOVDefinition.DisplayField` alone. Every other column an
author configured — including the common two-column "Code | Description" shape
`LOVDefinition.CreateLookup`'s own factory method builds by default — is silently invisible on
WinForms while fully rendered on WPF. Confirmed this is a display-only gap, not also a
functional/search one: `LoadRecordsAsync`'s search delegates to `_host.LoadLovDataAsync`
(the engine), so filtering itself is unaffected by which columns the dialog happens to render.

**Why not fixed in this pass.** Closing this properly means giving `WinFormLovDialog` a real
multi-column grid (`BeepGridPro`, the control the codebase already uses for other multi-column
surfaces, rather than `BeepListBox`) and wiring all seven `LOVColumn` display properties into it —
new column-binding, sorting, and formatting logic, not a value flowing into an already-built,
already-consumed sink the way `IsReadOnly`/`Order`/`MaxRecords` were. That is real
control-authoring work of the same kind as the already-documented WPF single-record
date-format-mask gap and the WPF navigation-bar auto-discovery deferral — scoped out of this
pass deliberately rather than attempted as a rushed control swap.

**Where:** `TheTechIdea.Beep.Forms.WinForms/Forms/FeatureControls/WinFormLovDialog.cs` (Beep.Forms,
unchanged); `TheTechIdea.Beep.Forms.Wpf/Dialogs/WpfLovPickerDialog.xaml.cs` (Beep.Forms, unchanged,
already correct). `TheTechIdea.Beep.Forms.WinForms/Forms/ENGINE-GAP-ANALYSIS.md`'s "LOV |
... | WinFormLovDialog | Implemented" row is accurate for LOV's core mechanics (return sentinel,
related-field population, single-value display) but did not call out this narrower, real
multi-column-rendering gap — corrected alongside this entry.

---

### G0.50: `RecordGroup.LastPopulatedAt` never set by `PopulateRecordGroupAsync` (FIXED 2026-08-26)

**What:** Continuing the `Models/*.cs` survey into `RecordGroup.cs`. `IsPopulated` and
`LastPopulatedAt` are evident siblings — both describe the same "this group was just queried"
event, and both are exposed identically through `RecordGroupPanel.GetGroups()` on both hosts and
through the IDE's Record Group navigator node — but `PopulateRecordGroupAsync` only ever set
`IsPopulated = true`, immediately after building `rg.Records`, and never touched
`LastPopulatedAt`. Any caller reading the timestamp to show "populated N minutes ago" got `null`
forever, on every record group in the product.

**Fix:** One line, at the exact call site that already sets `IsPopulated`:
`rg.LastPopulatedAt = DateTime.UtcNow;`.

**Where:** `FormsManager.RecordGroups.cs` (`PopulateRecordGroupAsync`).

**Risk of fix:** Negligible — pure addition, no existing behavior changes. One new test,
`PopulateRecordGroupAsync_OnSuccess_SetsLastPopulatedAt`, proven via revert (commenting out the
assignment failed the predicted `Assert.NotNull` check). Full 193-test suite green across 3
consecutive runs; full engine rebuild clean.

---

### G0.51: Saved query templates can be created and listed but never loaded back and re-run —
the "load" half of Query Templates is missing from both hosts' feature-control surface
(INVESTIGATED, NOT FIXED, 2026-08-26)

**What:** Continuing the `Models/*.cs` survey into `QueryTemplateInfo.cs`. `QueryBuilderManager
.SaveQueryTemplate`/`.LoadQueryTemplate`/`.GetQueryTemplates` are all genuinely implemented in
BeepDM, and both hosts expose all three through their `IBeepFormsHost` (`WinFormFormHost.Query.cs`
`LoadQueryTemplate`, `BeepWpfForms.QueryTriggers.cs`'s equivalent). But `WinFormQueryPanel` — the
actual feature-control surface an application uses (mirrored on the WPF side) — only exposes
`SaveTemplate` and `GetTemplates`. Neither calls `LoadQueryTemplate`, and there is no method
anywhere that takes a loaded template's `Filters` and actually re-applies them to the block before
calling `ExecuteQueryAsync`. `ENGINE-GAP-ANALYSIS.md`'s "Query templates/history | ... |
Implemented" row is accurate for save/list, but the entire point of a saved query template —
running it again later — has no path to happen. A user can save a query as a template and see it
in a list; there is no way to pick one and re-run it.

**Why not fixed in this pass.** `IBlockView.ExecuteQueryAsync(CancellationToken)` takes no filter
parameter, so there is no existing sink a loaded template's `Filters` could simply flow into —
unlike `IsReadOnly`/`Order`/`MaxRecords`/`LastPopulatedAt`, this is not "a value sitting next to an
already-consumed field." Making it work needs a real design decision this pass should not make
unilaterally: should applying a saved template populate the on-screen Enter-Query fields visually
(so the user sees the re-applied criteria before executing, matching Oracle Forms' own
`GET_QUERY_FILTER`/`PUT_QUERY_FILTER` visual behavior) or apply the filters silently as a WHERE
clause the user never sees? Either needs either a new `IBlockView`/`IBeepFormsHost` method or a
way to push `AppFilter`s into the block's Enter-Query field state, not a same-pass property
wire-up.

**Where:** `Helpers/QueryBuilderManager.cs` (BeepDM, unchanged, already has everything needed
except the design decision above); `WinFormQueryPanel.cs`,
`BeepWpfBlockFeaturePanels.cs`/`BeepWpfForms.QueryTriggers.cs` (Beep.Forms, unchanged — missing the
load-and-apply method on both hosts equally, not a WinForms-vs-WPF asymmetry this time).

---

### G0.52: `HandleUnsavedChangesPrompt` was a stub that always silently returned `Save`, never
actually asking the user (FIXED 2026-08-26)

**What:** Continuing the `Models/*.cs` survey to `UnsavedChangesAction`/`UnsavedChangesEventArgs`
found their sole consumer in `CreateNewRecordInMasterBlockAsync`'s unsaved-changes branch was
itself a stub. `HandleUnsavedChangesPrompt`'s own comment read *"In a real application, this
would show a dialog to the user / For now, we'll use a simple default behavior"* — and the
"default behavior" was to unconditionally return `UnsavedChangesAction.Save`, regardless of
`validationIssues`, regardless of whether a real `IAlertProvider` was wired on the host. A user
who deliberately wanted to *discard* an in-progress edit before creating a new master record — a
legitimate, common Oracle Forms workflow — got that edit **silently committed instead**, with no
way to say otherwise, on every runtime host, every time. This is a more severe defect than most
of this series' findings: not a display gap or an unfed convenience property, but a data-handling
decision silently made for the user in the wrong direction.

**Fix:** `ShowAlertAsync` (Oracle Forms `SHOW_ALERT`, already fully implemented — the exact
mechanism "Messages and alerts" already uses end to end on both runtime hosts via
`IAlertProvider`) supports up to three buttons and reports back which one was pressed. Wired
`HandleUnsavedChangesPrompt` to actually call it with "Save"/"Discard"/"Cancel" and map the result:
`Button1`→`Save`, `Button2`→`Discard`, `Button3`→`Cancel`, and — critically —
`AlertResult.None` (returned when a caller's custom provider genuinely dismisses without a
choice) also maps to `Cancel`, the same safe default the method's own exception handler already
used, since Cancel is the one outcome that neither silently commits data the user may not have
wanted saved nor silently discards data they may have wanted kept. Note `DefaultAlertProvider`
(used only when no host-specific `IAlertProvider` is wired at all — a headless engine, most test
scenarios) documents itself as "No UI available — auto-accept" and always returns `Button1`, so
that narrow no-UI-registered case is unchanged from before (still resolves to Save) — the real
behavior change is for every caller running against an actual WinForms/WPF host, where the user's
real answer is now genuinely asked and respected for the first time.

**Where:** `FormsManager.ModeTransitions.cs` (`HandleUnsavedChangesPrompt`).

**Risk of fix:** The behavior change is the fix — a caller relying on the old "always saves,
never asks" behavior will now see a real prompt when a host `IAlertProvider` is wired, which is
the documented, intended behavior the stub's own comment described as missing. One new test,
`CreateNewRecordInMasterBlockAsync_AlertProviderChoosesCancel_CancelsOperation`, proven via
revert — reverting to the old hardcoded `Save` reproduced the exact original defect concretely: the
test's mock uow then failed to actually commit (`"Cannot create new record: Save failed - ..."`),
a vivid demonstration of the stub silently attempting to save data the (mocked) user had just
chosen to cancel. Full 194-test suite green across 5 consecutive runs; full engine rebuild clean.

---
## P0 — Correctness / Existing-User Impact

### G0.1: Multi-form transactional rollback (FIXED 2026-06)

**Fix:** `CommitFormAsync` now walks the call stack via `ResolveCrossFormCommitTargets()`
to discover all FormsManager instances that should participate in the commit.
When called from a modal child form, all dirty blocks from the entire call
chain are committed together. `TryCrossFormTransactionCommitAsync` handles
failure by rolling back already-committed forms in reverse order.

This matches Oracle Forms' behavior where `CALL_FORM` shares the same database
session and `COMMIT` from the child commits everything.

> **Amended by G0.24 (2026-08-22):** the "rolling back already-committed forms"
> description above was the intent, not the implementation — no transaction was
> ever opened, so the "rollback" discarded in-memory dirty state on blocks whose
> writes were already durably persisted. See G0.24 for the fix and why a true
> rollback requires a transaction to have been open in the first place.

**Where:** `FormsManager.FormOperations.cs` — `ResolveCrossFormCommitTargets()`,
`TryCrossFormTransactionCommitAsync()`. Lines 488-580.

**Risk of fix:** Medium. Existing users that relied on the old "first caller
commits, then child commits independently" pattern will see a behavior change.
A form that calls `CommitFormAsync` from a modal child now commits the parent's
blocks too.

---

### G0.2: `WHEN-CUSTOM-ITEM-EVENT` now a first-class trigger (FIXED 2026-06-17)

**Fix:** Fixed duplicate enum value (was 174, now 178 — removed collision with
`WhenMouseMove`). Added `OnCustomItemEvent` event and `TriggerCustomItemEvent` method
to `IEventManager` / `EventManager`. Added `CustomItemEventArgs` model carrying
`EventType`, `BlockName`, `ItemName`, `Payload`, and `Properties` dictionary.

**Where:** `Models/TriggerEnums.cs:381`, `Models/CustomItemEventArgs.cs` (new),
`Helpers/EventManager.cs:54,254-264`, `Interfaces/ICoreHelpers.cs:72-78`.

---

### G0.3: Master/detail sync — silent failure on computed keys (FIXED 2026-06)

**Fix:** Added `CanRead` + `CanWrite` check with computed-property heuristic in
`Helpers/RelationshipManager.cs:323-378`. Loud log on unresolvable keys.

**Risk of fix:** Low.

---

### G0.4: Sequence collision in distributed scenarios (IMPROVED 2026-06-17)

**What:** In-memory `SequenceProvider` is per-instance. Two instances can return duplicate values.
Not a blocking gap for single-instance use. For distributed scenarios, use a
datasource-backed sequence by passing a custom `ISequenceProvider` via the constructor.

**Where:** `Helpers/SequenceProvider.cs`. No code changes needed — the interface supports injection.

---

### G0.5: TriggerDependencyManager depth limit + cycle timeout (FIXED 2026-06-17)

**Fix:** Added `MaxDependencyDepth` (default: 100) and `CycleDetectionTimeout` (default: 5s)
properties. `OrderByDependency` tracks traversal depth; `FindCycle` checks a deadline and
skips detection with a warning on timeout.

**Where:** `Helpers/TriggerDependencyManager.cs:19-33,64-75,97-103`.

---

### G0.6: Reflection-based UoW method resolution (FIXED 2026-06)

**Fix:** Replaced 6 `GetMethod("DeleteAsync")` / `GetMethod("Get")` reflection
sites with direct `IUnitofWork` interface calls. No more silent-no-op on
renamed methods.

---

### G0.7: Reflection on `Units` (Count, CurrentIndex) (FIXED 2026-06)

**Fix:** Replaced with `dynamic` dispatch. Filtered-units count now correct.

---

### G0.8: `LOVManager` concurrency + perf defects (FIXED 2026-06)

6 bugs fixed: cache read/write race, O(N) validation scan, re-registration
silent overwrite, culture-sensitive search, cleanup abort on bad definition,
property lookup bypass of `RecordPropertyAccessor`.

---

### G0.9: `TriggerManager` correctness + consolidation (FIXED 2026-06)

8 bugs fixed: re-register double-fire, 4 Get*Triggers lock-less reads,
`_suspended` non-volatile, `ClearAllTriggers` race, reflection bypass,
case-sensitive field lookup, missing cancellation token, silent missing-dep
in `OrderByDependency`.

---

### G0.10: Multi-form / inter-form correctness (FIXED 2026-06)

7 bugs fixed: broken modal-suspension, stack imbalance on exception, stack
corruption in `ReturnToCallerAsync`, TOCTOU `FormExists`/`GetForm`, silent
handler-exception swallow in message bus, silent overwrite on re-register,
TOCTOU lock release.

---

### G0.11: `ModeTransitions` correctness (FIXED 2026-06)

4 bugs fixed: `EnterQuery` source-mode rejection, double-mutate, dead
parameter, silent Query default for missing blocks.

---

### G0.12: `ValidationManager` second-pass (FIXED 2026-06)

7 bugs fixed: NRE on double-unregister, NRE on missing item, orphan entries on
concurrent `ClearAllRules`, wrong sentinel constants, `FutureDateRule`/`PastDateRule`
no-ops, uniqueness security bypass on DB error, custom-validator silent false.

---

### G0.13: `Master/Detail` second-pass (FIXED 2026-06)

4 bugs fixed: silent downgrade of explicit config, `;` separator not parsed,
over-strict primary-key fallback, dead fields on `DataBlockRelationship`.

---

### G0.14: `Triggers` second-pass (FIXED 2026-06)

6 bugs fixed: sync-over-async deadlock, `Clone` dropped `DependsOn`/`ChainMode`,
`Cancelled` not flagged in `WasCancelled`, `IsEnabled` non-volatile,
partial-registration race, timezone mix (local time vs UTC).

---

## P1 — CRUD & Data Management Parity Gaps

### G1.1: Composite-key master/detail relationships (FIXED 2026-06-17)

**Fix:** Added `DataBlockRelationship.KeyFieldMappings` collection and new
`CreateMasterDetailRelation` overload accepting `DataBlockFieldMapping[]`.
The resolver already supports multi-field mappings via `MasterDetailKeyResolution.Mappings`.
Backward-compatible — single-key string overload still works.

**Where:** `Models/DataBlockRelationship.cs:21-33`, `FormsManager.Relationships.cs:118-185`.

---

### G1.2: `RECORD_GROUP` / `RECORDGROUP_FROM_QUERY` built-ins (FIXED 2026-06-17)

**Fix:** Added `RecordGroup` model, `IRecordGroupRegistry` interface, and FormsManager
implementation. `PopulateRecordGroupAsync` creates a UoW, executes the query, and stores
records in-memory. Usable for LOVs, combo boxes, and find dialogs.

**Where:** `Models/RecordGroup.cs` (new), `Interfaces/IRecordGroupAndParameterInterfaces.cs` (new),
`FormsManager.RecordGroups.cs:13-86`, `Interfaces/IUnitofWorksManager.cs` (new members).

---

### G1.3: `LIST_VALUES` built-in (ALREADY EXISTS)

**Clarification:** `IBeepBuiltins.ListValues(blockName, fieldName)` already exists at
`Builtins/IBeepBuiltins.cs:246`. The host (`IBuiltinHost.ListLovRecords`) returns the
LOV's records as a `IReadOnlyList<object>`. No engine-side gap — already surfaced.

**Where:** `Builtins/IBeepBuiltins.cs:246`, `Builtins/IBeepBuiltins.cs:106`.

---

### G1.4: `PARAMETER` / `PARAMETER_LIST` built-ins (FIXED 2026-06-17)

**Fix:** Added `ParameterList` model, `IParameterListManager` interface, and FormsManager
implementation. Supports Create/Destroy/Add/Get/Remove/Has/Clear operations on named
parameter lists. Thread-safe via `ConcurrentDictionary`.

**Where:** `Models/ParameterList.cs` (new), `Interfaces/IRecordGroupAndParameterInterfaces.cs` (new),
`FormsManager.RecordGroups.cs:91-153`, `Interfaces/IUnitofWorksManager.cs` (new members).

---

### G1.5: `PROGRAM_UNIT` built-in — DEFERRED. RDBMS/datasource-specific (Oracle PL/SQL,
SQL Server T-SQL, etc. have incompatible calling conventions). The datasource driver
should own stored-procedure execution. Use custom triggers with `IDataSource` for
database-side procedure calls.

---

## Code Quality Fixes (Second Pass, 2026-06-17)

### CQ-1: Duplicate `SetAuditDefaults` / `ApplyAuditDefaults` (FIXED)
`ApplyAuditDefaults` was a duplicate of `SetAuditDefaults` with the same signature
and same delegate. Marked `ApplyAuditDefaults` as `[Obsolete]` and routed to
`SetAuditDefaults`. Both exist for backward compatibility.
**Where:** `FormsManager.EnhancedOperations.cs:554-562`.

### CQ-2: `OpenFormAsync` overload ambiguity (FIXED)
`FormsManager.FormOperations.cs:48` opens the LOCAL form; `FormsManager.MultiFormNavigation.cs:120`
opened a DIFFERENT form modelessly (confusing same-name overload). Renamed the
multi-form version to `OpenFormModelessAsync`; kept `OpenFormAsync` as `[Obsolete]` alias.
**Where:** `FormsManager.MultiFormNavigation.cs:120-140`.

### CQ-3: DI bypass for `_securityManager`, `_pagingManager`, `_auditManager`, `_crossBlockValidation` (FIXED)
These four managers were hardcoded to `new` instances, breaking the DI pattern.
Added constructor parameters with fallback defaults, matching the other 20+ managers.
**Where:** `FormsManager.Core.cs:119-124,154-157`.

### CQ-4: TriggerChaining DI not used (FIXED)
`InitializeTriggerChaining` supported DI parameters but the constructor never passed them.
Added `ITriggerExecutionLog` and `ITriggerDependencyManager` constructor params.
**Where:** `FormsManager.Core.cs:125-126,161`.

### CQ-5: `BeepFormsHostAdapter` stub implementations (FIXED)
Multi-form methods (`MultiFormOpenForm`, `MultiFormCloseForm`, etc.), application/form
property methods, and `ListLovRecords` were all no-op stubs returning null/false/empty.
Wired them to delegate to `_host.FormsManager` where applicable. `ListLovRecords` now
attempts to read LOV data from the block's UoW.
**Where:** `Builtins/BeepFormsHostAdapter.cs:78-120`.

### CQ-6: TriggerEnums.cs reserved-range comment (FIXED)
`WhenValidateRecord = 55` was followed by comment "Reserved 55-69" overwriting
the occupied value. Changed to "Reserved 56-69."
**Where:** `Models/TriggerEnums.cs:150`.

### CQ-11: `IBeepFormsHost` missing `CancellationToken` on 4 mutation methods (FIXED 2026-06-17)
Added `CancellationToken ct = default` to `SaveBlockAsync`, `RollbackBlockAsync`,
`InsertBlockRecordAsync`, `DeleteBlockCurrentRecordAsync` on `IBeepFormsHost`.
`BeepFormsHostAdapter` now forwards `ct` instead of silently dropping it.
**Where:** `Hosts/IBeepFormsHost.cs:56-59`, `Builtins/BeepFormsHostAdapter.cs:53-56`.

### CQ-12: `FormsManager.Logging.cs` file-scoped namespace (FIXED 2026-06-17)
Converted from file-scoped namespace (`namespace X;`) to block-scoped (`namespace X { }`)
to match all other 27 `FormsManager.*.cs` partials.
**Where:** `FormsManager.Logging.cs:1-80`.

### CQ-13: `ModeTransitionValidationResult` / `BlockModeInfo` placement (FIXED 2026-06-17)
Moved from inline definitions in `FormsManager.ModeTransitions.cs` to dedicated model file
`Models/ModeTransitionModels.cs`, matching the pattern of 65 other model classes.
**Where:** `Models/ModeTransitionModels.cs` (new), `FormsManager.ModeTransitions.cs:983-1045` (removed).

### CQ-15: `SetAuditDefaults` missing `Environment.UserName` fallback (FIXED 2026-06-17)
When `currentUser` was null, the audit-field code silently skipped `CreatedBy`/`ModifiedBy`
fields. Added `effectiveUser = currentUser ?? Environment.UserName` fallback in
`FormsSimulationHelper.SetAuditDefaults` so user audit fields are never silently skipped.
**Where:** `Helpers/FormsSimulationHelper.cs:74-88`.

### CQ-16: `PostBlockAsync` missing from host interfaces (FIXED 2026-06-17)
Added `PostBlockAsync(string, CancellationToken)` to both `IBuiltinHost` and `IBeepFormsHost`
interfaces. The `BeepBuiltins.Post()` stub (which calls Commit instead of Post) can now be
updated in the WinForms layer to call `PostBlockAsync` once the host implements it.
**Where:** `Builtins/IBeepBuiltins.cs:63`, `Hosts/IBeepFormsHost.cs:65`,
`Builtins/BeepFormsHostAdapter.cs:61`.

### CQ-17: Unused `DataBlockMode` enum values documented (FIXED 2026-06-17)
`Normal` (0), `ReadOnly` (4), and `Insert` (5) had no code that set or checked them.
Marked as "reserved for future use" with doc comments explaining current alternatives.
**Where:** `Models/DataBlockInfo.cs:91-109`.

### CQ-19: Security violation lambda → named method (memory leak fix) (FIXED 2026-06-17)
`InitializeSecurity` used an anonymous lambda for `OnSecurityViolation`, making
unsubscription impossible. Replaced with named `OnSecurityViolationHandler` method
and added `-=` call in `Dispose()`.
**Where:** `FormsManager.Security.cs:18-27`, `FormsManager.Lifecycle.cs:40`.

### CQ-20: Orphaned `DisposeTriggerChaining` never called (FIXED 2026-06-17)
`DisposeTriggerChaining()` existed but was never invoked from `Dispose()`. Added
the call in `Dispose()` alongside the other cleanup unsubscriptions.
**Where:** `FormsManager.Lifecycle.cs:41`.

### CQ-21: `_dirtyStateManager.OnUnsavedChanges` never unsubscribed (FIXED 2026-06-17)
Added `-=` unsubscription in `Dispose()`. Previously only subscribed in
`InitializeManager` with no matching cleanup.
**Where:** `FormsManager.Lifecycle.cs:39`.

### CQ-29: `OnBlockFieldChanged` audit handler never unsubscribed (FIXED 2026-06-17)
`HandleBlockFieldChangedForAudit` was subscribed to `OnBlockFieldChanged` in
`InitializeAudit()` with no matching `-=` in `Dispose()`, creating a self-referencing
handler that prevented garbage collection of the FormsManager instance. Added the
unsubscription in `Dispose()` alongside the other cleanup unsubscriptions.
**Where:** `FormsManager.Audit.cs:25`, `FormsManager.Lifecycle.cs:46`.

### CQ-22: `Blocks` property returned mutable ConcurrentDictionary (FIXED 2026-06-17)
Replaced `=> _blocks` with `=> new ReadOnlyDictionary<string, DataBlockInfo>(_blocks)`.
Prevents callers from casting to `ConcurrentDictionary` and mutating internal state.
**Where:** `FormsManager.Properties.cs:44`.

### CQ-24: `PostBlockAsync` chain completed (FIXED 2026-06-17)
Added `PostBlockAsync` to `FormsManager.BasicDataOps.cs` that calls `UoW.SaveChangesAsync`
(validate + send, no commit). Added to `IUnitofWorksManager` interface so host layer can
call it without casting. The `BeepBuiltins.Post()` can now be updated to call
`Host.PostBlockAsync()` instead of `Commit()`.
**Where:** `FormsManager.BasicDataOps.cs:286-307`, `Interfaces/IUnitofWorksManager.cs:430`.

### CQ-25: Missing interface methods added to `IUnitofWorksManager` (FIXED 2026-06-17)
Added 25+ critical methods to the interface: alerts (`SetMessage`, `ClearMessage`,
`ShowAlertAsync`), inter-form communication (`SetGlobalVariable`, `GetGlobalVariable`,
`PostMessage`, `BroadcastMessage`, `SubscribeToMessage`, `UnsubscribeFromMessage`,
`SendParameterToForm`), key triggers (`RegisterKeyTrigger`, `FireKeyTriggerAsync`),
multi-form navigation (`CallFormAsync`, `OpenFormModelessAsync`, `NewFormAsync`,
`ReturnToCallerAsync`), and `RaiseFormTriggerAsync`. Hosts no longer need to cast
`IUnitofWorksManager` to `FormsManager`.
**Where:** `Interfaces/IUnitofWorksManager.cs:433-458`.

### CQ-26: `ShowAlertAsync` on `IBeepFormsHost` — adapter no longer a stub (FIXED 2026-06-17)
Added `ShowAlertAsync` to `IBeepFormsHost`. `BeepFormsHostAdapter` now delegates
directly to `_host.ShowAlertAsync(...)` instead of returning a hardcoded
`Task.FromResult(1)`.
**Where:** `Hosts/IBeepFormsHost.cs:68-69`, `Builtins/BeepFormsHostAdapter.cs:72-73`.

### CQ-27: 4 remaining silent catch blocks — added logging (FIXED 2026-06-17)
Added `LogError` to cross-form rollback catch in `FormOperations.cs`. Added
`Debug.WriteLine` to `BeepFormsHostAdapter.ListLovRecords` catch.
`BlockPropertyManager.GetBlockProperty<T>` and `DirtyStateManager` dynamic catch
are legitimate type-conversion / optional-feature guards — left as-is with comments.
**Where:** `FormsManager.FormOperations.cs:687`, `Builtins/BeepFormsHostAdapter.cs:137`.

### CQ-28: Fragile string-type references replaced (FIXED 2026-06-17)
Replaced `"TheTechIdea.Beep.ConfigUtil.PassedArgs"` string resolution with a
`Lazy<Type>` cached field. Falls back to `typeof(object)` if the assembly reference
isn't available. Compile-time-validated type name.
**Where:** `FormsManager.ExtendedOperations.cs:21-23,486,527`.
Added null-conditional operators (`?.`) to `_lockManager`, `_dirtyStateManager` field
accesses on other FormsManager instances. These fields are initialized in the constructor
but the null-conditional provides defense-in-depth.
**Where:** `FormsManager.FormOperations.cs:307,673,686`.

### G1.6: `DBMS_APPLICATION_INFO` built-ins (FIXED 2026-06-17)

**Fix:** Added `ClientInfo` model with `ClientInfo`, `ModuleName`, `Action`, `ClientHost`,
`ClientIpAddress`, and `UserName`. FormsManager exposes `SetClientInfo`, `SetClientModule`,
`SetClientAction` methods. Datasource-agnostic — each driver translates these into its
native equivalent where supported.

**Where:** `Models/ClientInfo.cs` (new), `FormsManager.RecordGroups.cs:157-210`.

### G1.7: `CLIENT_HOST` / `CLIENT_INFO` built-ins (FIXED 2026-06-17)

**Fix:** Combined with G1.6. FormsManager exposes `SetClientHost`, `SetClientIpAddress`,
`GetClientHost` (defaults to `Environment.MachineName`), `GetClientIpAddress`.

**Where:** Same as G1.6.

---

## P2 — Data Management Nice-to-Have

### G2.1: Built-in query construction language (ENHANCED 2026-06-17)

**Enhancement:** The existing `ParseWhereClause` in `QueryBuilderManager` was enhanced with
proper parentheses-aware AND splitting, IN clause parsing, BETWEEN val1 AND val2 support,
and parameterized placeholder handling (`:1`, `:name`). The basic parser already existed;
this update added the missing operator support and robustness.

**Where:** `Helpers/QueryBuilderManager.cs:89-180`. `ParseWhereClause` + `SplitWhereConditions` + enhanced `ParseCondition`.

---

### G2.2: `EDITOR` / `TEXT_IO` built-ins (FIXED 2026-06-17)

**Fix:** Added `ReadTextFileAsync`, `WriteTextFileAsync`, `AppendTextFileAsync`,
`ReadTextLinesAsync` (TEXT_IO equivalents) and `GetMultiLineText`/`SetMultiLineText`
(EDITOR equivalents) to FormsManager. File I/O operations are datasource-agnostic.

**Where:** `FormsManager.ExtendedOperations.cs:39-82`.

### G2.3: `VARR` / batch operations — DEFERRED. Existing batch commit (`CommitFormBatchAsync`)
already handles bulk DML. Per-record VARR arrays are a niche Oracle concept.

### G2.4: `DBMS_PIPE` / `DBMS_ALERT` — DEFERRED. Datasource-agnostic engine; cross-session
messaging is a datasource-specific concern. Use `IFormMessageBus` for inter-form messaging.

### G2.5: `SET_APPLICATION_PROPERTY` presets (FIXED 2026-06-17)

**Fix:** Added `SetApplicationProperty`/`GetApplicationProperty`/`HasApplicationProperty`/
`RemoveApplicationProperty` to FormsManager with a thread-safe `ConcurrentDictionary` backing.
The host can set/read any property key — presets like `CURSOR_MODE` and `DATA_MODE` are
just conventions on a generic property bag.

**Where:** `FormsManager.ExtendedOperations.cs:14-30`.

---

## P3 — IUnitofWork / IDataSource Capability Gaps

`IUnitofWork<T>` (313 lines, 34 properties, 71 methods, 17 events) and
`IDataSource` (313 lines, 15 properties, 21 methods) provide extensive
CRUD, navigation, validation, and schema-management capabilities that
`FormsManager` does not yet surface.

> **Interface impact: NONE.** All gaps below use methods that already exist on
> the interfaces. No `IDataSource` or `IUnitofWork` changes are needed —
> every implementation (RDBMS, NoSQL, file, web API) already supports or
> gracefully degrades these operations. The work is purely additive:
> new wrapper methods on `FormsManager` that delegate to existing UoW/DataSource
> methods.

### IUnitofWork features not surfaced by FormsManager

#### G3.1: Bookmarks (FIXED 2026-06-17)

**Fix:** Added `SetBlockBookmark`, `GoToBlockBookmark`, `RemoveBlockBookmark`,
`ClearBlockBookmarks` to FormsManager. Delegates to UoW via reflection.

**Where:** `FormsManager.ExtendedOperations.cs:88-127`.

---

#### G3.2: Computed Columns (FIXED 2026-06-17; formula-string variant added 2026-08-26)

**Fix:** Added `RegisterBlockComputed`, `UnregisterBlockComputed`, `GetBlockComputedValue`,
`GetBlockComputedColumnNames`, `GetAllBlockComputedValues` to FormsManager.
Thread-safe via `ConcurrentDictionary`. Evaluates computation against current UoW record.

**2026-08-26: `FieldFormulaEvaluator` (`Helpers/FieldFormulaEvaluator.cs`) — a complete infix
formula parser for Oracle Forms' no-code `Calculation = Formula` item mode (+, -, *, / with
parentheses and field-name references) — had zero callers anywhere in the engine.** Found during a
sweep of `Helpers/*.cs` for the same "built, zero real callers" shape `SystemVariablesManager` had
(G0.36). Confirmed it is a genuinely *different* capability from `RegisterBlockComputed` above, not
a duplicate: `RegisterBlockComputed` takes a `Func<object, object>` — a delegate a *developer*
writes in code — while `FieldFormulaEvaluator` parses a *text expression* (e.g. `"QTY * PRICE"`) a
*form author* could type into a Formula property with no code at all, the actual Oracle Forms
authoring experience for calculated items. New `RegisterBlockComputedFormula(blockName,
columnName, formula)` is a thin adapter: it does not duplicate `RegisterBlockComputed`'s storage,
lookup, or error handling, it only supplies the delegate `FieldFormulaEvaluator` needs, built from
`RecordPropertyAccessor.GetAllReadable` (itself pre-existing, already used elsewhere for exactly
this "record → field dictionary" job — no new record-reflection code needed either). A malformed
formula throws inside the delegate; `GetBlockComputedValue` already catches, logs, and returns
`null` for any computation failure, so a bad formula degrades exactly the way a throwing
hand-written delegate always has — no new error handling needed. Two new tests
(`RegisterBlockComputedFormula_MultiplicationFormula_EvaluatesAgainstCurrentRecord`,
`RegisterBlockComputedFormula_MalformedFormula_ReturnsNullRatherThanThrowing`), proven via revert;
full engine build plus `FormsManager.Tests` (178/178) green across 5 consecutive runs. This is
engine-only: no IDE authoring surface (a `BlockFieldDefinition.Formula` property, a field-editor
text box) exists yet for a form author to actually type a formula through the IDE — that remains
its own, separately-scoped future piece of work; this pass only closes the "the evaluator exists
but nothing can reach it" gap at the engine API level.

**Where:** `FormsManager.ExtendedOperations.cs:131-180` (original); `RegisterBlockComputedFormula`
added immediately after `UnregisterBlockComputed` in the same file; `Editor/Forms.Tests
/FormsManagerTests.cs` (two new tests).

---

#### G3.3: Freeze / Batch Update (FIXED 2026-06-17)

**Fix:** Added `FreezeBlock`, `UnfreezeBlock`, `BeginBlockBatchUpdate` to FormsManager.
Delegates to UoW via reflection. Safe no-op when UoW doesn't support the feature.

**Where:** `FormsManager.ExtendedOperations.cs:184-218`.

---

#### G3.4: Entity-Level Search / Clone (FIXED 2026-06-17)

**Fix:** Added `FindBlockRecordAsync`, `FindBlockRecordsAsync`, `CloneBlockRecordAsync` to
FormsManager. Delegates to UoW `FindAsync`/`FindManyAsync`/`CloneItem` via reflection
with async support.

**Where:** `FormsManager.ExtendedOperations.cs:222-274`.

---

#### G3.5: UoW Change Log (FIXED 2026-06-17)

**Fix:** Added `GetBlockDetailedChangeLog` to FormsManager. Delegates to UoW
`GetChangeLog` to get per-property before/after values. Returns empty list on
unsupported UoW.

**Where:** `FormsManager.ExtendedOperations.cs:278-291`.

---

#### G3.6: UoW event → FormsManager sync is now complete (FIXED 2026-06)

**Fix:** `EventManager.cs` rewritten to subscribe to all 22 IUnitofWork events
via stored named delegates. Non-generic-safe optional events (OnItemReverted,
batch, rollback) use `dynamic` dispatch with try/catch fallback — silently
skipped on implementations that don't expose them. All delegates are now
removable via `UnsubscribeFromUnitOfWorkEvents`, eliminating the permanent
memory leak from the previous anonymous-lambda approach.

**Where:** `Helpers/EventManager.cs` — full rewrite (lines 1-450).

---

#### G3.7: UoW Virtual/Lazy Loading (FIXED 2026-06-17)

**Fix:** Added `EnableBlockVirtualMode`, `DisableBlockVirtualMode`, `GoToBlockPageAsync`,
`PrefetchBlockAdjacentPagesAsync` to FormsManager. Delegates to UoW native virtual mode
methods. Complements existing `FormsManager.Performance.cs` paging — callers can choose
the UoW-native or the FormsManager-level paging path.

**Where:** `FormsManager.ExtendedOperations.cs:295-343`.

---

### IDataSource features not surfaced by FormsManager

> **Design note:** `IDataSource` abstracts RDBMS, NoSQL, file, and web API
> sources. The gaps below must work with any data source or degrade
> gracefully when a source doesn't support a capability.

#### G3.8: Relationship auto-discovery — DEFERRED. Datasource-dependent metadata;
RDBMS has FKs, files don't. Opt-in only via `SetupBlockAsync` when source supports it.

**Source:** `IDataSource.GetChildTablesList(tableName, schema, filter)` →
`IEnumerable<ChildRelation>`, `GetEntityforeignkeys(entityName, schema)` →
`IEnumerable<RelationShipKeys>`. These represent the source's knowledge
of entity relationships. For RDBMS sources this is FK metadata; for NoSQL
it's embedded references; for file sources it may be empty.

**What FormsManager should do:** `SetupBlockAsync` should optionally
auto-discover and register known relationships. Return an empty list
when the source doesn't support relationship metadata. Always opt-in;
never force-register discovered relationships.

**Effort:** Medium. **Risk:** Low (opt-in). Cap auto-discovery at
`MaxAutoRelationships` to avoid flooding on schema-heavy sources.

---

#### G3.9: Entity lifecycle operations — DEFERRED. Datasource-dependent DDL (creates
tables for RDBMS, collections for NoSQL, file schemas for files, no-op for web APIs).
Too complex to surface at the engine level; each host/driver should own entity lifecycle.

---

#### G3.10: Source-level aggregate queries (FIXED 2026-06-17)

**Fix:** Added `GetBlockAggregateScalarAsync` to FormsManager. Delegates to
`IDataSource.GetScalarAsync` for COUNT/MAX/MIN/SUM that hit the source directly
instead of computing on in-memory UoW data.

**Where:** `FormsManager.ExtendedOperations.cs:347-368`.

---

#### G3.11: Source-level transactions (FIXED 2026-06-17)

**Fix:** Added `BeginFormTransaction`, `EndFormTransaction`, `CommitFormTransaction` to
FormsManager. Iterates over all blocks' datasources, attempting to create a shared
transaction boundary. On sources that don't support transactions (file, web API),
catches and silently continues. Cross-block atomicity when the source supports it.

**Where:** `FormsManager.ExtendedOperations.cs:372-443`.

---

### IUnitofWork features already surfaced

Features the engine already delegates to UoW (no gap):

| UoW Feature | FormsManager Equivalent | How |
|-------------|------------------------|-----|
| `Get(filters)` | `ExecuteQueryAsync(blockName, filters)` | `BasicDataOps.cs` |
| `New()` | `InsertRecordAsync(blockName)` | `EnhancedOperations.cs` |
| `Delete()` | `DeleteCurrentRecordAsync(blockName)` | `BasicDataOps.cs` |
| `Commit()` | `CommitFormAsync()` | `FormOperations.cs` |
| `Rollback()` | `RollbackFormAsync()` | `FormOperations.cs` |
| `MoveFirst/Next/Previous/Last` | `FirstRecordAsync` etc. | `Navigation.cs` |
| `MoveTo(index)` | `NavigateToRecordAsync(blockName, index)` | `Navigation.cs` |
| `Sum/Average/Min/Max/Count/GroupBy` | `GetBlockSum` etc. | `DataOperations.cs` |
| `ToDataTable/ToJson/ToCsv` | `ExportBlockTo*` | `DataOperations.cs` |
| `CommitBatchAsync` | `CommitFormBatchAsync` | `DataOperations.cs` |
| `RefreshAsync` | `RefreshBlockAsync` | `DataOperations.cs` |
| `RevertItem` | `RevertCurrentRecord` / `RevertRecord` | `DataOperations.cs` |
| `Undo/Redo` | `UndoBlock` / `RedoBlock` | `DataOperations.cs` |
| `IsEmpty` | `HasUnsavedChanges` (inverse) | `DirtyState.cs` |
| `ValidateItem/ValidateAll` | `ValidateField` / `ValidateBlock` | `Validation.cs` |
| `GetInsertedItems/UpdatedItems/DeletedItems` | `GetBlockChangeSummary` | `DataOperations.cs` |

---

### IDataSource features already surfaced

| DataSource Feature | FormsManager Equivalent | How |
|--------------------|------------------------|-----|
| `GetEntityStructure(name, refresh)` | `BlockItemWorkflowCoordinator.ResolveEntityStructure` | IDE layer |
| `Entities` / `EntitiesNames` | `ConnectionNavigatorProvider.CreateEntityGroupNode` | IDE layer |
| `Openconnection` / `Closeconnection` | `ConnectionWorkflowCoordinator.OpenConnectionAsync/CloseConnectionAsync` | IDE layer |
| `GetEntity(name, filters)` | `ExecuteQueryAsync` → via UoW | `BasicDataOps.cs` |
| `ConnectionStatus` | `CreateDataSourceNode` → `IsOpen` badge | IDE provider |
| `DatasourceType` / `Category` | `GetDatabaseTypeUnicodeIcon` | IDE provider |
| `InsertEntity/UpdateEntity/DeleteEntity` | Not used directly — goes through UoW instead | Design decision |

These LOOK like gaps but are deliberate:

- **No PL/SQL engine** — Forms with PL/SQL must be ported to C# triggers.
- **No visual rendering** (fonts, colors, layouts, windows, menus, images,
  OLE, ActiveX, reports, FTP, web calls, filesystem paths) — host UI concerns.
- **No keyboard plumbing** (tab order, accelerators beyond `KEY-` triggers).
- **No data-source abstraction** — engine works through `IUnitofWork`/`IDataSource`
  regardless of backing store.
- **No user-management / authentication** — engine trusts the `SecurityContext`.

---

## See Also

- [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md) — concept-by-concept mapping
- [`enhancements.md`](enhancements.md) — improvement opportunities
- [`architecture.md`](architecture.md) — engine structure and host model
