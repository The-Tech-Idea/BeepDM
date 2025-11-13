using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Spectre.Console;

namespace BeepShell.Shared.Utilities
{
    /// <summary>
    /// Helper class to download NuGet packages using dotnet CLI
    /// </summary>
    public class NuGetPackageDownloader
    {
        private readonly string _downloadDirectory;

        public NuGetPackageDownloader(string downloadDirectory)
        {
            _downloadDirectory = downloadDirectory;
            
            // Ensure download directory exists
            if (!Directory.Exists(_downloadDirectory))
            {
                Directory.CreateDirectory(_downloadDirectory);
            }
        }

        /// <summary>
        /// Downloads a NuGet package from NuGet.org or specified source
        /// </summary>
        /// <param name="packageName">Name of the NuGet package</param>
        /// <param name="version">Specific version (optional, downloads latest if not specified)</param>
        /// <param name="source">NuGet source URL (defaults to nuget.org)</param>
        /// <returns>Path to the downloaded .nupkg file, or null if failed</returns>
        public async Task<string?> DownloadPackageAsync(string packageName, string? version = null, string? source = null)
        {
            try
            {
                var nugetSource = source ?? "https://api.nuget.org/v3/index.json";
                
                AnsiConsole.MarkupLine($"[dim]Downloading {packageName} from {nugetSource}...[/]");

                // Use dotnet CLI to add package to a temporary project, which downloads it
                var tempDir = Path.Combine(_downloadDirectory, $"temp_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Create a temporary .csproj file
                    var csprojPath = Path.Combine(tempDir, "temp.csproj");
                    var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
                    File.WriteAllText(csprojPath, csprojContent);

                    // Build the dotnet add package command
                    var versionArg = !string.IsNullOrWhiteSpace(version) ? $" -v {version}" : "";
                    var sourceArg = $" -s \"{nugetSource}\"";
                    var command = $"add \"{csprojPath}\" package {packageName}{versionArg}{sourceArg} --package-directory \"{_downloadDirectory}\"";

                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = tempDir
                    };

                    using var process = new Process { StartInfo = processStartInfo };
                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to download package: {error}");
                        return null;
                    }

                    AnsiConsole.MarkupLine($"[green]✓[/] Package downloaded successfully");

                    // Find the downloaded .nupkg file
                    var nupkgFiles = Directory.GetFiles(_downloadDirectory, $"{packageName.ToLower()}*.nupkg", SearchOption.AllDirectories);
                    
                    if (nupkgFiles.Any())
                    {
                        var nupkgPath = nupkgFiles.OrderByDescending(f => new FileInfo(f).LastWriteTime).First();
                        AnsiConsole.MarkupLine($"[dim]Package location: {nupkgPath}[/]");
                        return nupkgPath;
                    }

                    // If .nupkg not found, look for DLL files in the package directory
                    var packageFolder = Path.Combine(_downloadDirectory, packageName.ToLower());
                    if (Directory.Exists(packageFolder))
                    {
                        var dllFiles = Directory.GetFiles(packageFolder, "*.dll", SearchOption.AllDirectories);
                        if (dllFiles.Any())
                        {
                            // Return the directory containing the DLLs
                            return packageFolder;
                        }
                    }

                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Package downloaded but files not found in expected location");
                    return _downloadDirectory;
                }
                finally
                {
                    // Cleanup temp directory
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error downloading package: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts a .nupkg file to get the DLL files
        /// </summary>
        public string? ExtractNuGetPackage(string nupkgPath)
        {
            try
            {
                if (!File.Exists(nupkgPath))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Package file not found: {nupkgPath}");
                    return null;
                }

                var extractDir = Path.Combine(_downloadDirectory, Path.GetFileNameWithoutExtension(nupkgPath));
                
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                }
                
                Directory.CreateDirectory(extractDir);

                // .nupkg files are just ZIP files
                System.IO.Compression.ZipFile.ExtractToDirectory(nupkgPath, extractDir);

                AnsiConsole.MarkupLine($"[green]✓[/] Package extracted to: {extractDir}");

                // Use the comprehensive framework finder
                var compatiblePath = FindCompatibleFrameworkFolder(extractDir);
                
                if (!string.IsNullOrEmpty(compatiblePath))
                {
                    return compatiblePath;
                }

                // If no compatible framework found, return the extract directory as fallback
                AnsiConsole.MarkupLine($"[yellow]⚠[/] No compatible framework folder found, using extraction root");
                return extractDir;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error extracting package: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Searches for available versions of a package
        /// </summary>
        public async Task<string[]> SearchPackageVersionsAsync(string packageName, string? source = null)
        {
            try
            {
                var nugetSource = source ?? "https://api.nuget.org/v3/index.json";
                
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"package search {packageName} --exact-match -s \"{nugetSource}\" --format json",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Parse the versions from output (simplified)
                // In production, you'd parse the JSON properly
                return new[] { "latest" };
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Downloads a package and all its dependencies recursively
        /// </summary>
        /// <param name="packageName">Name of the NuGet package</param>
        /// <param name="version">Specific version (optional)</param>
        /// <param name="source">NuGet source URL</param>
        /// <param name="downloadedPackages">Set to track already downloaded packages (prevents circular dependencies)</param>
        /// <returns>Dictionary of package names to their DLL directories</returns>
        public async Task<Dictionary<string, string>> DownloadPackageWithDependenciesAsync(
            string packageName, 
            string? version = null, 
            string? source = null,
            HashSet<string>? downloadedPackages = null)
        {
            var result = new Dictionary<string, string>();
            downloadedPackages ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Prevent re-downloading the same package
            var packageKey = $"{packageName}|{version ?? "latest"}";
            if (downloadedPackages.Contains(packageKey))
            {
                return result;
            }

            downloadedPackages.Add(packageKey);

            try
            {
                AnsiConsole.MarkupLine($"[cyan]→[/] Downloading {packageName}{(version != null ? $" v{version}" : "")}...");

                // Download the main package
                var nupkgPath = await DownloadPackageAsync(packageName, version, source);
                
                if (string.IsNullOrEmpty(nupkgPath))
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Failed to download {packageName}");
                    return result;
                }

                // Determine the package root directory (where .nuspec is located)
                string packageRootDir;
                if (nupkgPath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
                {
                    packageRootDir = Path.Combine(_downloadDirectory, Path.GetFileNameWithoutExtension(nupkgPath));
                    
                    // Extract if not already extracted
                    if (!Directory.Exists(packageRootDir))
                    {
                        Directory.CreateDirectory(packageRootDir);
                        System.IO.Compression.ZipFile.ExtractToDirectory(nupkgPath, packageRootDir);
                        AnsiConsole.MarkupLine($"[green]✓[/] Package extracted to: {packageRootDir}");
                    }
                }
                else if (Directory.Exists(nupkgPath))
                {
                    packageRootDir = nupkgPath;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Invalid package path: {nupkgPath}");
                    return result;
                }

                // Find compatible framework folder WITHIN the package root
                var compatibleFrameworkPath = FindCompatibleFrameworkFolder(packageRootDir);

                if (!string.IsNullOrEmpty(compatibleFrameworkPath))
                {
                    result[packageName] = compatibleFrameworkPath;

                    // Parse .nuspec to find dependencies (now using the correct package root)
                    
                    var dependencies = GetPackageDependencies(packageRootDir);
                    
                    if (dependencies.Any())
                    {
                        AnsiConsole.MarkupLine($"[dim]  → Found {dependencies.Count} dependencies for {packageName}[/]");
                        
                        // Log each dependency being downloaded
                        foreach (var (depName, depVersion) in dependencies)
                        {
                            AnsiConsole.MarkupLine($"[dim]    - {depName}{(depVersion != null ? $" ({depVersion})" : "")}[/]");
                        }
                        
                        // Download each dependency recursively
                        foreach (var (depName, depVersion) in dependencies)
                        {
                            var depResults = await DownloadPackageWithDependenciesAsync(
                                depName, 
                                depVersion, 
                                source, 
                                downloadedPackages);
                            
                            // Merge results
                            foreach (var kvp in depResults)
                            {
                                if (!result.ContainsKey(kvp.Key))
                                {
                                    result[kvp.Key] = kvp.Value;
                                }
                            }
                        }
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] No compatible framework folder found for {packageName}");
                    AnsiConsole.MarkupLine($"[dim]  Package location: {packageRootDir}[/]");
                }

                return result;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error downloading {packageName} with dependencies: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Parses the .nuspec file to extract package dependencies
        /// </summary>
        private List<(string Name, string? Version)> GetPackageDependencies(string extractedPackagePath)
        {
            var dependencies = new List<(string, string?)>();

            try
            {
                // Find .nuspec file
                var nuspecFiles = Directory.GetFiles(extractedPackagePath, "*.nuspec", SearchOption.AllDirectories);
                
                if (!nuspecFiles.Any())
                {
                    return dependencies;
                }

                var nuspecPath = nuspecFiles.First();
                AnsiConsole.MarkupLine($"[dim]  → Parsing dependencies from: {Path.GetFileName(nuspecPath)}[/]");
                
                var nuspecXml = XDocument.Load(nuspecPath);
                
                // Parse dependencies from .nuspec
                var ns = nuspecXml.Root?.GetDefaultNamespace();
                if (ns == null) return dependencies;

                var dependencyGroups = nuspecXml.Descendants(ns + "dependencies").Elements(ns + "group");
                
                foreach (var group in dependencyGroups)
                {
                    // Get target framework
                    var targetFramework = group.Attribute("targetFramework")?.Value;
                    
                    // Filter for compatible frameworks (net6.0, net7.0, net8.0, netstandard2.0, netstandard2.1)
                    if (!string.IsNullOrEmpty(targetFramework) && 
                        !IsCompatibleFramework(targetFramework))
                    {
                        continue;
                    }

                    foreach (var dep in group.Elements(ns + "dependency"))
                    {
                        var depName = dep.Attribute("id")?.Value;
                        var depVersion = dep.Attribute("version")?.Value;

                        if (!string.IsNullOrEmpty(depName) && 
                            !IsSystemPackage(depName)) // Skip system/framework packages
                        {
                            dependencies.Add((depName, depVersion));
                        }
                    }
                }

                // Also check for dependencies without groups (older format)
                var directDeps = nuspecXml.Descendants(ns + "dependencies")
                    .Elements(ns + "dependency")
                    .Where(d => !d.Parent.Name.LocalName.Equals("group", StringComparison.OrdinalIgnoreCase));

                foreach (var dep in directDeps)
                {
                    var depName = dep.Attribute("id")?.Value;
                    var depVersion = dep.Attribute("version")?.Value;

                    if (!string.IsNullOrEmpty(depName) && 
                        !IsSystemPackage(depName))
                    {
                        dependencies.Add((depName, depVersion));
                    }
                }

                return dependencies.Distinct().ToList();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Error parsing dependencies: {ex.Message}");
                return dependencies;
            }
        }

        /// <summary>
        /// Finds the most compatible framework folder in a package directory
        /// </summary>
        private string? FindCompatibleFrameworkFolder(string packageDirectory)
        {
            try
            {
                var foundDlls = new List<string>();
                
                // 1. Check lib folder first (most common location)
                var libFolders = Directory.GetDirectories(packageDirectory, "lib", SearchOption.AllDirectories);
                if (libFolders.Any())
                {
                    var libPath = FindBestFrameworkMatch(libFolders[0]);
                    if (!string.IsNullOrEmpty(libPath))
                    {
                        return libPath;
                    }
                }

                // 2. Check runtimes folder (platform-specific DLLs)
                var runtimesFolders = Directory.GetDirectories(packageDirectory, "runtimes", SearchOption.AllDirectories);
                if (runtimesFolders.Any())
                {
                    // Look for win-x64, win, or any runtime folder with DLLs
                    var runtimePreference = new[] { "win-x64", "win-x86", "win", "any", "unix", "linux-x64" };
                    
                    foreach (var runtimeId in runtimePreference)
                    {
                        var runtimePath = Directory.GetDirectories(runtimesFolders[0], runtimeId, SearchOption.AllDirectories).FirstOrDefault();
                        if (runtimePath != null)
                        {
                            // Look for lib or native folder inside runtime
                            var libInRuntime = Path.Combine(runtimePath, "lib");
                            if (Directory.Exists(libInRuntime))
                            {
                                var frameworkPath = FindBestFrameworkMatch(libInRuntime);
                                if (!string.IsNullOrEmpty(frameworkPath))
                                {
                                    AnsiConsole.MarkupLine($"[cyan]→[/] Using runtime-specific: {runtimeId}");
                                    return frameworkPath;
                                }
                            }
                            
                            // Check for direct DLLs in runtime folder
                            var runtimeDlls = Directory.GetFiles(runtimePath, "*.dll", SearchOption.TopDirectoryOnly);
                            if (runtimeDlls.Any())
                            {
                                AnsiConsole.MarkupLine($"[cyan]→[/] Using runtime-specific: {runtimeId}");
                                return runtimePath;
                            }
                        }
                    }
                    
                    // Fall back to any runtime folder with DLLs
                    var anyRuntimeWithDlls = Directory.GetDirectories(runtimesFolders[0], "*", SearchOption.AllDirectories)
                        .FirstOrDefault(d => Directory.GetFiles(d, "*.dll", SearchOption.AllDirectories).Any());
                    
                    if (anyRuntimeWithDlls != null)
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Using runtime: {Path.GetFileName(anyRuntimeWithDlls)}");
                        
                        // Check for lib subfolder first
                        var libInRuntime = Directory.GetDirectories(anyRuntimeWithDlls, "lib", SearchOption.TopDirectoryOnly).FirstOrDefault();
                        if (libInRuntime != null)
                        {
                            var frameworkPath = FindBestFrameworkMatch(libInRuntime);
                            if (!string.IsNullOrEmpty(frameworkPath))
                            {
                                return frameworkPath;
                            }
                        }
                        
                        // Return the runtime folder itself if it has DLLs
                        if (Directory.GetFiles(anyRuntimeWithDlls, "*.dll", SearchOption.TopDirectoryOnly).Any())
                        {
                            return anyRuntimeWithDlls;
                        }
                    }
                }

                // 3. Check ref folder (reference assemblies - compile-time only, but better than nothing)
                var refFolders = Directory.GetDirectories(packageDirectory, "ref", SearchOption.AllDirectories);
                if (refFolders.Any())
                {
                    var refPath = FindBestFrameworkMatch(refFolders[0]);
                    if (!string.IsNullOrEmpty(refPath))
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Using reference assemblies (ref) - may need runtime DLLs");
                        return refPath;
                    }
                }

                // 4. Check for DLLs directly in package root
                var directDlls = Directory.GetFiles(packageDirectory, "*.dll", SearchOption.TopDirectoryOnly);
                if (directDlls.Any())
                {
                    AnsiConsole.MarkupLine($"[cyan]→[/] Found DLLs in package root");
                    return packageDirectory;
                }

                return null;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Error finding compatible framework: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds the best framework match within a lib/ref folder
        /// </summary>
        private string? FindBestFrameworkMatch(string libOrRefFolder)
        {
            // Look for the most compatible framework folder - search ONLY direct children, not recursive
            var preferredOrder = new[] 
            { 
                "net8.0",           // Exact match for BeepShell runtime (net8.0)
                "net8.0-windows",   // Windows-specific net8.0
                "net7.0",           // Compatible with net8.0
                "net6.0",           // Compatible with net8.0
                "net5.0",           // Compatible with net8.0
                "netcoreapp3.1",    // Compatible with net8.0
                "netstandard2.1",   // Compatible with .NET Core 3.0+
                "netstandard2.0",   // Compatible with .NET Core 2.0+
                "net48",            // .NET Framework (may have compatibility issues)
                "net472", 
                "net471", 
                "net47", 
                "net462", 
                "net461", 
                "net46",
                "net45",
                "net40"
            };
            
            // Get all direct child directories (framework folders)
            var frameworkFolders = Directory.Exists(libOrRefFolder) 
                ? Directory.GetDirectories(libOrRefFolder, "*", SearchOption.TopDirectoryOnly)
                : new string[0];
            
            // Try each preferred framework in order
            foreach (var preferred in preferredOrder)
            {
                var targetFolder = frameworkFolders.FirstOrDefault(f => 
                    Path.GetFileName(f).Equals(preferred, StringComparison.OrdinalIgnoreCase));
                
                if (targetFolder != null && Directory.GetFiles(targetFolder, "*.dll").Any())
                {
                    var framework = Path.GetFileName(targetFolder);
                    AnsiConsole.MarkupLine($"[cyan]→[/] Selected framework: {framework}");
                    return targetFolder;
                }
            }
            
            // Return any folder with DLLs (with warning)
            var anyLibFolder = frameworkFolders.FirstOrDefault(d => Directory.GetFiles(d, "*.dll").Any());
            
            if (anyLibFolder != null)
            {
                var framework = Path.GetFileName(anyLibFolder);
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Using framework: {framework} (may have compatibility issues)");
                return anyLibFolder;
            }

            return null;
        }

        /// <summary>
        /// Checks if a framework is compatible with net8.0 runtime
        /// </summary>
        private bool IsCompatibleFramework(string targetFramework)
        {
            var compatible = new[]
            {
                "net8.0", "net7.0", "net6.0", "net5.0", 
                "netcoreapp3.1", "netcoreapp3.0", "netcoreapp2.2", "netcoreapp2.1", "netcoreapp2.0",
                "netstandard2.1", "netstandard2.0", "netstandard1.6", "netstandard1.5", "netstandard1.4",
                "netstandard1.3", "netstandard1.2", "netstandard1.1", "netstandard1.0"
            };

            return compatible.Any(f => targetFramework.StartsWith(f, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a package is a system/framework package that doesn't need to be downloaded
        /// </summary>
        private bool IsSystemPackage(string packageName)
        {
            // These are built into the .NET runtime and don't need separate download
            var systemPackages = new[]
            {
                "Microsoft.NETCore.Platforms",
                "NETStandard.Library",
                "Microsoft.Win32.Primitives",
                "System.AppContext",
                "System.Collections",
                "System.Collections.Concurrent",
                "System.Console",
                "System.Diagnostics.Debug",
                "System.Diagnostics.Tools",
                "System.Diagnostics.Tracing",
                "System.Globalization",
                "System.Globalization.Calendars",
                "System.IO",
                "System.IO.Compression",
                "System.IO.Compression.ZipFile",
                "System.IO.FileSystem",
                "System.IO.FileSystem.Primitives",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Net.Http",
                "System.Net.Primitives",
                "System.Net.Sockets",
                "System.ObjectModel",
                "System.Reflection",
                "System.Reflection.Extensions",
                "System.Reflection.Primitives",
                "System.Resources.ResourceManager",
                "System.Runtime",
                "System.Runtime.Extensions",
                "System.Runtime.Handles",
                "System.Runtime.InteropServices",
                "System.Runtime.InteropServices.RuntimeInformation",
                "System.Runtime.Numerics",
                "System.Security.Cryptography.Algorithms",
                "System.Security.Cryptography.Encoding",
                "System.Security.Cryptography.Primitives",
                "System.Security.Cryptography.X509Certificates",
                "System.Text.Encoding",
                "System.Text.Encoding.Extensions",
                "System.Text.RegularExpressions",
                "System.Threading",
                "System.Threading.Tasks",
                "System.Threading.Timer",
                "System.Xml.ReaderWriter",
                "System.Xml.XDocument"
            };

            return systemPackages.Any(sp => packageName.Equals(sp, StringComparison.OrdinalIgnoreCase));
        }
    }
}
