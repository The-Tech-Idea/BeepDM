# AssemblyHandler Loading And Scanning Reference

## Load/Scan Pipeline Notes
- `LoadAllAssembly` is the canonical orchestrator and should remain the default startup path.
- `LoadFolderAssemblies(...)` supports config-driven folders plus fallback paths and subfolder traversal.
- `ScanAssembly(...)` is broad registration; `ScanAssemblyForDataSources(...)` is targeted.
- Large assemblies use parallel type staging and then deterministic registration in `ProcessTypeInfo(...)`.

## Scenario A: Startup scan with progress + statistics

```csharp
using System;
using System.Threading;
using TheTechIdea.Beep.Tools;

public static class LoadingScanExamples
{
    public static void Startup(IAssemblyHandler handler)
    {
        var progress = new Progress<PassedArgs>(p => Console.WriteLine(p?.Messege));
        handler.LoadAllAssembly(progress, CancellationToken.None);

        var s = handler.GetLoadStatistics();
        Console.WriteLine($"Assemblies={s.TotalAssembliesLoaded}, Drivers={s.DriversFound}, DataSources={s.DataSourcesFound}");
    }
}
```

## Scenario B: Folder increment load (NuGet extraction / plugin drop)

```csharp
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class FolderLoadExamples
{
    public static int LoadPlugins(IAssemblyHandler handler, string folder)
    {
        var loaded = handler.LoadAssembliesFromFolder(folder, FolderFileTypes.OtherDLL, scanForDataSources: true);
        return loaded.Count;
    }
}
```

## Scenario C: Drivers + data sources from explicit category folders

```csharp
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class CategoryLoadExamples
{
    public static void LoadTypedFolders(IAssemblyHandler handler, string driversPath, string sourcesPath)
    {
        handler.LoadAssembly(driversPath, FolderFileTypes.ConnectionDriver);
        handler.LoadAssembly(sourcesPath, FolderFileTypes.DataSources);
    }
}
```

## Registration Targets by Interface
- `IDataSource` -> `DataSourcesClasses` + `ConfigEditor.DataSourcesClasses`
- `IDM_Addin` -> `ConfigEditor.Addins`
- `IWorkFlowAction`/`IWorkFlowStep`/`IWorkFlowEditor`/`IWorkFlowRule` -> respective workflow lists
- `IFunctionExtension` -> `ConfigEditor.GlobalFunctions`
- `IPrintManager` -> `ConfigEditor.PrintManagers`
- `ILoaderExtention` -> `LoaderExtensions` + `LoaderExtensionClasses`

## Exact Flow: How DataSources and Extensions become AssemblyClassDefinition

This is the concrete registration flow used by `ScanAssembly(...)` + `ProcessTypeInfo(...)`:

```csharp
// 1) ScanAssembly collects TypeInfo and calls ProcessTypeInfo(typeInfo, asm)
private void ProcessTypeInfo(TypeInfo typeInfo, Assembly asm)
{
    // Loader extension registration
    if (typeInfo.ImplementedInterfaces.Contains(typeof(ILoaderExtention)))
    {
        LoaderExtensions.Add(typeInfo);
        LoaderExtensionClasses.Add(GetAssemblyClassDefinition(typeInfo, "ILoaderExtention"));
    }

    // Data source registration
    if (typeInfo.ImplementedInterfaces.Contains(typeof(IDataSource)))
    {
        var xcls = GetAssemblyClassDefinition(typeInfo, "IDataSource");
        DataSourcesClasses.Add(xcls);
        ConfigEditor.DataSourcesClasses.Add(xcls);
    }
}
```

Important:
- Both `LoaderExtensionClasses` and `DataSourcesClasses` store `AssemblyClassDefinition`.
- `DataSourcesClasses` is mirrored into `ConfigEditor.DataSourcesClasses` for app-wide discovery.
- `LoaderExtensions` stores raw `Type` objects, while `LoaderExtensionClasses` stores metadata objects.

## What `GetAssemblyClassDefinition(...)` fills

When creating each `AssemblyClassDefinition`, the helper populates:
- identity: `className`, `dllname`, `PackageName`, `type`, `componentType`
- addin metadata: `classProperties` (`AddinAttribute`)
- datasource flags: `IsDataSource`, `DatasourceType`, `Category` (when `IDataSource`)
- visual metadata: `VisSchema` (`AddinVisSchema`) and `ConfigEditor.AddinTreeStructure` entries (when applicable)
- command metadata: `Methods` list from `[CommandAttribute]` methods
- optional order from `IOrder`

## Verification Checklist
- No duplicate assembly tracking across `LoadedAssemblies`, `Assemblies`, and `_loadedAssemblyCache`.
- `LoadAllAssembly` leaves tree/addin hierarchy consistent for non-DataConnector mode.
- Progress messages continue through the full pipeline and error conditions remain log-driven, non-fatal where expected.

