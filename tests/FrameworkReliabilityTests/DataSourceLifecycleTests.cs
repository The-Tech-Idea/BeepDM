using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Caching;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace FrameworkReliabilityTests;

public sealed class DataSourceLifecycleTests
{
    public sealed class Control
    {
        public int Created;
        public int Disposals;
        public bool BlockFirst;
        public Action<ConnectionProperties>? OnConstruct;
        public readonly ManualResetEventSlim Release = new(false);
        public readonly TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<bool> Disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly ConcurrentBag<CountingSource> Sources = new();
    }

    public sealed class CountingSource : CachedMemoryDataSource
    {
        public static readonly ConditionalWeakTable<ConnectionProperties, Control> Controls = new();
        private readonly Control _control;

        public CountingSource(ConnectionProperties connection)
        {
            _control = Controls.GetValue(connection, _ => new Control());
            DatasourceName = connection.ConnectionName;
            GuidID = Guid.NewGuid().ToString();
            var attempt = Interlocked.Increment(ref _control.Created);
            _control.Sources.Add(this);
            _control.Started.TrySetResult(true);
            if (_control.BlockFirst && attempt == 1 && !_control.Release.Wait(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("The test did not release construction.");
            _control.OnConstruct?.Invoke(connection);
        }

        protected override void Dispose(bool disposing)
        {
            Interlocked.Increment(ref _control.Disposals);
            base.Dispose(disposing);
            _control.Disposed.TrySetResult(true);
        }
    }

    private sealed class Harness : IDisposable
    {
        public readonly ConnectionProperties Connection = new()
        {
            ConnectionName = "shared-name", DatabaseType = DataSourceType.CachedMemory,
            Category = DatasourceCategory.INMEMORY, IsInMemory = true
        };
        public readonly Control Control;
        public readonly Mock<IConfigEditor> Config = new();
        public readonly Mock<IAssemblyHandler> Loader = new();
        public readonly DMEEditor Editor;

        public Harness()
        {
            Control = CountingSource.Controls.GetValue(Connection, _ => new Control());
            Config.SetupProperty(c => c.DataConnections, new List<ConnectionProperties> { Connection });
            Config.SetupProperty(c => c.DataDriversClasses, new List<ConnectionDriversConfig>
            {
                new() { DatasourceType = DataSourceType.CachedMemory, classHandler = typeof(CountingSource).FullName! }
            });
            Config.SetupProperty(c => c.DataSourcesClasses, new List<AssemblyClassDefinition>
            {
                new() { className = typeof(CountingSource).FullName!, type = typeof(CountingSource) }
            });
            Loader.Setup(l => l.GetType(typeof(CountingSource).AssemblyQualifiedName!)).Returns(typeof(CountingSource));
            Editor = new DMEEditor(Mock.Of<IDMLogger>(), Mock.Of<IUtil>(), new ErrorsInfo(), Config.Object, Loader.Object);
        }

        public void Dispose() { Control.Release.Set(); Editor.Dispose(); }
    }

    [Fact]
    public async Task ParallelNameGuidSyncAsyncAndHelperLookups_CreateOneOwnedSource()
    {
        using var harness = new Harness();
        var tasks = Enumerable.Range(0, 32).Select(index => Task.Run(async () => (index % 4) switch
        {
            0 => harness.Editor.GetDataSource("SHARED-NAME"),
            1 => harness.Editor.GetDataSourceUsingGuidID(harness.Connection.GuidID.ToUpperInvariant()),
            2 => await harness.Editor.CreateNewDataSourceConnectionAsync(harness.Connection.ConnectionName),
            _ => await DataSourceLifecycleHelper.CreateDataSourceAsync(harness.Connection, harness.Editor, false)
        }));
        var sources = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(15));
        Assert.All(sources, source => Assert.Same(sources[0], source));
        Assert.NotNull(sources[0]);
        Assert.Equal(1, harness.Control.Created);
        Assert.Same(sources[0], Assert.Single(harness.Editor.DataSources));
        Assert.Equal(harness.Connection.GuidID, sources[0].GuidID);
        Assert.Same(harness.Connection, sources[0].Dataconnection.ConnectionProp);
        Assert.Same(harness.Config.Object.DataDriversClasses[0], sources[0].Dataconnection.DataSourceDriver);
        harness.Editor.Dispose();
        Assert.Equal(1, harness.Control.Disposals);
    }

