using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Rules;


namespace TheTechIdea.Beep.Editor.Defaults
{
    public static class DefaultsManager 
    {
        private static  IConfigEditor _configEditor;
        private static  IRuleEngine _rulesEditor;
        private static  IDMLogger _logger;
        private static IDMEEditor _editor;



        /// <summary>
        /// Retrieves the default values for a specified data source.
        /// </summary>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <returns>A list of DefaultValue objects.</returns>
        public static List<DefaultValue> GetDefaults(IDMEEditor editor, string dataSourceName)
        {
            _editor = editor;
            _configEditor = _editor.ConfigEditor;
            var connection = _configEditor.DataConnections
                .FirstOrDefault(c => c.ConnectionName.Equals(dataSourceName, StringComparison.InvariantCultureIgnoreCase));

            if (connection == null)
            {
                _logger.WriteLog($"DefaultsManager: Could not find DataSource '{dataSourceName}'.");
                return null;
            }

            return connection.DatasourceDefaults ?? new List<DefaultValue>();
        }

        /// <summary>
        /// Resolves the default value for a specific DefaultValue object.
        /// </summary>
        /// <param name="defaultValue">The DefaultValue object containing the rule or static value.</param>
        /// <param name="parameters">The parameters to pass to the rule, if applicable.</param>
        /// <returns>The resolved value.</returns>
        public static object ResolveDefaultValue(IDMEEditor editor, DefaultValue defaultValue, IPassedArgs parameters)
        {
            _editor = editor;
            _configEditor = _editor.ConfigEditor;
            if (defaultValue == null)
                return null;

            // If there's a Rule, execute it
            if (!string.IsNullOrEmpty(defaultValue.Rule))
            {
                try
                {
                    // Parse and execute the rule
                    //var ruleStructure = _rulesEditor.SolveRule(defaultValue.Rule);
                    //if (ruleStructure != null)
                    //{
                    //    // Pass parameters and execute the rule
                    //    return _rulesEditor.SolveRule(defaultValue.Rule,parameters);
                    //}
                }
                catch (Exception ex)
                {
                    _logger.WriteLog($"DefaultsManager: Error resolving rule '{defaultValue.Rule}'. Exception: {ex.Message}");
                    return null;
                }
            }

            // If no Rule, return the static PropertyValue
            return defaultValue.PropertyValue;
        }

        /// <summary>
        /// Resolves the default value for a given data source and field name.
        /// </summary>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <param name="fieldName">The name of the field to find the default for.</param>
        /// <param name="parameters">The parameters to pass to the rule, if applicable.</param>
        /// <returns>The resolved value.</returns>
        public static object ResolveDefaultValue(IDMEEditor editor, string dataSourceName, string fieldName, IPassedArgs parameters)
        {
            _editor = editor;
            _configEditor = _editor.ConfigEditor;
            // Get defaults for the data source
            var defaults = GetDefaults(editor,dataSourceName);
            if (defaults == null || !defaults.Any())
                return null;

            // Find the DefaultValue for the specified field name
            var defaultValue = defaults.FirstOrDefault(d => d.PropertyName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (defaultValue == null)
                return null;

            // Resolve the value using the other method
            return ResolveDefaultValue(editor,defaultValue, parameters);
        }

        /// <summary>
        /// Saves the default values for a specified data source.
        /// </summary>
        /// <param name="defaults">The default values to save.</param>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <returns>Error information.</returns>
        public static IErrorsInfo SaveDefaults(IDMEEditor editor, List<DefaultValue> defaults, string dataSourceName)
        {
            _editor = editor;
            _configEditor = _editor.ConfigEditor;
            _logger = _editor.Logger;
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

       
    }
}
