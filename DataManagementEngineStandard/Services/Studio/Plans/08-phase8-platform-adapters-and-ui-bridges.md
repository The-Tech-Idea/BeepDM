# Phase 09 — Platform Adapters & UI Bridges

> **Scope:** build the **per-platform adapters** that let any host
> (Blazor Server, Blazor Wasm, WinForms, WPF, Maui, Console, WebApi) consume
> the Studio. Each adapter is a small, focused class that:
>
> - owns the `IStudioService` instance (singleton per process),
> - bridges the engine's `IProgress<PassedArgs>` to the host UI's progress primitive,
> - registers a host-appropriate `IStudioHostAdapter` for the engine to call into,
> - wires the `SyncRunnerHostedService` (and any other background services)
>   to the host's `IHost` lifetime.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

The Studio's public surface is platform-agnostic — interfaces and POCOs only.
The **adapters** are where the platform-specific code lives:

- A Blazor Server adapter wires `IStudioProgress` to SignalR.
- A Blazor Wasm adapter wires it to an HTTP API + WebSocket.
- A WinForms adapter wires it to `BackgroundWorker` + `IProgress<int>`.
- A WPF adapter wires it to `Dispatcher` + `IProgress<T>`.
- A Maui adapter wires it to `MainThread.BeginInvokeOnMainThread`.
- A Console adapter wires it to `Spectre.Console.Progress`.
- A WebApi adapter wires it to `IHubContext<>` for push progress.

Every adapter is **< 200 lines** and does the same thing: own the
`IStudioService`, hand out a progress bridge, and register a background
service if the host supports it.

The host project (Blazor, WinForms, etc.) references the engine
(`DataManagementEngineStandard`) and exactly one adapter.

## Folder layout (this phase creates)

```
Services/Studio/Adapters/
├── IStudioHostAdapter.cs                              ← Phase 1 stub
├── StudioHostContext.cs                               ← POCO with IStudioService + progress + ct
├── StudioProgressBridge.cs                            ← IStudioProgress ↔ IProgress<PassedArgs>
├── BlazorServerStudioAdapter.cs
├── BlazorWasmStudioAdapter.cs
├── WinFormsStudioAdapter.cs
├── WpfStudioAdapter.cs
├── MauiStudioAdapter.cs
├── ConsoleStudioAdapter.cs
├── WebApiStudioAdapter.cs
├── BeepServiceExtensions.StudioAdapters.cs            ← AddBeepBlazorStudio(), AddBeepWinFormsStudio(), ...
└── Tests will land in the host test project, not the engine
```

## Public surface

```csharp
// Adapters/IStudioHostAdapter.cs
public interface IStudioHostAdapter
{
    string HostName { get; }                           // "BlazorServer" | "WinForms" | ...
    IStudioService Studio { get; }
    IStudioProgress CreateProgress(string operationId, string operationName);

    // Host-specific hooks (optional in the base interface; each adapter adds more)
    Task OnStartupAsync(CancellationToken ct);
    Task OnShutdownAsync(CancellationToken ct);
}

// Adapters/StudioHostContext.cs
public sealed record StudioHostContext(
    IStudioService Studio,
    string HostName,
    Action<IStudioProgress>? ProgressFactory = null,    // optional: host can wrap the adapter's progress
    CancellationToken ShutdownToken = default);
```

## Progress bridge

`StudioProgressBridge` is the central adapter between the engine's
`IProgress<PassedArgs>` (defined in `DataManagementEngineStandard`) and
the Studio's `IStudioProgress`:

```csharp
public static class StudioProgressBridge
{
    /// <summary>Wraps a host-supplied IStudioProgress as an IProgress<PassedArgs>.</summary>
    public static IProgress<PassedArgs> ToEngineProgress(IStudioProgress studio);

    /// <summary>Wraps an engine-supplied IProgress<PassedArgs> as an IStudioProgress.</summary>
    public static IStudioProgress ToStudioProgress(IProgress<PassedArgs> engine, string operationId, string operationName);

    /// <summary>Bridges both directions at once — useful for run + observe patterns.</summary>
    public static (IProgress<PassedArgs> Engine, IStudioProgress Studio) Pair(string operationId, string operationName);
}
```

The bridge ensures that the engine's existing `IMigrationManager.ExecuteMigrationPlan(...)`
+ `BeepSyncManager.SyncDataAsync(...)` callers can stream their native progress events
into the host UI without any change on the engine side.

## Per-platform adapters

### `BlazorServerStudioAdapter`

Wires:
- `IStudioService` as a **singleton** (one per process).
- `IStudioProgress` to a `Channel<StudioProgressUpdate>` that the
  `SyncProgressHub` SignalR hub drains and pushes to the browser.
- `SyncRunnerHostedService` as a `IHostedService` (Blazor Server has
  `IHost`).
