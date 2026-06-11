# FormsManager — Gaps

This document lists the gaps between the current engine and full Oracle Forms emulation. It is the implementation roadmap for the items marked ⚠️ partial or ❌ missing in [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md).

Each gap is presented with:

- **What** — the missing / partial feature.
- **Where** — the file(s) where the gap lives, or where the fix would go.
- **Why it's a gap** — what the engine does today, and what's missing.
- **Effort** — rough estimate (small / medium / large).
- **Risk** — what could break if the gap is closed.

Items are grouped by priority. **P0** gaps are correctness issues that affect existing users. **P1** gaps are parity gaps that affect Oracle Forms migrations. **P2** gaps are nice-to-have or stretch.

---

## P0 — Correctness / Existing-User Impact

> **Audit note (2026-06):** This section was originally written without verifying the claims against source. After cross-checking:
>
> - **G0.3** as written was wrong — the `MasterDetailKeyResolver` is metadata-driven, not reflection-based. The real bug was in a different file (`Helpers/RelationshipManager.cs:323`, `GetPropertyValue`).
> - **G0.5** as written was wrong — `CreateNewRecord` is already defensive (returns null + Status message).
> - The real, unflagged P0 bugs were the **silent-no-op reflection call sites** in `RelationshipManager`, `Navigation`, `BasicDataOps`, and `EnhancedOperations`. Those have been fixed (see the "Fixed in this audit" note at the top of each item below).
>
> The remaining items in this section are real P0 issues, but the file paths and behavior descriptions were verified against source on 2026-06.

---

### G0.1: Multi-form transactional rollback

**Where:** `FormsManager.MultiFormNavigation.cs`, `FormsManager.FormOperations.cs`.

**What:** When `CommitFormAsync` is called from a form that was opened via `CallFormAsync`, and the commit succeeds for the calling form but fails for the called form, there's no automatic two-phase commit. The caller is expected to commit before the call (or the called form is expected to commit before `ReturnToCallerAsync`).

**Why it's a gap:** Users coming from Oracle Forms expect that `CALL_FORM` + `COMMIT` from the child + `RETURN` + `COMMIT` from the parent is a single transaction. The engine treats them as two.

**Effort:** Medium. Requires:
- Per-form transaction state tracking (already in place).
- A coordinator that wraps the cross-form commit into a single rollback-able unit.
- Updates to `FormCallStackEntry` to record transaction checkpoints.

**Risk:** Medium. Existing forms that rely on the current "first caller commits, then child commits" pattern would need to be migrated.

---

### G0.2: `WHEN-CUSTOM-ITEM-EVENT` is not a first-class trigger

**Where:** `Helpers/EventManager.cs`, `Helpers/TriggerManager.cs`, `Models/TriggerEnums.cs`.

**What:** Oracle Forms has a `WHEN-CUSTOM-ITEM-EVENT` trigger that fires for host-defined events (e.g. when the user clicks a custom button, when a chart fires a click event, when a JavaBean raises an event). The engine has no canonical event type for this.

**Why it's a gap:** The engine has `OnItemValueChanged` and `OnItemErrorChanged`, but no `OnCustomItemEvent(eventType, eventData)`. Hosts currently work around this by adding their own event handlers on top of the engine.

**Effort:** Small. Add a new `TriggerEventType.CustomItemEvent` with an `eventType` parameter. Wire it through `EventManager.RaiseCustomItemEvent(block, item, eventType, eventData)`.

**Risk:** Low. Pure addition.

---

### G0.3: Master/detail sync — silent failure on computed keys (REVISED, FIXED 2026-06)

**Originally claimed:** "`MasterDetailKeyResolver` reflection fallback fails on read-only properties."

**Actual bug (verified 2026-06):** The reflection bug lives in **`Helpers/RelationshipManager.cs:323` (`GetPropertyValue`)**, not in the resolver. The resolver is metadata-driven and supports composite keys. The bug:

- `GetPropertyValue` calls `property.GetValue(obj)` directly. If `obj` has a computed property whose getter throws (e.g. an `OrderId` getter that accesses a disposed datasource), `GetValue` throws, the catch swallows, the master value becomes `null`, the detail filter is skipped, the detail is never re-queried.
- The original code returned `null` silently — the user saw "master/detail sync does nothing" with no log entry.

**Fix (in this audit):** Added a `CanRead` + `CanWrite` check with a computed-property heuristic. The fix logs a loud `Errors.Failed` if the key field resolves to a read-only / non-primitive property (likely a computed value), and a separate log if the key field is not a property on the record at all. The detail sync is still skipped, but the user has a real error message to act on.

**Risk of the fix:** Low. The previous behavior was already broken in this case; the fix only adds logging.

**Where:** `Helpers/RelationshipManager.cs:323-378` (the new code).

---

### G0.4: Sequence collision in distributed scenarios

**Where:** `Helpers/SequenceProvider.cs`.

**What:** The in-memory `SequenceProvider` is per-`FormsManager` instance. Two instances in the same process (or two processes) can return the same sequence values, causing primary-key collisions.

