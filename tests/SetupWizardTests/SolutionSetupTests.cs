using System.Text.Json;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.SetUp.Definition;
using TheTechIdea.Beep.SetUp.Solution;
using TheTechIdea.Beep.SetUp.State;
using TheTechIdea.Beep.SetUp.Steps;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Guards for Phase 7 (.plans/setup/PHASE-07-Solution-Aggregate-MultiApp.md): set up a whole
/// solution — N apps, dependency order, one fails ≠ abort others, per-app state isolation.
/// </summary>
public class SolutionSetupTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "beep-solution-tests", Guid.NewGuid().ToString("N"));

    public SolutionSetupTests() => Directory.CreateDirectory(_dir);
    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    /// <summary>A single-step definition whose one step either passes or fails on execute.</summary>
    private string WriteDefinition(string id, bool stepFails)
    {
        // driver-provision with no editor: it fails at Execute (no driver config). We model
        // pass/fail by choosing a step that succeeds vs one that fails, via a marker option.
        var def = new SetupDefinition
        {
            Id = id,
            Steps =
            {
                new SetupStepDefinition
                {
                    StepId = "marker",
                    Type = MarkerStepFactory.TypeKey,
                    Options = System.Text.Json.JsonSerializer.SerializeToElement(
                        new MarkerOptions { Fail = stepFails }, SetupJson.Options)
                }
            }
        };
        var path = Path.Combine(_dir, $"{id}.setup.json");
        File.WriteAllText(path, new JsonSetupDefinitionSerializer().Serialize(def));
        return path;
    }

    private AppDefinition App(string id, bool stepFails = false, bool withDefinition = true)
        => new()
        {
            Id = id,
            Name = id,
            SetupDefinitionPath = withDefinition ? WriteDefinition(id, stepFails) : null
        };

    private (ISolutionSetupOrchestrator orch, LocalJsonSetupStateStore store) NewOrchestrator()
    {
        var store = new LocalJsonSetupStateStore(Path.Combine(_dir, "state"));
        var factory = new MarkerStepFactory();
        var resolver = new SetupWizardResolver(factory, store);
        return (new SolutionSetupOrchestrator(resolver, store), store);
    }

    private static SetupContext Ctx(AppDefinition app) =>
        new() { Options = new SetupOptions(), State = new SetupState() };

    // ── happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SetsUp_AllApps_WhenAllSucceed()
    {
        var (orch, _) = NewOrchestrator();
        var apps = new[] { App("a"), App("b"), App("c") };

        var report = await orch.SetupSolutionAsync(apps, "Development", Ctx);

        Assert.True(report.Succeeded);
        Assert.All(report.Apps, r => Assert.Equal(AppSetupOutcome.Succeeded, r.Outcome));
    }

    [Fact]
    public async Task App_WithNoDefinition_IsSkipped_NotFailed()
    {
        var (orch, _) = NewOrchestrator();
        var apps = new[] { App("a"), App("b", withDefinition: false) };

        var report = await orch.SetupSolutionAsync(apps, "Development", Ctx);

        Assert.True(report.Succeeded);   // no-definition is not a failure
        Assert.Equal(AppSetupOutcome.SkippedNoDefinition, report.Apps.Single(r => r.AppId == "b").Outcome);
    }

    // ── one fails, others unaffected ─────────────────────────────────────────

    [Fact]
    public async Task OneApp_Fails_OthersStillRun()
    {
        var (orch, _) = NewOrchestrator();
        var apps = new[] { App("a", stepFails: true), App("b"), App("c") };

        var report = await orch.SetupSolutionAsync(apps, "Development", Ctx);

        Assert.False(report.Succeeded);
        Assert.Equal(AppSetupOutcome.Failed, report.Apps.Single(r => r.AppId == "a").Outcome);
        // Independent apps must not be aborted by a's failure.
        Assert.Equal(AppSetupOutcome.Succeeded, report.Apps.Single(r => r.AppId == "b").Outcome);
        Assert.Equal(AppSetupOutcome.Succeeded, report.Apps.Single(r => r.AppId == "c").Outcome);
    }

    [Fact]
    public async Task DependentApp_Skipped_WhenDependencyFails()
    {
        var (orch, _) = NewOrchestrator();
        var apps = new[] { App("db", stepFails: true), App("api") };
        var options = new SolutionSetupOptions
        {
            Dependencies = new Dictionary<string, IReadOnlyList<string>> { ["api"] = new[] { "db" } }
        };

        var report = await orch.SetupSolutionAsync(apps, "Development", Ctx, options);

        Assert.Equal(AppSetupOutcome.Failed, report.Apps.Single(r => r.AppId == "db").Outcome);
        // api depends on db, which failed → skipped, never set up on a broken prereq.
        Assert.Equal(AppSetupOutcome.SkippedDependencyFailed, report.Apps.Single(r => r.AppId == "api").Outcome);
    }

    [Fact]
    public async Task Apps_RunInDependencyOrder()
    {
        var (orch, _) = NewOrchestrator();
        // Supplied out of order; api depends on db, web depends on api.
        var apps = new[] { App("web"), App("api"), App("db") };
        var options = new SolutionSetupOptions
        {
            Dependencies = new Dictionary<string, IReadOnlyList<string>>
            {
                ["api"] = new[] { "db" },
                ["web"] = new[] { "api" }
            }
        };

        var report = await orch.SetupSolutionAsync(apps, "Development", Ctx, options);

        Assert.Equal(new[] { "db", "api", "web" }, report.Apps.Select(r => r.AppId));
    }

    // ── per-app state isolation + status ─────────────────────────────────────

    [Fact]
    public async Task Apps_Have_IsolatedState_OnSharedStore()
    {
        var (orch, store) = NewOrchestrator();
        var apps = new[] { App("a"), App("b") };

        await orch.SetupSolutionAsync(apps, "Development", Ctx);

        // Each app's checkpoint lives under its own appId slot.
        var stateA = await store.LoadAsync(new SetupStateKey("a", "Development", "a"));
        var stateB = await store.LoadAsync(new SetupStateKey("b", "Development", "b"));
        Assert.NotNull(stateA);
        Assert.NotNull(stateB);
        Assert.Contains("marker", stateA!.CompletedStepIds);
    }

    [Fact]
    public async Task GetStatus_Reports_PerApp_Progress()
    {
        var (orch, _) = NewOrchestrator();
        var apps = new[] { App("a"), App("b", withDefinition: false) };

        await orch.SetupSolutionAsync(apps, "Development", Ctx);
        var status = await orch.GetStatusAsync(apps, "Development");

        Assert.Equal(SetupProgress.Complete, status.Apps.Single(s => s.AppId == "a").Progress);
        Assert.Equal(SetupProgress.NotSetUp, status.Apps.Single(s => s.AppId == "b").Progress);
    }

    // ── a minimal step type registered in the factory ────────────────────────

    private sealed class MarkerOptions { public bool Fail { get; set; } }

    private sealed class MarkerStep : ISetupStep
    {
        private readonly bool _fail;
        public MarkerStep(bool fail) => _fail = fail;
        public string StepId => "marker";
        public string StepName => "marker";
        public string Description => "marker";
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        public System.Text.Json.JsonElement? SerializeOptions()
            => System.Text.Json.JsonSerializer.SerializeToElement(new MarkerOptions { Fail = _fail }, SetupJson.Options);
        public bool CanSkip(SetupContext context) => false;
        public IErrorsInfo Validate(SetupContext context) => new ErrorsInfo { Flag = Errors.Ok };
        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
            => new ErrorsInfo { Flag = _fail ? Errors.Failed : Errors.Ok, Message = "marker" };
    }

    private sealed class MarkerStepFactory : ISetupStepFactory
    {
        public const string TypeKey = "marker";
        public IReadOnlyCollection<string> RegisteredTypes => new[] { TypeKey };
        public bool CanCreate(string typeKey) => typeKey == TypeKey;
        public void Register(string typeKey, Func<System.Text.Json.JsonElement?, ISetupStep> factory) { }

        public ISetupStep Create(SetupStepDefinition definition)
        {
            var opts = definition.Options?.Deserialize<MarkerOptions>(SetupJson.Options) ?? new MarkerOptions();
            return new MarkerStep(opts.Fail);
        }
    }
}
