---
name: shared-context-scanning-discovery
description: Detailed guidance for SharedContextAssemblyHandler scanning and discovery using IScanningService and DriverDiscoveryAssistant. Use when modifying discovery of IDataSource, addins, workflow actions, loader extensions, and driver metadata.
---

# Shared Context Scanning And Discovery

Use this skill when touching discovery behavior in `SharedContextAssemblyHandler` and `PluginSystem/IScanningService.cs`.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Discovery Components
- `IScanningService` / `ScanningService`
- `DriverDiscoveryAssistant`
- `SharedContextManager` discovered collections:
  - `DiscoveredDrivers`
  - `DiscoveredDataSources`
  - `DiscoveredAddins`
  - `DiscoveredWorkflowActions`
  - `DiscoveredViewModels`
  - `DiscoveredLoaderExtensions`

## Scan Entry Points
- `ScanForDrivers()`
- `ScanForDataSources()`
- `ScanForAddins()`
- `ProcessExtensions()`
- `IScanningService.ScanAssembly(...)`
- `IScanningService.ScanAssemblyForDataSources(...)`

## Interface Categories Covered
- `IDataSource`
- `ILoaderExtention`
- `IDM_Addin`
- `IWorkFlowAction`
- `IWorkFlowStep`
- `IWorkFlowEditor`
- `IWorkFlowRule`
- `IBeepViewModel`
- `IFunctionExtension`
- `IPrintManager`

## Safe Change Rules
1. Keep discovery additive and resilient (continue on per-type failure).
2. Preserve updates to both config lists and shared-context discovered lists.
3. Keep component type tags (`componentType`) consistent in class definitions.
4. Avoid expensive full scans where targeted scans (`ScanAssemblyForDataSources`) are intended.

## Common Pitfalls
- Updating only `ConfigEditor` lists without updating `SharedContextManager` snapshots.
- Breaking command/addin metadata extraction in `GetAssemblyClassDefinition`.
- Changing scan order and unintentionally hiding extension-driven classes.

## Verification Checklist
- Data source scan populates both `ConfigEditor.DataSourcesClasses` and `DiscoveredDataSources`.
- Addin/workflow/viewmodel lists are non-empty when appropriate assemblies exist.
- Scanning statistics from `IScanningService.GetScanningStatistics()` align with discovered counts.

