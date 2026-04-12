using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Caching;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace Assembly_helpers.IntegrationTests;

public class FormsManagerLovDatasourceIntegrationTests
{
    [Fact]
    public async Task ShowLOVAsync_UsesConcreteDatasource_CachesRecords_AndPopulatesMappedFields()
    {
        var editor = new Mock<IDMEEditor>();
        var logger = new Mock<IDMLogger>();
        var dataSource = CreateCachedMemoryDatasource(editor.Object, logger.Object);
        editor.Setup(work => work.GetDataSource("LovCache")).Returns(dataSource);

        var manager = CreateFormsManager(editor.Object);
        var currentOrder = new OrderInputRecord();
        var unitOfWork = CreateUnitOfWork(currentOrder);

        manager.RegisterBlock("Orders", unitOfWork.Object, CreateEntityStructure("Orders", "CustomerId", "CustomerName", "CustomerCity"));

        var lov = LOVDefinition.CreateLookup("CustomersLov", "LovCache", "Customers", "CustomerId", "CompanyName")
            .MapField("CompanyName", "CustomerName")
            .MapField("City", "CustomerCity");
        lov.UseCache = true;

        manager.LOV.RegisterLOV("Orders", "CustomerId", lov);

        var firstResult = await manager.ShowLOVAsync("Orders", "CustomerId");

        firstResult.Success.Should().BeTrue();
        firstResult.FromCache.Should().BeFalse();
        firstResult.TotalCount.Should().Be(2);

        var selectedRecord = firstResult.Records
            .Cast<Dictionary<string, object>>()
            .Single(record => string.Equals(record["CustomerId"]?.ToString(), "ALFKI", System.StringComparison.Ordinal));

        var secondResult = await manager.ShowLOVAsync("Orders", "CustomerId", selectedRecord: selectedRecord);

        secondResult.Success.Should().BeTrue();
        secondResult.FromCache.Should().BeTrue();
        currentOrder.CustomerId.Should().Be("ALFKI");
        currentOrder.CustomerName.Should().Be("Alfreds Futterkiste");
        currentOrder.CustomerCity.Should().Be("Berlin");

        var filteredResult = manager.LOV.FilterLOVData("Orders", "CustomerId", "alf");

        filteredResult.Success.Should().BeTrue();
        filteredResult.FromCache.Should().BeTrue();
        filteredResult.TotalCount.Should().Be(1);
    }

    private static CachedMemoryDataSource CreateCachedMemoryDatasource(IDMEEditor editor, IDMLogger logger)
    {
        var dataSource = new CachedMemoryDataSource("LovCache", logger, editor, DataSourceType.CachedMemory, new ErrorsInfo());
        var entity = new EntityStructure
        {
            EntityName = "Customers",
            DatasourceEntityName = "Customers",
            Fields = new List<EntityField>
            {
                new() { FieldName = "CustomerId", IsKey = true },
                new() { FieldName = "CompanyName" },
                new() { FieldName = "City" }
            }
        };
        entity.PrimaryKeys = new List<EntityField> { entity.Fields[0] };

        dataSource.Openconnection();
        dataSource.CreateEntityAs(entity);
        dataSource.InsertEntity("Customers", new Dictionary<string, object>
        {
            ["CustomerId"] = "ALFKI",
            ["CompanyName"] = "Alfreds Futterkiste",
            ["City"] = "Berlin"
        });
        dataSource.InsertEntity("Customers", new Dictionary<string, object>
        {
            ["CustomerId"] = "BONAP",
            ["CompanyName"] = "Bon app",
            ["City"] = "Marseille"
        });

        return dataSource;
    }

    private static FormsManager CreateFormsManager(IDMEEditor editor)
    {
        var configurationManager = new Mock<IConfigurationManager>();
        configurationManager.SetupProperty(manager => manager.Configuration, new UnitofWorksManagerConfiguration());

        var dirtyStateManager = new Mock<IDirtyStateManager>();
        dirtyStateManager.Setup(manager => manager.GetDirtyBlocks()).Returns(new List<string>());
        dirtyStateManager.Setup(manager => manager.CheckAndHandleUnsavedChangesAsync(It.IsAny<string>())).ReturnsAsync(true);

        return new FormsManager(
            editor,
            dirtyStateManager: dirtyStateManager.Object,
            configurationManager: configurationManager.Object);
    }

    private static Mock<IUnitofWork> CreateUnitOfWork(OrderInputRecord currentRecord)
    {
        var units = new ObservableBindingList<OrderInputRecord>(new[] { currentRecord });
        units.CurrentIndex = 0;

        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.Units).Returns(() => units);
        unitOfWork.SetupGet(work => work.CurrentItem).Returns(() => units.Current);
        unitOfWork.SetupGet(work => work.TotalItemCount).Returns(() => units.Count);
        unitOfWork.SetupGet(work => work.IsDirty).Returns(false);
        unitOfWork.SetupGet(work => work.EntityType).Returns(typeof(OrderInputRecord));

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

    public class OrderInputRecord : INotifyPropertyChanged
    {
        private string _customerId = string.Empty;
        private string _customerName = string.Empty;
        private string _customerCity = string.Empty;

        public string CustomerId
        {
            get => _customerId;
            set => SetField(ref _customerId, value);
        }

        public string CustomerName
        {
            get => _customerName;
            set => SetField(ref _customerName, value);
        }

        public string CustomerCity
        {
            get => _customerCity;
            set => SetField(ref _customerCity, value);
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