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

## Notes
- Prefer handler APIs over direct ad-hoc reflection in callers.
- Keep type names fully-qualified when calling `GetType` and `CreateInstanceFromString`.

