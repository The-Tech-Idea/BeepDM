using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public sealed class OwnedFormTransactionTests
{
    public sealed class Row : Entity { public string Id { get; set; } = "row"; }
    private sealed class Harness : IDisposable
    {
        public Mock<IDataSource> Source { get; } = new();
        public Mock<IOwnedTransactionDataSource> Owned { get; }
        public UnitofWork<Row> Work { get; }
        public UnitOfWorkWrapper Wrapper { get; }
        public FormsManager Form { get; }
        public object? Owner;
        public Harness(bool supported = true)
        {
            Owned = Source.As<IOwnedTransactionDataSource>();
            Source.SetupGet(x => x.DatasourceType).Returns(DataSourceType.SqlServer);
            Source.SetupGet(x => x.DatasourceName).Returns("owned-form");
            Source.SetupGet(x => x.ErrorObject).Returns(new ErrorsInfo());
            Source.Setup(x => x.InsertEntity("Rows", It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Ok });
            Owned.SetupGet(x => x.SupportsOwnedTransactions).Returns(supported);
            Owned.Setup(x => x.OwnsTransaction(It.IsAny<object>())).Returns((object value) => ReferenceEquals(value, Owner));
            Owned.Setup(x => x.BeginOwnedTransaction(It.IsAny<object>())).Returns((object value) =>
            { Owner = value; return new ErrorsInfo { Flag = Errors.Ok }; });
            Owned.Setup(x => x.CommitOwnedTransaction(It.IsAny<object>())).Returns((object value) =>
            { Assert.Same(Owner, value); Owner = null; return new ErrorsInfo { Flag = Errors.Ok }; });
            Owned.Setup(x => x.RollbackOwnedTransaction(It.IsAny<object>())).Returns((object value) =>
            { Assert.Same(Owner, value); Owner = null; return new ErrorsInfo { Flag = Errors.Ok }; });
            var editor = new Mock<IDMEEditor>(); editor.SetupGet(x => x.ErrorObject).Returns(new ErrorsInfo());
            editor.Setup(x => x.GetDataSource("owned-form")).Returns(Source.Object);
            Work = new UnitofWork<Row>(editor.Object, "owned-form", "Rows", new EntityStructure
                { EntityName = "Rows", Fields = new List<EntityField> { new() { FieldName = "Id", Fieldtype = "System.String", IsKey = true } } }, "Id");
            Work.Add(new Row()); Wrapper = new UnitOfWorkWrapper(Work); Form = new FormsManager(editor.Object);
            Form.RegisterBlock("record", Wrapper, Work.EntityStructure); Form.GetBlock("record").Mode = DataBlockMode.CRUD;
        }
        public void Dispose() { Form.Dispose(); Wrapper.Dispose(); Work.Dispose(); }
    }
    [Fact]
    public async Task A_confirmed_refusal_releases_form_admission_without_rolling_back_a_foreign_owner()
    {
        using var h = new Harness(); var refuse = true;
        h.Owned.Setup(x => x.BeginOwnedTransaction(It.IsAny<object>())).Returns((object owner) =>
        {
            if (refuse) return (IErrorsInfo)new TransactionOwnershipRefusal("Another owner is active.");
            h.Owner = owner; return new ErrorsInfo { Flag = Errors.Ok };
        });
        var first = await h.Form.CommitFormWithOutcomeAsync();
        Assert.Equal(Errors.Failed, first.Flag); Assert.False(first.RequiresReconciliation); Assert.True(h.Work.IsDirty);
        h.Owned.Verify(x => x.RollbackOwnedTransaction(It.IsAny<object>()), Times.Never);
        h.Source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        refuse = false; var next = await h.Form.CommitFormWithOutcomeAsync();
        Assert.Equal(Errors.Ok, next.Flag); Assert.True(next.AllWritesCommitted);
        h.Source.Verify(x => x.InsertEntity("Rows", It.IsAny<object>()), Times.Once);
        h.Source.Verify(x => x.BeginTransaction(It.IsAny<TheTechIdea.Beep.Addin.PassedArgs>()), Times.Never);
    }
    [Fact]
    public async Task An_uncertain_begin_stays_unresolved_and_cannot_be_automatically_replayed()
    {
        using var h = new Harness();
        h.Owned.Setup(x => x.BeginOwnedTransaction(It.IsAny<object>())).Throws(new IOException("Uncertain native begin"));
        h.Owned.Setup(x => x.RollbackOwnedTransaction(It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Failed, Message = "No acknowledged rollback" });
        Assert.True((await h.Form.CommitFormWithOutcomeAsync()).RequiresReconciliation);
        Assert.Equal(Errors.Failed, (await h.Form.CommitFormWithOutcomeAsync()).Flag);
        h.Owned.Verify(x => x.BeginOwnedTransaction(It.IsAny<object>()), Times.Once);
        h.Source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }
    [Fact]
    public async Task An_advertised_but_unimplemented_owner_cannot_fall_back_to_independent_writes()
    {
        using var h = new Harness(); h.Owned.Setup(x => x.BeginOwnedTransaction(It.IsAny<object>())).Throws<NotImplementedException>();
        h.Owned.Setup(x => x.RollbackOwnedTransaction(It.IsAny<object>())).Returns(new ErrorsInfo { Flag = Errors.Failed });
        var result = await h.Form.CommitFormWithOutcomeAsync();
        Assert.Equal(Errors.Failed, result.Flag); Assert.False(result.UsesIndependentCommits);
        h.Source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        h.Source.Verify(x => x.BeginTransaction(It.IsAny<TheTechIdea.Beep.Addin.PassedArgs>()), Times.Never);
    }
    [Fact]
    public async Task Unsupported_owner_capability_refuses_before_begin_or_write()
    {
        using var h = new Harness(false); var result = await h.Form.CommitFormWithOutcomeAsync();
        Assert.Equal(Errors.Failed, result.Flag); Assert.False(result.RequiresReconciliation);
        h.Owned.Verify(x => x.BeginOwnedTransaction(It.IsAny<object>()), Times.Never);
        h.Source.Verify(x => x.InsertEntity(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }
    [Fact]
    public async Task A_confirmed_form_commit_retains_returned_cleanup_warnings()
    {
        using var h = new Harness(); var warning = new IOException("Confirmed cleanup warning");
        h.Owned.Setup(x => x.CommitOwnedTransaction(It.IsAny<object>())).Returns((object owner) =>
        {
            Assert.Same(h.Owner, owner); h.Owner = null;
            return new ErrorsInfo { Flag = Errors.Ok, Errors = new() { new ErrorsInfo { Flag = Errors.Warning, Ex = warning } } };
        });
        var result = await h.Form.CommitFormWithOutcomeAsync(); Assert.Equal(Errors.Ok, result.Flag);
        Assert.Contains(result.Errors, error => error.Flag == Errors.Warning && ReferenceEquals(error.Ex, warning));
    }
}
