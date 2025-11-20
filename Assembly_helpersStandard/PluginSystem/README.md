# Plugin System — Nugget Download & Loading

This folder contains code for fully-featured plugin system helpers, including a NuGet package downloader and integration with the local plugin loader.

## New helpers included

- `NuggetPackageDownloader` — lightweight download/extract/resolve/parse helper that will:
  - Use the dotnet CLI to fetch a NuGet package into a local package cache
  - Extract `.nupkg` files
  - Inspect `.nuspec` and resolve dependencies
  - Find framework-specific DLL folders (lib, runtimes, ref)
  - Download dependencies recursively while skipping system packages
  - Log progress via `IDMLogger`

- `NuggetPluginLoader` — orchestrates downloading and loads the resulted assembly folders into the shared context. It uses the `AssemblyLoadingAssistant` to load assemblies and integrates with existing nugget and shared-context systems.

- `AssemblyLoadingAssistant.LoadNuggetFromNugetAsync` — convenience helper to download + load a nugget and its dependencies using the `NuggetPackageDownloader` and `NuggetPluginLoader`.

- `AssemblyHandler.LoadNuggetFromNuGetAsync` — convenience method on `AssemblyHandler` so consumers can quickly request a package by name, download dependencies, and load the assemblies into the engine.

## How to use

You can use these methods through the programmatic API (e.g. in unit tests / script / admin tools) or wire them to a command in your CLI/UIs.

Example usage via `AssemblyHandler` (typical in code that has access to `IDMEEditor` / `DMEEditor.assemblyHandler`):

```csharp
// Async usage (supports multiple feed sources, shared context toggle, and optional install-to-app-path)
var sources = new[] { "https://api.nuget.org/v3/index.json", "https://myinternalfeed.example/v3/index.json" };
var assemblies = await assemblyHandler.LoadNuggetFromNuGetAsync("Oracle.ManagedDataAccess.Core", "23.4.0", sources, useSingleSharedContext: true, appInstallPath: "Plugins");

// assemblies now contains the loaded System.Reflection.Assembly objects

// If you prefer to use AssemblyLoadingAssistant directly
var assistant = (AssemblyLoadingAssistant)assemblyHandler; // or resolve from DI
var loadedAssemblies = await assistant.LoadNuggetFromNugetAsync("Oracle.ManagedDataAccess.Core", "23.4.0", sources, useSingleSharedContext: true, appInstallPath: "Plugins");

// You may then scan loaded assemblies for types
foreach (var asm in loadedAssemblies)
{
    assemblyHandler.ScanAssembly(asm);
}
```

Note: `LoadNuggetFromNuGetAsync` downloads packages to `{ConfigEditor.ExePath}/NugetDownloads/` by default, and loads the best matching framework DLLs into the process via existing loading helpers.

New features:
- If you specify `appInstallPath`, packages will be copied into `Plugins/<pluginId>/<version>` format by default.
- `PluginRegistry` (`SharedContextAssemblyHandler.PluginRegistry`) keeps an index of installed plugins and their state at `{ConfigEditor.ExePath}/plugins_registry.json`.
- Use `SharedContextAssemblyHandler.PluginInstaller.Uninstall(pluginId)` to uninstall a plugin, which removes files and unregisters it; it refuses to remove loaded plugins unless `force=true`.
- You can request process-hosted isolation by using `useProcessHost:true` while calling `LoadNuggetFromNugetAsync` — it starts an executable from the plugin folder when present.

## Implementation notes

- We did not add external dependencies (no Spectre.Console or JSON libraries). The downloader uses dotnet CLI and simple file system operations.
- Downloading uses a temporary `temp.csproj` with `dotnet add package` to rely on the dotnet package manager for robustness.
- The logic checks `.nuspec` files and finds dependencies in `group` sections; older packages are also supported.

## CLI/Command Integration

If you want to expose this as a command to the CLI or the editor, create a command similar to `driver install` which accepts package name, version and source, then call:

```csharp
// example allowing the specification of sources, whether loaded nugget packages should share a single
// shared load context (so they can resolve each other's types), and whether the package should be
// copied to the app's plugin folder for runtime resolution.
var sources = new[] { "https://api.nuget.org/v3/index.json" };
var assemblies = await assemblyHandler.LoadNuggetFromNuGetAsync(packageName, version, sources, useSingleSharedContext: true, appInstallPath: "Plugins");
```

Add scanning and registration of loaded types using existing `ScanAssembly` and loader extensibility points.

---

## Testing

- Build the workspace to validate compile-time correctness.
- Run manual command sequence:
  1. `dotnet run --project BeepShell` and call your CLI command to download and load the package.
  2. Use `assemblyHandler.LoadNuggetFromNuGetAsync("Oracle.ManagedDataAccess.Core")` (you may also specify sources, shared-context toggle and appInstallPath) from a small test harness.

If you want, I can add a CLI command and/or unit tests that exercise this new code.

Integration tests have been added to validate key plugin behaviors. To run them locally:

```powershell
dotnet test Assembly_helpersStandard\tests\IntegrationTests\Assembly_helpers.IntegrationTests.csproj
```

The integration tests validate:
- Multi-feed package fetch and install to temporary feeds (using `dotnet pack` into local feeds)
- Cross-plugin type sharing using the `useSingleSharedContext` model
- Per-nugget unload & memory reclamation using collectible AssemblyLoadContexts
- Uninstall behavior and plugin registry cleanup via `PluginInstaller` and `PluginRegistry`
- Conflict resolution & version management (plugins installed under `Plugins/<pluginId>/<version>` with `overwrite=false` behavior)

Examples:

// Uninstall a plugin (unregister & delete folder) via the shared context assembly handler's PluginInstaller
var handler = (SharedContextAssemblyHandler)assemblyHandler;
var result = handler.PluginInstaller.Uninstall("Oracle.ManagedDataAccess.Core", null, force: false);

// Start process-hosted plugin during installation
var assemblies = await assemblyHandler.LoadNuggetFromNuGetAsync("My.NativePlugin", "1.0.0", sources, useSingleSharedContext: false, appInstallPath: "Plugins", useProcessHost: true);