- `EfCoreEntityAdapter` (optional) loaded via reflection when the host
  references the adapter assembly.

```csharp
public sealed class BlazorServerStudioAdapter : IStudioHostAdapter
{
    public string HostName => "BlazorServer";
    public IStudioService Studio { get; }
    public IStudioProgress CreateProgress(string opId, string opName) =>
        new SignalRStudioProgress(opId, opName, _hubContext);
}
```

The Blazor host's `Program.cs` calls:

```csharp
builder.Services.AddBeepStudio(opts => { opts.DataRoot = ...; });
builder.Services.AddBeepBlazorStudio();
```

### `BlazorWasmStudioAdapter`

Same shape, but:
- `IStudioService` runs in the **browser** (compiled to WASM).
- The actual long-running operations call a remote API
  (`BeepDMS.Api` in the Blazor workspace).
- The progress bridge uses `Channel<StudioProgressUpdate>` over a
  WebSocket that the API serves.

The Wasm adapter is a **thin client** — the engine logic still runs
in the API process, but the host UI is in the browser. The
`IStudioService` interface is identical; the implementation is a
typed HTTP client that calls the API.

### `WinFormsStudioAdapter`

Wires:
- `IStudioService` as a **singleton** (one per `Application`).
- `IStudioProgress` to `IProgress<int>` via `SynchronizationContext.Post`
  so the UI thread marshals the updates.
- `SyncRunnerHostedService` runs in a `Task.Run` started by the
  adapter on `OnStartupAsync` (WinForms doesn't have `IHost`).

The WinForms host's `Program.cs` calls:

```csharp
ApplicationConfiguration.Initialize();
var adapter = new WinFormsStudioAdapter();
await adapter.OnStartupAsync(default);
Application.Run(new MainForm(adapter));
```

### `WpfStudioAdapter`

Same shape as WinForms but:
- `SynchronizationContext` is WPF's `DispatcherSynchronizationContext`.
- The adapter exposes an `IStudioProgress` that the WPF `Window`
  binds to via the `Dispatcher`.

### `MauiStudioAdapter`

Wires:
- `IStudioService` as a **singleton** in the MAUI app's `MauiProgram`.
- `IStudioProgress` to `MainThread.BeginInvokeOnMainThread` so updates
  reach the UI thread.
- `SyncRunnerHostedService` as a hosted service (MAUI has `IHost`).

### `ConsoleStudioAdapter`

Wires:
- `IStudioService` as a **singleton** in the console host.
- `IStudioProgress` to `Spectre.Console.Progress` for a rich TUI.
- `SyncRunnerHostedService` as a hosted service (Console has `IHost`).
- The console's `IConfiguration` is loaded from `appsettings.json`
  + env vars + command-line args.

### `WebApiStudioAdapter`

Wires:
- `IStudioService` as a **singleton** in the API host.
- `IStudioProgress` to `IHubContext<StudioProgressHub>` so any
  connected client (browser, CLI, another API) can subscribe.
- `SyncRunnerHostedService` as a hosted service (WebApi has `IHost`).
- Exposes the engine's full surface as JSON endpoints + SignalR
  push (the API project in the Blazor workspace uses this adapter).

## DI extensions

```csharp
// BeepServiceExtensions.StudioAdapters.cs
public static IServiceCollection AddBeepBlazorStudio(this IServiceCollection services)
{
    services.AddSingleton<IStudioHostAdapter, BlazorServerStudioAdapter>();
    services.AddSingleton<SyncProgressHub>();           // SignalR hub
    services.AddHostedService(p => (SyncRunnerHostedService)p.GetRequiredService<IStudioService>().Sync.RunnerService);
    return services;
}

public static IServiceCollection AddBeepBlazorWasmStudio(this IServiceCollection services) { ... }
public static IServiceCollection AddBeepWinFormsStudio(this IServiceCollection services) { ... }
public static IServiceCollection AddBeepWpfStudio(this IServiceCollection services) { ... }
public static IServiceCollection AddBeepMauiStudio(this IServiceCollection services) { ... }
public static IServiceCollection AddBeepConsoleStudio(this IServiceCollection services) { ... }
public static IServiceCollection AddBeepWebApiStudio(this IServiceCollection services) { ... }
```

## Cross-cutting

- **Every adapter** registers `IStudioHostAdapter` and uses `IStudioService`
  from the engine. The host UI never references the engine types directly
  (no `DMEEditor`, no `IBeepAudit`).
- **No adapter** takes a dependency on a UI toolkit it doesn't need.
  The Blazor adapter references `Microsoft.AspNetCore.SignalR`; the
  WinForms adapter references `System.Windows.Forms`; etc. Each
  adapter is in the **engine** project but the **references are
  guarded** by the host project's `.csproj` (a host that doesn't
  reference the adapter assembly simply doesn't compile the adapter
  in — but for v1 we ship them all and let the linker trim).
