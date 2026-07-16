using Moq;
using TheTechIdea.Beep.SetUp.Rollback;
using TheTechIdea.Beep.SetUp.Steps;

namespace TheTechIdea.Beep.SetUp.Tests;

/// <summary>
/// Guards for Phase 4 (.plans/setup/PHASE-04-Rollback-And-Compensation.md): a failed run's
/// completed steps are undone in reverse, best-effort, with skip-vs-undo reported honestly.
/// </summary>
public class RollbackTests
{
    /// <summary>Records execute + rollback calls in the order they happen across all steps.</summary>
    private sealed class RecordingStep : ISetupStep
    {
        private readonly List<string> _log;
        private readonly bool _supportsRollback;
        private readonly bool _rollbackFails;
        private readonly bool _failExecute;

        public RecordingStep(string id, List<string> log, bool supportsRollback = true,
            bool rollbackFails = false, bool failExecute = false)
        {
            StepId = id; _log = log; _supportsRollback = supportsRollback;
            _rollbackFails = rollbackFails; _failExecute = failExecute;
        }

        public string StepId { get; }
        public string StepName => StepId;
        public string Description => StepId;
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        public bool CanSkip(SetupContext context) => false;
        public IErrorsInfo Validate(SetupContext context) => new ErrorsInfo { Flag = Errors.Ok };

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            _log.Add($"exec:{StepId}");
            return new ErrorsInfo { Flag = _failExecute ? Errors.Failed : Errors.Ok, Message = StepId };
        }

        public bool SupportsRollback => _supportsRollback;

