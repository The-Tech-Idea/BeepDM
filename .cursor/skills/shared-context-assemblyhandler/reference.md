# SharedContextAssemblyHandler Reference

End-to-end usage patterns for `SharedContextAssemblyHandler` through `IAssemblyHandler`.

## Scenario A: Startup initialization and full discovery

```csharp
using System;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class SharedContextHandlerExamples
{
    public static IAssemblyHandler Initialize(
        IConfigEditor config,
        IErrorsInfo errors,
        IDMLogger logger,
        IUtil util)
    {
        IAssemblyHandler handler = new SharedContextAssemblyHandler(config, errors, logger, util);

        var progress = new Progress<PassedArgs>(p =>
        {
            logger?.LogWithContext($"Load progress: {p?.Messege}", null);
        });

        var result = handler.LoadAllAssembly(progress, CancellationToken.None);
        logger?.LogWithContext($"Load result: {result.Flag} - {result.Message}", null);

        var stats = handler.GetLoadStatistics();
        logger?.LogWithContext(
            $"Assemblies={stats.TotalAssembliesLoaded}, Drivers={stats.DriversFound}, DataSources={stats.DataSourcesFound}",
            null);

        return handler;
    }
}
```

## Scenario B: Plugin/NuGet flow through one handler

```csharp
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Tools;

public static class SharedContextUnifiedFlow
{
    public static async Task<bool> InstallAndDiscoverAsync(IAssemblyHandler handler, string term)
    {
        var matches = await handler.SearchNuGetPackagesAsync(term, take: 10);
        var selected = matches.FirstOrDefault();
        if (selected == null)
        {
            return false;
        }

        var versions = await handler.GetNuGetPackageVersionsAsync(selected.PackageId);
        var version = versions.FirstOrDefault() ?? selected.Version;

        var loaded = await handler.LoadNuggetFromNuGetAsync(selected.PackageId, version);
        return loaded.Count > 0;
    }
}
```