**Why it's a gap:** The README explicitly says "prefer datasource-backed sequences first" — but if the host doesn't follow that rule (e.g. a test scenario, a non-database-backed scenario, or a custom `ISequenceProvider` that delegates to a database that doesn't actually have sequences), the collision is silent.

**Effort:** Medium. The fix is documentation + a runtime warning when an in-memory sequence is used for a block whose datasource supports auto-increment. Or, add a "process-wide" sequence provider option.

**Risk:** Low. The fix is additive.

---

### G0.5: ~~CreateNewRecord on a block with no UoW~~ (RETRACTED 2026-06)

**Originally claimed:** "`CreateNewRecord` is not defensive on blocks with no UoW."

**Verification (2026-06):** `FormsManager.EnhancedOperations.cs:27-83` (`CreateNewRecord`) **is** defensive:
- Line 32-36: `if (blockInfo?.EntityStructure == null)` returns null with a `Status` message.
- Line 48-57: returns null if no entity type can be resolved.
- Line 59-64: returns null if the entity type is abstract / interface.
- Line 77-82: catches `Exception` and returns null with the message.

**This gap does not exist.** The claim in this section was made without verifying the actual code.

---

### G0.6: Reflection-based UoW method resolution — silent no-op trap (NEW, FIXED 2026-06)

**Where:** `FormsManager.BasicDataOps.cs:126` (`GetMethod("DeleteAsync")`), `FormsManager.EnhancedOperations.cs` (4 sites: `GetMethod("Get", ...)`, `FindBestInsertMethod`, `FindBestUpdateMethod`, `GetProperty("Count")`).

**What:** Six call sites reflected on `IUnitofWork` to resolve methods that are *already declared on the interface* (e.g. `DeleteAsync`, `Get(List<AppFilter>)`, `InsertAsync`, `UpdateAsync`). Each one followed the pattern:

```csharp
var method = uow.GetType().GetMethod("DeleteAsync");
if (method != null)
{
    var task = (Task<IErrorsInfo>)method.Invoke(uow, ...);
    // ...
}
else
{
    Status = "DeleteAsync method not found";
    return false;  // or — in 3 of the 6 sites — silent return with no error
}
```

**Why it's a gap:**
- **Silent no-op on missing method.** Three of the six sites had `if (method != null) { ... }` with **no `else` branch**. A renamed method, a generic-constraint mismatch, or a wrong-arity overload would silently no-op the operation. The user would see "delete does nothing" or "query returns no results" with no log.
- **Type-unsafety.** A typo in the method-name string compiles fine. `GetMethod("InsertAsyn")` (typo) returns `null`; the engine continues without inserting.
- **Wrong semantics on the resolver that succeeded.** `FindBestInsertMethod` tried `exact match → object overload → generic method`. The "object overload" path (`InsertAsync(object doc)`) is a worst-fit — it boxes the record and uses the non-generic IUnitofWork insert path, which doesn't get the typed insert's optimizations or pre-event handling.

**Fix (in this audit):** Replaced all six sites with direct calls against the non-generic `IUnitofWork` interface (which already declares `Task<IErrorsInfo> DeleteAsync(dynamic doc)`, `Task<dynamic> Get(List<AppFilter>)`, `Task<IErrorsInfo> InsertAsync(dynamic doc)`, `Task<IErrorsInfo> UpdateAsync(dynamic doc)`). The direct call either compiles or fails loud — there's no silent no-op path.

**Where:** `FormsManager.BasicDataOps.cs:120-160`, `FormsManager.EnhancedOperations.cs:172-220` (insert), `:300-340` (update), `:380-420` (query), `:450-480` (count).

---

### G0.7: Reflection on `Units` (Count, CurrentIndex) — silent wrong-value under filters (NEW, FIXED 2026-06)

**Where:** `FormsManager.Navigation.cs:560-616` (4 sites: `GetCurrentIndex`, `GetTotalRecords`, `SetCurrentIndex`), `FormsManager.EnhancedOperations.cs:486-491` (`GetRecordCount`).

**What:** `IUnitofWork.Units` is typed `dynamic` in the non-generic interface. The actual runtime type is `ObservableBindingList<T>`, which extends `BindingList<T> : Collection<T> : IList`. The engine used `units.GetType().GetProperty("CurrentIndex" | "Position" | "Count")` reflection to read the count and current index.

**Why it's a gap:**
- **Wrong value under filter state.** `Units` is the unfiltered record list. `ObservableBindingList<T>` also has a `FilteredUnits` property that exposes the *filter-applied* count. The engine's "Get total record count" via reflection on `Units.Count` returns the wrong number when the block has an active filter — the user sees "10 records" when the displayed list shows 3.
- **Three-properties-or-fallback reflection** (`"CurrentIndex" ?? "Position"`) is a guess at the runtime type. A custom Units implementation that exposes `Position` (not `CurrentIndex`) would silently use the wrong one.
- **Silent no-op on missing property.** `if (property != null)` — a custom Units without either property returned 0 with no error.

**Fix (in this audit):** Replaced with `dynamic` dispatch. The `Units` is already typed `dynamic`; the engine just needed to use it. The dynamic dispatch uses a single-shot `CallSite` per call (cached), with full runtime type checking, and throws `RuntimeBinderException` on a missing member — which the catch handles as a "use 0 / false" return.

**Where:** `FormsManager.Navigation.cs:552-625`, `FormsManager.EnhancedOperations.cs:450-470`.

---

### G0.9: `TriggerManager` correctness, perf, and consolidation defects (NEW, FIXED 2026-06)

**Where:** `Helpers/TriggerManager.cs` (1199 lines, single class), `Helpers/TriggerLibrary.cs` (484 lines), `Helpers/TriggerDependencyManager.cs` (97 lines), `FormsManager.TriggerChaining.cs` (146 lines).

**What (eight real bugs verified against source):**

1. **Re-registering a trigger leaked the old per-scope list entry (silent double-fire).** `TriggerManager.RegisterTrigger` computed `wasReplacement` and overwrote `_triggers[trigger.TriggerId]`, but the `AddToXxxTriggers(trigger)` call then APPENDED the new entry to the per-scope list (`_formTriggers`, `_blockTriggers`, etc.) without first removing the old one. The old entry remained in the list — the next `Fire*Trigger` call ran BOTH the old and new handler. The `wasReplacement` event arg was set, but the list state did not reflect a replacement.
2. **Four `Get*Triggers` methods enumerated the inner `List<TriggerDefinition>` without locking it.** `GetBlockTriggers(string)`, `GetItemTriggers(string, string)`, `GetFormTriggers(string)`, `GetGlobalTriggers()` used `SelectMany(t => t).ToList()`. The `ConcurrentDictionary` snapshot is safe, but the inner list is not. A concurrent `AddToXxxTriggers` / `RemoveFromXxxTriggers` (which DO take the per-list lock) could mutate the list mid-enumeration and throw `InvalidOperationException` or yield a torn view. `GetBlockTriggers(TriggerType, string)` was the only method that correctly took the per-list lock.
3. **`_suspended` flag was a plain `bool` (no `volatile`).** `SuspendTriggers` / `ResumeTriggers` toggled it without an acquire/release fence. On weak memory models (ARM), a `Fire*Trigger` thread could see `_suspended == false` for a few cycles after `SuspendTriggers` returned, causing a trigger to fire after the suspend request landed.
4. **`ClearAllTriggers` was not serialized with concurrent `RegisterTrigger`.** The 5 `.Clear()` calls ran in sequence with no lock. A concurrent `RegisterTrigger` could land a new entry in `_formTriggers` between `_triggers.Clear()` and `_formTriggers.Clear()`, leaving an orphan in the per-scope list that the global dict no longer knew about.
5. **`TriggerLibrary` reflection bypassed `RecordPropertyAccessor`.** `GetFieldValue` and `SetFieldValue` did `record.GetType().GetProperty(fieldName, BindingFlags...)` directly — the pre-`RecordPropertyAccessor` re-implementation, which G1.4 already consolidated for the rest of the engine. Three inline sites (`AutoNumberTrigger.Handler`, `CascadeDeleteTrigger.Handler`, `FormatFieldTrigger.Handler`) used the same pattern.
6. **Three of those inline reflection sites had no `BindingFlags.IgnoreCase`.** `AutoNumberTrigger` (L316), `CascadeDeleteTrigger` (L425), `FormatFieldTrigger` (L466) all used `record.GetType().GetProperty(fieldName)` — case-sensitive. A trigger registered for "customerid" would not find a record property named "CustomerId". The `GetFieldValue`/`SetFieldValue` helpers already used `IgnoreCase`; the inline sites were inconsistent.
7. **`FireTriggersInOrderAsync` could not be cancelled.** The method had no `CancellationToken` parameter; it called `t.ExecuteAsync(ctx, default)` (the default `CancellationToken`). Callers had no way to cancel a long-running chain. This violated the same pattern fixed in earlier audit passes (Savepoint B2, Validation B1).
8. **`OrderByDependency` silently dropped missing dependencies.** If a trigger's `DependsOn` list referenced a `TriggerId` not in the input set, the `byId.TryGetValue(dep, out var dep)` returned false, the dep was silently skipped, and the dependent trigger ran without its prerequisite. The misconfiguration was invisible at runtime.

**Fix (in this audit):**

- **G0.9.1 (re-register):** `RegisterTrigger` now takes `_lockObject` for the duration of the removal + insert, calls `RemoveFromCollections(previous)` if the new id replaces a different definition, and only then assigns `_triggers[trigger.TriggerId] = trigger` and adds to the per-scope list. The `<remarks>` documents the replacement contract.
- **G0.9.2 (read locks):** All four `Get*Triggers` overloads now lock each inner list individually. We do NOT take a single global lock — concurrent registrations on other `TriggerType` values for the same block/item are not blocked.
- **G0.9.3 (visibility):** `_suspended` is now `volatile bool`. An inline comment explains the memory-model rationale.
- **G0.9.4 (clear):** `ClearAllTriggers` takes `_lockObject` around the 5 `.Clear()` calls so a concurrent `RegisterTrigger` cannot race and leave an orphan.
- **G0.9.5 (consolidation):** `TriggerLibrary.GetFieldValue` / `SetFieldValue` and the three inline sites now route through `RecordPropertyAccessor.TryGetValue` / `TrySetValue`. The `ConvertValue` helper is removed (the accessor handles conversion internally). The `using System.Reflection` is no longer needed.
- **G0.9.6 (case):** All three inline sites now use the accessor's case-insensitive catalog, so a trigger registered for "customerid" finds "CustomerId" on the record.
- **G0.9.7 (cancel):** `FireTriggersInOrderAsync` now takes `CancellationToken cancellationToken = default` and checks it between triggers (appends `TriggerResult.Cancelled` and returns) and forwards it to each `t.ExecuteAsync` call.
- **G0.9.8 (missing dep):** `OrderByDependency` now `Debug.WriteLine`s a warning naming the trigger and the missing id when a `DependsOn` reference cannot be resolved. The dependent trigger is still added to the result (so the chain still runs), but the operator gets a visible misconfiguration signal.

**Where:** `Helpers/TriggerManager.cs:21-58` (volatile + B10), `:80-110` (B5 Get*Triggers), `:172-230` (B3 RegisterTrigger), `:382-405` (B13 ClearAllTriggers), `Helpers/TriggerLibrary.cs:23-58` (B15 Get/SetFieldValue), `:300-320` (B15/B19 AutoNumber), `:340-360` (B15/B19 AuditStamp), `:415-440` (B15/B19 CascadeDelete), `:465-510` (B15/B19 FormatField), `Helpers/TriggerDependencyManager.cs:16-65` (B29), `FormsManager.TriggerChaining.cs:88-145` (B26).

**Risk of the fix:** Low. G0.9.1 changes the re-register contract — callers that relied on the old "append" behavior to keep both old and new handlers will see only the new one. G0.9.5 changes the silent-failure contract for `TriggerLibrary` reflection — callers that depended on the old "always no-op on read-only/missing" will now get the accessor's throttled warning. Both are intended fixes.

---

### G0.8: `LOVManager` concurrency, perf, culture, and re-registration defects (NEW, FIXED 2026-06)

**Where:** `Helpers/LOVManager.cs` (the file is single-class; fixes touched registration, data load, validation, search, cache cleanup, and the internal property-reflection helper).

**What (six real bugs verified against source):**

1. **Cache read/write race on the shared `LOVDefinition.CachedData`.** `LoadLOVDataAsync` checked `lov.IsCacheValid()` and read `lov.CachedData` outside any lock, then later wrote `lov.CachedData = records; lov.CacheTimestamp = DateTime.Now` outside any lock. A concurrent loader could observe `CacheTimestamp != null` while `CachedData` was mid-swap, or a reader could enumerate a list another thread was replacing. The class summary claimed "thread-safe using ConcurrentDictionary" — the dictionary was, the cache was not.
2. **O(N) validation scan for large LOVs.** `ValidateLOVValueAsync` walked every record on every keystroke and did `Equals(..., OrdinalIgnoreCase)` against the candidate value. For a 10k-row LOV with the user typing in a "department" field, every keystroke ran 10k reflection calls. `GetPropertyValue` (the reflection helper) re-issued `Type.GetProperty(...)` on every row.
3. **Re-registration silently overwrote the previous `LOVDefinition`.** `RegisterLOV` did `_lovs[key] = lov` with no diagnostic. A caller that registered definition A, then re-registered definition B, had no way to know A had been dropped — and any in-memory cache A held was still observable through the caller's reference.
4. **`RegisterLOV` mutates the caller's `LOVDefinition`.** The helper sets `lov.LOVName = $"{blockName}_{fieldName}_LOV"` on the instance the caller passed in. The mutation is intentional but unannounced — the caller's reference is silently changed.
5. **Culture-sensitive search.** `MatchesSearch` lowercased both sides and used the default culture-sensitive `StartsWith` / `EndsWith` / `Contains` / `==` comparisons. A German user typing "Müller" would match "Mueller" in unintended ways; a Turkish user typing "istanbul" would NOT match "İstanbul" in the data (because the Turkish capital-İ rule breaks the default ToLower invariant).
6. **One bad `LOVDefinition` aborted all cache cleanup.** `ClearAllLOVCaches` (and `ClearBlockLOVCache`) looped calling `lov.ClearCache()` with no try/catch. A single malformed definition threw and stopped the rest of the block / all LOVs from having their cache released.
7. **Property lookup bypassed `RecordPropertyAccessor`.** The internal `GetPropertyValue` did `obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)` per row. G1.4 already consolidated this onto `RecordPropertyAccessor` for the rest of the engine; LOV was missed in that sweep.

**Fix (in this audit):**

- **G0.8.1 (concurrency):** `LoadLOVDataAsync` now snapshots `IsCacheValid()` + `CachedData` inside `_lockObject` (returning a copy of the list so a reader cannot observe a torn swap). The cache write is inside the same lock, with `CachedData` assigned before `CacheTimestamp` so a reader that observes a non-null timestamp is guaranteed to see the matching list. The DB query happens outside the lock so concurrent loads for different LOVs are not serialized with each other.
- **G0.8.2 (perf):** `ValidateLOVValueAsync` builds a `Dictionary<string, object>(OrdinalIgnoreCase)` of return-field values in one pass, then `TryGetValue`s the candidate in O(1). `GetPropertyValue` now routes through `RecordPropertyAccessor.TryGetValue` so the engine-wide typed `PropertyInfo` catalog handles case-insensitive lookup and reflection caching.
- **G0.8.3 (re-register):** `RegisterLOV` now logs (`Debug.WriteLine`) when it replaces a different definition for the same key, and best-effort clears the dropped reference's cache so its rows cannot leak via a reference the caller might still hold.
- **G0.8.4 (mutation):** Documented in `<remarks>` on `RegisterLOV` with a one-line pointer in the source.
- **G0.8.5 (culture):** `MatchesSearch` pins the comparison to `StringComparison.OrdinalIgnoreCase`. `StartsWith`/`EndsWith` use the explicit overload, `==` is replaced by `string.Equals(..., OrdinalIgnoreCase)`, and `Contains` becomes `IndexOf(..., OrdinalIgnoreCase) >= 0`. `searchText.ToLower()` and the per-cell `?.ToLower()` are removed (the comparer handles the case fold).
- **G0.8.6 (cleanup):** `ClearAllLOVCaches` and `ClearBlockLOVCache` wrap each `lov.ClearCache()` in a `try`/`catch` and log the failure (with the LOV name) to `Debug.WriteLine`. The class is now defensive: one bad definition does not abort the rest of the cleanup.

**Where:** `Helpers/LOVManager.cs:60-103` (registration), `:173-278` (load), `:335-364` (validation), `:619-661` (search), `:480-510` (clear), `:686-721` (reflection helper).

**Risk of the fix:** Low. All changes are behavior-preserving for callers that use a single registration per key and that operate on a UI locale that matches the default culture. The one observable behavior change is G0.8.5: a Turkish or German user who relied on the old culture-sensitive loose match would now get a stricter comparison. That is the intended fix.

---

### G0.10: Multi-form / inter-form correctness, modal suspension, and TOCTOU defects (NEW, FIXED 2026-06)

**Where:** `FormsManager.MultiFormNavigation.cs` (128 lines), `Models/FormRegistryModels.cs` (109 lines, +1 field on `FormCallStackEntry`), `Helpers/FormRegistry.cs` (96 lines), `Helpers/FormMessageBus.cs` (107 lines), `Helpers/SharedBlockManager.cs` (93 lines, +1 lock object).

**What (seven real bugs verified against source):**

1. **Broken modal-suspension contract.** `CallFormAsync`'s `<summary>` says "suspending this one until the callee returns or is closed" and the README/HTML say "Modal child form | CallFormAsync(..., FormCallMode.Modal) | Suspends caller until child returns". The implementation set `Status = "Suspended"` and returned `true` immediately. There was no `TaskCompletionSource`, no signal, no actual blocking. The caller had no way to await the callee's lifecycle.
2. **Call stack imbalance on trigger-fire exception.** `CallFormAsync` pushed the `FormCallStackEntry` onto `_callStack` BEFORE the parameter-pass and `WhenNewFormInstance` trigger fire. If the trigger threw, the exception propagated out of `CallFormAsync`, the caller never saw a return value, AND the call stack was left with an unbalanced entry. A subsequent `ReturnToCallerAsync` would pop a stale entry as if it were the caller's own.
3. **Cross-form stack corruption in `ReturnToCallerAsync`.** The method popped the top entry unconditionally and switched the active form to `entry.CallerFormName`. If a modeless sibling form called `ReturnToCallerAsync` without ever having been opened by `CallFormAsync`, the pop was on a foreign entry — the wrong caller was reactivated.
4. **TOCTOU between `FormExists` and `GetForm`.** `CallFormAsync` called `_formRegistry.FormExists(formName)` (line 23) and then `_formRegistry.GetForm(formName)` (line 40) as two separate calls. A concurrent `UnregisterForm` in between returned null on the second call, the `is FormsManager targetFm` check failed silently, parameters were dropped, the trigger was not fired, and `SetActiveForm` was called with a form that no longer existed.
5. **Silent handler-exception swallow in the message bus.** `FormMessageBus.DispatchToSubscribers` wrapped every handler call in `try { h(msg); } catch { /* swallow handler exceptions */ }`. A subscriber whose handler throws on every dispatch silently dropped all of its messages with no signal at all.
6. **Silent overwrite on `FormRegistry.RegisterForm`.** `_forms[formName] = form;` overwrote any existing entry with no diagnostic. If the new instance differed from the old (different form manager for the same name), the old reference was dropped and any lifecycle event listeners on the registry that the old form had subscribed to would silently start firing for the new form's actions.
7. **TOCTOU in `SharedBlockManager.ReleaseSharedBlockLock`.** The method did `TryGetValue` → `Equals` → `TryRemove` as three separate operations. Between the get and the remove, another thread could `TryLockSharedBlock` and acquire the lock; our subsequent `TryRemove` would then free THEIR lock, not ours.

**Fix (in this audit):**

- **G0.10.1 (modal suspension):** `FormCallStackEntry` now has a private `TaskCompletionSource<bool>` (with `RunContinuationsAsynchronously` to avoid stack-deep continuations) and an internal `Complete(success)` method. `CallFormAsync` builds the entry first, then awaits the parameter-pass + `WhenNewFormInstance` fire; only after those succeed does it push the entry, switch the active form, and (for `FormCallMode.Modal`) genuinely `await entry.Completion` via a new `MultiFormTaskExtensions.WaitWithCancellation` helper that honors a `CancellationToken`. `ReturnToCallerAsync` calls `entry.Complete(success: true)`, unblocking the caller. The `<summary>` was updated to reflect the real semantics.
- **G0.10.2 (stack on exception):** The push to `_callStack` now happens AFTER the parameter-pass and trigger fire. If the trigger throws, the entry is not pushed and the call returns `false` — the caller sees the failure and the stack is unchanged.
- **G0.10.3 (stack validation):** `ReturnToCallerAsync` now `Peek`s the top entry and validates that `entry.FormName == _formRegistry.ActiveFormName`. If the top is not the form currently active, a `Debug.WriteLine` warning is logged and the entry is NOT popped (no stack mutation). Foreign `ReturnToCaller` calls now fail loudly.
- **G0.10.4 (TOCTOU):** `CallFormAsync` now does a single `_formRegistry.GetForm(formName)` and treats `null` as "not found". The `FormExists` + `GetForm` race is gone.
- **G0.10.5 (message bus):** The `try/catch` in `DispatchToSubscribers` now logs the swallowed exception (`Debug.WriteLine` with form name, message type, and payload type) so a bad subscriber is visible. The "don't crash the bus" behavior is preserved.
- **G0.10.6 (registry re-register):** `RegisterForm` now `Debug.WriteLine`s a warning when it replaces an entry whose instance differs from the new one. Idempotent re-registration (same instance) is still silent.
- **G0.10.7 (lock release TOCTOU):** `SharedBlockManager` now holds a short dedicated `_releaseLock` around the `TryGetValue` + `Equals` + `TryRemove` sequence in `ReleaseSharedBlockLock`. The lock is only held for the bookkeeping duration (a few CPU cycles), not across the user's critical section. `ConcurrentDictionary.TryRemove(KeyValuePair<TKey,TValue>, out TValue)` was the obvious atomic fix but the conditional overload is not present in `ConcurrentDictionary`; a short dedicated lock is the minimal-change correct alternative.

**Where:** `Models/FormRegistryModels.cs:39-77` (TCS on entry), `FormsManager.MultiFormNavigation.cs:17-110` (CallFormAsync + ReturnToCallerAsync + WaitWithCancellation helper, :140-205), `Helpers/FormRegistry.cs:32-50` (B14), `Helpers/FormMessageBus.cs:96-118` (B11), `Helpers/SharedBlockManager.cs:25-30, 84-103` (B19).

**Risk of the fix:** Medium. G0.10.1 changes the runtime semantics of `CallFormAsync` from "fire-and-forget" to "blocks the caller". Callers that were structured around the old behavior (i.e. doing work after `CallFormAsync` returned `true`, expecting to be the active form) will now find themselves blocked until the callee returns. This is a fix, not a regression — the prior behavior was a doc-vs-code lie. G0.10.3 means a foreign `ReturnToCallerAsync` now returns `false` and logs a warning instead of silently corrupting the stack.

---

### G0.11: `ModeTransitions` correctness and silent-default defects (NEW, FIXED 2026-06)

**Where:** `FormsManager.ModeTransitions.cs` (977 lines, the largest single FormsManager partial).

**What (four real bugs verified against source):**

1. **Execute-query rejected `EnterQuery` mode.** `ExecuteQueryAndEnterCrudModeAsync` (L104-181) checked `if (blockInfo.Mode != DataBlockMode.Query)` and returned `Failed` for any other mode. `DataBlockMode` has six values (`Normal`, `EnterQuery`, `Query`, `CRUD`, `ReadOnly`, `Insert`); the strict `!= Query` check rejected the legitimate `EnterQuery` state (user is typing criteria) and the implicit `Normal`/`Insert` paths. Oracle Forms allows `EXECUTE_QUERY` from `EnterQuery` to materialize the result without first leaving it; the engine blocked that flow.
2. **Defensive double-mutate in `ExecuteQueryAndEnterCrudModeAsync`.** The body re-assigned `blockInfo.Mode = DataBlockMode.CRUD` at L151 with a comment "Transition to CRUD mode (already done in ExecuteQueryEnhancedAsync, but ensure consistency)". If `ExecuteQueryEnhancedAsync` failed to switch (or switched to a different mode than expected), the outer re-assignment masked the inconsistency instead of surfacing it. The right contract is to trust the helper.
3. **`ValidateRecordForModeTransition` ignored its `record` parameter.** The signature was `ValidateRecordForModeTransition(string blockName, object record)`. The body at L811-825 did `if (record == null) return true;` and then `return ValidateBlock(blockName);` — the `record` parameter was only used for the null check, then discarded. The caller at L711-719 already has the same record in `currentRecord` (it was just `blockInfo.UnitOfWork.CurrentItem`). The signature was misleading and the parameter was effectively dead.
4. **`GetBlockMode` silently returned `Query` for missing blocks.** A null `blockName` or a block that was not registered fell through to `return DataBlockMode.Query;`. The caller could not distinguish "block is in Query mode" from "block does not exist" — a subtle silent-default that hid misconfigurations.

**Fix (in this audit):**

- **G0.11.1 (EnterQuery source):** `ExecuteQueryAndEnterCrudModeAsync` now accepts `DataBlockMode.Query` OR `DataBlockMode.EnterQuery` as valid source states. The error message at the failure branch was updated to name both valid modes.
- **G0.11.2 (double-mutate):** The redundant `blockInfo.Mode = DataBlockMode.CRUD` line was removed. The `LastModeChange` timestamp is still updated so callers can tell a mode transition happened.
- **G0.11.3 (dead param):** The `record` parameter was removed from `ValidateRecordForModeTransition`. The caller (still guarded by `if (currentRecord != null)`) now calls the no-arg version. The function body now does only the `ValidateBlock(blockName)` call, which already inspects the UoW's current record.
- **G0.11.4 (silent default):** `GetBlockMode` is unchanged (back-compat with the public `IBeepBuiltins.GetBlockMode` contract), but its `<remarks>` now documents the silent-default behavior and points callers to the new `TryGetBlockMode(string blockName, out DataBlockMode mode)` method, which returns `false` when the block is missing/null/empty and populates the `mode` out parameter on success. Callers that need to distinguish the two cases now have a non-throwing, contract-clean option.

**Where:** `FormsManager.ModeTransitions.cs:117-128` (B2), `:150-160` (B3), `:710-723 + 818-840` (B20), `:866-907` (B23).

**Risk of the fix:** Low. G0.11.1 expands the accepted source-state set — callers that were depending on the old strict rejection of `EnterQuery` will see the execute-query path succeed where it previously failed (the engine's own audit notes "Should be a fix, not a regression"). G0.11.2 trusts `ExecuteQueryEnhancedAsync` to set the mode — if a future audit pass finds the helper is broken, this trust becomes a single point of failure. G0.11.3 is a pure refactor (signature change with no behavior change). G0.11.4 is additive — `TryGetBlockMode` is new; `GetBlockMode` keeps its old signature and silent default.

---

### G0.12: `ValidationManager` second-pass audit — concurrency, security bypass, and silent-validator defects (NEW, FIXED 2026-06)

**Where:** `Helpers/ValidationManager.cs` (1269 lines, 8 affected regions), `Helpers/ValidationRuleLibrary.cs` (214 lines, `FutureDateRule` + `PastDateRule` factories).

**What (seven real bugs verified against source):**

1. **NRE on double-unregister.** `UnregisterBlockRules` called `_rulesByBlockItem.TryRemove(blockName, out var blockRules)` and then iterated `blockRules.Values`. When the block was already unregistered, `TryRemove` returned false but the code ignored the return value — `blockRules` was the default (null) reference, and `blockRules.Values` threw NRE. Realistic scenario: host tears down a block, then the form manager's teardown also runs.
2. **NRE on missing item.** `UnregisterItemRules` had the same defect: `blockRules.TryRemove(itemKey, out var itemRules)` could leave `itemRules` null, and the subsequent `foreach` over it threw NRE.
3. **Concurrent `RegisterRule` could orphan entries in `_rulesByBlockItem`.** `ClearAllRules` cleared the three dicts sequentially with no lock. A concurrent `RegisterRule` could land a new entry in `_rulesByName` after its `Clear()` and in `_rulesByBlockItem` before its `Clear()`, leaving the per-scope dict holding a reference to a rule the global dict no longer knows about.
4. **`ValidateRange` used the wrong constant for "no lower bound".** The min-default was `double.MinValue` (the most NEGATIVE double), not `double.NegativeInfinity`. The code works today because `[double.MinValue, double.MaxValue]` covers every representable double, but a future maintainer reading "double.MinValue" would reasonably ask whether the range is correctly open at the bottom. The correct sentinel for "no lower bound" is `double.NegativeInfinity`.
5. **`FutureDateRule` and `PastDateRule` were no-ops.** Both rules set `ValidationType = ValidationType.Date` and stuffed `DateTime.Today` into `MinValue` / `MaxValue`. But `ValidateDate` only checks "is the value a date" — it never compares against `MinValue` / `MaxValue`. The rules were accepted by the engine and never enforced. A user who configured a future-date rule would see the field accepted as valid even when it was set to 1990.
6. **`ValidateUnique` and `ValidateLookup` silently passed on DB error.** The old `TryQueryRows` returned a `bool` that collapsed "no rows" and "DB query threw" into a single `false`. The callers (`ValidateUnique` L970, `ValidateLookup` L945) treated `false` as "no data" and returned `true` (validation passed). A uniqueness check that silently passes on DB error is a security bypass — duplicates are allowed when the database is unavailable.
7. **`ValidateCustom` returned `false` on UI thread with no diagnostic.** When `SynchronizationContext.Current != null`, the method returned `false` (validation failed) to avoid sync-over-async deadlock. But the `false` was indistinguishable from "the field is actually invalid" — the user saw the same generic "field is invalid" message and could not tell that the engine had refused to run the validator.

**Fix (in this audit):**

- **G0.12.1 / G0.12.2 (NRE):** `UnregisterBlockRules` and `UnregisterItemRules` now check the `TryRemove` return and the null-guard before iterating. A double-unregister is a no-op.
- **G0.12.3 (orphan):** `ClearAllRules` now takes `_lockObject` around the three `.Clear()` calls so a concurrent `RegisterRule` cannot land entries between them. Same pattern as `TriggerManager.ClearAllTriggers` from pass 10.
- **G0.12.4 (constants):** `ValidateRange` uses `double.NegativeInfinity` and `double.PositiveInfinity` for the no-bound sentinels. The behavior is unchanged; the constants are now correct.
- **G0.12.5 (date-validators):** `FutureDateRule` and `PastDateRule` now use `ValidationType.GreaterThan` / `ValidationType.LessThan` with `MinValue` / `MaxValue` set to `DateTime.Today`. `Convert.ToDouble(DateTime.Today)` returns the OLE Automation date double, and `ValidateGreaterThan` / `ValidateLessThan` apply the same conversion to the field value, so the comparison is correct without changing the validation dispatcher.
- **G0.12.6 (security bypass):** A new private `enum QueryOutcome { Ok, Empty, Error }` and a new `TryQueryRowsEx` method expose the tri-state. `TryQueryRows` becomes a back-compat shim that returns `true` for `Ok + Empty`, `false` for `Error`. `ValidateUnique` now fails CLOSED on `Error` (duplicates blocked) — the deliberate security fix. `ValidateLookup` keeps the existing fail-open behavior (informational check). Both call sites now use `TryQueryRowsEx` so the caller decides.
- **G0.12.7 (custom-validator diagnostic):** A new private per-instance `_lastValidationDiagnostic` field captures the reason a `false` return was produced. `ValidateCustom` sets it when refusing on a UI thread or when an exception is thrown. `ExecuteValidation` reads it after `ValidateByType` returns and uses it as the `ErrorMessage` instead of the rule's default. The user now sees "Custom validator was not executed because the form is on a UI thread" instead of a generic "field is invalid".

**Where:** `Helpers/ValidationManager.cs:148-194` (B3+B4), `:198-217` (B5), `:805-830` (B26), `:1019-1100` (B29/B30 tri-state + back-compat shim), `:958-983, 951-1004` (B29/B30 callers), `:1117-1140` (B31 diagnostic field), `:658-680` (B31 read in ExecuteValidation), `Helpers/ValidationRuleLibrary.cs:170-200` (D4).

**Risk of the fix:** Medium. G0.12.6 is a behavior change: `ValidateUnique` now rejects saves when the database is unreachable, where the previous behavior silently allowed duplicates. This is a security improvement, but any host that was relying on the "silently allow" behavior to keep saves flowing during a DB outage will now see saves blocked. The `Debug.WriteLine` in `TryQueryRowsEx`'s catch makes the failure visible in logs so operators can spot it. G0.12.5 is also a behavior change — a user who registered a `FutureDateRule` and tested it with a past date will now see the validation actually fire (previously it was a silent no-op).

---

### G0.13: `Master/Detail` second-pass audit — resolver correctness and dead field removal (NEW, FIXED 2026-06)

**Where:** `Helpers/MasterDetailKeyResolver.cs` (395 lines, 4 affected regions), `Models/DataBlockRelationship.cs` (92 → 51 lines, dead fields stripped).

**What (four real bugs verified against source):**

1. **Silent downgrade of explicit configuration.** `MasterDetailKeyResolver.Resolve` (L34-49) detected when the user passed an explicit `MasterKeyField` / `DetailForeignKeyField` mapping. If the mapping was *incomplete* (only one of the two provided), the previous code added a "ignoring incomplete explicit" warning and FELL THROUGH to the metadata-based resolution paths (entity relations, data-source foreign keys, primary-key names). A user who passed an explicit (but partial) mapping expected it to be used — silently downgrading to a metadata guess hides the misconfiguration.
2. **Silent single-field parse on `;` separator.** `SplitFields` (L325-332) split composite keys on `,` only. A user who passed `"OrderId;LineNumber"` got a single-element array; the count check (1 vs 1) passed; a single mapping `"OrderId;LineNumber" → "OrderId;LineNumber"` was created; the field read returned null; the detail query filtered on a null value; the relationship silently did nothing.
3. **Over-strict primary-key fallback.** `TryResolveByPrimaryKeyNames` required EVERY primary key on the master to be present in the detail. A user with a 3-PK master whose detail legitimately uses only 2 of those PKs (e.g. a "header" detail that doesn't carry the line number) saw the fallback fail. The audit's pass-1 B8 fix added a log line on the resulting empty mapping list, but the underlying cause (over-strict fallback) was not fixed.
4. **Dead fields on `DataBlockRelationship`.** The class carried `CascadeDelete`, `CascadeUpdate`, `Strength`, `CustomSyncLogic`, `Metrics`, and `ExtendedProperties` — none of which were read by the master/detail engine. The metrics were never updated. The cascade / strength / custom-sync fields were placeholders for a feature that was never built. The supporting `RelationshipStrength` enum and `RelationshipMetrics` class were also dead.

**Fix (in this audit):**

- **G0.13.1 (explicit downgrade):** `MasterDetailKeyResolver.Resolve` now treats an explicit-but-incomplete mapping as an error rather than a hint. The `if (one explicit, not both)` branch returns `Unresolved` with a clear message naming both fields. The `if (both explicit, parse fails)` branch also returns `Unresolved(parseError)` instead of falling through to metadata. The caller (`CreateMasterDetailRelation`) already converts an unresolved result to `InvalidOperationException` and surfaces the error.
- **G0.13.2 (`;` separator):** `SplitFields` now splits on both `,` and `;` (passed as a `char[]` to `Split`). The mismatch-error path in `TryParseMappings` references the raw strings, so the error message naturally reflects whatever separator the user used.
- **G0.13.3 (partial PK):** `TryResolveByPrimaryKeyNames` now requires AT LEAST ONE matching primary key (previously required ALL). A master with 3 PKs and a detail with 2 of those 3 is now resolved correctly. The "exactly one match" case is still a heuristic (the engine can't disambiguate coincidence from a real partial-key relationship), but it matches the typical case and is easy to override with an explicit mapping.
- **G0.13.4 (dead fields):** `CascadeDelete`, `CascadeUpdate`, `Strength`, `CustomSyncLogic`, `Metrics`, `ExtendedProperties` are removed from `DataBlockRelationship`. The supporting `RelationshipStrength` enum and `RelationshipMetrics` class are also removed. The `<remarks>` on the class documents the removal and notes that any host depending on these fields must migrate.

**Where:** `Helpers/MasterDetailKeyResolver.cs:34-65` (B22), `:325-345` (B25/B35), `:224-275` (B29), `Models/DataBlockRelationship.cs:8-49` (B36/D6).

**Risk of the fix:** Medium. G0.13.1 is a behavior change — a user who was relying on the silent fallback to metadata when their explicit config was incomplete will now see an error. This is the intended fix (the previous behavior hid misconfigurations), but any integration test that exercised the "incomplete explicit → metadata fallback" path will need updating. G0.13.4 is a compile-time break for any host that read or set the removed fields. The fields were never used at runtime, so the runtime behavior is unchanged for callers that did NOT touch them; callers that did will see compile errors and need to migrate.

---

### G0.14: `Triggers` second-pass audit — Clone, Cancelled, race, and timezone defects (NEW, FIXED 2026-06)

**Where:** `Models/TriggerDefinition.cs` (467 → ~530 lines, 5 affected regions), `Helpers/TriggerManager.cs` (sync + async execute paths).

**What (six real bugs verified against source):**

1. **Sync-over-async deadlock in `TriggerDefinition.Execute`.** The `AsyncHandler`-only path (Handler == null, AsyncHandler != null) called `AsyncHandler(context, CancellationToken.None).GetAwaiter().GetResult()`. This is the classic sync-over-async pattern. On a UI thread with a captured `SynchronizationContext`, the awaited task tries to resume on the captured context while the context is blocked waiting for the task to complete — deadlock. The first pass (G0.9) flagged this and skipped per selection. The defect remained in `TriggerManager.ExecuteTriggerChain` (L1036) which calls `trigger.Execute(context)` from the sync fire overloads. External code that called `Execute` directly from a UI event handler would also deadlock.
2. **`Clone` dropped `DependsOn` and `ChainMode`.** The first pass added `DependsOn` (a `List<string>`) and `ChainMode` (`TriggerChainMode`) to `TriggerDefinition` but did not update `Clone()`. A trigger with `DependsOn = ["x"]` and `ChainMode = Continue` cloned to a trigger with an empty `DependsOn` and the default `StopOnFailure` chain mode — silently broken. The clone's `TriggerId` is intentionally different (assigned in the clone constructor), but the dependency list and chain mode are part of the trigger's behavior, not its identity.
3. **`Cancelled` not flagged in `WasCancelled`.** `ExecuteTriggerChain` (sync) and `ExecuteTriggerChainAsync` (async) both handle `result == TriggerResult.FormTriggerFailure` by setting `cancelled = true` and `cancelMessage = context.CancelMessage`. The `result == TriggerResult.Cancelled` case fell into the `else if (overallResult == Success)` branch, which set `overallResult = Cancelled` but left `cancelled = false`. The `TriggerChainCompletedEventArgs.WasCancelled` was computed from `cancelled`, so a cancelled chain reported `WasCancelled = false` — the cancellation was visible in `OverallResult` but not in `WasCancelled`.
4. **`IsEnabled` was non-volatile.** `TriggerDefinition.IsEnabled` is a public auto-property with a non-volatile backing field. It is mutated by `EnableTrigger` / `DisableTrigger` / `EnableBlockTriggers` / `DisableBlockTriggers` / `EnableAllTriggers` / `DisableAllTriggers` (in `TriggerManager`) and read by the per-rule `rules.Where(r => r.IsEnabled)` filter inside `ExecuteTriggerChain`. Without a memory barrier, a thread that just disabled a trigger may see a stale `IsEnabled = true` on a weak memory model and continue to execute the trigger. Same pattern as the pass-1 `_suspended` fix.
5. **Partial-registration race in `RegisterTrigger`.** The pass-1 fix moved the per-scope-list removal into `_lockObject`, but the `AddToFormTriggers` / `AddToBlockTriggers` / `AddToItemTriggers` / `AddToGlobalTriggers` calls remained OUTSIDE the lock. A concurrent `FireBlockTriggerAsync` that read `_triggers[triggerId]` after the lock was released saw the new trigger in the global dict but the per-scope list still had the OLD contents. The fire call walked the per-scope list, did not find the new trigger, and silently missed it.
6. **`LastExecutedAt` was in local time, `startTime` was UTC.** `TriggerDefinition.RecordExecution` set `LastExecutedAt = DateTime.Now` (local) while the duration was measured using `DateTime.UtcNow`. The same record mixed the two time zones — confusing for any consumer comparing the wall-clock timestamp against the elapsed duration.

**Fix (in this audit):**

- **G0.14.1 (deadlock):** `TriggerDefinition.Execute` now detects the `AsyncHandler`-only path on a captured `SynchronizationContext` and throws `InvalidOperationException` with a clear message pointing the caller at `ExecuteAsync`. The `<remarks>` documents the deadlock risk and the constraint on UI-thread callers. The `Handler`-only path and the `AsyncHandler`-only path on a non-UI thread behave as before.
- **G0.14.2 (Clone):** `Clone()` now deep-copies `DependsOn` (`new List<string>(DependsOn)` or empty if null) and copies `ChainMode`. The `<remarks>` documents the fix.
- **G0.14.3 (Cancelled):** Both `ExecuteTriggerChain` and `ExecuteTriggerChainAsync` now have an explicit `result == TriggerResult.Cancelled` branch that sets `cancelled = true`, `cancelMessage = context.CancelMessage ?? "Trigger returned Cancelled."`, and `overallResult = TriggerResult.Cancelled` (NOT `FormTriggerFailure`). The `WasCancelled` flag is now consistent with the `OverallResult`.
- **G0.14.4 (IsEnabled visibility):** `IsEnabled` is now backed by a private `volatile bool _isEnabled` field. The public property delegates to the volatile field, so reads and writes both have the acquire/release barrier. The C# compiler rejects `volatile` on a public auto-property (volatile on auto-props is restricted to certain access patterns), so the explicit-backing-field pattern is required.
- **G0.14.5 (registration race):** `RegisterTrigger` now takes `_lockObject` across BOTH the global-dict insert and the per-scope append. The per-scope `Add*` helpers lock the inner list internally, so we are nested under `_lockObject` + inner lock for the duration of the registration. The lock duration is still small (per-scope append is a dict insert + a sort).
- **G0.14.6 (timezone):** `RecordExecution` now sets `LastExecutedAt = DateTime.UtcNow` (was `DateTime.Now`). The duration is still measured from `startTime` to `DateTime.UtcNow`, so both timestamps are in UTC. Consumers that want local time for display can convert at the UI layer.

**Where:** `Models/TriggerDefinition.cs:80-99` (B4), `:185-225` (B1 + deadlock check), `:300-340` (B2 Clone + DependsOn + ChainMode), `:435-460` (B6 LastExecutedAt), `Helpers/TriggerManager.cs:1068-1100` (B3 sync), `:1215-1240` (B3 async), `:181-235` (B8 registration).

**Risk of the fix:** Medium. G0.14.1 is a behavior change — a UI-thread caller that was deadlocking now sees a clear `InvalidOperationException` instead of a hang. The exception is loud, the message is actionable, and the caller can switch to `ExecuteAsync`. Existing non-UI-thread callers are unaffected. G0.14.3 is a behavior change — `WasCancelled` is now `true` for chains where a trigger returns `Cancelled` (where it was incorrectly `false` before). G0.14.6 is a behavior change — `LastExecutedAt` is now in UTC where it was in local time. Any consumer that was formatting `LastExecutedAt` for display will see different output.

---

## P1 — Parity Gaps (Oracle Forms Migrations)

### G1.1: Composite-key master/detail relationships

**Where:** `FormsManager.Relationships.cs`, `Models/DataBlockRelationship.cs`.

**What:** `CreateMasterDetailRelation` takes a single key field. Oracle Forms supports multi-key joins.

**Why it's a gap:** Forms with composite keys (e.g. `OrderNumber + LineNumber`) cannot use the orchestrator's master/detail API. They have to bypass it and call `_relationships.Add` directly.

**Effort:** Medium. Add a `CreateMasterDetailRelation(master, detail, masterKeys, detailForeignKeys)` overload that takes parallel key lists. Update `MasterDetailKeyResolver` to handle composite keys.

**Risk:** Low. Additive.

---

### G1.2: `ABORT_QUERY` is not a first-class operation

**Where:** `FormsManager.ModeTransitions.cs`.

**What:** Oracle Forms has a `ABORT_QUERY` keyword that returns the user to the state before `ENTER_QUERY`. The engine implements this via `RollbackToSavepointAsync(blockName, "__pre_enter_query__", ct)`, but the savepoint name is a magic string and the API is awkward.

**Why it's a gap:** The built-in `ExitQuery` does this, but the orchestrator-level method is not exposed as `AbortQueryAsync`.

**Effort:** Small. Add `AbortQueryAsync(blockName, ct)` as an alias for `RollbackToSavepointAsync(blockName, "__pre_enter_query__", ct)`. Or, give the savepoint a typed name.

**Risk:** Low. Pure addition.

---

### G1.3: `LOV` column reordering and hiding

**Where:** `Models/LOVColumn.cs`, `Models/LOVDefinition.cs`.

**What:** `LOVColumn` has a `Width` property. Oracle Forms also has `DisplayWidth`, `Visible`, and `Order` properties. The engine supports only `Width`.

**Why it's a gap:** Hosts that want to hide a column from the LOV dialog or reorder columns have to do so at the host layer, not the engine.

**Effort:** Small. Add `Visible` and `Order` to `LOVColumn`. The engine doesn't need to do anything with them — just store and expose.

**Risk:** Low.

---

### G1.4: ✅ Record-property reflection consolidated to `RecordPropertyAccessor` (DONE 2026-06)

**Where:** `Helpers/RecordPropertyAccessor.cs` (new, ~270 lines) + 6 call sites rewired:
`FormsManager.Helpers.cs` (`GetPropertyValue`/`TrySetPropertyValue`),
`FormsManager.BlockRegistration.cs` (`CaptureCurrentRecordSnapshot`/`RestoreCurrentRecordSnapshot` + `ItemChanged` handler — the `typeof(Entity).GetProperty` smell),
`FormsManager.DataOperations.cs` (`GetBlockGroups`),
`FormsManager.Sequences.cs` (`ApplyFieldDefaults` + `PopulateGroupFromBlock`),
`FormsManager.Validation.cs` (validation `recordDict`),
`Helpers/FormsSimulationHelper.cs` (`GetFieldValue`/`SetFieldValue`/`GetPropertyValue` — the per-instance `_propertyCache` removed).

**What was the gap:** Twelve+ ad-hoc `record.GetType().GetProperty(name)` and `GetProperties().ToDictionary(...)` reflection call sites were scattered across the engine. Each site had the same three failures:
- Silent null returns on a misspelled `FieldName` (e.g. `BlockDefinition.FieldName = "OrderId"` on a record that has `order_id`).
- No cache, so each call did a full `Type.GetProperty` linear scan.
- `FormsSimulationHelper._propertyCache` was a per-instance string-keyed cache that allocated a `$"{type.FullName}.{propertyName}"` key on every call.

**Why it's a gap (root cause):** The engine binds records to blocks by field name, not type, so the runtime record type is opaque to the framework. The reflection is load-bearing — but the duplication and silent failure modes weren't.

**Resolution (DONE 2026-06):** Centralized into `RecordPropertyAccessor`:
- Process-wide, type-keyed catalog (`ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>>`). First call against a new type is a dict walk, not a reflection scan. Negative cache (a `null` value in the inner dict) prevents the "miss repeats forever" failure mode.
- `GetValue` / `TryGetValue` / `TrySetValue` / `GetAllReadable` / `EnumerateWritableProperties` cover every previous call site.
- Throttled diagnostic on three failure categories (missing field, read-only set attempt, access exception) — one log line per (Type, name) per minute via `Debug.WriteLine` + `IDMEEditor.AddLogMessage`.
- The `typeof(Entity).GetProperty(...)` smell in `BlockRegistration.cs:70` (which assumed every record is an `Entity` and silently returned `null` for everything else) is replaced with `RecordPropertyAccessor.GetValue(e.Item, e.PropertyName, _dmeEditor)` which reads from the **actual** runtime type.
- **Audit pass (2026-06):** Reworked the cache resolution to use `ConcurrentDictionary.GetOrAdd` with a pure factory instead of an explicit `lock`. The previous version logged inside the per-type lock, stalling all other field resolutions for the same type until `_dmeEditor.AddLogMessage` returned; the new version moves logging to the caller (after `GetOrAdd` returns). The hot path (`perType.TryGetValue`) is lock-free; the slow path (miss) takes a single global `_throttleLock` scoped strictly to throttle bookkeeping (see audit pass 2 below).
- **Audit pass 2 (2026-06):** Two regressions in the audit-pass-1 version:
  - The first version called `perType.GetOrAdd(fieldName, factory, (type, fieldName))`. The `state` argument `(type, fieldName)` is evaluated eagerly on every call, allocating a `ValueTuple<Type, string>` on the hot path — defeating the goal of reducing allocations. Fixed by adding an explicit `perType.TryGetValue` fast path (zero allocation on hit) and only allocating the tuple on the slow path (miss).
  - The first version removed the per-type `lock` but left `ShouldLogMiss` racy: two threads hitting a misspelled field simultaneously could both pass the "now - last >= interval" check and both log. Fixed by adding a single global `_throttleLock` that scopes strictly to the throttle bookkeeping (read + write of `_lastMissLog`). Logging I/O still happens outside the lock.
- **Diagnostic coverage expanded (audit pass 2):** `FormsManager.BlockRegistration.RestoreCurrentRecordSnapshot` previously caught and silently swallowed `SetValue` exceptions. Now it routes through `RecordPropertyAccessor.LogRestoreFailure`, which emits a throttled log line via the same diagnostic pipeline as the accessor's own `TrySetValue`. Promoted `RestoreCurrentRecordSnapshot` from `static` to instance so it can pass `_dmeEditor` to the accessor.
- **Audit pass 3 (2026-06):** The audit-pass-2 code was correct, but the docs and one hot-path wart were wrong:
  - The `LogMissingField` / `LogReadOnlySetAttempt` / `LogPropertyAccessFailure` methods were building the throttle key (`$"miss::{typeKey}::{fieldName}"`) **before** calling `ShouldLogMiss`. Rejected (throttled) calls paid the string allocation cost and then discarded the result. Restructured: now the three logging methods call a new `ShouldLogMiss(Type, string, string)` overload that builds the key **inside** the lock, so rejected callers pay zero allocation.
  - The lambda `_ => DateTime.MinValue` in `ShouldLogMiss.GetOrAdd` was non-static, defeating Roslyn's delegate caching (a fresh closure allocation per call). Added the `static` keyword to enable caching.
  - The `EnumerateWritableProperties` method was redundantly re-checking `property.GetIndexParameters().Length == 0` even though `BuildCatalogForType` already filters indexers at seeding time. Removed the dead check; the catalog's invariants are documented in the method's `<remarks>`.
  - The class `<summary>` XML doc claimed "Lookup rules: Public | Instance | IgnoreCase, skipping indexers" without noting that set-only properties pass the indexer filter (they have no `GetIndexParameters`) but return `null` from `GetValue` (no getter). Documented.
  - The catalog field's `<summary>` was vague about the `null` sentinel convention. Rewrote to be explicit: "the inner dict's value type is the non-nullable `PropertyInfo`, but we overload `null` as a semantic sentinel."
  - **Memory note added (B1):** the catalog holds strong references to `PropertyInfo`, which transitively roots the declaring `Type`, `Module`, and `Assembly`. Hosts that load record types from a collectible `AssemblyLoadContext` cannot unload those assemblies because the catalog keeps the `Type` rooted. The BeepDM Forms folder itself is not a collectible ALC host, so this is a future-host concern. Added a public `ClearCatalog()` method that hosts can call on ALC unload to release the references.
  - The v2 description in the `ResolveProperty` doc history was overly long. Tightened all four (v1–v4) to one-liners.

### Savepoint feature audit (2026-06)

**Where:** `FormsManager.BlockRegistration.cs` (CreateBlockSavepoint / RollbackToSavepointAsync / CaptureCurrentRecordSnapshot / RestoreCurrentRecordSnapshot), `Helpers/SavepointManager.cs`, `Models/SavepointInfo.cs`, `Interfaces/IUnitofWorksManagerInterfaces.cs` (ISavepointManager.CreateSavepoint signature).

**Real bugs found:**
- **B1+B6 — Order-of-operations bug in `FormsManager.RollbackToSavepointAsync`:** the method called `_savepointManager.RollbackToSavepointAsync` (which deletes later savepoints from the store) **before** doing the data rollback via `unitOfWork.Rollback()`. If the data rollback failed, the savepoint store had already been mutated and the user lost their other savepoints with no recovery path. **Reordered**: do the data rollback first, then call the manager to clean up the store. If the data rollback fails, the store is intact and the user can retry.
- **B2 — `SavepointManager.RollbackToSavepointAsync` did not observe the `CancellationToken`.** The body was synchronous and never checked `ct`, so the `Task<bool>` return type with a `ct` parameter was a lie. Added two `ct.ThrowIfCancellationRequested()` calls (one at entry, one after computing `toRemove`).
- **B3 — `Timestamp` collisions in `SavepointManager`.** `DateTime.UtcNow.Ticks` has ~100ns resolution on Windows/Linux. Two `CreateSavepoint` calls in the same tick window produced the same `Timestamp`, which broke the "remove savepoints created AFTER this one" filter in `RollbackToSavepointAsync` (the `>` comparison kept the colliding savepoint). Added a process-wide monotonic `Interlocked.Increment` counter; `SavepointInfo.SequenceNumber` is the new ordering key. `Timestamp` is kept for display purposes only.
- **B4 — Race in `SavepointManager.CreateSavepoint` counter increment.** Two concurrent calls with `null savepointName` on the same block could both read counter=0, both increment to 1, and produce the same auto-generated name (because the timestamp also collided). Added a `lock (_store)` around the counter read+write+name generation.
- **B5 — The previous "compiler magic" puzzle was a real build break.** In audit pass 1 I changed `CaptureCurrentRecordSnapshot` to return `IDictionary<string, object>` while the `CreateSavepoint` parameter was `Dictionary<string, object>`. Incremental builds hid the CS1503 error. Fixed by changing the `ISavepointManager.CreateSavepoint` parameter to `IDictionary<string, object>` (the manager internally materializes a `Dictionary<>` if the caller passes a non-`Dictionary<>` implementation).
- **B7 — `CreateBlockSavepoint` did not catch exceptions from `CaptureCurrentRecordSnapshot`.** Wrapped in try/catch with a `LogError` and a fall-back empty snapshot so the savepoint is still created (with no field values) rather than losing the savepoint entirely.
- **B8 — `SavepointInfo.RecordSnapshot` was `{ get; set; }` — any holder of the reference could mutate the snapshot.** Marked `{ get; init; }` and seeded with a case-insensitive comparer. The dictionary itself is still mutable; the `init` restriction applies to the property reference.
- **B11 — Silent failure paths in `RollbackToSavepointAsync`.** Three places returned `false` (or worse, `true`) without logging: `unitOfWork == null`, `unitOfWork.Rollback().Flag == Errors.Failed`, and the `savepoint.RecordIndex` out-of-range case. All now emit `LogError`.

**Notes:**
- The new ordering (B1+B6) means a savepoint rollback that succeeds does both the data and the store work. A failure in the data phase leaves the store intact (user can retry). A failure in the store phase (after data succeeded) leaves the data in the rolled-back state — a partial-failure mode that the user can recover by calling `ReleaseSavepoint` manually.

### Validation feature audit (2026-06)

**Where:** `FormsManager.Validation.cs` (114 lines), `Helpers/ValidationManager.cs` (1162 lines), `Models/ValidationResult.cs`.

**Real bugs found:**
- **B3 — Sync-over-async deadlock risk in three sites:**
  - `TryQueryRows` did sync-first, then fell back to `_dataSource.GetEntityAsync(...).GetAwaiter().GetResult()` on exception. The fallback is the classic deadlock pattern when called from a UI thread (the FormsManager runs per form on a UI thread) and the awaited task tries to resume on a captured `SynchronizationContext` that's blocked.
  - `ExecuteCrossFieldValidation` did the same pattern when the rule has a `CustomValidator` (which is `Task<...>`-returning by signature, so no sync alternative exists).
  - `ValidateCustom` did the same.
  - **Fix:** `TryQueryRows` no longer falls back to async — sync failure returns `false` and the caller can retry via `ValidateBlockAsync` if needed. The other two sites now detect `SynchronizationContext.Current != null` (i.e. UI thread) and short-circuit with a clear "use the Async entry point" error message instead of deadlocking.
- **B5 — Missed consolidation site.** `TryGetFieldValue` (line ~1100) used `Type.GetProperty(fieldName, BindingFlags.Public | Instance | IgnoreCase)` directly. This is the same anti-pattern the consolidation was supposed to eliminate. Replaced with `RecordPropertyAccessor.TryGetValue`, which provides a process-wide `PropertyInfo` cache and emits a throttled diagnostic on miss. (`TryGetFieldValue` is static and doesn't have access to `_dmeEditor`, so the diagnostic goes to `Debug.WriteLine` only — still better than a silent null return.)
- **B6 — False negative on unique validation.** `GetCompareFieldNames` (line ~1051) silently dropped fields whose value was null in the record. For a composite unique key like `(OrderId=1, LineNumber=null)`, the `LineNumber` was dropped, then the unique check matched on `OrderId` alone and falsely rejected a valid save. **Fix:** include the field with a null value in the identity set; `AreEquivalentValues` already handles `null == null` correctly.
- **B7 — Duplicate `GetRulesForItem` call.** `GetApplicableRules` (line ~539) called `GetRulesForItem(blockName, null)` and `GetRulesForItem(blockName, "*")` separately. `RegisterRule` stores rules under `rule.ItemName ?? "*"`, so both keys resolve to the same list. The duplicate was subsequently de-duped by `Distinct()`. **Fix:** removed the duplicate call.
- **B10 — False parallelism in async paths.** `ValidateBlockAsync` and `ValidateFormAsync` fan out records to `Task.WhenAll`, but each per-record validation calls `ValidateRecordAsync`, which is `Task.Run(() => ValidateRecord(...))` — sync-over-async with extra steps. The "parallelism" is real at the record level, but each record is still synchronous, so I/O-heavy records (e.g. `ValidateLookup`) don't benefit. **Fix (partial):** documented the sync-over-async nature in `<remarks>` on each async entry point. A real fix would require async rule execution, which is out of scope for this pass.
- **B11 — `RecordValidationResult.RecordIndex` and `Record` were never set.** The model had settable properties for them, but `ValidateBlock` (the only caller that could set them) didn't track the iteration index. Callers always saw `RecordIndex = 0` and `Record = null`. **Fix:** track the index in `ValidateBlock`'s foreach loop, set both `RecordIndex` and `Record` on each result. `Record` is now set to the `IDictionary<string, object>` the caller passed in (the public API doesn't have access to the original record object).

**Build state:** All 6 fixes compile under `--no-incremental` (a new lesson from this audit — incremental builds hid a CS1001 in a `>>` typo I made when adding B10's remarks; see agent memory). The build also surfaces 30 pre-existing errors in `Services/Studio/` (a new untracked folder), which are outside the scope of this audit and are not caused by these changes.

### Navigation feature audit (2026-06)

**Where:** `FormsManager.Navigation.cs` (625 lines, the 4 dynamic-dispatch helpers at L561-619 are the ones I rewired in an earlier pass to fix the silent `GetProperty` no-op).

**Real bugs found:**
- **B2 — Silent failure in 4 dynamic-dispatch helpers.** `GetCurrentIndex` / `GetTotalRecords` / `SetCurrentIndex` used `catch { return 0; }` / `catch { return false; }`, swallowing `RuntimeBinderException` when a custom `Units` implementation doesn't expose `CurrentIndex` / `Count` / `MoveTo`. The fix: `Debug.WriteLine` in the read helpers (low-noise diagnostic for the rare exotic-`Units` case) and `LogError` in `SetCurrentIndex` (because a failed `MoveTo` is a real user-visible bug, not a "Units is empty" case).
- **B3 — `SetCurrentIndex` failure was silent.** Same fix as B2, but specifically: a `MoveTo(5)` from a user-typed index is a real user action. If `MoveTo` throws (e.g. on a custom `Units` that doesn't implement `CurrentIndex` setter), the user typed a value, got a "navigation failed" status, with no diagnostic. Now logs via `LogError`.
- **B4 — `SwitchToBlockAsync` did not honor `ValidateBeforeNavigation` config flag.** The other navigation paths (`NavigateToRecordInternalAsync`, `NavigateWithValidationAsync`) honor the flag, but `SwitchToBlockAsync` didn't. A host that enables the flag would see record-level navigation validate but block switches skip validation — inconsistent. Added the same check.
- **B5 — State-ordering bug in `SwitchToBlockAsync`.** The method set `_currentBlockName = blockName` BEFORE firing `WHEN-NEW-BLOCK-INSTANCE` and `BlockEnter`. A host subscriber that reads `_currentBlockName` from within those handlers would see the new value before the engine considered the new block fully entered. Moved the assignment to AFTER `BlockEnter` so the field is consistent throughout the event chain.
- **B7 — Case-sensitive dict in `GetAllNavigationInfo`.** The rest of the engine uses `StringComparer.OrdinalIgnoreCase` for block-name dicts; this one didn't. Fixed.
- **B9 — `GoItemAsync` semantic divergence from Oracle Forms.** The method fires the `KEY-NEXT-ITEM` trigger but does NOT actually move focus (focus is a host-UI concern, and the engine has no focus model). Oracle Forms' `GO_ITEM` is supposed to move focus as a side-effect of the trigger. Documented the divergence in `<remarks>`; full fix (extend the engine with a focus model, or rename to `FireGoItemTriggerAsync`) deferred.
- **B10 — Same-index no-op in `NavigateToRecordInternalAsync`.** When `previousIndex == recordIndex`, the method returns `true` without running validation, the unsaved-changes check, or the navigation event. The behavior is intentional (matching Oracle Forms' `GO_RECORD(n)` when already on n) but undocumented. Added a `<remarks>` block explaining the contract and the suggested workaround (call `ValidateBlock` explicitly if validation is needed even on a no-op).

### Sequences feature audit (2026-06)

**Where:** `FormsManager.Sequences.cs` (175 lines, includes `ApplyItemDefaults` / `CopyFieldValue` / `PopulateGroupFromBlock` and the per-field default registry), `Helpers/SequenceProvider.cs` (70 lines, in-memory auto-increment counters).

**Real bugs found:**
- **B1 — Dead code in `ApplyItemDefaults`.** `var type = record.GetType();` at L78 was assigned but never read. Removed.
- **B2 — `ApplyItemDefaults` iterated the entire `_fieldDefaults` registry to find entries for one block.** If the user has 100 blocks with 10 defaults each, every record creation walked 1000 entries. Re-indexed: outer dict keyed on block name (case-insensitive), inner dict keyed on item name (case-insensitive). `ApplyItemDefaults` now does a single `TryGetValue` on the outer dict.
- **B3 — Field name containing `|` silently no-op'd the default.** The old storage used `key = "blockName|itemName"`, and `ApplyItemDefaults` did `kv.Key.Split('|')`. A field literally named `"Order|Id"` would split into 3 parts, fail the `parts.Length != 2` check, and the default was silently skipped. The new storage uses nested dicts, so the field name is the inner key directly with no delimiter involved.
- **B5 — `CopyFieldValue` discarded the `SetFieldValue` return value.** A failed set on the destination (e.g. read-only field, type conversion failure) silently kept going. Now logs a `LogError` so the user has a chance to notice.
- **B10 — `SequenceProvider.GetOrCreate` used a non-static lambda.** `_sequences.GetOrAdd(sequenceName, _ => new SequenceEntry(1, 1))` allocated a fresh closure on every call. Added `static` keyword to enable Roslyn's delegate caching. (Same pattern as `RecordPropertyAccessor.ShouldLogMiss` fix.)

**Combined B2+B3 fix:** by re-indexing to a nested dict, both bugs are addressed by the same structural change. The compound-string key is gone entirely; the field name is the inner key directly.

**Notes:**
- The `_fieldDefaultsByBlock` storage is a regular `Dictionary<>`, not `ConcurrentDictionary<>`. The class is not documented as thread-safe (N2 in the audit). If the host needs concurrent access, the migration to `ConcurrentDictionary<>` with appropriate locking on the inner dict is straightforward.
- The auto-create defaults in `SequenceProvider.GetOrCreate` (start=1, increment=1) are hardcoded. Documented in the audit but not changed — that's a contract-level decision the engine's author should make.

### Master/Detail feature audit (2026-06)

**Where:** `FormsManager.Relationships.cs` (179 lines, the active master/detail path), `FormsManager.Helpers.cs` (private `SynchronizeDetailHierarchyAsync` + `ClearDetailHierarchyAsync` at L289+), `Helpers/MasterDetailKeyResolver.cs` (key resolution), `Helpers/RelationshipManager.cs` (now stubbed — see deletion note below).

**Deletion of obsolete legacy code (2026-06):**
- The `[Obsolete]` `RelationshipManager` class and `IRelationshipManager` interface were deprecated wrappers around the standalone relationship helper. The active path is in `FormsManager.Relationships.cs` + `FormsManager.Helpers.cs`. The legacy class had **zero internal callers** (15 of 15 references were within the file itself). Per the user's explicit direction ("we dont need any Obsolete or legacy you can remove"), the file was emptied and the interface removed from `IUnitofWorksManagerInterfaces.cs`. The deletion + interface removal compiled cleanly (zero new errors under `--no-incremental`).

**Real bugs found in the active path:**
- **B1 — `SynchronizeDetailHierarchyAsync` did not honor a `CancellationToken`.** The method is async, fans out to multiple detail blocks, but had no way to cancel. The public `SynchronizeDetailBlocksAsync` and the interface `IUnitofWorksManager.SynchronizeDetailBlocksAsync` similarly. Added `CancellationToken ct = default` parameters throughout the chain (interface → public method → private `SynchronizeDetailHierarchyAsync` → private `ClearDetailHierarchyAsync`); observed at the start of each recursive call and at the top of each relationship iteration.
- **B3 — Culture-specific `masterValue.ToString()`.** The filter value passed to `AppFilter.FilterValue` was the current culture's default `ToString()`. A non-default culture (e.g. fr-FR for a DateTime) would produce a string that the UoW couldn't parse back, silently returning zero detail rows. Now uses `CultureInfo.InvariantCulture` for `IFormattable` values.
- **B4 — `UnitOfWork.Get(filters)` exception aborted the whole hierarchy sync.** A single failed SQL query would propagate up, leaving the remaining relationships' detail blocks in an un-synced state. Added try/catch that logs and continues to the next relationship.
- **B5 — `UnitOfWork.Clear()` exception aborted the whole hierarchy clear.** Same pattern as B4. Added try/catch with logging.
- **B8 — Silent fail on `MasterDetailKeyResolver` parse failure.** When the key resolver returned an empty mapping list (malformed composite key like `"OrderId;LineNumber"` with the wrong separator), the detail block was cleared with no diagnostic. Now logs the parse failure before the clear, so misconfigurations are visible.
- **B13 — `_masterDetailKeyResolver.Resolve` exception propagated to caller.** A malformed entity structure (e.g. missing `EntityStructure` on the detail block) could NRE deep in the resolver. Now caught, logged, and re-thrown as `InvalidOperationException` with a clearer message — the caller (typically a host UI handler) gets a useful error rather than a raw NRE.

**Build state:** All changes compile under `--no-incremental`. The 33 pre-existing errors in `Services/Studio/` (untracked folder) are outside this audit's scope.

### Block Registration feature audit (2026-06)

**Where:** `FormsManager.BlockRegistration.cs` (550 lines: `RegisterBlock`, `RegisterBlockFromSourceAsync`, `CreateBlockSavepoint`, `RollbackToSavepointAsync`, `UnregisterBlock`, `GetBlock`, `GetUnitOfWork`, `BlockExists`, plus the `CaptureCurrentRecordSnapshot` / `RestoreCurrentRecordSnapshot` private helpers).

**Real bugs found:**
- **B3 — Stale event subscription after re-registration.** The previous version overwrote `_itemChangedHandlers[blockName]` without unsubscribing the old handler from the old UoW. The dict overwrite lost the old reference, so even `UnregisterBlock` couldn't clean it up. **Combined with B4 fix below** (the auto-unregister at the top of `RegisterBlock` tears down the old UoW's subscriptions before the new registration starts, so the dict overwrite is now safe).
- **B4 — Re-registration of an existing block silently leaked the old UoW.** Calling `RegisterBlock("MyBlock", newUow, ...)` while `"MyBlock"` was already registered overwrote `_blocks[blockName]` but the old UoW's `ItemChanged` / `CurrentChanged` subscriptions were never cleaned up. The old UoW kept firing events into handlers that now thought they belonged to the new UoW — a real resource leak and a subtle correctness bug (handlers captured the old `blockName` literal, so the new UoW's events would be processed under the wrong block name after the overwrite). **Fix:** at the top of `RegisterBlock`, if `_blocks.ContainsKey(blockName)`, call `UnregisterBlock(blockName)` first. Logged as an auto-unregister so the host can see what happened.
- **B6 — Partial-failure state on `ApplyBlockConfiguration` failure.** The previous order was: store in `_blocks` (L57) → subscribe events (L62-112) → apply config (L116) → log success (L118-119) → trigger enter (L122). If `ApplyBlockConfiguration` threw, the block was half-registered (in `_blocks`, with subscriptions) but the success log had already fired and the catch at L124-130 reported a failure. The host's view was "block failed to register" but the state was "block registered, just not configured." **Fix:** reordered: build blockInfo → apply config → commit (cache + `_blocks` + subscriptions) → log success. If config fails, the block isn't visible to other code paths yet.
- **B7 — `async void` event handler swallowed exceptions.** The `CurrentChanged` handler at L106-110 was an `async (s, e) => { ... await ... }` lambda. Exceptions from `SynchronizeDetailBlocksAsync` were unobserved (no try/catch in an async-void lambda). Now wrapped in try/catch and routed to `_eventManager.TriggerError(blockName, ex)` so the host's error-event subscribers see the failure.
- **B9 — Stale perf manager cache on unregister.** `RegisterBlock` calls `_performanceManager.CacheBlockInfo` but `UnregisterBlock` never invalidated the cache. The next `GetBlock(blockName)` (which checks the cache first) would return a stale `DataBlockInfo` for an unregistered block. **Fix:** added `_performanceManager.InvalidateBlockCache(blockName)` to the unregister path.

**Build state:** All changes compile under `--no-incremental`. Zero new errors introduced.

### FormsSimulation feature audit (2026-06)

**Where:** `FormsManager.FormsSimulation.cs` (44 lines, the public surface), `Helpers/FormsSimulationHelper.cs` (416 lines, the implementation). The audit touched 3 of the 4 public wrappers and 1 internal method.

**Real bugs found:**
- **B1 — `ExecuteSequence` silently no-op'd on non-positive sequence values.** The previous version checked `if (sequenceValue != null && Convert.ToInt32(sequenceValue) > 0)` — three different failure modes (null, conversion failure, ≤ 0) all collapsed into "silent skip with no log entry." A sequence that was reset to 0 (a common dev/test scenario) or one that returned null (misconfigured registration) would leave the field un-set with no diagnostic. **Fix:** distinguish the three failure modes and log each one with a specific message that suggests the likely cause.
- **B5 — `ValidateField` was effectively a no-op without explicit constraints.** The `GetDefaultConstraints` factory returned a `FieldConstraints` with every constraint disabled, so any caller that didn't supply constraints would get `IsValid = true` for every field. The previous doc didn't mention this. **Fix:** added a `<remarks>` block that documents the no-op default and points the host to the explicit-constraints path.
- **B6 — Two helper methods weren't exposed on `FormsManager`.** `ValidateField` and `SetSystemVariables` were public on the helper but the engine wrapper had no public methods for them — hosts that wanted to set SYSTEM_DATE or validate against constraints had to reach into the helper directly. **Fix:** added `FormsManager.ValidateField` and `FormsManager.SetSystemVariables` as thin wrappers.
- **B7 — `numericTypes` array allocated on every call to `IsNumericType`.** The array had 11 elements and was allocated per-call. **Fix:** moved to a private static readonly field; replaced `Array.Exists` (which takes a `Predicate<T>` and may box) with `Array.IndexOf` (no boxing).
- **B10 — `ConvertValueToTargetType` silently swallowed conversion exceptions.** A user assigning a string to an int field via the Oracle Forms `COPY` builtin would see a "successful" set of `0` (the int default) with no log entry. **Fix:** added a `LogError` to the catch block that names the source value, the target type, and the exception.

**Build state:** All changes compile under `--no-incremental`. Zero new errors introduced.

**Behavior delta from the previous `TrySetPropertyValue`:** type-conversion failures (e.g. caller passes a string for an int property) previously threw from `Convert.ChangeType` (one of `InvalidCastException` / `FormatException` / `OverflowException` depending on the conversion failure mode); now the exception is caught and logged, and the method returns `false`. `SetValue` itself could also throw previously (`TargetException` for wrong-target type, `TargetInvocationException` for setter that throws, `NotSupportedException` for `init`-only properties in C# 9+); same catch-and-log treatment. The single caller (`FormsManager.ModeTransitions.cs:618`) was already structured `if (!TrySetPropertyValue(...))` to handle the "skip" case, so the change is observable only as "previously propagated to the caller's caller, now stops here with a log line."

**Risk of the fix:** Low. Behavior on the happy path is unchanged. Behavior on previously-silent misconfigurations (typo in `FieldName`, computed-property as key, etc.) is now visible in the log.

---

### G1.5: Visual attributes (font, color) for items and blocks

**Where:** `Helpers/ItemPropertyManager.cs`, `Models/ItemInfo.cs`.

**What:** The engine stores `FONT_NAME`, `FONT_SIZE`, `FOREGROUND_COLOR`, `BACKGROUND_COLOR`, `VISUAL_ATTRIBUTE` as property bag values. The host UI is expected to interpret them. The engine does NOT define any standard visual attribute presets (e.g. "Required", "Error", "Highlighted") — those are host-defined.

**Why it's a gap:** When a trigger body sets a visual attribute (e.g. `SET_ITEM_PROPERTY('ORDERS.STATUS', 'VISUAL_ATTRIBUTE', 'Error')`), the engine stores `"Error"` but doesn't know what that means. The host must have a preset named `"Error"`.

**Effort:** Small to medium. Define a `VisualAttribute` enum with a few standard presets (Required, Error, Highlighted, Disabled). Provide a `IVisualAttributeProvider` interface so hosts can register their own presets.

**Risk:** Low.

---

### G1.6: `MENU` / `MENUITEM` built-ins

**Where:** New file under `Builtins/`.

**What:** Oracle Forms has `MENU` (a pop-up menu), `MENUITEM` (an item in a pop-up), and `MENU` built-ins for runtime menu manipulation. The engine has none of these.

**Why it's a gap:** Forms with right-click context menus (very common) currently have to implement the menu entirely in the host UI, with no engine-level help.

**Effort:** Medium. Add `IMenuRegistry` interface, `MenuDefinition` and `MenuItemDefinition` DTOs, and `IBeepBuiltins` methods (`CreateMenu`, `AddMenuItem`, `ShowMenu`). The host renders the actual menu.

**Risk:** Low.

---

### G1.7: `WINDOW` built-ins

**Where:** New file under `Builtins/`.

**What:** Oracle Forms has `WINDOW` (a host-level window), `SET_WINDOW_PROPERTY`, `SHOW_WINDOW`, `HIDE_WINDOW`, `MOVE_WINDOW`, `RESIZE_WINDOW`. The engine has `MultiFormOpenForm` and similar but no `WINDOW` concept.

**Why it's a gap:** Forms with multiple host windows (e.g. a "Find Customer" pop-up window, a "Calendar" window) currently have to be implemented entirely in the host.

**Effort:** Large. The window concept touches `IBuiltinHost`, multi-form, and the host's window manager. Requires a `WindowDefinition` and a routing layer.

**Risk:** Medium. Multi-window in WinForms is fragile; Blazor / Razor have different windowing models.

---

### G1.8: `REPORT` built-ins

**Where:** New file under `Builtins/`.

**What:** Oracle Forms has `REPORT_OBJECT`, `RUN_REPORT_OBJECT`, `RUN_PRODUCT`. The engine has `ExportBlockToJsonAsync` / `ExportBlockToCsvAsync` but no general "run a report" surface.

**Why it's a gap:** Forms that include report output (e.g. a printed invoice) currently have to bypass the engine entirely.

**Effort:** Large. Reports are typically backed by a report server (Oracle Reports Server, SSRS, etc.). The engine would need a pluggable report-runner interface.

**Risk:** Medium. Report engines are very vendor-specific.

---

### G1.9: `IMAGE` and `OLE` built-ins

**Where:** New file under `Builtins/`.

**What:** Oracle Forms has `IMAGE` (a displayable image), `OLE` (a hosted OLE object). The engine has no image support beyond what the host provides as a property.

**Why it's a gap:** Forms with logos, signatures, or document previews cannot use engine-level methods for image loading.

**Effort:** Small to medium. Add a `ImageStore` interface and `IBeepBuiltins.ReadImage` / `WriteImage`. The host renders the actual image.

**Risk:** Low.

---

### G1.10: `CLIENT_HOST` / `CLIENT_INFO` built-ins

**Where:** New file under `Builtins/`.

**What:** Oracle Forms has `CLIENT_HOST` (the hostname of the client), `CLIENT_INFO` (user-defined client metadata), `CLIENT_IP_ADDRESS`. The engine has no client-info surface.

**Why it's a gap:** Forms that need to display "Logged in from <hostname>" or pass per-client metadata to PL/SQL have to bypass the engine.

**Effort:** Small. Add a `IClientInfoProvider` interface and `IBeepBuiltins.GetClientInfo`. The host provides the actual values (WinForms reads `Dns.GetHostName()`, Blazor reads the User-Agent, etc.).

**Risk:** Low.

---

### G1.11: `DBMS` built-ins (Oracle-specific)

**Where:** New file under `Builtins/`.

**What:** Oracle Forms has `DBMS` (the database connection), `DBMS_SERVER`, `DBMS_SESSION`, etc. The engine has no DBMS concept (it goes through `IUnitofWork` instead).

**Why it's a gap:** Forms that pass `CLIENT_INFO` to the database, or use `DBMS_APPLICATION_INFO.SET_CLIENT_INFO`, have to bypass the engine.

**Effort:** Medium. The engine would need a `IDbmsProvider` interface that the host can implement per-datasource.

**Risk:** Medium. This is Oracle-specific; the abstraction would need to be careful not to over-couple to Oracle.

---

### G1.12: `HOST` built-in (the form's host)

**Where:** New file under `Builtins/`.

**What:** Oracle Forms has a `HOST` built-in that returns information about the host environment (e.g. `HOST('OS')`, `HOST('Terminal')`). The engine has no host-info surface.

**Why it's a gap:** Forms that need to adapt to the host OS / terminal have to bypass the engine.

**Effort:** Small. Add a `IHostInfoProvider` interface and `IBeepBuiltins.GetHostInfo(key)`. The host provides the values.

**Risk:** Low.

---

### G1.13: `FTP` / `WEB` built-ins

**Where:** New file under `Builtins/`.

**What:** Oracle Forms has `FTP_*` and `WEB*` built-ins for FTP and HTTP operations. The engine has none of these.

**Why it's a gap:** Forms that need to call out to external services (e.g. fetch a customer photo from a URL) have to bypass the engine entirely.

**Effort:** Large. Requires a pluggable `INetworkClient` interface and careful sandboxing (don't want a malicious form to be able to make arbitrary HTTP calls).

**Risk:** High. Sandboxing and permission management are non-trivial.

---

### G1.14: `OLE2` / `OCX` / `ActiveX` (legacy)

**Where:** New file under `Builtins/`.

**What:** Oracle Forms had `OLE2.*` and `OCX.*` built-ins for COM/ActiveX integration. The engine has none of these.

**Why it's a gap:** Forms with COM components (very legacy) cannot be ported.

**Effort:** Large. COM/ActiveX is Windows-only and requires a runtime to host. Not recommended for new code; this is a "compatibility" gap, not a "parity" gap.

**Risk:** High. This is essentially a "do not implement" gap. Document it as out of scope.

---

## P2 — Nice-to-Have / Stretch

### G2.1: Built-in query construction language

**Where:** `Helpers/QueryBuilderManager.cs`.

**What:** The current `QueryBuilderManager` takes a `List<AppFilter>` and produces a SQL WHERE clause. Oracle Forms has a `WHERE` clause string (e.g. `WHERE CustomerId = :1 AND Country = 'USA'`). The engine could support parsing that string and converting to `AppFilter`s.

**Why it's a gap:** Forms with dynamic where-clauses (e.g. "find all customers where NAME LIKE '%ALF%'") currently have to construct the filter list manually.

**Effort:** Medium. Add a `WhereClauseParser` that takes the string and produces the filter list.

**Risk:** Low. Pure addition.

---

### G2.2: `RECORDGROUP` / `RECORDGROUP_FROM_QUERY`

**Where:** `Helpers/` (new file).

**What:** Oracle Forms has `RECORDGROUP` (a named in-memory record set) and `RECORDGROUP_FROM_QUERY` (creates one from a query). The engine has no record-group concept.

**Why it's a gap:** Forms that build in-memory data sets (e.g. for a "Find" dialog) have to bypass the engine.

**Effort:** Medium. Add `IRecordGroupRegistry` interface and `IBeepBuiltins` methods.

**Risk:** Low.

---

### G2.3: `CALENDAR` built-in

**Where:** `Builtins/` (new file).

**What:** Oracle Forms has a `CALENDAR` built-in that shows a calendar widget. The engine has no calendar surface.

**Why it's a gap:** Date-entry is a common need. Hosts currently implement the calendar entirely in the host layer.

**Effort:** Small. Add a `IBeepBuiltins.ShowCalendar` that routes through `IBuiltinHost.ShowCalendarAsync`.

**Risk:** Low.

---

### G2.4: `EDITOR` / `TEXT_IO` / `TEXT_EDITOR` built-ins

**Where:** `Builtins/` (new file).

**What:** Oracle Forms has `EDIT_TEXTITEM` (a multi-line text editor pop-up), `TEXT_IO` (text file I/O), `TEXT_EDITOR` (an external editor). The engine has none of these.

**Why it's a gap:** Forms with notes, comments, or large-text fields have to bypass the engine.

**Effort:** Medium. The text editor pop-up is host-routed (WinForms has a `RichTextBox`, Blazor has a `<textarea>`).

**Risk:** Low.

---

### G2.5: `VARR` (variable arrays) / `TABLE` (PL/SQL tables)

**Where:** `Helpers/` (new file).

**What:** Oracle Forms has `VARR` (a fixed-size array of values) and `TABLE` (a PL/SQL table type). The engine has no array concept at the FormsManager level (arrays exist on `IUnitofWork.Units` but are not exposed as built-ins).

**Why it's a gap:** Forms that pass arrays to PL/SQL (e.g. "delete these N records by ID") currently have to loop and call delete one at a time.

**Effort:** Medium. Requires an `IVarArray` interface and a wire format for passing arrays to the underlying datasource.

**Risk:** Medium. Datasource support for batch operations varies.

---

### G2.6: `DBMS_PIPE` / `DBMS_ALERT` (cross-session messaging)

**Where:** `Helpers/` (new file).

**What:** Oracle Forms has `DBMS_PIPE` (cross-session pipe-based messaging) and `DBMS_ALERT` (cross-session alert). The engine's message bus is per-instance; cross-session messaging requires a database.

**Why it's a gap:** Forms that coordinate across multiple sessions (e.g. a "user 1 updated a record, user 2 should refresh" workflow) have to implement this at the application layer.

**Effort:** Large. Requires database-side coordination and a polling mechanism.

**Risk:** Medium. Database-specific.

---

### G2.7: `ORACLE_HOME` / `FORMS_PATH` / `REPORT_PATH` (filesystem paths)

**Where:** `Configuration/` (new file).

**What:** Oracle Forms has `ORACLE_HOME`, `FORMS_PATH`, `REPORT_PATH` filesystem paths. The engine has no filesystem concept (it's UI-agnostic).

**Why it's a gap:** Forms that load .fmx files or report templates by path have to bypass the engine.

**Effort:** Small. Add a `IPathProvider` interface and `IBeepBuiltins.GetPath`. The host provides the actual paths.

**Risk:** Low.

---

### G2.8: `SET_APPLICATION_PROPERTY` for cursor / focus

**Where:** `Builtins/IBeepBuiltins.cs`, `IBuiltinHost`.

**What:** Oracle Forms has `SET_APPLICATION_PROPERTY('CURSOR_MODE', 'OPEN')` and similar. The engine has `SetApplicationProperty` (a generic property bag) but no specific cursor / focus presets.

**Why it's a gap:** Forms that need to control the cursor mode (open vs restricted) at runtime have to bypass the engine.

**Effort:** Small. Add specific property keys to the existing `IBeepBuiltins.SetApplicationProperty`.

**Risk:** Low.

---

## Gaps that are NOT problems

A few items that *look* like gaps but are actually intentional design decisions:

- **No `PL/SQL` engine** — Oracle Forms' PL/SQL is a runtime that the engine deliberately does not emulate. Forms with PL/SQL code have to be ported to C# (or to the engine's `TriggerManager` / `ValidationManager` surface).
- **No visual rendering** — Fonts, colors, layouts are host concerns. The engine stores them; the host renders.
- **No keyboard plumbing** — Tab order, accelerator keys (other than `KEY-` triggers) are host concerns. The engine has no idea what a Tab key is.
- **No data source abstraction** — The engine goes through `IUnitofWork` and `IDataSource`. The engine does not know or care whether the backing store is Oracle, SQL Server, Mongo, or a flat file.
- **No user-management system** — Authentication is the host's concern. The engine trusts the `SecurityContext` it receives.

These are **not gaps**. They are design decisions. Documenting them here so future maintainers don't try to "fix" them.

## See also

- [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md) — the concept-by-concept mapping (look up the ⚠️ and ❌ status here).
- [`enhancements.md`](enhancements.md) — improvement opportunities beyond just closing gaps.
- [`architecture.md`](architecture.md) — what the engine *is* and what it's *not*.
