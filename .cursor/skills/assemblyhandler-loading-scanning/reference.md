# AssemblyHandler Loading And Scanning Reference

Larger end-to-end examples for load orchestration and scan behavior.

## Scenario A: Startup scan with progress, cancellation, and verification

```csharp
using System;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class AssemblyLoadingExamples
{
    public static void RunStartupScan(IAssemblyHandler handler, IDMLogger logger)
    {
        using var cts = new CancellationTokenSource();

        var progress = new Progress<PassedArgs>(p =>
        {
            logger?.WriteLog($"[LoadAllAssembly] {p?.Messege}");
        });

        var result = handler.LoadAllAssembly(progress, cts.Token);
        if (result.Flag != Errors.Ok)
        {
            logger?.WriteLog($"LoadAllAssembly result: {result.Message}");
        }

        logger?.WriteLog($"Assemblies tracked: {handler.LoadedAssemblies.Count}");
        logger?.WriteLog($"Data source classes: {handler.DataSourcesClasses.Count}");
        logger?.WriteLog($"Loader extensions: {handler.LoaderExtensionClasses.Count}");
    }
}
```

## Scenario B: Incremental folder load for downloaded nuggets

```csharp
using System;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class AssemblyIncrementalLoadExamples
{
    public static void LoadPluginFolder(IAssemblyHandler handler, string pluginsFolder)
    {
        var loaded = handler.LoadAssembliesFromFolder(
            pluginsFolder,
            FolderFileTypes.OtherDLL,
            scanForDataSources: true);

        Console.WriteLine($"Incremental load complete, assemblies loaded: {loaded.Count}");
    }
}
```

## Scenario C: Full folder category load

```csharp
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class AssemblyCategoryLoadExamples
{
    public static void LoadDriversAndSources(IAssemblyHandler handler, string driversPath, string sourcesPath)
    {
        handler.LoadAssembly(driversPath, FolderFileTypes.ConnectionDriver);
        handler.LoadAssembly(sourcesPath, FolderFileTypes.DataSources);
    }
}
```

## Verification Tips
- `LoadAllAssembly` should populate `DataSourcesClasses` and `LoadedAssemblies`.
- `GetLoadStatistics()` should reflect load counts after startup.
- `ErrorObject.Message` can carry last error details even when operation continues.

