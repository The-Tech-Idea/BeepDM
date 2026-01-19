using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;
using TheTechIdea.Beep.Editor.Defaults.Helpers;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;

namespace TheTechIdea.Beep.Editor.Defaults
{
    /// <summary>
    /// Enhanced DefaultsManager with helper-based architecture and extensible resolvers
    /// Supports setting default values for entity columns with simple logic formulas
    /// </summary>
    public partial class DefaultsManager : IDisposable, IDefaultsManager
    {
        #region Private Fields

        protected static IConfigEditor _configEditor;
        protected static IDMLogger _logger;
        protected static IDMEEditor _editor;

        // Helper instances
        protected static IDefaultValueHelper _defaultValueHelper;
        protected static IDefaultValueResolverManager _resolverManager;
        protected static IDefaultValueValidationHelper _validationHelper;

        protected static bool _initialized = false;
        protected static readonly object _lockObject = new object();

        #endregion

        #region IDefaultsManager Interface Properties

        /// <summary>
        /// Gets the default value helper instance
        /// </summary>
        public static IDefaultValueHelper DefaultValueHelper
        {
            get
            {
                if (!_initialized)
                    throw new InvalidOperationException("DefaultsManager must be initialized before accessing DefaultValueHelper");
                return _defaultValueHelper;
            }
        }

        /// <summary>
        /// Gets the resolver manager instance
        /// </summary>
        public static IDefaultValueResolverManager ResolverManager
        {
            get
            {
                if (!_initialized)
                    throw new InvalidOperationException("DefaultsManager must be initialized before accessing ResolverManager");
                return _resolverManager;
            }
        }

