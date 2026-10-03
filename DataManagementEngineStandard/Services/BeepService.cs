
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Environments;
using TheTechIdea.Beep.JsonLoaderService;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Utils;
using TheTechIdea.Beep.Winform.Controls;





namespace TheTechIdea.Beep.Container.Services
{
    public class BeepService : IBeepService,IDisposable
    {

        // Add these fields for thread-safe initialization
        private readonly object _configLock = new object();
        private readonly object _assembliesLock = new object();
        internal bool RegisterComponentsOnConfigure { get; set; } = true;
        internal bool LoadDefaultMappings { get; set; } = true;
        /// <summary>Set before Configure; the configured graph captures this host-owned policy.</summary>
        public IConnectionSecretProtector ConnectionSecretProtector { get; set; } = TheTechIdea.Beep.Security.ConnectionCredentialProtection.Default;

        public BeepService(IServiceCollection services)
        {
            Services = services;
            // Adding Required Configurations
            // Initialize fields to prevent null reference exceptions
            Environments = new Dictionary<EnvironmentType, IBeepEnvironment>();
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;

        }
        public BeepService()
        {
            // Initialize fields to prevent null reference exceptions
            Environments = new Dictionary<EnvironmentType, IBeepEnvironment>();
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                ConfigureForDesignTime();
            }
        }
        
        bool isDev = false;
        private AssemblyHandlerType _assemblyHandlerType = AssemblyHandlerType.Default;

        /// <summary>
        /// Gets or sets the type of assembly handler to use.
        /// Must be set before calling Configure().
        /// </summary>
        public AssemblyHandlerType AssemblyHandlerType
        {
            get => _assemblyHandlerType;
            set => _assemblyHandlerType = value;
        }

        #region "System Components"
        public IDMEEditor DMEEditor { get; set; }
        public IConfigEditor Config_editor { get; set; }
        public IDMLogger lg { get; set; }
        public IUtil util { get; set; }
        public IErrorsInfo Erinfo { get; set; }
        public IJsonLoader jsonLoader { get; set; }
        public IAssemblyHandler LLoader { get; set; }
        public IServiceCollection Services { get; }
        //public IAppManager vis { get; set; }

        private string _appRepoName;

        /// <summary>
        /// Gets the application repository/container name. This is the preferred property name.
        /// </summary>
        public string AppRepoName 
        { 
            get => _appRepoName; 
            private set => _appRepoName = value; 
        }

        /// <summary>
        /// Gets the container name (legacy property, use AppRepoName instead).
        /// </summary>
        [Obsolete("Use AppRepoName instead. This property will be removed in a future version.", false)]
        public string Containername 
        { 
            get => _appRepoName; 
            private set => _appRepoName = value; 
        }

        public BeepConfigType ConfigureationType { get; private set; }
        public string BeepDirectory { get; private set; }

