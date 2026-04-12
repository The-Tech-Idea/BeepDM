using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using Xunit;

namespace Assembly_helpers.IntegrationTests;

public class FormsManagerMasterDetailIntegrationTests
{
    [Fact]
    public async Task MasterCurrentChanged_SynchronizesDetailBlockUsingRelationshipFilters()
    {
        var manager = CreateFormsManager();
        var (masterUnitOfWork, masterUnits) = CreateMasterUnitOfWork(
            new CustomerRecord { CustomerId = "ALFKI", Name = "Alfreds" },
            new CustomerRecord { CustomerId = "BONAP", Name = "Bon app" });

        List<AppFilter>? lastFilters = null;
        var allDetails = new List<OrderRecord>
        {
            new() { OrderId = 1, CustomerId = "ALFKI", Description = "Order A" },
            new() { OrderId = 2, CustomerId = "ALFKI", Description = "Order B" },
            new() { OrderId = 3, CustomerId = "BONAP", Description = "Order C" }
        };
        var initialSync = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondSync = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var detailUnitOfWork = CreateDetailUnitOfWork(allDetails, filters =>
        {
            lastFilters = filters;
            if (!initialSync.Task.IsCompleted)
            {
                initialSync.TrySetResult(true);
            }
            else
            {
                secondSync.TrySetResult(true);
            }
        });

        manager.RegisterBlock("Customers", masterUnitOfWork.Object, CreateEntityStructure("Customers", "CustomerId"), isMasterBlock: true);
        manager.RegisterBlock("Orders", detailUnitOfWork.Object, CreateEntityStructure("Orders", "OrderId", "CustomerId"));
        manager.CreateMasterDetailRelation("Customers", "Orders", "CustomerId", "CustomerId");

        masterUnitOfWork.Raise(work => work.CurrentChanged += null, EventArgs.Empty);
        await initialSync.Task;

        lastFilters.Should().NotBeNull();
        HasEqualityFilter(lastFilters!, "CustomerId", "ALFKI").Should().BeTrue();
        detailUnitOfWork.Object.TotalItemCount.Should().Be(2);
        ((OrderRecord)detailUnitOfWork.Object.CurrentItem).CustomerId.Should().Be("ALFKI");

        masterUnits.MoveTo(1);
        masterUnitOfWork.Raise(work => work.CurrentChanged += null, EventArgs.Empty);
        await secondSync.Task;

        HasEqualityFilter(lastFilters!, "CustomerId", "BONAP").Should().BeTrue();
        detailUnitOfWork.Object.TotalItemCount.Should().Be(1);
        ((OrderRecord)detailUnitOfWork.Object.CurrentItem).CustomerId.Should().Be("BONAP");
    }

    private static FormsManager CreateFormsManager()
    {
        var editor = new Mock<IDMEEditor>();
        var configurationManager = new Mock<IConfigurationManager>();
        configurationManager.SetupProperty(manager => manager.Configuration, new UnitofWorksManagerConfiguration());

        var dirtyStateManager = new Mock<IDirtyStateManager>();
        dirtyStateManager.Setup(manager => manager.GetDirtyBlocks()).Returns(new List<string>());
        dirtyStateManager.Setup(manager => manager.CheckAndHandleUnsavedChangesAsync(It.IsAny<string>())).ReturnsAsync(true);

        return new FormsManager(
            editor.Object,
            dirtyStateManager: dirtyStateManager.Object,
            configurationManager: configurationManager.Object);
    }

    private static (Mock<IUnitofWork> UnitOfWork, ObservableBindingList<CustomerRecord> Units) CreateMasterUnitOfWork(params CustomerRecord[] records)
    {
        var units = new ObservableBindingList<CustomerRecord>(records);
        if (units.Count > 0)
        {
            units.CurrentIndex = 0;
        }

        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.Units).Returns(() => units);
        unitOfWork.SetupGet(work => work.CurrentItem).Returns(() => units.Current);
        unitOfWork.SetupGet(work => work.TotalItemCount).Returns(() => units.Count);
        unitOfWork.SetupGet(work => work.IsDirty).Returns(false);
        unitOfWork.SetupGet(work => work.EntityType).Returns(typeof(CustomerRecord));
        unitOfWork.Setup(work => work.Clear()).Callback(() => units.Clear());
        unitOfWork.Setup(work => work.MoveTo(It.IsAny<int>())).Callback<int>(index => units.MoveTo(index));
        unitOfWork.Setup(work => work.MoveFirst()).Callback(() => units.MoveFirst());
        unitOfWork.Setup(work => work.MoveNext()).Callback(() => units.MoveNext());
        unitOfWork.Setup(work => work.MovePrevious()).Callback(() => units.MovePrevious());
        unitOfWork.Setup(work => work.MoveLast()).Callback(() => units.MoveLast());

        return (unitOfWork, units);
    }

    private static Mock<IUnitofWork> CreateDetailUnitOfWork(IEnumerable<OrderRecord> allRecords, Action<List<AppFilter>> onGet)
    {
        var units = new ObservableBindingList<OrderRecord>();
        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.Units).Returns(() => units);
        unitOfWork.SetupGet(work => work.CurrentItem).Returns(() => units.Current);
        unitOfWork.SetupGet(work => work.TotalItemCount).Returns(() => units.Count);
        unitOfWork.SetupGet(work => work.IsDirty).Returns(false);
        unitOfWork.SetupGet(work => work.EntityType).Returns(typeof(OrderRecord));
        unitOfWork.Setup(work => work.Clear()).Callback(() => units = new ObservableBindingList<OrderRecord>());
        unitOfWork.Setup(work => work.Get(It.IsAny<List<AppFilter>>())).Returns<List<AppFilter>>(filters =>
        {
            onGet(filters);
            var customerId = filters.First(filter => string.Equals(filter.FieldName, "CustomerId", StringComparison.OrdinalIgnoreCase)).FilterValue;
            units = new ObservableBindingList<OrderRecord>(allRecords.Where(record => string.Equals(record.CustomerId, customerId, StringComparison.OrdinalIgnoreCase)).ToList());
            if (units.Count > 0)
            {
                units.CurrentIndex = 0;
            }
            return Task.FromResult<dynamic>(units);
        });

        return unitOfWork;
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

    private static bool HasEqualityFilter(IEnumerable<AppFilter> filters, string fieldName, string value)
    {
        return filters.Any(filter =>
            string.Equals(filter.FieldName, fieldName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(filter.Operator, "=", StringComparison.OrdinalIgnoreCase)
            && string.Equals(filter.FilterValue, value, StringComparison.Ordinal));
    }

    public class CustomerRecord : INotifyPropertyChanged
    {
        private string _customerId = string.Empty;
        private string _name = string.Empty;

        public string CustomerId
        {
            get => _customerId;
            set => SetField(ref _customerId, value);
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

    public class OrderRecord : INotifyPropertyChanged
    {
        private int _orderId;
        private string _customerId = string.Empty;
        private string _description = string.Empty;

        public int OrderId
        {
            get => _orderId;
            set => SetField(ref _orderId, value);
        }

        public string CustomerId
        {
            get => _customerId;
            set => SetField(ref _customerId, value);
        }

        public string Description
        {
            get => _description;
            set => SetField(ref _description, value);
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