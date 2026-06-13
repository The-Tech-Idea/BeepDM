using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.SetUp.Tests;

public class SeederRegistryTests
{
    [Fact]
    public void Register_AddsSeeder()
    {
        var registry = new SeederRegistry();
        var seeder = new TestSeeder("seeder-1", "Test Seeder 1");
        var result = registry.Register(seeder);
        Assert.True(result);
    }

    [Fact]
    public void Register_DuplicateId_ReturnsFalse()
    {
        var registry = new SeederRegistry();
        var seeder = new TestSeeder("seeder-1", "Test Seeder 1");
        registry.Register(seeder);
        var result = registry.Register(new TestSeeder("seeder-1", "Duplicate"));
        Assert.False(result);
    }

    [Fact]
    public void GetOrderedSeeders_ReturnsInDependencyOrder()
    {
        var registry = new SeederRegistry();
        var seederA = new TestSeeder("a", "A");
        var seederB = new TestSeederWithDeps("b", "B", new[] { "a" });
        var seederC = new TestSeederWithDeps("c", "C", new[] { "b" });

        registry.Register(seederA);
        registry.Register(seederB);
        registry.Register(seederC);

        var ordered = registry.GetOrderedSeeders();
        Assert.Equal(3, ordered.Count);
        Assert.Equal("a", ordered[0].SeederId);
        Assert.Equal("b", ordered[1].SeederId);
        Assert.Equal("c", ordered[2].SeederId);
    }

    [Fact]
    public void GetOrderedSeeders_NoDependencies_ReturnsAll()
    {
        var registry = new SeederRegistry();
        registry.Register(new TestSeeder("x", "X"));
        registry.Register(new TestSeeder("y", "Y"));

        var ordered = registry.GetOrderedSeeders();
        Assert.Equal(2, ordered.Count);
    }

    [Fact]
    public void GetOrderedSeeders_CircularDependency_Throws()
    {
        var registry = new SeederRegistry();
        var seederA = new TestSeederWithDeps("a", "A", new[] { "b" });
        var seederB = new TestSeederWithDeps("b", "B", new[] { "a" });

        registry.Register(seederA);
        registry.Register(seederB);

        Assert.Throws<InvalidOperationException>(() => registry.GetOrderedSeeders());
    }

    [Fact]
    public void GetOrderedSeeders_UnknownDependency_Throws()
    {
        var registry = new SeederRegistry();
        var seeder = new TestSeederWithDeps("a", "A", new[] { "unknown" });
        registry.Register(seeder);

        Assert.Throws<InvalidOperationException>(() => registry.GetOrderedSeeders());
    }

    [Fact]
    public void Get_ReturnsRegisteredSeeder()
    {
        var registry = new SeederRegistry();
        var seeder = new TestSeeder("roles", "Roles Seeder");
        registry.Register(seeder);

        var found = registry.Get("roles");
        Assert.NotNull(found);
        Assert.Equal("roles", found!.SeederId);
    }

    [Fact]
    public void Get_ReturnsNull_ForUnknownId()
    {
        var registry = new SeederRegistry();
        Assert.Null(registry.Get("nonexistent"));
    }

    private sealed class TestSeeder : ISeeder
    {
        public TestSeeder(string id, string name) { SeederId = id; SeederName = name; }
        public string SeederId { get; }
        public string SeederName { get; }
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();
        bool ISeeder.IsAlreadySeeded(IDataSource dataSource, IDMEEditor editor) => false;
        TheTechIdea.Beep.ConfigUtil.IErrorsInfo ISeeder.Seed(IDataSource dataSource, IDMEEditor editor, IProgress<PassedArgs>? progress)
            => new ErrorsInfo { Flag = Errors.Ok };
    }

    private sealed class TestSeederWithDeps : ISeeder
    {
        private readonly string[] _deps;
        public TestSeederWithDeps(string id, string name, string[] deps)
        { SeederId = id; SeederName = name; _deps = deps; }
        public string SeederId { get; }
        public string SeederName { get; }
        public IReadOnlyList<string> DependsOn => _deps;
        bool ISeeder.IsAlreadySeeded(IDataSource dataSource, IDMEEditor editor) => false;
        TheTechIdea.Beep.ConfigUtil.IErrorsInfo ISeeder.Seed(IDataSource dataSource, IDMEEditor editor, IProgress<PassedArgs>? progress)
            => new ErrorsInfo { Flag = Errors.Ok };
    }
}
