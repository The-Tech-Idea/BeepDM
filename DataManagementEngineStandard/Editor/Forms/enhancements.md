# FormsManager — Enhancements

This document lists improvement opportunities that go beyond the gap items in [`gaps.md`](gaps.md). Where `gaps.md` covers "what the engine does not yet do," this doc covers "what the engine could do better."

Items are grouped by area, with a rough priority indicator (high / medium / low). **High** = clear value with low risk. **Medium** = clear value with moderate risk. **Low** = speculative or small payoff.

---

## Performance

### E.P1: Cache hit-rate tuning

**Where:** `Helpers/PerformanceManager.cs`.

**What:** The current cache uses a simple LRU with no per-block-size weighting. A block with 10,000 records and a block with 10 records are evicted by the same policy.

**Suggestion:** Weight eviction by `blockSize × accessCount` rather than just `accessCount`. Add a periodic `GetCacheStats()` call from a background timer in the host to inform tuning.

**Effort:** Small. **Risk:** Low.

---

### E.P2: Async pagination (load-page-async)

**Where:** `FormsManager.Performance.cs`.

**What:** `LoadPageAsync` is currently synchronous (the `await` is for the signature, not for offloading). For large pages, the page-load blocks the UI thread.

**Suggestion:** Add a `Task.Run` around the actual page load so the UI thread is free. Coordinate with the host's sync context for the result delivery.

**Effort:** Small. **Risk:** Low (the UoW is already thread-safe for individual block reads).

---

### E.P3: Connection pooling at the FormsManager level

**Where:** `FormsManager.Core.cs` (constructor), `Helpers/` (new file).

**What:** Currently each `IUnitofWork` opens its own datasource connection. When a form has 10 blocks, that's 10 connections.

**Suggestion:** Add a `IDataSourceConnectionPool` that the engine shares across blocks in the same form. Borrow a connection per block operation, return on commit/rollback.

**Effort:** Medium. **Risk:** Medium (transaction semantics on shared connections are tricky).

---

## Developer experience

### E.D1: Strongly-typed block names

**Where:** `FormsManager` (all partials).

**What:** Block names are currently strings. A typo (`"CUSTOMRES"` instead of `"CUSTOMERS"`) compiles fine and fails at runtime.

**Suggestion:** Add an `IBlockName` interface with named constants:

```csharp
public static class BlockNames
{
    public static readonly IBlockName Customers = BlockName.Of("CUSTOMERS");
    public static readonly IBlockName Orders = BlockName.Of("ORDERS");
}
```

Then `manager.GetBlock(BlockNames.Customers)` instead of `manager.GetBlock("CUSTOMERS")`. Compile-time check for typos.

**Effort:** Medium. **Risk:** Low (additive, can be adopted incrementally).

---

### E.D2: Fluent configuration

**Where:** `Configuration/UnitofWorksManagerConfiguration.cs`.

**What:** Currently `ConfigureAudit(Action<AuditConfiguration>)` is a callback. A fluent API would be cleaner.

**Suggestion:**

```csharp
manager.ConfigureAudit()
    .Enable()
    .For("ORDERS", "ORDER_ITEMS")
    .WithRetention(90)
    .WithStore(AuditStoreKind.File, @"C:\Logs\audit.log")
    .Apply();
```

**Effort:** Small. **Risk:** Low.

---

### E.D3: Compile-time validation of trigger names

**Where:** `Helpers/TriggerManager.cs`, `Models/TriggerEnums.cs`.

**What:** Trigger names are strings (`"WHEN-NEW-RECORD-INSTANCE"`). A typo compiles fine.

**Suggestion:** Add a `TriggerNames` static class with constants. Update `TriggerManager.RegisterTrigger` to accept `TriggerName` (a strongly-typed wrapper).

**Effort:** Small. **Risk:** Low.

---

### E.D4: Better test coverage for orchestration edge cases

**Where:** New tests in `tests/FormsManager.Tests/` (a project that does not yet exist — see E.T1).

**What:** Current tests cover the happy paths. The orchestrator has many edge cases that aren't tested:

- Trigger chain with circular dependencies.
- Master/detail with a detail that has its own detail (multi-level).
- Mode transition cancellation (OnPreQuery cancels → block stays in current mode).
- Concurrent register/unregister of the same block name.
- Form lifecycle with no `IUnitofWork` (defensive check).
- Multi-form call stack with `ReturnToCaller` from a deeply-nested call.

**Suggestion:** Add an integration test project (`tests/FormsManager.IntegrationTests/`) with these scenarios.

**Effort:** Large. **Risk:** Low.

---

### E.D5: Diagnostic mode for FormsManager

**Where:** `FormsManager.Core.cs` (constructor option), `Helpers/`.

**What:** A `FormsManager(options => { options.DiagnosticMode = DiagnosticMode.Verbose; })` that logs every state transition to a `IDiagnosticSink`. Useful for debugging "why didn't this trigger fire?"

