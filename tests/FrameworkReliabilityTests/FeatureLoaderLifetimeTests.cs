using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.FeatureManagement;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using Xunit;
namespace FrameworkReliabilityTests;
public sealed class FeatureLoaderLifetimeTests
{
    [Theory]
    [InlineData(AssemblyHandlerType.Default)]
    [InlineData(AssemblyHandlerType.SharedContext)]
    public void Disposing_an_engine_releases_feature_resolution_before_later_optional_assembly_probes(AssemblyHandlerType type)
    {
        var folder=Path.Combine(Path.GetTempPath(),"BeepDM-FeatureLifetime",Guid.NewGuid().ToString("N"));
        using var runtime=new BeepService{AssemblyHandlerType=type};runtime.Configure(folder,"feature-lifetime",BeepConfigType.DataConnector);
        var handler=runtime.LLoader;var feature=new FeatureLoader(handler);handler.LoaderExtensionInstances.Add(feature);
        // This teardown removes the old broken subscription too, so a failing regression cannot poison other tests.
        var callback=(ResolveEventHandler)feature.GetType().GetMethod("CurrentDomain_AssemblyResolve",BindingFlags.Instance|BindingFlags.NonPublic)!.CreateDelegate(typeof(ResolveEventHandler),feature);
        try
        {
            Assert.Null(Type.GetType("Absent.Type, BeepMissingBefore_"+Guid.NewGuid().ToString("N"),false));
            runtime.Dispose();runtime.Dispose();
            Assert.Null(Type.GetType("Absent.Type, BeepMissingAfter_"+Guid.NewGuid().ToString("N"),false));
            Assert.Empty(handler.LoaderExtensionInstances);
        }
        finally{AppDomain.CurrentDomain.AssemblyResolve-=callback;}
    }

    private sealed class CleanupProbe(bool fail) : ILoaderExtention,IDisposable
    {
        public int Releases;
        public IErrorsInfo LoadAllAssembly()=>new ErrorsInfo();
        public IErrorsInfo Scan(TheTechIdea.Beep.Tools.assemblies_rep assembly)=>new ErrorsInfo();
        public IErrorsInfo Scan(Assembly assembly)=>new ErrorsInfo();
        public IErrorsInfo Scan()=>new ErrorsInfo();
        public void Dispose(){Releases++;if(fail)throw new IOException("Fixture extension cleanup failed");}
    }
    [Theory]
    [InlineData(AssemblyHandlerType.Default)]
    [InlineData(AssemblyHandlerType.SharedContext)]
    public void Extension_cleanup_attempts_independent_resources_once_and_surfaces_failures(AssemblyHandlerType type)
    {
        var folder=Path.Combine(Path.GetTempPath(),"BeepDM-FeatureLifetime",Guid.NewGuid().ToString("N"));
        using var runtime=new BeepService{AssemblyHandlerType=type};runtime.Configure(folder,"cleanup-lifetime",BeepConfigType.DataConnector);
        var handler=runtime.LLoader;var failing=new CleanupProbe(true);var released=new CleanupProbe(false);
        handler.LoaderExtensionInstances.Add(failing);handler.LoaderExtensionInstances.Add(released);handler.LoaderExtensionInstances.Add(released);
        var failure=Assert.Throws<AggregateException>(()=>handler.Dispose());Assert.IsType<IOException>(Assert.Single(failure.InnerExceptions));
        Assert.Equal(1,failing.Releases);Assert.Equal(1,released.Releases);Assert.Empty(handler.LoaderExtensionInstances);handler.Dispose();Assert.Equal(1,released.Releases);
    }
}
