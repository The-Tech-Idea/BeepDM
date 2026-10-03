using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.ConfigUtil.Managers;
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.JsonLoaderService;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace FrameworkReliabilityTests;

public sealed class ConnectionAcknowledgementTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BeepDM-ConnectionAck", Guid.NewGuid().ToString("N"));
    private string PathName => Path.Combine(_folder, "DataConnections.json");
    private readonly CredentialTestKeys _keys = new();
    public ConnectionAcknowledgementTests() => Directory.CreateDirectory(_folder);

    private DataConnectionManager Manager(IJsonLoader? loader = null, IConnectionSecretProtector? protector = null) =>
        new(null, loader ?? new JsonLoader(), _folder, protector ?? _keys.Protector());

    [Fact]
    public void Fallback_AcknowledgesEncryptedSnapshot_AndReloadDoesNotAliasLiveInput()
    {
        var manager = Manager();
        manager.DataConnections.Add(CredentialProtectionTests.SensitiveConnection());
        Assert.Equal(PersistenceWriteStatus.Saved, manager.SaveDataConnectionsAcknowledged().Status);
        Assert.DoesNotContain(CredentialProtectionTests.Secret, File.ReadAllText(PathName));
        var restarted = Manager();
        var loaded = Assert.Single(restarted.LoadDataConnectionsValues());
        Assert.Equal(CredentialProtectionTests.Secret, loaded.ParameterList["token"]);
        loaded.ParameterList["token"] = "changed";
        Assert.Equal(CredentialProtectionTests.Secret, manager.DataConnections[0].ParameterList["token"]);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("throws")]
    public void CatalogFailure_IsObservable_AndNeverFallsBackToRawJson(string mode)
    {
        var catalog = new Mock<IConnectionCatalogRepository>();
        var failure = new IOException("catalog failed");
        if (mode == "false") catalog.Setup(c => c.Save(It.IsAny<IReadOnlyList<ConnectionProperties>>())).Returns(false);
        else catalog.Setup(c => c.Save(It.IsAny<IReadOnlyList<ConnectionProperties>>())).Throws(failure);
        var manager = Manager();
        manager.DataConnections.Add(CredentialProtectionTests.SensitiveConnection());
        manager.CatalogRepository = catalog.Object;
        var outcome = manager.SaveDataConnectionsAcknowledged();
        Assert.Equal(PersistenceWriteStatus.Failed, outcome.Status);
        Assert.NotNull(outcome.Error);
        if (mode == "throws") Assert.Same(failure, outcome.Error);
        Assert.Throws<IOException>(() => manager.SaveDataConnectionsValues());
        Assert.False(File.Exists(PathName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[null]")]
    [InlineData("[{\"ConnectionName\":\"one\",\"ConnectionName\":\"two\"}]")]
    [InlineData("[] []")]
    public void ExistingCorruption_IsNotOverwritten_AndDoesNotResetLiveState(string invalid)
    {
        File.WriteAllText(PathName, invalid);
        var manager = Manager();
        var memory = manager.DataConnections;
        memory.Add(CredentialProtectionTests.SensitiveConnection());
        Assert.NotNull(Record.Exception(() => manager.LoadDataConnectionsValues()));
        Assert.Same(memory, manager.DataConnections);
        Assert.Equal(PersistenceWriteStatus.Failed, manager.SaveDataConnectionsAcknowledged().Status);
        Assert.Equal(invalid, File.ReadAllText(PathName));
    }

    [Fact]
    public void MissingFile_IsTheOnlyFreshState_AndPlaintextLegacyIsUpgradedOnSave()
    {
        var manager = Manager();
        Assert.Empty(manager.LoadDataConnectionsValues());
        File.WriteAllText(PathName, "[{\"ConnectionName\":\"legacy\",\"GuidID\":\"legacy-id\",\"Password\":\"" + CredentialProtectionTests.Secret + "\"}]");
        Assert.Equal(CredentialProtectionTests.Secret, Assert.Single(manager.LoadDataConnectionsValues()).Password);
        manager.SaveDataConnectionsValues();
        Assert.DoesNotContain(CredentialProtectionTests.Secret, File.ReadAllText(PathName));
        Assert.Equal(CredentialProtectionTests.Secret, Assert.Single(Manager().LoadDataConnectionsValues()).Password);
    }

    [Fact]
    public void MissingKey_BlocksReadAndOverwrite_AndKeepsOriginalBytes()
    {
        var manager = Manager();
        manager.DataConnections.Add(CredentialProtectionTests.SensitiveConnection());
        manager.SaveDataConnectionsValues();
        var saved = File.ReadAllBytes(PathName);
        _keys.Keys.Clear();
        var restarted = Manager();
        Assert.Throws<CryptographicException>(() => restarted.LoadDataConnectionsValues());
        Assert.Equal(PersistenceWriteStatus.Failed, restarted.SaveDataConnectionsAcknowledged().Status);
        Assert.Equal(saved, File.ReadAllBytes(PathName));
    }

    [Theory]
    [InlineData("serialize")]
    [InlineData("key")]
    [InlineData("unsupported-loader")]
    public void PreparationFailure_PreservesPreviousFile(string fault)
    {
        var manager = Manager();
        manager.DataConnections.Add(CredentialProtectionTests.SensitiveConnection());
        manager.SaveDataConnectionsValues();
        var saved = File.ReadAllBytes(PathName);
        if (fault == "key") _keys.Keys.Clear();
        var loader = new Mock<IJsonLoader>();
        if (fault == "serialize") loader.As<IJsonSnapshotCodec>()
            .Setup(c => c.SerializeSnapshot(It.IsAny<object>())).Throws(new IOException("serialization failed"));
        var failed = fault == "key" ? Manager() : Manager(loader.Object);
        failed.DataConnections.Add(CredentialProtectionTests.SensitiveConnection());
        var outcome = failed.SaveDataConnectionsAcknowledged();
        Assert.Equal(fault == "unsupported-loader" ? PersistenceWriteStatus.Unsupported : PersistenceWriteStatus.Failed, outcome.Status);
        Assert.Equal(saved, File.ReadAllBytes(PathName));
    }

    [Fact]
    public void CancellationBeforeSave_DoesNotCallCatalogOrCreateFile()
    {
        var catalog = new Mock<IConnectionCatalogRepository>(MockBehavior.Strict);
        var manager = Manager();
        manager.CatalogRepository = catalog.Object;
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.Equal(PersistenceWriteStatus.Cancelled, manager.SaveDataConnectionsAcknowledged(cancelled.Token).Status);
        catalog.VerifyNoOtherCalls();
        Assert.False(File.Exists(PathName));
    }

    [Fact]
    public async Task CancellationWhileWaitingForLease_PreservesFile()
    {
        var manager = Manager();
        manager.DataConnections.Add(CredentialProtectionTests.SensitiveConnection());
        manager.SaveDataConnectionsValues();
        var saved = File.ReadAllBytes(PathName);
        using var lease = new FileStream(PathName + ".beep.lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var cancelled = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var result = await Task.Run(() => manager.SaveDataConnectionsAcknowledged(cancelled.Token)).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(PersistenceWriteStatus.Cancelled, result.Status);
        Assert.Equal(saved, File.ReadAllBytes(PathName));
    }

    [Fact]
    public void WindowsReplacementFailure_LeavesPreviousBytesAndNoTempFile()
    {
        if (!OperatingSystem.IsWindows()) return; // Windows file-sharing semantics; Unix qualification is separate.
        var manager = Manager();
        manager.DataConnections.Add(CredentialProtectionTests.SensitiveConnection());
        manager.SaveDataConnectionsValues();
        var saved = File.ReadAllBytes(PathName);
        using (var held = new FileStream(PathName, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.Equal(PersistenceWriteStatus.Failed, manager.SaveDataConnectionsAcknowledged().Status);
        Assert.Equal(saved, File.ReadAllBytes(PathName));
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public void ConfigFacade_ExposesOptionalAcknowledgementAndCapturedProtection()
    {
        var policy = _keys.Protector();
        using var config = new ConfigEditor(Mock.Of<IDMLogger>(), new ErrorsInfo(), new JsonLoader(), _folder,
            "facade", BeepConfigType.DataConnector, policy);
        config.DataConnections.Add(CredentialProtectionTests.SensitiveConnection());
        Assert.Same(policy, ((IConnectionProtectionContext)config).ConnectionSecretProtector);
        Assert.True(((IConnectionConfigurationPersistence)config).SaveDataConnectionsAcknowledged().IsSaved);
        Assert.DoesNotContain(CredentialProtectionTests.Secret, File.ReadAllText(Path.Combine(config.ConfigPath, "DataConnections.json")));
    }

    [Fact]
    public void RuntimeOptions_CaptureDistinctPoliciesWithoutProcessWideMutation()
    {
        var firstPolicy = _keys.Protector();
        var secondPolicy = new CredentialTestKeys().Protector();
        BeepServiceOptions? captured = null;
        var services = new ServiceCollection();
        BeepServiceRegistration.AddBeepRuntime(services, o =>
        {
            o.DirectoryPath = _folder; o.AppRepoName = "first"; o.EnableAssemblyLoading = false;
            o.ConnectionSecretProtector = firstPolicy; captured = o;
        });
        captured!.ConnectionSecretProtector = secondPolicy;
        using var first = services.BuildServiceProvider();
        var secondServices = new ServiceCollection();
        BeepServiceRegistration.AddBeepRuntime(secondServices, o =>
        {
            o.DirectoryPath = _folder; o.AppRepoName = "second"; o.EnableAssemblyLoading = false;
            o.ConnectionSecretProtector = secondPolicy;
        });
        using var second = secondServices.BuildServiceProvider();
        var a = first.GetRequiredService<IBeepService>();
        var b = second.GetRequiredService<IBeepService>();
        Assert.Same(firstPolicy, ((IConnectionProtectionContext)a.Config_editor).ConnectionSecretProtector);
        Assert.Same(secondPolicy, ((IConnectionProtectionContext)b.Config_editor).ConnectionSecretProtector);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);
}
