using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Defaults.Helpers
{
    /// <summary>
    /// Helper class for managing default value operations
    /// </summary>
    public class DefaultValueHelper : IDefaultValueHelper
    {
        private readonly IDMEEditor _editor;

        public DefaultValueHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        public List<DefaultValue> GetDefaults(string dataSourceName)
        {
            try
            {
                var connection = _editor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName.Equals(dataSourceName, StringComparison.InvariantCultureIgnoreCase));

                if (connection == null)
                {
                    _editor.AddLogMessage("DefaultValueHelper", $"Could not find DataSource '{dataSourceName}'.", DateTime.Now, -1, "", Errors.Failed);
                    return new List<DefaultValue>();
                }

                return connection.DatasourceDefaults ?? new List<DefaultValue>();
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("DefaultValueHelper", $"Error getting defaults for '{dataSourceName}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return new List<DefaultValue>();
            }
        }

        public IErrorsInfo SaveDefaults(List<DefaultValue> defaults, string dataSourceName)
        {
            var errorInfo = new ErrorsInfo();
            try
            {
                var connection = _editor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName.Equals(dataSourceName, StringComparison.InvariantCultureIgnoreCase));

                if (connection == null)
                {
                    errorInfo.Flag = Errors.Failed;
                    errorInfo.Message = $"Could not find DataSource '{dataSourceName}'.";
                    _editor.AddLogMessage("DefaultValueHelper", errorInfo.Message, DateTime.Now, -1, "", Errors.Failed);
                    return errorInfo;
                }

                connection.DatasourceDefaults = defaults ?? new List<DefaultValue>();
                _editor.ConfigEditor.SaveDataconnectionsValues();

                errorInfo.Flag = Errors.Ok;
                errorInfo.Message = $"Successfully saved {defaults?.Count ?? 0} defaults for '{dataSourceName}'.";
                _editor.AddLogMessage("DefaultValueHelper", errorInfo.Message, DateTime.Now, -1, "", Errors.Ok);
                return errorInfo;
            }
            catch (Exception ex)
            {
                errorInfo.Flag = Errors.Failed;
                errorInfo.Message = $"Error saving defaults for '{dataSourceName}': {ex.Message}";
                _editor.AddLogMessage("DefaultValueHelper", errorInfo.Message, DateTime.Now, -1, "", Errors.Failed);
                return errorInfo;
            }
        }

        public DefaultValue GetDefaultForField(string dataSourceName, string fieldName)
        {
            var defaults = GetDefaults(dataSourceName);
            return defaults.FirstOrDefault(d => 
                d.PropertyName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        }

        public DefaultValue CreateDefaultValue(string fieldName, string value, string rule = null)
        {
            var defaultValue = new DefaultValue
            {
                PropertyName = fieldName,
                PropertyValue = value, // Now correctly assigns string to object
                Rule = rule,
                PropertyCategory = "Default",
                Description = $"Default value for {fieldName}",
                IsEnabled = true
            };

            // Set the appropriate property type based on whether we have a rule or static value
            if (!string.IsNullOrEmpty(rule))
            {
                defaultValue.propertyType = DetermineRuleType(rule);
            }
            else if (!string.IsNullOrEmpty(value))
            {
                defaultValue.propertyType = DefaultValueType.Static;
            }
            else
            {
                defaultValue.propertyType = DefaultValueType.ReplaceValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Determines the appropriate DefaultValueType based on the rule content
        /// </summary>
        /// <param name="rule">The rule string to analyze</param>
        /// <returns>The appropriate DefaultValueType</returns>
        private DefaultValueType DetermineRuleType(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return DefaultValueType.Static;

            var lowerRule = rule.ToLowerInvariant();

            // Check for specific rule patterns and return appropriate types
            if (lowerRule.Contains("currentdatetime") || lowerRule.Contains("now"))
                return DefaultValueType.CurrentDateTime;
            if (lowerRule.Contains("currentuser") || lowerRule.Contains("username"))
                return DefaultValueType.CurrentUser;
            if (lowerRule.Contains("generateuniqueid") || lowerRule.Contains("newguid"))
                return DefaultValueType.GenerateUniqueId;
            if (lowerRule.Contains("autoincrement") || lowerRule.Contains("sequence"))
                return DefaultValueType.AutoIncrement;
            if (lowerRule.Contains("configurationvalue") || lowerRule.Contains("appsetting"))
                return DefaultValueType.ConfigurationValue;
            if (lowerRule.Contains("environmentvariable") || lowerRule.Contains("env:"))
                return DefaultValueType.EnvironmentVariable;
            if (lowerRule.Contains("sessionvalue") || lowerRule.Contains("session:"))
                return DefaultValueType.SessionValue;
            if (lowerRule.Contains("entitylookup") || lowerRule.Contains("lookup:"))
                return DefaultValueType.EntityLookup;
            if (lowerRule.Contains("webservice") || lowerRule.Contains("api:"))
                return DefaultValueType.WebService;
            if (lowerRule.Contains("storedprocedure") || lowerRule.Contains("sp:"))
                return DefaultValueType.StoredProcedure;
            if (lowerRule.Contains("randomvalue") || lowerRule.Contains("random"))
                return DefaultValueType.RandomValue;
            if (lowerRule.Contains("rolebasedvalue") || lowerRule.Contains("role:"))
                return DefaultValueType.RoleBasedValue;
            if (lowerRule.Contains("localizedvalue") || lowerRule.Contains("localized:"))
                return DefaultValueType.LocalizedValue;
            if (lowerRule.Contains("businesscalendar") || lowerRule.Contains("businessday"))
                return DefaultValueType.BusinessCalendar;
            if (lowerRule.Contains("userpreference") || lowerRule.Contains("preference:"))
                return DefaultValueType.UserPreference;
            if (lowerRule.Contains("locationbased") || lowerRule.Contains("location:"))
                return DefaultValueType.LocationBased;
            if (lowerRule.Contains("mlprediction") || lowerRule.Contains("predict:"))
                return DefaultValueType.MLPrediction;
            if (lowerRule.Contains("statistical") || lowerRule.Contains("stats:"))
                return DefaultValueType.Statistical;
            if (lowerRule.Contains("workflowcontext") || lowerRule.Contains("workflow:"))
                return DefaultValueType.WorkflowContext;
            if (lowerRule.Contains("parentvalue") || lowerRule.Contains("parent:"))
                return DefaultValueType.ParentValue;
            if (lowerRule.Contains("childvalue") || lowerRule.Contains("child:"))
                return DefaultValueType.ChildValue;
            if (lowerRule.Contains("cachedvalue") || lowerRule.Contains("cache:"))
                return DefaultValueType.CachedValue;
            if (lowerRule.Contains("auditvalue") || lowerRule.Contains("audit:"))
                return DefaultValueType.AuditValue;
            if (lowerRule.Contains("inheritedvalue") || lowerRule.Contains("inherit:"))
                return DefaultValueType.InheritedValue;
            if (lowerRule.Contains("template:") || lowerRule.Contains("from template"))
                return DefaultValueType.Template;
            if (lowerRule.Contains("computed") || lowerRule.Contains("calculate"))
                return DefaultValueType.Computed;
            if (lowerRule.Contains("conditional") || lowerRule.Contains("if "))
                return DefaultValueType.Conditional;
            if (lowerRule.Contains("mapping") || lowerRule.Contains("map:"))
                return DefaultValueType.Mapping;
            if (lowerRule.Contains("function(") || lowerRule.Contains("call "))
                return DefaultValueType.Function;
            if (lowerRule.Contains("expression") || ContainsExpression(lowerRule))
                return DefaultValueType.Expression;

            // Default to Rule type if no specific pattern is found
            return DefaultValueType.Rule;
        }

        /// <summary>
        /// Checks if the rule contains mathematical or logical expressions
        /// </summary>
        /// <param name="rule">The rule to check</param>
        /// <returns>True if it contains expression patterns</returns>
        private bool ContainsExpression(string rule)
        {
            var expressionIndicators = new[] { "+", "-", "*", "/", "=", "<", ">", "&&", "||", "math.", "string." };
            return expressionIndicators.Any(indicator => rule.Contains(indicator));
        }

        public IErrorsInfo ValidateDefaultValue(DefaultValue defaultValue)
        {
            var errorInfo = new ErrorsInfo { Flag = Errors.Ok };

            if (defaultValue == null)
            {
                errorInfo.Flag = Errors.Failed;
                errorInfo.Message = "Default value cannot be null";
                return errorInfo;
            }

            if (string.IsNullOrWhiteSpace(defaultValue.PropertyName))
            {
                errorInfo.Flag = Errors.Failed;
                errorInfo.Message = "Property name cannot be empty";
                return errorInfo;
            }

            // Enhanced validation logic
            var hasStaticValue = defaultValue.PropertyValue != null && 
                                 !IsEmptyValue(defaultValue.PropertyValue);
            var hasRule = !string.IsNullOrWhiteSpace(defaultValue.Rule);

            if (!hasStaticValue && !hasRule)
            {
                errorInfo.Flag = Errors.Failed;
                errorInfo.Message = "Either property value or rule must be specified";
                return errorInfo;
            }

            // Validate rule-based defaults
            if (hasRule)
            {
                var ruleValidation = ValidateRule(defaultValue.Rule, defaultValue.propertyType);
                if (ruleValidation.Flag != Errors.Ok)
                {
                    errorInfo.Flag = ruleValidation.Flag;
                    errorInfo.Message = $"Rule validation failed: {ruleValidation.Message}";
                    return errorInfo;
                }
            }

            // Validate property type consistency
            if (hasRule && hasStaticValue)
            {
                // If both rule and static value are present, rule should take precedence
                errorInfo.Flag = Errors.Warning;
                errorInfo.Message = "Both rule and static value are specified. Rule will take precedence.";
            }

            // Validate property type against rule content
            if (hasRule)
            {
                var expectedType = DetermineRuleType(defaultValue.Rule);
                if (defaultValue.propertyType != expectedType && defaultValue.propertyType != DefaultValueType.Rule)
                {
                    errorInfo.Flag = Errors.Warning;
                    errorInfo.Message = $"Property type '{defaultValue.propertyType}' may not match rule content (expected '{expectedType}')";
                }
            }

            return errorInfo;
        }

        /// <summary>
        /// Validates a rule string for syntax and structure
        /// </summary>
        /// <param name="rule">The rule to validate</param>
        /// <param name="ruleType">The expected rule type</param>
        /// <returns>Validation result</returns>
        private IErrorsInfo ValidateRule(string rule, DefaultValueType ruleType)
        {
            var errorInfo = new ErrorsInfo { Flag = Errors.Ok };

            if (string.IsNullOrWhiteSpace(rule))
            {
                errorInfo.Flag = Errors.Failed;
                errorInfo.Message = "Rule cannot be empty";
                return errorInfo;
            }

            // Basic syntax validation
            if (rule.Trim().Length != rule.Length)
            {
                errorInfo.Flag = Errors.Warning;
                errorInfo.Message = "Rule has leading or trailing whitespace";
            }

            // Validate specific rule patterns based on type
            switch (ruleType)
            {
                case DefaultValueType.ConfigurationValue:
                    if (!rule.Contains(":"))
                    {
                        errorInfo.Flag = Errors.Warning;
                        errorInfo.Message = "Configuration value rules should specify the setting name (e.g., 'ConfigurationValue:SettingName')";
                    }
                    break;

                case DefaultValueType.EntityLookup:
                    if (!rule.Contains(":"))
                    {
                        errorInfo.Flag = Errors.Warning;
                        errorInfo.Message = "Entity lookup rules should specify the lookup target (e.g., 'EntityLookup:TableName.FieldName')";
                    }
                    break;

                case DefaultValueType.Function:
                    if (!rule.Contains("(") || !rule.Contains(")"))
                    {
                        errorInfo.Flag = Errors.Warning;
                        errorInfo.Message = "Function rules should include parentheses for parameters";
                    }
                    break;

                case DefaultValueType.Expression:
                    if (!ContainsExpression(rule))
                    {
                        errorInfo.Flag = Errors.Warning;
                        errorInfo.Message = "Expression rules should contain mathematical or logical operators";
                    }
                    break;
            }

            return errorInfo;
        }

        /// <summary>
        /// Checks if a value is considered empty for its type
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>True if the value is considered empty</returns>
        private bool IsEmptyValue(object value)
        {
            if (value == null)
                return true;

            if (value is string stringValue)
                return string.IsNullOrWhiteSpace(stringValue);

            if (value is Array arrayValue)
                return arrayValue.Length == 0;

            if (value.GetType().IsValueType)
                return value.Equals(Activator.CreateInstance(value.GetType()));

            return false;
        }

        /// <summary>
        /// Creates a default value with enhanced type detection and validation
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="value">Static value (can be any type)</param>
        /// <param name="rule">Rule string</param>
        /// <param name="description">Optional description</param>
        /// <returns>Configured DefaultValue instance</returns>
        public DefaultValue CreateDefaultValue(string fieldName, object value, string rule = null, string description = null)
        {
            var defaultValue = new DefaultValue
            {
                PropertyName = fieldName,
                PropertyValue = value,
                Rule = rule,
                PropertyCategory = "Default",
                Description = description ?? $"Default value for {fieldName}",
                IsEnabled = true
            };

            // Set the appropriate property type
            if (!string.IsNullOrEmpty(rule))
            {
                defaultValue.propertyType = DetermineRuleType(rule);
            }
            else if (value != null && !IsEmptyValue(value))
            {
                defaultValue.propertyType = DefaultValueType.Static;
            }
            else
            {
                defaultValue.propertyType = DefaultValueType.ReplaceValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets all defaults for multiple data sources
        /// </summary>
        /// <param name="dataSourceNames">Names of data sources</param>
        /// <returns>Dictionary mapping data source names to their defaults</returns>
        public Dictionary<string, List<DefaultValue>> GetMultipleDefaults(IEnumerable<string> dataSourceNames)
        {
            var result = new Dictionary<string, List<DefaultValue>>();
            
            foreach (var dataSourceName in dataSourceNames)
            {
                try
                {
                    result[dataSourceName] = GetDefaults(dataSourceName);
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("DefaultValueHelper", $"Error getting defaults for '{dataSourceName}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    result[dataSourceName] = new List<DefaultValue>();
                }
            }

            return result;
        }

        /// <summary>
        /// Searches for defaults by property name across all data sources
        /// </summary>
        /// <param name="propertyName">Name of the property to search for</param>
        /// <returns>Dictionary mapping data source names to matching defaults</returns>
        public Dictionary<string, List<DefaultValue>> FindDefaultsByPropertyName(string propertyName)
        {
            var result = new Dictionary<string, List<DefaultValue>>();
            
            try
            {
                var connections = _editor.ConfigEditor.DataConnections ?? new List<ConnectionProperties>();
                
                foreach (var connection in connections)
                {
                    if (connection.DatasourceDefaults != null)
                    {
                        var matchingDefaults = connection.DatasourceDefaults
                            .Where(d => d.PropertyName.Contains(propertyName, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (matchingDefaults.Any())
                        {
                            result[connection.ConnectionName] = matchingDefaults;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("DefaultValueHelper", $"Error searching for property '{propertyName}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }

            return result;
        }
    }
}