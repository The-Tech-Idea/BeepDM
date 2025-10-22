using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Utils;

namespace TheTechIdea.Beep.CLI.Infrastructure
{
    /// <summary>
    /// Service provider for dependency injection in CLI
    /// </summary>
    public class BeepServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;
        
        public BeepServiceProvider(string profileName = "default", string? explicitConfigPath = null)
        {
            var services = new ServiceCollection();
            
            // Determine config path
            string configPath = explicitConfigPath ?? GetProfileConfigPath(profileName);
            EnsureConfigDirectoryExists(configPath);
            
            // Register core services as singletons
            services.AddSingleton<IDMLogger>(sp => new DMLogger());
            services.AddSingleton<IErrorsInfo>(sp => new ErrorsInfo());
            services.AddSingleton<IJsonLoader>(sp => new JsonLoader());
            
            // Register Util with resolved dependencies
            services.AddSingleton<IUtil>(sp =>
            {
                var logger = sp.GetRequiredService<IDMLogger>();
                var errorInfo = sp.GetRequiredService<IErrorsInfo>();
                var configEditor = sp.GetRequiredService<IConfigEditor>();
                
                return new Util(logger, errorInfo, configEditor);
            });
            
            // Register ConfigEditor with resolved dependencies
            services.AddSingleton<IConfigEditor>(sp =>
            {
                var logger = sp.GetRequiredService<IDMLogger>();
                var errorInfo = sp.GetRequiredService<IErrorsInfo>();
                var jsonLoader = sp.GetRequiredService<IJsonLoader>();
                
                return new ConfigEditor(
                    logger,
                    errorInfo,
                    jsonLoader,
                    configPath,
                    null,
                    BeepConfigType.Application
                );
            });
            
            // Register AssemblyHandler with resolved dependencies
            services.AddSingleton<IAssemblyHandler>(sp =>
            {
                var configEditor = sp.GetRequiredService<IConfigEditor>();
                var errorInfo = sp.GetRequiredService<IErrorsInfo>();
                var logger = sp.GetRequiredService<IDMLogger>();
                var util = sp.GetRequiredService<IUtil>();
                
                return new SharedContextAssemblyHandler(
                    configEditor,
                    errorInfo,
                    logger,
                    util
                );
            });
            
            // Register DMEEditor with all dependencies
            services.AddSingleton<IDMEEditor>(sp =>
            {
                var logger = sp.GetRequiredService<IDMLogger>();
                var util = sp.GetRequiredService<IUtil>();
                var errorInfo = sp.GetRequiredService<IErrorsInfo>();
                var configEditor = sp.GetRequiredService<IConfigEditor>();
                var assemblyHandler = sp.GetRequiredService<IAssemblyHandler>();
                
                var editor = new DMEEditor(
                    logger,
                    util,
                    errorInfo,
                    configEditor,
                    assemblyHandler
                );
                
                // Load assemblies on startup
                try
                {
                    editor.assemblyHandler.LoadAllAssembly(null, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.WriteLog($"Warning: Some assemblies failed to load: {ex.Message}");
                }
                
                return editor;
            });
            
            _serviceProvider = services.BuildServiceProvider();
        }
        
        public T GetService<T>() where T : notnull
        {
            return _serviceProvider.GetRequiredService<T>();
        }
        
        public IDMEEditor GetEditor() => GetService<IDMEEditor>();
        
        private static string GetProfileConfigPath(string profileName)
        {
            // Try environment variable first
            var envPath = Environment.GetEnvironmentVariable("BEEP_CONFIG_PATH");
            if (!string.IsNullOrEmpty(envPath))
                return envPath;

            // Check if there's a saved global config location
            var savedPath = ReadSavedConfigPath();
            if (!string.IsNullOrEmpty(savedPath) && profileName == "default")
                return savedPath;

            // Default to CLI profile location
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TheTechIdea",
                "BeepCLI",
                "Profiles",
                profileName
            );
        }
        
        private static string ReadSavedConfigPath()
        {
            try
            {
                var beepPathFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "TheTechIdea",
                    "Beep",
                    "BeepPath.txt"
                );

                if (File.Exists(beepPathFile))
                    return File.ReadAllText(beepPathFile).Trim();
            }
            catch
            {
                // Ignore errors
            }

            return string.Empty;
        }
        
        private static void EnsureConfigDirectoryExists(string configPath)
        {
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
        }
    }
}