**Suggestion:** Add a `DiagnosticLevel` enum and a `IDiagnosticSink` interface. Default sink is a no-op; verbose sink writes to `Debug.WriteLine`.

**Effort:** Small. **Risk:** Low.

---

### E.D6: Add property name validation in the BlockDefinition loading path (DONE 2026-06, partial)

**Where:** `Helpers/RecordPropertyAccessor.cs` (new), `FormsManager.BlockRegistration.cs:70-94` (the `ItemChanged` handler).

**What:** The `BlockDefinition.FieldName` strings are user-supplied and have no compile-time check. A misspelled `FieldName` (e.g. `Orderid` vs `OrderId`) used to silently no-op. The new `RecordPropertyAccessor` surfaces the miss as a throttled log line on first encounter per (record type, field name), but the misconfiguration is still active in production.

**Suggestion (next pass):** Add a `BlockDefinition.Validate(recordSample)` step at registration time that walks every `FieldName` and emits a single batched error if any field is missing. Block registration could optionally fail-fast (gated by a `ValidationMode.Strict` option) for hosts that want loud failure during development, and soft (current behavior) for production.

**Effort:** Small. **Risk:** Low (additive).

---

### E.D7: Cache hit-rate telemetry on `RecordPropertyAccessor` (open)

**Where:** `Helpers/RecordPropertyAccessor.cs`.

**What:** The current accessor emits throttled warnings on miss but does not record hit/miss counts. For a long-running form it would be useful to surface the hit rate (e.g. "in the last hour: 14,892 hits, 7 misses across 4 record types") so a host can decide whether the consolidation is delivering the expected win, or whether a particular record type has a property-name configuration problem that's worth fixing.

**Suggestion:** Add three `Interlocked` counters — `HitCount`, `MissCount`, `TypeCount` — and a `GetStats()` method that returns a snapshot. Expose the stats on `IDMEEditor` as `IFormsStatsAccessor` so hosts can subscribe. Default behavior: no observable change. Hosts that opt in get the telemetry.

**Effort:** Small. **Risk:** Low (counters are read-only, no behavior change).

---

## Architecture

### E.A1: Async-everywhere IUnitofWork

**Where:** `Editor/UOW/` (outside the Forms folder; affects `IUnitofWork` interface).

**What:** The current `IUnitofWork` is mostly sync (with async variants in places). The engine's `await` in `FormsManager.Navigation.cs` is essentially a no-op for sync UoWs.

**Suggestion:** Define a fully async `IUnitofWorkAsync` with `Task<...>` everywhere. Migrate `FormsManager` to use it. The existing `IUnitofWork` can be the sync facade.

**Effort:** Large. **Risk:** High (cross-cutting).

---

### E.A2: Pluggable concurrency model

**Where:** `FormsManager.Core.cs`.

**What:** The current concurrency model assumes the host UI is single-threaded (WinForms message loop). It uses a single `_lockObject` for state transitions.

**Suggestion:** Make the concurrency model pluggable:

```csharp
public enum ConcurrencyMode
{
    UiThreadOnly,        // current behavior; assumes WinForms message loop
    ThreadSafe,          // every operation lock-free; multi-thread callers
    Serialized,          // every operation serialized through a semaphore
    Custom               // IConcurrencyModel implementation
}
```

**Effort:** Medium. **Risk:** Medium.

---

### E.A3: Distributed FormsManager (multi-instance coordination)

**Where:** New project (outside the Forms folder).

**What:** A single-process `FormsManager` is great. A *distributed* `FormsManager` (where multiple `FormsManager` instances coordinate via a database or message queue) would enable load-balanced form servers.

**Suggestion:** Add a `IDistributedFormsManager` interface and a database-backed implementation. The current `IFormMessageBus` becomes the routing layer.

**Effort:** Very large. **Risk:** High. Not for this year.

---

## Documentation

### E.DOC1: Migrate the obsolete `EXECUTIVE_SUMMARY.md`

**Where:** `EXECUTIVE_SUMMARY.md` (existing file in this folder).

**What:** The current `EXECUTIVE_SUMMARY.md` is from an earlier state of the engine (it mentions "Update operations rely on reflection" and "No LOV implementation exists," both of which are now false). The README's "Stale / superseded documents" section already flags it.

**Suggestion:** Either:
1. **Delete it** — it's misleading.
2. **Replace it with a pointer** to the current `architecture.md` + `ORACLE-FORMS-MAPPING.md`.
3. **Update it** — if the executive-summary format is valuable, rewrite it with current content.

I recommend option 1 (delete). The README already covers the same ground better.

**Effort:** Trivial. **Risk:** None.

---

### E.DOC2: Delete `FormsManager.original.cs.bak`

**Where:** `FormsManager.original.cs.bak` (existing backup file).

**What:** A leftover backup from before the partial-class refactor. Everything has been moved to typed partials.

