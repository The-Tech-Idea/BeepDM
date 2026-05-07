using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.NuGetManagement.Models;

namespace TheTechIdea.Beep.NuGetManagement.Services
{
    /// <summary>
    /// Handles extracting and installing NuGet packages to the application directory.
    /// </summary>
    public class InstallService
    {
        private readonly IDMLogger _logger;

        /// <summary>
        /// Initializes a new instance of the InstallService.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        public InstallService(IDMLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Installs a downloaded NuGet package to the specified directory.
        /// </summary>
        /// <param name="packagePath">Path to the downloaded package directory.</param>
        /// <param name="installDirectory">Target installation directory.</param>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The package version.</param>
        /// <param name="overwrite">If true, overwrites existing files.</param>
        /// <returns>A <see cref="PackageInstallResult"/> containing installation status.</returns>
        public async Task<PackageInstallResult> InstallAsync(string packagePath, string installDirectory, string packageId, string version, bool overwrite = false)
        {
            var result = new PackageInstallResult { PackageId = packageId };
            var startTime = DateTime.UtcNow;

            try
            {
                if (string.IsNullOrWhiteSpace(packagePath) || !Directory.Exists(packagePath))
                {
                    result.Error = $"Invalid package path: {packagePath}";
                    return result;
                }

                var targetDir = Path.Combine(installDirectory, packageId, version ?? "latest");
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                // Find and extract nupkg if exists
                var nupkgFiles = Directory.GetFiles(packagePath, "*.nupkg", SearchOption.TopDirectoryOnly);
                if (nupkgFiles.Any())
                {
                    var extractDir = Path.Combine(packagePath, "extracted");
                    if (!Directory.Exists(extractDir))
                    {
                        ZipFile.ExtractToDirectory(nupkgFiles.First(), extractDir);
                    }
                    
                    // Find compatible framework folder
                    var compatiblePath = FindCompatibleFrameworkFolder(extractDir);
                    if (!string.IsNullOrEmpty(compatiblePath))
                    {
                        await CopyPackageFilesAsync(compatiblePath, targetDir, overwrite, result);
                    }
                    else
                    {
                        await CopyPackageFilesAsync(extractDir, targetDir, overwrite, result);
                    }
                }
                else
                {
                    // Already extracted - copy files
                    var compatiblePath = FindCompatibleFrameworkFolder(packagePath);
                    if (!string.IsNullOrEmpty(compatiblePath))
                    {
                        await CopyPackageFilesAsync(compatiblePath, targetDir, overwrite, result);
                    }
                    else
                    {
                        await CopyPackageFilesAsync(packagePath, targetDir, overwrite, result);
                    }
                }

                result.Success = true;
                result.InstallPath = targetDir;
                result.CompletedAt = DateTime.UtcNow;
                result.DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger?.LogWithContext($"Installed {packageId} to {targetDir}", null);

                return result;
            }
            catch (Exception ex)
            {
                result.Error = $"Error installing {packageId}: {ex.Message}";
                _logger?.LogWithContext(result.Error, ex);
                return result;
            }
        }

        public async Task<BulkInstallResult> BulkInstallAsync(List<PackageRequest> packages, string installDirectory)
        {
            var result = new BulkInstallResult { TotalRequested = packages.Count };

            foreach (var package in packages)
            {
                var installResult = await InstallAsync(
                    package.PackageId, // This should be the path, but for bulk we need to download first
                    installDirectory,
                    package.PackageId,
                    package.Version);

                if (installResult.Success)
                    result.Successful++;
                else
                    result.Failed++;

                result.Results.Add(installResult);
            }

            return result;
        }

        private async Task CopyPackageFilesAsync(string sourceDir, string targetDir, bool overwrite, PackageInstallResult result)
        {
            var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\runtimes\\") && !f.Contains("/runtimes/"));

            foreach (var file in files)
            {
                var relativePath = file.Substring(sourceDir.Length + 1);
                var destPath = Path.Combine(targetDir, relativePath);
                var destDir = Path.GetDirectoryName(destPath);

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                if (File.Exists(destPath) && !overwrite)
                {
                    result.Warnings.Add($"Skipped existing file: {relativePath}");
                    continue;
                }

                File.Copy(file, destPath, overwrite);
                result.InstalledFiles.Add(relativePath);
            }
        }

        private string FindCompatibleFrameworkFolder(string packageDirectory)
        {
            try
            {
                var libPath = Path.Combine(packageDirectory, "lib");
                if (!Directory.Exists(libPath))
                    return null;

                var preferredOrder = new[] { "net10.0", "net9.0", "net8.0", "net7.0", "net6.0", "net5.0", "netcoreapp3.1", "netstandard2.1", "netstandard2.0" };
                var frameworkFolders = Directory.GetDirectories(libPath);

                foreach (var preferred in preferredOrder)
                {
                    var targetFolder = frameworkFolders.FirstOrDefault(f => Path.GetFileName(f).Equals(preferred, StringComparison.OrdinalIgnoreCase));
                    if (targetFolder != null && Directory.GetFiles(targetFolder, "*.dll").Any())
                        return targetFolder;
                }

                return frameworkFolders.FirstOrDefault(d => Directory.GetFiles(d, "*.dll").Any());
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error finding compatible framework: {ex.Message}", ex);
                return null;
            }
        }

        public bool IsPackageInstalled(string packageId, string installDirectory, string version = null)
        {
            var packageDir = Path.Combine(installDirectory, packageId);
            if (!Directory.Exists(packageDir))
                return false;

            if (!string.IsNullOrWhiteSpace(version))
            {
                var versionDir = Path.Combine(packageDir, version);
                return Directory.Exists(versionDir) && Directory.GetFiles(versionDir, "*.dll", SearchOption.AllDirectories).Any();
            }

            return Directory.GetFiles(packageDir, "*.dll", SearchOption.AllDirectories).Any();
        }

        public string GetInstallPath(string packageId, string installDirectory, string version = null)
        {
            if (!string.IsNullOrWhiteSpace(version))
                return Path.Combine(installDirectory, packageId, version);
            return Path.Combine(installDirectory, packageId);
        }
    }
}
