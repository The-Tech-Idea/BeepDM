using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using Xunit;

namespace FormsManagerModeTransitions.Tests;

public class FormsManagerModeTransitionsTests
{
    [Fact]
    public async Task EnterQueryModeAsync_FromCrudMode_SetsQueryModeAndCurrentBlock()
    {
        var context = CreateFormsManager();
        var (unitOfWork, _) = CreateUnitOfWork();

        context.Manager.RegisterBlock("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));
        context.Manager.GetBlock("Customers")!.Mode = DataBlockMode.CRUD;

        var result = await context.Manager.EnterQueryModeAsync("Customers");

        result.Flag.Should().Be(Errors.Ok);
        context.Manager.GetBlockMode("Customers").Should().Be(DataBlockMode.Query);
        context.Manager.CurrentBlockName.Should().Be("Customers");
        unitOfWork.Verify(work => work.Clear(), Times.Once);
    }

    [Fact]
    public async Task EnterQueryModeAsync_WhenUnsavedChangesCannotBeResolved_Fails()
    {
        var context = CreateFormsManager();
        context.DirtyStateManager
            .Setup(manager => manager.CheckAndHandleUnsavedChangesAsync("Customers"))
            .ReturnsAsync(false);

        var (unitOfWork, _) = CreateUnitOfWork(isDirty: true);
        context.Manager.RegisterBlock("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));
        context.Manager.GetBlock("Customers")!.Mode = DataBlockMode.CRUD;

        var result = await context.Manager.EnterQueryModeAsync("Customers");

        result.Flag.Should().Be(Errors.Failed);
        context.Manager.GetBlockMode("Customers").Should().Be(DataBlockMode.CRUD);
        unitOfWork.Verify(work => work.Clear(), Times.Never);
    }

    [Fact]
    public async Task ExecuteQueryAndEnterCrudModeAsync_FromQueryMode_SetsCrudModeAndMovesToFirstRecord()
    {
        var context = CreateFormsManager();
        var (unitOfWork, queryRecords) = CreateQueryableUnitOfWork(
            new TestRecord { Id = 1, Name = "Alpha" },
            new TestRecord { Id = 2, Name = "Beta" });

        context.Manager.RegisterBlock("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));
        context.Manager.GetBlock("Customers")!.Mode = DataBlockMode.Query;

        var result = await context.Manager.ExecuteQueryAndEnterCrudModeAsync("Customers");

        result.Flag.Should().Be(Errors.Ok);
        context.Manager.GetBlockMode("Customers").Should().Be(DataBlockMode.CRUD);
        queryRecords.Count.Should().Be(2);
        context.Manager.GetRecordCount("Customers").Should().Be(2);
        context.Manager.GetCurrentRecordInfo("Customers")!.CurrentIndex.Should().Be(0);
    }

    [Fact]
    public async Task EnterCrudModeForNewRecordAsync_SimpleBlock_SetsCrudMode()
    {
        var context = CreateFormsManager();
        var (unitOfWork, _) = CreateUnitOfWork(entityType: typeof(TestRecord));

        context.Manager.RegisterBlock("Customers", unitOfWork.Object, CreateEntityStructure("Customers"));
        context.Manager.GetBlock("Customers")!.Mode = DataBlockMode.Query;

        var result = await context.Manager.EnterCrudModeForNewRecordAsync("Customers");

        result.Flag.Should().Be(Errors.Ok);
        context.Manager.GetBlockMode("Customers").Should().Be(DataBlockMode.CRUD);
        result.Message.Should().Contain("new record ready for data entry");
    }

    private static (FormsManager Manager, Mock<IDirtyStateManager> DirtyStateManager) CreateFormsManager()
    {
        var editor = new Mock<IDMEEditor>();
        var dirtyStateManager = new Mock<IDirtyStateManager>();
        dirtyStateManager
            .Setup(manager => manager.GetDirtyBlocks())
            .Returns(new List<string>());
        dirtyStateManager
            .Setup(manager => manager.CheckAndHandleUnsavedChangesAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var configurationManager = new Mock<IConfigurationManager>();
        configurationManager.SetupProperty(manager => manager.Configuration, new UnitofWorksManagerConfiguration());

        var manager = new FormsManager(
            editor.Object,
            dirtyStateManager: dirtyStateManager.Object,
            configurationManager: configurationManager.Object);

        return (manager, dirtyStateManager);
    }

    private static (Mock<IUnitofWork> UnitOfWork, ObservableBindingList<TestRecord> Units) CreateUnitOfWork(
        bool isDirty = false,
        System.Type? entityType = null)
    {
        var units = new ObservableBindingList<TestRecord>();
        var unitOfWork = new Mock<IUnitofWork>();

        unitOfWork.SetupGet(work => work.Units).Returns(() => units);
        unitOfWork.SetupGet(work => work.CurrentItem).Returns(() => units.Current);
        unitOfWork.SetupGet(work => work.TotalItemCount).Returns(() => units.Count);
        unitOfWork.SetupGet(work => work.IsDirty).Returns(isDirty);
        unitOfWork.SetupGet(work => work.EntityType).Returns(entityType ?? typeof(TestRecord));
        unitOfWork.Setup(work => work.Clear()).Callback(() => units.Clear());
        unitOfWork.Setup(work => work.MoveFirst()).Callback(() => units.MoveFirst());
        unitOfWork.Setup(work => work.MoveNext()).Callback(() => units.MoveNext());
        unitOfWork.Setup(work => work.MovePrevious()).Callback(() => units.MovePrevious());
        unitOfWork.Setup(work => work.MoveLast()).Callback(() => units.MoveLast());
        unitOfWork.Setup(work => work.MoveTo(It.IsAny<int>())).Callback<int>(index => units.MoveTo(index));

        return (unitOfWork, units);
    }

    private static (Mock<IUnitofWork> UnitOfWork, ObservableBindingList<TestRecord> QueryRecords)
        CreateQueryableUnitOfWork(params TestRecord[] queryItems)
    {
        var units = new ObservableBindingList<TestRecord>();
        var queryRecords = new ObservableBindingList<TestRecord>(queryItems);
        if (queryRecords.Count > 1)
        {
            queryRecords.CurrentIndex = 1;
        }

        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.Units).Returns(() => units);
        unitOfWork.SetupGet(work => work.CurrentItem).Returns(() => units.Current);
        unitOfWork.SetupGet(work => work.TotalItemCount).Returns(() => units.Count);
        unitOfWork.SetupGet(work => work.IsDirty).Returns(false);
        unitOfWork.SetupGet(work => work.EntityType).Returns(typeof(TestRecord));
        unitOfWork.Setup(work => work.Clear()).Callback(() => units = new ObservableBindingList<TestRecord>());
        unitOfWork.Setup(work => work.Get()).Returns(() =>
        {
            units = queryRecords;
            return Task.FromResult<dynamic>(queryRecords);
        });
        unitOfWork.Setup(work => work.MoveFirst()).Callback(() => units.MoveFirst());
        unitOfWork.Setup(work => work.MoveNext()).Callback(() => units.MoveNext());
        unitOfWork.Setup(work => work.MovePrevious()).Callback(() => units.MovePrevious());
        unitOfWork.Setup(work => work.MoveLast()).Callback(() => units.MoveLast());
        unitOfWork.Setup(work => work.MoveTo(It.IsAny<int>())).Callback<int>(index => units.MoveTo(index));

        return (unitOfWork, queryRecords);
    }

    private static EntityStructure CreateEntityStructure(string entityName)
    {
        return new EntityStructure
        {
            EntityName = entityName,
            DatasourceEntityName = entityName,
            Fields = new List<EntityField>()
        };
    }

    public class TestRecord : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}