**Suggestion:** Delete it. The file is unused (it's `.bak`, not compiled). The README's "Stale / superseded" section already flags it.

**Effort:** Trivial. **Risk:** None.

---

### E.DOC3: Add a CHANGELOG.md to the Forms folder

**Where:** New file at this folder root.

**What:** A changelog of the engine's evolution. Each release should note:
- New public APIs.
- New built-ins.
- New events.
- Behavior changes (e.g. "AbortOnStepFailure now defaults to true").
- Bug fixes.

**Suggestion:** Start a changelog file, dated to today. Add to it on each significant change.

**Effort:** Trivial. **Risk:** None.

---

## Test infrastructure

### E.T1: Create a test project for FormsManager

**Where:** `tests/FormsManager.Tests/` (new project).

**What:** The README mentions several test suites (FormsManager.Core.Tests, FormsManager.Navigation.Tests, etc.) but these test projects don't exist in the repo (verified — `tests/IntegrationTests/` is the only test directory and it's empty).

**Suggestion:** Create the test projects. They were mentioned in the README as if they exist; if they don't, that's a documentation-vs-code drift.

**Effort:** Large. **Risk:** None.

---

### E.T2: Add a fixture-based integration test for multi-form

**Where:** `tests/FormsManager.Tests/Integration/MultiForm/`.

**What:** The current tests (where they exist) cover single-form. Multi-form is harder to test (it requires a real host or a no-op host implementation).

**Suggestion:** Add a `NoOpBuiltinHost` test fixture that records calls without rendering. Use it to drive `CallFormAsync` → `ReturnToCallerAsync` sequences and verify the call stack.

**Effort:** Medium. **Risk:** None.

---

### E.T3: Add property-based tests for trigger order

**Where:** `tests/FormsManager.Tests/Triggers/`.

**What:** The trigger dependency manager is hard to test because the failure modes (cycles, missing dependencies) are edge cases.

**Suggestion:** Add a property-based test that generates random trigger graphs and verifies the engine never deadlocks, never throws on a valid graph, and produces a consistent ordering.

**Effort:** Medium. **Risk:** None.

---

## API surface

### E.API1: Expose `IFormRegistry` and friends as the public surface for the registry

**Where:** New `Forms/IFormRegistry.cs` and similar in `Interfaces/`.

**What:** Currently the orchestrator exposes `manager.Registry`, `manager.MessageBus`, `manager.SharedBlocks` — but these are helper properties, not separate types in the interface file.

**Suggestion:** Pull these into the `IUnitofWorksManagerInterfaces.cs` interface declarations so they appear in the public API docs (not just as helper properties).

**Effort:** Small. **Risk:** Low.

---

### E.API2: Expose `IBlockFactory` as a public factory

**Where:** `Interfaces/IUnitofWorksManagerInterfaces.cs`.

**What:** `IBlockFactory` is currently internal to the orchestrator. Hosts that want to construct blocks manually (e.g. for testing) have to go through the orchestrator.

**Suggestion:** Add `IBlockFactory` to the public interface. It already exists; just expose it.

**Effort:** Trivial. **Risk:** Low.

---

### E.API3: Add a `FormsManagerBuilder` for fluent construction

**Where:** New file in `Editor/Forms/`.

**What:** The current constructor is 28 lines with 24 optional parameters. Constructing it with non-default helpers is verbose.

**Suggestion:**

```csharp
var manager = FormsManagerBuilder.New(dmeEditor)
    .WithValidation(new CustomValidationManager())
    .WithAlertProvider(new CustomAlertProvider())
    .WithSequenceProvider(new DatabaseSequenceProvider(connection))
    .Build();
```

**Effort:** Small. **Risk:** Low.

---

## Documentation of design decisions

### E.DD1: Document the "why" of partial-class split

**Where:** `architecture.md` (already exists) — add a section.

**What:** The 28 `FormsManager*.cs` partials are split by concern, but the reasoning for that split is implicit. A new maintainer would benefit from a paragraph on "why partials, why this split."

**Suggestion:** Add a section to `architecture.md` explaining:
- Why partial classes (vs. composition).
- Why 28 partials (vs. 1 big class or 5 medium classes).
- How to add a new partial (the conventions).
- The naming convention (partial name = the concern).

**Effort:** Trivial. **Risk:** None.

---

### E.DD2: Document the helper-DI pattern

**Where:** `architecture.md` (already exists) — add a section.

**What:** The 24-helper-DI pattern is unusual. Most .NET classes have a handful of dependencies; `FormsManager` has 24. A new maintainer would benefit from "why so many? how do I know which helper to use for X?"

**Suggestion:** Add a section explaining:
- The "one helper per concern" philosophy.
- Why DI rather than internal state (testability, host-overridability).
- How to override a helper in the constructor.
- The trade-off (verbose constructor vs. testability).

**Effort:** Trivial. **Risk:** None.

---

## See also

- [`gaps.md`](gaps.md) — what the engine does not yet do.
- [`architecture.md`](architecture.md) — what the engine *is*.
- [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md) — the full concept mapping.
- [`functional-matrix.md`](functional-matrix.md) — every public type and capability.
