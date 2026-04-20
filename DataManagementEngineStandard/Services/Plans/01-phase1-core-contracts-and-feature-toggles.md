# Phase 01 — Core Contracts & Feature Toggles

## Objective

Define the public surface for both features (`IBeepLog`, `IBeepAudit`), their options, and the **opt-in registration extensions** that wire them into `BeepService` without breaking any existing consumer.

## Dependencies

- Phase 00 scope is locked.
- Existing types: `IBeepService` (`Services/IBeepService.cs`), `IDMLogger` (`DataManagementModelsStandard/Logger/IDMLogger.cs`).

## Scope

- **In**: New interfaces, options classes, registration helpers, no-op implementations.
- **Out**: Pipeline / sinks / redaction (Phases 02–07).

## Target files

```
Services/Logging/
  IBeepLog.cs
  BeepLoggingOptions.cs
  NullBeepLog.cs

Services/Audit/
  IBeepAudit.cs
  BeepAuditOptions.cs
  NullBeepAudit.cs

Services/Telemetry/
  TelemetryFeature.cs            // shared feature flags + lifecycle helpers

Services/
  BeepServiceExtensions.Logging.cs
  BeepServiceExtensions.Audit.cs
```

## Design notes

### `IBeepLog`

```csharp
public interface IBeepLog
{
    bool   IsEnabled { get; }
    LogLevel MinLevel { get; }

    void Log(LogLevel level, string category, string message,
             IReadOnlyDictionary<string, object> properties = null,
             Exception exception = null);

    void Trace(string message, object properties = null);
    void Debug(string message, object properties = null);
    void Info (string message, object properties = null);
    void Warn (string message, object properties = null);
    void Error(string message, Exception ex = null, object properties = null);
    void Critical(string message, Exception ex = null, object properties = null);

    Task FlushAsync(CancellationToken ct = default);
}
```

### `IBeepAudit`

```csharp
public interface IBeepAudit
{
    bool IsEnabled { get; }

    Task RecordAsync(AuditEvent evt, CancellationToken ct = default);

    Task<IReadOnlyList<AuditEvent>> QueryAsync(
        AuditQuery filter, CancellationToken ct = default);

    Task PurgeByUserAsync(string userId, CancellationToken ct = default);
    Task PurgeByEntityAsync(string blockOrEntity, string recordKey,
                            CancellationToken ct = default);

    Task<bool> VerifyIntegrityAsync(CancellationToken ct = default);
    Task FlushAsync(CancellationToken ct = default);
}
```

> Schema for `AuditEvent`/`AuditQuery` is finalized in Phase 08, but Phase 01 declares forward stubs so the interface compiles.

### Options shape

```csharp
public sealed class BeepLoggingOptions
{
    public bool      Enabled              { get; set; } = false;
    public LogLevel  MinLevel             { get; set; } = LogLevel.Information;
    public int       QueueCapacity        { get; set; } = 10_000;
    public BackpressureMode BackpressureMode { get; set; } = BackpressureMode.DropOldest;
    public TimeSpan  FlushInterval        { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan  ShutdownTimeout      { get; set; } = TimeSpan.FromSeconds(5);
    public long      StorageBudgetBytes   { get; set; } = 50L * 1024 * 1024;

    public IList<ITelemetrySink> Sinks       { get; } = new List<ITelemetrySink>();
    public IList<IRedactor>      Redactors   { get; } = new List<IRedactor>();
    public IList<IEnricher>      Enrichers   { get; } = new List<IEnricher>();
    public IList<ISampler>       Samplers    { get; } = new List<ISampler>();
}
```

`BeepAuditOptions` mirrors the shape but defaults `BackpressureMode = Block`, `Samplers` is empty (audit is never sampled), and adds `bool HashChain { get; set; } = true;` and `int RetentionDays { get; set; } = 365;`.

### Registration extensions

```csharp
public static IServiceCollection AddBeepLogging(
    this IServiceCollection services,
    Action<BeepLoggingOptions> configure)
{
    var opt = new BeepLoggingOptions();
    configure?.Invoke(opt);
    services.AddSingleton(opt);
    services.AddSingleton<IBeepLog>(sp =>
        opt.Enabled ? new BeepLog(opt /* + pipeline, Phase 02 */)
                    : new NullBeepLog());
    return services;
}

public static IServiceCollection AddBeepAudit(
    this IServiceCollection services,
    Action<BeepAuditOptions> configure) { /* mirror */ }
```

### `BeepService` integration

- Add nullable `IBeepLog BeepLog { get; }` and `IBeepAudit BeepAudit { get; }` to `IBeepService`.
- `BeepService.Stop()` calls `BeepLog?.FlushAsync(timeout)` then `BeepAudit?.FlushAsync(timeout)` before tearing down.
- When the feature is disabled, the `Null*` impls satisfy DI and add zero overhead.

## Implementation steps

1. Create the three new folders.
2. Add `IBeepLog`, `BeepLoggingOptions`, `NullBeepLog` (one file per class).
3. Add `IBeepAudit`, `BeepAuditOptions`, `NullBeepAudit` (forward-declare `AuditEvent`, `AuditQuery` as empty placeholders to be filled in Phase 08).
4. Add `TelemetryFeature.cs` containing `BackpressureMode` enum and a tiny `FeatureLifetime` helper struct used by both extensions.
5. Add `BeepServiceExtensions.Logging.cs` and `BeepServiceExtensions.Audit.cs`.
6. Extend `IBeepService` and `BeepService.Stop()` to call flush on both (no-op if Null).
7. Confirm no existing call sites break: search the solution for `IBeepService` consumers and verify additive-only changes.

## TODO checklist

- [ ] P01-01 Create `Services/Logging/`, `Services/Audit/`, `Services/Telemetry/` folders.
- [ ] P01-02 `IBeepLog.cs`.
- [ ] P01-03 `IBeepAudit.cs` (with placeholder `AuditEvent`/`AuditQuery`).
- [ ] P01-04 `BeepLoggingOptions.cs` + `BeepAuditOptions.cs`.
- [ ] P01-05 `BeepServiceExtensions.Logging.cs` (`AddBeepLogging`).
- [ ] P01-06 `BeepServiceExtensions.Audit.cs` (`AddBeepAudit`).
- [ ] P01-07 Wire opt-in into `BeepService.Stop()` flush sequence.
- [ ] P01-08 `NullBeepLog`, `NullBeepAudit`.

## Verification

- Solution builds clean with feature **off**.
- `services.AddBeepLogging(o => o.Enabled = false)` → resolves `NullBeepLog` (verified in unit test).
- `services.AddBeepLogging(o => o.Enabled = true)` → resolves a stub `BeepLog` that compiles (full impl in Phase 02).
- `IBeepService.BeepLog` and `IBeepService.BeepAudit` are non-null after `BeepService.Start()` regardless of opt-in (always at least the `Null*` impl).

## Risks

- **R1**: Bare `BeepLog` stub would be incomplete in Phase 01. Mitigation: Phase 01 ships only the `Null*` real impls; the production `BeepLog` is added with the pipeline in Phase 02 (and `AddBeepLogging` returns `NullBeepLog` until then if `Enabled=true` — log a one-time warning).
- **R2**: Forward-declared `AuditEvent` shape might conflict with Phase 08. Mitigation: keep it as `public partial class AuditEvent { }` so Phase 08 just adds members.
