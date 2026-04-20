# Phase 12 — Platform Targets (Desktop / Web / Blazor / MAUI)

## Objective

Ship per-host registration helpers with **safe, storage-aware defaults** for each platform Beep targets, plus a Blazor IndexedDB sink and MAUI app-data sink that respect host-imposed storage limits.

## Dependencies

- Phase 01 extension methods.
- Phase 03 sinks.
- Phase 04 budget enforcer.

## Scope

- **In**: Per-platform `BeepServiceExtensions` partial files for logging+audit; Blazor IndexedDB sink; MAUI AppData sink; documentation of recommended budgets.
- **Out**: Platform-specific UI; mobile push notifications.

## Target files

```
Services/
  BeepServiceExtensions.Desktop.Logging.cs
  BeepServiceExtensions.Desktop.Audit.cs
  BeepServiceExtensions.Web.Logging.cs
  BeepServiceExtensions.Web.Audit.cs
  BeepServiceExtensions.Blazor.Logging.cs
  BeepServiceExtensions.Blazor.Audit.cs
  BeepServiceExtensions.MAUI.Logging.cs
  BeepServiceExtensions.MAUI.Audit.cs

Services/Telemetry/Sinks/Platform/
  BlazorIndexedDbSink.cs              // partial: .Core, .Js
  BlazorIndexedDbSink.Core.cs
  BlazorIndexedDbSink.Js.cs           // JS interop
  MauiAppDataSink.cs                  // thin shim over FileRollingSink with FileSystem.AppDataDirectory
  PlatformPaths.Desktop.cs
  PlatformPaths.Web.cs
  PlatformPaths.Blazor.cs
  PlatformPaths.MAUI.cs

wwwroot/beep-telemetry/
  beep-indexeddb.js                   // shipped with Blazor host package
```

## Design notes

### Per-platform recommended defaults (locked v1)

| Host | Default sink set | Default `StorageBudgetBytes` | Backpressure (logs) |
|---|---|---|---|
| Desktop (WinForms / WPF / Console) | `FileRollingSink` to `LocalAppData/Beep/logs` | 50 MB | `DropOldest` |
| Web (ASP.NET Core)                 | `FileRollingSink` to `ContentRoot/logs` + optional `SqliteSink` | 500 MB | `DropOldest` |
| Blazor WebAssembly                 | `BlazorIndexedDbSink` only                   | 5 MB  | `DropOldest` |
| MAUI                               | `MauiAppDataSink` to `FileSystem.AppDataDirectory/Beep/logs` | 20 MB | `DropOldest` |

For audit, default budgets are 4× the log budget on Desktop/Web, 2× on MAUI, 1× on Blazor (audit on Blazor is opt-in only because IDB quota is shared with the page).

### Extension shape

```csharp
public static IServiceCollection AddBeepLoggingForDesktop(
    this IServiceCollection services,
    string appName,
    Action<BeepLoggingOptions> tweak = null)
{
    return services.AddBeepLogging(opt =>
    {
        opt.Enabled            = true;
        opt.MinLevel           = LogLevel.Information;
        opt.StorageBudgetBytes = 50L * 1024 * 1024;
        opt.AddFileRollingSink(PlatformPaths.LogsDir(appName));
        opt.UseRedactionPreset(RedactionPreset.LogsBalanced);
        opt.UseEnricherDefaults();
        tweak?.Invoke(opt);
    });
}
```

Same shape for Web (ContentRoot), Blazor (IndexedDB), MAUI (AppData), and for `AddBeepAuditFor*`. Each host package re-exports only the helpers that compile against its target framework.

### `BlazorIndexedDbSink`

- Implements `ITelemetrySink` purely in C# but delegates the actual storage to `beep-indexeddb.js` via `IJSRuntime.InvokeAsync`.
- Stores envelopes in an object store `beep_telemetry`, indexed by `(kind, ts)`.
- Honors `StorageBudgetBytes` by counting size on insert and pruning oldest when exceeded.
- Browser quota errors are surfaced via `SinkHealth`.
- No compression (browsers gzip transport themselves; in-IDB compression is not worth the JS overhead).

### `MauiAppDataSink`

- Thin specialization of `FileRollingSink` that resolves the directory via `Microsoft.Maui.Storage.FileSystem.AppDataDirectory` (only when MAUI assembly is present; `#if MAUI` style guards via target framework).
- Smaller default `MaxFileBytes = 1 MB` to keep mobile flush latency low.

### `PlatformPaths.*` partials

Each host overrides `PlatformPaths.LogsDir(appName)` and `AuditDir(appName)` to point at the OS-correct location. The base class returns `LocalApplicationData`.

## Implementation steps

