#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Moq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Schema;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

// Phase 7 — canonical class reader + diff/drift/cache/discovery fixes.

public class ClassReaderTests
{
    private static ClassCreator Reader() => new ClassCreator(new Mock<IDMEEditor>().Object);

    private static EntityField Field(EntityStructure e, string name) =>
        e.Fields.First(f => string.Equals(f.FieldName, name, StringComparison.OrdinalIgnoreCase));

    // ── 7-A: relations ──────────────────────────────────────────────────────

    [Fact]
    public void Reader_PopulatesRelations_FromNavigationProp_AndScalarFk()
    {
        var e = Reader().ConvertToEntityStructure(typeof(Product));

        var rel = e.Relations.FirstOrDefault(r => r.RelatedEntityID == "Category");
        Assert.NotNull(rel);
        Assert.Equal("CategoryId", rel!.EntityColumnID);
        Assert.Equal("Category", rel.RalationName);
        Assert.DoesNotContain(e.Relations, r => r.RelatedEntityID == "Review"); // collection nav: no local FK
    }

    [Fact]
    public void Reader_RemovesNavigationProps_FromColumns_ButKeepsScalarFk()
    {
        var e = Reader().ConvertToEntityStructure(typeof(Product));
        Assert.DoesNotContain(e.Fields, f => f.FieldName == "Category");
        Assert.DoesNotContain(e.Fields, f => f.FieldName == "Reviews");
        Assert.Contains(e.Fields, f => f.FieldName == "CategoryId");
    }

    // ── 7-B: indexes ────────────────────────────────────────────────────────

    [Fact]
    public void Reader_PopulatesIndexes_FromIndexAttribute_UsingColumnName()
    {
        var e = Reader().ConvertToEntityStructure(typeof(Product));
        var ix = e.Indexes.FirstOrDefault(i => i.IsUnique && i.Columns.Contains("product_name"));
        Assert.NotNull(ix); // [Index(nameof(Name))] mapped through [Column("product_name")]
    }

    // ── 7-B: precision / scale ──────────────────────────────────────────────

    [Fact]
    public void Reader_SetsPrecisionScale_FromPrecisionAttr_AndColumnTypeName()
    {
        var e = Reader().ConvertToEntityStructure(typeof(Product));
        Assert.Equal((short)18, Field(e, "Price").NumericPrecision);
        Assert.Equal((short)2, Field(e, "Price").NumericScale);
        Assert.Equal((short)10, Field(e, "Weight").NumericPrecision); // from [Column(TypeName="decimal(10,4)")]
        Assert.Equal((short)4, Field(e, "Weight").NumericScale);
    }

    // ── 7-B: enum storage ───────────────────────────────────────────────────

    [Fact]
    public void Reader_EnumStrategy_Int_IsDefault()
    {
        var e = Reader().ConvertToEntityStructure(typeof(Product));
        Assert.Equal(typeof(int).FullName, Field(e, "Status").Fieldtype);
        Assert.Equal(DbFieldCategory.Enum, Field(e, "Status").FieldCategory);
    }

    [Fact]
    public void Reader_EnumStrategy_String_UsesStringType()
    {
        var e = Reader().ConvertToEntityStructure(typeof(Product),
            new EntityReadOptions { EnumStorage = EnumStorageStrategy.String });
        var status = Field(e, "Status");
        Assert.Equal(typeof(string).FullName, status.Fieldtype);
        Assert.True(status.Size1 >= "Inactive".Length);
    }

    // ── 7-B: nullable reference types ───────────────────────────────────────

    [Fact]
    public void Reader_NRT_NonNullableString_IsNotNullable()
    {
        var e = Reader().ConvertToEntityStructure(typeof(Product));
        Assert.False(Field(e, "Title").AllowDBNull);      // string  (non-nullable NRT)
        Assert.True(Field(e, "Description").AllowDBNull); // string? (nullable NRT)
    }

    [Fact]
    public void Reader_NRT_Disabled_TreatsAllReferenceTypesNullable()
    {
        var e = Reader().ConvertToEntityStructure(typeof(Product),
            new EntityReadOptions { HonorNullableReferenceTypes = false });
        Assert.True(Field(e, "Title").AllowDBNull);
    }

    // ── 7-B: convention key without silent identity ─────────────────────────

