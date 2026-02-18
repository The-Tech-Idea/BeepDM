using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Handles assembly-related operations such as loading, scanning for extensions, and managing driver configurations.
    /// Core partial class containing fields, properties, and constructor.
    /// </summary>
    public partial class AssemblyHandler : IAssemblyHandler
    {
        #region Private Fields
        private ParentChildObject a;
        private string Name { get; set; }
        private string Descr { get; set; }
        private Dictionary<string, Type> _typeCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private ConcurrentDictionary<string, Assembly> _loadedAssemblyCache = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private List<ConnectionDriversConfig> DataDriversConfig = new List<ConnectionDriversConfig>();
        private bool disposedValue;
        IProgress<PassedArgs> Progress;
        CancellationToken Token;
        /// <summary>
        /// NuGet package manager for loading/unloading nuggets
        /// </summary>
        private NuggetManager _nuggetManager;
        #endregion

        #region Public Properties - IAssemblyHandler Implementation
        
        /// <summary>
        /// List of classes that extend the loader functionality.
        /// </summary>
        public List<AssemblyClassDefinition> LoaderExtensionClasses { get; set; } = new List<AssemblyClassDefinition>();

        /// <summary>
        /// List of loader extension types
        /// </summary>
        public List<Type> LoaderExtensions { get; set; } = new List<Type>();

        /// <summary>
        /// Instantiated loader extension objects that have already been created and executed
        /// </summary>
        public List<ILoaderExtention> LoaderExtensionInstances { get; set; } = new List<ILoaderExtention>();

        /// <summary>
        /// List of namespaces to ignore during scanning
        /// </summary>
        public List<string> NamespacestoIgnore { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the current domain in which the assembly is executed.
        /// </summary>
        public AppDomain CurrentDomain { get; set; }

        /// <summary>
        /// Error handling object.
        /// </summary>
        public IErrorsInfo ErrorObject { get; set; }

        /// <summary>
        /// Logging interface for tracking activities and errors.
        /// </summary>
        public IDMLogger Logger { get; set; }

        /// <summary>
        /// Utility functions for assembly handling.
        /// </summary>
        public IUtil Utilfunction { get; set; }

        /// <summary>
        /// Interface for configuration editing.
        /// </summary>
        public IConfigEditor ConfigEditor { get; set; }

        /// <summary>
        /// List of assemblies loaded or referenced.
        /// </summary>
        public List<assemblies_rep> Assemblies { get; set; } = new List<assemblies_rep>();

        /// <summary>
        /// List of classes that represent data sources.
        /// </summary>
        public List<AssemblyClassDefinition> DataSourcesClasses { get; set; } = new List<AssemblyClassDefinition>();

        /// <summary>
        /// List of loaded assemblies
        /// </summary>
        public List<Assembly> LoadedAssemblies { get; set; } = new List<Assembly>();

        #endregion

        #region Constructor
        
        /// <summary>
        /// Constructor for AssemblyHandler, initializes necessary properties.
        /// </summary>
        /// <param name="pConfigEditor">Configuration editor.</param>
        /// <param name="pErrorObject">Error handling object.</param>
        /// <param name="pLogger">Logging interface.</param>
        /// <param name="pUtilfunction">Utility functions.</param>
        public AssemblyHandler(IConfigEditor pConfigEditor, IErrorsInfo pErrorObject, IDMLogger pLogger, IUtil pUtilfunction)
        {
            ConfigEditor = pConfigEditor;
            ErrorObject = pErrorObject;
            Logger = pLogger;
            Utilfunction = pUtilfunction;
            CurrentDomain = AppDomain.CurrentDomain;
            DataSourcesClasses = new List<AssemblyClassDefinition>();
            CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            
            // Initialize NuggetManager
            _nuggetManager = new NuggetManager(Logger, ErrorObject, Utilfunction);
            
            // Initialize loaded assemblies from dependency context
            InitializeLoadedAssemblies();
        }

        #endregion

        #region Assembly Resolution

        /// <summary>
        /// Handles assembly resolution when the runtime cannot find an assembly
        /// </summary>
        public Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var assemblyName = new AssemblyName(args.Name);
                
                // Check loaded assemblies first
                foreach (var assembly in LoadedAssemblies)
                {
                    if (assembly.GetName().Name.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return assembly;
                    }
                }

                // Check assembly cache
                foreach (var kvp in _loadedAssemblyCache)
                {
                    if (kvp.Value.GetName().Name.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Value;
                    }
                }

                Logger?.WriteLog($"CurrentDomain_AssemblyResolve: Could not resolve assembly '{args.Name}'");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"CurrentDomain_AssemblyResolve: Error - {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Type Cache Management

        /// <summary>
        /// Add a type to the type cache for fast lookups
        /// </summary>
        public void AddTypeToCache(string fullName, Type type)
        {
            if (!string.IsNullOrWhiteSpace(fullName) && type != null)
            {
                _typeCache[fullName] = type;
            }
        }

        /// <summary>
        /// Get a type from the cache
        /// </summary>
        public Type GetTypeFromCache(string fullName)
        {
            if (_typeCache.TryGetValue(fullName, out var type))
            {
                return type;
            }
            return null;
        }

        /// <summary>
        /// Clear the type cache
        /// </summary>
        public void ClearTypeCache()
        {
            _typeCache.Clear();
        }

        #endregion

        #region Dispose Pattern

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">Indicates whether the method call comes from a Dispose method (its value is true) or from a finalizer (its value is false).</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    _nuggetManager?.Clear();
                    LoaderExtensions = null;
                    LoaderExtensionClasses = null;
                    Assemblies = null;
                    DataSourcesClasses = null;
                    DataDriversConfig = null;
                    _typeCache?.Clear();
                    _loadedAssemblyCache?.Clear();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