        CancellationTokenSource tokenSource;
        CancellationToken token;
        private bool disposedValue;
        private int _disposeStarted;
        private bool isconfigloaded = false;
        private bool isassembliesloaded=false;
        private bool isDesignTime;
        #endregion
        public void ConfigureForDesignTime()
        {
            try
            {
                Configure(AppContext.BaseDirectory, "DesignTimeContainer", BeepConfigType.DataConnector, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Design-time configuration failed: {ex.Message}");
            }
        }
        public void Configure(string directorypath, string containername, BeepConfigType configType, bool AddasSingleton = false)
        {
            lock (_configLock)
            {
                ObjectDisposedException.ThrowIf(disposedValue || Volatile.Read(ref _disposeStarted) != 0, this);
                if (DMEEditor != null)
                    throw new InvalidOperationException("This runtime is already configured. Create a new runtime rather than replacing an active graph.");
                try
                {
                    // Store configuration parameters
                    AppRepoName = containername ?? "Beep";
                    ConfigureationType = configType;

                    // Use EnvironmentService for base path if directorypath is null
                    if (string.IsNullOrEmpty(directorypath))
                    {
                        BeepDirectory = EnvironmentService.CreateMainFolder();
                    }
                    else
                    {
                        BeepDirectory = Path.GetFullPath(directorypath);
                    }

                    // Initialize core components
                    Erinfo = new ErrorsInfo();
                    lg = new DMLogger();
                    jsonLoader = new JsonLoader();

                    // Determine root path
                    string root = Path.Combine(BeepDirectory, "Beep");
                    Directory.CreateDirectory(root);

                    // Create core services
                    Config_editor = new ConfigEditor(lg, Erinfo, jsonLoader, root, containername, configType, ConnectionSecretProtector);
                    util = new Util(lg, Erinfo, Config_editor);
                    LLoader = CreateAssemblyHandler(Config_editor, Erinfo, lg, util);
                    DMEEditor = new DMEEditor(lg, util, Erinfo, Config_editor, LLoader);

                    // Wire the catalog-aware connection repository so all connection
                    // CRUD flows through a single source of truth (scope/profile-aware).
                    var catalogRepo = new BeepConnectionRepository(this);
                    Config_editor.ConnectionCatalogRepository = catalogRepo;

                    // Register services if collection provided
                    if (Services != null && RegisterComponentsOnConfigure)
                    {
                        if (AddasSingleton)
                            LoadServicesSingleton();
                        else
                            LoadServicesScoped();
                    }

                    // Initialize arguments
                    DMEEditor.Passedarguments = new PassedArgs();
                    DMEEditor.Passedarguments.Objects = new List<ObjectItem>();

                    DMEEditor.ErrorObject.Flag = Errors.Ok;

                    // Load configurations if not already loaded
                    if (!isconfigloaded)
                    {
                        LoadConfigurations(containername);
                    }
                }
                catch (Exception ex)
                {
                    try { lg?.WriteLog($"BeepService configuration failed: {ex.Message}"); }
                    catch (Exception logError)
                    {
                        // Failed initialization still releases its graph when logging is unavailable.
                        System.Diagnostics.Debug.WriteLine($"Configuration failed: {ex}; logger: {logError}");
                    }
                    Dispose();
                    throw new InvalidOperationException("BeepService configuration failed.", ex);
                }
            }
        }

        /// <summary>
        /// Creates the appropriate assembly handler based on the configured type.
        /// </summary>
        private IAssemblyHandler CreateAssemblyHandler(IConfigEditor configEditor, IErrorsInfo errorObject, IDMLogger logger, IUtil util)
        {
            return _assemblyHandlerType switch
            {
                AssemblyHandlerType.SharedContext => new SharedContextAssemblyHandler(configEditor, errorObject, logger, util),
                _ => new AssemblyHandler(configEditor, errorObject, logger, util)
            };
        }

        public void LoadServicesScoped()
        {
            RegisterRuntimeServices(ServiceLifetime.Scoped);
        }
        public void LoadServicesSingleton()
        {
            RegisterRuntimeServices(ServiceLifetime.Singleton);
        }

        private void RegisterRuntimeServices(ServiceLifetime lifetime)
        {
            if (Services == null) throw new InvalidOperationException("This runtime has no service collection.");
            if (TheTechIdea.Beep.Container.BeepServiceRegistration.HasRuntimeRegistration(Services)) return;
            TheTechIdea.Beep.Container.BeepServiceRegistration.AddBeepRuntime(Services, options =>
            {
                options.DirectoryPath = BeepDirectory;
                options.AppRepoName = AppRepoName;
                options.ConfigType = ConfigureationType;
                options.ServiceLifetime = lifetime;
                options.AssemblyHandlerType = AssemblyHandlerType;
                options.EnableAutoMapping = LoadDefaultMappings;
                options.EnableAssemblyLoading = false;
            });
        }
        public void LoadConfigurations(string AppReponame)
        {
            lock (_configLock)
            {
                if (isconfigloaded)
                    return;

                ObjectDisposedException.ThrowIf(disposedValue || Volatile.Read(ref _disposeStarted) != 0, this);
                if (LoadDefaultMappings)
                {
                    EnvironmentService.AddAllConnectionConfigurations(this.DMEEditor);
                    EnvironmentService.AddAllDataSourceMappings(this.DMEEditor);
                    EnvironmentService.AddAllDataSourceQueryConfigurations(this.DMEEditor);
                }

                isconfigloaded = true;
            }
        }
        public async Task LoadAssembliesAsync(Progress<PassedArgs> progress)
        {
            await Task.Run(() =>
            {
                LoadAssemblies(progress);
            });
        }
        public void LoadAssemblies(Progress<PassedArgs> progress)
        {
            lock (_configLock)
            lock (_assembliesLock)
            {
                ObjectDisposedException.ThrowIf(disposedValue || Volatile.Read(ref _disposeStarted) != 0, this);
                if (isassembliesloaded) return;
                if (LLoader == null || Config_editor == null) throw new InvalidOperationException("Configure the runtime before loading assemblies.");
                LLoader.LoadAllAssembly(progress ?? new Progress<PassedArgs>(), token);
                if (LLoader.Assemblies != null)
                    Config_editor.LoadedAssemblies = LLoader.Assemblies.Select(c => c.DllLib).ToList();
                RestoreDriverConfigAndAutoLoad();
                isassembliesloaded = true;
            }
        }

        public void LoadAssemblies() => LoadAssemblies(new Progress<PassedArgs>());

        private void RestoreDriverConfigAndAutoLoad()
        {
            if (Config_editor == null || LLoader == null)
                return;

            Config_editor.LoadConnectionDriversConfigValues();

            if (Config_editor.DataDriversClasses == null || Config_editor.DataDriversClasses.Count == 0)
                return;

            bool changed = false;
            foreach (var driver in Config_editor.DataDriversClasses.Where(ShouldAutoLoadDriver))
            {
                try
                {
                    if (LLoader.LoadDriverFromLocalPackage(driver, out _))
                    {
                        changed = true;
                        continue;
                    }

                    var hasLocalPackage = LLoader.HasLocalPackage(driver);
                    if (driver.NuggetMissing == hasLocalPackage)
                    {
                        driver.NuggetMissing = !hasLocalPackage;
                        changed = true;
                    }
                }
                catch (Exception ex)
                {
                    lg?.WriteLog($"AutoLoad driver '{driver?.PackageName}' failed: {ex.Message}");
                }
            }

            if (LLoader?.Assemblies != null)
                Config_editor.LoadedAssemblies = LLoader.Assemblies.Select(c => c.DllLib).ToList();

            if (changed)
                Config_editor.SaveConnectionDriversConfigValues();
        }

        private static bool ShouldAutoLoadDriver(ConnectionDriversConfig driver)
        {
            return driver != null
                && driver.AutoLoad
                && !string.IsNullOrWhiteSpace(driver.PackageName)
                && (driver.IsMissing || !driver.NuggetMissing);
        }
        public Dictionary<EnvironmentType, IBeepEnvironment> Environments { get; set; }
        public void LoadEnvironments()
        {
            // Load Environments from IBeepEnvironment in Environments
            ObjectDisposedException.ThrowIf(disposedValue, this);
            
                string envpath = Path.Combine(BeepDirectory, "Environments");
                if (Directory.Exists(envpath))
                {
                    string[] files = Directory.GetFiles(envpath, "*.json");
                    foreach (string file in files)
                    {
                        string json = File.ReadAllText(file);
                        IBeepEnvironment env = jsonLoader.DeserializeSingleObjectFromjsonString<IBeepEnvironment>(json);
                        Environments.Add(env.EnvironmentType, env);
                    }
                }
            

        }
        public void SaveEnvironments()
        {
            // Save Environments from IBeepEnvironment in Environments
            ObjectDisposedException.ThrowIf(disposedValue, this);
            string envpath = Path.Combine(BeepDirectory, "Environments");
            if (Directory.Exists(envpath))
            {
                // save each environment in a json file
                foreach (KeyValuePair<EnvironmentType, IBeepEnvironment> env in Environments)
                {
                    string json = jsonLoader.SerializeObject(env.Value);
                    File.WriteAllText(Path.Combine(envpath, env.Value.EnvironmentName + ".json"), json);
                }
                
            }
        }
        public virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposeStarted, 1) != 0) return;
            if (!disposing) { disposedValue = true; return; }
            var logger = lg;
            void Report(Exception error)
            {
                try { logger?.WriteLog($"Runtime cleanup failed: {error.Message}"); }
                catch (Exception logError) { System.Diagnostics.Debug.WriteLine($"Cleanup: {error}; logger: {logError}"); }
            }
            try { tokenSource?.Cancel(); }
            catch (Exception ex) { Report(ex); }
            lock (_configLock)
            lock (_assembliesLock)
            {
                disposedValue = true;
                var editor = DMEEditor;
                var concreteEditor = editor as TheTechIdea.Beep.DMEEditor;
                var editorConfig = concreteEditor?.ConfigEditor;
                var editorLoader = concreteEditor?.assemblyHandler;
                var released = new HashSet<object>(ReferenceEqualityComparer.Instance);
                void Release(object resource)
                {
                    if (resource is not IDisposable disposable || !released.Add(resource)) return;
                    try { disposable.Dispose(); }
                    catch (Exception ex) { Report(ex); }
                }
                Release(editor);
                // DMEEditor owns these components; don't dispose its graph twice.
                if (editor is not TheTechIdea.Beep.DMEEditor || !ReferenceEquals(editorConfig, Config_editor)) Release(Config_editor);
                if (editor is not TheTechIdea.Beep.DMEEditor || !ReferenceEquals(editorLoader, LLoader)) Release(LLoader);
                Release(util);
                Release(jsonLoader);
                Release(tokenSource);
                DMEEditor = null;
                Config_editor = null;
                LLoader = null;
                util = null;
                jsonLoader = null;
                Erinfo = null;
                tokenSource = null;
                Release(logger);
                lg = null;
            }
        }
        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~BeepService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
