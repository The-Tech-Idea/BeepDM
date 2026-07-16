using System.Diagnostics;
using TheTechIdea.Beep.SetUp.Audit;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Guards for Phase 6 (.plans/setup/PHASE-06-Audit-Reporting-Telemetry.md): every run is answerable
/// (who/what/when/result), the log is append-only, auditing never fails the run, and per-step spans
/// are emitted.
/// </summary>
public class AuditTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "beep-audit-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    private sealed class OkStep : ISetupStep
    {
        public OkStep(string id) => StepId = id;
        public string StepId { get; }
        public string StepName => StepId;
        public string Description => StepId;
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        public bool CanSkip(SetupContext context) => false;
        public IErrorsInfo Validate(SetupContext context) => new ErrorsInfo { Flag = Errors.Ok };
        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
            => new ErrorsInfo { Flag = Errors.Ok, Message = "ok" };
    }

    private sealed class FailStepStep : ISetupStep
    {
        public FailStepStep(string id) => StepId = id;
        public string StepId { get; }
        public string StepName => StepId;
        public string Description => StepId;
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        public bool CanSkip(SetupContext context) => false;
        public IErrorsInfo Validate(SetupContext context) => new ErrorsInfo { Flag = Errors.Ok };
        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
            => new ErrorsInfo { Flag = Errors.Failed, Message = "boom" };
    }

    private static SetupContext NewContext() =>
        new() { Options = new SetupOptions(), State = new SetupState() };

    // ── JSONL sink: append-only ──────────────────────────────────────────────

    [Fact]
    public async Task JsonlSink_IsAppendOnly_AcrossRuns()
    {
        var path = Path.Combine(_dir, "a.jsonl");
        var sink = new JsonlSetupAuditSink(path);

        await sink.RecordAsync(new SetupAuditEvent { RunId = "r1", Action = SetupAuditAction.RunStarted });
        await sink.RecordAsync(new SetupAuditEvent { RunId = "r2", Action = SetupAuditAction.RunStarted });

        // The prior run's record must survive — the old checkpoint overwrote in place.
        var r1 = await sink.QueryAsync("r1");
        var r2 = await sink.QueryAsync("r2");
        Assert.Single(r1);
        Assert.Single(r2);
        Assert.Equal(2, (await sink.QueryAsync(null)).Count);
    }

    // ── wizard emits the full lifecycle with actor + definition hash ─────────

    [Fact]
    public void Wizard_Emits_RunStarted_StepCompleted_RunCompleted()
    {
        var path = Path.Combine(_dir, "b.jsonl");
        var wizard = new SetupWizardBuilder()
            .WithId("audited")
            .WithAudit(new JsonlSetupAuditSink(path))
            .AddStep(new OkStep("a"))
            .Build();

        Assert.Equal(Errors.Ok, wizard.Run(NewContext()).Flag);

        var events = new JsonlSetupAuditSink(path).QueryAsync(null).Result;
        var actions = events.Select(e => e.Action).ToList();
        Assert.Contains(SetupAuditAction.RunStarted, actions);
        Assert.Contains(SetupAuditAction.StepStarted, actions);
        Assert.Contains(SetupAuditAction.StepCompleted, actions);
        Assert.Contains(SetupAuditAction.RunCompleted, actions);

        // Every event carries the actor; never claims solo was authenticated.
        Assert.All(events, e => Assert.False(string.IsNullOrEmpty(e.ActorId)));
        Assert.All(events, e => Assert.False(e.ActorAuthenticated));
    }

    [Fact]
    public void Wizard_Emits_RunFailed_OnStepFailure()
    {
        var path = Path.Combine(_dir, "c.jsonl");
        var wizard = new SetupWizardBuilder()
            .WithId("fails")
            .WithAudit(new JsonlSetupAuditSink(path))
            .AddStep(new FailStepStep("bad"))
            .Build();

        wizard.Run(NewContext());

        var actions = new JsonlSetupAuditSink(path).QueryAsync(null).Result.Select(e => e.Action).ToList();
        Assert.Contains(SetupAuditAction.StepFailed, actions);
        Assert.Contains(SetupAuditAction.RunFailed, actions);
        Assert.DoesNotContain(SetupAuditAction.RunCompleted, actions);
    }

    [Fact]
    public void Definition_Run_CarriesDefinitionHash_InAudit()
    {
        var path = Path.Combine(_dir, "d.jsonl");
        var def = new Definition.SetupDefinition
        {
            Id = "hash-wiz",
            Steps = { new Definition.SetupStepDefinition
                { StepId = "driver-provision", Type = Definition.SetupStepFactory.TypeKeys.DriverProvision,
                  Options = System.Text.Json.JsonSerializer.SerializeToElement(
                      new Steps.DriverProvisionStepOptions { PackageName = "SQLite" },
                      Definition.SetupJson.Options) } }
        };
        // No datasource here, so the driver step fails — but the run still emits audit events, which
        // is all this test checks (the definition hash is on every event).
        var wizard = SetupWizardBuilder
            .FromDefinition(def, new Definition.SetupStepFactory())
            .WithAudit(new JsonlSetupAuditSink(path))
            .Build();

        wizard.Run(NewContext());

        var events = new JsonlSetupAuditSink(path).QueryAsync(null).Result;
        Assert.NotEmpty(events);
        Assert.All(events, e => Assert.False(string.IsNullOrEmpty(e.DefinitionHash)));
    }

    // ── auditing must never fail the run ─────────────────────────────────────

    private sealed class ThrowingSink : ISetupAuditSink
    {
        public Task RecordAsync(SetupAuditEvent evt, CancellationToken token = default)
            => throw new InvalidOperationException("sink down");
        public Task<IReadOnlyList<SetupAuditEvent>> QueryAsync(string runId, CancellationToken token = default)
            => throw new InvalidOperationException("sink down");
    }

    [Fact]
    public void AuditSinkFailure_DoesNotFailRun()
    {
        var wizard = new SetupWizardBuilder()
            .WithId("resilient")
            .WithAudit(new ThrowingSink())
            .AddStep(new OkStep("a"))
            .Build();

        // A dead audit sink must not take down a migration.
        Assert.Equal(Errors.Ok, wizard.Run(NewContext()).Flag);
    }

    // ── telemetry spans ──────────────────────────────────────────────────────

    [Fact]
    public void PerStep_Spans_AreEmitted()
    {
        var stepNames = new List<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == Telemetry.SetupActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = a => stepNames.Add(a.OperationName)
        };
        ActivitySource.AddActivityListener(listener);

        var wizard = new SetupWizardBuilder().WithId("traced")
            .AddStep(new OkStep("alpha")).AddStep(new OkStep("beta")).Build();
        wizard.Run(NewContext());

        Assert.Contains("setup.step.alpha", stepNames);
        Assert.Contains("setup.step.beta", stepNames);
    }
}
