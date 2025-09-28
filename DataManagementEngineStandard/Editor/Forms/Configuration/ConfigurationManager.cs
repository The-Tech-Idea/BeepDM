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
        public UnitofWorksManagerConfiguration Configuration { get; set; }
        #endregion

        #region Constructor
        public ConfigurationManager(string configFilePath = null)
        {
            _configFilePath = configFilePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DefaultConfigFileName);
            Configuration = UnitofWorksManagerConfiguration.Default;
        }
        #endregion

        #region Public Methods

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

        public void ResetToDefaults()
        {
            Configuration = UnitofWorksManagerConfiguration.Default;
            SaveConfiguration();
        }

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