    [Fact]
    public void Reader_ConventionKey_NoSilentIdentity_ByDefault()
    {
        var e = Reader().ConvertToEntityStructure(typeof(ConventionKeyed));
        var id = Field(e, "Id");
        Assert.True(id.IsKey);
        Assert.False(id.IsAutoIncrement);
    }

    [Fact]
    public void Reader_ConventionKey_ImpliesIdentity_WhenOptedIn()
    {
        var e = Reader().ConvertToEntityStructure(typeof(ConventionKeyed),
            new EntityReadOptions { ConventionKeyImpliesIdentity = true });
        Assert.True(Field(e, "Id").IsAutoIncrement);
    }

    // ── 7-B (W10): [Comment] and [ConcurrencyCheck] ─────────────────────────

    [Fact]
    public void Reader_ReadsComment_AndConcurrencyCheck()
    {
        var e = Reader().ConvertToEntityStructure(typeof(Audited));
        Assert.Equal("the display name", Field(e, "Name").Description);
        Assert.True(Field(e, "Version").IsRowVersion); // [ConcurrencyCheck] marks concurrency column
    }

    // ── W10: [Unicode] ──────────────────────────────────────────────────────

    [Fact]
    public void Reader_ReadsUnicodeAttribute()
    {
        var e = Reader().ConvertToEntityStructure(typeof(UnicodeSample));
        Assert.True(Field(e, "DefaultStr").IsUnicode);   // default
        Assert.False(Field(e, "AsciiStr").IsUnicode);    // [Unicode(false)]
    }
}

public class UnicodePreferenceTests
{
    [Theory]
    [InlineData("nvarchar(50)", false, "varchar(50)")]
    [InlineData("nvarchar(50)", true, "nvarchar(50)")]
    [InlineData("nchar(10)", false, "char(10)")]
    [InlineData("ntext", false, "text")]
    [InlineData("nvarchar2(4000)", false, "varchar2(4000)")] // Oracle
    [InlineData("varchar(50)", false, "varchar(50)")]        // already non-unicode
    [InlineData("text", false, "text")]                      // no distinction (e.g. Postgres)
    public void ApplyUnicodePreference_DowngradesOnlyWhenAsked(string input, bool isUnicode, string expected)
    {
        Assert.Equal(expected,
            TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers.RdbmsHelper
                .ApplyUnicodePreference(input, isUnicode));
    }

