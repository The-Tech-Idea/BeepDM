# Phase 3 — Pluggable State Store & Concurrency

**Goal:** Make setup state substitutable — local JSON for solo, shared/remote for enterprise — and
make concurrent runs safe. **This is where the solo/enterprise split becomes real.**

**Pre-condition:** Phase 2 (a definition is data, so state can key against its `ContentHash`).

**Files touched:** `DataManagementModelsStandard/SetUp/`, `DataManagementEngineStandard/SetUp/`

---

## ✅ Status: complete

All items P3-01..06 landed (per-item summary in the master tracker). 174/174 tests green;
`StateStoreTests.cs` (9) + `WizardStatePersistenceTests.cs` (4) cover it.

Notes for later phases:

- **The async store meets a sync wizard via `Task.Run(f).GetAwaiter().GetResult()`** (the
  `DriverProvisionStep` NuGet-load precedent). `SetupWizard.Run` is still synchronous; P8's "real
  cancellation" is where the wizard's async model gets revisited, and the store is already async-ready
  for it.
- **`RemoteSetupStateStore` is tested against an in-memory `ISetupStateTransport`, not HTTP.** The
  concurrency/lease logic — the hard, correctness-critical part — is fully covered. An
  `HttpSetupStateTransport` was intentionally *not* written: shipping untested HTTP would repeat the
  Studio mistake. It's a thin, obvious adapter to add when a concrete backend contract exists.
- **The schema-version gate lives in the wizard (`AcceptStateVersion`), not the stores**, so both
  stores are consistent and a future v1→v2 migration has one home.
- **`SetupCheckpointStore` was deleted, not deprecated.** It was `internal` with no external
  consumers, and leaving it would mean two persistence paths — the confusion this phase set out to
  remove.

---

## What was wrong before this phase

`SetupCheckpointStore` is `internal static` and **not behind an interface** — it cannot be
substituted without editing the class. Beyond that:

| Problem | Evidence |
|---|---|
| Checkpointing silently off | `StateFilePath == null` → `LoadPersistedState`/`PersistState` both no-op |
| Lock is in-process only | per-path `ConcurrentDictionary<string, object>`; two processes on a shared path interleave |
| `RunId` detection unimplemented | doc says "detect stale/concurrent checkpoints"; **nothing compares RunIds** — `LoadPersistedState` unconditionally overwrites `state.RunId` |
| Wizards collide | `wizardId` is not part of the state path; two wizards sharing `StateFilePath` silently corrupt each other |
| Each run destroys history | `File.Move(overwrite: true)` — no prior-run record (P6 depends on fixing this) |

---

## 3-A  `ISetupStateStore`

**New:** `DataManagementModelsStandard/SetUp/State/ISetupStateStore.cs`

```csharp
public interface ISetupStateStore
{
    Task<SetupState> LoadAsync(SetupStateKey key, CancellationToken token = default);
    Task SaveAsync(SetupStateKey key, SetupState state, CancellationToken token = default);

    /// <summary>Acquire an exclusive lease. Returns null if another runner holds it.</summary>
    Task<ISetupStateLease> TryAcquireLeaseAsync(SetupStateKey key, TimeSpan ttl, CancellationToken token = default);
}

public interface ISetupStateLease : IAsyncDisposable
{
    string RunId { get; }
    DateTimeOffset ExpiresAt { get; }
    Task<bool> RenewAsync(CancellationToken token = default);
}

/// <summary>Identity of a state record. wizardId fixes the collision; appId is P7's hook.</summary>
public sealed record SetupStateKey(string WizardId, string Environment, string AppId = null);
```

`SetupStateKey` carrying `AppId` now means P7 doesn't need to reshape the store.

## 3-B  `LocalJsonSetupStateStore` — solo default

Port `SetupCheckpointStore`'s atomic write (temp + `File.Move(overwrite: true)`, retries,
`FileShare.ReadWrite | Delete`) behind the interface. Path derives from `SetupStateKey`, **not** a
raw `StateFilePath`:

```
{ConfigEditor.ConfigPath}/setup/{appId|_}/{environment}/{wizardId}.state.json
```

Lease = a sibling `.lock` file holding `RunId` + expiry, honoured cross-process via
`FileShare.None` + TTL. Keep `SetupOptions.StateFilePath` working (`[Obsolete]`) — shipped contract.

## 3-C  `RemoteSetupStateStore` — enterprise

HTTP-backed, optimistic concurrency via ETag/`If-Match` → `SetupStateConflictException` on mismatch.
Server-side lease with TTL + renewal. Retry with backoff on transient failures; a lease that can't
renew **fails the run** rather than continuing unguarded.

No specific backend is mandated here — the interface is the contract. Ship the HTTP one; teams can
implement `ISetupStateStore` against their own store.

## 3-D  Implement the lease

Wire `RunId` to mean what its doc claims:

- `SetupWizard.Run` acquires a lease before step 1; releases in `finally`.
- Loading a state whose `RunId` differs **and** whose lease is unexpired → refuse to run
  ("another runner holds this setup"), don't silently take over.
- An expired lease is reclaimable — that's the crash-recovery path.
- `LoadPersistedState` must stop unconditionally overwriting `state.RunId`.

## 3-E  DI

```csharp
services.AddBeepSetup();                                  // → LocalJsonSetupStateStore
services.AddBeepSetup().AsEnterprise(o => o.UseRemoteState(uri));  // → RemoteSetupStateStore
```

`SetupWizard` depends on `ISetupStateStore`, never on a concrete store.

## 3-F  Tests

| Test | Guards |
|---|---|
| `LocalStore_RoundTrips_State` | 3-B |
| `TwoRunners_SameKey_SecondIsRefused` | 3-D |
| `ExpiredLease_IsReclaimable` | 3-D |
| `TwoWizards_DifferentIds_DoNotCollide` | 3-A key |
| `RemoteStore_Conflict_Throws_SetupStateConflict` | 3-C |
| `NullStateFilePath_LogsWarning_DoesNotSilentlyNoOp` | P1-08 regression |

## Files summary

| Action | File | Est. |
|---|---|---|
| New | `Models/SetUp/State/ISetupStateStore.cs` | ~40 |
| New | `Engine/SetUp/State/LocalJsonSetupStateStore.cs` | ~180 |
| New | `Engine/SetUp/State/RemoteSetupStateStore.cs` | ~200 |
| Modify | `Engine/SetUp/SetupCheckpointStore.cs` → delegate to store, `[Obsolete]` | ~30 |
| Modify | `Engine/SetUp/SetupWizard.cs` (lease lifecycle) | ~40 |
| Modify | `Engine/SetUp/SetupWizardServiceExtensions.cs` | ~40 |
| New | `tests/SetupWizardTests/StateStoreTests.cs` | ~220 |
