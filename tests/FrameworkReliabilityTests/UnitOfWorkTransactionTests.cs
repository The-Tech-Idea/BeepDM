using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public class UnitOfWorkTransactionTests
{
    public sealed class Row : Entity
    {
        private int _id;
        private string _name = "initial";
        public int Id { get => _id; set => SetProperty(ref _id, value); }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
    }

    private static (UnitofWork<Row> Uow, Mock<IDataSource> Source) Harness()
    {
        var source = new Mock<IDataSource>();
        source.SetupGet(x => x.DatasourceType).Returns(DataSourceType.SqlLite);
        source.Setup(x => x.BeginTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        source.Setup(x => x.EndTransaction(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        source.Setup(x => x.Commit(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        source.Setup(x => x.InsertEntity("Rows", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        source.Setup(x => x.UpdateEntity("Rows", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        source.Setup(x => x.DeleteEntity("Rows", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var editor = new Mock<IDMEEditor>();
        editor.Setup(x => x.GetDataSource("database")).Returns(source.Object);
        var uow = new UnitofWork<Row>(editor.Object, "database", "Rows",
            new EntityStructure
            {
                EntityName = "Rows", Fields = new List<EntityField>
                {
                    new() { FieldName = "Id", IsKey = true, IsAutoIncrement = true }
                }
            }, "Id")
        {
            Units = new ObservableBindingList<Row>()
        };
        return (uow, source);
    }

    private static (UnitofWork<Row> Uow, Mock<IDataSource> Source, Mock<IOwnedTransactionDataSource> Owned) OwnedHarness(bool supported = true)
    {
        var source = new Mock<IDataSource>(); var owned = source.As<IOwnedTransactionDataSource>();
        source.SetupGet(x => x.DatasourceType).Returns(DataSourceType.SqlServer);
        owned.SetupGet(x => x.SupportsOwnedTransactions).Returns(supported);
        source.Setup(x => x.InsertEntity("Rows", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        var editor = new Mock<IDMEEditor>(); editor.Setup(x => x.GetDataSource("database")).Returns(source.Object);
        var work = new UnitofWork<Row>(editor.Object, "database", "Rows", new EntityStructure
        { EntityName = "Rows", Fields = new List<EntityField> { new() { FieldName = "Id", IsKey = true } } }, "Id");
        work.Units.Add(new Row()); return (work, source, owned);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OwnedTransactionUsesOneOpaqueTokenAndNeverLegacyFinish(bool failWrite)
    {
        var (work, source, owned) = OwnedHarness(); object? captured = null;
        owned.Setup(x => x.BeginOwnedTransaction(It.IsAny<object>())).Returns((object owner) =>
        { captured = owner; return new ErrorsInfo { Flag = Errors.Ok }; });
        owned.Setup(x => x.CommitOwnedTransaction(It.IsAny<object>())).Returns((object owner) =>
        { Assert.Same(captured, owner); return new ErrorsInfo { Flag = Errors.Ok }; });
        owned.Setup(x => x.RollbackOwnedTransaction(It.IsAny<object>())).Returns((object owner) =>
        { Assert.Same(captured, owner); return new ErrorsInfo { Flag = Errors.Ok }; });
        if (failWrite) source.Setup(x => x.InsertEntity("Rows", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Failed });
        Assert.Equal(failWrite ? Errors.Failed : Errors.Ok, (await work.Commit()).Flag); Assert.NotNull(captured);
        owned.Verify(x => x.CommitOwnedTransaction(It.IsAny<object>()), failWrite ? Times.Never : Times.Once);
        owned.Verify(x => x.RollbackOwnedTransaction(It.IsAny<object>()), failWrite ? Times.Once : Times.Never);
        source.Verify(x => x.BeginTransaction(It.IsAny<PassedArgs>()), Times.Never);
        source.Verify(x => x.Commit(It.IsAny<PassedArgs>()), Times.Never);
        source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Never);
    }

    [Fact]
    public async Task AcknowledgedOwnedCommitPreservesCleanupWarningsWithoutRollingBack()
    {
        var (work, source, owned) = OwnedHarness();
        owned.Setup(x => x.BeginOwnedTransaction(It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
        owned.Setup(x => x.CommitOwnedTransaction(It.IsAny<object>())).Returns(new ErrorsInfo
        { Flag = Errors.Ok, Errors = new List<IErrorsInfo> { new ErrorsInfo { Flag = Errors.Warning, Message = "Confirmed cleanup warning" } } });
        var result = await work.Commit(); Assert.Equal(Errors.Ok, result.Flag); Assert.False(work.IsDirty);
        Assert.Contains(result.Errors, x => x.Message == "Confirmed cleanup warning");
        owned.Verify(x => x.RollbackOwnedTransaction(It.IsAny<object>()), Times.Never);
        source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Never);
    }

    [Fact]
    public async Task FailedOwnedBeginCleanupDoesNotReplaceTheOriginalFailure()
    {
        var (work, source, owned) = OwnedHarness();
        owned.Setup(x => x.BeginOwnedTransaction(It.IsAny<object>())).Throws(new IOException("Original owned begin failure"));
        owned.Setup(x => x.OwnsTransaction(It.IsAny<object>())).Returns(true);
        owned.Setup(x => x.RollbackOwnedTransaction(It.IsAny<object>())).Throws(new IOException("Owned begin cleanup failure"));
        var result = await work.Commit(); Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("Original owned begin failure", result.Message);
        Assert.Contains(result.Errors, x => x.Ex is IOException && x.Ex.Message == "Owned begin cleanup failure");
        source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Never);
    }

    [Fact]
    public async Task UnsupportedOwnedCapabilityCannotSilentlyFallBackToLegacyTransactions()
    {
        var (work, source, owned) = OwnedHarness(false);
        Assert.Equal(Errors.Failed, (await work.Commit()).Flag); Assert.True(work.IsDirty);
        owned.Verify(x => x.BeginOwnedTransaction(It.IsAny<object>()), Times.Never);
        source.Verify(x => x.BeginTransaction(It.IsAny<PassedArgs>()), Times.Never);
        source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task ThrowingOwnedBeginResolvesOnlyTheTokenItActuallyAllocated()
    {
        var (work, source, owned) = OwnedHarness(); object? captured = null;
        owned.Setup(x => x.BeginOwnedTransaction(It.IsAny<object>())).Returns((object owner) =>
        { captured = owner; throw new IOException("Fictional allocated begin failure"); });
        owned.Setup(x => x.OwnsTransaction(It.IsAny<object>())).Returns((object owner) => ReferenceEquals(captured, owner));
        owned.Setup(x => x.RollbackOwnedTransaction(It.IsAny<object>())).Returns((object owner) =>
        { Assert.Same(captured, owner); return new ErrorsInfo { Flag = Errors.Ok }; });
        Assert.Equal(Errors.Failed, (await work.Commit()).Flag); Assert.True(work.IsDirty);
        owned.Verify(x => x.RollbackOwnedTransaction(It.IsAny<object>()), Times.Once);
        source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DiscardingPendingEditsNeverEndsADatasourceTransaction(bool transactionWouldRefuse)
    {
        var (uow, source) = Harness();
        var existing = new Row { Id = 12 }; uow.Units.Add(existing); uow.Units.AcceptChanges();
        existing.Name = "unsaved edit"; uow.Units.Add(new Row());
        if (transactionWouldRefuse)
            source.Setup(x => x.EndTransaction(It.IsAny<PassedArgs>())).Throws(new IOException("Another owner controls this transaction"));
        var result = await uow.Rollback();
        Assert.Equal(Errors.Ok, result.Flag); Assert.False(uow.IsDirty);
        Assert.Same(existing, Assert.Single(uow.Units)); Assert.Equal("initial", existing.Name);
        source.Verify(x => x.BeginTransaction(It.IsAny<PassedArgs>()), Times.Never);
        source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Never);
        source.Verify(x => x.Commit(It.IsAny<PassedArgs>()), Times.Never);
    }

    [Fact]
    public async Task DiscardHoldsAdmissionWhileRestoringRowsAndRejectsReentrantCommit()
    {
        var (uow, source) = Harness(); var row = new Row { Id = 12 };
        uow.Units.Add(row); uow.Units.AcceptChanges(); row.Name = "unsaved edit";
        Errors? reentrant = null;
        row.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Row.Name))
                reentrant = uow.Commit().GetAwaiter().GetResult().Flag;
        };
        Assert.Equal(Errors.Ok, (await uow.Rollback()).Flag);
        Assert.Equal(Errors.Failed, reentrant); Assert.Equal("initial", row.Name); Assert.False(uow.IsDirty);
        source.Verify(x => x.BeginTransaction(It.IsAny<PassedArgs>()), Times.Never);
        source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Never);
        row.Name = "later edit"; Assert.Equal(Errors.Ok, (await uow.Commit()).Flag);
    }

    [Fact]
    public async Task FailedRecordRollsBackWithoutAcceptingEarlierSuccessfulRecords()
    {
        var (uow, source) = Harness();
        var first = new Row();
        var second = new Row();
        uow.Units.Add(first);
        uow.Units.Add(second);
        source.Setup(x => x.InsertEntity("Rows", second)).Returns(new ErrorsInfo { Flag = Errors.Failed, Message = "constraint violation" });
        int saved = 0;
        uow.Units.AfterSave += (_, _) => saved++;
        var result = await uow.Commit();
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("constraint violation", result.Message);
        Assert.Equal(2, uow.Units.GetPendingChanges().Added.Count);
        Assert.Equal(0, saved);
        source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Once);
        source.Verify(x => x.Commit(It.IsAny<PassedArgs>()), Times.Never);
    }

    [Fact]
    public async Task CommitFailureKeepsUpdatesAndDeletedRowsPending()
    {
        var (uow, source) = Harness();
        var updated = new Row { Id = 1 };
        var deleted = new Row { Id = 2 };
        uow.Units.Add(updated);
        uow.Units.Add(deleted);
        uow.Units.AcceptChanges();
        updated.Name = "edited";
        uow.Units.Remove(deleted);
        source.Setup(x => x.Commit(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Failed, Message = "commit rejected" });
        var result = await uow.Commit();
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Single(uow.Units.GetPendingChanges().Modified);
        Assert.Single(uow.Units.GetPendingChanges().Deleted);
        Assert.Equal("edited", updated.Name);
        Assert.Equal("initial", uow.Units.GetTrackingItem(updated).OriginalValues["Name"]);
        source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GeneratedIdentityIsAcceptedOnCommitAndRestoredOnRollback(bool success)
    {
        var (uow, source) = Harness();
        var row = new Row();
        uow.Units.Add(row);
        source.Setup(x => x.InsertEntity("Rows", row)).Returns(() =>
        {
            row.Id = 99;
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        source.Setup(x => x.Commit(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = success ? Errors.Ok : Errors.Failed });
        var result = await uow.Commit();
        Assert.Equal(success ? Errors.Ok : Errors.Failed, result.Flag);
        Assert.Equal(success ? 99 : 0, row.Id);
        Assert.Equal(!success, uow.IsDirty);
        Assert.Equal(success ? 0 : 1, uow.Units.GetPendingChanges().Added.Count);
    }

    [Fact]
    public async Task RollbackExceptionDoesNotHideOriginalCommitFailure()
    {
        var (uow, source) = Harness();
        uow.Units.Add(new Row());
        source.Setup(x => x.Commit(It.IsAny<PassedArgs>())).Returns(new ErrorsInfo { Flag = Errors.Failed, Message = "primary failure" });
        source.Setup(x => x.EndTransaction(It.IsAny<PassedArgs>())).Throws(new IOException("rollback failure"));
        var result = await uow.Commit();
        Assert.Contains("primary failure", result.Message);
        Assert.Contains("rollback failure", Assert.Single(result.Errors).Message);
        Assert.True(uow.IsDirty);
    }

    [Fact]
    public async Task EditDuringCommitRemainsPendingAsUpdateNotAnotherInsert()
    {
        var (uow, source) = Harness();
        var row = new Row();
        uow.Units.Add(row);
        source.Setup(x => x.Commit(It.IsAny<PassedArgs>())).Returns(() =>
        {
            row.Name = "changed while committing";
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        var result = await uow.Commit();
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Empty(uow.Units.GetPendingChanges().Added);
        Assert.Single(uow.Units.GetPendingChanges().Modified);
        Assert.Equal("initial", uow.Units.GetTrackingItem(row).OriginalValues["Name"]);
    }

    [Fact]
    public async Task AfterSaveRunsOnlyAfterDatabaseCommitAndCannotLoseOtherAcceptance()
    {
        var (uow, source) = Harness();
        uow.Units.Add(new Row());
        uow.Units.Add(new Row());
        bool committed = false;
        int saved = 0;
        source.Setup(x => x.Commit(It.IsAny<PassedArgs>())).Returns(() =>
        {
            committed = true;
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        uow.Units.AfterSave += (_, _) =>
        {
            Assert.True(committed);
            saved++;
            throw new InvalidOperationException("consumer callback failed");
        };
        var result = await uow.Commit();
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.False(uow.IsDirty);
        Assert.Equal(2, saved);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, error => Assert.Equal(Errors.Warning, error.Flag));
    }

    [Fact]
    public async Task DeferredCollectionCommitCanBeDiscardedAndRetried()
    {
        var rows = new ObservableBindingList<Row>();
        rows.Add(new Row());
        Task<IErrorsInfo> Write(Row _) => Task.FromResult<IErrorsInfo>(new ErrorsInfo { Flag = Errors.Ok });
        var first = await rows.CommitAllAsync(Write, Write, Write, CommitOrder.DeletesFirst, false);
        Assert.True(rows.HasChanges);
        rows.DiscardCommit(first);
        var second = await rows.CommitAllAsync(Write, Write, Write, CommitOrder.DeletesFirst, false);
        rows.AcceptCommit(second);
        Assert.False(rows.HasChanges);
        Assert.Throws<InvalidOperationException>(() => rows.AcceptCommit(first));
    }

    [Fact]
    public async Task LaterEditsAfterSuccessfulCommitAreIncludedInTheNextCommit()
    {
        var (uow, source) = Harness();
        var row = new Row();
        uow.Units.Add(row);
        Assert.Equal(Errors.Ok, (await uow.Commit()).Flag);
        row.Name = "second save";
        Assert.True(uow.IsDirty);
        Assert.Single(uow.Units.GetPendingChanges().Modified);
        Assert.Equal("initial", uow.Units.GetTrackingItem(row).OriginalValues["Name"]);
        Assert.Equal(Errors.Ok, (await uow.Commit()).Flag);
        source.Verify(x => x.UpdateEntity("Rows", row), Times.Once);
        source.Verify(x => x.InsertEntity("Rows", row), Times.Once);
    }

    [Fact]
    public void RepeatedRejectPreservesTheAcceptedBaseline()
    {
        var rows = new ObservableBindingList<Row>();
        var row = new Row();
        rows.Add(row);
        rows.AcceptChanges();
        row.Name = "first edit";
        rows.RejectChanges();
        row.Name = "second edit";
        rows.RejectChanges();
        Assert.Equal("initial", row.Name);
    }

    [Fact]
    public async Task PostCommitExceptionIsWarningNotDatabaseFailure()
    {
        var (uow, _) = Harness();
        uow.Units.Add(new Row());
        uow.PostCommit += (_, _) => throw new InvalidOperationException("notification failed");
        var result = await uow.Commit();
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.False(uow.IsDirty);
        Assert.Equal(Errors.Warning, Assert.Single(result.Errors).Flag);
    }

    [Fact]
    public async Task LaterBeforeSaveExceptionRollsBackEarlierGeneratedIdentity()
    {
        var (uow, source) = Harness();
        var first = new Row();
        var second = new Row();
        uow.Units.Add(first);
        uow.Units.Add(second);
        source.Setup(x => x.InsertEntity("Rows", first)).Returns(() =>
        {
            first.Id = 77;
            return new ErrorsInfo { Flag = Errors.Ok };
        });
        uow.Units.BeforeSave += (_, args) =>
        {
            if (ReferenceEquals(args.Item, second)) throw new InvalidOperationException("before save failed");
        };
        var result = await uow.Commit();
        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(0, first.Id);
        Assert.Equal(2, uow.Units.GetPendingChanges().Added.Count);
        source.Verify(x => x.EndTransaction(It.IsAny<PassedArgs>()), Times.Once);
    }
}
