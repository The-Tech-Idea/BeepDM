using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

public sealed class DecimalWideningAdmissionTests
{
    private const string Target = "fixture/sqlserver/decimal-target";
    private static EntityField Field(short scale) => new() { FieldName = "Amount", Fieldtype = typeof(decimal).FullName!, NumericPrecision = 28, NumericScale = scale, AllowDBNull = true };
    private static (MigrationTestHarness Harness, MigrationManager Manager, Mock<IDecimalColumnMigrationProvider> Provider) Build()
    {
        var harness = new MigrationTestHarness(); var manager = harness.Build();
        var standard = new Mock<ISchemaMigrationProvider>(); var provider = standard.As<IDecimalColumnMigrationProvider>();
        provider.SetupGet(x => x.DecimalMigrationTargetIdentity).Returns(Target);
        provider.Setup(x => x.WidenDecimalColumn(It.IsAny<string>(), It.IsAny<EntityField>(), It.IsAny<EntityField>(), Target))
            .Returns(SchemaMigrationResults.Ok("Confirmed"));
        Mock.Get(manager.DMEEditor).Setup(x => x.GetMigrationProvider(It.IsAny<IDataSource>())).Returns(standard.Object);
        return (harness, manager, provider);
    }
    [Fact]
    public void Saved_intent_and_outcome_release_the_existing_claim_as_completed()
    {
        var test = Build(); Assert.True(test.Manager.SupportsGuardedDecimalWidening);
        Assert.Equal(Errors.Ok, test.Manager.WidenDecimalColumn("Amounts", Field(0), Field(8)).Flag);
        Assert.Equal(new[] { "DecimalWidening.Intent.v1", "DecimalWidening.Outcome.v1" }, test.Harness.History.Migrations.Select(x => x.Name));
        Assert.Equal(MigrationClaimDisposition.Completed, test.Harness.ExecutionStorage.ReadMigrationExecutionClaim(Target).Disposition);
        Assert.Equal(Errors.Ok, test.Manager.CheckDecimalWideningReadiness().Flag);
    }
    [Fact]
    public void Failed_intent_acknowledgement_admits_no_native_operation()
    {
        var test = Build(); test.Harness.HistoryWrite = _ => new(PersistenceWriteStatus.Failed);
        Assert.Equal(Errors.Failed, test.Manager.WidenDecimalColumn("Amounts", Field(0), Field(8)).Flag);
        test.Provider.Verify(x => x.WidenDecimalColumn(It.IsAny<string>(), It.IsAny<EntityField>(), It.IsAny<EntityField>(), It.IsAny<string>()), Times.Never);
        Assert.Equal(MigrationClaimDisposition.SafeToRetry, test.Harness.ExecutionStorage.ReadMigrationExecutionClaim(Target).Disposition);
    }
    [Fact]
    public void Failed_outcome_acknowledgement_blocks_retry_and_boot_even_after_native_success()
    {
        var test = Build(); test.Harness.HistoryWrite = r => new(r.Name.Contains("Outcome") ? PersistenceWriteStatus.Failed : PersistenceWriteStatus.Saved);
        Assert.Equal(Errors.Failed, test.Manager.WidenDecimalColumn("Amounts", Field(0), Field(8)).Flag);
        Assert.Equal(Errors.Failed, test.Manager.CheckDecimalWideningReadiness().Flag);
        Assert.Equal(Errors.Failed, test.Manager.WidenDecimalColumn("Amounts", Field(0), Field(8)).Flag);
        test.Provider.Verify(x => x.WidenDecimalColumn(It.IsAny<string>(), It.IsAny<EntityField>(), It.IsAny<EntityField>(), It.IsAny<string>()), Times.Once);
        Assert.Equal(MigrationClaimState.RequiresReconciliation, test.Harness.ExecutionStorage.ReadMigrationExecutionClaim(Target).State);
    }
    [Fact]
    public void Provider_exception_keeps_the_captured_intent_and_requires_reconciliation()
    {
        var test = Build(); test.Provider.Setup(x => x.WidenDecimalColumn(It.IsAny<string>(), It.IsAny<EntityField>(), It.IsAny<EntityField>(), It.IsAny<string>())).Throws(new IOException("injected provider acknowledgement failure"));
        Assert.Equal(Errors.Failed, test.Manager.WidenDecimalColumn("Amounts", Field(0), Field(8)).Flag);
        Assert.Single(test.Harness.History.Migrations); Assert.Equal(Errors.Failed, test.Manager.CheckDecimalWideningReadiness().Flag);
    }
    [Fact]
    public void Storage_and_metadata_remain_captured_through_persistence_callbacks()
    {
        var test = Build(); var captured = test.Harness.ExecutionStorage; var replacement = new MigrationTestHarness().ExecutionStorage;
        var expected = Field(0); var desired = Field(8);
        test.Harness.HistoryWrite = r =>
        {
            test.Harness.ExecutionStorage = replacement; expected.NumericScale = 7; desired.NumericScale = 2;
            Assert.Throws<InvalidOperationException>(() => test.Manager.MigrateDataSource = null);
            return new(PersistenceWriteStatus.Saved);
        };
        Assert.Equal(Errors.Ok, test.Manager.WidenDecimalColumn("Amounts", expected, desired).Flag);
        test.Provider.Verify(x => x.WidenDecimalColumn("Amounts", It.Is<EntityField>(f => f.NumericScale == 0), It.Is<EntityField>(f => f.NumericScale == 8), Target), Times.Once);
        Assert.Equal(MigrationClaimDisposition.Completed, captured.ReadMigrationExecutionClaim(Target).Disposition);
        Assert.Null(replacement.ReadMigrationExecutionClaim(Target));
    }
    [Fact]
    public void Missing_captured_storage_and_unsupported_provider_do_not_fall_back_to_alter()
    {
        var test = Build(); test.Harness.ExecutionStorage = null!;
        Assert.Equal(Errors.Failed, test.Manager.WidenDecimalColumn("Amounts", Field(0), Field(8)).Flag);
        test.Provider.Verify(x => x.WidenDecimalColumn(It.IsAny<string>(), It.IsAny<EntityField>(), It.IsAny<EntityField>(), It.IsAny<string>()), Times.Never);
        var other = new MigrationTestHarness(); var manager = other.Build();
        Assert.False(manager.SupportsGuardedDecimalWidening);
        Assert.Equal(Errors.Failed, manager.WidenDecimalColumn("Amounts", Field(0), Field(8)).Flag);
        Assert.Empty(other.ProviderCalls); Assert.Empty(other.ExecutedSql);
    }
    [Fact]
    public async Task A_competing_manager_is_refused_before_native_invocation()
    {
        var first = Build(); var second = Build(); second.Harness.ExecutionStorage = first.Harness.ExecutionStorage;
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        first.Provider.Setup(x => x.WidenDecimalColumn(It.IsAny<string>(), It.IsAny<EntityField>(), It.IsAny<EntityField>(), It.IsAny<string>()))
            .Returns(() => { entered.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(20))); return SchemaMigrationResults.Ok(); });
        var work = Task.Run(() => first.Manager.WidenDecimalColumn("Amounts", Field(0), Field(8)));
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(20)));
            Assert.Equal(Errors.Failed, second.Manager.WidenDecimalColumn("Amounts", Field(0), Field(8)).Flag);
            second.Provider.Verify(x => x.WidenDecimalColumn(It.IsAny<string>(), It.IsAny<EntityField>(), It.IsAny<EntityField>(), It.IsAny<string>()), Times.Never);
        }
        finally { release.Set(); }
        Assert.Equal(Errors.Ok, (await work).Flag);
    }
}
