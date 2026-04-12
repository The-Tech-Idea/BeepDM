using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using Xunit;
using LOVManagerClass = TheTechIdea.Beep.Editor.UOWManager.Helpers.LOVManager;

namespace LOVManager.Tests;

public class LOVManagerTests
{
    [Fact]
    public async Task RegisterLOV_AndLoadLOVDataAsync_LoadsRecordsAndRaisesEvent()
    {
        var records = CreateCountryRecords();
        var (manager, _, dataSource) = CreateManager(records, ConnectionState.Closed);
        LOVDataLoadedEventArgs? loadedArgs = null;

        manager.LOVDataLoaded += (_, args) => loadedArgs = args;
        manager.RegisterLOV("Orders", "ShipCountry", CreateCountryLov());

        var result = await manager.LoadLOVDataAsync("Orders", "ShipCountry");

        result.Success.Should().BeTrue();
        result.TotalCount.Should().Be(3);
        result.FromCache.Should().BeFalse();
        loadedArgs.Should().NotBeNull();
        loadedArgs!.BlockName.Should().Be("Orders");
        loadedArgs.FieldName.Should().Be("ShipCountry");
        loadedArgs.RecordCount.Should().Be(3);
        loadedArgs.FromCache.Should().BeFalse();
        dataSource.Verify(source => source.Openconnection(), Times.Once);
        dataSource.Verify(source => source.GetEntity(
            "Countries",
            It.Is<List<AppFilter>>(filters => filters == null)), Times.Once);
    }

    [Fact]
    public async Task LoadLOVDataAsync_UsesCacheOnSubsequentCalls()
    {
        var records = CreateCountryRecords();
        var (manager, _, dataSource) = CreateManager(records);
        manager.RegisterLOV("Orders", "ShipCountry", CreateCountryLov());

        var first = await manager.LoadLOVDataAsync("Orders", "ShipCountry");
        var second = await manager.LoadLOVDataAsync("Orders", "ShipCountry");

        first.Success.Should().BeTrue();
        first.FromCache.Should().BeFalse();
        second.Success.Should().BeTrue();
        second.FromCache.Should().BeTrue();
        second.TotalCount.Should().Be(3);
        dataSource.Verify(source => source.GetEntity(
            "Countries",
            It.IsAny<List<AppFilter>>()), Times.Once);
    }

    [Fact]
    public async Task LoadLOVDataAsync_WithSearchText_BuildsSearchFiltersForSearchableColumns()
    {
        var records = CreateCountryRecords();
        var (manager, _, dataSource) = CreateManager(records);
        manager.RegisterLOV("Orders", "ShipCountry", CreateCountryLov(searchMode: LOVSearchMode.StartsWith));

        var result = await manager.LoadLOVDataAsync("Orders", "ShipCountry", "Can");

        result.Success.Should().BeTrue();
        dataSource.Verify(source => source.GetEntity(
            "Countries",
            It.Is<List<AppFilter>>(filters => HasLikeFilter(filters, "CountryName", "Can%"))), Times.Once);
    }

    [Fact]
    public async Task FilterLOVData_UsesCachedDataAndSearchableColumns()
    {
        var records = CreateCountryRecords();
        var (manager, _, _) = CreateManager(records);
        manager.RegisterLOV("Orders", "ShipCountry", CreateCountryLov());
        await manager.LoadLOVDataAsync("Orders", "ShipCountry");

        var result = manager.FilterLOVData("Orders", "ShipCountry", "can");

        result.Success.Should().BeTrue();
        result.FromCache.Should().BeTrue();
        result.TotalCount.Should().Be(1);
        var record = result.Records.Should().ContainSingle().Subject;
        ((IDictionary<string, object>)record)["CountryName"].Should().Be("Canada");
    }

    [Fact]
    public async Task ValidateLOVValueAsync_WhenValueIsMissing_ReturnsSuggestionsAndRaisesEvent()
    {
        var records = CreateCountryRecords();
        var (manager, _, _) = CreateManager(records);
        LOVValidationEventArgs? validationArgs = null;

        manager.LOVValidationFailed += (_, args) => validationArgs = args;
        manager.RegisterLOV("Orders", "ShipCountry", CreateCountryLov(returnField: "CountryName"));

        var result = await manager.ValidateLOVValueAsync("Orders", "ShipCountry", "Canad");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Canad");
        result.Suggestions.Should().ContainSingle();
        ((IDictionary<string, object>)result.Suggestions[0])["CountryName"].Should().Be("Canada");
        validationArgs.Should().NotBeNull();
        validationArgs!.BlockName.Should().Be("Orders");
        validationArgs.FieldName.Should().Be("ShipCountry");
        validationArgs.Value.Should().Be("Canad");
        validationArgs.Suggestions.Should().ContainSingle();
    }

    [Fact]
    public void GetRelatedFieldValues_ReturnsReturnValueAndMappedFields()
    {
        var (manager, _, _) = CreateManager(CreateCountryRecords());
        var lov = CreateCountryLov();
        lov.MapField("Region", "ShipRegion");

        var values = manager.GetRelatedFieldValues(lov, CreateCountryRecord("CA", "Canada", "North America"));

        values["__RETURN_VALUE__"].Should().Be("CA");
        values["ShipRegion"].Should().Be("North America");
    }

    private static (LOVManagerClass Manager, Mock<IDMEEditor> Editor, Mock<IDataSource> DataSource) CreateManager(
        IEnumerable<object> records,
        ConnectionState connectionState = ConnectionState.Open)
    {
        var dataSource = new Mock<IDataSource>();
        dataSource.SetupGet(source => source.ConnectionStatus).Returns(connectionState);
        dataSource.Setup(source => source.Openconnection()).Returns(ConnectionState.Open);
        dataSource.Setup(source => source.GetEntity("Countries", It.IsAny<List<AppFilter>>())).Returns(records);

        var editor = new Mock<IDMEEditor>();
        editor.Setup(editor => editor.GetDataSource("LookupSource")).Returns(dataSource.Object);

        var manager = new LOVManagerClass(editor.Object, new ConcurrentDictionary<string, DataBlockInfo>());
        return (manager, editor, dataSource);
    }

    private static LOVDefinition CreateCountryLov(string? returnField = "CountryCode", LOVSearchMode searchMode = LOVSearchMode.Contains)
    {
        return new LOVDefinition
        {
            LOVName = "CountriesLov",
            DataSourceName = "LookupSource",
            EntityName = "Countries",
            DisplayField = "CountryName",
            ReturnField = returnField,
            UseCache = true,
            CacheDurationMinutes = 30,
            SearchMode = searchMode,
            ValidationType = LOVValidationType.ListOnly,
            Columns = new List<LOVColumn>
            {
                LOVColumn.Create("CountryName", "Country"),
                LOVColumn.Create("CountryCode", "Code")
            }
        };
    }

    private static List<object> CreateCountryRecords()
    {
        return new List<object>
        {
            CreateCountryRecord("CA", "Canada", "North America"),
            CreateCountryRecord("CM", "Cameroon", "Africa"),
            CreateCountryRecord("US", "United States", "North America")
        };
    }

    private static Dictionary<string, object> CreateCountryRecord(string code, string name, string region)
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["CountryCode"] = code,
            ["CountryName"] = name,
            ["Region"] = region
        };
    }

    private static bool HasLikeFilter(IEnumerable<AppFilter> filters, string fieldName, string filterValue)
    {
        foreach (var filter in filters)
        {
            if (string.Equals(filter.FieldName, fieldName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(filter.Operator, "LIKE", StringComparison.OrdinalIgnoreCase)
                && string.Equals(filter.FilterValue, filterValue, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}