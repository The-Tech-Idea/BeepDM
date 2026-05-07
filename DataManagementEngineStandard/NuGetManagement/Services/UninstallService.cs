using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
 

namespace TheTechIdea.Beep.NuGetManagement.Services
{
    public class UninstallService
    {
        private readonly IDMLogger _logger;
        private readonly LoadService _loadService;

        public UninstallService(IDMLogger logger, LoadService loadService)
        {
            _logger = logger;
            _loadService = loadService;
        }

        public async Task<bool> UninstallAsync(string packageId, string installDirectory, bool removeDependencies = false)
        {
            try
            {
                _logger?.LogWithContext($"Uninstalling package {packageId}...", null);

                // Unload if currently loaded
                await _loadService.UnloadAsync(packageId);

                // Remove installation directory
                var packageDir = Path.Combine(installDirectory, packageId);
                if (Directory.Exists(packageDir))
                {
                    Directory.Delete(packageDir, true);
                    _logger?.LogWithContext($"Removed installation directory: {packageDir}", null);
                }

                // Remove from cache if requested
                // Note: We typically don't remove from global cache to avoid breaking other packages

                _logger?.LogWithContext($"Successfully uninstalled {packageId}", null);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error uninstalling {packageId}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<BulkUninstallResult> BulkUninstallAsync(List<string> packageIds, string installDirectory)
        {
            var result = new BulkUninstallResult { TotalRequested = packageIds.Count };

            foreach (var packageId in packageIds)
            {
                var success = await UninstallAsync(packageId, installDirectory);
                if (success)
                    result.Successful++;
                else
                    result.Failed++;
            }

            return result;
        }

        public async Task<bool> RepairAsync(string packageId, string installDirectory, DownloadService downloadService, InstallService installService)
        {
            try
            {
                _logger?.LogWithContext($"Repairing package {packageId}...", null);

                // Unload
                await _loadService.UnloadAsync(packageId);

                // Remove existing installation
                var packageDir = Path.Combine(installDirectory, packageId);
                if (Directory.Exists(packageDir))
                {
                    Directory.Delete(packageDir, true);
                }

                // Re-download and install
                var downloadResult = await downloadService.DownloadAsync(packageId);
                if (!downloadResult.Success)
                {
                    _logger?.LogWithContext($"Failed to re-download {packageId} for repair", null);
                    return false;
                }

                var installResult = await installService.InstallAsync(
                    downloadResult.InstallPath,
                    installDirectory,
                    packageId,
                    downloadResult.Version);

                if (!installResult.Success)
                {
                    _logger?.LogWithContext($"Failed to re-install {packageId} for repair", null);
                    return false;
                }

                // Reload
                await _loadService.LoadAsync(installResult.InstallPath, packageId, downloadResult.Version);

                _logger?.LogWithContext($"Successfully repaired {packageId}", null);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error repairing {packageId}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> VerifyAsync(string packageId, string installDirectory)
        {
            try
            {
                var packageDir = Path.Combine(installDirectory, packageId);
                if (!Directory.Exists(packageDir))
                {
                    _logger?.LogWithContext($"Package {packageId} not found for verification", null);
                    return false;
                }

                // Check for DLL files
                var dllFiles = Directory.GetFiles(packageDir, "*.dll", SearchOption.AllDirectories);
                if (!dllFiles.Any())
                {
                    _logger?.LogWithContext($"Package {packageId} has no DLL files", null);
                    return false;
                }

                // Verify each DLL can be read
                foreach (var dll in dllFiles)
                {
                    try
                    {
                        using (var stream = File.OpenRead(dll))
                        {
                            // Just verify we can open the file
                            var header = new byte[2];
                            stream.Read(header, 0, 2);
                            if (header[0] != 'M' || header[1] != 'Z')
                            {
                                _logger?.LogWithContext($"Package {packageId}: Invalid DLL header in {Path.GetFileName(dll)}", null);
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWithContext($"Package {packageId}: Error reading {Path.GetFileName(dll)}: {ex.Message}", null);
                        return false;
                    }
                }

                _logger?.LogWithContext($"Package {packageId} verified successfully", null);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error verifying {packageId}: {ex.Message}", ex);
                return false;
            }
        }
    }
}
