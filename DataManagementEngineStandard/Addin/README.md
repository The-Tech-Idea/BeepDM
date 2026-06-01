# Addin

Extension point for addin DLLs implementing the `IDM_Addin` interface.

## Purpose
During application startup, AssemblyHandler scans this directory for DLLs containing `IDM_Addin` implementations. Discovered addins are registered in `ConfigEditor` and made available through the addin tree.

## Key Files
- This directory holds addin DLLs at runtime (populated during assembly loading)
- Addins are discovered by `AssemblyHandler.ScanAssembly()` when `[AddinAttribute]` is present

## Registration
```csharp
[AddinAttribute(Caption = "Copy Entity Manager", Name = "CopyEntityManager",
    misc = "ImportDataManager", addinType = AddinType.Class)]
public class CopyEntityManager : IDM_Addin
{
    public void Run(IPassedArgs args) { /* implementation */ }
    public void SetConfig(IDMEEditor editor, IDMLogger logger, 
        IUtil util, string[] args, IPassedArgs e, IErrorsInfo per) { }
}
```

## Related Documentation
- [AssemblyHandler Help](../Help/assemblyhandler-loading-nuget-extensions.html)
- [Creating Custom DataSources](../Help/creating-custom-datasources.html)
