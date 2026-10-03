using Moq;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Roslyn;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public class GeneratedTypeIdentityTests
{
    private static IDMEEditor Editor()
    {
        var editor = new Mock<IDMEEditor>();
        editor.SetupGet(value => value.classCreator).Returns(new ClassCreator(editor.Object));
        return editor.Object;
    }

    private static List<EntityField> Fields(string type) => new()
    {
        new() { FieldName = "Id", Fieldtype = type, IsKey = true }
    };

    [Fact]
    public void ChangedMetadataDoesNotReuseStaleGeneratedProperties()
    {
        var name = "Rows" + Guid.NewGuid().ToString("N");
        var editor = Editor();
        var first = DMTypeBuilder.GetOrCreateType(editor, name, "first", name, Fields("System.Int32"));
        var changed = DMTypeBuilder.GetOrCreateType(editor, name, "first", name, Fields("System.String"));
        Assert.Equal(typeof(int?), first.GetProperty("Id")!.PropertyType);
        Assert.Equal(typeof(string), changed.GetProperty("Id")!.PropertyType);
        Assert.NotSame(first, changed);
    }

    [Fact]
    public void SameNameInDifferentNamespacesDoesNotHitBareCompilerKey()
    {
        var name = "Rows" + Guid.NewGuid().ToString("N");
        var first = DMTypeBuilder.GetOrCreateType(Editor(), "First", "first", name, Fields("System.Int32"));
        var second = DMTypeBuilder.GetOrCreateType(Editor(), "Second", "second", name, Fields("System.String"));
        Assert.Equal("First." + name, first.FullName);
        Assert.Equal("Second." + name, second.FullName);
        Assert.Equal(typeof(string), second.GetProperty("Id")!.PropertyType);
    }

    [Fact]
    public void CompilerSelectsExactNameInsteadOfFirstSubstringMatch()
    {
        var name = "Row" + Guid.NewGuid().ToString("N");
        var compiled = RoslynCompiler.CompileClassTypeandAssembly(name,
            $"namespace Generated {{ public class {name}Helper {{ }} public class {name} {{ public int Id {{ get; set; }} }} }}");
        Assert.Equal(name, compiled.Item1.Name);
        Assert.NotNull(compiled.Item1.GetProperty("Id"));
    }

    [Fact]
    public void CompilerUsesCodeIdentityForSameRequestedName()
    {
        var name = "Row" + Guid.NewGuid().ToString("N");
        var first = RoslynCompiler.CompileClassTypeandAssembly(name,
            $"namespace Generated {{ public class {name} {{ public int Id {{ get; set; }} }} }}");
        var second = RoslynCompiler.CompileClassTypeandAssembly(name,
            $"namespace Generated {{ public class {name} {{ public string Id {{ get; set; }} }} }}");
        Assert.Equal(typeof(int), first.Item1.GetProperty("Id")!.PropertyType);
        Assert.Equal(typeof(string), second.Item1.GetProperty("Id")!.PropertyType);
        Assert.NotSame(first.Item1, second.Item1);
    }

    [Fact]
    public void EquivalentMetadataReusesTypeAcrossEditorsWithoutCarryingEditorState()
    {
        var name = "Rows" + Guid.NewGuid().ToString("N");
        var first = DMTypeBuilder.GetOrCreateType(Editor(), name, "first", name, Fields("System.Int32"));
        var second = DMTypeBuilder.GetOrCreateType(Editor(), name, "second", name, Fields("System.Int32"));
        Assert.Same(first, second);
        Assert.True(typeof(Entity).IsAssignableFrom(second));
        var value = Activator.CreateInstance(second)!;
        second.GetProperty("Id")!.SetValue(value, 7);
        Assert.Equal(7, second.GetProperty("Id")!.GetValue(value));
    }

    [Fact]
    public void ChangedAnnotationsGenerateFreshValidationMetadata()
    {
        var name = "Rows" + Guid.NewGuid().ToString("N");
        var editor = Editor();
        var fields = Fields("System.String");
        fields[0].IsRequired = false;
        var first = DMTypeBuilder.GetOrCreateType(editor, name, "first", name, fields);
        fields[0].IsRequired = true;
        fields[0].MaxLength = 27;
        var second = DMTypeBuilder.GetOrCreateType(editor, name, "first", name, fields);
        Assert.Null(first.GetProperty("Id")!.GetCustomAttribute<RequiredAttribute>());
        Assert.NotNull(second.GetProperty("Id")!.GetCustomAttribute<RequiredAttribute>());
        Assert.Equal(27, second.GetProperty("Id")!.GetCustomAttribute<MaxLengthAttribute>()!.Length);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void LegacyBareNameSeedCannotOverrideActualMetadata()
    {
        var name = "Rows" + Guid.NewGuid().ToString("N");
        var key = name + "." + name;
        DMTypeBuilder.typeCache[key] = typeof(string);
        try
        {
            var type = DMTypeBuilder.GetOrCreateType(Editor(), name, "first", name, Fields("System.Int32"));
            Assert.Equal(typeof(int?), type.GetProperty("Id")!.PropertyType);
            Assert.NotEqual(typeof(string), type);
            Assert.Equal(typeof(string), DMTypeBuilder.typeCache[key]);
        }
        finally { DMTypeBuilder.typeCache.TryRemove(key, out _); }
    }

    [Fact]
    public void MetadataIsCapturedBeforeCustomGenerationMutatesCallerFields()
    {
        var editor = new Mock<IDMEEditor>();
        var creator = new Mock<IClassCreator>();
        editor.SetupGet(value => value.classCreator).Returns(creator.Object);
        var fields = Fields("System.Int32");
        var name = "Rows" + Guid.NewGuid().ToString("N");
        creator.Setup(value => value.CreateEntityClass(It.IsAny<EntityStructure>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), false))
            .Returns((EntityStructure captured, string header, string extra, string path, string ns, bool files) =>
            {
                Assert.NotSame(fields, captured.Fields);
                Assert.NotSame(fields[0], captured.Fields[0]);
                fields[0].Fieldtype = "System.String";
                return new ClassCreator(editor.Object).CreateEntityClass(captured, header, extra, path, ns, files);
            });
        var type = DMTypeBuilder.GetOrCreateType(editor.Object, name, "first", name, fields);
        Assert.Equal(typeof(int?), type.GetProperty("Id")!.PropertyType);
        Assert.Equal("System.String", fields[0].Fieldtype);
    }

    [Fact]
    public async Task ConcurrentSameSourceCompilationReturnsOneTypeAndAssembly()
    {
        var name = "Row" + Guid.NewGuid().ToString("N");
        var code = $"namespace Generated {{ public class {name} {{ public int Id {{ get; set; }} }} }}";
        var results = await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() => RoslynCompiler.CompileClassTypeandAssembly(name, code))));
        Assert.All(results, result =>
        {
            Assert.Same(results[0].Item1, result.Item1);
            Assert.Same(results[0].Item2, result.Item2);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConcurrentObjectCreationReturnsItsOwnSchema(bool datasourceOverload)
    {
        var name = "Rows" + Guid.NewGuid().ToString("N");
        var editor = Editor();
        var results = await Task.WhenAll(Enumerable.Range(0, 24).Select(index => Task.Run(() =>
        {
            var expected = index % 2 == 0 ? typeof(int?) : typeof(string);
            var fields = Fields(index % 2 == 0 ? "System.Int32" : "System.String");
            var value = datasourceOverload ? DMTypeBuilder.CreateNewObject(editor, name, "ds", name, fields) :
                DMTypeBuilder.CreateNewObject(editor, name, name, fields);
            return (expected, value);
        })));
        Assert.All(results, result => Assert.Equal(result.expected, result.value.GetType().GetProperty("Id")!.PropertyType));
    }

    [Fact]
    public void CompilerDoesNotReturnPrefixTypeWhenExactTypeIsAbsent()
    {
        var name = "Row" + Guid.NewGuid().ToString("N");
        var result = RoslynCompiler.CompileClassTypeandAssembly(name,
            $"namespace Generated {{ public class {name}Helper {{ }} }}");
        Assert.Null(result.Item1);
        Assert.NotNull(result.Item2);
    }

    [Fact]
    public void FullNamesDisambiguateSameSimpleName()
    {
        var name = "Row" + Guid.NewGuid().ToString("N");
        var code = $"namespace First {{ public class {name} {{ }} }} namespace Second {{ public class {name} {{ }} }}";
        Assert.Throws<InvalidOperationException>(() => RoslynCompiler.CompileClassTypeandAssembly(name, code));
        var result = RoslynCompiler.CompileClassTypeandAssembly("Second." + name, code);
        Assert.Equal("Second." + name, result.Item1.FullName);
    }

    [Fact]
    public void EmitFailureIsSafeAndDoesNotMaskLaterValidSource()
    {
        var name = "Row" + Guid.NewGuid().ToString("N");
        var invalid = $"namespace Generated {{ public class {name} {{ public secret_row_value_bad_type Id {{ get; set; }} }} }}";
        var error = Assert.Throws<InvalidOperationException>(() => RoslynCompiler.CompileClassTypeandAssembly(name, invalid));
        Assert.DoesNotContain("secret", error.Message);
        Assert.Null(error.InnerException);
        Assert.Contains("CS0246", error.Message);
        var valid = RoslynCompiler.CompileClassTypeandAssembly(name,
            $"namespace Generated {{ public class {name} {{ public int Id {{ get; set; }} }} }}");
        Assert.Equal(typeof(int), valid.Item1.GetProperty("Id")!.PropertyType);
    }

    [Fact]
    public void TargetedCompilerRemovalDropsAllSourceVariantsOfOnlyThatRequest()
    {
        var name = "Row" + Guid.NewGuid().ToString("N");
        var intCode = $"namespace Generated {{ public class {name} {{ public int Id {{ get; set; }} }} }}";
        var stringCode = $"namespace Generated {{ public class {name} {{ public string Id {{ get; set; }} }} }}";
        var first = RoslynCompiler.CompileClassTypeandAssembly(name, intCode);
        var second = RoslynCompiler.CompileClassTypeandAssembly(name, stringCode);
        var qualified = RoslynCompiler.CompileClassTypeandAssembly("Generated." + name, intCode);
        Assert.True(RoslynCompiler.RemoveFromCache(name));
        Assert.False(RoslynCompiler.RemoveFromCache(name));
        Assert.NotSame(first.Item1, RoslynCompiler.CompileClassTypeandAssembly(name, intCode).Item1);
        Assert.NotSame(second.Item1, RoslynCompiler.CompileClassTypeandAssembly(name, stringCode).Item1);
        Assert.Same(qualified.Item1, RoslynCompiler.CompileClassTypeandAssembly("Generated." + name, intCode).Item1);
    }

    [Fact]
    public void EntityTypeFactoryUsesCapturedSchemaAndTableAnnotations()
    {
        var name = "Rows" + Guid.NewGuid().ToString("N");
        var source = new EntityStructure(name)
        {
            DataSourceID = "ds", Fields = Fields("System.Int32"),
            HasDataAnnotations = true, SchemaOrOwnerOrDatabase = "first"
        };
        var first = EntityTypeFactory.GetOrCreate(Editor(), source)!;
        source.Fields[0].Fieldtype = "System.String";
        source.SchemaOrOwnerOrDatabase = "second";
        var second = EntityTypeFactory.GetOrCreate(Editor(), source)!;
        Assert.Equal(typeof(int?), first.GetProperty("Id")!.PropertyType);
        Assert.Equal(typeof(string), second.GetProperty("Id")!.PropertyType);
        Assert.Equal("second", second.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>()!.Schema);
        EntityTypeFactory.Invalidate("ds");
        Assert.Same(second, EntityTypeFactory.GetOrCreate(Editor(), source));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MissingDestinationMetadataCannotGenerateAnEmptyType(bool nullFields)
    {
        var name = "Rows" + Guid.NewGuid().ToString("N");
        Assert.Throws<ArgumentException>(() => DMTypeBuilder.GetOrCreateType(Editor(), name, "ds", name,
            nullFields ? null! : new List<EntityField>()));
    }
}