        /// <summary>
        /// Gets the validation helper instance
        /// </summary>
        public static IDefaultValueValidationHelper ValidationHelper
        {
            get
            {
                if (!_initialized)
                    throw new InvalidOperationException("DefaultsManager must be initialized before accessing ValidationHelper");
                return _validationHelper;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the DefaultsManager with helper instances
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        public static void Initialize(IDMEEditor editor)
        {
            lock (_lockObject)
            {
                if (_initialized && _editor == editor)
                    return;

                _editor = editor ?? throw new ArgumentNullException(nameof(editor));
                _configEditor = _editor.ConfigEditor;
                _logger = _editor.Logger;

                // Initialize helpers
                _defaultValueHelper = new DefaultValueHelper(_editor);
                _resolverManager = new DefaultValueResolverManager(_editor);
                _validationHelper = new DefaultValueValidationHelper(_editor);

                _initialized = true;

                _logger?.WriteLog("DefaultsManager initialized successfully with helper-based architecture");
            }
        }

        /// <summary>
        /// Ensures the manager is initialized
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        protected static void EnsureInitialized(IDMEEditor editor)
        {
            if (!_initialized || _editor != editor)
            {
                Initialize(editor);
            }
        }

        #endregion

        #region Core Public API - Backward Compatible

        /// <summary>
        /// Retrieves the default values for a specified data source.
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <returns>A list of DefaultValue objects.</returns>
        public static List<DefaultValue> GetDefaults(IDMEEditor editor, string dataSourceName)
        {
            EnsureInitialized(editor);
            return _defaultValueHelper.GetDefaults(dataSourceName);
        }

        /// <summary>
        /// Resolves the default value for a specific DefaultValue object using enhanced resolver system.
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="defaultValue">The DefaultValue object containing the rule or static value.</param>
        /// <param name="parameters">The parameters to pass to the rule, if applicable.</param>
        /// <returns>The resolved value.</returns>
        public static object ResolveDefaultValue(IDMEEditor editor, DefaultValue defaultValue, IPassedArgs parameters)
        {
            EnsureInitialized(editor);

            if (defaultValue == null)
                return null;

            try
            {
                // If there's a Rule, execute it using the resolver manager
                if (!string.IsNullOrEmpty(defaultValue.Rule))
                {
                    var resolvedValue = _resolverManager.ResolveValue(defaultValue.Rule, parameters);
                    if (resolvedValue != null)
                    {
                        _logger?.WriteLog($"DefaultsManager: Successfully resolved rule '{defaultValue.Rule}' to '{resolvedValue}'");
                        return resolvedValue;
                    }
                    else
                    {
                        _logger?.WriteLog($"DefaultsManager: Could not resolve rule '{defaultValue.Rule}', using static value");
                    }
                }

                // If no Rule or rule resolution failed, return the static PropertyValue
                return defaultValue.PropertyValue;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"DefaultsManager: Error resolving rule '{defaultValue.Rule}'. Exception: {ex.Message}");
                return defaultValue.PropertyValue; // Fallback to static value
            }
        }

        /// <summary>
        /// Resolves the default value for a given data source and field name.
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <param name="FieldName">The name of the field to find the default for.</param>
        /// <param name="parameters">The parameters to pass to the rule, if applicable.</param>
        /// <returns>The resolved value.</returns>
        public static object ResolveDefaultValue(IDMEEditor editor, string dataSourceName, string FieldName, IPassedArgs parameters)
        {
            EnsureInitialized(editor);

            // Get the DefaultValue for the specified field name
            var defaultValue = _defaultValueHelper.GetDefaultForField(dataSourceName, FieldName);
            if (defaultValue == null)
                return null;

            // Resolve the value using the enhanced resolver
            return ResolveDefaultValue(editor, defaultValue, parameters);
        }

        /// <summary>
        /// Saves the default values for a specified data source.
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="defaults">The default values to save.</param>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <returns>Error information.</returns>
        public static IErrorsInfo SaveDefaults(IDMEEditor editor, List<DefaultValue> defaults, string dataSourceName)
        {
            EnsureInitialized(editor);
            return _defaultValueHelper.SaveDefaults(defaults, dataSourceName);
        }

        #endregion

        #region Enhanced API - Validation and Testing

        /// <summary>
        /// Creates a new default value with validation
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="FieldName">Name of the field</param>
        /// <param name="value">Default value (can be null if rule is provided)</param>
        /// <param name="rule">Optional rule for dynamic value generation</param>
        /// <returns>Validation result and created default value</returns>
        public static (IErrorsInfo validation, DefaultValue defaultValue) CreateDefaultValue(
            IDMEEditor editor, string FieldName, string value, string rule = null)
        {
            EnsureInitialized(editor);

            var defaultValue = _defaultValueHelper.CreateDefaultValue(FieldName, value, rule);
            var validation = _validationHelper.ValidateDefaultValue(defaultValue);

            return (validation, defaultValue);
        }

        /// <summary>
        /// Validates a default value configuration
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="defaultValue">Default value to validate</param>
        /// <returns>Validation result</returns>
        public static IErrorsInfo ValidateDefaultValue(IDMEEditor editor, DefaultValue defaultValue)
        {
            EnsureInitialized(editor);
            return _validationHelper.ValidateDefaultValue(defaultValue);
        }

        /// <summary>
        /// Validates a rule syntax
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="rule">Rule to validate</param>
        /// <returns>Validation result</returns>
        public static IErrorsInfo ValidateRule(IDMEEditor editor, string rule)
        {
            EnsureInitialized(editor);
            return _validationHelper.ValidateRule(rule);
        }

        /// <summary>
        /// Tests a rule without applying it to data
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="rule">Rule to test</param>
        /// <param name="parameters">Optional parameters for testing</param>
        /// <returns>Test result with resolved value or error information</returns>
        public static (IErrorsInfo result, object value) TestRule(IDMEEditor editor, string rule, IPassedArgs parameters = null)
        {
            EnsureInitialized(editor);

            try
            {
                // First validate the rule
                var validation = _validationHelper.ValidateRule(rule);
                if (validation.Flag == Errors.Failed)
                {
                    return (validation, null);
                }

                // Try to resolve the rule
                var resolvedValue = _resolverManager.ResolveValue(rule, parameters);

                var successResult = CreateErrorsInfo(Errors.Ok, $"Rule '{rule}' resolved successfully to: {resolvedValue}");

                return (successResult, resolvedValue);
            }
            catch (Exception ex)
            {
                var errorResult = CreateErrorsInfo(Errors.Failed, $"Error testing rule '{rule}': {ex.Message}");
                return (errorResult, null);
            }
        }

        #endregion

        #region Resolver Management

        /// <summary>
        /// Registers a custom resolver for default value rules
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="resolver">Custom resolver to register</param>
        public static void RegisterCustomResolver(IDMEEditor editor, IDefaultValueResolver resolver)
        {
            EnsureInitialized(editor);
            _resolverManager.RegisterResolver(resolver);
        }

        /// <summary>
        /// Gets all available resolvers and their capabilities
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <returns>Dictionary of resolver names and their supported rule types</returns>
        public static Dictionary<string, IEnumerable<string>> GetAvailableResolvers(IDMEEditor editor)
        {
            EnsureInitialized(editor);

            var resolvers = _resolverManager.GetResolvers();
            return resolvers.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.SupportedRuleTypes
            );
        }

