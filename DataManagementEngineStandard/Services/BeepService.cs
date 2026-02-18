
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
using TheTechIdea.Beep.Environments;
using TheTechIdea.Beep.JsonLoaderService;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Utils;





namespace TheTechIdea.Beep.Container.Services
{
    public class BeepService : IBeepService,IDisposable
    {

        // Add these fields for thread-safe initialization
        private readonly object _configLock = new object();
        private readonly object _assembliesLock = new object();

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
                    BeepDirectory = directorypath;
                }

                // Initialize core components
                Erinfo = new ErrorsInfo();
                lg = new DMLogger();
                jsonLoader = new JsonLoader();

                // Determine root path
                string root = Path.Combine(BeepDirectory, "Beep");

                // Create core services
                Config_editor = new ConfigEditor(lg, Erinfo, jsonLoader, root, containername, configType);
                util = new Util(lg, Erinfo, Config_editor);
                LLoader = new AssemblyHandler(Config_editor, Erinfo, lg, util);
                DMEEditor = new DMEEditor(lg, util, Erinfo, Config_editor, LLoader);

                // Register services if collection provided
                if (Services != null)
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
                // Create minimal valid state even on error
          

          
                // Log the error
                lg?.WriteLog($"BeepService configuration failed: {ex.Message}");
                Console.WriteLine($"BeepService configuration failed: {ex.Message}");
            }
        }
        public void LoadServicesScoped()
        {
            Services.AddKeyedScoped<IDMLogger, DMLogger>("Logger");
            Services.AddKeyedScoped<IConfigEditor, ConfigEditor>("ConfigEditor");
            Services.AddKeyedScoped<IDMEEditor, DMEEditor>("Editor");
            Services.AddKeyedScoped<IUtil, Util>("Util");
            Services.AddKeyedScoped<IJsonLoader, JsonLoader>("JsonLoader");
            Services.AddKeyedScoped<IAssemblyHandler, AssemblyHandler>("AssemblyHandler");

        }
        public void LoadServicesSingleton()
        {
            Services.AddSingleton<IDMLogger>(lg);
            Services.AddSingleton<IConfigEditor>(Config_editor);
            Services.AddSingleton<IDMEEditor>(DMEEditor);
            Services.AddSingleton<IUtil>(util);
            Services.AddSingleton<IJsonLoader>(jsonLoader);
            Services.AddSingleton<IAssemblyHandler>(LLoader);

        }
        public void LoadConfigurations(string AppReponame)
        {
            lock (_configLock)
            {
                if (isconfigloaded)
                    return;

                EnvironmentService.AddAllConnectionConfigurations(this.DMEEditor);
                EnvironmentService.AddAllDataSourceMappings(this.DMEEditor);
                EnvironmentService.AddAllDataSourceQueryConfigurations(this.DMEEditor);
                EnvironmentService.CreateMainFolder();
                EnvironmentService.CreateAppRepofolder(AppReponame);

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
            if (isassembliesloaded)
            {
                return;
            }
            isassembliesloaded = true;
            LLoader.LoadAllAssembly(progress, token);
            Config_editor.LoadedAssemblies = LLoader.Assemblies.Select(c => c.DllLib).ToList();
        }

        public void LoadAssemblies()
        {
            lock (_assembliesLock)
            {
                if (isassembliesloaded)
                    return;

                Progress<PassedArgs> progress = new Progress<PassedArgs>();
                tokenSource = tokenSource ?? new CancellationTokenSource();
                token = tokenSource.Token;

                LLoader.LoadAllAssembly(progress, token);

                if (Config_editor != null && LLoader?.Assemblies != null)
                    Config_editor.LoadedAssemblies = LLoader.Assemblies.Select(c => c.DllLib).ToList();

                isassembliesloaded = true;
            }
        }
        public Dictionary<EnvironmentType, IBeepEnvironment> Environments { get; set; }
        public void LoadEnvironments()
        {
            // Load Environments from IBeepEnvironment in Environments
            if(string.IsNullOrEmpty(EnvironmentService.AppRepoDataPath))
            {
                EnvironmentService.CreateAppRepofolder(AppRepoName);
            }
            
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
            if (string.IsNullOrEmpty(EnvironmentService.AppRepoDataPath))
            {
                EnvironmentService.CreateAppRepofolder(AppRepoName);
            }
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
        public  virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    // Check each managed object to see if it implements IDisposable, then call Dispose on it.
                    DMEEditor?.Dispose();
                    Config_editor?.Dispose();
                    //lg?.Dispose();
                    //util?.Dispose();
                    //Erinfo?.Dispose();
                    //jsonLoader?.Dispose();
                    LLoader?.Dispose();

                    // If you're using any managed resources that need to be disposed, dispose them here.
                    // For example, if you have a Stream or a SqlConnection, dispose them here.
                    // stream?.Dispose();
                    // sqlConnection?.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override the finalizer below.
                // Set large fields to null.
                DMEEditor = null;
                Config_editor = null;
                lg = null;
                util = null;
                Erinfo = null;
                jsonLoader = null;
                LLoader = null;

                disposedValue = true;
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
