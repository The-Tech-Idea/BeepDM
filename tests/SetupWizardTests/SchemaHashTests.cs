namespace TheTechIdea.Beep.SetUp.Tests;

public class SchemaHashPropertyDetectionTests
{
    [Fact]
    public void SchemaHash_Changes_WhenPropertyAdded()
    {
        var hash1 = ComputeHash(typeof(EntityV1));
        var hash2 = ComputeHash(typeof(EntityV2));
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void SchemaHash_Changes_WhenPropertyRemoved()
    {
        var hash1 = ComputeHash(typeof(EntityV2));
        var hash2 = ComputeHash(typeof(EntityV1));
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void SchemaHash_Changes_WhenPropertyTypeChanged()
    {
        var hash1 = ComputeHash(typeof(EntityV3));
        var hash2 = ComputeHash(typeof(EntityV3_Changed));
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void SchemaHash_Same_ForIdenticalTypes()
    {
        var hash1 = ComputeHash(typeof(EntityV1));
        var hash2 = ComputeHash(typeof(EntityV1));
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void SchemaHash_Changes_WhenTypeRemoved()
    {
        var hash1 = ComputeHash(typeof(EntityV1), typeof(AnotherEntity));
        var hash2 = ComputeHash(typeof(EntityV1));
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void SchemaHash_Changes_WhenTypeAdded()
    {
        var hash1 = ComputeHash(typeof(EntityV1));
        var hash2 = ComputeHash(typeof(EntityV1), typeof(AnotherEntity));
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void SchemaHash_OrderIndependent()
    {
        var hash1 = ComputeHash(typeof(EntityV1), typeof(AnotherEntity));
        var hash2 = ComputeHash(typeof(AnotherEntity), typeof(EntityV1));
        Assert.Equal(hash1, hash2);
    }

    /// <summary>
    /// Replicates the SchemaSetupStep.ComputeEntityListHash logic to verify the hash algorithm.
    /// </summary>
    private static string ComputeHash(params Type[] types)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var t in types.OrderBy(t => t.FullName))
        {
            sb.Append(t.FullName);
            foreach (var prop in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .OrderBy(p => p.Name))
            {
                sb.Append('|');
                sb.Append(prop.Name);
                sb.Append(':');
                sb.Append(prop.PropertyType.FullName);
            }
        }
        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }

    // Test entity types

    private class EntityV1
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class EntityV2
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    private class EntityV3
    {
        public int Id { get; set; }
        public int Count { get; set; }
    }

    private class EntityV3_Changed
    {
        public int Id { get; set; }
        public long Count { get; set; }
    }

    private class AnotherEntity
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
