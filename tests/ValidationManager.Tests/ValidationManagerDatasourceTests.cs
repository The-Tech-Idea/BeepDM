using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using Xunit;
using ValidationManagerClass = TheTechIdea.Beep.Editor.UOWManager.Helpers.ValidationManager;

namespace FormsValidation.Tests;

public class ValidationManagerDatasourceTests
{
    [Fact]
    public void ValidateRecord_LookupRule_QueriesConfiguredLookupEntity()
    {
        var dataSource = CreateDataSource();
        dataSource
            .Setup(source => source.GetEntity(
                "Customers",
                It.Is<List<AppFilter>>(filters => HasEqualityFilter(filters, "CustomerId", "ALFKI"))))
            .Returns(new object[] { new Dictionary<string, object> { ["CustomerId"] = "ALFKI" } });

        var manager = new ValidationManagerClass();
        manager.SetDataSource(dataSource.Object);
        manager.RegisterRule(new ValidationRule
        {
            RuleName = "LookupCustomer",
            BlockName = "Orders",
            ItemName = "CustomerId",
            ValidationType = ValidationType.Lookup,
            LookupSource = "Customers|CustomerId",
            Timing = ValidationTiming.Manual
        });

        var result = manager.ValidateRecord("Orders", new Dictionary<string, object>
        {
            ["CustomerId"] = "ALFKI"
        });

        result.IsValid.Should().BeTrue();
        dataSource.VerifyAll();
    }

    [Fact]
    public void ValidateRecord_UniqueRule_IgnoresCurrentRecordUsingCompareFieldName()
    {
        var dataSource = CreateDataSource();
        dataSource
            .Setup(source => source.GetEntity(
                "Products",
                It.Is<List<AppFilter>>(filters => HasEqualityFilter(filters, "Code", "ABC-001"))))
            .Returns(new object[]
            {
                new Dictionary<string, object>
                {
                    ["Id"] = 7,
                    ["Code"] = "ABC-001"
                }
            });

        var manager = new ValidationManagerClass();
        manager.SetDataSource(dataSource.Object);
        manager.RegisterRule(new ValidationRule
        {
            RuleName = "UniqueProductCode",
            BlockName = "Products",
            ItemName = "Code",
            CompareFieldName = "Id",
            ValidationType = ValidationType.Unique,
            Timing = ValidationTiming.Manual
        });

        var result = manager.ValidateRecord("Products", new Dictionary<string, object>
        {
            ["Id"] = 7,
            ["Code"] = "ABC-001"
        });

        result.IsValid.Should().BeTrue();
        dataSource.VerifyAll();
    }

    [Fact]
    public void ValidateRecord_DatabaseRule_UsesCompareFieldNameWhenLookupSourceOnlyProvidesEntity()
    {
        var dataSource = CreateDataSource();
        dataSource
            .Setup(source => source.GetEntity(
                "Customers",
                It.Is<List<AppFilter>>(filters => HasEqualityFilter(filters, "CustomerId", "ALFKI"))))
            .Returns(new object[] { new Dictionary<string, object> { ["CustomerId"] = "ALFKI" } });

        var manager = new ValidationManagerClass();
        manager.SetDataSource(dataSource.Object);
        manager.RegisterRule(new ValidationRule
        {
            RuleName = "ForeignKeyCustomer",
            BlockName = "Orders",
            ItemName = "CustomerId",
            CompareFieldName = "CustomerId",
            ValidationType = ValidationType.Database,
            LookupSource = "Customers",
            Timing = ValidationTiming.Manual
        });

        var result = manager.ValidateRecord("Orders", new Dictionary<string, object>
        {
            ["CustomerId"] = "ALFKI"
        });

        result.IsValid.Should().BeTrue();
        dataSource.VerifyAll();
    }

    [Fact]
    public void FormsManager_ValidateBlock_UsesRegisteredBlockDataSource_ForUniqueRules()
    {
        var dataSource = CreateDataSource();
        dataSource
            .Setup(source => source.GetEntity(
                "Products",
                It.Is<List<AppFilter>>(filters => HasEqualityFilter(filters, "Code", "DUP-001"))))
            .Returns(new object[]
            {
                new Dictionary<string, object>
                {
                    ["Code"] = "DUP-001"
                }
            });

        var manager = CreateFormsManager();
        var unitOfWork = new Mock<IUnitofWork>();
        unitOfWork.SetupGet(work => work.DataSource).Returns(dataSource.Object);
        unitOfWork.SetupGet(work => work.CurrentItem).Returns(new Dictionary<string, object>
        {
            ["Code"] = "DUP-001"
        });

        manager.RegisterBlock("Products", unitOfWork.Object, CreateEntityStructure("Products"));
        manager.Validation.RegisterRule(new ValidationRule
        {
            RuleName = "UniqueBlockCode",
            BlockName = "Products",
            ItemName = "Code",
            ValidationType = ValidationType.Unique,
            Timing = ValidationTiming.Manual
        });

        var isValid = manager.ValidateBlock("Products");

        isValid.Should().BeFalse();
        dataSource.VerifyAll();
    }

    private static Mock<IDataSource> CreateDataSource()
    {
        var dataSource = new Mock<IDataSource>();
        dataSource.SetupProperty(source => source.DatasourceName, "TestSource");
        return dataSource;
    }

    private static FormsManager CreateFormsManager()
    {
        var editor = new Mock<IDMEEditor>();
        var configurationManager = new Mock<IConfigurationManager>();
        configurationManager.SetupProperty(manager => manager.Configuration, new UnitofWorksManagerConfiguration());

        return new FormsManager(editor.Object, configurationManager: configurationManager.Object);
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

    private static bool HasEqualityFilter(IEnumerable<AppFilter> filters, string fieldName, string filterValue)
    {
        foreach (var filter in filters)
        {
            if (string.Equals(filter.FieldName, fieldName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(filter.Operator, "=", StringComparison.OrdinalIgnoreCase)
                && string.Equals(filter.FilterValue, filterValue, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}