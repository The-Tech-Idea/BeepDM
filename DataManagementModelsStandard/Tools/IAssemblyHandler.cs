
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

using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    public interface IAssemblyHandler:IDisposable
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

        #endregion

        #region Statistics

        /// <summary>
        /// Gets assembly loading statistics.
        /// </summary>
        AssemblyLoadStatistics GetLoadStatistics();

        #endregion
    }
}
