# Shared Context Scanning And Discovery Reference

End-to-end examples for discovery through `IScanningService`, handler scan methods, and shared-context snapshots.

## Scenario A: Full scanning pass and category verification

```csharp
using System;
using TheTechIdea.Beep.Tools;

public static class SharedContextScanningReference
{
    public static void ScanAll(IAssemblyHandler handler)
    {
        handler.ScanForDrivers();
        handler.ScanForDataSources();
        handler.ScanForAddins();
        handler.ProcessExtensions();

        Console.WriteLine($"Drivers: {handler.ConfigEditor?.DriversClasses?.Count ?? 0}");
        Console.WriteLine($"DataSources: {handler.DataSourcesClasses?.Count ?? 0}");
        Console.WriteLine($"Addins: {handler.ConfigEditor?.AddinFolders?.Count ?? 0}");
    }
}
```

## Scenario B: Targeted scan for newly loaded assembly

```csharp
using System;
using System.Reflection;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class SharedContextTargetedScan
{
    public static bool ScanOneAssembly(IAssemblyHandler handler, string assemblyPath)
    {
        var load = handler.LoadAssembly(assemblyPath);
        if (load.Flag != Errors.Ok)
        {
            Console.WriteLine(load.Message);
            return false;
        }

        Assembly asm = Assembly.LoadFrom(assemblyPath);
        handler.ScanAssemblyForDataSources(asm);
        Console.WriteLine("Targeted datasource scan completed.");
        return true;
    }
}
```

## Scenario C: Build metadata for UX catalogs

```csharp
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Tools;

public static class SharedContextMetadataCatalog
{
    public static List<AssemblyClassDefinition> GetDataSourceDefinitions(IAssemblyHandler handler)
    {
        var definitions = handler.LoadedAssemblies
            .SelectMany(a => handler.GetAssemblyClassDefinition(a, "DataSource"))
            .GroupBy(d => d.className)
            .Select(g => g.First())
            .ToList();

        return definitions;
    }
}
```

