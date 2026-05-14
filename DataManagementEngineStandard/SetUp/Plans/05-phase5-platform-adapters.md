# Phase 5 — Platform Adapters

## Objective

Implement `ISetupWizardAdapter` for every supported platform, bridging each platform's DI/progress/navigation conventions to the platform-neutral `ISetupWizard` contract built in Phases 1–4.

One wizard, many surfaces. The adapter isolates all platform-specific code so the wizard core never imports WinForms, Blazor, MAUI, or System.CommandLine namespaces.

---

## Supported Platforms

| Platform | Adapter Class | Service Lifetime | Progress Surface | DI Method |
|---|---|---|---|---|
| Desktop WinForms / WPF | `DesktopSetupWizardAdapter` | Singleton | `IProgress<PassedArgs>` → callback | `AddBeepForDesktop()` |
| Console / CLI (BeepShell) | `ConsoleSetupWizardAdapter` | Singleton | `AnsiConsole` table / progress bar | `ShellServiceProvider` |
| ASP.NET Core Web API | `WebApiSetupWizardAdapter` | Scoped | Background task + `/setup/status` endpoint | `AddBeepForWeb()` |
| Blazor Server | `BlazorServerSetupWizardAdapter` | Scoped | SignalR `IHubContext` push | `AddBeepForBlazorServer()` |
| Blazor WASM | `BlazorWasmSetupWizardAdapter` | Singleton | `IJSRuntime` localStorage state | `AddBeepForBlazorWasm()` |
| MAUI | `MauiSetupWizardAdapter` | Singleton | `IProgress<PassedArgs>` → `MainThread.InvokeOnMainThreadAsync` | `AddBeepForMaui()` |

---

## Core Adapter Contract

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public interface ISetupWizardAdapter
    {
        /// <summary>Run the entire wizard and surface progress/errors to the platform UI.</summary>
        Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default);

        /// <summary>Show a single step's UI representation (name, description).</summary>
        void ShowStep(ISetupStep step, int stepIndex, int totalSteps);

        /// <summary>Update the progress indicator for the current step.</summary>
        void ShowProgress(string stepId, int percentComplete, string message);

        /// <summary>Display the final setup result to the user.</summary>
        void ShowResult(SetupReport report);
    }
}
```

---

## Per-Platform Specifications

---

### 1. `DesktopSetupWizardAdapter` (WinForms / WPF)

**Pattern**: Modal progress dialog → `IProgress<PassedArgs>` → `Action<PassedArgs>` callback  
**DI**: `AddBeepForDesktop()` → Singleton `IBeepService`

```csharp
namespace TheTechIdea.Beep.SetUp.Adapters
{
    /// <summary>
    /// Bridges the setup wizard to a WinForms/WPF progress surface.
    /// The caller provides an Action&lt;PassedArgs&gt; callback that updates the
    /// application's wait/progress form.
    /// </summary>
    public class DesktopSetupWizardAdapter : ISetupWizardAdapter
    {
        private readonly Action<PassedArgs> _progressCallback;
        private readonly Action<SetupReport> _completedCallback;

        public DesktopSetupWizardAdapter(
            Action<PassedArgs> progressCallback,
            Action<SetupReport> completedCallback = null)
        {
            _progressCallback = progressCallback;
            _completedCallback = completedCallback;
        }

        public async Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            var progress = new Progress<PassedArgs>(args => _progressCallback?.Invoke(args));
            await Task.Run(() => wizard.Run(context, progress), cancellationToken);
            var report = wizard.GetReport();
            _completedCallback?.Invoke(report);
            return report;
        }

        public void ShowStep(ISetupStep step, int stepIndex, int totalSteps) =>
            _progressCallback?.Invoke(new PassedArgs
            {
                Messege = $"Step {stepIndex + 1}/{totalSteps}: {step.StepName}",
                ParameterInt1 = (int)(stepIndex * 100.0 / totalSteps)
            });

        public void ShowProgress(string stepId, int percentComplete, string message) =>
            _progressCallback?.Invoke(new PassedArgs
            {
                Messege = message,
                ParameterInt1 = percentComplete
            });

