using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO.Compression;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Lightweight NuGet package downloader extracted/implemented for Assembly_helpersStandard
    /// This mirrors the feature set used in BeepShell.Shared but uses IDMLogger instead of console UI.
    /// </summary>
    public class NuggetPackageDownloader
    {
        private readonly string _downloadDirectory;
        private readonly IDMLogger _logger;

        public NuggetPackageDownloader(string downloadDirectory, IDMLogger logger)
        {
            _downloadDirectory = downloadDirectory ?? Path.Combine(AppContext.BaseDirectory, "NugetDownloads");
            _logger = logger;

            if (!Directory.Exists(_downloadDirectory))
                Directory.CreateDirectory(_downloadDirectory);
        }

        public async Task<string?> DownloadPackageAsync(string packageName, string? version = null, IEnumerable<string> sources = null)
        {
            try
            {
                var sourceList = (sources ?? new[] { "https://api.nuget.org/v3/index.json" }).ToList();
                _logger?.LogWithContext($"Downloading {packageName} from {string.Join(',', sourceList)}...", null);

                var tempDir = Path.Combine(_downloadDirectory, $"temp_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    var csprojPath = Path.Combine(tempDir, "temp.csproj");
                    var csprojContent = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net8.0</TargetFramework>\n  </PropertyGroup>\n</Project>";
                    File.WriteAllText(csprojPath, csprojContent);

                    var versionArg = !string.IsNullOrWhiteSpace(version) ? $" -v {version}" : "";
                    string successfulSource = null;
                    foreach (var candidate in sourceList)
                    {
                        var sourceArg = $" -s \"{candidate}\"";
                        var command = $"add \"{csprojPath}\" package {packageName}{versionArg}{sourceArg} --package-directory \"{_downloadDirectory}\"";
                        var psi = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = command,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = tempDir
                        };

                        using var proc = new Process { StartInfo = psi };
                        proc.Start();
                        var output = await proc.StandardOutput.ReadToEndAsync();
                        var error = await proc.StandardError.ReadToEndAsync();
                        await proc.WaitForExitAsync();
                        if (proc.ExitCode == 0)
                        {
                            successfulSource = candidate;
                            _logger?.LogWithContext($"Downloaded {packageName} from {candidate}", null);
                            break;
                        }
                        else
                        {
                            _logger?.LogWithContext($"Not found on {candidate}: {error}", null);
                        }
                    }

                    if (successfulSource == null)
                    {
                        _logger?.LogWithContext($"Failed to download package {packageName} from any provided source", null);
                        return null;
                    }

                    // Find the nupkg or package folder
                    var nupkgFiles = Directory.GetFiles(_downloadDirectory, $"{packageName.ToLower()}*.nupkg", SearchOption.AllDirectories);
                    if (nupkgFiles.Any())
                    {
                        var nupkgPath = nupkgFiles.OrderByDescending(f => new FileInfo(f).LastWriteTime).First();
                        _logger?.LogWithContext($"Package downloaded: {nupkgPath}", null);
                        return nupkgPath;
                    }

                    var packageFolder = Path.Combine(_downloadDirectory, packageName.ToLower());
                    if (Directory.Exists(packageFolder))
                    {
                        var dllFiles = Directory.GetFiles(packageFolder, "*.dll", SearchOption.AllDirectories);
                        if (dllFiles.Any())
                        {
                            _logger?.LogWithContext($"Package folder downloaded: {packageFolder}", null);
                            return packageFolder;
                        }
                    }

                    _logger?.LogWithContext($"Package downloaded but could not find files: {packageName}", null);
                    return _downloadDirectory;
                }
                finally
                {
                    try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error downloading package {packageName}: {ex.Message}", ex);
                return null;
            }
        }

        public string? ExtractNuGetPackage(string nupkgPath)
        {
            try
            {
                if (!File.Exists(nupkgPath))
                {
                    _logger?.LogWithContext($"Package file not found: {nupkgPath}", null);
                    return null;
                }

                var extractDir = Path.Combine(_downloadDirectory, Path.GetFileNameWithoutExtension(nupkgPath));
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                Directory.CreateDirectory(extractDir);

                ZipFile.ExtractToDirectory(nupkgPath, extractDir);
                _logger?.LogWithContext($"Package extracted: {extractDir}", null);

                var compatible = FindCompatibleFrameworkFolder(extractDir);
                if (!string.IsNullOrEmpty(compatible)) return compatible;

                _logger?.LogWithContext($"No compatible framework folder found, using extraction root: {extractDir}", null);
                return extractDir;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error extracting package: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<string[]> SearchPackageVersionsAsync(string packageName, IEnumerable<string> sources = null)
        {
            try
            {
                var sourceList = (sources ?? new[] { "https://api.nuget.org/v3/index.json" }).ToList();
                var nugetSource = sourceList.First();
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

                // Simplified, return latest placeholder if output contains text
                if (!string.IsNullOrWhiteSpace(output)) return new[] { "latest" };
                return Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public async Task<Dictionary<string, string>> DownloadPackageWithDependenciesAsync(
            string packageName,
            string? version = null,
            IEnumerable<string> sources = null,
            HashSet<string>? downloadedPackages = null)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            downloadedPackages ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var packageKey = $"{packageName}|{version ?? "latest"}";
            if (downloadedPackages.Contains(packageKey)) return result;
            downloadedPackages.Add(packageKey);

            try
            {
                _logger?.LogWithContext($"Downloading {packageName} {(version != null ? $"v{version}" : string.Empty)}...", null);
                var nupkgPath = await DownloadPackageAsync(packageName, version, sources);
                if (string.IsNullOrEmpty(nupkgPath))
                {
                    _logger?.LogWithContext($"Failed to download {packageName}", null);
                    return result;
                }

                string packageRootDir;
                if (nupkgPath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
                {
                    packageRootDir = Path.Combine(_downloadDirectory, Path.GetFileNameWithoutExtension(nupkgPath));
                    if (!Directory.Exists(packageRootDir))
                    {
                        Directory.CreateDirectory(packageRootDir);
                        ZipFile.ExtractToDirectory(nupkgPath, packageRootDir);
                        _logger?.LogWithContext($"Extracted to {packageRootDir}", null);
                    }
                }
                else if (Directory.Exists(nupkgPath))
                {
                    packageRootDir = nupkgPath;
                }
                else
                {
                    _logger?.LogWithContext($"Invalid package path: {nupkgPath}", null);
                    return result;
                }

                var compatibleFrameworkPath = FindCompatibleFrameworkFolder(packageRootDir);
                if (!string.IsNullOrEmpty(compatibleFrameworkPath))
                {
                    result[packageName] = compatibleFrameworkPath;

                    var dependencies = GetPackageDependencies(packageRootDir);
                    if (dependencies.Any())
                    {
                        _logger?.LogWithContext($"Found {dependencies.Count} dependencies for {packageName}", null);
                        foreach (var (depName, depVersion) in dependencies)
                        {
                            var depResults = await DownloadPackageWithDependenciesAsync(depName, depVersion, sources, downloadedPackages);
                            foreach (var kvp in depResults)
                            {
                                if (!result.ContainsKey(kvp.Key)) result[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                else
                {
                    _logger?.LogWithContext($"No compatible framework folder for {packageName}, using root: {packageRootDir}", null);
                    result[packageName] = packageRootDir;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error downloading {packageName}: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Installs files from a package folder into the application directory so that runtime resolution
        /// can find the assemblies easily. Copies DLLs and native runtime files.
        /// </summary>
        public string? InstallPackageToAppDirectory(string packageFrameworkPath, string? appDirectory, string? pluginId = null, string? version = null, bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(packageFrameworkPath) || !Directory.Exists(packageFrameworkPath))
            {
                _logger?.LogWithContext($"InstallPackageToAppDirectory: invalid source path {packageFrameworkPath}", null);
                return null;
            }

            var baseTargets = appDirectory ?? Path.Combine(AppContext.BaseDirectory, "Plugins");
            var pluginFolderName = pluginId ?? Path.GetFileName(packageFrameworkPath).Replace(".", "_");
            var ver = !string.IsNullOrWhiteSpace(version) ? version : DateTime.UtcNow.Ticks.ToString();
            var targetDir = Path.Combine(baseTargets, pluginFolderName, ver);
            try
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                foreach (var file in Directory.GetFiles(packageFrameworkPath, "*.dll", SearchOption.AllDirectories))
                {
                    var dest = Path.Combine(targetDir, Path.GetFileName(file));
                    if (File.Exists(dest) && !overwrite) continue;
                    File.Copy(file, dest, overwrite);
                }

                // Copy native assets and resources
                var nativeFiles = Directory.GetFiles(packageFrameworkPath, "*.so", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(packageFrameworkPath, "*.dylib", SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(packageFrameworkPath, "*.dll", SearchOption.AllDirectories));
                foreach (var nf in nativeFiles)
                {
                        var dest = Path.Combine(targetDir, Path.GetFileName(nf));
                        if (File.Exists(dest) && !overwrite) continue;
                        File.Copy(nf, dest, overwrite);
                }

                _logger?.LogWithContext($"Installed package to {targetDir}", null);
                return targetDir;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"InstallPackageToAppDirectory failed: {ex.Message}", ex);
                return null;
            }
        }

        private List<(string Name, string? Version)> GetPackageDependencies(string extractedPackagePath)
        {
            var dependencies = new List<(string, string?)>();
            try
            {
                var nuspecFiles = Directory.GetFiles(extractedPackagePath, "*.nuspec", SearchOption.AllDirectories);
                if (!nuspecFiles.Any()) return dependencies;

                var nuspecPath = nuspecFiles.First();
                _logger?.LogWithContext($"Parsing dependencies from {Path.GetFileName(nuspecPath)}", null);

                var doc = XDocument.Load(nuspecPath);
                var ns = doc.Root?.GetDefaultNamespace();
                if (ns == null) return dependencies;

                var dependencyGroups = doc.Descendants(ns + "dependencies").Elements(ns + "group");
                foreach (var group in dependencyGroups)
                {
                    var targetFramework = group.Attribute("targetFramework")?.Value;
                    if (!string.IsNullOrEmpty(targetFramework) && !IsCompatibleFramework(targetFramework)) continue;

                    foreach (var dep in group.Elements(ns + "dependency"))
                    {
                        var depName = dep.Attribute("id")?.Value;
                        var depVersion = dep.Attribute("version")?.Value;
                        if (!string.IsNullOrEmpty(depName) && !IsSystemPackage(depName))
                        {
                            dependencies.Add((depName, depVersion));
                        }
                    }
                }

                var directDeps = doc.Descendants(ns + "dependencies").Elements(ns + "dependency");
                foreach (var dep in directDeps)
                {
                    var depName = dep.Attribute("id")?.Value;
                    var depVersion = dep.Attribute("version")?.Value;
                    if (!string.IsNullOrEmpty(depName) && !IsSystemPackage(depName)) dependencies.Add((depName, depVersion));
                }

                return dependencies.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error parsing nuspec: {ex.Message}", ex);
                return dependencies;
            }
        }

        private string? FindCompatibleFrameworkFolder(string packageDirectory)
        {
            try
            {
                var libFolders = Directory.GetDirectories(packageDirectory, "lib", SearchOption.AllDirectories);
                if (libFolders.Any())
                {
                    var path = FindBestFrameworkMatch(libFolders[0]);
                    if (!string.IsNullOrEmpty(path)) return path;
                }

                var runtimesFolders = Directory.GetDirectories(packageDirectory, "runtimes", SearchOption.AllDirectories);
                if (runtimesFolders.Any())
                {
                    var runtimePreference = new[] { "win-x64", "win-x86", "win", "any", "linux-x64", "unix" };
                    foreach (var runtimeId in runtimePreference)
                    {
                        var runtimePath = Directory.GetDirectories(runtimesFolders[0], runtimeId, SearchOption.AllDirectories).FirstOrDefault();
                        if (runtimePath != null)
                        {
                            var libInRuntime = Path.Combine(runtimePath, "lib");
                            if (Directory.Exists(libInRuntime))
                            {
                                var frameworkPath = FindBestFrameworkMatch(libInRuntime);
                                if (!string.IsNullOrEmpty(frameworkPath)) return frameworkPath;
                            }

                            var runtimeDlls = Directory.GetFiles(runtimePath, "*.dll", SearchOption.TopDirectoryOnly);
                            if (runtimeDlls.Any()) return runtimePath;
                        }
                    }

                    var anyRuntime = Directory.GetDirectories(runtimesFolders[0], "*", SearchOption.AllDirectories)
                        .FirstOrDefault(d => Directory.GetFiles(d, "*.dll", SearchOption.AllDirectories).Any());
                    if (anyRuntime != null)
                    {
                        var libInRuntime = Directory.GetDirectories(anyRuntime, "lib", SearchOption.TopDirectoryOnly).FirstOrDefault();
                        if (libInRuntime != null)
                        {
                            var frameworkPath = FindBestFrameworkMatch(libInRuntime);
                            if (!string.IsNullOrEmpty(frameworkPath)) return frameworkPath;
                        }
                        if (Directory.GetFiles(anyRuntime, "*.dll", SearchOption.TopDirectoryOnly).Any()) return anyRuntime;
                    }
                }

                var refFolders = Directory.GetDirectories(packageDirectory, "ref", SearchOption.AllDirectories);
                if (refFolders.Any())
                {
                    var refPath = FindBestFrameworkMatch(refFolders[0]);
                    if (!string.IsNullOrEmpty(refPath)) return refPath;
                }

                var directDlls = Directory.GetFiles(packageDirectory, "*.dll", SearchOption.TopDirectoryOnly);
                if (directDlls.Any()) return packageDirectory;

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error finding compatible framework: {ex.Message}", ex);
                return null;
            }
        }

        private string? FindBestFrameworkMatch(string libOrRefFolder)
        {
            var preferredOrder = new[] { "net8.0", "net8.0-windows", "net7.0", "net6.0", "net5.0", "netcoreapp3.1", "netstandard2.1", "netstandard2.0", "net48" };
            var frameworkFolders = Directory.Exists(libOrRefFolder) ? Directory.GetDirectories(libOrRefFolder, "*", SearchOption.TopDirectoryOnly) : Array.Empty<string>();
            foreach (var preferred in preferredOrder)
            {
                var targetFolder = frameworkFolders.FirstOrDefault(f => Path.GetFileName(f).Equals(preferred, StringComparison.OrdinalIgnoreCase));
                if (targetFolder != null && Directory.GetFiles(targetFolder, "*.dll").Any()) return targetFolder;
            }
            var anyFolder = frameworkFolders.FirstOrDefault(d => Directory.GetFiles(d, "*.dll").Any());
            return anyFolder;
        }

        private bool IsCompatibleFramework(string targetFramework)
        {
            var compatible = new[] { "net8.0", "net7.0", "net6.0", "net5.0", "netcoreapp3.1", "netstandard2.1", "netstandard2.0" };
            return compatible.Any(f => targetFramework.StartsWith(f, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsSystemPackage(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName)) return false;
            var system = new[] { "Microsoft.NETCore.Platforms", "NETStandard.Library", "System.*", "Microsoft.*" };
            return system.Any(sp => packageName.StartsWith(sp, StringComparison.OrdinalIgnoreCase));
        }
    }
}
