using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.NuGetManagement
{
    /// <summary>
    /// Manages loading and saving NuGetPackageManager configuration.
    /// </summary>
    public class NuGetManagerConfiguration
    {
        private readonly IDMLogger _logger;
        private readonly string _configPath;

        /// <summary>
        /// Initializes a new instance of NuGetManagerConfiguration.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="configDirectory">Optional directory for configuration file.</param>
        public NuGetManagerConfiguration(IDMLogger logger, string configDirectory = null)
        {
            _logger = logger;
            _configPath = Path.Combine(configDirectory ?? AppContext.BaseDirectory, "nuget_manager.config.json");
        }

        /// <summary>
        /// Loads configuration from disk or creates default if not exists.
        /// </summary>
        /// <returns>The loaded or default configuration.</returns>
        public NuGetManagerConfig Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonConvert.DeserializeObject<NuGetManagerConfig>(json);
                    if (config != null)
                    {
                        _logger?.LogWithContext($"Loaded NuGet manager configuration from {_configPath}", null);
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error loading configuration from {_configPath}: {ex.Message}. Using defaults.", ex);
            }

            // Return default configuration
            var defaultConfig = new NuGetManagerConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        /// <summary>
        /// Saves configuration to disk.
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        public void Save(NuGetManagerConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                _logger?.LogWithContext($"Saved NuGet manager configuration to {_configPath}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error saving configuration to {_configPath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the configuration file path.
        /// </summary>
        /// <returns>The path to the configuration file.</returns>
        public string GetConfigPath()
        {
            return _configPath;
        }
    }
}
