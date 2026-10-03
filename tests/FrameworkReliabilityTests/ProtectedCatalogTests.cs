using System.Diagnostics;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.ConfigUtil.Managers;
using TheTechIdea.Beep.JsonLoaderService;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Winform.Controls;
using Xunit;

namespace FrameworkReliabilityTests;

public sealed class ProtectedCatalogTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BeepDM-ProtectedCatalog", Guid.NewGuid().ToString("N"));
    private readonly CredentialTestKeys _keys = new();
    private string CatalogPath(ConnectionStorageScope scope = ConnectionStorageScope.Project) =>
        Path.Combine(_folder, "ConnectionCatalogs", "protected-test", $"{scope.ToString().ToLowerInvariant()}.connections.json");
    private Mock<IBeepService> Service()
    {
        var service = new Mock<IBeepService>();
        service.SetupGet(s => s.BeepDirectory).Returns(_folder);
        service.SetupGet(s => s.AppRepoName).Returns("protected-test");
        return service;
    }
    private JsonConnectionStorageProvider Store() => new(Service().Object, _keys.Protector());
    public ProtectedCatalogTests() => Directory.CreateDirectory(_folder);

    [Fact]
    public async Task Catalog_EncryptsSyncAndAsyncWrites_AndDecryptsIndependentInstance()
    {
        using var store = Store();
        var original = CredentialProtectionTests.SensitiveConnection();
        Assert.True(store.SaveConnections(ConnectionStorageScope.Project, "Default", new[] { original }));
        var second = CredentialProtectionTests.SensitiveConnection();
        second.GuidID = "second"; second.ConnectionName = "second";
        Assert.True(await store.AddOrUpdateAsync(ConnectionStorageScope.Project, "Default", second, true));
        Assert.DoesNotContain(CredentialProtectionTests.Secret, File.ReadAllText(CatalogPath()));
        using var restarted = Store();
        var restored = await restarted.LoadConnectionsAsync(ConnectionStorageScope.Project, "Default", false);
        Assert.Equal(2, restored.Count);
        Assert.All(restored, c => Assert.Equal(CredentialProtectionTests.Secret, c.Headers[0].Headervalue));
        Assert.Equal(CredentialProtectionTests.Secret, original.Password);
        Assert.Null(original.ProtectedCredentialPayload);
    }

    [Theory]
    [InlineData("save")]
    [InlineData("async-save")]
    [InlineData("remove")]
    [InlineData("add")]
    public async Task Catalog_MissingOldKey_CannotOverwriteUnreadableEvidence(string mutation)
    {
        using var store = Store();
        store.SaveConnections(ConnectionStorageScope.Project, "Default", new[] { CredentialProtectionTests.SensitiveConnection() });
        var saved = File.ReadAllBytes(CatalogPath());
        _keys.Keys.Clear();
        Assert.Throws<CryptographicException>(() => store.LoadConnections(ConnectionStorageScope.Project, "Default", false));
        var error = await Record.ExceptionAsync(async () =>
        {
            if (mutation == "save") store.SaveConnections(ConnectionStorageScope.Project, "Default", Array.Empty<ConnectionProperties>());
            if (mutation == "async-save") await store.SaveConnectionsAsync(ConnectionStorageScope.Project, "Default", Array.Empty<ConnectionProperties>());
            if (mutation == "remove") store.Remove(ConnectionStorageScope.Project, "Default", "sensitive", true);
            if (mutation == "add") store.AddOrUpdate(ConnectionStorageScope.Project, "Default", new ConnectionProperties { ConnectionName = "metadata" }, true);
        });
        Assert.IsType<CryptographicException>(error);
        Assert.Equal(saved, File.ReadAllBytes(CatalogPath()));
    }

    [Fact]
    public void RedactedExport_WorksWithoutKeys_AndEncryptedExportFailsWithoutChangingTarget()
    {
        using var store = Store();
        store.SaveConnections(ConnectionStorageScope.Project, "Default", new[] { CredentialProtectionTests.SensitiveConnection() });
        _keys.Keys.Clear();
        var redacted = Path.Combine(_folder, "redacted.json");
        Assert.True(store.ExportPackage(ConnectionStorageScope.Project, "Default", redacted, false, out _));
        var redactedBytes = File.ReadAllText(redacted);
        Assert.DoesNotContain(CredentialProtectionTests.Secret, redactedBytes);
        Assert.Null(JObject.Parse(redactedBytes)["Records"]![0]!["Connection"]!["ProtectedCredentialPayload"]!.Value<string>());
        var encrypted = Path.Combine(_folder, "encrypted.json");
        File.WriteAllText(encrypted, "existing export evidence");
        Assert.Throws<CryptographicException>(() => store.ExportPackage(ConnectionStorageScope.Project, "Default", encrypted, true, out _));
        Assert.Equal("existing export evidence", File.ReadAllText(encrypted));
    }

    [Fact]
    public void EncryptedExportImportPromotionAndRename_RebindIdentityBeforeProtection()
    {
        using var store = Store();
        var original = CredentialProtectionTests.SensitiveConnection();
        store.SaveConnections(ConnectionStorageScope.User, "Default", new[] { original });
        Assert.True(store.Promote(ConnectionStorageScope.User, ConnectionStorageScope.Project, "Default", ConnectionConflictPolicy.Skip, out _));
        var export = Path.Combine(_folder, "encrypted.json");
        Assert.True(store.ExportPackage(ConnectionStorageScope.Project, "Default", export, true, out _));
        Assert.DoesNotContain(CredentialProtectionTests.Secret, File.ReadAllText(export));
        Assert.True(store.ImportPackage(ConnectionStorageScope.Project, "Default", export, ConnectionConflictPolicy.Rename, false, out _));
        var records = store.LoadConnections(ConnectionStorageScope.Project, "Default", false);
        Assert.Equal(2, records.Count);
        var renamed = Assert.Single(records, c => c.ConnectionName == "sensitive_Imported");
        Assert.NotEqual(original.GuidID, renamed.GuidID);
        Assert.Equal(CredentialProtectionTests.Secret, renamed.Password);
        Assert.Equal(CredentialProtectionTests.Secret, renamed.Headers[0].Headervalue);
        Assert.True(store.ImportPackage(ConnectionStorageScope.Machine, "Default", export, ConnectionConflictPolicy.Skip, true, out _));
        Assert.Equal(CredentialProtectionTests.Secret, Assert.Single(store.LoadConnections(ConnectionStorageScope.Machine, "Default", false)).Password);
    }

    [Fact]
    public void CatalogRotation_ReadsRetainedKey_AndRewritesUsingCurrentKey()
    {
        using var store = Store();
        store.SaveConnections(ConnectionStorageScope.Project, "Default", new[] { CredentialProtectionTests.SensitiveConnection() });
        _keys.Keys["next"] = RandomNumberGenerator.GetBytes(32); _keys.CurrentKeyId = "next";
        var loaded = store.LoadConnections(ConnectionStorageScope.Project, "Default", false);
        Assert.True(store.SaveConnections(ConnectionStorageScope.Project, "Default", loaded));
        _keys.Keys.Remove("first");
        using var restarted = Store();
        Assert.Equal(CredentialProtectionTests.Secret, Assert.Single(restarted.LoadConnections(ConnectionStorageScope.Project, "Default", false)).Password);
    }

    [Fact]
    public void RealCatalogObserverFailure_StillAcknowledgesSavedBytesThroughConfigManager()
    {
        using var store = Store();
        var repository = new BeepConnectionRepository(Service().Object, store);
        repository.ConnectionsChanged += (_, _) => throw new IOException(CredentialProtectionTests.Secret);
        var manager = new DataConnectionManager(null, new JsonLoader(), _folder, _keys.Protector()) { CatalogRepository = repository };
        manager.DataConnections.Add(CredentialProtectionTests.SensitiveConnection());
        Assert.Equal(PersistenceWriteStatus.Saved, manager.SaveDataConnectionsAcknowledged().Status);
        Assert.False(File.Exists(Path.Combine(_folder, "DataConnections.json")));
        Assert.DoesNotContain(CredentialProtectionTests.Secret, File.ReadAllText(CatalogPath()));
        Assert.Equal(CredentialProtectionTests.Secret, Assert.Single(manager.LoadDataConnectionsValues()).Password);
    }

    [Fact]
    public void VersionTwo_WritesAreExplicit_AndLegacyPlaintextPackageIsUpgraded()
    {
        using var store = Store();
        var legacy = new ConnectionCatalogPackage
        {
            Records = new() { new ConnectionCatalogRecord { Connection = CredentialProtectionTests.SensitiveConnection() } }
        };
        var incoming = Path.Combine(_folder, "legacy.json");
        File.WriteAllText(incoming, System.Text.Json.JsonSerializer.Serialize(legacy));
        Assert.True(store.ImportPackage(ConnectionStorageScope.Project, "Default", incoming, ConnectionConflictPolicy.Skip, false, out _));
        var bytes = File.ReadAllText(CatalogPath());
        var package = JObject.Parse(bytes);
        Assert.Equal("2.0", package["PackageVersion"]!.Value<string>());
        Assert.Equal("2.0", package["Records"]![0]!["PackageVersion"]!.Value<string>());
        Assert.DoesNotContain(CredentialProtectionTests.Secret, bytes);
        Assert.Equal(CredentialProtectionTests.Secret, Assert.Single(store.LoadConnections(ConnectionStorageScope.Project, "Default", false)).Password);
    }

    [Theory]
    [InlineData("outer-version")]
    [InlineData("record-version")]
    [InlineData("unversioned-envelope")]
    [InlineData("duplicate")]
    [InlineData("missing-name")]
    public void Catalog_RejectsUnsupportedAmbiguousAndInvalidPackages_WithoutReplacement(string fault)
    {
        using var store = Store();
        store.SaveConnections(ConnectionStorageScope.Project, "Default", new[] { CredentialProtectionTests.SensitiveConnection() });
        var package = JObject.Parse(File.ReadAllText(CatalogPath()));
        if (fault == "outer-version") package["PackageVersion"] = "99.0";
        if (fault == "record-version") package["Records"]![0]!["PackageVersion"] = "99.0";
        if (fault == "unversioned-envelope")
        {
            package["PackageVersion"] = "1.0"; package["Records"]![0]!["PackageVersion"] = "1.0";
        }
        if (fault == "missing-name") package["Records"]![0]!["Connection"]!["ConnectionName"] = "";
        var invalid = package.ToString(Newtonsoft.Json.Formatting.None);
        if (fault == "duplicate") invalid = invalid.Insert(1, "\"PackageVersion\":\"2.0\",");
        File.WriteAllText(CatalogPath(), invalid);
        Assert.Throws<InvalidDataException>(() => store.LoadConnections(ConnectionStorageScope.Project, "Default", false));
        Assert.Throws<InvalidDataException>(() => store.SaveConnections(ConnectionStorageScope.Project, "Default", Array.Empty<ConnectionProperties>()));
        Assert.Equal(invalid, File.ReadAllText(CatalogPath()));
    }

    [Fact]
    public async Task HostProvidedAesKey_ReloadsEncryptedCatalogInSeparateProcess()
    {
        using var store = Store();
        store.SaveConnections(ConnectionStorageScope.Project, "Default", new[] { CredentialProtectionTests.SensitiveConnection() });
        var worker = Path.Combine(AppContext.BaseDirectory, "persistence-worker", "FrameworkPersistenceWorker.dll");
        Assert.True(File.Exists(worker));
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var argument in new[] { worker, "protected-catalog", _folder }) start.ArgumentList.Add(argument);
        // Test-only external key handoff: not a production key store and never a command-line argument.
        start.Environment["BEEP_TEST_CREDENTIAL_KEY"] = Convert.ToBase64String(_keys.Keys["first"]);
        using var process = Process.Start(start)!;
        try
        {
            var errors = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
            Assert.True(process.ExitCode == 0, await errors);
        }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
        }
        Assert.DoesNotContain(Convert.ToBase64String(_keys.Keys["first"]), File.ReadAllText(CatalogPath()));
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);
}