    [Fact]
    public void LegacyCreateTableSql_HonorsIsUnicode()
    {
        var entity = new EntityStructure
        {
            EntityName = "T",
            DatabaseType = DataSourceType.SqlServer,
            Fields = new List<EntityField>
            {
                new() { FieldName = "DefaultStr", Fieldtype = "System.String", IsUnicode = true },
                new() { FieldName = "AsciiStr", Fieldtype = "System.String", IsUnicode = false }
            }
        };

        var (sql, ok, _) = TheTechIdea.Beep.Helpers.RDBMSHelpers.DatabaseObjectCreationHelper
            .GenerateCreateTableSQL(entity);

        Assert.True(ok);
        var def = System.Text.RegularExpressions.Regex.Match(sql, @"DefaultStr\s+(\w+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value;
        var ascii = System.Text.RegularExpressions.Regex.Match(sql, @"AsciiStr\s+(\w+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value;

        Assert.StartsWith("NVARCHAR", def, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("VARCHAR", ascii, StringComparison.OrdinalIgnoreCase);
        Assert.False(ascii.StartsWith("N", StringComparison.OrdinalIgnoreCase)); // downgraded
    }
}

public class DiffDriftCacheTests
{
    // ── 7-C: column-name-aware diff ─────────────────────────────────────────

    [Fact]
    public void Diff_UsesColumnName_WhenColumnAttributePresent()
    {
        var mgr = new MigrationTestHarness().Build();

        var desired = new EntityStructure
        {
            EntityName = "Widget",
            Fields = new List<EntityField>
            {
                new() { FieldName = "Status", ColumnName = "status_code", Fieldtype = "System.Int32" }
            }
        };
        var current = new EntityStructure
        {
            EntityName = "Widget",
            Fields = new List<EntityField> { new() { FieldName = "status_code", Fieldtype = "System.Int32" } }
        };

        Assert.Empty(mgr.GetMissingColumns(current, desired)); // renamed column is NOT "missing"
    }

    [Fact]
    public void Diff_ReportsGenuinelyMissingColumn()
    {
        var mgr = new MigrationTestHarness().Build();
        var desired = new EntityStructure
        {
            EntityName = "Widget",
            Fields = new List<EntityField> { new() { FieldName = "NewCol", Fieldtype = "System.String" } }
        };
        var current = new EntityStructure { EntityName = "Widget", Fields = new List<EntityField>() };

        Assert.Single(mgr.GetMissingColumns(current, desired));
    }

    // ── 7-C lint: a [Column] rename against a still-live CLR-named column ────

    [Fact]
    public void Plan_LintsRename_WhenClrNameStillLive()
    {
        var desired = new EntityStructure
        {
            EntityName = "Review",
            DatasourceEntityName = "Review",
            Fields = new List<EntityField>
            {
                new() { FieldName = "Id", Fieldtype = "System.Int32" },
                new() { FieldName = "Title", ColumnName = "review_title", Fieldtype = "System.String" }
            }
        };
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(Review), desired)
            .WithExisting(MigrationTestHarness.Entity("Review", "Id", "Title")); // old CLR-named column still live
        var mgr = harness.Build();

        var plan = mgr.BuildMigrationPlanForTypes(new[] { typeof(Review) });
        var addOp = plan.Operations.First(o => o.Kind == MigrationPlanOperationKind.AddMissingColumns);

        Assert.Contains(addOp.ProviderAssumptions, a => a.Contains("rename", StringComparison.OrdinalIgnoreCase));
    }

    // ── 7-D: [Table]-aware drift ────────────────────────────────────────────

    [Fact]
    public void Drift_ResolvesTableAttributeName()
    {
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(TabledWidget), MigrationTestHarness.Entity("TabledWidget", "Id", "Name"))
            .WithExisting(MigrationTestHarness.Entity("TBL_WIDGET", "Id", "Name"));
        var mgr = harness.Build();

        var report = mgr.InspectDrift(typeof(TabledWidget));

        Assert.Equal("TBL_WIDGET", report.Current.EntityName); // resolved via [Table]
        Assert.Empty(report.AddedFields);                      // matched, not "all added"
    }

    // ── 7-E: conversion cache ───────────────────────────────────────────────

    [Fact]
    public void TryGetEntityStructure_CachesConversion_PerType()
    {
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(TabledWidget), MigrationTestHarness.Entity("TBL_WIDGET", "Id", "Name"))
            .WithExisting(MigrationTestHarness.Entity("TBL_WIDGET", "Id", "Name"));
        var mgr = harness.Build();

        mgr.InspectDrift(typeof(TabledWidget));
        mgr.InspectDrift(typeof(TabledWidget));

        Assert.Equal(1, harness.ConversionCount);
    }

    [Fact]
    public void ClearEntityStructureCache_ForcesReconversion()
    {
        var harness = new MigrationTestHarness()
            .WithDesired(typeof(TabledWidget), MigrationTestHarness.Entity("TBL_WIDGET", "Id"))
            .WithExisting(MigrationTestHarness.Entity("TBL_WIDGET", "Id"));
        var mgr = harness.Build();

        mgr.InspectDrift(typeof(TabledWidget));
        mgr.ClearEntityStructureCache();
        mgr.InspectDrift(typeof(TabledWidget));

        Assert.Equal(2, harness.ConversionCount);
    }
}

public class DiscoveryMarkerTests
{
    private static MigrationManager DiscoveryManager()
    {
        var editor = new Mock<IDMEEditor>();
        var cc = new ClassCreator(editor.Object);
        editor.SetupGet(e => e.classCreator).Returns(cc);
        return new MigrationManager(editor.Object, new Mock<IDataSource>().Object);
    }

    [Fact]
    public void Discovery_Scoped_AcceptsPocoAndOptIn_ExcludesIgnored()
    {
        var found = DiscoveryManager().DiscoverEntityTypes("Beep.Phase7.DiscoverySamples");

        Assert.Contains(typeof(global::Beep.Phase7.DiscoverySamples.PlainDto), found);
        Assert.Contains(typeof(global::Beep.Phase7.DiscoverySamples.OptInEntity), found);
        Assert.Contains(typeof(global::Beep.Phase7.DiscoverySamples.EfEntity), found);
        Assert.DoesNotContain(typeof(global::Beep.Phase7.DiscoverySamples.IgnoredEntity), found);
    }

