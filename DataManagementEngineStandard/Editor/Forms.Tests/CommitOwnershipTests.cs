using System.Collections.Concurrent;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class CommitOwnershipTests
{
    public sealed class Row : Entity
    {
        private int _id;
        public int Id { get => _id; set => SetProperty(ref _id, value); }
        private string _name = "initial";
        public string Name { get => _name; set => SetProperty(ref _name, value); }
    }

    private static EntityStructure Structure(string name) => new()
    {
        EntityName = name, Fields = new List<EntityField>
        {
            new() { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true, IsAutoIncrement = true },
            new() { FieldName = "Name", Fieldtype = "System.String" }
        }
    };

    private sealed class Provider
    {
        public readonly Mock<IDataSource> Source = new();
        public bool Open;
        public int Begins, Commits, Aborts, Writes;
        public Func<int, IErrorsInfo>? OnWrite;
        public Func<IErrorsInfo>? OnCommit;
        public Func<IErrorsInfo>? OnBegin;
        public Func<IErrorsInfo>? OnAbort;
        public Provider()
        {
            Source.SetupGet(s => s.DatasourceType).Returns(DataSourceType.SqlLite);
            Source.SetupGet(s => s.DatasourceName).Returns("database");
            Source.Setup(s => s.BeginTransaction(It.IsAny<PassedArgs>())).Returns(() =>
            {
                Begins++;
                if (Open) throw new InvalidOperationException("Nested transaction rejected");
                Open = true;
                return OnBegin?.Invoke() ?? Ok();
            });
            Source.Setup(s => s.Commit(It.IsAny<PassedArgs>())).Returns(() =>
            {
                Commits++;
                Assert.True(Open);
                var result = OnCommit != null ? OnCommit() : Ok();
                if (result?.Flag == Errors.Ok) Open = false;
                return result!;
            });
            Source.Setup(s => s.EndTransaction(It.IsAny<PassedArgs>())).Returns(() =>
            {
                Aborts++;
                var result = OnAbort != null ? OnAbort() : Ok();
                if (result?.Flag == Errors.Ok) Open = false;
                return result!;
            });
            Source.Setup(s => s.InsertEntity(It.IsAny<string>(), It.IsAny<object>())).Returns((string _, object item) =>
            {
                Assert.True(Open);
                Writes++;
                ((Row)item).Id = 100 + Writes;
                return OnWrite?.Invoke(Writes) ?? Ok();
            });
        }
    }

    private static ErrorsInfo Ok() => new() { Flag = Errors.Ok };
    private static ErrorsInfo Fail(string text) => new() { Flag = Errors.Failed, Message = text };

    private sealed class SafeFailure : ErrorsInfo, ISafeWriteRetry
    {
        public bool IsSafeToRetry => true;
    }

    private static UnitofWork<Row> Register(FormsManager manager, IDMEEditor editor, Provider provider, string name)
    {
        var uow = new UnitofWork<Row>(editor, "database", name, Structure(name), "Id")
        {
            DataSource = provider.Source.Object, Units = new ObservableBindingList<Row>()
        };
        uow.Units.Add(new Row());
        manager.RegisterBlock(name, new UnitOfWorkWrapper(uow), Structure(name));
        manager.GetBlock(name).Mode = DataBlockMode.CRUD;
        return uow;
    }

    [Fact]
    public async Task TwoRealUowsShareExactlyOneTransactionAndAcceptOnlyAfterCommit()
    {
        var provider = new Provider();
        var editor = new Mock<IDMEEditor>().Object;
        using var manager = new FormsManager(editor);
        using var first = Register(manager, editor, provider, "A");
        using var second = Register(manager, editor, provider, "B");
        var notifications = 0;
        first.Units.AfterSave += (_, _) => { Assert.False(provider.Open); notifications++; };
        second.Units.AfterSave += (_, _) => { Assert.False(provider.Open); notifications++; };
        provider.OnCommit = () =>
        {
            Assert.True(first.IsDirty);
            Assert.True(second.IsDirty);
            Assert.Equal(0, notifications);
            return Ok();
        };
        var result = Assert.IsType<FormCommitResult>(await manager.CommitFormAsync());
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.True(result.AllWritesCommitted);
        Assert.False(result.UsesIndependentCommits);
        Assert.Equal(1, provider.Begins);
        Assert.Equal(1, provider.Commits);
        Assert.Equal(0, provider.Aborts);
        Assert.Equal(2, notifications);
        Assert.False(first.IsDirty);
        Assert.False(second.IsDirty);
    }

    [Fact]
    public async Task SecondBlockFailureRollsBackBothAndRestoresGeneratedKeys()
    {
        var provider = new Provider { OnWrite = n => n == 2 ? Fail("constraint") : Ok() };
        var editor = new Mock<IDMEEditor>().Object;
        using var manager = new FormsManager(editor);
        using var first = Register(manager, editor, provider, "A");
        using var second = Register(manager, editor, provider, "B");
        var result = Assert.IsType<FormCommitResult>(await manager.CommitFormAsync());
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.All(result.Blocks, b => Assert.Equal(FormBlockCommitState.RolledBack, b.State));
        Assert.True(first.IsDirty);
        Assert.True(second.IsDirty);
        Assert.Equal(0, first.Units[0].Id);
        Assert.Equal(0, second.Units[0].Id);
        Assert.Equal(0, provider.Commits);
        Assert.Equal(1, provider.Aborts);
        Assert.False(provider.Open);
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("null")]
    [InlineData("throw")]
    public async Task UnconfirmedCommitRetainsTrackingAndBlocksReplayUntilReconciled(string mode)
    {
        var provider = new Provider { OnCommit = () => mode == "throw" ? throw new IOException("lost acknowledgement") :
            mode == "null" ? null! : Fail("unknown durability") };
        var editor = new Mock<IDMEEditor>().Object;
        using var manager = new FormsManager(editor);
        using var uow = Register(manager, editor, provider, "A");
        var result = Assert.IsType<FormCommitResult>(await manager.CommitFormAsync());
        var block = Assert.Single(result.Blocks);
        Assert.Equal(FormBlockCommitState.Unknown, block.State);
        Assert.NotNull(block.Reconciliation);
        Assert.True(result.RequiresReconciliation);
        Assert.True(uow.IsDirty);
        Assert.Equal(Errors.Failed, (await uow.Commit()).Flag);
        Assert.Equal(Errors.Failed, (await uow.Rollback()).Flag);
        Assert.Equal(1, provider.Writes);
        Assert.Equal(1, provider.Aborts);
        var beforeRetry = provider.Begins;
        var repeated = await manager.CommitFormWithOutcomeAsync();
        Assert.Equal(Errors.Failed, repeated.Flag);
        Assert.True(repeated.RequiresReconciliation);
        Assert.Equal(beforeRetry, provider.Begins);
        Assert.Equal(1, provider.Writes);
        Assert.Equal(Errors.Ok, block.Reconciliation.Complete(UnitofWorkCommitOutcome.RolledBack).Flag);
        Assert.Equal(0, uow.Units[0].Id);
        Assert.True(uow.IsDirty);
    }

    [Fact]
    public async Task NewEditDuringProviderCommitRemainsDirtyAndFormStatusChanged()
    {
        var provider = new Provider();
        var editor = new Mock<IDMEEditor>().Object;
        var variables = new Mock<ISystemVariablesManager>();
        using var manager = new FormsManager(editor, systemVariablesManager: variables.Object);
        using var uow = Register(manager, editor, provider, "A");
        provider.OnCommit = () => { uow.Units[0].Name = "new unsaved edit"; return Ok(); };
        var result = await manager.CommitFormWithOutcomeAsync();
        Assert.True(result.AllWritesCommitted);
        Assert.True(uow.IsDirty);
        Assert.Single(uow.Units.GetPendingChanges().Modified);
        variables.Verify(v => v.SetBlockStatus("A", "CHANGED"), Times.AtLeastOnce);
        variables.Verify(v => v.SetFormStatus("CHANGED"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LaterProviderBeginThrowsAndCleansUpEveryAttemptedBegin()
    {
        var first = new Provider();
        var second = new Provider();
        var attempts = 0;
        first.OnBegin = second.OnBegin = () => ++attempts == 2 ? throw new IOException("begin failed after allocation") : Ok();
        var editor = new Mock<IDMEEditor>().Object;
        using var manager = new FormsManager(editor);
        using var a = Register(manager, editor, first, "A");
        using var b = Register(manager, editor, second, "B");
        var result = Assert.IsType<FormCommitResult>(await manager.CommitFormAsync());
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, first.Writes + second.Writes);
        Assert.Equal(2, attempts);
        Assert.Equal(first.Begins, first.Aborts);
        Assert.Equal(second.Begins, second.Aborts);
        Assert.False(first.Open);
        Assert.False(second.Open);
    }

    [Fact]
    public async Task FailedBeginCleanupIsExplicitAndProviderCannotBeAutomaticallyReused()
    {
        var provider = new Provider { OnBegin = () => Fail("begin failed"), OnAbort = () => Fail("cleanup failed") };
        var editor = new Mock<IDMEEditor>().Object;
        using var manager = new FormsManager(editor);
        using var uow = Register(manager, editor, provider, "A");
        var outcome = await manager.CommitFormWithOutcomeAsync();
        Assert.True(outcome.RequiresReconciliation);
        Assert.Equal(FormDataSourceCommitState.Unknown, Assert.Single(outcome.DataSources).State);
        Assert.Equal(FormBlockCommitState.Unattempted, Assert.Single(outcome.Blocks).State);
        var retry = await manager.CommitFormWithOutcomeAsync();
        Assert.Equal(Errors.Failed, retry.Flag);
        Assert.Equal(1, provider.Begins);
        Assert.Equal(0, provider.Writes);
    }

    [Fact]
    public async Task FailedRollbackRetainsReconciliationInsteadOfAcceptingOrRestoringKeys()
    {
        var provider = new Provider { OnWrite = _ => Fail("constraint"), OnAbort = () => Fail("rollback unavailable") };
        var editor = new Mock<IDMEEditor>().Object;
        using var manager = new FormsManager(editor);
        using var uow = Register(manager, editor, provider, "A");
        var result = Assert.IsType<FormCommitResult>(await manager.CommitFormAsync());
        Assert.True(result.RequiresReconciliation);
        Assert.Equal(FormBlockCommitState.Unknown, Assert.Single(result.Blocks).State);
        Assert.NotEqual(0, uow.Units[0].Id);
        Assert.Contains(result.Errors, e => e.Message.Contains("cleanup failed"));
    }

    [Fact]
    public async Task PostCommitObserverFailureCannotBecomeWriteFailure()
    {
        var provider = new Provider();
        var editor = new Mock<IDMEEditor>().Object;
        using var manager = new FormsManager(editor);
        using var uow = Register(manager, editor, provider, "A");
        var calls = 0;
        manager.OnFormCommit += (_, _) => { if (++calls == 2) throw new InvalidOperationException("view closed"); };
        var result = Assert.IsType<FormCommitResult>(await manager.CommitFormAsync());
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.True(result.AllWritesCommitted);
        Assert.False(uow.IsDirty);
        Assert.Contains(result.Errors, e => e.Flag == Errors.Warning && e.Message == "view closed");
        Assert.Equal(0, provider.Aborts);
    }

    [Fact]
    public async Task WrapperForwardsEnlistmentAndDatasourceCannotChangeWhilePrepared()
    {
        var provider = new Provider();
        var editor = new Mock<IDMEEditor>().Object;
        using var manager = new FormsManager(editor);
        using var uow = Register(manager, editor, provider, "A");
        using var wrapper = new UnitOfWorkWrapper(uow);
        provider.Source.Object.BeginTransaction(new PassedArgs());
        var stage = await wrapper.PrepareCommitAsync(provider.Source.Object);
        Assert.Equal(Errors.Ok, stage.Result.Flag);
        stage.Result.Flag = Errors.Failed;
        Assert.Equal(Errors.Ok, stage.Result.Flag);
        Assert.Throws<ArgumentOutOfRangeException>(() => stage.Complete((UnitofWorkCommitOutcome)100));
        Assert.False(stage.IsResolved);
        Assert.Throws<InvalidOperationException>(() => uow.DataSource = new Mock<IDataSource>().Object);
        Assert.Throws<InvalidOperationException>(() => uow.Units = new ObservableBindingList<Row>());
        provider.Source.Object.EndTransaction(new PassedArgs());
        Assert.Equal(Errors.Ok, stage.Complete(UnitofWorkCommitOutcome.RolledBack).Flag);
        Assert.Equal(0, provider.Commits);
    }

    [Fact]
    public async Task MissingSaveTargetsAndThrownLookupsCannotReturnSuccess()
    {
        var blocks = new ConcurrentDictionary<string, DataBlockInfo>();
        var helper = new DirtyStateManager(new Mock<IDMEEditor>().Object, blocks, _ => new List<string>(), _ => null!,
            _ => new List<DataBlockRelationship>());
        Assert.False(await helper.SaveDirtyBlocksAsync(new List<string> { "gone" }));
        var results = await helper.SaveDirtyBlocksWithResultsAsync(new List<string> { "gone", "alsoGone" });
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.False(r.Success));
        var throwing = new DirtyStateManager(new Mock<IDMEEditor>().Object, blocks, _ => new List<string>(),
            _ => throw new IOException("lookup failed"), _ => new List<DataBlockRelationship>());
        Assert.False(await throwing.SaveDirtyBlocksAsync(new List<string> { "broken" }));
    }

    [Fact]
    public async Task ExplicitSafeFailureUsesConfiguredRetriesAndAccountsForEveryBlock()
    {
        var unit = new Mock<IUnitofWork>();
        unit.Setup(u => u.Commit()).ReturnsAsync(new SafeFailure { Flag = Errors.Failed, Message = "no writes occurred" });
        var blocks = new ConcurrentDictionary<string, DataBlockInfo>();
        blocks["A"] = new DataBlockInfo { BlockName = "A", UnitOfWork = unit.Object };
        blocks["B"] = new DataBlockInfo { BlockName = "B", UnitOfWork = new Mock<IUnitofWork>().Object };
        var helper = new DirtyStateManager(new Mock<IDMEEditor>().Object, blocks, _ => new List<string>(),
            name => blocks.GetValueOrDefault(name)!, _ => new List<DataBlockRelationship>(),
            () => new SaveOptions { MaxRetries = 2, RetryDelayMs = 0, StopOnFirstError = true });
        var results = await helper.SaveDirtyBlocksWithResultsAsync(new List<string> { "A", "B" });
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.False(r.Success));
        unit.Verify(u => u.Commit(), Times.Exactly(3));
    }

    [Fact]
    public async Task MidCommitFailurePreservesCommittedProviderAndAbortsRemainingProviders()
    {
        var providers = new[] { new Provider(), new Provider(), new Provider() };
        int commits = 0;
        foreach (var provider in providers) provider.OnCommit = () => ++commits == 2 ? Fail("lost acknowledgement") : Ok();
        var editor = new Mock<IDMEEditor>().Object;
        using var manager = new FormsManager(editor);
        using var a = Register(manager, editor, providers[0], "A");
        using var b = Register(manager, editor, providers[1], "B");
        using var c = Register(manager, editor, providers[2], "C");
        var result = Assert.IsType<FormCommitResult>(await manager.CommitFormAsync());
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(1, result.Blocks.Count(b => b.State == FormBlockCommitState.Committed));
        Assert.Equal(1, result.Blocks.Count(b => b.State == FormBlockCommitState.Unknown));
        Assert.Equal(1, result.Blocks.Count(b => b.State == FormBlockCommitState.RolledBack));
        Assert.Equal(2, providers.Sum(p => p.Aborts));
        Assert.All(providers, p => Assert.False(p.Open));
        Assert.Equal(2, providers.Sum(p => p.Commits));
    }

    [Fact]
    public async Task AnotherManagerCannotBeginOrAbortTheActiveFormsTransaction()
    {
        var provider = new Provider();
        var editor = new Mock<IDMEEditor>().Object;
        using var first = new FormsManager(editor);
        using var second = new FormsManager(editor);
        using var a = Register(first, editor, provider, "A");
        using var b = Register(second, editor, provider, "B");
        Task<FormCommitResult>? nested = null;
        provider.OnWrite = _ => { nested = second.CommitFormWithOutcomeAsync(); return Ok(); };
        var outcome = await first.CommitFormWithOutcomeAsync();
        Assert.True(outcome.AllWritesCommitted);
        Assert.NotNull(nested);
        Assert.Equal(Errors.Failed, (await nested).Flag);
        Assert.Equal(1, provider.Begins);
        Assert.Equal(1, provider.Commits);
        Assert.Equal(0, provider.Aborts);
        Assert.Equal(1, provider.Writes);
        Assert.True(b.IsDirty);
    }

    [Fact]
    public async Task ReplacedBlockBeforeProviderCommitAbortsCapturedWrites()
    {
        var provider = new Provider();
        var editor = new Mock<IDMEEditor>().Object;
        using var manager = new FormsManager(editor);
        using var a = Register(manager, editor, provider, "A");
        using var b = Register(manager, editor, provider, "B");
        provider.OnWrite = n =>
        {
            if (n == 2) manager.RegisterBlock("A", new Mock<IUnitofWork>().Object, Structure("A"));
            return Ok();
        };
        var outcome = await manager.CommitFormWithOutcomeAsync();
        Assert.Equal(Errors.Failed, outcome.Flag);
        Assert.All(outcome.Blocks, block => Assert.Equal(FormBlockCommitState.RolledBack, block.State));
        Assert.Equal(0, provider.Commits);
        Assert.Equal(1, provider.Aborts);
        Assert.True(a.IsDirty);
        Assert.True(b.IsDirty);
    }
}