        public void ShowResult(SetupReport report) =>
            _completedCallback?.Invoke(report);
    }
}
```

**Usage in WinForms Program.cs**:
```csharp
// 1. Register BeepDM as Singleton
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddBeepForDesktop(opts =>
{
    opts.AppRepoName = "MyApp";
    opts.DirectoryPath = AppContext.BaseDirectory;
});
var host = builder.Build();
var beepService = host.Services.GetRequiredService<IBeepService>();

// 2. Build wizard
var wizard = new SetupWizardBuilder()
    .WithId("myapp-setup")
    .AddStep(new ConnectionConfigStep(new ConnectionConfigStepOptions
    {
        ConnectionName = "AppDb",
        ConnectionProperties = AppDbContext.CreateConnectionProps(beepService.DMEEditor)
    }))
    .AddStep(new SchemaSetupStep(new SchemaSetupStepOptions
    {
        EntityTypes = AppEntityTypes.All
    }))
    .AddStep(new SeedingStep(new SeedingStepOptions { Registry = AppSeeders.Registry }))
    .Build();

// 3. Run via adapter
var adapter = new DesktopSetupWizardAdapter(
    progressCallback: args => waitForm.UpdateProgress(args.ParameterInt1, args.Messege),
    completedCallback: report => waitForm.Close());

var context = new SetupContext { Editor = beepService.DMEEditor };
var report = await adapter.RunAsync(wizard, context);
```

---

### 2. `ConsoleSetupWizardAdapter` (CLI / BeepShell)

**Pattern**: `AnsiConsole` progress columns + table output for report  
**DI**: `ShellServiceProvider` singleton `IDMEEditor`

```csharp
namespace TheTechIdea.Beep.SetUp.Adapters
{
    public class ConsoleSetupWizardAdapter : ISetupWizardAdapter
    {
        public async Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            SetupReport report = null;

            await AnsiConsole.Progress()
                .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(),
                         new PercentageColumn(), new ElapsedTimeColumn())
                .StartAsync(async ctx =>
                {
                    var tasks = wizard.Steps.ToDictionary(
                        s => s.StepId,
                        s => ctx.AddTask($"[cyan]{s.StepName}[/]", maxValue: 100));

                    var progress = new Progress<PassedArgs>(args =>
                    {
                        // Find the active task by checking SetupState
                        var activeStep = wizard.Steps.FirstOrDefault(
                            s => !context.State.IsStepCompleted(s.StepId));
                        if (activeStep != null && tasks.TryGetValue(activeStep.StepId, out var task))
                            task.Value = args.ParameterInt1;
                    });

                    await Task.Run(() => wizard.Run(context, progress), cancellationToken);
                    report = wizard.GetReport();
                });

            ShowResult(report);
            return report;
        }

        public void ShowStep(ISetupStep step, int stepIndex, int totalSteps) =>
            AnsiConsole.MarkupLine($"[bold cyan]► Step {stepIndex + 1}/{totalSteps}:[/] {step.StepName}");

        public void ShowProgress(string stepId, int percentComplete, string message) =>
            AnsiConsole.MarkupLine($"  [grey]{percentComplete}%[/] {message}");

        public void ShowResult(SetupReport report)
        {
            var table = new Table()
                .AddColumn("Step").AddColumn("Result").AddColumn("Message").AddColumn("Elapsed");
            foreach (var r in report.StepResults)
            {
                table.AddRow(
                    r.StepName,
                    r.Skipped ? "[grey]Skipped[/]" :
                        (r.Succeeded ? "[green]✓ OK[/]" : "[red]✗ FAIL[/]"),
                    r.Message ?? string.Empty,
                    r.Elapsed.ToString(@"mm\:ss\.fff"));
            }
            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine(report.Succeeded
                ? $"[bold green]Setup complete![/] Hash: [grey]{report.ContentHash[..12]}[/]"
                : $"[bold red]Setup failed.[/]");
        }
    }
}
```

---

### 3. `WebApiSetupWizardAdapter` (ASP.NET Core)

**Pattern**: Kick off background task via `IHostedService`; poll `/api/setup/status`  
**DI**: `AddBeepForWeb()` → Scoped `IBeepService`

```csharp
namespace TheTechIdea.Beep.SetUp.Adapters
{
    /// <summary>
    /// Runs the setup wizard on a background thread and exposes status via
    /// an in-memory state object that the /api/setup/status endpoint polls.
    /// </summary>
    public class WebApiSetupWizardAdapter : ISetupWizardAdapter
    {
        public SetupAdapterStatus Status { get; } = new SetupAdapterStatus();