    [Fact]
    public void Discovery_Unscoped_DoesNotSweepUpBarePocos()
    {
        var found = DiscoveryManager().DiscoverAllEntityTypes();
        Assert.DoesNotContain(typeof(global::Beep.Phase7.DiscoverySamples.PlainDto), found);
    }
}

public class PlanRelationTests
{
    // ── 7-A end to end (W1): applyForeignKeys now emits FK ops for a PLAIN type,
    //     because the reflection reader populates Relations. ─────────────────────
    [Fact]
    public void Plan_WithApplyForeignKeys_EmitsFkOps_ForPlainTypes()
    {
        var editor = new Mock<IDMEEditor>();
        var cc = new ClassCreator(editor.Object);
        editor.SetupGet(e => e.classCreator).Returns(cc);

        var ds = new Mock<IDataSource>();
        ds.SetupGet(d => d.DatasourceType).Returns(DataSourceType.SqlServer);
        ds.SetupGet(d => d.Category).Returns(DatasourceCategory.RDBMS);
        ds.SetupGet(d => d.DatasourceName).Returns("testdb");
        ds.Setup(d => d.CheckEntityExist(It.IsAny<string>())).Returns(false);

        var mgr = new MigrationManager(editor.Object, ds.Object);
        var plan = mgr.BuildMigrationPlanForTypes(new[] { typeof(Product) },
            detectRelationships: true, applyForeignKeys: true);

        Assert.Contains(plan.Operations, o => o.Kind == MigrationPlanOperationKind.AddForeignKey);
    }
}

public class ReaderParityTests
{
    private static ClassCreator Reader() => new ClassCreator(new Mock<IDMEEditor>().Object);

    private static EntityField F(EntityStructure e, string n) =>
        e.Fields.First(f => string.Equals(f.FieldName, n, StringComparison.OrdinalIgnoreCase));

    private const string Source = @"
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Parity.Sample
{
    [Table(""PARITY"")]
    public class ParitySample
    {
        [Key] public int Id { get; set; }
        [Column(""full_name"")][Required][MaxLength(100)] public string Name { get; set; }
        public string? Note { get; set; }
        public int ParityOrderId { get; set; }
        public ParityOrder ParityOrder { get; set; }
    }
    public class ParityOrder { public int Id { get; set; } }
}";

    [Fact]
    public void RoslynAndReflectionReaders_AgreeOnAnnotationSemantics()
    {
        var cc = Reader();
        var refl = cc.ConvertToEntityStructure(typeof(ParitySample));
        var roslyn = cc.ParseSourceToEntityStructures(Source).First(e => e.EntityName == "PARITY");

        Assert.Equal("PARITY", refl.EntityName);            // [Table]
        Assert.Equal(refl.EntityName, roslyn.EntityName);

        Assert.True(F(refl, "Id").IsKey);                   // [Key]
        Assert.True(F(roslyn, "Id").IsKey);

        Assert.Equal("full_name", F(refl, "Name").ColumnName); // [Column]
        Assert.Equal("full_name", F(roslyn, "Name").ColumnName);
        Assert.True(F(refl, "Name").IsRequired);            // [Required]
        Assert.True(F(roslyn, "Name").IsRequired);
        Assert.Equal(100, F(refl, "Name").MaxLength);       // [MaxLength]
        Assert.Equal(100, F(roslyn, "Name").MaxLength);

        Assert.DoesNotContain(refl.Fields, f => f.FieldName == "ParityOrder");   // nav removed
        Assert.DoesNotContain(roslyn.Fields, f => f.FieldName == "ParityOrder");
        Assert.Contains(refl.Fields, f => f.FieldName == "ParityOrderId");       // scalar FK kept
        Assert.Contains(roslyn.Fields, f => f.FieldName == "ParityOrderId");

        var rRefl = refl.Relations.First();                 // relation agreement
        var rRos = roslyn.Relations.First();
        Assert.Equal("ParityOrder", rRefl.RelatedEntityID);
        Assert.Equal(rRefl.RelatedEntityID, rRos.RelatedEntityID);
        Assert.Equal("ParityOrderId", rRefl.EntityColumnID);
        Assert.Equal(rRefl.EntityColumnID, rRos.EntityColumnID);
    }
}

