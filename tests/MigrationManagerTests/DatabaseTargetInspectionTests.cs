using TheTechIdea.Beep.Editor.Migration;
using TheTechIdea.Beep.Editor.SchemaMigration;
using Xunit;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

/// <summary>Blind spot: recording provider plus real ownership receipt files; not a native server or crash test.</summary>
public sealed class DatabaseTargetInspectionTests
{
    private static string ClaimPath() => Path.Combine(Path.GetTempPath(), "BeepDatabaseTargetInspection", Guid.NewGuid().ToString("N"), "claim.json");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Inspection_does_not_create_files_or_adopt_an_existing_database(bool exists)
    {
        var provider = new Provider { Exists = exists };
        var path = ClaimPath();
        var target = MigrationManager.BindDatabaseTarget(provider, path);
        var result = target.Inspect();
        Assert.Equal(exists, result.DatabaseExists);
        Assert.False(result.OwnershipConfirmed);
        Assert.False(result.RequiresReconciliation);
        Assert.False(Directory.Exists(Path.GetDirectoryName(path)));
        Assert.Equal(0, provider.Creates);
        Assert.Equal(0, provider.Stamps);
    }

    [Fact]
    public async Task Acknowledged_creation_is_reported_as_owned_without_another_create()
    {
        var provider = new Provider();
        var target = MigrationManager.BindDatabaseTarget(provider, ClaimPath());
        await target.EnsureCreatedAsync();
        var result = target.Inspect();
        Assert.True(result.DatabaseExists);
        Assert.True(result.OwnershipConfirmed);
        Assert.False(result.RequiresReconciliation);
        Assert.Equal(1, provider.Creates);
        Assert.Equal(1, provider.Stamps);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_dispatched_unacknowledged_create_requires_reconciliation_even_if_the_catalog_is_missing(bool existsAfterFailure)
    {
        var provider = new Provider { FailCreation = true, ExistsAfterFailure = existsAfterFailure };
        var target = MigrationManager.BindDatabaseTarget(provider, ClaimPath());
        await Assert.ThrowsAsync<IOException>(() => target.EnsureCreatedAsync());
        var result = target.Inspect();
        Assert.Equal(existsAfterFailure, result.DatabaseExists);
        Assert.False(result.OwnershipConfirmed);
        Assert.True(result.RequiresReconciliation);
        Assert.Equal(1, provider.Creates);
        Assert.Equal(0, provider.Stamps);
    }

    private sealed class Provider : IDatabaseTargetMigrationProvider
    {
        public DatabaseTargetMigrationIdentity Identity { get; } = new("Recording", "server", "catalog", "inspection-test");
        public bool Exists { get; set; }
        public bool FailCreation { get; init; }
        public bool ExistsAfterFailure { get; init; }
        public int Creates { get; private set; }
        public int Stamps { get; private set; }
        private Guid owner;
        public bool DatabaseExists() => Exists;
        public bool OwnerMatches(Guid expected) => Exists && owner == expected;
        public Task CreateDatabaseAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Creates++;
            if (FailCreation) { Exists = ExistsAfterFailure; throw new IOException("Creation acknowledgement unavailable."); }
            Exists = true;
            return Task.CompletedTask;
        }
        public Task StampOwnerAsync(Guid value, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Stamps++; owner = value;
            return Task.CompletedTask;
        }
    }
}
