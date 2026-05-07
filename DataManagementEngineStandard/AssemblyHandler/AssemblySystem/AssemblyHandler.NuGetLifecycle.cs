using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.NuGet;
using TheTechIdea.Beep.NuGetManagement;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// AssemblyHandler partial class - IAssemblyLoadContext and NuGet Lifecycle Implementation
    /// Provides unified NuGet package management capabilities using NuGetPackageManager.
    /// </summary>
    public partial class AssemblyHandler : IAssemblyHandler
    {
        #region IAssemblyLoadContext Implementation

        /// <summary>
        /// Gets whether this load context supports shared context mode.
        /// Default AssemblyHandler does not support shared context isolation.
        /// </summary>
        public bool SupportsSharedContext => false;

        /// <summary>
        /// Loads a package from the specified path into the current AppDomain.
        /// </summary>
        public async Task<NuggetInfo> LoadPackageAsync(string packagePath, string packageId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(packagePath) || !Directory.Exists(packagePath))
                {
                    Logger?.WriteLog($"LoadPackageAsync: Invalid path {packagePath}");
                    return null;
                }

                var assemblies = LoadAssembliesFromFolder(packagePath, FolderFileTypes.Nugget, scanForDataSources: true);
                
                var nuggetInfo = new NuggetInfo
                {
                    Id = packageId,
                    Name = packageId,
                    SourcePath = packagePath,
                    LoadedAt = DateTime.UtcNow,
                    LoadedAssemblies = assemblies?.ToList() ?? new List<Assembly>(),
                    IsSharedContext = false,
                    IsActive = true
                };

                // Track the nugget via legacy manager if available
                // Note: NuggetManager tracks automatically via LoadNugget

                Logger?.WriteLog($"LoadPackageAsync: Loaded {nuggetInfo.LoadedAssemblies.Count} assemblies from {packageId}");
                return nuggetInfo;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LoadPackageAsync: Error loading {packageId} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Unloads a package by removing its assemblies from tracking.
        /// Note: In default AssemblyHandler, assemblies cannot be unloaded from AppDomain.
        /// </summary>
        public bool UnloadPackage(string packageId)
        {
            try
            {
                if (_nuggetManager != null)
                {
                    return _nuggetManager.UnloadNugget(packageId);
                }

                // Fallback: remove from local tracking
                var toRemove = LoadedAssemblies.Where(a => 
                    a.Location.Contains(packageId, StringComparison.OrdinalIgnoreCase)).ToList();
                
                foreach (var assembly in toRemove)
                {
                    LoadedAssemblies.Remove(assembly);
                    Assemblies.RemoveAll(a => a.DllLib == assembly);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"UnloadPackage: Error unloading {packageId} - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all currently loaded assemblies.
        /// </summary>
        public IEnumerable<Assembly> GetLoadedAssemblies()
        {
            return LoadedAssemblies?.AsEnumerable() ?? Enumerable.Empty<Assembly>();
        }

        /// <summary>
        /// Checks if a package is currently loaded.
        /// </summary>
        public bool IsPackageLoaded(string packageId)
        {
            if (_nuggetManager != null)
            {
                return _nuggetManager.GetNuggetInfo(packageId) != null;
            }

            return LoadedAssemblies.Any(a => 
                a.GetName().Name.Contains(packageId, StringComparison.OrdinalIgnoreCase) ||
                a.Location.Contains(packageId, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region NuGet Package Lifecycle

        /// <summary>
        /// Gets the enhanced NuGet package manager instance.
        /// Lazy-initialized on first access.
        /// </summary>
        public NuGetPackageManager NuGetManager
        {
            get
            {
                if (_nugetPackageManager == null)
                {
                    _nugetPackageManager = new NuGetPackageManager(Logger, this);
                }
                return _nugetPackageManager;
            }
        }

        /// <summary>
        /// Installs and loads a NuGet package with all its dependencies.
        /// </summary>
        public async Task<NuggetInfo> InstallAndLoadNuGetPackageAsync(string packageName, string version = null)
        {
            try
            {
                ErrorObject.Flag = Errors.Ok;
                var nuggetInfo = await NuGetManager.LoadAsync(packageName, version, false);
                
                if (nuggetInfo?.LoadedAssemblies != null)
                {
                    foreach (var assembly in nuggetInfo.LoadedAssemblies)
                    {
                        if (!LoadedAssemblies.Contains(assembly))
                            LoadedAssemblies.Add(assembly);
                        
                        var assemblyRep = new assemblies_rep(assembly, nuggetInfo.SourcePath, assembly.FullName, FolderFileTypes.Nugget);
                        if (!Assemblies.Any(a => a.DllLib == assembly))
                            Assemblies.Add(assemblyRep);
                    }
                    
                    Logger?.WriteLog($"Successfully installed and loaded NuGet package: {packageName}");
                }
                
                return nuggetInfo;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                ErrorObject.Ex = ex;
                Logger?.WriteLog($"Error installing/loading NuGet package {packageName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Installs a NuGet package without loading it.
        /// </summary>
        public async Task<PackageInstallResult> InstallNuGetPackageAsync(string packageName, string version = null)
        {
            try
            {
                ErrorObject.Flag = Errors.Ok;
                return await NuGetManager.InstallAsync(packageName, version);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                ErrorObject.Ex = ex;
                Logger?.WriteLog($"Error installing NuGet package {packageName}: {ex.Message}");
                return new PackageInstallResult { PackageId = packageName, Error = ex.Message };
            }
        }

        /// <summary>
        /// Updates a NuGet package to the latest or specified version.
        /// </summary>
        public async Task<PackageUpdateResult> UpdateNuGetPackageAsync(string packageName, string version = null)
        {
            try
            {
                ErrorObject.Flag = Errors.Ok;
                var result = await NuGetManager.UpdateAsync(packageName, version);
                
                if (result.Success && result.WasUpdated)
                {
                    Logger?.WriteLog($"Successfully updated {packageName} to {result.NewVersion}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                ErrorObject.Ex = ex;
                Logger?.WriteLog($"Error updating NuGet package {packageName}: {ex.Message}");
                return new PackageUpdateResult { PackageId = packageName, Error = ex.Message };
            }
        }

        /// <summary>
        /// Uninstalls a NuGet package completely.
        /// </summary>
        public async Task<bool> UninstallNuGetPackageAsync(string packageName, bool removeDependencies = false)
        {
            try
            {
                ErrorObject.Flag = Errors.Ok;
                var result = await NuGetManager.UninstallAsync(packageName, removeDependencies);
                
                if (result)
                {
                    Logger?.WriteLog($"Successfully uninstalled NuGet package: {packageName}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                ErrorObject.Ex = ex;
                Logger?.WriteLog($"Error uninstalling NuGet package {packageName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Repairs a corrupted or damaged NuGet package.
        /// </summary>
        public async Task<bool> RepairNuGetPackageAsync(string packageName)
        {
            try
            {
                ErrorObject.Flag = Errors.Ok;
                var result = await NuGetManager.RepairAsync(packageName);
                
                if (result)
                {
                    Logger?.WriteLog($"Successfully repaired NuGet package: {packageName}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                ErrorObject.Ex = ex;
                Logger?.WriteLog($"Error repairing NuGet package {packageName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Performs bulk update of all installed NuGet packages.
        /// </summary>
        public async Task<BulkUpdateResult> UpdateAllNuGetPackagesAsync()
        {
            try
            {
                ErrorObject.Flag = Errors.Ok;
                return await NuGetManager.BulkUpdateAsync();
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                ErrorObject.Ex = ex;
                Logger?.WriteLog($"Error during bulk update: {ex.Message}");
                return new BulkUpdateResult { Error = ex.Message };
            }
        }

        /// <summary>
        /// Gets detailed metadata for a NuGet package.
        /// </summary>
        public async Task<PackageMetadata> GetNuGetPackageMetadataAsync(string packageName, string version)
        {
            try
            {
                return await NuGetManager.GetMetadataAsync(packageName, version);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting metadata for {packageName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets dependency tree for a NuGet package.
        /// </summary>
        public async Task<List<PackageDependency>> GetNuGetPackageDependenciesAsync(string packageName, string version)
        {
            try
            {
                return await NuGetManager.GetDependenciesAsync(packageName, version);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting dependencies for {packageName}: {ex.Message}");
                return new List<PackageDependency>();
            }
        }

        /// <summary>
        /// Gets all installed NuGet packages.
        /// </summary>
        public async Task<List<InstalledPackageInfo>> GetInstalledNuGetPackagesAsync()
        {
            try
            {
                return await NuGetManager.GetInstalledPackagesAsync();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting installed packages: {ex.Message}");
                return new List<InstalledPackageInfo>();
            }
        }

        /// <summary>
        /// Checks if a NuGet package is installed.
        /// </summary>
        public async Task<bool> IsNuGetPackageInstalledAsync(string packageName)
        {
            try
            {
                return await NuGetManager.IsInstalledAsync(packageName);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error checking if {packageName} is installed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clears the NuGet package cache.
        /// </summary>
        public async Task ClearNuGetCacheAsync(string packageName = null, string version = null)
        {
            try
            {
                await NuGetManager.ClearCacheAsync(packageName, version);
                Logger?.WriteLog($"Cleared NuGet cache{(packageName != null ? $" for {packageName}" : "")}");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error clearing NuGet cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Exports installed packages to a JSON file.
        /// </summary>
        public async Task ExportInstalledPackagesAsync(string filePath)
        {
            try
            {
                await NuGetManager.ExportPackagesAsync(filePath);
                Logger?.WriteLog($"Exported installed packages to {filePath}");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error exporting packages to {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Imports packages from a JSON file.
        /// </summary>
        public async Task ImportPackagesAsync(string filePath)
        {
            try
            {
                await NuGetManager.ImportPackagesAsync(filePath);
                Logger?.WriteLog($"Imported packages from {filePath}");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error importing packages from {filePath}: {ex.Message}");
            }
        }

        #endregion
    }
}
