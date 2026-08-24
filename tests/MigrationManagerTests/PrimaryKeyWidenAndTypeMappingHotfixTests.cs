using System.Collections.Generic;
using System.Linq;
using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>
/// Coverage for a batch of pending, uncommitted fixes found sitting in the working tree
/// (2026-08-24) — SQL Server's default string-type mapping was silently truncating strings to
/// a single character, and a new primary-key-widening migration path was built on top of that
/// fix. Both were already correct and well-commented ("live-verified both ways" against a real
/// SQL Server) but had zero automated test coverage. These tests supply it before the change
/// set is committed.
/// </summary>
public class DataTypeMappingHotfixTests
{
    public DataTypeMappingHotfixTests()
    {
        // DataTypeMappingLookup's caches are static — clear before every test so one test's
        // registered mappings can never leak into another's lookup for the same class/field-type.
        DataTypeMappingLookup.ClearCache();
    }

    private static Mock<IDMEEditor> BuildEditor(List<DatatypeMapping> dataTypesMap)
    {
        var configMock = new Mock<IConfigEditor>(MockBehavior.Loose);
        configMock.SetupGet(c => c.DataTypesMap).Returns(dataTypesMap);

        var editorMock = new Mock<IDMEEditor>(MockBehavior.Loose);
        editorMock.SetupGet(e => e.ConfigEditor).Returns(configMock.Object);
        return editorMock;
    }

    [Fact]
    public void GetDataTypeFromDataSourceClassName_NoSizeGiven_PrefersFavRowOverDeclarationOrder()
    {
        // Declaration order deliberately puts the bad "char" row first — this is exactly the
        // shape that silently truncated every unattributed SQL Server string column before the
        // fix: nothing about Fav was consulted, so whichever mapping happened to be declared
        // first for the .NET type won.
        var mappings = new List<DatatypeMapping>
        {
            new() { DataSourceName = "HotfixDS1", NetDataType = "System.String", DataType = "char", Fav = false },
            new() { DataSourceName = "HotfixDS1", NetDataType = "System.String", DataType = "nvarchar(N)", Fav = true },
        };
        var editor = BuildEditor(mappings);
        var field = new EntityField { FieldName = "Name", Fieldtype = "System.String" };

        var result = DataTypeMappingLookup.GetDataTypeFromDataSourceClassName("HotfixDS1", field, editor.Object);

        Assert.Equal("nvarchar(4000)", result);
    }

    [Fact]
    public void GetDataTypeFromDataSourceClassName_NoFavRowRegistered_StillFallsBackToWhateverMatches()
    {
        var mappings = new List<DatatypeMapping>
        {
            new() { DataSourceName = "HotfixDS2", NetDataType = "System.String", DataType = "text", Fav = false },
        };
        var editor = BuildEditor(mappings);
        var field = new EntityField { FieldName = "Notes", Fieldtype = "System.String" };

        var result = DataTypeMappingLookup.GetDataTypeFromDataSourceClassName("HotfixDS2", field, editor.Object);

        Assert.Equal("text", result);
    }

    [Fact]
    public void GetDataTypeFromDataSourceClassName_ExplicitSize_StillGoesThroughTheSizedBranchNotThePlaceholderDefault()
    {
        var mappings = new List<DatatypeMapping>
        {
            new() { DataSourceName = "HotfixDS3", NetDataType = "System.String", DataType = "char", Fav = false },
            new() { DataSourceName = "HotfixDS3", NetDataType = "System.String", DataType = "nvarchar(N)", Fav = true },
        };
        var editor = BuildEditor(mappings);
        var field = new EntityField { FieldName = "Code", Fieldtype = "System.String", Size1 = 20 };

        var result = DataTypeMappingLookup.GetDataTypeFromDataSourceClassName("HotfixDS3", field, editor.Object);

        Assert.Equal("nvarchar(20)", result);
    }
}

public class RdbmsHelperHotfixTests
{
    private static RdbmsHelper BuildHelper(DataSourceType type)
    {
        // MapClrTypeToDatasourceType (which ResolveFieldType/GenerateAlterColumnSql call) reads
        // the static, hardcoded per-provider DatabaseTypeMappingRepository tables — a separate
        // data source from DataTypeMappingLookup's ConfigEditor.DataTypesMap (see that class's
        // own remark above). No further IDMEEditor interaction is needed for these calls.
        var editorMock = new Mock<IDMEEditor>(MockBehavior.Loose);
        return new RdbmsHelper(editorMock.Object) { SupportedType = type };
    }

    [Fact]
    public void GenerateDropPrimaryKeySql_ProducesAlterTableDropConstraint()
    {
        var helper = BuildHelper(DataSourceType.SqlServer);

        var (sql, ok, err) = helper.GenerateDropPrimaryKeySql("Orders", "PK_Orders_Id");

        Assert.True(ok, err);
        Assert.Contains("DROP CONSTRAINT", sql);
        Assert.Contains("Orders", sql);
        Assert.Contains("PK_Orders_Id", sql);
    }

    [Fact]
    public void GenerateDropPrimaryKeySql_MissingConstraintName_FailsRatherThanEmittingInvalidSql()
    {
        var helper = BuildHelper(DataSourceType.SqlServer);

        var (sql, ok, err) = helper.GenerateDropPrimaryKeySql("Orders", "");

        Assert.False(ok);
        Assert.True(string.IsNullOrEmpty(sql));
        Assert.False(string.IsNullOrEmpty(err));
    }

