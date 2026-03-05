# Shared Context Loading And Resolution Reference

Large examples for loading orchestration and resolver-friendly behavior.

## Scenario A: Full load with progress and post-checks

```csharp
using System;
using System.Threading;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class SharedContextLoadingReference
{
    public static bool ExecuteLoad(IAssemblyHandler handler)
    {
        var progress = new Progress<PassedArgs>(p => Console.WriteLine(p?.Messege));
        var result = handler.LoadAllAssembly(progress, CancellationToken.None);
        if (result.Flag != Errors.Ok)
        {
            Console.WriteLine(result.Message);
            return false;
        }

        Console.WriteLine($"Loaded assemblies: {handler.LoadedAssemblies.Count}");
        Console.WriteLine($"Discovered datasources: {handler.DataSourcesClasses.Count}");
        return true;
    }
}
```

## Scenario B: Folder-specific incremental loading

```csharp
using System;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class SharedContextIncrementalLoading
{
    public static void LoadPluginFolders(IAssemblyHandler handler, string driversPath, string dataSourcesPath)
    {
        handler.LoadAssembly(driversPath, FolderFileTypes.ConnectionDriver);
        handler.LoadAssembly(dataSourcesPath, FolderFileTypes.DataSources);

        var stats = handler.GetLoadStatistics();
        Console.WriteLine($"After incremental load: assemblies={stats.TotalAssembliesLoaded}");
    }
}
```

## Scenario C: Runtime and appdomain assisted resolution check

```csharp
using System;
using System.Reflection;
using TheTechIdea.Beep.Tools;

public static class SharedContextResolverChecks
{
    public static void ValidateResolver(IAssemblyHandler handler)
    {
        var resolved = handler.CurrentDomain_AssemblyResolve(
            AppDomain.CurrentDomain,
            new ResolveEventArgs(typeof(object).Assembly.FullName));

        Console.WriteLine(resolved != null
            ? $"Resolved: {resolved.GetName().Name}"
            : "Resolver returned null");
    }
}
```