        public async Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            Status.State = "Running";
            var progress = new Progress<PassedArgs>(args =>
            {
                Status.CurrentMessage = args.Messege;
                Status.PercentComplete = args.ParameterInt1;
            });

            try
            {
                await Task.Run(() => wizard.Run(context, progress), cancellationToken);
                var report = wizard.GetReport();
                Status.State = report.Succeeded ? "Completed" : "Failed";
                Status.Report = report;
                return report;
            }
            catch (OperationCanceledException)
            {
                Status.State = "Cancelled";
                throw;
            }
        }

        public void ShowStep(ISetupStep step, int stepIndex, int totalSteps)
        {
            Status.CurrentStepName = step.StepName;
            Status.CurrentStepIndex = stepIndex;
            Status.TotalSteps = totalSteps;
        }
        public void ShowProgress(string stepId, int pct, string msg)
        {
            Status.PercentComplete = pct;
            Status.CurrentMessage = msg;
        }
        public void ShowResult(SetupReport report) => Status.Report = report;
    }

    public class SetupAdapterStatus
    {
        public string State { get; set; } = "Idle";
        public string CurrentStepName { get; set; }
        public int CurrentStepIndex { get; set; }
        public int TotalSteps { get; set; }
        public int PercentComplete { get; set; }
        public string CurrentMessage { get; set; }
        public SetupReport Report { get; set; }
    }
}
```

**Minimal API endpoint**:
```csharp
// Program.cs
app.MapPost("/api/setup/run", async (ISetupWizardFactory factory, IBeepService beep) =>
{
    var (wizard, context) = factory.CreateDefault(beep.DMEEditor);
    var adapter = new WebApiSetupWizardAdapter();
    _ = adapter.RunAsync(wizard, context); // fire-and-forget, poll status
    return Results.Accepted("/api/setup/status");
});

app.MapGet("/api/setup/status", (WebApiSetupWizardAdapter adapter) =>
    Results.Ok(adapter.Status));
```

---

### 4. `BlazorServerSetupWizardAdapter`

**Pattern**: SignalR hub push → Blazor component state update  
**DI**: `AddBeepForBlazorServer()` → Scoped `IBeepService`

```csharp
namespace TheTechIdea.Beep.SetUp.Adapters
{
    public class BlazorServerSetupWizardAdapter : ISetupWizardAdapter
    {
        private readonly IHubContext<SetupProgressHub> _hub;

        public BlazorServerSetupWizardAdapter(IHubContext<SetupProgressHub> hub)
        {
            _hub = hub;
        }

        public async Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            var progress = new Progress<PassedArgs>(async args =>
            {
                await _hub.Clients.All.SendAsync("SetupProgress",
                    new { pct = args.ParameterInt1, msg = args.Messege },
                    cancellationToken);
            });

            await Task.Run(() => wizard.Run(context, progress), cancellationToken);
            var report = wizard.GetReport();
            await _hub.Clients.All.SendAsync("SetupComplete",
                new { succeeded = report.Succeeded, hash = report.ContentHash },
                cancellationToken);
            return report;
        }

        public void ShowStep(ISetupStep step, int i, int t) { }
        public void ShowProgress(string id, int pct, string msg) { }
        public void ShowResult(SetupReport r) { }
    }
}
```

**Blazor component scaffold**:
```razor
@* Pages/Setup.razor *@
@inject ISetupWizardFactory WizardFactory
@inject IBeepService Beep
@inject IHubContext<SetupProgressHub> Hub

