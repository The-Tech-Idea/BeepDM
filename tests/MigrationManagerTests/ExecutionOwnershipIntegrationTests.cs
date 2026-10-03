using System.Diagnostics;
using System.Reflection;
using Moq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.JsonLoaderService;
using TheTechIdea.Beep.Services.Persistence;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

[Collection("Checkpoint restart")]
public sealed class ExecutionOwnershipIntegrationTests : IDisposable
{
    private sealed class Product { public int Id { get; set; } }
    private sealed class Order { public int Id { get; set; } }
    private const string Target = "test-target/sqlserver";
    private readonly string _root = Path.Combine(Path.GetTempPath(), "BeepDM-ManagerOwnership", Guid.NewGuid().ToString("N"));

    private (MigrationTestHarness Harness, MigrationManager Manager, MigrationPlanArtifact Plan, MigrationStorageProbe Store)
        Build(string? root = null, bool two = false, EntityStructure? existing = null, EntityStructure? desired = null, bool indexes = false,
            string alias = "testdb")
    {
        var store = new MigrationStorageProbe(new FileMigrationExecutionStorage(root ?? _root, new JsonLoader()));
        var harness = new MigrationTestHarness { ExecutionStorage = store, TargetName = alias }
            .WithDesired(typeof(Product), desired ?? MigrationTestHarness.Entity("Product", "Id"));
        if (two) harness.WithDesired(typeof(Order), MigrationTestHarness.Entity("SalesOrder", "Id"));
        if (existing != null) harness.WithExisting(existing);
        var config = new Mock<IConfigEditor>();
        config.Setup(c => c.LoadMigrationHistory(It.IsAny<string>())).Returns<string>(store.LoadMigrationHistory);
        config.As<IMigrationHistoryPersistence>().Setup(c => c.AppendMigrationRecordAcknowledged(It.IsAny<string>(),
            It.IsAny<DataSourceType>(), It.IsAny<MigrationRecord>(), It.IsAny<CancellationToken>()))
            .Returns<string, DataSourceType, MigrationRecord, CancellationToken>(store.AppendMigrationRecordAcknowledged);
        config.As<IMigrationExecutionStorageProvider>().Setup(c => c.CaptureMigrationExecutionStorage()).Returns(() => harness.ExecutionStorage);
        harness.ConfigOverride = config.Object;
        var manager = harness.Build();
        var plan = manager.BuildMigrationPlanForTypes(two ? new[] { typeof(Product), typeof(Order) } : new[] { typeof(Product) }, applyIndexes: indexes);
        return (harness, manager, plan, store);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompetingManagers_SamePhysicalTarget_DenyBeforeCheckpointOrDdl(bool sameToken)
    {
        var first = Build();
        var other = Build(alias: sameToken ? "testdb" : "different-alias");
        var token = Guid.NewGuid().ToString("N");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        first.Harness.BeforeCreateEntity = _ => { entered.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(30))); };
        var work = Task.Run(() => first.Manager.ExecuteMigrationPlan(first.Plan, executionToken: token));
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(30)));
            var before = other.Store.LoadMigrationHistory(other.Harness.TargetName).Migrations.Count;
            var denied = other.Manager.ExecuteMigrationPlan(other.Plan, executionToken: sameToken ? token : Guid.NewGuid().ToString("N"));
            Assert.Equal(MigrationAdmissionStatus.Busy, denied.OwnershipAdmissionStatus);
            Assert.Empty(other.Harness.ProviderCalls);
            Assert.Equal(before, other.Store.LoadMigrationHistory(other.Harness.TargetName).Migrations.Count);
            var active = first.Store.ReadMigrationExecutionClaim(Target);
            Assert.False(first.Store.ReconcileMigrationExecution(Target, active.ClaimId, "operator", "evidence").IsSaved);
            Assert.Equal(MigrationAdmissionStatus.Busy, first.Manager.ExecuteMigrationPlan(first.Plan).OwnershipAdmissionStatus);
        }
        finally { release.Set(); }
        var result = await work.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.True(result.Success, result.Message);
        Assert.True(result.OwnershipFinished);
    }

    [Fact]
    public void IndependentStores_CanReuseToken_WithoutProcessGlobalAuthority()
    {
        var first = Build();
        var other = Build(Path.Combine(_root, "independent"));
        var token = "same-token";
        Assert.True(first.Manager.ExecuteMigrationPlan(first.Plan, executionToken: token).Success);
        Assert.True(other.Manager.ExecuteMigrationPlan(other.Plan, executionToken: token).Success);
        Assert.Single(other.Harness.ProviderCalls);
        Assert.NotEqual(first.Store.ScopeIdentity, other.Store.ScopeIdentity);
    }

    [Fact]
    public void CapturedStoreAndBindings_RemainPinnedThroughCallbacks()
    {
        var setup = Build();
        var alternate = new FileMigrationExecutionStorage(Path.Combine(_root, "alternate"), new JsonLoader());
        setup.Harness.BeforeProviderResolve = () =>
        {
            setup.Harness.ExecutionStorage = alternate;
            Assert.Throws<InvalidOperationException>(() => setup.Manager.MigrateDataSource = null);
            Assert.Throws<InvalidOperationException>(() => setup.Manager.ExecutionTargetIdentity = "changed");
        };
        var result = setup.Manager.ExecuteMigrationPlan(setup.Plan);
        Assert.True(result.Success, result.Message);
        Assert.Contains(setup.Store.LoadMigrationHistory("testdb").Migrations, r => r.Name == "ExecuteMigrationPlan.Checkpoint");
        Assert.DoesNotContain(alternate.LoadMigrationHistory("testdb").Migrations, r => r.Name == "ExecuteMigrationPlan.Checkpoint");
        Assert.Equal(MigrationClaimDisposition.Completed, setup.Store.ReadMigrationExecutionClaim(Target).Disposition);
        Assert.Null(alternate.ReadMigrationExecutionClaim(Target));
    }

    [Fact]
    public void PublicProgressAndResultMutation_CannotSkipLaterDdlOrChangeStoredIntent()
    {
        var setup = Build(two: true);
        var token = Guid.NewGuid().ToString("N");
        var progress = new InlineProgress(() =>
        {
            var view = setup.Manager.GetExecutionCheckpoint(token);
            view.Steps[1].Status = MigrationExecutionStepStatus.Completed;
            view.ApprovedPlan.Operations[1].SchemaSnapshot.DesiredSchema.EntityName = "Unapproved";
        });
        var result = setup.Manager.ExecuteMigrationPlan(setup.Plan, executionToken: token, progress: progress);
        Assert.True(result.Success, result.Message);
        Assert.Equal(2, setup.Harness.ProviderCalls.Count);
        result.Checkpoint.Steps.Clear();
        result.Checkpoint.ApprovedPlan.Operations.Clear();
        var resumed = setup.Harness.Build().ResumeMigrationPlan(token);
        Assert.True(resumed.Success, resumed.Message);
        Assert.Equal(2, resumed.Checkpoint.Steps.Count);
        Assert.Equal(2, setup.Harness.ProviderCalls.Count);
    }

    [Fact]
    public void TargetMutationAfterAcknowledgement_StopsLaterWorkAndRetainsCounts()
    {
        var setup = Build(two: true);
        var result = setup.Manager.ExecuteMigrationPlan(setup.Plan, progress: new InlineProgress(() => setup.Harness.TargetGuid = "changed"));
        Assert.False(result.Success);
        Assert.True(result.RequiresReconciliation);
        Assert.Equal(1, result.AppliedCount);
        Assert.Single(setup.Harness.ProviderCalls);
        Assert.Equal(MigrationClaimState.RequiresReconciliation, setup.Store.ReadMigrationExecutionClaim(Target).State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NegativeOrThrowingProviderAcknowledgement_IsNotRetriedOrReleased(bool throws)
    {
        var setup = Build();
        if (throws) setup.Harness.BeforeCreateEntity = _ => throw new TimeoutException("injected provider timeout");
        else setup.Harness.FailOps.Add("CreateEntityAs");
        var result = setup.Manager.ExecuteMigrationPlan(setup.Plan);
        Assert.False(result.Success);
        Assert.True(result.RequiresReconciliation);
        Assert.Single(setup.Harness.ProviderCalls);
        Assert.Equal(MigrationClaimState.RequiresReconciliation, setup.Store.ReadMigrationExecutionClaim(Target).State);
        setup.Harness.BeforeCreateEntity = null;
        setup.Harness.FailOps.Clear();
        Assert.Equal(MigrationAdmissionStatus.RequiresReconciliation, setup.Harness.Build().ExecuteMigrationPlan(setup.Plan).OwnershipAdmissionStatus);
        Assert.Single(setup.Harness.ProviderCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnacknowledgedOwnershipFinish_DoesNotHideSavedCheckpointOrCounts(bool throws)
    {
        var setup = Build();
        setup.Store.FinishOverride = _ => throws ? throw new IOException("injected finish") : new(PersistenceWriteStatus.Failed);
        var result = setup.Manager.ExecuteMigrationPlan(setup.Plan);
        Assert.False(result.Success);
        Assert.True(result.CheckpointPersisted);
        Assert.Equal(PersistenceWriteStatus.Saved, result.CheckpointPersistenceStatus);
        Assert.False(result.OwnershipFinished);
        Assert.Equal(PersistenceWriteStatus.Failed, result.OwnershipPersistenceStatus);
        Assert.Equal(1, result.AppliedCount);
        Assert.True(result.RequiresReconciliation);
        setup.Store.FinishOverride = null;
        Assert.Equal(MigrationAdmissionStatus.RequiresReconciliation, setup.Harness.Build().ResumeMigrationPlan(result.ExecutionToken).OwnershipAdmissionStatus);
        Assert.Single(setup.Harness.ProviderCalls);
    }

    [Fact]
    public void InvalidClaimReceipt_IsDisposedButNeverFinished()
    {
        var setup = Build();
        setup.Store.ClaimOverride = claim => new("foreign-key", claim.ClaimId, claim.ExecutionToken,
            claim.PlanHash, claim.Revision, claim.State, claim.Disposition, claim.UpdatedOnUtc);
        var result = setup.Manager.ExecuteMigrationPlan(setup.Plan);
        Assert.False(result.Success);
        Assert.True(result.RequiresReconciliation);
        Assert.Equal(0, setup.Store.FinishCalls);
        Assert.Empty(setup.Harness.ProviderCalls);
        setup.Store.ClaimOverride = null;
        Assert.Equal(MigrationClaimState.Owned, setup.Store.ReadMigrationExecutionClaim(Target).State);
        Assert.Equal(MigrationAdmissionStatus.RequiresReconciliation, setup.Harness.Build().ExecuteMigrationPlan(setup.Plan).OwnershipAdmissionStatus);
    }

    [Fact]
    public async Task CancellationAfterAcknowledgement_PreservesCountsAndBlocksReplay()
    {
        var setup = Build(two: true);
        using var cancellation = new CancellationTokenSource();
        var result = await setup.Manager.ExecuteMigrationPlanAsync(setup.Plan,
            progress: new InlineProgress(cancellation.Cancel), token: cancellation.Token);
        Assert.False(result.Success);
        Assert.True(result.RequiresReconciliation);
        Assert.Equal(1, result.AppliedCount);
        Assert.Single(setup.Harness.ProviderCalls);
        Assert.Equal(MigrationClaimState.RequiresReconciliation, setup.Store.ReadMigrationExecutionClaim(Target).State);
    }

    [Fact]
    public void MissingCanonicalIdentity_DeniesBeforeProviderOrCheckpoint()
    {
        var setup = Build();
        setup.Manager.ExecutionTargetIdentity = null;
        var result = setup.Manager.ExecuteMigrationPlan(setup.Plan);
        Assert.Equal(MigrationAdmissionStatus.Unsupported, result.OwnershipAdmissionStatus);
        Assert.Empty(setup.Harness.ProviderCalls);
        Assert.DoesNotContain(setup.Store.LoadMigrationHistory("testdb").Migrations, r => r.Name == "ExecuteMigrationPlan.Checkpoint");
    }

    [Fact]
    public void CapturedRootIdentity_IsStableAcrossTrailingSeparators()
    {
        var first = new FileMigrationExecutionStorage(_root, new JsonLoader());
        var second = new FileMigrationExecutionStorage(_root + Path.DirectorySeparatorChar, new JsonLoader());
        Assert.Equal(first.ScopeIdentity, second.ScopeIdentity);
    }

    [Fact]
    public void DirectCheckpointCreation_HoldsAndFinishesOwnership_AndReturnsOwnedCopy()
    {
        var setup = Build();
        var checkpoint = setup.Manager.CreateExecutionCheckpoint(setup.Plan);
        Assert.NotEmpty(checkpoint.OwnershipStoreIdentity);
        Assert.NotEmpty(checkpoint.OwnershipTargetKey);
        Assert.Equal(MigrationClaimDisposition.SafeToRetry, setup.Store.ReadMigrationExecutionClaim(Target).Disposition);
        checkpoint.Steps.Clear();
        Assert.Single(setup.Harness.Build().GetExecutionCheckpoint(checkpoint.ExecutionToken).Steps);
        Assert.True(setup.Manager.ExecuteMigrationPlan(setup.Plan, executionToken: checkpoint.ExecutionToken).Success);
    }

    [Fact]
    public void OwnershiplessPersistedCheckpoint_IsNotAutomaticallyAdopted()
    {
        var setup = Build();
        var checkpoint = setup.Manager.CreateExecutionCheckpoint(setup.Plan);
        var history = setup.Store.LoadMigrationHistory("testdb");
        var record = history.Migrations.Last(r => r.Name == "ExecuteMigrationPlan.Checkpoint");
        checkpoint.OwnershipStoreIdentity = checkpoint.OwnershipTargetKey = null;
        record.Notes = new JsonLoader().SerializeSnapshot(checkpoint);
        Assert.True(setup.Store.SaveMigrationHistoryAcknowledged(history).IsSaved);
        var before = history.Migrations.Count;
        var result = setup.Manager.ExecuteMigrationPlan(setup.Plan, executionToken: checkpoint.ExecutionToken);
        Assert.False(result.Success);
        Assert.True(result.RequiresReconciliation);
        Assert.Empty(setup.Harness.ProviderCalls);
        Assert.Equal(before, setup.Store.LoadMigrationHistory("testdb").Migrations.Count);
    }

    [Fact]
    public void Compensation_UsesReverseOrderAndDurableMarkers_WithoutRepeatingCompletedWork()
    {
        var desired = MigrationTestHarness.Entity("Product", "Id");
        desired.Indexes.Add(new EntityIndex { Name = "IX_First", Columns = new() { "Id" } });
        desired.Indexes.Add(new EntityIndex { Name = "IX_Second", Columns = new() { "Id" } });
        var setup = Build(existing: MigrationTestHarness.Entity("Product", "Id"), desired: desired, indexes: true);
        var forward = setup.Manager.ExecuteMigrationPlan(setup.Plan);
        Assert.True(forward.Success, forward.Message);
        setup.Harness.ProviderCalls.Clear();
        var rollback = setup.Manager.RollbackFailedExecution(forward.ExecutionToken, false);
        Assert.True(rollback.Success, rollback.Message);
        Assert.True(rollback.OwnershipFinished);
        Assert.Equal(2, rollback.AppliedCount);
        Assert.Equal(new[] { "DropIndex:Product.IX_Second", "DropIndex:Product.IX_First" }, setup.Harness.ProviderCalls);
        Assert.True(setup.Manager.GetExecutionCheckpoint(forward.ExecutionToken).CompensationCompleted);
        setup.Harness.ProviderCalls.Clear();
        Assert.True(setup.Harness.Build().RollbackFailedExecution(forward.ExecutionToken, false).Success);
        Assert.False(setup.Harness.Build().ResumeMigrationPlan(forward.ExecutionToken).Success);
        Assert.Empty(setup.Harness.ProviderCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DropCompensation_RestoresCapturedDefinition_InsteadOfDroppingAgain(bool foreignKey)
    {
        var current = MigrationTestHarness.Entity("Product", "Id");
        if (foreignKey) current.Relations.Add(new RelationShipKeys { RalationName = "FK_Original", EntityColumnID = "Id",
            RelatedEntityID = "Parent", RelatedEntityColumnID = "Id" });
        else current.Indexes.Add(new EntityIndex { Name = "IX_Original", Columns = new() { "Id" }, IsUnique = true });
        var setup = Build(existing: current);
        var operation = setup.Plan.Operations[0];
        operation.Kind = foreignKey ? MigrationPlanOperationKind.DropForeignKey : MigrationPlanOperationKind.DropIndex;
        operation.TargetName = foreignKey ? "FK_Original" : "IX_Original";
        SealFixturePlan(setup.Plan);
        var forward = setup.Manager.ExecuteMigrationPlan(setup.Plan);
        Assert.True(forward.Success, forward.Message);
        current.Indexes.Clear();
        current.Relations.Clear();
        setup.Harness.ProviderCalls.Clear();
        var rollback = setup.Manager.RollbackFailedExecution(forward.ExecutionToken, false);
        Assert.True(rollback.Success, rollback.Message);
        Assert.Equal(1, rollback.AppliedCount);
        Assert.Contains(setup.Harness.ProviderCalls, call => call.StartsWith(foreignKey ? "AddForeignKey:" : "CreateIndex:"));
        Assert.DoesNotContain(setup.Harness.ProviderCalls, call => call.StartsWith("Drop"));
    }

    [Fact]
    public void PendingCompensation_DoesNotDropObjectsFromUnappliedOperations()
    {
        var desired = MigrationTestHarness.Entity("Product", "Id");
        desired.Indexes.Add(new EntityIndex { Name = "IX_Pending", Columns = new() { "Id" } });
        var setup = Build(existing: MigrationTestHarness.Entity("Product", "Id"), desired: desired, indexes: true);
        var checkpoint = setup.Manager.CreateExecutionCheckpoint(setup.Plan);
        var result = setup.Manager.RollbackFailedExecution(checkpoint.ExecutionToken, false);
        Assert.True(result.Success, result.Message);
        Assert.Empty(result.ExecutedActions);
        Assert.Empty(setup.Harness.ProviderCalls);
    }

    [Fact]
    public void MissingOriginalDefinition_ReportsManualRecovery_NotCompletedCompensation()
    {
        var setup = Build(existing: MigrationTestHarness.Entity("Product", "Id"));
        setup.Plan.Operations[0].Kind = MigrationPlanOperationKind.DropIndex;
        setup.Plan.Operations[0].TargetName = "IX_Unknown";
        SealFixturePlan(setup.Plan);
        var forward = setup.Manager.ExecuteMigrationPlan(setup.Plan);
        Assert.True(forward.Success, forward.Message);
        setup.Harness.ProviderCalls.Clear();
        var rollback = setup.Manager.RollbackFailedExecution(forward.ExecutionToken, false);
        Assert.False(rollback.Success);
        Assert.True(rollback.RequiresOperatorIntervention);
        Assert.Single(rollback.ManualActions);
        Assert.Empty(rollback.ExecutedActions);
        Assert.Empty(setup.Harness.ProviderCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompensationSaveFailure_SeparatesPreProviderDenialFromUncertainAcknowledgedEffects(bool afterProvider)
    {
        var desired = MigrationTestHarness.Entity("Product", "Id");
        desired.Indexes.Add(new EntityIndex { Name = "IX_Test", Columns = new() { "Id" } });
        var setup = Build(existing: MigrationTestHarness.Entity("Product", "Id"), desired: desired, indexes: true);
        var forward = setup.Manager.ExecuteMigrationPlan(setup.Plan);
        Assert.True(forward.Success, forward.Message);
        setup.Harness.ProviderCalls.Clear();
        setup.Store.AppendOverride = record =>
        {
            var cp = new JsonLoader().DeserializeSnapshot<MigrationExecutionCheckpoint>(record.Notes);
            return cp.CompensationSteps.Any(s => s.Status == (afterProvider ? MigrationExecutionStepStatus.Completed : MigrationExecutionStepStatus.Running))
                ? new(PersistenceWriteStatus.Failed) : null;
        };
        var rollback = setup.Manager.RollbackFailedExecution(forward.ExecutionToken, false);
        Assert.False(rollback.Success);
        Assert.Equal(afterProvider, rollback.RequiresReconciliation);
        Assert.Equal(afterProvider ? 1 : 0, rollback.AppliedCount);
        Assert.Equal(afterProvider ? 1 : 0, setup.Harness.ProviderCalls.Count);
    }

    // Programmatic drop operations are not emitted by the type planner; construct approved test intent using its real hash.
    private static void SealFixturePlan(MigrationPlanArtifact plan) => plan.PlanHash = (string)typeof(MigrationManager)
        .GetMethod("ComputePlanHash", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { plan })!;
    private sealed class InlineProgress(Action action) : IProgress<PassedArgs> { public void Report(PassedArgs args) => action(); }

    [Fact]
    public async Task ActualManagerInChildProcess_HoldsTargetAndLeavesRunningEvidenceAfterCrash()
    {
        var setup = Build();
        var token = Guid.NewGuid().ToString("N");
        var worker = Path.Combine(AppContext.BaseDirectory, "persistence-worker", "FrameworkPersistenceWorker.dll");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { worker, "migration-execution-hold", _root, "testdb", setup.Plan.PlanId, token }) start.ArgumentList.Add(arg);
        using var child = Process.Start(start)!;
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            Assert.Equal("provider-entered", await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(45)));
            var checkpoint = setup.Manager.GetExecutionCheckpoint(token);
            Assert.Contains(checkpoint.Steps, s => s.Status == MigrationExecutionStepStatus.Running);
            Assert.Equal(MigrationAdmissionStatus.Busy, setup.Manager.ExecuteMigrationPlan(setup.Plan).OwnershipAdmissionStatus);
            var claim = setup.Store.ReadMigrationExecutionClaim(Target);
            Assert.False(setup.Store.ReconcileMigrationExecution(Target, claim.ClaimId, "operator", "live-evidence").IsSaved);
            child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
            var watch = Stopwatch.StartNew();
            MigrationExecutionResult denied;
            do
            {
                denied = setup.Manager.ExecuteMigrationPlan(setup.Plan);
                if (denied.OwnershipAdmissionStatus != MigrationAdmissionStatus.Busy) break;
                await Task.Delay(20);
            } while (watch.Elapsed < TimeSpan.FromSeconds(20));
            Assert.Equal(MigrationAdmissionStatus.RequiresReconciliation, denied.OwnershipAdmissionStatus);
            Assert.True(setup.Store.ReconcileMigrationExecution(Target, claim.ClaimId, "operator", "crash-evidence").IsSaved);
            var resumed = setup.Harness.Build().ResumeMigrationPlan(token);
            Assert.False(resumed.Success);
            Assert.True(resumed.RequiresReconciliation); // Clearing the ownership claim does not prove the DDL outcome.
            Assert.Empty(setup.Harness.ProviderCalls);
        }
        finally
        {
            if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); }
            await errors;
        }
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
