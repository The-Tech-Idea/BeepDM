using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.NuGetManagement
{
    /// <summary>
    /// Configuration options for NuGetPackageManager behavior.
    /// </summary>
    public class NuGetManagerConfig
    {
        /// <summary>
        /// Default installation directory for packages.
        /// </summary>
        public string InstallDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Plugins");
        
        /// <summary>
        /// Path to the global NuGet cache directory.
        /// </summary>
        public string CachePath { get; set; }
        
        /// <summary>
        /// Maximum cache size in megabytes.
        /// </summary>
        public long MaxCacheSizeMB { get; set; } = 1024;
        
        /// <summary>
        /// Whether to enable automatic cache cleanup.
        /// </summary>
        public bool AutoCleanup { get; set; } = true;
        
        /// <summary>
        /// Number of days to keep packages in cache before cleanup.
        /// </summary>
        public int CacheRetentionDays { get; set; } = 30;
        
        /// <summary>
        /// Whether to prefer loading packages into shared context.
        /// </summary>
        public bool PreferSharedContext { get; set; } = true;
        
        /// <summary>
        /// Whether to include prerelease versions in searches and updates.
        /// </summary>
        public bool IncludePrerelease { get; set; } = false;
        
        /// <summary>
        /// Whether to check for package vulnerabilities before installation.
        /// </summary>
        public bool CheckVulnerabilities { get; set; } = true;
        
        /// <summary>
        /// Whether to require signed packages.
        /// </summary>
        public bool RequireSignedPackages { get; set; } = false;
        
        /// <summary>
        /// Whether to allow untrusted certificates during signature verification.
        /// </summary>
        public bool AllowUntrustedCertificates { get; set; } = false;
        
        /// <summary>
        /// Whether to generate lock files after package operations.
        /// </summary>
        public bool AutoGenerateLockFile { get; set; } = false;
        
        /// <summary>
        /// Default timeout for network operations in seconds.
        /// </summary>
        public int NetworkTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Number of retry attempts for failed downloads.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>
        /// List of default NuGet sources.
        /// </summary>
        public List<NuGetSourceSetting> DefaultSources { get; set; } = new List<NuGetSourceSetting>
        {
            new NuGetSourceSetting { Name = "nuget.org", Url = "https://api.nuget.org/v3/index.json", Enabled = true }
        };
    }

    /// <summary>
    /// Represents a NuGet source configuration entry.
    /// </summary>
    public class NuGetSourceSetting
    {
        /// <summary>The source name.</summary>
        public string Name { get; set; }
        /// <summary>The source URL or path.</summary>
        public string Url { get; set; }
        /// <summary>Whether the source is enabled.</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>Source priority (lower is higher priority).</summary>
        public int Priority { get; set; } = 100;
        /// <summary>Username for authenticated feeds.</summary>
        public string Username { get; set; }
        /// <summary>Password for authenticated feeds.</summary>
        public string Password { get; set; }
    }

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
