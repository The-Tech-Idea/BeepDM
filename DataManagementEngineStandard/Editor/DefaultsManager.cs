using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
    public class DefaultsManager : IDisposable
    {
        private readonly IConfigEditor _configEditor;
        private readonly IDMLogger _logger;

        public DefaultsManager(IConfigEditor configEditor, IDMLogger logger)
        {
            _configEditor = configEditor ?? throw new ArgumentNullException(nameof(configEditor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves the default values for a specified data source.
        /// </summary>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <returns>List of DefaultValue objects.</returns>
        public List<DefaultValue> GetDefaults(string dataSourceName)
        {
            try
            {
                var connection = _configEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName.Equals(dataSourceName, StringComparison.InvariantCultureIgnoreCase));

                if (connection == null)
                {
                    _logger.WriteLog($"DefaultsManager: Could not find DataSource '{dataSourceName}'.");
                    return null;
                }

                return connection.DatasourceDefaults ?? new List<DefaultValue>();
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"DefaultsManager: Error retrieving defaults for '{dataSourceName}'. Exception: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves the default values for a specified data source.
        /// </summary>
        /// <param name="defaults">The default values to save.</param>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <returns>Error information.</returns>
        public IErrorsInfo SaveDefaults(List<DefaultValue> defaults, string dataSourceName)
        {
            var errorInfo = new ErrorsInfo();
            try
            {
                var connection = _configEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName.Equals(dataSourceName, StringComparison.InvariantCultureIgnoreCase));

                if (connection == null)
                {
                    errorInfo.Flag = Errors.Failed;
                    errorInfo.Message = $"DefaultsManager: Could not find DataSource '{dataSourceName}'.";
                    _logger.WriteLog(errorInfo.Message);
                    return errorInfo;
                }

                connection.DatasourceDefaults = defaults;
                _configEditor.SaveDataconnectionsValues();

                _logger.WriteLog($"DefaultsManager: Successfully saved defaults for '{dataSourceName}'.");
                return errorInfo;
            }
            catch (Exception ex)
            {
                errorInfo.Flag = Errors.Failed;
                errorInfo.Message = $"DefaultsManager: Error saving defaults for '{dataSourceName}'. Exception: {ex.Message}";
                _logger.WriteLog(errorInfo.Message);
                return errorInfo;
            }
        }

        public void Dispose()
        {
            // Clean up resources if necessary
        }
    }
}
