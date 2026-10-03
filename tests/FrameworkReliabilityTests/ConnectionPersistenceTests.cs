using System.Diagnostics;
using System.Text.Json;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Winform.Controls;
using TheTechIdea.Beep.JsonLoaderService;
using Xunit;

namespace FrameworkReliabilityTests;

public sealed class ConnectionPersistenceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BeepDM-CatalogTests", Guid.NewGuid().ToString("N"));

    public ConnectionPersistenceTests() => Directory.CreateDirectory(_folder);

    private JsonConnectionStorageProvider Store()
    {
        var service = new Mock<IBeepService>();
        service.SetupGet(s => s.BeepDirectory).Returns(_folder);
        service.SetupGet(s => s.AppRepoName).Returns("catalog-test");
        return new JsonConnectionStorageProvider(service.Object);
    }

    [Fact]
    public async Task Catalog_MixedSyncAsyncInstances_PreserveAllUpdatesAndProfiles()
    {
        await Task.WhenAll(Enumerable.Range(0, 30).Select(index => Task.Run(async () =>
        {
            using var store = Store();
            var connection = new ConnectionProperties { ConnectionName = $"row-{index}", GuidID = $"id-{index}" };
            if (index % 2 == 0)
                Assert.True(store.AddOrUpdate(ConnectionStorageScope.Project, "Default", connection, true));
            else
                Assert.True(await store.AddOrUpdateAsync(ConnectionStorageScope.Project, "Default", connection, true));
        })));
        using var restarted = Store();
        Assert.Equal(30, restarted.LoadConnections(ConnectionStorageScope.Project, "Default", false).Count);
        Assert.True(await restarted.SaveConnectionsAsync(ConnectionStorageScope.Project, "Alternate", new[]
        {
            new ConnectionProperties { ConnectionName = "other-profile" }
        }));
        Assert.Equal(30, (await restarted.LoadConnectionsAsync(ConnectionStorageScope.Project, "Default", false)).Count);
        Assert.Single(restarted.LoadConnections(ConnectionStorageScope.Project, "Alternate", false));
    }

    [Fact]
    public async Task Catalog_SeparateProcessesAndParent_PreserveAllUpdates()
    {
        using var first = StartWorker("0");
        using var second = StartWorker("15");
        using var store = Store();
        for (int i = 0; i < 15; i++)
            Assert.True(await store.AddOrUpdateAsync(ConnectionStorageScope.Project, "Default", new ConnectionProperties
            {
                ConnectionName = $"parent-{i}", GuidID = $"parent-id-{i}"
            }, true));
        await Task.WhenAll(CompleteWorker(first), CompleteWorker(second));
        Assert.Equal(45, (await store.LoadConnectionsAsync(ConnectionStorageScope.Project, "Default", false)).Count);
    }

    [Fact]
    public void Catalog_Precedence_ProjectOverridesUserAndMachine()
    {
        using var store = Store();
        foreach (var scope in new[] { ConnectionStorageScope.Machine, ConnectionStorageScope.User, ConnectionStorageScope.Project })
            Assert.True(store.SaveConnections(scope, "Default", new[]
            {
                new ConnectionProperties { ConnectionName = scope.ToString(), GuidID = "same-identity" }
            }));
        Assert.Equal("Project", Assert.Single(store.LoadConnections(ConnectionStorageScope.Project, "Default", true)).ConnectionName);
        Assert.Equal("User", Assert.Single(store.LoadConnections(ConnectionStorageScope.User, "Default", true)).ConnectionName);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"PackageVersion\":\"3.0\",\"Records\":[]}")]
    [InlineData("{\"SourceScope\":\"Machine\",\"Records\":[]}")]
    public async Task Catalog_CorruptOrForeignFile_IsNotTreatedAsEmptyOrOverwritten(string invalid)
    {
        using var store = Store();
        store.SaveConnections(ConnectionStorageScope.Project, "Default", Array.Empty<ConnectionProperties>());
        var path = Path.Combine(_folder, "ConnectionCatalogs", "catalog-test", "project.connections.json");
        await File.WriteAllTextAsync(path, invalid);
        Assert.NotNull(await Record.ExceptionAsync(() => store.LoadConnectionsAsync(ConnectionStorageScope.Project, "Default", false)));
        Assert.NotNull(Record.Exception(() => store.SaveConnections(ConnectionStorageScope.Project, "Default", Array.Empty<ConnectionProperties>())));
        Assert.Equal(invalid, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Catalog_RemoveAndNonpersistingChanges_KeepExistingSemantics()
    {
        using var store = Store();
        var connection = new ConnectionProperties { ConnectionName = "kept", GuidID = "kept-id" };
        Assert.True(await store.AddOrUpdateAsync(ConnectionStorageScope.Project, "Default", connection, true));
        Assert.True(store.Remove(ConnectionStorageScope.Project, "Default", "kept", false));
        Assert.Single(await store.LoadConnectionsAsync(ConnectionStorageScope.Project, "Default", false));
        Assert.True(await store.RemoveAsync(ConnectionStorageScope.Project, "Default", "kept", true));
        Assert.False(store.Remove(ConnectionStorageScope.Project, "Default", "kept", true));
        Assert.Empty(store.LoadConnections(ConnectionStorageScope.Project, "Default", false));
    }

    [Fact]
    public async Task Catalog_Dispose_RejectsSyncAndAsyncCalls()
    {
        var store = Store();
        store.Dispose();
        Assert.Throws<ObjectDisposedException>(() => store.LoadConnections(ConnectionStorageScope.Project, "Default", false));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => store.LoadConnectionsAsync(ConnectionStorageScope.Project, "Default", false));
    }

    [Fact]
    public void Catalog_ExportImportAndPromote_PreserveRecordsAndEmptyOnlyPolicy()
    {
        using var store = Store();
        store.SaveConnections(ConnectionStorageScope.User, "Default", new[] { new ConnectionProperties { ConnectionName = "one", GuidID = "one-id" } });
        Assert.True(store.Promote(ConnectionStorageScope.User, ConnectionStorageScope.Project, "Default", ConnectionConflictPolicy.Skip, out _));
        var export = Path.Combine(_folder, "export.json");
        Assert.True(store.ExportPackage(ConnectionStorageScope.Project, "Default", export, false, out _));
        Assert.True(store.ImportPackage(ConnectionStorageScope.Machine, "Default", export, ConnectionConflictPolicy.Skip, true, out _));
        Assert.False(store.ImportPackage(ConnectionStorageScope.Machine, "Default", export, ConnectionConflictPolicy.Skip, true, out _));
        Assert.Single(store.LoadConnections(ConnectionStorageScope.Machine, "Default", false));
    }

    [Fact]
    public void JsonLoader_CorruptRead_ThrowsRatherThanReturningMissingDefault()
    {
        var path = Path.Combine(_folder, "config.json");
        File.WriteAllText(path, "{broken");
        var loader = new JsonLoader();
        Assert.ThrowsAny<Newtonsoft.Json.JsonException>(() => loader.DeserializeSingleObject<ConnectionProperties>(path));
        Assert.Null(loader.DeserializeSingleObject<ConnectionProperties>(Path.Combine(_folder, "missing.json")));
        Assert.Empty(loader.DeserializeObject<ConnectionProperties>(Path.Combine(_folder, "missing.json")));
    }

    private Process StartWorker(string offset)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "persistence-worker", "FrameworkPersistenceWorker.dll");
        Assert.True(File.Exists(path));
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true
        };
        foreach (var argument in new[] { path, "catalog", _folder, "15", offset }) start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }

    private static async Task CompleteWorker(Process process)
    {
        try
        {
            var errors = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
            Assert.True(process.ExitCode == 0, await errors);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);
}