<MudButton OnClick="RunSetup">Run Setup</MudButton>
<MudProgressLinear Value="@_pct" />
<MudText>@_msg</MudText>

@code {
    int _pct; string _msg;

    async Task RunSetup()
    {
        var (wizard, context) = WizardFactory.CreateDefault(Beep.DMEEditor);
        var adapter = new BlazorServerSetupWizardAdapter(Hub);
        var report = await adapter.RunAsync(wizard, context);
        _msg = report.Succeeded ? "Setup complete!" : "Setup failed.";
    }
}
```

---

### 5. `BlazorWasmSetupWizardAdapter`

**Pattern**: Run locally in WASM; persist `SetupState` to `localStorage` via `IJSRuntime`  
**DI**: `AddBeepForBlazorWasm()` → Singleton

```csharp
namespace TheTechIdea.Beep.SetUp.Adapters
{
    public class BlazorWasmSetupWizardAdapter : ISetupWizardAdapter
    {
        private readonly IJSRuntime _js;

        public BlazorWasmSetupWizardAdapter(IJSRuntime js) => _js = js;

        public async Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            // Load persisted state
            var stateJson = await _js.InvokeAsync<string>(
                "localStorage.getItem", "beep-setup-state");
            if (!string.IsNullOrEmpty(stateJson))
            {
                // Deserialize and inject into wizard for resume
                var savedState = System.Text.Json.JsonSerializer
                    .Deserialize<SetupState>(stateJson);
                context.State = savedState;
            }

            var progress = new Progress<PassedArgs>(args =>
            {
                // In WASM, progress updates are in-process; update component state directly
            });

            wizard.Run(context, progress);
            var report = wizard.GetReport();

            // Persist state to localStorage
            if (context.State != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(context.State);
                await _js.InvokeVoidAsync("localStorage.setItem", "beep-setup-state", json);
            }

            return report;
        }

        public void ShowStep(ISetupStep step, int i, int t) { }
        public void ShowProgress(string id, int pct, string msg) { }
        public void ShowResult(SetupReport r) { }
    }
}
```

---

### 6. `MauiSetupWizardAdapter`

**Pattern**: `IProgress<PassedArgs>` → `MainThread.InvokeOnMainThreadAsync` → ViewModel  
**DI**: `AddBeepForMaui()` → Singleton

```csharp
namespace TheTechIdea.Beep.SetUp.Adapters
{
    public class MauiSetupWizardAdapter : ISetupWizardAdapter
    {
        private readonly Action<int, string> _progressAction; // (percent, message)
        private readonly Action<SetupReport> _completedAction;

        public MauiSetupWizardAdapter(
            Action<int, string> progressAction,
            Action<SetupReport> completedAction = null)
        {
            _progressAction = progressAction;
            _completedAction = completedAction;
        }

        public async Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            var progress = new Progress<PassedArgs>(args =>
            {
                MainThread.InvokeOnMainThread(() =>
                    _progressAction?.Invoke(args.ParameterInt1, args.Messege));
            });

            await Task.Run(() => wizard.Run(context, progress), cancellationToken);
            var report = wizard.GetReport();

            await MainThread.InvokeOnMainThreadAsync(() =>
                _completedAction?.Invoke(report));

            return report;
        }

        public void ShowStep(ISetupStep step, int i, int t) =>
            MainThread.InvokeOnMainThread(() =>
                _progressAction?.Invoke((int)(i * 100.0 / t), step.StepName));
        public void ShowProgress(string id, int pct, string msg) =>
            MainThread.InvokeOnMainThread(() => _progressAction?.Invoke(pct, msg));
        public void ShowResult(SetupReport r) =>
            MainThread.InvokeOnMainThread(() => _completedAction?.Invoke(r));
    }
}
```

---

## `ISetupWizardFactory` Helper

To avoid duplicating wizard construction per platform, introduce a factory:

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public interface ISetupWizardFactory
    {
        (ISetupWizard wizard, SetupContext context) CreateDefault(IDMEEditor editor);
        (ISetupWizard wizard, SetupContext context) Create(
            IDMEEditor editor, SetupOptions options, Action<SetupWizardBuilder> configure);
    }
}
```