        public Task<IErrorsInfo> RollbackAsync(SetupContext context, IProgress<PassedArgs> progress = null,
            CancellationToken token = default)
        {
            _log.Add($"rollback:{StepId}");
            return Task.FromResult<IErrorsInfo>(new ErrorsInfo
            {
                Flag = _rollbackFails ? Errors.Failed : Errors.Ok,
                Message = StepId
            });
        }
    }

    private static SetupContext ContextWithCompleted(params string[] completed) => new()
    {
        Options = new SetupOptions(),
        State = new SetupState
        {
            RunId = "r1",
            CompletedStepIds = new HashSet<string>(completed, StringComparer.Ordinal)
        }
    };

    [Fact]
    public async Task Rollback_UndoesCompletedSteps_InReverse()
    {
        var log = new List<string>();
        var steps = new List<ISetupStep>
        {
            new RecordingStep("a", log), new RecordingStep("b", log), new RecordingStep("c", log)
        };
        // a and b completed; c did not.
        var ctx = ContextWithCompleted("a", "b");

        var report = await new RollbackOrchestrator().RollbackAsync(steps, ctx);

        Assert.True(report.Succeeded);
        Assert.Equal(new[] { "rollback:b", "rollback:a" }, log);   // reverse, and c skipped entirely
    }

    [Fact]
    public async Task Rollback_Continues_WhenOneStepFails()
    {
        var log = new List<string>();
        var steps = new List<ISetupStep>
        {
            new RecordingStep("a", log),
            new RecordingStep("b", log, rollbackFails: true),
            new RecordingStep("c", log)
        };
        var ctx = ContextWithCompleted("a", "b", "c");

        var report = await new RollbackOrchestrator().RollbackAsync(steps, ctx);

        // b's failed rollback must not stop a's — stopping strands more state.
        Assert.False(report.Succeeded);
        Assert.Equal(new[] { "rollback:c", "rollback:b", "rollback:a" }, log);
        Assert.False(report.StepResults.Single(r => r.StepId == "b").Succeeded);
        Assert.True(report.StepResults.Single(r => r.StepId == "a").Succeeded);
    }

    [Fact]
    public async Task Rollback_SkipsSteps_ThatNeverCompleted()
    {
        var log = new List<string>();
        var steps = new List<ISetupStep> { new RecordingStep("a", log), new RecordingStep("b", log) };
        var ctx = ContextWithCompleted("a");   // b never ran

        await new RollbackOrchestrator().RollbackAsync(steps, ctx);

        Assert.Equal(new[] { "rollback:a" }, log);
    }

    [Fact]
    public async Task NonCompensatingStep_ReportsSkipped_NotSucceeded()
    {
        var log = new List<string>();
        var steps = new List<ISetupStep> { new RecordingStep("a", log, supportsRollback: false) };
        var ctx = ContextWithCompleted("a");

        var report = await new RollbackOrchestrator().RollbackAsync(steps, ctx);

        var result = Assert.Single(report.StepResults);
        Assert.True(result.Skipped);
        Assert.DoesNotContain("rollback:a", log);   // RollbackAsync not even called
    }

    [Fact]
    public async Task ThrowingRollback_IsRecordedAsFailure_NotPropagated()
    {
        var throwing = new ThrowingStep("x");
        var ctx = ContextWithCompleted("x");

        // Must not throw out of the orchestrator.
        var report = await new RollbackOrchestrator().RollbackAsync(new List<ISetupStep> { throwing }, ctx);

        Assert.False(report.Succeeded);
        Assert.False(report.StepResults.Single().Succeeded);
    }

    private sealed class ThrowingStep : ISetupStep
    {
        public ThrowingStep(string id) => StepId = id;
        public string StepId { get; }
        public string StepName => StepId;
        public string Description => StepId;
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        public bool CanSkip(SetupContext context) => false;
        public IErrorsInfo Validate(SetupContext context) => new ErrorsInfo { Flag = Errors.Ok };
        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
            => new ErrorsInfo { Flag = Errors.Ok };
        public bool SupportsRollback => true;
        public Task<IErrorsInfo> RollbackAsync(SetupContext context, IProgress<PassedArgs> progress = null,
            CancellationToken token = default) => throw new InvalidOperationException("boom");
    }

    // ── wizard integration ───────────────────────────────────────────────────

    [Fact]
    public void AutoRollback_Off_DoesNotRunRollback_AndReportHasNoRollbackJson()
    {
        var log = new List<string>();
        var wizard = new SetupWizardBuilder()
            .WithId("no-auto")
            .AddStep(new RecordingStep("a", log))
            .AddStep(new RecordingStep("b", log, failExecute: true))
            .Build();

        var result = wizard.Run(new SetupContext { Options = new SetupOptions(), State = new SetupState() });

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.DoesNotContain(log, e => e.StartsWith("rollback:"));   // default: no auto-rollback
        Assert.Null(wizard.GetReport().RollbackReportJson);
    }

    [Fact]
    public void AutoRollback_On_UndoesCompletedSteps_AndRecordsReport()
    {
        var log = new List<string>();
        var wizard = new SetupWizardBuilder()
            .WithId("auto")
            .AddStep(new RecordingStep("a", log))                     // completes
            .AddStep(new RecordingStep("b", log, failExecute: true))  // fails
            .Build();

        var ctx = new SetupContext
        {
            Options = new SetupOptions { AutoRollbackOnFailure = true },
            State = new SetupState()
        };
        var result = wizard.Run(ctx);

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("rollback:a", log);   // the completed step was undone
        Assert.Contains("\"StepId\": \"a\"", wizard.GetReport().RollbackReportJson);
    }

    // ── schema/backup correctness fixes (4-C, 4-E) ───────────────────────────

    [Fact]
    public async Task SchemaRollback_Fails_Loudly_WhenNoExecutionTokenRecorded()
    {
        var editor = new Mock<IDMEEditor>();
        var ds = new Mock<IDataSource>();
        var ctx = new SetupContext
        {
            Editor = editor.Object,
            DataSource = ds.Object,
            Options = new SetupOptions(),
            State = new SetupState()   // no ExecutionToken in Metadata
        };

        var step = new SchemaSetupStep(new SchemaSetupStepOptions());
        var result = await step.RollbackAsync(ctx);

        // Must not silently claim the schema was undone.
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("execution token", result.Message);
    }

    [Fact]
    public async Task NoBackupProvider_ReportsNotConfirmed()
    {
        // The honest default: no backup, rather than the old "confirmed whenever not strict".
        var confirmed = await new NoBackupConfirmationProvider()
            .IsBackupConfirmedAsync(new SetupContext());

        Assert.False(confirmed);
    }
}
