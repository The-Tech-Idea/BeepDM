using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using Xunit;

namespace Assembly_helpers.IntegrationTests;

public class FormsManagerLifecycleIntegrationTests
{
    [Fact]
    public async Task OpenQueryCommitAndCloseAsync_CompletesSingleBlockLifecycle()
    {
        var dirtyBlocks = new List<string>();
        var isDirty = false;
        var manager = CreateFormsManager(dirtyBlocks, () => isDirty, value => isDirty = value);
        var (unitOfWork, queryRecords) = CreateQueryableUnitOfWork(() => isDirty,
            new TestRecord { Id = 1, Name = "Alpha" },
            new TestRecord { Id = 2, Name = "Beta" });

        manager.RegisterBlock("Orders", unitOfWork.Object, CreateEntityStructure("Orders"));
        manager.Locking.SetLockMode("Orders", LockMode.Manual);

        var opened = await manager.OpenFormAsync("OrdersForm");

        opened.Should().BeTrue();
        manager.CurrentFormName.Should().Be("OrdersForm");

        var queryResult = await manager.ExecuteQueryAndEnterCrudModeAsync("Orders");

        queryResult.Flag.Should().Be(Errors.Ok);
        manager.GetBlockMode("Orders").Should().Be(DataBlockMode.CRUD);
        manager.GetRecordCount("Orders").Should().Be(2);
        manager.GetCurrentRecordInfo("Orders")!.CurrentIndex.Should().Be(0);
        queryRecords.Current.Should().BeSameAs(manager.GetCurrentRecord("Orders"));

        queryRecords.Current!.Name = "Alpha Updated";
        dirtyBlocks.Add("Orders");
        isDirty = true;
        await manager.Locking.LockCurrentRecordAsync("Orders");
        manager.Locking.GetLockedRecordCount("Orders").Should().Be(1);

        var commitResult = await manager.CommitFormAsync();

        commitResult.Flag.Should().Be(Errors.Ok);
        commitResult.Message.Should().Contain("committed successfully");
        manager.GetDirtyBlocks().Should().BeEmpty();
        manager.IsDirty.Should().BeFalse();
        manager.Locking.GetLockedRecordCount("Orders").Should().Be(0);

        var closed = await manager.CloseFormAsync();

        closed.Should().BeTrue();
        manager.CurrentFormName.Should().BeNull();
        manager.CurrentBlockName.Should().BeNull();
    }

    private static FormsManager CreateFormsManager(List<string> dirtyBlocks, System.Func<bool> getIsDirty, System.Action<bool> setIsDirty)
    {
        var editor = new Mock<IDMEEditor>();
        var configurationManager = new Mock<IConfigurationManager>();
        configurationManager.SetupProperty(manager => manager.Configuration, new UnitofWorksManagerConfiguration
        {
            ValidateBeforeCommit = false
        });

        var dirtyStateManager = new Mock<IDirtyStateManager>();
        dirtyStateManager.Setup(manager => manager.GetDirtyBlocks()).Returns(() => new List<string>(dirtyBlocks));
        dirtyStateManager.Setup(manager => manager.SaveDirtyBlocksAsync(It.IsAny<List<string>>())).ReturnsAsync(() =>
        {
            dirtyBlocks.Clear();
            setIsDirty(false);
            return true;
        });
        dirtyStateManager.Setup(manager => manager.RollbackDirtyBlocksAsync(It.IsAny<List<string>>())).ReturnsAsync(() =>
        {
            dirtyBlocks.Clear();
            setIsDirty(false);
            return true;
        });
        dirtyStateManager.Setup(manager => manager.CheckAndHandleUnsavedChangesAsync(It.IsAny<string>())).ReturnsAsync(true);

        return new FormsManager(
            editor.Object,
            dirtyStateManager: dirtyStateManager.Object,
            configurationManager: configurationManager.Object);
    }

    private static (Mock<IUnitofWork> UnitOfWork, ObservableBindingList<TestRecord> QueryRecords)
        CreateQueryableUnitOfWork(System.Func<bool> isDirtyProvider, params TestRecord[] queryItems)
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
        unitOfWork.SetupGet(work => work.IsDirty).Returns(() => isDirtyProvider());
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