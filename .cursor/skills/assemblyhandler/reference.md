# AssemblyHandler Reference

This reference provides end-to-end usage patterns for `IAssemblyHandler` and `AssemblyHandler`.

## Scenario A: Full Startup Discovery + Basic Diagnostics

```csharp
using System;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class AssemblyHandlerReferenceExamples
{
    public static IAssemblyHandler InitializeAndScan(
        IConfigEditor configEditor,
        IErrorsInfo errors,
        IDMLogger logger,
        IUtil util)
    {
        IAssemblyHandler handler = new AssemblyHandler(configEditor, errors, logger, util);

        var progress = new Progress<PassedArgs>(p =>
        {
            logger?.WriteLog($"AssemblyHandler progress: {p?.Messege}");
        });

        var token = CancellationToken.None;
        var result = handler.LoadAllAssembly(progress, token);
        if (result.Flag != Errors.Ok)
        {
            logger?.WriteLog($"LoadAllAssembly warning/failure: {result.Message}");
        }

        var stats = handler.GetLoadStatistics();
        logger?.WriteLog(
            $"Loaded={stats.TotalAssembliesLoaded}, Drivers={stats.DriversFound}, DataSources={stats.DataSourcesFound}, Time={stats.TotalLoadTime.TotalSeconds:F1}s");

        return handler;
    }
}
```

## Scenario B: NuGet-driven Plugin Lifecycle

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Tools;

public static class AssemblyHandlerNugetFlow
{
    public static async Task SearchInstallTrackAsync(IAssemblyHandler handler)
    {
        var search = await handler.SearchNuGetPackagesAsync("sqlite", take: 10);
        var package = search.FirstOrDefault();
        if (package == null)
        {
            return;
        }

        var versions = await handler.GetNuGetPackageVersionsAsync(package.PackageId);
        var selectedVersion = versions.FirstOrDefault() ?? package.Version;

        var loaded = await handler.LoadNuggetFromNuGetAsync(
            package.PackageId,
            selectedVersion,
            useSingleSharedContext: true);

        if (loaded.Count > 0)
        {
            // Example: manually track provenance for a known driver class after install
            handler.TrackDriverPackage(
                package.PackageId,
                selectedVersion,
                "MyNamespace.SqliteDataSource",
                (DataSourceType)0);
        }
    }
}
```

## Scenario C: Source management + targeted folder load

```csharp
using System;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class AssemblyHandlerOps
{
    public static void ConfigureSourcesAndLoadFolder(IAssemblyHandler handler, string folderPath)
    {
        handler.AddNuGetSource("internal-feed", "https://nuget.mycompany.local/v3/index.json", isEnabled: true);
        var active = handler.GetActiveSourceUrls();

        // Load extracted package binaries from a known folder
        var loaded = handler.LoadAssembliesFromFolder(folderPath, FolderFileTypes.OtherDLL, scanForDataSources: true);
        Console.WriteLine($"Loaded from folder: {loaded.Count} assemblies; active sources: {active.Count}");
    }
}
```

