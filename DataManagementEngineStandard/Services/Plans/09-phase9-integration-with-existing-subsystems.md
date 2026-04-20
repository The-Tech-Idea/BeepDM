# Phase 09 — Integration With Existing Subsystems

## Objective

Connect every existing log/audit emitter in the codebase to the new pipeline through **bridges**, so the rollout adds capability without breaking any caller. Legacy `IDMLogger`, `Microsoft.Extensions.Logging`, `AuditManager`, `IProxyAuditSink`, and (when it lands) `IDistributedAuditSink` all flow into `IBeepLog` / `IBeepAudit`.

## Dependencies

- Phase 02 pipeline.
- Phase 08 `AuditEvent` schema.

## Scope

- **In**: Bridge classes, registration helpers, compatibility shims.
- **Out**: Replacing existing APIs (we adapt; we do not remove).

## Target files

```
Services/Logging/Bridges/
  DMLoggerToBeepLogBridge.cs
  MicrosoftLoggerProvider.cs
  MicrosoftLoggerScope.cs

Services/Audit/Bridges/
  FormsAuditBridge.cs
  ProxyAuditBridge.cs
  DistributedAuditBridge.cs          // wired only when distributed plan phase 13 lands
  AuditBridgeRegistry.cs
```

## Design notes

### `DMLoggerToBeepLogBridge`

Wraps an `IBeepLog` and exposes the `IDMLogger` API. Forwards each call as:

```csharp
public void WriteLog(string info)        => _log.Info(info);
public void LogError(string err)         => _log.Error(err);
public void LogWarning(string w)         => _log.Warn(w);
// ...
public void LogStructured(string m, object props)
    => _log.Log(LogLevel.Information, "DMLogger", m,
                AsDictionary(props));
```

Registration: when `AddBeepLogging` is called with `Enabled = true`, also replace the DI registration of `IDMLogger` with the bridge. When disabled, keep the existing `DMLogger`. The bridge's `Onevent`/`PropertyChanged` events relay through if the underlying `BeepLog` raises them (optional; default off).

### `MicrosoftLoggerProvider`

Implements `Microsoft.Extensions.Logging.ILoggerProvider` so any framework that already uses MEL (ASP.NET Core, EF, gRPC, hosted services) automatically writes into the Beep pipeline:

```csharp
services.AddBeepLogging(opt => { opt.Enabled = true; ... });
services.AddSingleton<ILoggerProvider, MicrosoftLoggerProvider>();
```

`MicrosoftLoggerScope` returns a no-op `IDisposable` for `BeginScope` and stores scope state in `BeepActivityScope` (Phase 06).

### `FormsAuditBridge`

`Editor/Forms/Helpers/AuditManager` already records field-level changes and flushes on commit. The bridge listens for `Store.Save(AuditEntry)` (we will introduce a small `SaveAndForward` extension on `IAuditStore`) and converts each `AuditEntry` to an `AuditEvent` of category `DataAccess`:

| `AuditEntry` field | `AuditEvent` field |
|---|---|
| `FormName`            | `Properties["formName"]` |
| `BlockName`           | `Source = $"Forms.Block.{BlockName}"` |
| `RecordKey`           | `RecordKey` |
| `Operation`           | `Operation` |
| `UserName`            | `UserName` |
| `Timestamp`           | `TimestampUtc` |
| `FieldChanges`        | `FieldChanges` |

Forms can opt out via `BeepAuditOptions.BridgeForms = false`.

### `ProxyAuditBridge`

`Proxy/IProxyAuditSink` already records cluster events. The bridge implements `IProxyAuditSink`, forwards to `IBeepAudit` with `Category = AuditCategory.Custom` (or `Distributed` if cluster-tier), `Source = "Proxy.{ClusterName}"`. Existing direct sinks (e.g. `FileProxyAuditSink`) keep working in parallel; operator can disable them once they trust the bridge.

### `DistributedAuditBridge`

Activated when `IDistributedAuditSink` from `DistributedDatasource/DistributedPlans/13-...md` is implemented. Maps shard events, transaction-coordinator events, and resharding events to `Category = AuditCategory.Distributed`.

### `AuditBridgeRegistry`

Centralizes bridge wiring so apps don't have to register each one separately:

```csharp
services.AddBeepAudit(opt =>
{
    opt.Enabled       = true;
    opt.BridgeForms   = true;     // default
    opt.BridgeProxy   = true;     // default
    opt.BridgeDistributed = false;// off until phase exists
});
```

### Compatibility shim

When the unified features are **off**, every bridge is replaced with a `NullBridge` that calls into the legacy stores directly. Result: zero-impact baseline.

## Implementation steps

1. Add `DMLoggerToBeepLogBridge` and `MicrosoftLoggerProvider`/`MicrosoftLoggerScope`.
2. Add `IAuditStore.SaveAndForward` extension and the `FormsAuditBridge`.
3. Add `ProxyAuditBridge` (no changes to `IProxyAuditSink`).
4. Add `DistributedAuditBridge` skeleton with TODO link to distributed phase 13.
5. Add `AuditBridgeRegistry` and wire it from `BeepServiceExtensions.Audit`.
6. Update `AddBeepLogging` to optionally replace `IDMLogger` registration with the bridge.
7. Tests: existing FormsManager + proxy paths still produce their original audit entries *and* now also produce `IBeepAudit` events when bridges are on.

## TODO checklist

- [ ] P09-01 `DMLoggerToBeepLogBridge.cs`.
- [ ] P09-02 `MicrosoftLoggerProvider.cs` + `MicrosoftLoggerScope.cs`.
- [ ] P09-03 `FormsAuditBridge.cs` (+ `IAuditStore.SaveAndForward` extension).
- [ ] P09-04 `ProxyAuditBridge.cs`.
- [ ] P09-05 `DistributedAuditBridge.cs` skeleton.
- [ ] P09-06 `AuditBridgeRegistry.cs` + extension wiring.
- [ ] P09-07 Tests for forms / proxy / MEL forwarding.

## Verification

- Existing apps that never enable the new features behave identically (CI test compares behavior with/without `AddBeepLogging`).
- When enabled, an ASP.NET Core warning log lands in the file rolling sink with the right category.
- A `FormsManager.Commit()` produces both legacy `AuditEntry` records and modern `AuditEvent` records.

## Risks

- **R1**: Double-writes (legacy store + new pipeline) cost extra IO. Mitigation: operator can flip off the legacy sink once the bridge is trusted.
- **R2**: Bridge ordering (which fires first). Mitigation: bridges always run synchronously after the legacy store call, never before; failure of the new pipeline cannot break the legacy flow.
