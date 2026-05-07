
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Logger;

using TheTechIdea.Beep.NuGet;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Specifies the type of assembly handler implementation to use.
    /// </summary>
    public enum AssemblyHandlerType
    {
        /// <summary>
        /// The default AssemblyHandler implementation (AssemblySystem).
        /// </summary>
        Default,

        /// <summary>
        /// The SharedContextAssemblyHandler implementation (PluginSystem) with enhanced isolation and plugin management.
        /// </summary>
        SharedContext
    }

    public interface IAssemblyHandler : IDisposable, IAssemblyLoadContext
    {
        //  List<IDM_Addin> AddIns { get; set; }

     
        List<string> NamespacestoIgnore { get; set; }
        List<assemblies_rep> Assemblies { get; set; }
        List<Assembly> LoadedAssemblies { get;  set; }
        List<Type> LoaderExtensions { get; set; }
        List<AssemblyClassDefinition> LoaderExtensionClasses { get; set; }
        List<ILoaderExtention> LoaderExtensionInstances { get; set; }
        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args);
        List<AssemblyClassDefinition> DataSourcesClasses { get; set; }
        IConfigEditor ConfigEditor { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        IDMLogger Logger { get; set; }
        IUtil Utilfunction { get; set; }
        bool AddEngineDefaultDrivers();
        void CheckDriverAlreadyExistinList();
        object CreateInstanceFromString(string typeName, params object[] args);
        object CreateInstanceFromString(string dll, string typeName, params object[] args);
        IErrorsInfo GetBuiltinClasses();
        List<ParentChildObject> GetAddinObjects(Assembly asm);
        List<ConnectionDriversConfig> GetDrivers(Assembly asm);
        object GetInstance(string strFullyQualifiedName);
        ParentChildObject RearrangeAddin(string p, string parentid, string Objt);
        Type GetType(string strFullyQualifiedName);
        string LoadAssembly(string path, FolderFileTypes fileTypes);
        IErrorsInfo LoadAllAssembly(IProgress<PassedArgs> progress, CancellationToken token);
        Assembly LoadAssembly(string path);
        bool LoadNugget(string path);
        bool UnloadNugget(string nuggetname);
        bool UnloadAssembly(string assemblyname);
        List<NuggetInfo> GetAllNuggets();
        bool RunMethod(object ObjInstance, string FullClassName, string MethodName);
        AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type, string typename);
        List<Assembly> LoadAssembliesFromFolder(string folderPath, FolderFileTypes folderFileType, bool scanForDataSources = true);
        void AddTypeToCache(string fullName, Type type);

        #region NuGet Search & Download

        /// <summary>
        /// Searches NuGet for packages matching a keyword.
        /// </summary>
        Task<List<NuGetSearchResult>> SearchNuGetPackagesAsync(string searchTerm, int skip = 0, int take = 20, bool includePrerelease = false, CancellationToken token = default);

        /// <summary>
        /// Gets all available versions for a NuGet package.
        /// </summary>
        Task<List<string>> GetNuGetPackageVersionsAsync(string packageId, bool includePrerelease = false, CancellationToken token = default);

        /// <summary>
        /// Downloads and loads a NuGet package with all its dependencies.
        /// </summary>
        Task<List<Assembly>> LoadNuggetFromNuGetAsync(string packageName, string version = null, IEnumerable<string> sources = null, bool useSingleSharedContext = true, string appInstallPath = null, bool useProcessHost = false);

        /// <summary>
        /// Resolves the best local filesystem path for a driver's NuGet package.
        /// Returns null when the package is not available locally.
        /// </summary>
        string ResolveLocalPackagePath(ConnectionDriversConfig driver);

        /// <summary>
        /// Returns true when a driver's NuGet package exists locally in an app plugin folder,
        /// explicit NuggetSource path, or the user's global NuGet cache.
        /// </summary>
        bool HasLocalPackage(ConnectionDriversConfig driver);

        /// <summary>
        /// Loads a driver from a locally available package path without attempting any download.
        /// Returns the resolved load path used for the attempt.
        /// </summary>
        bool LoadDriverFromLocalPackage(ConnectionDriversConfig driver, out string loadPath);

        #endregion

        #region NuGet Source Management

        /// <summary>
        /// Gets all configured NuGet package sources.
        /// </summary>
        List<NuGetSourceConfig> GetNuGetSources();

        /// <summary>
        /// Adds a NuGet package source.
        /// </summary>
        void AddNuGetSource(string name, string url, bool isEnabled = true);

        /// <summary>
        /// Removes a NuGet package source by name.
        /// </summary>
        void RemoveNuGetSource(string name);

        /// <summary>
        /// Gets the list of active (enabled) source URLs.
        /// </summary>
        List<string> GetActiveSourceUrls();

        #endregion

        #region Driver Package Tracking

        /// <summary>
        /// Tracks the association between a driver class and its NuGet package.
        /// </summary>
        void TrackDriverPackage(string packageId, string version, string driverClassName, DataSourceType dsType);

        /// <summary>
        /// Gets all driver-to-package mappings.
        /// </summary>
        List<DriverPackageMapping> GetAllDriverPackageMappings();

        /// <summary>
        /// Checks whether a driver was installed from a NuGet package.
        /// </summary>
        bool IsDriverFromNuGet(string driverClassName);

        /// <summary>
        /// Returns true when the driver's IDataSource class is registered in DataSourcesClasses
        /// OR the ADO.NET assembly (dllname) is present in LoadedAssemblies.
        /// Use this to distinguish "downloaded" (NuggetMissing=false) from "loaded in AppDomain".
        /// </summary>
        bool IsDriverClassLoaded(string classHandler, string dllName = null);

        #endregion

        #region Statistics

        /// <summary>
        /// Gets assembly loading statistics.
        /// </summary>
        AssemblyLoadStatistics GetLoadStatistics();

        #endregion

        #region NuGet Package Lifecycle (Unified)

        /// <summary>
        /// Installs and loads a NuGet package with all its dependencies.
        /// </summary>
        /// <param name="packageName">The package identifier.</param>
        /// <param name="version">The specific version. If null, latest is used.</param>
        /// <returns>NuggetInfo containing loaded assembly information, or null if failed.</returns>
        Task<NuggetInfo> InstallAndLoadNuGetPackageAsync(string packageName, string version = null);

        /// <summary>
        /// Installs a NuGet package without loading it.
        /// </summary>
        /// <param name="packageName">The package identifier.</param>
        /// <param name="version">The specific version. If null, latest is used.</param>
        /// <returns>Installation result.</returns>
        Task<PackageInstallResult> InstallNuGetPackageAsync(string packageName, string version = null);

        /// <summary>
        /// Updates a NuGet package to the latest or specified version.
        /// </summary>
        /// <param name="packageName">The package identifier.</param>
        /// <param name="version">Target version. If null, updates to latest.</param>
        /// <returns>Update result.</returns>
        Task<PackageUpdateResult> UpdateNuGetPackageAsync(string packageName, string version = null);

        /// <summary>
        /// Uninstalls a NuGet package completely.
        /// </summary>
        /// <param name="packageName">The package identifier.</param>
        /// <param name="removeDependencies">If true, removes unused dependencies.</param>
        /// <returns>True if uninstalled successfully; otherwise, false.</returns>
        Task<bool> UninstallNuGetPackageAsync(string packageName, bool removeDependencies = false);

        /// <summary>
        /// Repairs a corrupted or damaged NuGet package.
        /// </summary>
        /// <param name="packageName">The package identifier.</param>
        /// <returns>True if repaired successfully; otherwise, false.</returns>
        Task<bool> RepairNuGetPackageAsync(string packageName);

        /// <summary>
        /// Performs bulk update of all installed NuGet packages.
        /// </summary>
        /// <returns>Bulk update result.</returns>
        Task<BulkUpdateResult> UpdateAllNuGetPackagesAsync();

        /// <summary>
        /// Gets detailed metadata for a NuGet package.
        /// </summary>
        /// <param name="packageName">The package identifier.</param>
        /// <param name="version">The package version.</param>
        /// <returns>Package metadata, or null if not found.</returns>
        Task<PackageMetadata> GetNuGetPackageMetadataAsync(string packageName, string version);

        /// <summary>
        /// Gets dependency tree for a NuGet package.
        /// </summary>
        /// <param name="packageName">The package identifier.</param>
        /// <param name="version">The package version.</param>
        /// <returns>List of package dependencies.</returns>
        Task<List<PackageDependency>> GetNuGetPackageDependenciesAsync(string packageName, string version);

        /// <summary>
        /// Gets all installed NuGet packages.
        /// </summary>
        /// <returns>List of installed package information.</returns>
        Task<List<InstalledPackageInfo>> GetInstalledNuGetPackagesAsync();

        /// <summary>
        /// Checks if a NuGet package is installed.
        /// </summary>
        /// <param name="packageName">The package identifier.</param>
        /// <returns>True if installed; otherwise, false.</returns>
        Task<bool> IsNuGetPackageInstalledAsync(string packageName);

        /// <summary>
        /// Clears the NuGet package cache.
        /// </summary>
        /// <param name="packageName">Optional specific package to clear. If null, clears all.</param>
        /// <param name="version">Optional specific version to clear.</param>
        Task ClearNuGetCacheAsync(string packageName = null, string version = null);

        /// <summary>
        /// Exports installed packages to a JSON file.
        /// </summary>
        /// <param name="filePath">The export file path.</param>
        Task ExportInstalledPackagesAsync(string filePath);

        /// <summary>
        /// Imports packages from a JSON file.
        /// </summary>
        /// <param name="filePath">The import file path.</param>
        Task ImportPackagesAsync(string filePath);

        #endregion
    }
}