---

## File Layout

```
DataManagementEngineStandard/
  SetUp/
    ISetupWizardAdapter.cs
    ISetupWizardFactory.cs
    Adapters/
      DesktopSetupWizardAdapter.cs
      ConsoleSetupWizardAdapter.cs
      WebApiSetupWizardAdapter.cs
      BlazorServerSetupWizardAdapter.cs
      BlazorWasmSetupWizardAdapter.cs
      MauiSetupWizardAdapter.cs
      SetupAdapterStatus.cs
```

> **Note**: Blazor and MAUI adapters import platform-specific namespaces (`Microsoft.AspNetCore.SignalR`, `Microsoft.JSInterop`, `Microsoft.Maui.ApplicationModel`). These files must live in the appropriate platform project (or be conditionally compiled) if `DataManagementEngineStandard` does not reference those packages.
> Preferred pattern: move adapter files to the platform project and have them depend on `DataManagementEngineStandard`; only the contracts (`ISetupWizardAdapter`, `ISetupWizardFactory`) live in `DataManagementEngineStandard`.

---

## DI Registration Patterns

### Desktop
```csharp
builder.Services.AddBeepForDesktop(opts => { ... });
builder.Services.AddSingleton<ISetupWizardFactory, DefaultSetupWizardFactory>();
// Adapter is created manually at startup (not usually DI-registered)
```

### Web API
```csharp
builder.Services.AddBeepForWeb(opts => { ... });
builder.Services.AddSingleton<WebApiSetupWizardAdapter>();
builder.Services.AddSingleton<ISetupWizardFactory, DefaultSetupWizardFactory>();
```

### Blazor Server
```csharp
builder.Services.AddBeepForBlazorServer(opts => { ... });
builder.Services.AddScoped<BlazorServerSetupWizardAdapter>();
builder.Services.AddScoped<ISetupWizardFactory, DefaultSetupWizardFactory>();
```

### Blazor WASM
```csharp
builder.Services.AddBeepForBlazorWasm(opts => { ... });
builder.Services.AddSingleton<BlazorWasmSetupWizardAdapter>();
```

### MAUI
```csharp
builder.Services.AddBeepForMaui(opts => { ... });
builder.Services.AddSingleton<ISetupWizardFactory, DefaultSetupWizardFactory>();
// Adapter created inline using ViewModel callbacks
```

---

## Testing Approach

| Test | Description |
|---|---|
| `DesktopAdapter_RunAsync_CallsProgressCallback` | Verify callback fires with correct percent |
| `ConsoleAdapter_ShowResult_FormatsTable` | Table rows match step results |
| `WebApiAdapter_RunAsync_UpdatesStatus` | Status.State transitions from Running → Completed |
| `WebApiAdapter_Cancelled_StateIsCancelled` | CancellationToken cancels → State=Cancelled |
| `BlazorWasmAdapter_PersistsState_ToLocalStorage` | JS interop called with serialized state JSON |
| `MauiAdapter_ProgressOnMainThread` | Verify MainThread.InvokeOnMainThread is called |

---

## Acceptance Criteria

- [ ] `ISetupWizardAdapter` and `ISetupWizardFactory` exist in `SetUp/`.
- [ ] All 6 adapter classes implement `ISetupWizardAdapter`.
- [ ] `DesktopSetupWizardAdapter` invokes the progress callback on every `PassedArgs` event.
- [ ] `ConsoleSetupWizardAdapter.ShowResult` outputs a table with per-step status.
- [ ] `WebApiSetupWizardAdapter.Status` reflects Running → Completed/Failed transitions.
- [ ] `BlazorWasmSetupWizardAdapter` loads and saves `SetupState` to `localStorage`.
- [ ] `MauiSetupWizardAdapter` dispatches all progress calls to the main thread.
- [ ] Adapters compile without referencing the wizard core's internal implementation details.
