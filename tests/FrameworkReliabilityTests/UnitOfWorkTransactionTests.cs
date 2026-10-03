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
