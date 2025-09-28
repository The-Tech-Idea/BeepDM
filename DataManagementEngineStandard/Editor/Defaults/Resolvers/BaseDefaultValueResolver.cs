using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Base resolver class for common functionality across all default value resolvers
    /// </summary>
    public abstract class BaseDefaultValueResolver : IDefaultValueResolver
    {
        protected readonly IDMEEditor Editor;

        protected BaseDefaultValueResolver(IDMEEditor editor)
        {
            Editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        public abstract string ResolverName { get; }
        public abstract IEnumerable<string> SupportedRuleTypes { get; }
        public abstract object ResolveValue(string rule, IPassedArgs parameters);
        public abstract bool CanHandle(string rule);
        public abstract IEnumerable<string> GetExamples();

        /// <summary>
        /// Logs an error message with the resolver context
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="ex">Optional exception</param>
        protected virtual void LogError(string message, Exception ex = null)
        {
            var fullMessage = ex != null ? $"{message}: {ex.Message}" : message;
            Editor.AddLogMessage(ResolverName, fullMessage, DateTime.Now, -1, "", Errors.Failed);
        }

        /// <summary>
        /// Logs an informational message with the resolver context
        /// </summary>
        /// <param name="message">Info message</param>
        protected virtual void LogInfo(string message)
        {
            Editor.AddLogMessage(ResolverName, message, DateTime.Now, -1, "", Errors.Ok);
        }

        /// <summary>
        /// Logs a warning message with the resolver context
        /// </summary>
        /// <param name="message">Warning message</param>
        protected virtual void LogWarning(string message)
        {
            Editor.AddLogMessage(ResolverName, message, DateTime.Now, -1, "", Errors.Warning);
        }

        /// <summary>
        /// Safely extracts a parameter value from IPassedArgs
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <param name="parameters">Parameters object</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Extracted value or default</returns>
        protected virtual T GetParameterValue<T>(IPassedArgs parameters, string propertyName, T defaultValue = default(T))
        {
            if (parameters == null)
                return defaultValue;

            try
            {
                var property = parameters.GetType().GetProperty(propertyName);
                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(parameters);
                    if (value is T directValue)
                        return directValue;
                    
                    // Try to convert if not direct match
                    if (value != null && typeof(T) != typeof(object))
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Error extracting parameter '{propertyName}': {ex.Message}");
            }

            return defaultValue;
        }

        /// <summary>
        /// Extracts content from parentheses in a rule (e.g., "FUNCTION(content)" -> "content")
        /// </summary>
        /// <param name="rule">Rule string</param>
        /// <returns>Content inside parentheses or empty string</returns>
        protected virtual string ExtractParenthesesContent(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return string.Empty;

            var startIndex = rule.IndexOf('(');
            var endIndex = rule.LastIndexOf(')');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                return rule.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Splits comma-separated parameters, respecting quoted strings
        /// </summary>
        /// <param name="parameterString">Parameter string to split</param>
        /// <returns>Array of parameter strings</returns>
        protected virtual string[] SplitParameters(string parameterString)
        {
            if (string.IsNullOrWhiteSpace(parameterString))
                return new string[0];

            var parameters = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;
            var quoteChar = '"';

            for (int i = 0; i < parameterString.Length; i++)
            {
                var ch = parameterString[i];

                if ((ch == '"' || ch == '\'') && !inQuotes)
                {
                    inQuotes = true;
                    quoteChar = ch;
                }
                else if (ch == quoteChar && inQuotes)
                {
                    inQuotes = false;
                }
                else if (ch == ',' && !inQuotes)
                {
                    parameters.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            // Add the last parameter
            if (current.Length > 0)
            {
                parameters.Add(current.ToString().Trim());
            }

            return parameters.ToArray();
        }

        /// <summary>
        /// Removes quotes from a string if present
        /// </summary>
        /// <param name="value">String value</param>
        /// <returns>Unquoted string</returns>
        protected virtual string RemoveQuotes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            value = value.Trim();
            if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }

        /// <summary>
        /// Tries to convert a string value to the specified type
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="value">String value to convert</param>
        /// <param name="result">Converted result</param>
        /// <returns>True if conversion succeeded</returns>
        protected virtual bool TryConvert<T>(string value, out T result)
        {
            result = default(T);

            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                if (typeof(T) == typeof(string))
                {
                    result = (T)(object)value;
                    return true;
                }

                result = (T)Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that a rule has the expected function format (e.g., "FUNCTION(params)")
        /// </summary>
        /// <param name="rule">Rule to validate</param>
        /// <param name="functionName">Expected function name</param>
        /// <returns>True if rule has correct format</returns>
        protected virtual bool ValidateFunctionFormat(string rule, string functionName)
        {
            if (string.IsNullOrWhiteSpace(rule) || string.IsNullOrWhiteSpace(functionName))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();
            var upperFunction = functionName.ToUpperInvariant();

            return upperRule.StartsWith($"{upperFunction}(") && upperRule.EndsWith(")");
        }

        /// <summary>
        /// Gets a data source from parameters
        /// </summary>
        /// <param name="parameters">Parameters object</param>
        /// <param name="dataSourceName">Optional specific data source name</param>
        /// <returns>IDataSource instance or null</returns>
        protected virtual IDataSource GetDataSource(IPassedArgs parameters, string dataSourceName = null)
        {
            // Try to get from parameters first
            var dataSource = GetParameterValue<IDataSource>(parameters, "DataSource");
            if (dataSource != null)
                return dataSource;

            // Try to get by name from editor
            if (!string.IsNullOrWhiteSpace(dataSourceName))
            {
                try
                {
                    return Editor.GetDataSource(dataSourceName);
                }
                catch (Exception ex)
                {
                    LogWarning($"Could not get data source '{dataSourceName}': {ex.Message}");
                }
            }

            // Try to get from parameters by name
            var dsName = GetParameterValue<string>(parameters, "DatasourceName");
            if (!string.IsNullOrWhiteSpace(dsName))
            {
                try
                {
                    return Editor.GetDataSource(dsName);
                }
                catch (Exception ex)
                {
                    LogWarning($"Could not get data source '{dsName}': {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Gets an object from parameters by property name
        /// </summary>
        /// <param name="parameters">Parameters object</param>
        /// <param name="propertyName">Property name to look for</param>
        /// <returns>Object value or null</returns>
        protected virtual object GetObjectFromParameters(IPassedArgs parameters, string propertyName = "Objects")
        {
            if (parameters == null)
                return null;

            // Try direct property access
            var directValue = GetParameterValue<object>(parameters, propertyName);
            if (directValue != null)
                return directValue;

            // Try Objects array
            var objects = GetParameterValue<object[]>(parameters, "Objects");
            if (objects != null && objects.Length > 0)
                return objects[0];

            return null;
        }
    }
}