- **Progress bridging** is one-way in this phase: the engine emits
  `IProgress<PassedArgs>`, the host UI receives `IStudioProgress`.
  The reverse direction (host → engine) is added in a future phase
  if needed.

---

## Todo Tracker

| # | Task | Status | Notes |
|---|------|--------|-------|
| P09-01 | `Adapters/IStudioHostAdapter.cs` (full interface, not the Phase 1 stub) | ⬜ | |
| P09-02 | `Adapters/StudioHostContext.cs` | ⬜ | |
| P09-03 | `Adapters/StudioProgressBridge.cs` — `IStudioProgress` ↔ `IProgress<PassedArgs>` | ⬜ | |
| P09-04 | `Adapters/BlazorServerStudioAdapter.cs` + `Adapters/BlazorWasmStudioAdapter.cs` | ⬜ | |
| P09-05 | `Adapters/WinFormsStudioAdapter.cs` + `Adapters/WpfStudioAdapter.cs` | ⬜ | |
| P09-06 | `Adapters/MauiStudioAdapter.cs` + `Adapters/ConsoleStudioAdapter.cs` + `Adapters/WebApiStudioAdapter.cs` | ⬜ | |
| P09-07 | `BeepServiceExtensions.StudioAdapters.cs` — `AddBeepBlazorStudio`, `AddBeepWinFormsStudio`, … | ⬜ | |
| P09-08 | Wire the engine's `csproj` to conditionally include the per-platform adapters (e.g. `<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-windows'">`) | ⬜ | |
| P09-09 | Tests: one per adapter (UI-thread marshalling is the main risk) — land in the host test project | ⬜ | |
| P09-10 | Update the Blazor host's `.plans/phase-18.md` … `phase-24.md` to point at the Studio + adapters (the host becomes a thin shell) | ⬜ | |
| P09-11 | Document: how to add a new host adapter (e.g. Avalonia, Uno) | ⬜ | |
| P09-12 | Update `00-overview-and-scope.md` + `MASTER-TODO-TRACKER.md` to mark Phase 09 done | ⬜ | |

---

## Validation (definition of done)

- [ ] `dotnet build DataManagementEngineStandard` succeeds with **0 errors** for the default target.
- [ ] `dotnet build -f net10.0-windows` succeeds when the WinForms + WPF adapters are included.
- [ ] `dotnet build -f net10.0` succeeds for the Blazor / Console / WebApi adapters.
- [ ] `BlazorServerStudioAdapter.CreateProgress` returns an `IStudioProgress` that, when `Report(...)` is called, fires a SignalR event to a connected test client.
- [ ] `WinFormsStudioAdapter.OnStartupAsync` starts the `SyncRunnerHostedService` on a `Task.Run` and the progress reports reach the UI thread without an exception.
- [ ] `StudioProgressBridge.Pair(...)` round-trips a `PassedArgs` event into a `StudioProgressUpdate` without loss of fields.
- [ ] All adapter tests pass.

---

## Pitfalls

1. **Don't put UI toolkit code in the engine core** — the engine's main `csproj` must stay UI-toolkit-free. The per-platform adapter files can reference `System.Windows.Forms`, `Microsoft.Maui.*`, etc., but the main `StudioService.cs` and sub-service implementations must not.
2. **Don't bypass `SyncRunnerHostedService`** — every adapter must register it as a hosted service (or wrap it in `Task.Run` for hosts that lack `IHost`). The host UI is not the right thread to run a long sync on.
3. **Don't leak the engine's `IProgress<PassedArgs>` to the host UI** — always go through `IStudioProgress`. The host UI should never know the engine has its own progress type.
4. **Don't run multiple `IStudioService` instances per process** — it's a singleton. The Blazor host especially must not accidentally create one per circuit.
5. **Don't block the host UI thread on a Studio call** — every method on `IStudioService` is async and returns `StudioResult<T>`. The adapter does not add sync wrappers.
6. **Don't add new platform-specific code paths to the engine after this phase** — if a new platform is needed (Avalonia, Uno), add a new adapter under `Adapters/`, not new code paths in the existing services.

---

## Related

- Phase 01 — contracts (every adapter consumes `IStudioService`)
- Phases 2-8 — sub-services the adapters expose
- `C:\Users\f_ald\source\repos\The-Tech-Idea\BeepWeb\.plans\phase-18.md` … `phase-24.md` — the Blazor host's plan, which will be updated to point at the Studio + the `BlazorServerStudioAdapter` (Phase 9 is the integration point that makes the Blazor RCL a thin shell)
- `BeepDM/DataManagementEngineStandard/SetUp/Adapters/` — the engine's existing per-platform setup wizard adapters (we follow the same pattern for the Studio)