public class ReadOptionsThreadingTests
{
    private static (MigrationManager mgr, Mock<IDataSource> ds) NewManager(EntityReadOptions options = null)
    {
        var editor = new Mock<IDMEEditor>();
        var cc = new ClassCreator(editor.Object);
        editor.SetupGet(e => e.classCreator).Returns(cc);

        var ds = new Mock<IDataSource>();
        ds.SetupGet(d => d.DatasourceName).Returns("testdb");
        ds.SetupGet(d => d.DatasourceType).Returns(DataSourceType.SqlServer);
        ds.SetupGet(d => d.Category).Returns(DatasourceCategory.RDBMS);
        ds.Setup(d => d.GetEntityStructure(It.IsAny<string>(), It.IsAny<bool>())).Returns((EntityStructure)null);

        var mgr = new MigrationManager(editor.Object, ds.Object);
        if (options != null) mgr.ReadOptions = options;
        return (mgr, ds);
    }

    private static string StatusType(SchemaDriftReport report) =>
        report.Baseline.Fields.First(f => f.Name == "Status").DataType;

    [Fact]
    public void ReadOptions_Default_StoresEnumAsInt()
    {
        var (mgr, _) = NewManager();
        Assert.Equal("System.Int32", StatusType(mgr.InspectDrift(typeof(Product))));
    }

    [Fact]
    public void ReadOptions_StringEnum_FlowsThroughToPlanningReader()
    {
        var (mgr, _) = NewManager(new EntityReadOptions { EnumStorage = EnumStorageStrategy.String });
        Assert.Equal("System.String", StatusType(mgr.InspectDrift(typeof(Product))));
    }
}

// ── Test types ──────────────────────────────────────────────────────────────

[Table("PARITY")]
public class ParitySample
{
    [Key] public int Id { get; set; }
    [Column("full_name")][Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int ParityOrderId { get; set; }
    public ParityOrder? ParityOrder { get; set; }
}
public class ParityOrder { public int Id { get; set; } }

[Table("PRODUCTS")]
[Index(nameof(Name), IsUnique = true)]
public class Product
{
    [Key] public int Id { get; set; }
    [Column("product_name")][Required] public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty; // non-nullable NRT
    public string? Description { get; set; }           // nullable NRT
    public Status Status { get; set; }
    [Precision(18, 2)] public decimal Price { get; set; }
    [Column(TypeName = "decimal(10,4)")] public decimal Weight { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }            // reference navigation
    public List<Review> Reviews { get; set; } = new(); // collection navigation
}

public enum Status { Active, Inactive, Pending }
public class Category { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public class Review { public int Id { get; set; } public int ProductId { get; set; } }
public class ConventionKeyed { public int Id { get; set; } public string Name { get; set; } = string.Empty; }

public class Audited
{
    [Key] public int Id { get; set; }
    [ConcurrencyCheck] public int Version { get; set; }
    [Comment("the display name")] public string Name { get; set; } = string.Empty;
}

/// <summary>Test double for EF Core's CommentAttribute — matched by simple name "CommentAttribute".</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CommentAttribute : Attribute
{
    public CommentAttribute(string comment) { Comment = comment; }
    public string Comment { get; }
}

public class UnicodeSample
{
    [Key] public int Id { get; set; }
    public string DefaultStr { get; set; } = string.Empty;
    [Unicode(false)] public string AsciiStr { get; set; } = string.Empty;
}

/// <summary>Test double for EF Core's UnicodeAttribute — matched by simple name "UnicodeAttribute".</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UnicodeAttribute : Attribute
{
    public UnicodeAttribute(bool isUnicode = true) { IsUnicode = isUnicode; }
    public bool IsUnicode { get; }
}

[Table("TBL_WIDGET")]
public class TabledWidget { [Key] public int Id { get; set; } public string Name { get; set; } = string.Empty; }

/// <summary>Test double for EF Core's PrecisionAttribute — the reader matches it by simple name.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PrecisionAttribute : Attribute
{
    public PrecisionAttribute(int precision, int scale) { Precision = precision; Scale = scale; }
    public int Precision { get; }
    public int Scale { get; }
}

/// <summary>Test double for EF Core's IndexAttribute — matched by simple name "IndexAttribute".</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class IndexAttribute : Attribute
{
    public IndexAttribute(params string[] propertyNames) { PropertyNames = propertyNames; }
    public IReadOnlyList<string> PropertyNames { get; }
    public string? Name { get; set; }
    public bool IsUnique { get; set; }
}
