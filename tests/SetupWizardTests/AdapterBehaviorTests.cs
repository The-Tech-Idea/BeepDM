using TheTechIdea.Beep.SetUp.Adapters;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Guards for Phase 1 P1-10 (.plans/setup/PHASE-01-Stabilize-Correctness.md).
///
/// These were written BEFORE the SetupWizardAdapterBase refactor so they pin the behavior the
/// refactor must preserve — notably Maui's awaited main-thread completion and WebApi's status
/// state machine, both of which a naive base class silently breaks.
/// </summary>
public class AdapterBehaviorTests
{
    // ── stub wizard ──────────────────────────────────────────────────────────

    private sealed class StubWizard : ISetupWizard
    {
        private readonly Exception _throwOnRun;
        private readonly SetupReport _report;

        public StubWizard(Exception throwOnRun = null, SetupReport report = null, bool nullReport = false)
        {
            _throwOnRun = throwOnRun;
            _report = nullReport
                ? null
                : report ?? new SetupReport
                {
                    Succeeded = true,
                    WizardId = "stub",
                    // ConsoleSetupWizardAdapter.ShowResult enumerates StepResults and slices
                    // ContentHash — a real report from BuildReport always carries both.
                    StepResults = Array.Empty<SetupStepResult>(),
                    ContentHash = new string('A', 64)
                };
        }

        public IReadOnlyList<ISetupStep> Steps { get; init; } = Array.Empty<ISetupStep>();
        public SetupState State { get; } = new SetupState();
        public SetupOptions Options { get; } = new SetupOptions();

        public IErrorsInfo Run(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            progress?.Report(new PassedArgs { ParameterInt1 = 50, Messege = "working" });
            if (_throwOnRun != null) throw _throwOnRun;
            return new ErrorsInfo { Flag = Errors.Ok };
        }

        public IErrorsInfo Resume(SetupContext context, IProgress<PassedArgs> progress = null)
            => Run(context, progress);

        public SetupReport GetReport() => _report;

        public Task<IErrorsInfo> RunAsync(SetupContext context, IProgress<PassedArgs> progress = null,
            CancellationToken token = default)
            => Task.FromResult(Run(context, progress));
    }

    private static SetupContext NewContext() => new()
    {
        Options = new SetupOptions(),
        State = new SetupState()
    };

    public static IEnumerable<object[]> AllAdapters() => new List<object[]>
    {
        new object[] { new ConsoleSetupWizardAdapter() },
        new object[] { new DesktopSetupWizardAdapter(_ => { }) },
        new object[] { new WebApiSetupWizardAdapter() },
        new object[] { new BlazorServerSetupWizardAdapter() },
        new object[] { new BlazorWasmSetupWizardAdapter() },
        new object[] { new MauiSetupWizardAdapter((_, _) => { }) },
    };

    // ── the P1-10 defect: inconsistent exception handling ────────────────────

    [Theory]
    [MemberData(nameof(AllAdapters))]
    public async Task Adapter_SurfacesReport_WhenWizardThrowsUnexpectedly(ISetupWizardAdapter adapter)
    {
        var wizard = new StubWizard(new InvalidOperationException("boom"));

        // Only WebApi caught a general Exception; the other five let it escape, so an adapter's
        // behavior on a throw depended on which platform you happened to run.
        var report = await adapter.RunAsync(wizard, NewContext());

        Assert.NotNull(report);
    }

    [Theory]
    [MemberData(nameof(AllAdapters))]
    public async Task Adapter_ReturnsReport_OnSuccess(ISetupWizardAdapter adapter)
    {
        var wizard = new StubWizard();

        var report = await adapter.RunAsync(wizard, NewContext());

        Assert.NotNull(report);
        Assert.True(report.Succeeded);
    }

    // ── behavior a naive base class would silently break ─────────────────────

    private sealed class RecordingMauiAdapter : MauiSetupWizardAdapter
    {
        public bool CompletionMarshalled { get; private set; }
        public bool MarshalCompletedBeforeReturn { get; private set; }

        public RecordingMauiAdapter() : base((_, _) => { }) { }

        protected override async Task InvokeOnMainThreadAsync(Action action)
        {
            // Force a real await so a fire-and-forget caller loses the ordering guarantee.
            await Task.Delay(20).ConfigureAwait(false);
            action();
            CompletionMarshalled = true;
            MarshalCompletedBeforeReturn = true;
        }
    }

    [Fact]
    public async Task Maui_AwaitsMainThreadCompletion_BeforeRunAsyncReturns()
    {
        var adapter = new RecordingMauiAdapter();

        await adapter.RunAsync(new StubWizard(), NewContext());

        // If completion were fire-and-forget, the 20ms delay would still be pending here.
        Assert.True(adapter.CompletionMarshalled,
            "Maui must await its main-thread completion callback before RunAsync returns.");
        Assert.True(adapter.MarshalCompletedBeforeReturn);
    }

    [Fact]
    public async Task WebApi_Status_ReachesCompleted_OnSuccess()
    {
        var adapter = new WebApiSetupWizardAdapter();

        await adapter.RunAsync(new StubWizard(), NewContext());

        Assert.Equal("Completed", adapter.Status.State);
        Assert.NotNull(adapter.Status.Report);
    }

    [Fact]
    public async Task WebApi_Status_ReachesFailed_WhenWizardThrows()
    {
        var adapter = new WebApiSetupWizardAdapter();

        await adapter.RunAsync(new StubWizard(new InvalidOperationException("boom")), NewContext());

        Assert.Equal("Failed", adapter.Status.State);
    }

    [Fact]
    public async Task WebApi_NeverReturnsNull_EvenWithoutAReport()
    {
        var adapter = new WebApiSetupWizardAdapter();

        // WebApi is the only adapter that null-guards its return.
        var report = await adapter.RunAsync(new StubWizard(nullReport: true), NewContext());

        Assert.NotNull(report);
    }

    private sealed class RecordingWasmAdapter : BlazorWasmSetupWizardAdapter
    {
        public string SavedJson { get; private set; }
        public bool LoadCalled { get; private set; }

        protected override Task<string> LoadStateAsync(string key)
        {
            LoadCalled = true;
            return Task.FromResult<string>(null);
        }

        protected override Task SaveStateAsync(string key, string json)
        {
            SavedJson = json;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task BlazorWasm_LoadsAndPersistsState_AroundRun()
    {
        var adapter = new RecordingWasmAdapter();

        await adapter.RunAsync(new StubWizard(), NewContext());

        Assert.True(adapter.LoadCalled, "WASM must attempt a resume load before running.");
        Assert.False(string.IsNullOrEmpty(adapter.SavedJson),
            "WASM must persist state after the run so Resume() can continue.");
    }
}
