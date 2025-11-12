using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace TheTechIdea.Beep.Shell.Infrastructure
{
    /// <summary>
    /// Persistent service provider for BeepShell
    /// Unlike CLI which creates new instances per command, this maintains state across the session
    /// </summary>
    public class ShellServiceProvider : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _profileName;
        private bool _disposed = false;

        public string ProfileName => _profileName;
        public string ConfigPath { get; private set; }

        public ShellServiceProvider(string profileName = "default", string? explicitConfigPath = null)
        {
            _profileName = profileName;
            var services = new ServiceCollection();
            
            // Determine config path
            ConfigPath = explicitConfigPath ?? GetProfileConfigPath(profileName);
            EnsureConfigDirectoryExists(ConfigPath);
            
            // Register core services as singletons (these persist for the session)
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
                    ConfigPath,
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
                
                return new AssemblyHandler(
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
        
        /// <summary>
        /// Reload configuration from disk (useful after external changes)
        /// </summary>
        public void ReloadConfiguration()
        {
            var configEditor = GetService<IConfigEditor>();
            // ConfigEditor will reload on next access
        }

        /// <summary>
        /// Switch to a different profile (recreates services)
        /// </summary>
        public ShellServiceProvider SwitchProfile(string newProfileName)
        {
            // Close current connections first
            Dispose();
            
            // Create new provider with new profile
            return new ShellServiceProvider(newProfileName);
        }
        
        private static string GetProfileConfigPath(string profileName)
        {
            // Try environment variable first
            var envPath = Environment.GetEnvironmentVariable("BEEP_CONFIG_PATH");
            if (!string.IsNullOrEmpty(envPath))
                return envPath;

            // Default to executable directory so BeepShell keeps its config alongside the binary
            var exeDir = GetExecutableDirectory();
            if (!string.IsNullOrEmpty(exeDir))
            {
                if (profileName.Equals("default", StringComparison.OrdinalIgnoreCase))
                {
                    return exeDir;
                }

                var profileDir = Path.Combine(exeDir, "Profiles", profileName);
                return profileDir;
            }

            // Check if there's a saved global config location (legacy fallback)
            var savedPath = ReadSavedConfigPath();
            if (!string.IsNullOrEmpty(savedPath) && profileName == "default")
                return savedPath;

            // Default to shell profile location
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TheTechIdea",
                "BeepShell",
                "Profiles",
                profileName
            );
        }
        
        private static string ReadSavedConfigPath()
        {
            // BeepShell ignores the legacy BeepPath.txt file to always use its own exe directory
            // This prevents interference from other Beep applications (like WinForms samples)
            return string.Empty;
        }
        
        private static void EnsureConfigDirectoryExists(string configPath)
        {
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
        }

        private static string GetExecutableDirectory()
        {
            try
            {
                // Prefer AppContext.BaseDirectory for compatibility with single-file publish
                var baseDir = AppContext.BaseDirectory;
                if (!string.IsNullOrWhiteSpace(baseDir))
                {
                    return Path.GetFullPath(baseDir);
                }

                var entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location;
                if (!string.IsNullOrWhiteSpace(entryAssemblyLocation))
                {
                    return Path.GetDirectoryName(entryAssemblyLocation) ?? string.Empty;
                }

                return Directory.GetCurrentDirectory();
            }
            catch
            {
                return string.Empty;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Close all open data sources
                var editor = GetService<IDMEEditor>();
                if (editor?.DataSources != null)
                {
                    foreach (var ds in editor.DataSources.ToList())
                    {
                        try
                        {
                            if (ds.ConnectionStatus == System.Data.ConnectionState.Open)
                            {
                                ds.Closeconnection();
                            }
                            ds.Dispose();
                        }
                        catch
                        {
                            // Ignore disposal errors
                        }
                    }
                }

                // Dispose service provider
                if (_serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