        /// <summary>
        /// Gets examples for all available resolvers
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <returns>Dictionary of resolver names and their examples</returns>
        public static Dictionary<string, IEnumerable<string>> GetResolverExamples(IDMEEditor editor)
        {
            EnsureInitialized(editor);

            var resolvers = _resolverManager.GetResolvers();
            return resolvers.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.GetExamples()
            );
        }

        #endregion

        #region Template Creation Methods

        /// <summary>
        /// Creates a default value template based on the specified template name
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        /// <param name="templateName">Name of the template to create (Audit, UserManagement, etc.)</param>
        /// <returns>List of default values based on the template</returns>
        public static List<DefaultValue> CreateDefaultValueTemplate(IDMEEditor editor, string templateName)
        {
            EnsureInitialized(editor);

            var defaults = new List<DefaultValue>();

            switch (templateName.ToLowerInvariant())
            {
                case "audit":
                    defaults.AddRange(CreateAuditTemplate());
                    break;

                case "usermanagement":
                    defaults.AddRange(CreateUserManagementTemplate());
                    break;

                case "orderprocessing":
                    defaults.AddRange(CreateOrderProcessingTemplate());
                    break;

                case "customermanagement":
                    defaults.AddRange(CreateCustomerManagementTemplate());
                    break;

                case "productcatalog":
                    defaults.AddRange(CreateProductCatalogTemplate());
                    break;

                case "financial":
                    defaults.AddRange(CreateFinancialTemplate());
                    break;

                case "inventory":
                    defaults.AddRange(CreateInventoryTemplate());
                    break;

                case "basic":
                    defaults.AddRange(CreateBasicTemplate());
                    break;

                default:
                    _logger?.WriteLog($"DefaultsManager: Unknown template name '{templateName}', returning empty defaults");
                    break;
            }

            return defaults;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Creates an IErrorsInfo object with the specified flag and message
        /// </summary>
        /// <param name="flag">The error flag</param>
        /// <param name="message">The error message</param>
        /// <returns>IErrorsInfo object</returns>
        protected static IErrorsInfo CreateErrorsInfo(Errors flag, string message)
        {
            return new ErrorsInfo
            {
                Flag = flag,
                Message = message
            };
        }

        /// <summary>
        /// Converts a value to the target type
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="targetType">Target type</param>
        /// <returns>Converted value</returns>
        protected static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
                }

                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error converting value '{value}' to type '{targetType}': {ex.Message}");
                return value; // Return original value if conversion fails
            }
        }

        #endregion

        #region IDisposable Implementation

        private static bool _disposed = false;

        /// <summary>
        /// Disposes resources used by the DefaultsManager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lockObject)
                {
                    // Clean up resources
                    _defaultValueHelper = null;
                    _resolverManager = null;
                    _validationHelper = null;
                    _initialized = false;
                    _disposed = true;
                }
            }
        }

        #endregion
    }
}
