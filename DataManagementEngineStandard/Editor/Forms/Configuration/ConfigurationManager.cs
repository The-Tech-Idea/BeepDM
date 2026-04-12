using System;
using System.IO;
using System.Text.Json;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager.Configuration
{
    /// <summary>
    /// Configuration manager for UnitofWorksManager
    /// </summary>
    public class ConfigurationManager : IConfigurationManager
    {
        #region Fields
        private const string DefaultConfigFileName = "UnitofWorksManager.config.json";
        private string _configFilePath;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the currently loaded FormsManager configuration instance.
        /// </summary>
        public UnitofWorksManagerConfiguration Configuration { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a configuration manager that loads from the supplied file path or the default application-data path.
        /// </summary>
        /// <param name="configFilePath">Optional explicit configuration file path.</param>
        public ConfigurationManager(string configFilePath = null)
        {
            _configFilePath = configFilePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DefaultConfigFileName);
            Configuration = UnitofWorksManagerConfiguration.Default;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Loads configuration from disk, falling back to defaults when no file exists or deserialization fails.
        /// </summary>
        public void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<UnitofWorksManagerConfiguration>(json);
                    Configuration = config ?? UnitofWorksManagerConfiguration.Default;
                }
                else
                {
                    Configuration = UnitofWorksManagerConfiguration.Default;
                    SaveConfiguration(); // Create default config file
                }
            }
            catch (Exception)
            {
                Configuration = UnitofWorksManagerConfiguration.Default;
            }
        }

        /// <summary>
        /// Persists the current configuration to disk.
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                var json = JsonSerializer.Serialize(Configuration, new JsonSerializerOptions { WriteIndented = true });
                var directory = Path.GetDirectoryName(_configFilePath);
                
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception)
            {
                // Log error but don't throw
            }
        }

        /// <summary>
        /// Resets the in-memory configuration to defaults and immediately saves it.
        /// </summary>
        public void ResetToDefaults()
        {
            Configuration = UnitofWorksManagerConfiguration.Default;
            SaveConfiguration();
        }

        /// <summary>
        /// Validates that the required top-level configuration sections are present.
        /// </summary>
        /// <returns><c>true</c> when the current configuration contains the required sections; otherwise <c>false</c>.</returns>
        public bool ValidateConfiguration()
        {
            return Configuration != null &&
                   Configuration.Performance != null &&
                   Configuration.Validation != null &&
                   Configuration.Navigation != null;
        }

        #endregion
    }
}