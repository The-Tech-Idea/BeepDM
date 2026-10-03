using System.Reflection;
using System.Reflection.Emit;
using TheTechIdea.Beep.Report;
using FieldMergeStrategy = TheTechIdea.Beep.DataBase.EntitiesExtensions.FieldMergeStrategy;
using TheTechIdea.Beep.DataBase;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public class EntityMetadataSnapshotTests
{
    [Fact]
    public void FieldCloneTerminatesAndDoesNotCopyObservers()
    {
        var method = typeof(EntityField).GetMethod(nameof(EntityField.Clone))!;
        // Reject the known recursion before invoking it: stack overflow kills the test host.
        Assert.DoesNotContain(CalledMethods(method), called => called == method);
        var field = new EntityField { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true };
        int notifications = 0;
        field.PropertyChanged += (_, _) => notifications++;

        var copy = field.Clone();
        Assert.NotSame(field, copy);
        Assert.Equal(field.GuidID, copy.GuidID);
        Assert.Equal(field.Fieldtype, copy.Fieldtype);
        Assert.True(copy.IsKey);
        copy.FieldName = "CopiedId";
        Assert.Equal("Id", field.FieldName);
        Assert.Equal(0, notifications);
        field.FieldName = "SourceId";
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void CloneStructureOnlyCopiesFieldsAndRebindsKeys()
    {
        var source = Structure();
        var copy = source.CloneStructureOnly();
        Assert.NotSame(source.Fields[0], copy.Fields[0]);
        Assert.Same(copy.Fields[0], Assert.Single(copy.PrimaryKeys));
        copy.Fields[0].FieldName = "CopiedId";
        Assert.Equal("Id", source.Fields[0].FieldName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyMergeFieldsCopiesAddedAndOverwrittenFields(bool overwrite)
    {
        var source = Structure();
        var destination = new EntityStructure("destination");
        if (overwrite) destination.Fields.Add(new EntityField { FieldName = "Id" });
        destination.MergeFields(source, overwrite);
        var field = Assert.Single(destination.Fields);
        Assert.NotSame(source.Fields[0], field);
        Assert.Equal("System.Int32", field.Fieldtype);
        Assert.Same(field, Assert.Single(destination.PrimaryKeys));
    }

    [Theory]
    [InlineData(FieldMergeStrategy.KeepExisting)]
    [InlineData(FieldMergeStrategy.Overwrite)]
    [InlineData(FieldMergeStrategy.PreferNonNull)]
    public void StrategyMergeCopiesAddedFields(FieldMergeStrategy strategy)
    {
        var source = Structure();
        var destination = new EntityStructure("destination");
        destination.MergeFields(source, strategy);
        Assert.NotSame(source.Fields[0], Assert.Single(destination.Fields));
    }

    [Fact]
    public void PreferNonNullMergeCopiesExistingFieldInsteadOfMutatingIt()
    {
        var existing = new EntityField { FieldName = "Id", Fieldtype = "", Description = "" };
        var source = Structure();
        var destination = new EntityStructure("destination") { Fields = new() { existing } };
        destination.MergeFields(source, FieldMergeStrategy.PreferNonNull);
        var merged = Assert.Single(destination.Fields);
        Assert.NotSame(existing, merged);
        Assert.Equal("System.Int32", merged.Fieldtype);
        Assert.Equal("", existing.Fieldtype);
    }

    [Fact]
    public void LegacyStructureCloneRemainsShallow()
    {
        var source = Structure();
        var clone = (EntityStructure)source.Clone();
        Assert.NotSame(source, clone);
        Assert.Same(source.Fields, clone.Fields);
        Assert.Same(source.PrimaryKeys, clone.PrimaryKeys);
        Assert.Same(source.Indexes, clone.Indexes);
    }

    [Fact]
    public void SnapshotCopiesAllMetadataAndRetainsPrimaryKeyIdentity()
    {
        var source = Structure();
        var copy = EntityMetadataSnapshot.Capture(source);
        Assert.NotSame(source, copy);
        Assert.Equal(source.GuidID, copy.GuidID);
        Assert.Equal(source.EntityName, copy.EntityName);
        Assert.Same(copy.Fields[0], copy.PrimaryKeys[0]);
        Assert.Equal(source.Fields[0].GuidID, copy.Fields[0].GuidID);
        Assert.NotSame(source.Parameters[0], copy.Parameters[0]);
        Assert.NotSame(source.Relations[0], copy.Relations[0]);
        Assert.NotSame(source.Filters[0], copy.Filters[0]);
        Assert.NotSame(source.Indexes[0], copy.Indexes[0]);
        Assert.Equal(source.Indexes[0].GuidID, copy.Indexes[0].GuidID);
        Assert.Equal(source.Filters[0].GuidID, copy.Filters[0].GuidID);
        Assert.Equal(source.Relations[0].GuidID, copy.Relations[0].GuidID);
        Assert.Equal(source.Parameters[0].GuidID, copy.Parameters[0].GuidID);

        copy.Fields[0].Fieldtype = "System.String";
        copy.Parameters[0].StringValue = "copied";
        copy.Relations[0].RelatedEntityID = "CopiedParent";
        copy.Filters[0].FilterValue = "2";
        copy.Indexes[0].Columns[0] = "CopiedId";
        Assert.Equal("System.Int32", source.Fields[0].Fieldtype);
        Assert.Equal("initial", source.Parameters[0].StringValue);
        Assert.Equal("Parent", source.Relations[0].RelatedEntityID);
        Assert.Equal("1", source.Filters[0].FilterValue);
        Assert.Equal("Id", source.Indexes[0].Columns[0]);

        source.Fields.Clear();
        source.PrimaryKeys.Clear();
        source.Parameters.Clear();
        source.Relations.Clear();
        source.Filters.Clear();
        source.Indexes.Clear();
        Assert.Single(copy.Fields);
        Assert.Single(copy.PrimaryKeys);
        Assert.Single(copy.Parameters);
        Assert.Single(copy.Relations);
        Assert.Single(copy.Filters);
        Assert.Single(copy.Indexes);
    }

    [Fact]
    public void SnapshotPreservesNullCollectionsAndEntries()
    {
        var source = new EntityStructure
        {
            Fields = new() { null! }, PrimaryKeys = null!, Parameters = null!,
            Relations = null!, Indexes = new() { new EntityIndex { Columns = null!, Options = null! }, null! },
            Filters = null!
        };
        var copy = EntityMetadataSnapshot.Capture(source);
        Assert.Null(Assert.Single(copy.Fields));
        Assert.Null(copy.PrimaryKeys);
        Assert.Null(copy.Parameters);
        Assert.Null(copy.Relations);
        Assert.Null(copy.Filters);
        Assert.Null(copy.Indexes[0].Columns);
        Assert.Null(copy.Indexes[0].Options);
        Assert.Null(copy.Indexes[1]);
    }

    [Fact]
    public void SnapshotDoesNotConflateSameNameFieldsOrOrphanKeyDescriptors()
    {
        var source = Structure();
        var orphan = new EntityField { FieldName = "Id", Fieldtype = "System.Int64" };
        source.PrimaryKeys.Add(orphan);
        source.Fields.Add(source.Fields[0]);
        var copy = EntityMetadataSnapshot.Capture(source);
        Assert.Same(copy.Fields[0], copy.Fields[1]);
        Assert.Same(copy.Fields[0], copy.PrimaryKeys[0]);
        Assert.NotSame(copy.Fields[0], copy.PrimaryKeys[1]);
        Assert.NotSame(orphan, copy.PrimaryKeys[1]);
        Assert.Equal("System.Int64", copy.PrimaryKeys[1].Fieldtype);
    }

    [Fact]
    public void SnapshotCopiesNestedOptionsAndPreservesAliasesAndComparer()
    {
        var source = Structure();
        var bytes = new byte[] { 1, 2 };
        var numbers = new List<int?> { 1, null, 3 };
        var shared = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["bytes"] = bytes, ["numbers"] = numbers,
            ["scalars"] = new object[] { TimeSpan.FromSeconds(1), Guid.NewGuid(), typeof(int), DateOnly.MinValue }
        };
        source.Indexes[0].Options["nested"] = shared;
        source.Indexes[0].Options["alias"] = shared;
        var copy = EntityMetadataSnapshot.Capture(source);
        var options = copy.Indexes[0].Options;
        Assert.Same(options["nested"], options["ALIAS"]);
        var nested = Assert.IsType<Dictionary<string, object>>(options["nested"]);
        Assert.False(nested.ContainsKey("BYTES"));
        Assert.NotSame(shared, nested);
        var copiedBytes = Assert.IsType<byte[]>(nested["bytes"]);
        var copiedNumbers = Assert.IsType<List<int?>>(nested["numbers"]);
        Assert.Equal(numbers, copiedNumbers);
        Assert.Equal(shared["scalars"], nested["scalars"]);
        copiedBytes[0] = 9;
        copiedNumbers[0] = 9;
        Assert.Equal(1, bytes[0]);
        Assert.Equal(1, numbers[0]);
    }

    [Fact]
    public void SnapshotDoesNotRetainAnyModelEventObservers()
    {
        var source = Structure();
        int calls = 0;
        source.PropertyChanged += (_, _) => calls++;
        source.Fields[0].PropertyChanged += (_, _) => calls++;
        source.Parameters[0].PropertyChanged += (_, _) => calls++;
        source.Relations[0].PropertyChanged += (_, _) => calls++;
        source.Filters[0].PropertyChanged += (_, _) => calls++;
        var copy = EntityMetadataSnapshot.Capture(source);
        copy.EntityName = "Copied";
        copy.Fields[0].FieldName = "CopiedId";
        copy.Parameters[0].StringValue = "Copied";
        copy.Relations[0].RelatedEntityID = "Copied";
        copy.Filters[0].FilterValue = "Copied";
        Assert.Equal(0, calls);
        source.EntityName = "Changed";
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("custom")]
    [InlineData("cycle")]
    [InlineData("model-cycle")]
    [InlineData("derived-field")]
    [InlineData("derived-list")]
    [InlineData("custom-comparer")]
    [InlineData("matrix")]
    [InlineData("empty-custom-array")]
    [InlineData("depth")]
    [InlineData("size")]
    public void UnsupportedGraphsFailSafelyWithoutChangingSource(string scenario)
    {
        var source = Structure();
        object value = new Opaque();
        switch (scenario)
        {
            case "cycle":
                var cycle = new List<object>();
                cycle.Add(cycle);
                value = cycle;
                break;
            case "model-cycle": value = source; break;
            case "derived-field": source.Fields[0] = new CustomField(); value = true; break;
            case "derived-list": value = new CustomList(); break;
            case "custom-comparer":
                value = new Dictionary<string, object>(new CustomComparer());
                break;
            case "matrix": value = new int[1, 1]; break;
            case "empty-custom-array": value = Array.Empty<Opaque>(); break;
            case "depth":
                value = 1;
                for (int i = 0; i < 70; i++) value = new List<object> { value };
                break;
            case "size": value = new byte[10001]; break;
        }
        source.Indexes[0].Options["secret-option-key"] = value;
        var error = Assert.ThrowsAny<Exception>(() => EntityMetadataSnapshot.Capture(source));
        Assert.True(error is NotSupportedException or InvalidOperationException);
        Assert.DoesNotContain("secret", error.Message);
        Assert.Null(error.InnerException);
        Assert.Same(value, source.Indexes[0].Options["secret-option-key"]);
        Assert.Single(source.Fields);
        Assert.Single(source.Indexes);
    }

    [Fact]
    public void SnapshotRejectsDerivedRootWithoutInvokingItsCustomGetter()
    {
        Assert.Throws<NotSupportedException>(() => EntityMetadataSnapshot.Capture(new CustomStructure()));
    }

    [Fact]
    public void SnapshotRequiresSource()
    {
        Assert.Throws<ArgumentNullException>(() => EntityMetadataSnapshot.Capture(null!));
    }

    [Fact]
    public void SnapshotPreservesEveryScalarMetadataProperty()
    {
        var source = Structure();
        var models = new object[] { source, source.Fields[0], source.Parameters[0], source.Relations[0],
            source.Filters[0], source.Indexes[0] };
        foreach (var model in models)
        {
            foreach (var property in ScalarProperties(model))
            {
                var type = property.PropertyType;
                object value = type == typeof(string) ? "copied-" + property.Name :
                    type == typeof(bool) ? true : type == typeof(Type) ? typeof(decimal) :
                    type == typeof(DateTime) ? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) :
                    type.IsEnum ? Enum.GetValues(type).GetValue(Enum.GetValues(type).Length - 1)! :
                    Convert.ChangeType(7, type);
                property.SetValue(model, value);
            }
        }
        var snapshot = EntityMetadataSnapshot.Capture(source);
        var copies = new object[] { snapshot, snapshot.Fields[0], snapshot.Parameters[0], snapshot.Relations[0],
            snapshot.Filters[0], snapshot.Indexes[0] };
        for (int i = 0; i < models.Length; i++)
            foreach (var property in ScalarProperties(models[i]))
                Assert.Equal(property.GetValue(models[i]), property.GetValue(copies[i]));
    }

    private static IEnumerable<PropertyInfo> ScalarProperties(object model) =>
        model.GetType().GetProperties().Where(property => property.CanRead && property.CanWrite &&
            (property.PropertyType.IsValueType || property.PropertyType == typeof(string) ||
                property.PropertyType == typeof(Type)));

    private static EntityStructure Structure()
    {
        var field = new EntityField { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true };
        return new EntityStructure("Rows")
        {
            Fields = new() { field }, PrimaryKeys = new() { field },
            Parameters = new() { new EntityParameters { parameterName = "p", StringValue = "initial" } },
            Relations = new() { new RelationShipKeys { RelatedEntityID = "Parent", EntityColumnID = "Id" } },
            Filters = new() { new AppFilter { FieldName = "Id", FilterValue = "1", FieldType = typeof(int) } },
            Indexes = new() { new EntityIndex { Name = "ix", Columns = new() { "Id" } } }
        };
    }

    private sealed class Opaque
    {
        public override string ToString() => throw new InvalidOperationException("secret");
    }
    private sealed class CustomField : EntityField { }
    private sealed class CustomList : List<object> { }
    private sealed class CustomStructure : EntityStructure
    {
        public string Payload => throw new InvalidOperationException("secret");
    }
    private sealed class CustomComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);
        public int GetHashCode(string obj) => obj.GetHashCode();
    }

    private static IEnumerable<MethodBase?> CalledMethods(MethodInfo method)
    {
        var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(opcode => unchecked((ushort)opcode.Value));
        var il = method.GetMethodBody()!.GetILAsByteArray()!;
        for (int offset = 0; offset < il.Length;)
        {
            ushort value = il[offset++];
            if (value == 0xfe) value = (ushort)(0xfe00 | il[offset++]);
            var opcode = opcodes[value];
            if (opcode.OperandType == OperandType.InlineMethod)
                yield return method.Module.ResolveMethod(BitConverter.ToInt32(il, offset));
            offset += opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                _ => 4
            };
        }
    }
}