    [Fact]
    public void GenerateAlterColumnSql_SqlServer_NotNullColumn_StatesNotNullExplicitly()
    {
        var helper = BuildHelper(DataSourceType.SqlServer);
        var column = new EntityField { FieldName = "Id", Fieldtype = "System.String", Size = 128, AllowDBNull = false };

        var (sql, ok, err) = helper.GenerateAlterColumnSql("Orders", "Id", column);

        Assert.True(ok, err);
        Assert.Contains("ALTER COLUMN", sql);
        Assert.EndsWith("NOT NULL", sql);
    }

    [Fact]
    public void GenerateAlterColumnSql_SqlServer_NullableColumn_StatesNullExplicitlyRatherThanOmittingIt()
    {
        // The defect this fixes: SQL Server does not treat an omitted nullability clause as
        // "keep the current setting" — verified live, per the source's own comment — an
        // omitted clause on a formerly NOT NULL column left it silently nullable.
        var helper = BuildHelper(DataSourceType.SqlServer);
        var column = new EntityField { FieldName = "MiddleName", Fieldtype = "System.String", Size = 64, AllowDBNull = true };

        var (sql, ok, err) = helper.GenerateAlterColumnSql("Orders", "MiddleName", column);

        Assert.True(ok, err);
        Assert.EndsWith("NULL", sql);
        Assert.DoesNotContain("NOT NULL", sql);
    }

    [Fact]
    public void GenerateAlterColumnSql_Postgres_UsesTwoClauseMultiActionSyntaxForTypeAndNullability()
    {
        // Postgres has no single-clause "change type AND nullability" form — TYPE and
        // SET/DROP NOT NULL are separate sub-actions of one ALTER TABLE statement.
        var helper = BuildHelper(DataSourceType.Postgre);
        var column = new EntityField { FieldName = "Id", Fieldtype = "System.String", Size = 128, AllowDBNull = false };

        var (sql, ok, err) = helper.GenerateAlterColumnSql("Orders", "Id", column);

        Assert.True(ok, err);
        Assert.Contains("TYPE", sql);
        Assert.Contains("SET NOT NULL", sql);
    }

    [Fact]
    public void GenerateAddColumnSql_ResolvesFieldTypeInsteadOfEmittingTheRawClrTypeName()
    {
        // Before the fix this used column.Fieldtype directly ("System.String"), which is
        // syntactically invalid DDL on every provider.
        var helper = BuildHelper(DataSourceType.SqlServer);
        var column = new EntityField { FieldName = "ChangedBy", Fieldtype = "System.String", Size = 128, AllowDBNull = true };

        var (sql, ok, err) = helper.GenerateAddColumnSql("Orders", column);

        Assert.True(ok, err);
        Assert.DoesNotContain("System.String", sql);
        Assert.Contains("varchar", sql, System.StringComparison.OrdinalIgnoreCase);
    }
}

public class PrimaryKeyWidenTests
{
    private static EntityField WidenedIdColumn() =>
        new() { FieldName = "Id", Fieldtype = "System.String", Size = 4000, AllowDBNull = false };

    [Fact]
    public void WidenPrimaryKeyColumn_HappyPath_DropsAltersThenRecreatesTheConstraintInOrder()
    {
        var harness = new MigrationTestHarness { RunQueryRows = new List<object[]> { new object[] { "PK_Orders_Id" } } };
        var manager = harness.Build();

        var result = manager.WidenPrimaryKeyColumn("Orders", WidenedIdColumn());

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(3, harness.ExecutedSql.Count);
        Assert.Contains("DROP CONSTRAINT", harness.ExecutedSql[0]);
        Assert.Contains("ALTER COLUMN", harness.ExecutedSql[1]);
        Assert.Contains("PRIMARY KEY", harness.ExecutedSql[2]);
    }

    [Fact]
    public void WidenPrimaryKeyColumn_AlterStepFails_StillRestoresThePrimaryKeyAndReportsFailure()
    {
        // The safety guarantee WidenPrimaryKeyColumn's own remarks promise: the table must
        // never be left without its primary key, even when the widen itself fails.
        var harness = new MigrationTestHarness { RunQueryRows = new List<object[]> { new object[] { "PK_Orders_Id" } } };
        harness.FailSqlContaining.Add("ALTER COLUMN");
        var manager = harness.Build();

        var result = manager.WidenPrimaryKeyColumn("Orders", WidenedIdColumn());

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains(harness.ExecutedSql, s => s.Contains("PRIMARY KEY"));
    }

    [Fact]
    public void WidenPrimaryKeyColumn_AddPrimaryKeyStepAlsoFails_ReportsTheNoPrimaryKeyDangerExplicitly()
    {
        var harness = new MigrationTestHarness { RunQueryRows = new List<object[]> { new object[] { "PK_Orders_Id" } } };
        harness.FailSqlContaining.Add("PRIMARY KEY");
        var manager = harness.Build();

        var result = manager.WidenPrimaryKeyColumn("Orders", WidenedIdColumn());

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("NO primary key", result.Message);
    }

    [Fact]
    public void WidenPrimaryKeyColumn_NoDiscoverableConstraint_FailsBeforeIssuingAnyDdl()
    {
        var harness = new MigrationTestHarness { RunQueryRows = new List<object[]>() };
        var manager = harness.Build();

        var result = manager.WidenPrimaryKeyColumn("Orders", WidenedIdColumn());

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Empty(harness.ExecutedSql);
    }

    [Fact]
    public void WidenPrimaryKeyColumn_MissingColumnName_FailsFast()
    {
        var harness = new MigrationTestHarness();
        var manager = harness.Build();

        var result = manager.WidenPrimaryKeyColumn("Orders", new EntityField { FieldName = "" });

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Empty(harness.ExecutedSql);
    }
}
