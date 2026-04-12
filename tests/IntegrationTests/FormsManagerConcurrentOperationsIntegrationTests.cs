using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using Xunit;

namespace Assembly_helpers.IntegrationTests;

public class FormsManagerConcurrentOperationsIntegrationTests
{
    [Fact]
    public async Task NavigateToRecordAsync_OnDifferentBlocks_WithOverlappingCalls_KeepsBlockStateConsistent()
    {
        var overlapGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiters = 0;
        var manager = CreateFormsManager(async _ =>
        {
            if (Interlocked.Increment(ref waiters) == 2)
            {
                overlapGate.TrySetResult(true);
            }

            await overlapGate.Task;
            return true;
        });

        var (customersUnitOfWork, customerUnits) = CreateUnitOfWork(
            new NavigationRecord { Id = 1, Name = "Alpha" },
            new NavigationRecord { Id = 2, Name = "Beta" },
            new NavigationRecord { Id = 3, Name = "Gamma" });
        var (ordersUnitOfWork, orderUnits) = CreateUnitOfWork(
            new NavigationRecord { Id = 10, Name = "North" },
            new NavigationRecord { Id = 11, Name = "South" },
            new NavigationRecord { Id = 12, Name = "West" });

        manager.RegisterBlock("Customers", customersUnitOfWork.Object, CreateEntityStructure("Customers", "Id", "Name"));
        manager.RegisterBlock("Orders", ordersUnitOfWork.Object, CreateEntityStructure("Orders", "Id", "Name"));

        var customerNavigation = manager.NavigateToRecordAsync("Customers", 2);
        var orderNavigation = manager.NavigateToRecordAsync("Orders", 1);

        var results = await Task.WhenAll(customerNavigation, orderNavigation);

        results.Should().OnlyContain(success => success);
        waiters.Should().Be(2);

        manager.GetCurrentRecordInfo("Customers")!.CurrentIndex.Should().Be(2);
        manager.GetCurrentRecordInfo("Orders")!.CurrentIndex.Should().Be(1);
        manager.GetRecordCount("Customers").Should().Be(3);
        manager.GetRecordCount("Orders").Should().Be(3);
        customerUnits.Current!.Name.Should().Be("Gamma");
        orderUnits.Current!.Name.Should().Be("South");
    }

    private static FormsManager CreateFormsManager(System.Func<string, Task<bool>> unsavedChangesHandler)
    {
        var editor = new Mock<IDMEEditor>();
        var configurationManager = new Mock<IConfigurationManager>();
        configurationManager.SetupProperty(manager => manager.Configuration, new UnitofWorksManagerConfiguration());

        var dirtyStateManager = new Mock<IDirtyStateManager>();
        dirtyStateManager.Setup(manager => manager.GetDirtyBlocks()).Returns(new List<string>());
        dirtyStateManager.Setup(manager => manager.CheckAndHandleUnsavedChangesAsync(It.IsAny<string>()))
            .Returns<string>(blockName => unsavedChangesHandler(blockName));

        return new FormsManager(
            editor.Object,
            dirtyStateManager: dirtyStateManager.Object,
            configurationManager: configurationManager.Object);
    }

    private static (Mock<IUnitofWork> UnitOfWork, ObservableBindingList<NavigationRecord> Units) CreateUnitOfWork(params NavigationRecord[] records)
    {
        var units = new ObservableBindingList<NavigationRecord>(records);
        if (units.Count > 0)
        {
            units.CurrentIndex = 0;
        }

        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.Units).Returns(() => units);
        unitOfWork.SetupGet(work => work.CurrentItem).Returns(() => units.Current);
        unitOfWork.SetupGet(work => work.TotalItemCount).Returns(() => units.Count);
        unitOfWork.SetupGet(work => work.IsDirty).Returns(false);
        unitOfWork.SetupGet(work => work.EntityType).Returns(typeof(NavigationRecord));
        unitOfWork.Setup(work => work.MoveTo(It.IsAny<int>())).Callback<int>(index => units.MoveTo(index));
        unitOfWork.Setup(work => work.MoveFirst()).Callback(() => units.MoveFirst());
        unitOfWork.Setup(work => work.MoveNext()).Callback(() => units.MoveNext());
        unitOfWork.Setup(work => work.MovePrevious()).Callback(() => units.MovePrevious());
        unitOfWork.Setup(work => work.MoveLast()).Callback(() => units.MoveLast());
        unitOfWork.Setup(work => work.Clear()).Callback(() => units.Clear());

        return (unitOfWork, units);
    }

    private static EntityStructure CreateEntityStructure(string entityName, params string[] fieldNames)
    {
        var entity = new EntityStructure
        {
            EntityName = entityName,
            DatasourceEntityName = entityName,
            Fields = new List<EntityField>()
        };

        foreach (var fieldName in fieldNames)
        {
            entity.Fields.Add(new EntityField { FieldName = fieldName });
        }

        return entity;
    }

    public class NavigationRecord : INotifyPropertyChanged
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