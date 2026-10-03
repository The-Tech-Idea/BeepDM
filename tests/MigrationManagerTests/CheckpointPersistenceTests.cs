using System.Diagnostics;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.ConfigUtil.Managers;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.JsonLoaderService;
using TheTechIdea.Beep.Services.Persistence;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

[Collection("Checkpoint restart")]
public sealed class CheckpointPersistenceTests : IDisposable
{
    private sealed class Product { public int Id { get; set; } }
    private sealed class Order { public int Id { get; set; } }
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BeepDM-MigrationPersistence", Guid.NewGuid().ToString("N"));

    private static (MigrationTestHarness Harness, MigrationManager Manager, MigrationPlanArtifact Plan) Build(bool twoSteps = false)
    {
        var harness = new MigrationTestHarness().WithDesired(typeof(Product), MigrationTestHarness.Entity("Product", "Id"));
        if (twoSteps) harness.WithDesired(typeof(Order), MigrationTestHarness.Entity("SalesOrder", "Id"));
        var manager = harness.Build();
        var plan = manager.BuildMigrationPlanForTypes(twoSteps ? new[] { typeof(Product), typeof(Order) } : new[] { typeof(Product) });
        return (harness, manager, plan);
    }

    private static PersistenceWriteResult Failed() => new(PersistenceWriteStatus.Failed, new IOException("injected checkpoint failure"));
    private static MigrationExecutionCheckpoint Snapshot(MigrationRecord record) =>
        new JsonLoader().DeserializeSnapshot<MigrationExecutionCheckpoint>(record.Notes);

    [Fact]
    public void InitialCheckpointFailure_AdmitsNoDdl_AndCanRetryAfterStorageRecovery()
    {
        var (harness, manager, plan) = Build();
        harness.HistoryWrite = record => record.Name == "ExecuteMigrationPlan.Checkpoint" ? Failed() : new(PersistenceWriteStatus.Saved);
        var result = manager.ExecuteMigrationPlan(plan, executionToken: Guid.NewGuid().ToString("N"));
        Assert.False(result.Success);
        Assert.False(result.CheckpointPersisted);
        Assert.Equal(PersistenceWriteStatus.Failed, result.CheckpointPersistenceStatus);
        Assert.False(result.RequiresReconciliation);
        Assert.Empty(harness.ProviderCalls);
        harness.HistoryWrite = null;
        Assert.True(manager.ExecuteMigrationPlan(plan, executionToken: result.ExecutionToken).Success);
        Assert.Single(harness.ProviderCalls);
    }

    [Fact]
    public void BeforeAttemptCheckpointFailure_IsNotIgnoredByRetryHooks()
    {
        var (harness, manager, plan) = Build();
        harness.HistoryWrite = record => record.Name == "ExecuteMigrationPlan.Checkpoint" &&
            Snapshot(record).Steps.Any(step => step.Status == MigrationExecutionStepStatus.Running)
            ? Failed() : new(PersistenceWriteStatus.Saved);
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.False(result.Success);
        Assert.Empty(harness.ProviderCalls);
        Assert.Equal(0, result.AppliedCount);
        Assert.False(result.RequiresReconciliation);
        Assert.DoesNotContain(result.Checkpoint.Steps, step => step.Status == MigrationExecutionStepStatus.Running);
    }

