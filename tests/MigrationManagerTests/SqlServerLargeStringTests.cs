using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

[CollectionDefinition("SQL Server string mapping", DisableParallelization = true)]
public sealed class SqlServerStringMappingCollection;

[Collection("SQL Server string mapping")]
public sealed class SqlServerLargeStringTests
{
    private static IDMEEditor Editor(string className, string template)
    {
        DataTypeMappingLookup.ClearCache();
        var config = new Mock<IConfigEditor>();
        config.SetupGet(x => x.DataDriversClasses).Returns(
            [new ConnectionDriversConfig { DatasourceType = DataSourceType.SqlServer, classHandler = className }]);
        config.SetupGet(x => x.DataTypesMap).Returns(
            [new DatatypeMapping { DataSourceName = className, NetDataType = "System.String", DataType = template, Fav = true }]);
        var editor = new Mock<IDMEEditor>();
        editor.SetupGet(x => x.ConfigEditor).Returns(config.Object);
        return editor.Object;
    }

    [Theory]
    [InlineData(0, "nvarchar(4000)")]
    [InlineData(128, "nvarchar(128)")]
    [InlineData(4000, "nvarchar(4000)")]
    [InlineData(4001, "nvarchar(max)")]
    [InlineData(32768, "nvarchar(max)")]
    [InlineData(int.MaxValue, "nvarchar(max)")]
    public void ConfiguredLookup_PreservesBoundedColumnsAndUsesLargeValueStorage(int size, string expected)
    {
        var editor = Editor("SQLServerDataSource", "nvarchar(N)");
        var field = new EntityField { FieldName = "Payload", Fieldtype = "System.String", Size1 = size };

        Assert.Equal(expected, DataTypeMappingLookup.GetDataTypeFromDataSourceClassName("SQLServerDataSource", field, editor));
        Assert.Equal("nvarchar(N)", editor.ConfigEditor.DataTypesMap.Single().DataType);
    }

    [Theory]
    [InlineData("nvarchar(N)", 4000, "nvarchar(4000)")]
    [InlineData("nvarchar(N)", 4001, "nvarchar(max)")]
    [InlineData("varchar(N)", 8000, "varchar(8000)")]
    [InlineData("varchar(N)", 8001, "varchar(max)")]
    public void MappingRecordApi_UsesTheSameSqlServerLengthRule(string template, int size, string expected)
    {
        var editor = Editor("SQLServerDataSource", template);
        var field = new EntityField { FieldName = "Payload", Fieldtype = "System.String", Size1 = size };

        Assert.Equal(expected, DataTypeMappingLookup.GetDataTypeMappingForString("SQLServerDataSource", field.Fieldtype, field, editor).DataType);
        Assert.Equal(template, editor.ConfigEditor.DataTypesMap.Single().DataType);
    }

    [Theory]
    [InlineData(128, "nvarchar(128)")]
    [InlineData(4000, "nvarchar(4000)")]
    [InlineData(4001, "nvarchar(max)")]
    [InlineData(32768, "nvarchar(max)")]
    public void AddColumnGenerator_UsesAValidSqlServerType(int size, string expected)
    {
        var helper = new RdbmsHelper(Editor("SQLServerDataSource", "nvarchar(N)")) { SupportedType = DataSourceType.SqlServer };
        var field = new EntityField { FieldName = "Payload", Fieldtype = "System.String", Size = size };

        var result = helper.GenerateAddColumnSql("Documents", field);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(expected, result.Sql);
    }

    [Fact]
    public void OtherProviderMapping_DoesNotAcquireSqlServerMaxSyntax()
    {
        var editor = Editor("OtherProvider", "nvarchar(N)");
        var field = new EntityField { FieldName = "Payload", Fieldtype = "System.String", Size1 = 32768 };

        Assert.Equal("nvarchar(32768)", DataTypeMappingLookup.GetDataTypeFromDataSourceClassName("OtherProvider", field, editor));
        Assert.Equal("nvarchar(32768)", DataTypeMappingLookup.GetDataTypeMappingForString("OtherProvider", field.Fieldtype, field, editor).DataType);
    }
}
