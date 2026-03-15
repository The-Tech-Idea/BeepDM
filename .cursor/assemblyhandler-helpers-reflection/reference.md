# AssemblyHandler Helpers And Reflection Reference

Larger examples for reflection-based activation, metadata extraction, and driver scanning.

## Scenario A: Resolve type, create instance, and invoke method

```csharp
using System;
using TheTechIdea.Beep.Tools;

public static class AssemblyHelperReflectionExamples
{
    public static bool CreateAndRun(IAssemblyHandler handler, string fullTypeName, string methodName)
    {
        var instance = handler.CreateInstanceFromString(fullTypeName);
        if (instance == null)
        {
            return false;
        }

        var ok = handler.RunMethod(instance, fullTypeName, methodName);
        return ok;
    }
}
```

## Scenario B: Extract class metadata from loaded assemblies

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Tools;

public static class AssemblyMetadataExamples
{
    public static List<AssemblyClassDefinition> CollectDataSources(IAssemblyHandler handler)
    {
        var output = new List<AssemblyClassDefinition>();

        foreach (var asm in handler.LoadedAssemblies)
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch
            {
                continue;
            }

            foreach (var t in types)
            {
                var ti = t.GetTypeInfo();
                if (ti.ImplementedInterfaces.Any(i => i.Name == "IDataSource"))
                {
                    var def = handler.GetAssemblyClassDefinition(ti, "IDataSource");
                    output.Add(def);
                }
            }
        }

        return output;
    }
}
```

## Exact AssemblyClassDefinition build path (current code pattern)

`GetAssemblyClassDefinition(TypeInfo type, string typename)` is the canonical builder used by scanning.

```csharp
public AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type, string typename)
{
    var xcls = new AssemblyClassDefinition();
    xcls.className = type.Name;
    xcls.dllname = type.Module.Name;
    xcls.PackageName = type.FullName;
    xcls.componentType = typename;
    xcls.type = type;

    xcls.classProperties = (AddinAttribute)type.GetCustomAttribute(typeof(AddinAttribute), false);
    if (xcls.classProperties != null)
    {
        xcls.Order = xcls.classProperties.order;
        xcls.RootName = xcls.classProperties.misc;

        if (type.ImplementedInterfaces.Contains(typeof(IDataSource)))
        {
            xcls.IsDataSource = true;
            xcls.DatasourceType = xcls.classProperties.DatasourceType;
            xcls.Category = xcls.classProperties.Category;
        }

        xcls.VisSchema = (AddinVisSchema)type.GetCustomAttribute(typeof(AddinVisSchema), false);
        // If IAddinVisSchema: push AddinTreeStructure entry into ConfigEditor.AddinTreeStructure
        // Extract [CommandAttribute] methods into xcls.Methods
    }

    // If type implements IOrder: xcls.Order = instance.Order
    return xcls;
}
```

### Where those definitions are stored
- IDataSource types:
  - `DataSourcesClasses.Add(xcls)`
  - `ConfigEditor.DataSourcesClasses.Add(xcls)`
- ILoaderExtention types:
  - `LoaderExtensions.Add(typeInfo)` (raw type list)
  - `LoaderExtensionClasses.Add(GetAssemblyClassDefinition(typeInfo, "ILoaderExtention"))`
- IDM_Addin types:
  - `ConfigEditor.Addins.Add(GetAssemblyClassDefinition(typeInfo, "IDM_Addin"))`

## Scenario C: ADO.NET driver scan + defaults

```csharp
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Tools;

public static class AssemblyDriverDiscoveryExamples
{
    public static List<ConnectionDriversConfig> ScanDrivers(IAssemblyHandler handler)
    {
        var all = new List<ConnectionDriversConfig>();

        foreach (var asm in handler.LoadedAssemblies)
        {
            all.AddRange(handler.GetDrivers(asm));
        }

        handler.AddEngineDefaultDrivers();
        handler.CheckDriverAlreadyExistinList();
        return all;
    }
}
```

## Scenario D: Build tree objects from scanned addin schema

```csharp
using System.Collections.Generic;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

public static class AddinTreeExamples
{
    public static List<ParentChildObject> BuildHierarchy(IAssemblyHandler handler)
    {
        // Uses AddinTreeStructure built during metadata extraction.
        return handler.GetAddinObjectsFromTree();
    }
}
```

## Helper Notes
- Prefer handler APIs over direct ad-hoc reflection in callers.
- Keep type names fully-qualified when calling `GetType` and `CreateInstanceFromString`.
- `GetAssemblyClassDefinition` is the canonical metadata parser (attributes, commands, vis schema, ordering).
- `GetDrivers` supports ADO.NET pattern discovery and special-case fallback handling.
- `SyncNuggetAssembliesToHandlerCollections` should remain the only helper that bulk-syncs nugget-loaded assemblies into handler collections and scan state.