    [Fact]
    public async Task SameNamesAcrossEditors_NeverShareOrUnregisterEachOthersSources()
    {
        using var first = new Harness();
        using var second = new Harness();
        var a = await first.Editor.CreateNewDataSourceConnectionAsync("shared-name");
        var b = await second.Editor.CreateNewDataSourceConnectionAsync("shared-name");
        Assert.NotSame(a, b);
        Assert.Same(a, DataSourceLifecycleHelper.GetCachedDataSource(first.Editor, "shared-name"));
        Assert.Same(b, DataSourceLifecycleHelper.GetCachedDataSource(second.Editor, "shared-name"));
        first.Editor.Dispose();
        Assert.Equal(1, first.Control.Disposals);
        Assert.Equal(0, second.Control.Disposals);
        Assert.Same(b, second.Editor.GetDataSource("shared-name"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemovalReleasesSourceAndInvalidatesBothAliases_WithoutDeletingNormalConnection(bool useGuid)
    {
        using var harness = new Harness();
        var original = await harness.Editor.CreateNewDataSourceConnectionAsync("shared-name");
        Assert.True(useGuid ? harness.Editor.RemoveDataDourceUsingGuidID(harness.Connection.GuidID) : harness.Editor.RemoveDataDource("shared-name"));
        Assert.Empty(harness.Editor.DataSources);
        Assert.Equal(1, harness.Control.Disposals);
        Assert.Single(harness.Config.Object.DataConnections);
        var replacement = harness.Editor.GetDataSourceUsingGuidID(harness.Connection.GuidID);
        Assert.NotSame(original, replacement);
        Assert.Same(replacement, harness.Editor.GetDataSource("shared-name"));
        await DataSourceLifecycleHelper.DisposeDataSourceAsync(original);
        Assert.Equal(1, harness.Control.Disposals);
        Assert.Same(replacement, harness.Editor.GetDataSource("shared-name"));
        harness.Editor.Dispose();
        Assert.Equal(2, harness.Control.Disposals);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingCreationRemovedByEitherAlias_CannotPublishAnOldGeneration(bool useGuid)
    {
        using var harness = new Harness();
        harness.Control.BlockFirst = true;
        var pending = Task.Run(() => harness.Editor.CreateNewDataSourceConnectionAsync("shared-name"));
        await harness.Control.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(useGuid ? harness.Editor.RemoveDataDourceUsingGuidID(harness.Connection.GuidID) : harness.Editor.RemoveDataDource("shared-name"));
        var replacement = await harness.Editor.CreateNewDataSourceConnectionAsync("shared-name");
        harness.Control.Release.Set();
        Assert.Null(await pending.WaitAsync(TimeSpan.FromSeconds(5)));
        await harness.Control.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Same(replacement, Assert.Single(harness.Editor.DataSources));
        Assert.Equal(1, harness.Control.Disposals);
    }

    [Fact]
    public async Task DisposalDuringConstruction_RejectsLatePublicationAndReleasesTheConstructedSource()
    {
        using var harness = new Harness();
        harness.Control.BlockFirst = true;
        var pending = Task.Run(() => harness.Editor.CreateNewDataSourceConnectionAsync("shared-name"));
        await harness.Control.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        harness.Editor.Dispose();
        harness.Control.Release.Set();
        Assert.Null(await pending.WaitAsync(TimeSpan.FromSeconds(5)));
        await harness.Control.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, harness.Control.Disposals);
        Assert.Null(harness.Editor.DataSources);
        Assert.Throws<ObjectDisposedException>(() => harness.Editor.GetDataSource("shared-name"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => harness.Editor.CreateNewDataSourceConnectionAsync(harness.Connection, "shared-name"));
    }

    [Fact]
    public async Task ConfigurationFailure_ReleasesUnpublishedSource_AndAllowsExplicitRetry()
    {
        using var harness = new Harness();
        harness.Connection.IsInMemory = false;
        harness.Config.Setup(c => c.LoadDataSourceEntitiesValues("shared-name")).Throws(new IOException("corrupt metadata"));
        Assert.Null(await harness.Editor.CreateNewDataSourceConnectionAsync("shared-name"));
        Assert.Empty(harness.Editor.DataSources);
        Assert.Equal(1, harness.Control.Disposals);
        harness.Connection.IsInMemory = true;
        Assert.NotNull(await harness.Editor.CreateNewDataSourceConnectionAsync("shared-name"));
        Assert.Equal(2, harness.Control.Created);
    }

    [Fact]
    public async Task DirectHelperDisposal_RemovesOnlyItsOwnedGeneration()
    {
        using var harness = new Harness();
        var source = await harness.Editor.CreateNewDataSourceConnectionAsync("shared-name");
        await DataSourceLifecycleHelper.DisposeDataSourceAsync(source);
        await DataSourceLifecycleHelper.DisposeDataSourceAsync(source);
        Assert.Empty(harness.Editor.DataSources);
        Assert.Equal(1, harness.Control.Disposals);
        Assert.NotSame(source, await harness.Editor.CreateNewDataSourceConnectionAsync("shared-name"));
    }

    [Fact]
    public void CleanupContinuesAfterCloseDisposeAndLoggerFailures_AndDoesNotDisposeTwice()
    {
        using var harness = new Harness();
        var failed = new Mock<IDataSource>();
        failed.SetupGet(s => s.DatasourceName).Returns("failed");
        failed.SetupGet(s => s.ConnectionStatus).Returns(ConnectionState.Open);
        failed.Setup(s => s.Closeconnection()).Throws(new IOException("close"));
        failed.Setup(s => s.Dispose()).Throws(new IOException("dispose"));
        var other = new Mock<IDataSource>();
        other.SetupGet(s => s.DatasourceName).Returns("other");
        var unreadable = new Mock<IDataSource>();
        unreadable.SetupGet(s => s.DatasourceName).Throws(new IOException("unavailable provider metadata"));
        var logger = new Mock<IDMLogger>();
        logger.Setup(l => l.WriteLog(It.IsAny<string>())).Throws(new IOException("logger"));
        harness.Editor.Logger = logger.Object;
        harness.Config.Setup(c => c.Dispose()).Throws(new IOException("config"));
        harness.Editor.DataSources.AddRange(new[] { failed.Object, failed.Object, other.Object, unreadable.Object });
        Parallel.For(0, 8, _ => harness.Editor.Dispose());
        failed.Verify(s => s.Closeconnection(), Times.Once);
        failed.Verify(s => s.Dispose(), Times.Once);
        other.Verify(s => s.Dispose(), Times.Once);
        unreadable.Verify(s => s.Dispose(), Times.Once);
        harness.Loader.Verify(l => l.Dispose(), Times.Once);
        harness.Config.Verify(c => c.Dispose(), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemovalReportsFailedCleanup_ButAlwaysDetachesAndDisposes(bool useGuid)
    {
        using var harness = new Harness();
        var source = new Mock<IDataSource>();
        source.SetupGet(s => s.DatasourceName).Returns("failed");
        source.SetupGet(s => s.GuidID).Returns("failed-guid");
        source.SetupGet(s => s.ConnectionStatus).Returns(ConnectionState.Open);
        source.Setup(s => s.Closeconnection()).Throws(new IOException("close failed"));
        harness.Editor.DataSources.Add(source.Object);
        Assert.False(useGuid ? harness.Editor.RemoveDataDourceUsingGuidID("failed-guid") : harness.Editor.RemoveDataDource("failed"));
        Assert.Empty(harness.Editor.DataSources);
        source.Verify(s => s.Dispose(), Times.Once);
        Assert.False(harness.Editor.RemoveDataDource("failed"));
    }

    [Fact]
    public async Task RemovalWaitsForAnAdmittedClose_AndDisposesAfterItCompletes()
    {
        using var harness = new Harness();
        using var release = new ManualResetEventSlim(false);
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new Mock<IDataConnection>();
        connection.Setup(c => c.CloseConn()).Returns(() =>
        {
            started.TrySetResult(true);
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            return ConnectionState.Closed;
        });
        var source = new Mock<IDataSource>();
        source.SetupGet(s => s.DatasourceName).Returns("close-race");
        source.SetupGet(s => s.Dataconnection).Returns(connection.Object);
        harness.Editor.DataSources.Add(source.Object);
        var close = Task.Run(() => harness.Editor.CloseDataSource("close-race"));
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var remove = Task.Run(() => harness.Editor.RemoveDataDource("close-race"));
            Assert.True(await Task.Run(() => SpinWait.SpinUntil(() => DataSourceLifecycleHelper.GetAllCachedDataSources(harness.Editor).Count == 0, TimeSpan.FromSeconds(5))));
            source.Verify(s => s.Dispose(), Times.Never);
            Assert.False(remove.IsCompleted);
            release.Set();
            Assert.True(await close.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.True(await remove.WaitAsync(TimeSpan.FromSeconds(5)));
            source.Verify(s => s.Dispose(), Times.Once);
        }
        finally { release.Set(); }
    }

    [Fact]
    public async Task ConstructorCanResolveAnotherSource_WithoutHoldingARegistryLock()
    {
        using var harness = new Harness();
        var dependency = new ConnectionProperties
        {
            ConnectionName = "dependency", DatabaseType = DataSourceType.CachedMemory,
            Category = DatasourceCategory.INMEMORY, IsInMemory = true
        };
        harness.Config.Object.DataConnections.Add(dependency);
        harness.Control.OnConstruct = _ => Assert.NotNull(harness.Editor.GetDataSource("dependency"));
        Assert.NotNull(await Task.Run(() => harness.Editor.GetDataSource("shared-name")).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(2, harness.Editor.DataSources.Count);
    }

    [Fact]
    public async Task RecursiveConstructionOfSameIdentity_FailsInsteadOfDeadlocking()
    {
        using var harness = new Harness();
        harness.Control.OnConstruct = _ => harness.Editor.GetDataSource("shared-name");
        Assert.Null(await Task.Run(() => harness.Editor.GetDataSource("shared-name")).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Empty(harness.Editor.DataSources);
        harness.Control.OnConstruct = null;
        Assert.NotNull(await harness.Editor.CreateNewDataSourceConnectionAsync("shared-name"));
    }

    [Fact]
    public void DuplicateNamesInLegacyList_AreStillAllReleasedAtShutdown()
    {
        using var harness = new Harness();
        var a = new Mock<IDataSource>();
        var b = new Mock<IDataSource>();
        a.SetupGet(s => s.DatasourceName).Returns("duplicate");
        b.SetupGet(s => s.DatasourceName).Returns("duplicate");
        harness.Editor.DataSources.AddRange(new[] { a.Object, b.Object });
        harness.Editor.Dispose();
        a.Verify(s => s.Dispose(), Times.Once);
        b.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task LocalCreationRejectsNonLocalImplementationsWithoutLeakingThem()
    {
        using var harness = new Harness();
        Assert.Null(await Task.Run(() => harness.Editor.CreateLocalDataSourceConnection(harness.Connection,
            "shared-name", typeof(CountingSource).FullName!)));
        Assert.Empty(harness.Editor.DataSources);
        Assert.Equal(1, harness.Control.Disposals);
    }

    [Fact]
    public void RemovalFromProviderCallback_DefersDisposalUntilTheCallbackReturns()
    {
        using var harness = new Harness();
        var source = new Mock<IDataSource>();
        var connection = new Mock<IDataConnection>();
        source.SetupGet(s => s.DatasourceName).Returns("callback");
        source.SetupGet(s => s.Dataconnection).Returns(connection.Object);
        connection.Setup(c => c.CloseConn()).Returns(() =>
        {
            Assert.True(harness.Editor.RemoveDataDource("callback"));
            source.Verify(s => s.Dispose(), Times.Never);
            return ConnectionState.Closed;
        });
        harness.Editor.DataSources.Add(source.Object);
        Assert.True(harness.Editor.CloseDataSource("callback"));
        source.Verify(s => s.Dispose(), Times.Once);
        Assert.Empty(harness.Editor.DataSources);
    }
}
