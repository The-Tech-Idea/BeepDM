using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace FrameworkReliabilityTests;

public sealed class RuntimeIsolationTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "BeepDM-RuntimeTests", Guid.NewGuid().ToString("N"));

    private ServiceCollection Services(string name, ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        BeepServiceRegistration.AddBeepRuntime(services, options => Options(options, name, lifetime));
        return services;
    }

    private void Options(BeepServiceOptions options, string name, ServiceLifetime lifetime)
    {
        options.DirectoryPath = Path.Combine(_folder, name);
        options.AppRepoName = name;
        options.ServiceLifetime = lifetime;
        options.EnableAssemblyLoading = false;
    }

    [Fact]
    public void DeferredRegistration_DoesNotCreateRuntimeOrStorage_AndResolutionDoesNotMutateDescriptors()
    {
        var services = Services("deferred", ServiceLifetime.Scoped);
        var count = services.Count;
        Assert.False(Directory.Exists(_folder));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IBeepService>();
        Assert.NotNull(runtime.DMEEditor);
        Assert.Equal(count, services.Count);
        Assert.Equal("deferred", runtime.AppRepoName);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void IndependentContainers_KeepDistinctOptionsAndMutableGraphs(ServiceLifetime lifetime)
    {
        using var first = Services("first", lifetime).BuildServiceProvider();
        using var second = Services("second", lifetime).BuildServiceProvider();
        using var firstScope = first.CreateScope();
        using var secondScope = second.CreateScope();
        var a = firstScope.ServiceProvider.GetRequiredService<IBeepService>();
        var b = secondScope.ServiceProvider.GetRequiredService<IBeepService>();

        Assert.NotSame(a, b);
        Assert.NotSame(a.DMEEditor, b.DMEEditor);
        Assert.NotSame(a.Config_editor, b.Config_editor);
        Assert.NotSame(a.LLoader, b.LLoader);
        Assert.NotSame(a.Erinfo, b.Erinfo);
        Assert.NotSame(a.Config_editor.DataDriversClasses, b.Config_editor.DataDriversClasses);
        Assert.NotEmpty(a.Config_editor.DataDriversClasses);
        Assert.NotEmpty(b.Config_editor.DataDriversClasses);
        Assert.NotEmpty(a.Config_editor.DataTypesMap);
        Assert.NotEmpty(b.Config_editor.DataTypesMap);
        Assert.NotEmpty(a.Config_editor.QueryList);
        Assert.NotEmpty(b.Config_editor.QueryList);
        Assert.StartsWith(Path.Combine(_folder, "first"), a.Config_editor.ConfigPath);
        Assert.StartsWith(Path.Combine(_folder, "second"), b.Config_editor.ConfigPath);

        a.Config_editor.DataDriversClasses[0].DriverClass = "first-only";
        Assert.NotEqual("first-only", b.Config_editor.DataDriversClasses[0].DriverClass);
        a.Config_editor.DataConnections.Add(new ConnectionProperties { ConnectionName = "private" });
        Assert.DoesNotContain(b.Config_editor.DataConnections, c => c.ConnectionName == "private");
        Assert.Same(a, BeepServiceRegistration.GetBeepService(a.DMEEditor));
        Assert.Same(b, BeepServiceRegistration.GetBeepService(b.DMEEditor));
    }

    [Fact]
    public void TwoProvidersBuiltFromSameCollection_OwnDifferentSingletons()
    {
        var services = Services("same-options", ServiceLifetime.Singleton);
        using var first = services.BuildServiceProvider();
        using var second = services.BuildServiceProvider();
        var a = first.GetRequiredService<IBeepService>();
        var b = second.GetRequiredService<IBeepService>();
        Assert.NotSame(a, b);
        Assert.NotSame(a.Config_editor, b.Config_editor);
        Assert.Same(a, first.GetRequiredService<IBeepService>());
        Assert.Same(b, second.GetRequiredService<IBeepService>());
        first.Dispose();
        Assert.Null(a.DMEEditor);
        Assert.NotNull(b.DMEEditor);
        Assert.NotEmpty(b.Config_editor.DataDriversClasses);
    }

    [Fact]
    public async Task ConcurrentScopes_HavePrivateState_AndDisposeTheirOwnDatasources()
    {
        using var provider = Services("scopes", ServiceLifetime.Scoped).BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var runtimes = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => Task.Run(() =>
        {
            using var scope = provider.CreateScope();
            var runtime = scope.ServiceProvider.GetRequiredService<IBeepService>();
            Assert.Same(runtime, scope.ServiceProvider.GetRequiredService<IBeepService>());
            var source = new Mock<IDataSource>();
            runtime.DMEEditor.DataSources.Add(source.Object);
            runtime.Config_editor.DataConnections.Add(new ConnectionProperties { ConnectionName = $"scope-{index}" });
            Assert.Single(runtime.Config_editor.DataConnections);
            scope.Dispose();
            source.Verify(ds => ds.Dispose(), Times.Once);
            Assert.Null(runtime.DMEEditor);
            return runtime;
        })));
        Assert.Equal(8, runtimes.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IBeepService>());
    }

    [Fact]
    public void ScopedComponentsAndKeyedAliases_ResolveTheSameRuntimeGraph()
    {
        using var provider = Services("aliases", ServiceLifetime.Scoped).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IBeepService>();
        Assert.Same(runtime.DMEEditor, scope.ServiceProvider.GetRequiredService<IDMEEditor>());
        Assert.Same(runtime.DMEEditor, scope.ServiceProvider.GetRequiredKeyedService<IDMEEditor>("Editor"));
        Assert.Same(runtime.Config_editor, scope.ServiceProvider.GetRequiredService<IConfigEditor>());
        Assert.Same(runtime.Config_editor, scope.ServiceProvider.GetRequiredKeyedService<IConfigEditor>("ConfigEditor"));
        Assert.Same(runtime.LLoader, scope.ServiceProvider.GetRequiredKeyedService<IAssemblyHandler>("AssemblyHandler"));
        Assert.Same(runtime.Erinfo, scope.ServiceProvider.GetRequiredService<IErrorsInfo>());
    }

    [Fact]
    public void TransientResolution_ReturnsFreshOwnedGraphs()
    {
        var provider = Services("transient", ServiceLifetime.Transient).BuildServiceProvider();
        var first = provider.GetRequiredService<IBeepService>();
        var second = provider.GetRequiredService<IBeepService>();
        Assert.NotSame(first, second);
        Assert.NotSame(first.DMEEditor, second.DMEEditor);
        provider.Dispose();
        Assert.Null(first.DMEEditor);
        Assert.Null(second.DMEEditor);
    }

    [Fact]
    public void CapturedOptions_CannotBeChangedThroughTheConfigureObject()
    {
        var services = new ServiceCollection();
        BeepServiceOptions? configured = null;
        BeepServiceRegistration.AddBeepRuntime(services, options =>
        {
            Options(options, "original", ServiceLifetime.Scoped);
            configured = options;
        });
        configured!.DirectoryPath = Path.Combine(_folder, "changed");
        configured.AppRepoName = "changed";
        configured.ServiceLifetime = ServiceLifetime.Singleton;
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var runtime = scope.ServiceProvider.GetRequiredService<IBeepService>();
        Assert.Equal("original", runtime.AppRepoName);
        Assert.Equal(Path.Combine(_folder, "original"), runtime.BeepDirectory);
        Assert.Equal(ServiceLifetime.Scoped, services.Single(s => s.ServiceType == typeof(IBeepService)).Lifetime);
    }

    [Fact]
    public void LegacyBuilder_ReturnsCallerOwnedStartupRuntime_NotAContainerGlobal()
    {
        var services = new ServiceCollection();
        using var startup = (BeepService)services.AddBeepServices().WithDirectory(_folder).WithAppRepo("legacy")
            .WithAssemblyLoading(false).WithViewDiscovery(false).Build();
        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IBeepService>();
        Assert.NotSame(startup, runtime);
        Assert.Same(startup, BeepServiceRegistration.GetBeepService(startup.DMEEditor));
        startup.Dispose();
        Assert.NotNull(runtime.DMEEditor);
        Assert.NotEmpty(runtime.Config_editor.DataDriversClasses);
    }

    [Fact]
    public void DuplicateCollectionRegistration_IsRejectedWithoutChangingExistingDescriptors()
    {
        var services = Services("original", ServiceLifetime.Scoped);
        var count = services.Count;
        Assert.Throws<InvalidOperationException>(() => BeepServiceRegistration.AddBeepRuntime(services,
            options => Options(options, "duplicate", ServiceLifetime.Singleton)));
        Assert.Equal(count, services.Count);
    }

    [Fact]
    public void FailedExplicitRoot_DoesNotSilentlyBindToGlobalOrTemporaryConfig()
    {
        Directory.CreateDirectory(_folder);
        var blocker = Path.Combine(_folder, "file-not-directory");
        File.WriteAllText(blocker, "keep");
        var services = new ServiceCollection();
        BeepServiceRegistration.AddBeepRuntime(services, options =>
        {
            Options(options, "blocked", ServiceLifetime.Singleton);
            options.DirectoryPath = blocker;
        });
        using var provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IBeepService>());
        Assert.Equal("keep", File.ReadAllText(blocker));
    }

    [Theory]
    [InlineData(ConnectionState.Closed, true)]
    [InlineData(ConnectionState.Open, false)]
    [InlineData(ConnectionState.Broken, false)]
    public void NameAndGuidClose_ReportClosedAsSuccess(ConnectionState result, bool expected)
    {
        var connection = new Mock<IDataConnection>();
        connection.Setup(c => c.CloseConn()).Returns(result);
        var source = new Mock<IDataSource>();
        source.SetupGet(s => s.Dataconnection).Returns(connection.Object);
        var editor = Editor();
        editor.Setup(e => e.GetDataSource("name")).Returns(source.Object);
        editor.Setup(e => e.GetDataSourceUsingGuidID("guid")).Returns(source.Object);
        Assert.Equal(expected, editor.Object.CloseDataSource("name"));
        Assert.Equal(expected, editor.Object.CloseDataSourceUsingGuidID("guid"));
        Assert.Equal(expected, editor.Object.CloseDataSource("name"));
    }

    [Fact]
    public void Close_MissingSourceAndProviderException_ReturnFalse()
    {
        var editor = Editor();
        editor.Setup(e => e.GetDataSource("missing")).Returns((IDataSource)null!);
        editor.Setup(e => e.GetDataSourceUsingGuidID("missing")).Returns((IDataSource)null!);
        Assert.False(editor.Object.CloseDataSource("missing"));
        Assert.False(editor.Object.CloseDataSourceUsingGuidID("missing"));
        var connection = new Mock<IDataConnection>();
        connection.Setup(c => c.CloseConn()).Throws(new InvalidOperationException("failed"));
        var source = new Mock<IDataSource>();
        source.SetupGet(s => s.Dataconnection).Returns(connection.Object);
        editor.Setup(e => e.GetDataSource("failed")).Returns(source.Object);
        Assert.False(editor.Object.CloseDataSource("failed"));
    }

    private static Mock<DMEEditor> Editor() => new(Mock.Of<IDMLogger>(), Mock.Of<IUtil>(),
        new ErrorsInfo(), Mock.Of<IConfigEditor>(), Mock.Of<IAssemblyHandler>()) { CallBase = true };

    [Fact]
    public void ConcreteEditorGraph_IsReleasedOnce_WhenRuntimeIsDisposedConcurrently()
    {
        var config = new Mock<IConfigEditor>();
        var loader = new Mock<IAssemblyHandler>();
        var editor = new DMEEditor(Mock.Of<IDMLogger>(), Mock.Of<IUtil>(), new ErrorsInfo(), config.Object, loader.Object);
        var source = new Mock<IDataSource>();
        source.SetupGet(s => s.DatasourceName).Returns("owned");
        editor.DataSources.Add(source.Object);
        var service = new BeepService { DMEEditor = editor, Config_editor = config.Object, LLoader = loader.Object };
        Parallel.For(0, 8, _ => service.Dispose());
        source.Verify(s => s.Dispose(), Times.Once);
        config.Verify(c => c.Dispose(), Times.Once);
        loader.Verify(l => l.Dispose(), Times.Once);
        Assert.Null(service.DMEEditor);
    }

    [Fact]
    public void CleanupFailureDoesNotSkipIndependentRuntimeComponents()
    {
        var editor = new Mock<IDMEEditor>();
        editor.Setup(e => e.Dispose()).Throws(new IOException("editor cleanup"));
        editor.SetupGet(e => e.ConfigEditor).Throws(new IOException("unavailable editor config"));
        editor.SetupGet(e => e.assemblyHandler).Throws(new IOException("unavailable editor loader"));
        var config = new Mock<IConfigEditor>();
        config.Setup(c => c.Dispose()).Throws(new IOException("config cleanup"));
        var loader = new Mock<IAssemblyHandler>();
        using var service = new BeepService { DMEEditor = editor.Object, Config_editor = config.Object, LLoader = loader.Object };
        service.Dispose();
        editor.Verify(e => e.Dispose(), Times.Once);
        config.Verify(c => c.Dispose(), Times.Once);
        loader.Verify(l => l.Dispose(), Times.Once);
        Assert.Null(service.DMEEditor);
        Assert.Null(service.LLoader);
        Assert.Throws<ObjectDisposedException>(() => service.Configure(_folder, "after-disposal", BeepConfigType.DataConnector));
        Assert.Throws<ObjectDisposedException>(() => service.LoadAssemblies());
    }

    [Fact]
    public async Task ConcurrentConfigureBuildsOnlyOneGraph_AndCannotReplaceIt()
    {
        using var runtime = new BeepService();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            try { runtime.Configure(_folder, "single", BeepConfigType.DataConnector); return true; }
            catch (InvalidOperationException) { return false; }
        })));
        Assert.Single(results.Where(result => result));
        var editor = runtime.DMEEditor;
        Assert.Throws<InvalidOperationException>(() => runtime.Configure(_folder, "replacement", BeepConfigType.DataConnector));
        Assert.Same(editor, runtime.DMEEditor);
    }

    [Fact]
    public void PreRegisteredComponentsArePreserved_WithoutChangingRuntimeOwnership()
    {
        var services = new ServiceCollection();
        var external = Mock.Of<IConfigEditor>();
        services.AddSingleton(external);
        services.AddKeyedSingleton<IConfigEditor>("ConfigEditor", external);
        BeepServiceRegistration.AddBeepRuntime(services, options => Options(options, "overrides", ServiceLifetime.Scoped));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Same(external, scope.ServiceProvider.GetRequiredService<IConfigEditor>());
        Assert.Same(external, scope.ServiceProvider.GetRequiredKeyedService<IConfigEditor>("ConfigEditor"));
        Assert.NotSame(external, scope.ServiceProvider.GetRequiredService<IBeepService>().Config_editor);
    }

    [Fact]
    public void FailedDirectConfiguration_ReleasesPartialGraphAndRejectsReuse()
    {
        Directory.CreateDirectory(_folder);
        var blocker = Path.Combine(_folder, "blocker");
        File.WriteAllText(blocker, "keep");
        using var service = new BeepService();
        Assert.Throws<InvalidOperationException>(() => service.Configure(blocker, "failed", BeepConfigType.DataConnector));
        Assert.Null(service.DMEEditor);
        Assert.Null(service.lg);
        Assert.Null(service.Config_editor);
        Assert.Throws<ObjectDisposedException>(() => service.Configure(_folder, "retry", BeepConfigType.DataConnector));
        Assert.Equal("keep", File.ReadAllText(blocker));
    }

    [Fact]
    public void AssemblyLoadingException_DoesNotMarkTheAttemptComplete()
    {
        var loader = new Mock<IAssemblyHandler>();
        loader.SetupSequence(l => l.LoadAllAssembly(It.IsAny<IProgress<TheTechIdea.Beep.Addin.PassedArgs>>(), It.IsAny<CancellationToken>()))
            .Throws(new IOException("load failed"))
            .Returns(new ErrorsInfo { Flag = Errors.Ok });
        using var service = new BeepService { Config_editor = Mock.Of<IConfigEditor>(), LLoader = loader.Object };
        Assert.Throws<IOException>(() => service.LoadAssemblies());
        service.LoadAssemblies();
        service.LoadAssemblies();
        loader.Verify(l => l.LoadAllAssembly(It.IsAny<IProgress<TheTechIdea.Beep.Addin.PassedArgs>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }
}