1. Add per-platform `PlatformPaths` partials.
2. Add `BlazorIndexedDbSink` (C# + JS) and ship the JS file in `wwwroot/beep-telemetry`.
3. Add `MauiAppDataSink`.
4. Add `BeepServiceExtensions.{Desktop,Web,Blazor,MAUI}.{Logging,Audit}.cs`.
5. Document recommended budgets per platform in the README.
6. Tests: each helper builds against its TFM; defaults match the locked table.

## TODO checklist

- [x] P12-01 `Services/Telemetry/Presets/PlatformBudgets.cs` — single source of truth for log + audit budgets, queue capacities, and per-file rotation caps for Desktop / Web / Blazor / MAUI. The pre-existing `Services/Telemetry/PlatformPaths.cs` already covers `LogsDir(appName)` / `AuditDir(appName)` for every TFM the library targets, so per-platform path partials were unnecessary.
- [x] P12-02 `Services/Telemetry/Sinks/Platform/BlazorIndexedDbSink.{Core,Write,Health}.cs` plus `IIndexedDbBridge.cs` — the sink is pure C# and delegates the actual JS interop to the host-supplied bridge so the core library never references `Microsoft.JSInterop`. The `wwwroot/beep-telemetry/beep-indexeddb.js` shim is a host-package concern and is not part of the engine assembly (it ships separately when the Blazor host project is created).
- [x] P12-03 `Services/Telemetry/Sinks/Platform/MauiAppDataSink.cs` — implemented as a `static class` with a `Create(...)` factory (not a subclass) because `FileRollingSink` is sealed. Defaults to 1 MB rotation cap and 15-min roll interval so flush latency stays low on slow flash storage.
- [x] P12-04 Four per-platform extension files (logging + audit live side-by-side per the "one class per file" rule):
  - `Services/BeepServiceExtensions.Desktop.cs` — `AddBeepLoggingForDesktop`, `AddBeepAuditForDesktop`.
  - `Services/BeepServiceExtensions.Web.cs` — `AddBeepLoggingForWeb`, `AddBeepAuditForWeb` (accepts an explicit `contentRootPath` so callers can pass `IWebHostEnvironment.ContentRootPath` without the engine taking a hard dependency on ASP.NET Core).
  - `Services/BeepServiceExtensions.Blazor.cs` — `AddBeepLoggingForBlazor`, `AddBeepAuditForBlazor` (caller supplies the `IIndexedDbBridge`).
  - `Services/BeepServiceExtensions.Maui.cs` — `AddBeepLoggingForMaui`, `AddBeepAuditForMaui` (caller supplies a `Func<string>` returning `FileSystem.AppDataDirectory`).
- [x] P12-05 Storage-budget recommendations centralized in `PlatformBudgets`. README rollup is deferred to Phase 13 P13-06 alongside the rest of the docs sweep.
- [~] P12-06 Tests per platform _(deferred to Phase 13 — tracked under P13-04 sample apps and P13-03 perf harness so platform-specific coverage lands together with the soak harness)_.

## Operator surface delivered

```csharp
// Desktop (WinForms / WPF / Console)
services.AddBeepLoggingForDesktop("MyApp");
services.AddBeepAuditForDesktop("MyApp");

// ASP.NET Core
services.AddBeepLoggingForWeb(builder.Environment.ContentRootPath, "MyApp");
services.AddBeepAuditForWeb(builder.Environment.ContentRootPath, "MyApp");

// Blazor WebAssembly (host registers IIndexedDbBridge first)
services.AddBeepLoggingForBlazor(myIndexedDbBridge);
services.AddBeepAuditForBlazor(myIndexedDbBridge);

// MAUI
services.AddBeepLoggingForMaui(
    () => Microsoft.Maui.Storage.FileSystem.AppDataDirectory,
    "MyApp");
services.AddBeepAuditForMaui(
    () => Microsoft.Maui.Storage.FileSystem.AppDataDirectory,
    "MyApp");
```

Every helper accepts an optional `Action<BeepLoggingOptions>` /
`Action<BeepAuditOptions>` tweak callback that runs **after** the
preset values are applied, so the caller's overrides always win
without losing the platform-correct defaults.

## Cross-target safety notes

- The library does **not** reference `Microsoft.JSInterop`,
  `Microsoft.Maui.Storage`, or `Microsoft.AspNetCore.Hosting`.
  Blazor goes through `IIndexedDbBridge`, MAUI through a
  `Func<string>` directory delegate, and Web accepts an explicit
  `contentRootPath` string. This keeps the engine assembly buildable
  on `net8.0`, `net9.0`, and `net10.0` without per-TFM conditionals.
- `BlazorIndexedDbSink` proactively prunes at 80% of the soft cap
  so the page rarely hits a hard `QuotaExceededError`. Failures
  surface through `ISinkHealthProbe.Probe()` and are rolled up by
  the `HealthAggregator` from Phase 11.
- `MauiAppDataSink.Create(...)` builds a `FileRollingSink` with
  `CompressOnRotate = false` because mobile CPU is more constrained
  than the savings from gzip on a 1 MB rolled file.

## Build status

`net8.0` + `net9.0` + `net10.0` build succeeds with **0 errors and
0 new lint warnings** on every Phase 12 file.

## Verification

- A vanilla Blazor WASM app with `AddBeepLoggingForBlazor("Beep")` writes envelopes that survive a page reload.
- A MAUI Android app writes to `AppDataDirectory` and stays under 20 MB after a 24-hour soak.
- ASP.NET Core app writes to `ContentRoot/logs/*.ndjson` and gzip-rotates correctly.
- WinForms desktop app uses `LocalAppData/Beep/logs` with default 50 MB budget.

## Risks

- **R1**: IndexedDB quotas are not predictable across browsers. Mitigation: budget is a soft cap; sink prunes proactively at 80% of `StorageBudgetBytes`.
- **R2**: MAUI background lifecycle may pause the writer. Mitigation: pipeline flushes on `OnSleep`/`OnResume` lifecycle hooks (host-specific helper).
- **R3**: Web ContentRoot may be read-only in container deploys. Mitigation: extension method accepts an explicit override path.