    [Fact]
    public void SaveAfterAcknowledgedDdl_StopsLaterWork_AndBlocksReplay()
    {
        var (harness, manager, plan) = Build(twoSteps: true);
        harness.HistoryWrite = record => record.Name == "ExecuteMigrationPlan.Checkpoint" &&
            Snapshot(record).Steps.Any(step => step.Status == MigrationExecutionStepStatus.Completed)
            ? Failed() : new(PersistenceWriteStatus.Saved);
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.False(result.Success);
        Assert.False(result.CheckpointPersisted);
        Assert.True(result.RequiresOperatorIntervention);
        Assert.True(result.RequiresReconciliation);
        Assert.Equal(1, result.AppliedCount);
        Assert.Single(harness.ProviderCalls);
        Assert.False(result.Checkpoint.IsCompleted);
        harness.HistoryWrite = null;
        Assert.True(manager.ResumeMigrationPlan(result.ExecutionToken).RequiresReconciliation);
        Assert.Single(harness.ProviderCalls);
        var restarted = harness.Build().ResumeMigrationPlan(result.ExecutionToken);
        Assert.False(restarted.Success);
        Assert.True(restarted.RequiresReconciliation); // durable Running marker is an uncertain outcome
        Assert.Single(harness.ProviderCalls);
    }

    [Fact]
    public void FinalCheckpointSaveFailure_CannotBecomeCompletedSuccess()
    {
        var (harness, manager, plan) = Build();
        harness.HistoryWrite = record => record.Name == "ExecuteMigrationPlan.Checkpoint" && Snapshot(record).IsCompleted
            ? Failed() : new(PersistenceWriteStatus.Saved);
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.False(result.Success);
        Assert.True(result.RequiresReconciliation);
        Assert.Equal(1, result.AppliedCount);
        Assert.False(result.Checkpoint.IsCompleted);
        Assert.Single(harness.ProviderCalls);
    }

