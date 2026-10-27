using System;
using System.Collections.Generic;
using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

public class DecimalStorageShapeTests
{
    private static RdbmsHelper Helper(bool configured)
    {
        var editor = new Mock<IDMEEditor>();
        if (configured)
        {
            var config = new Mock<IConfigEditor>();
            config.SetupGet(x => x.DataTypesMap).Returns(DatabaseTypeMappingRepository.GenerateSqlServerDataTypesMapping());
            config.SetupGet(x => x.DataDriversClasses).Returns(new List<ConnectionDriversConfig> { new() { DatasourceType = Utilities.DataSourceType.SqlServer, classHandler = "SQLServerDataSource" } });
            editor.SetupGet(x => x.ConfigEditor).Returns(config.Object);
        }
        return new RdbmsHelper(editor.Object) { SupportedType = Utilities.DataSourceType.SqlServer };
    }

    [Theory]
    [InlineData(null, null, "decimal(28,8)")]
    [InlineData(18, 0, "decimal(18,0)")]
    [InlineData(18, 2, "decimal(18,2)")]
    [InlineData(18, 3, "decimal(18,3)")]
    [InlineData(28, 4, "decimal(28,4)")]
    [InlineData(28, 12, "decimal(28,12)")]
    [InlineData(29, 28, "decimal(29,28)")]
    public void Sql_server_forward_mapping_resolves_declared_or_default_storage_shape(int? precision, int? scale, string expected)
    {
        foreach (var configured in new[] { false, true })
        {
            var helper = Helper(configured);
            Assert.Equal(expected, helper.MapClrTypeToDatasourceType(typeof(decimal), precision: precision, scale: scale), ignoreCase: true);
            Assert.Equal(expected, helper.MapClrTypeToDatasourceType(typeof(decimal?), precision: precision, scale: scale), ignoreCase: true);
        }
    }

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(0, 2)]
    [InlineData(10, -1)]
    [InlineData(10, 11)]
    public void Invalid_decimal_shape_is_not_changed_into_a_string_column(int precision, int scale)
    {
        foreach (var configured in new[] { false, true })
            Assert.Throws<ArgumentOutOfRangeException>(() => Helper(configured).MapClrTypeToDatasourceType(typeof(decimal), precision: precision, scale: scale));
    }

    [Theory]
    [InlineData(0, 0, "decimal(28,8)")]
    [InlineData(18, 0, "decimal(18,0)")]
    [InlineData(28, 8, "decimal(28,8)")]
    public void Existing_bare_decimal_catalogs_receive_shape_without_mutating_the_metadata(short precision, short scale, string expected)
    {
        var name = "DecimalLegacy_" + Guid.NewGuid().ToString("N");
        var config = new Mock<IConfigEditor>();
        config.SetupGet(x => x.DataTypesMap).Returns(new List<DatatypeMapping>
        {
            new() { DataSourceName = name, NetDataType = "System.Decimal", DataType = "decimal", Fav = false }
        });
        var editor = new Mock<IDMEEditor>(); editor.SetupGet(x => x.ConfigEditor).Returns(config.Object);
        var field = new EntityField { FieldName = "Amount", Fieldtype = "System.Decimal", NumericPrecision = precision, NumericScale = scale };
        Assert.Equal(expected, DataTypeMappingLookup.GetDataTypeFromDataSourceClassName(name, field, editor.Object));
        Assert.Equal(precision, field.NumericPrecision); Assert.Equal(scale, field.NumericScale);
    }

    [Theory]
    [InlineData(18, 0)]
    [InlineData(28, 8)]
    [InlineData(29, 28)]
    public void Create_and_add_column_helpers_honor_the_same_shape(short precision, short scale)
    {
        var helper = new RdbmsHelper(new Mock<IDMEEditor>().Object) { SupportedType = Utilities.DataSourceType.SqlServer };
        var field = new EntityField { FieldName = "Amount", Fieldtype = "System.Decimal", NumericPrecision = precision, NumericScale = scale };
        var entity = new EntityStructure { EntityName = "PrecisionProbe", DatabaseType = Utilities.DataSourceType.SqlServer, Fields = new List<EntityField> { field } };
        var create = helper.GenerateCreateTableSql(entity);
        var add = helper.GenerateAddColumnSql("PrecisionProbe", field);
        Assert.True(create.Success, create.ErrorMessage); Assert.True(add.Success, add.ErrorMessage);
        Assert.Contains($"decimal({precision},{scale})", create.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"decimal({precision},{scale})", add.Sql, StringComparison.OrdinalIgnoreCase);
    }
}
