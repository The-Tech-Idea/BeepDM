using Moq;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Winform.Controls;
using Xunit;

namespace FrameworkReliabilityTests;

public sealed class ConnectionNotificationTests
{
    private static (BeepConnectionRepository Repository, Mock<IConnectionStorageProvider> Storage) Fixture(bool saved = true)
    {
        var storage = new Mock<IConnectionStorageProvider>();
        storage.Setup(s => s.SaveConnections(It.IsAny<ConnectionStorageScope>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<ConnectionProperties>>())).Returns(saved);
        storage.Setup(s => s.AddOrUpdate(It.IsAny<ConnectionStorageScope>(), It.IsAny<string>(), It.IsAny<ConnectionProperties>(), true)).Returns(saved);
        storage.Setup(s => s.Remove(It.IsAny<ConnectionStorageScope>(), It.IsAny<string>(), It.IsAny<string>(), true)).Returns(saved);
        storage.Setup(s => s.LoadConnections(It.IsAny<ConnectionStorageScope>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Array.Empty<ConnectionProperties>());
        string message = "storage result";
        storage.Setup(s => s.Promote(It.IsAny<ConnectionStorageScope>(), It.IsAny<ConnectionStorageScope>(), It.IsAny<string>(), It.IsAny<ConnectionConflictPolicy>(), out message)).Returns(saved);
        storage.Setup(s => s.ImportPackage(It.IsAny<ConnectionStorageScope>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ConnectionConflictPolicy>(), It.IsAny<bool>(), out message)).Returns(saved);
        return (new BeepConnectionRepository(Mock.Of<IBeepService>(), storage.Object), storage);
    }

    private static bool Mutate(BeepConnectionRepository repository, string operation) => operation switch
    {
        "Save" => repository.Save(new List<ConnectionProperties>()),
        "AddOrUpdate" => repository.AddOrUpdate(new ConnectionProperties { ConnectionName = "test" }),
        "Remove" => repository.Remove("test"),
        "Promote" => repository.Promote(ConnectionStorageScope.User, ConnectionConflictPolicy.Skip, out _),
        "ImportPackage" => repository.ImportPackage("package.json", ConnectionConflictPolicy.Skip, false, out _),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    [Theory]
    [InlineData("Save")]
    [InlineData("AddOrUpdate")]
    [InlineData("Remove")]
    [InlineData("Promote")]
    [InlineData("ImportPackage")]
    public void CommittedWrite_ThrowingObserver_DoesNotChangeOutcomeOrSkipLaterObservers(string operation)
    {
        var (repository, _) = Fixture();
        int observed = 0;
        repository.ConnectionsChanged += (_, _) => throw new InvalidOperationException("observer sentinel credential");
        repository.ConnectionsChanged += (_, _) => observed++;
        Assert.True(Mutate(repository, operation));
        Assert.Equal(1, observed);
    }

    [Fact]
    public void FailedWrite_DoesNotNotify_AndPreservesStorageException()
    {
        var (repository, storage) = Fixture(false);
        int observed = 0;
        repository.ConnectionsChanged += (_, _) => observed++;
        Assert.False(repository.Save(new List<ConnectionProperties>()));
        var failure = new IOException("primary storage failure");
        storage.Setup(s => s.SaveConnections(It.IsAny<ConnectionStorageScope>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<ConnectionProperties>>())).Throws(failure);
        Assert.Same(failure, Assert.Throws<IOException>(() => repository.Save(new List<ConnectionProperties>())));
        Assert.Equal(0, observed);
    }

    [Fact]
    public void Observer_CanWaitForRepositoryAccessOnAnotherThread()
    {
        var (repository, _) = Fixture();
        bool completed = false;
        repository.ConnectionsChanged += (_, _) =>
        {
            Task.Run(() => repository.LoadConnections()).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            completed = true;
        };
        Assert.True(repository.Save(new List<ConnectionProperties>()));
        Assert.True(completed);
    }

    [Fact]
    public void DiagnosticSubscribersAndLoggerFailures_CannotReclassifyCommitOrLeakMessages()
    {
        var storage = Fixture().Storage;
        var logger = new Mock<IDMLogger>();
        var logs = new List<string>();
        logger.Setup(l => l.WriteLog(It.IsAny<string>())).Callback<string>(logs.Add).Throws(new IOException(CredentialProtectionTests.Secret));
        var service = new Mock<IBeepService>();
        service.SetupGet(s => s.lg).Returns(logger.Object);
        var repository = new BeepConnectionRepository(service.Object, storage.Object);
        repository.ConnectionsChanged += (_, _) => throw new InvalidOperationException(CredentialProtectionTests.Secret);
        repository.NotificationFailed += (_, _) => throw new IOException(CredentialProtectionTests.Secret);
        ConnectionCatalogNotificationFailureEventArgs? observed = null;
        repository.NotificationFailed += (_, e) => observed = e;
        Assert.True(repository.Save(new List<ConnectionProperties>()));
        Assert.NotNull(observed);
        Assert.Equal("Save", observed.Operation);
        Assert.Equal(ConnectionStorageScope.Project, observed.Scope);
        Assert.Equal(nameof(InvalidOperationException), observed.ExceptionType);
        Assert.Equal(2, logs.Count);
        Assert.All(logs, log => Assert.DoesNotContain(CredentialProtectionTests.Secret, log));
    }

    [Fact]
    public void NotificationList_IsCapturedForOneChange_AndInvalidScopeIsRejectedBeforeStorage()
    {
        var (repository, storage) = Fixture();
        int observed = 0;
        EventHandler late = (_, _) => observed++;
        repository.ConnectionsChanged += (_, _) => repository.ConnectionsChanged += late;
        Assert.True(repository.Save(new List<ConnectionProperties>()));
        Assert.Equal(0, observed);
        Assert.True(repository.Save(new List<ConnectionProperties>()));
        Assert.Equal(1, observed);
        repository.ActiveScope = (ConnectionStorageScope)999;
        Assert.Throws<ArgumentOutOfRangeException>(() => repository.Save(new List<ConnectionProperties>()));
        storage.Verify(s => s.SaveConnections(It.IsAny<ConnectionStorageScope>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<ConnectionProperties>>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SlowObserver_DoesNotHoldScopeLock()
    {
        var (repository, _) = Fixture();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        repository.ConnectionsChanged += (_, _) =>
        {
            entered.Set();
            Assert.True(release.Wait(TimeSpan.FromSeconds(10)));
        };
        var save = Task.Run(() => repository.Save(new List<ConnectionProperties>()));
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            Assert.Empty(await Task.Run(() => repository.LoadConnections()).WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            release.Set();
            Assert.True(await save.WaitAsync(TimeSpan.FromSeconds(5)));
        }
    }
}