    [Fact]
    public void ProviderFailure_RemainsPrimaryWhenFailureCheckpointAlsoFails()
    {
        var (harness, manager, plan) = Build();
        harness.FailOps.Add("CreateEntityAs");
        harness.HistoryWrite = record => record.Name == "ExecuteMigrationPlan.Checkpoint" && Snapshot(record).HasFailed
            ? Failed() : new(PersistenceWriteStatus.Saved);
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.False(result.Success);
        Assert.Equal(0, result.AppliedCount);
        Assert.NotEmpty(result.FailedSteps);
        Assert.Contains("Failed to create entity 'Product'", result.Message);
        Assert.Contains("Checkpoint was not acknowledged", result.Message);
        Assert.Single(harness.ProviderCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnsupportedOrNullAcknowledgement_FailsClosed(bool nullAcknowledgement)
    {
        var (harness, manager, plan) = Build();
        var config = new Mock<IConfigEditor>();
        if (nullAcknowledgement)
        {
            var storage = new Mock<IMigrationExecutionStorage>();
            storage.SetupGet(c => c.ScopeIdentity).Returns(harness.ExecutionStorage.ScopeIdentity);
            storage.Setup(c => c.LoadMigrationHistory(It.IsAny<string>())).Returns<string>(harness.ExecutionStorage.LoadMigrationHistory);
            storage.Setup(c => c.TryAcquireMigrationExecution(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, string, string, CancellationToken>(harness.ExecutionStorage.TryAcquireMigrationExecution);
            storage.Setup(c => c.AppendMigrationRecordAcknowledged(
                It.IsAny<string>(), It.IsAny<DataSourceType>(), It.IsAny<MigrationRecord>(), It.IsAny<CancellationToken>()))
                .Returns((PersistenceWriteResult)null);
            config.As<IMigrationExecutionStorageProvider>().Setup(c => c.CaptureMigrationExecutionStorage()).Returns(storage.Object);
        }
        harness.ConfigOverride = config.Object;
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.False(result.Success);
        Assert.Equal(nullAcknowledgement ? PersistenceWriteStatus.Failed : PersistenceWriteStatus.Unsupported, result.CheckpointPersistenceStatus);
        Assert.Empty(harness.ProviderCalls);
        config.Verify(c => c.AppendMigrationRecord(It.IsAny<string>(), It.IsAny<DataSourceType>(), It.IsAny<MigrationRecord>()), Times.Never);
    }

    [Fact]
    public void Planning_ExposesFailedPersistenceWithoutClaimingTheArtifactWasSaved()
    {
        var (harness, manager, _) = Build();
        harness.HistoryWrite = _ => Failed();
        var plan = manager.BuildMigrationPlanForTypes(new[] { typeof(Product) });
        Assert.Equal(PersistenceWriteStatus.Failed, plan.PlanPersistenceStatus);
        Assert.Equal(nameof(IOException), plan.PlanPersistenceErrorCode);
        Assert.False(manager.ExecuteMigrationPlan(plan).Success);
        Assert.Empty(harness.ProviderCalls);
    }

    [Fact]
    public async Task RealFilesystemPlanAndCompletedCheckpoint_ResumeInSeparateProcess()
    {
        Directory.CreateDirectory(_folder);
        var store = new MigrationHistoryManager(null, new JsonLoader(), new ConfigandSettings { ConfigPath = _folder }, null);
        var config = new Mock<IConfigEditor>();
        config.Setup(c => c.LoadMigrationHistory(It.IsAny<string>())).Returns<string>(store.Load);
        config.As<IMigrationHistoryPersistence>().Setup(c => c.AppendMigrationRecordAcknowledged(
            It.IsAny<string>(), It.IsAny<DataSourceType>(), It.IsAny<MigrationRecord>(), It.IsAny<CancellationToken>()))
            .Returns<string, DataSourceType, MigrationRecord, CancellationToken>(store.AppendAcknowledged);
        config.As<IMigrationExecutionStorageProvider>().Setup(c => c.CaptureMigrationExecutionStorage())
            .Returns(new FileMigrationExecutionStorage(_folder, new JsonLoader()));
        var desired = MigrationTestHarness.Entity("Product", "Id");
        desired.Fields[0].Size1 = 77;
        var harness = new MigrationTestHarness { ConfigOverride = config.Object }.WithDesired(typeof(Product), desired);
        var manager = harness.Build();
        var plan = manager.BuildMigrationPlanForTypes(new[] { typeof(Product) });
        Assert.Equal(PersistenceWriteStatus.Saved, plan.PlanPersistenceStatus);
        var result = manager.ExecuteMigrationPlan(plan);
        Assert.True(result.Success, result.Message);
        Assert.True(result.CheckpointPersisted);
        var worker = Path.Combine(AppContext.BaseDirectory, "persistence-worker", "FrameworkPersistenceWorker.dll");
        Assert.True(File.Exists(worker));
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var arg in new[] { worker, "migration-resume", _folder, "testdb", plan.PlanId, result.ExecutionToken, plan.PlanHash })
            start.ArgumentList.Add(arg);
        using var child = Process.Start(start)!;
        try
        {
            var errors = child.StandardError.ReadToEndAsync();
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
            Assert.True(child.ExitCode == 0, await errors);
        }
        finally
        {
            if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidPersistedCheckpoint_IsNotReplacedByFreshExecution(bool wrongToken)
    {
        var (harness, manager, plan) = Build();
        var executionToken = Guid.NewGuid().ToString("N");
        var checkpoint = manager.CreateExecutionCheckpoint(plan, executionToken);
        if (wrongToken) checkpoint.ExecutionToken = "different-token";
        var notes = wrongToken ? new JsonLoader().SerializeSnapshot(checkpoint) : "{broken";
        harness.History.Migrations.Add(new MigrationRecord
        {
            MigrationId = executionToken, Name = "ExecuteMigrationPlan.Checkpoint", Notes = notes,
            AppliedOnUtc = DateTime.UtcNow.AddMinutes(1)
        });
        manager = harness.Build();
        var before = harness.History.Migrations.Count;
        var result = manager.ExecuteMigrationPlan(plan, executionToken: executionToken);
        Assert.False(result.Success);
        Assert.True(result.RequiresReconciliation);
        Assert.Empty(harness.ProviderCalls);
        Assert.Equal(before, harness.History.Migrations.Count);
    }

    public void Dispose() { if (Directory.Exists(_folder)) Directory.Delete(_folder, true); }
}
