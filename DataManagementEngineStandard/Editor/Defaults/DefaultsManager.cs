using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;
using TheTechIdea.Beep.Editor.Defaults.Helpers;
using TheTechIdea.Beep.Editor.Defaults.RuleParsing;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.Defaults.Resolvers;

namespace TheTechIdea.Beep.Editor.Defaults
{
    /// <summary>
    /// Manages default values for entity fields.
    ///
    /// Dual surface:
    ///   • Static helpers  — call-site convenience, all methods take IDMEEditor
    ///   • Instance / IDefaultsManager — for dependency-injection scenarios
    ///
    /// Rule-string convention:
    ///   ":NOW", ":USERNAME", ":NEWGUID"  — expression (parsed + resolved at runtime)
    ///   "Active", "1"                    — literal (used as-is, no resolver invoked)
    /// </summary>
    public partial class DefaultsManager : IDisposable, IDefaultsManager
    {
        #region Private State

        protected static IConfigEditor _configEditor;
        protected static IDMLogger     _logger;
        protected static IDMEEditor    _editor;

        protected static IDefaultValueHelper          _defaultValueHelper;
        protected static IDefaultValueResolverManager _resolverManager;
        protected static IDefaultValueValidationHelper _validationHelper;

        protected static bool _initialized = false;
        protected static readonly object _lockObject = new object();

        // Profile registry: "datasource::entity" -> EntityDefaultsProfile
        private static readonly Dictionary<string, EntityDefaultsProfile> _profiles =
            new Dictionary<string, EntityDefaultsProfile>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Initialization

        /// <summary>Initializes or re-initializes the manager with the supplied editor.</summary>
        public static void Initialize(IDMEEditor editor)
        {
            lock (_lockObject)
            {
                if (_initialized && _editor == editor)
                    return;

                _editor       = editor ?? throw new ArgumentNullException(nameof(editor));
                _configEditor = _editor.ConfigEditor;
                _logger       = _editor.Logger;

                _defaultValueHelper = new DefaultValueHelper(_editor);
                _resolverManager    = new DefaultValueResolverManager(_editor);
                _validationHelper   = new DefaultValueValidationHelper(_editor);

                _initialized = true;
                _logger?.WriteLog("DefaultsManager initialized.");
            }
        }

        protected static void EnsureInitialized(IDMEEditor editor)
        {
            if (!_initialized || _editor != editor)
                Initialize(editor);
        }

        #endregion

        #region Static accessor properties

        public static IDefaultValueHelper DefaultValueHelper
        {
            get
            {
                if (!_initialized) throw new InvalidOperationException("DefaultsManager not initialized.");
                return _defaultValueHelper;
            }
        }

        public static IDefaultValueResolverManager ResolverManager
        {
            get
            {
                if (!_initialized) throw new InvalidOperationException("DefaultsManager not initialized.");
                return _resolverManager;
            }
        }

        public static IDefaultValueValidationHelper ValidationHelper
        {
            get
            {
                if (!_initialized) throw new InvalidOperationException("DefaultsManager not initialized.");
                return _validationHelper;
            }
        }

        #endregion

        #region Profile Registry (static)

        private static string ProfileKey(string datasource, string entity) =>
            string.Concat(datasource, "::", entity);

        /// <summary>Registers (or replaces) a profile for datasource + entity.</summary>
        public static void RegisterProfile(string datasourceName, string entityName, EntityDefaultsProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            lock (_lockObject)
                _profiles[ProfileKey(datasourceName, entityName)] = profile;
        }

        /// <summary>Returns the registered profile, or null if none.</summary>
        public static EntityDefaultsProfile GetProfile(string datasourceName, string entityName)
        {
            lock (_lockObject)
            {
                _profiles.TryGetValue(ProfileKey(datasourceName, entityName), out var p);
                return p;
            }
        }

        /// <summary>Removes the registered profile (no-op if not found).</summary>
        public static void RemoveProfile(string datasourceName, string entityName)
        {
            lock (_lockObject)
                _profiles.Remove(ProfileKey(datasourceName, entityName));
        }

        #endregion

        #region Resolve (single rule string)

        /// <summary>
        /// Resolves a single rule string and returns the value.
        /// Prefix with ":" for an expression (e.g. ":NOW", ":USERNAME").
        /// No prefix = literal (returns the string as-is).
        /// </summary>
        public static object Resolve(IDMEEditor editor, string ruleString, IPassedArgs context = null)
        {
            EnsureInitialized(editor);
            if (string.IsNullOrWhiteSpace(ruleString))
                return null;

            var parsed = RuleNormalizer.Normalize(ruleString);
            if (parsed.IsLiteral)
                return parsed.NormalizedRule;

            return _resolverManager.ResolveValue(parsed.NormalizedRule, context);
        }

        #endregion

        #region Apply (public static wrappers)

        /// <summary>Applies the registered profile defaults to a dictionary record.</summary>
        public static IErrorsInfo Apply(IDMEEditor editor, string datasource, string entity,
            Dictionary<string, object> record, IPassedArgs context = null)
            => ApplyInstance(editor, datasource, entity, record, context);

        /// <summary>Applies the registered profile defaults to a POCO via reflection.</summary>
        public static IErrorsInfo Apply<T>(IDMEEditor editor, string datasource, string entity,
            T poco, IPassedArgs context = null) where T : class
            => ApplyInstance(editor, datasource, entity, poco, context);

        /// <summary>Applies the registered profile defaults to a DataRow.</summary>
        public static IErrorsInfo Apply(IDMEEditor editor, string datasource, string entity,
            System.Data.DataRow row, IPassedArgs context = null)
            => ApplyInstance(editor, datasource, entity, row, context);

        #endregion

        #region Core Backward-compat API

        public static List<DefaultValue> GetDefaults(IDMEEditor editor, string dataSourceName)
        {
            EnsureInitialized(editor);
            return _defaultValueHelper.GetDefaults(dataSourceName);
        }

        public static object ResolveDefaultValue(IDMEEditor editor, DefaultValue defaultValue, IPassedArgs parameters)
        {
            EnsureInitialized(editor);
            if (defaultValue == null) return null;
            try
            {
                if (!string.IsNullOrEmpty(defaultValue.Rule))
                {
                    var resolved = Resolve(editor, defaultValue.Rule, parameters);
                    if (resolved != null) return resolved;
                }
                return defaultValue.PropertyValue;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"DefaultsManager: error resolving '{defaultValue.Rule}': {ex.Message}");
                return defaultValue.PropertyValue;
            }
        }

        public static object ResolveDefaultValue(IDMEEditor editor, string dataSourceName, string fieldName, IPassedArgs parameters)
        {
            EnsureInitialized(editor);
            var dv = _defaultValueHelper.GetDefaultForField(dataSourceName, fieldName);
            return dv == null ? null : ResolveDefaultValue(editor, dv, parameters);
        }

        public static IErrorsInfo SaveDefaults(IDMEEditor editor, List<DefaultValue> defaults, string dataSourceName)
        {
            EnsureInitialized(editor);
            return _defaultValueHelper.SaveDefaults(defaults, dataSourceName);
        }

        public static (IErrorsInfo validation, DefaultValue defaultValue) CreateDefaultValue(
            IDMEEditor editor, string fieldName, string value, string rule = null)
        {
            EnsureInitialized(editor);
            var dv  = _defaultValueHelper.CreateDefaultValue(fieldName, value, rule);
            var err = _validationHelper.ValidateDefaultValue(dv);
            return (err, dv);
        }

        public static IErrorsInfo ValidateDefaultValue(IDMEEditor editor, DefaultValue defaultValue)
        {
            EnsureInitialized(editor);
            return _validationHelper.ValidateDefaultValue(defaultValue);
        }

        public static IErrorsInfo ValidateRule(IDMEEditor editor, string rule)
        {
            EnsureInitialized(editor);
            return _validationHelper.ValidateRule(rule);
        }

        public static (IErrorsInfo result, object value) TestRule(IDMEEditor editor, string rule, IPassedArgs parameters = null)
        {
            EnsureInitialized(editor);
            try
            {
                var validation = _validationHelper.ValidateRule(rule);
                if (validation.Flag == Errors.Failed)
                    return (validation, null);
                var val = Resolve(editor, rule, parameters);
                return (CreateError(Errors.Ok, $"Rule '{rule}' resolved to: {val}"), val);
            }
            catch (Exception ex)
            {
                return (CreateError(Errors.Failed, $"Error testing rule '{rule}': {ex.Message}"), null);
            }
        }

        #endregion

        #region Column Default API (backward compat)

        public static IErrorsInfo SetColumnDefault(IDMEEditor editor, string dataSourceName, string entityName,
            string columnName, string defaultValue, bool isRule = false)
        {
            EnsureInitialized(editor);
            try
            {
                if (string.IsNullOrWhiteSpace(dataSourceName)) return CreateError("Data source name cannot be empty");
                if (string.IsNullOrWhiteSpace(entityName))     return CreateError("Entity name cannot be empty");
                if (string.IsNullOrWhiteSpace(columnName))     return CreateError("Column name cannot be empty");

                var fieldName = $"{entityName}.{columnName}";
                var (validation, dvObj) = CreateDefaultValue(editor, fieldName,
                    isRule ? null : defaultValue,
                    isRule ? defaultValue : null);

                if (validation.Flag == Errors.Failed) return validation;

                var defaults = GetDefaults(editor, dataSourceName);
                defaults.RemoveAll(d => d.PropertyName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                defaults.Add(dvObj);
                return SaveDefaults(editor, defaults, dataSourceName);
            }
            catch (Exception ex) { return CreateError($"Error setting column default: {ex.Message}"); }
        }

        public static object GetColumnDefault(IDMEEditor editor, string dataSourceName, string entityName,
            string columnName, IPassedArgs parameters = null)
        {
            EnsureInitialized(editor);
            return ResolveDefaultValue(editor, dataSourceName, $"{entityName}.{columnName}", parameters);
        }

        public static IErrorsInfo RemoveColumnDefault(IDMEEditor editor, string dataSourceName,
            string entityName, string columnName)
        {
            EnsureInitialized(editor);
            try
            {
                var fieldName = $"{entityName}.{columnName}";
                var defaults  = GetDefaults(editor, dataSourceName);
                var removed   = defaults.RemoveAll(d => d.PropertyName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                if (removed > 0)
                {
                    var r = SaveDefaults(editor, defaults, dataSourceName);
                    if (r.Flag == Errors.Ok) r.Message = $"Removed default for {entityName}.{columnName}";
                    return r;
                }
                return new ErrorsInfo { Flag = Errors.Ok, Message = $"No default found for {entityName}.{columnName}" };
            }
            catch (Exception ex) { return CreateError($"Error removing column default: {ex.Message}"); }
        }

        public static Dictionary<string, DefaultValue> GetEntityDefaults(IDMEEditor editor,
            string dataSourceName, string entityName)
        {
            EnsureInitialized(editor);
            var defaults     = GetDefaults(editor, dataSourceName);
            var entityPrefix = $"{entityName}.";
            return defaults
                .Where(d => d.PropertyName.StartsWith(entityPrefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    d => d.PropertyName.Substring(entityPrefix.Length),
                    d => d,
                    StringComparer.OrdinalIgnoreCase);
        }

        public static IErrorsInfo SetMultipleColumnDefaults(IDMEEditor editor, string dataSourceName,
            string entityName, Dictionary<string, (string value, bool isRule)> columnDefaults)
        {
            EnsureInitialized(editor);
            try
            {
                if (columnDefaults == null || !columnDefaults.Any())
                    return CreateError("No column defaults provided");

                var defaults     = GetDefaults(editor, dataSourceName);
                var entityPrefix = $"{entityName}.";
                var errors       = new List<string>();

                defaults.RemoveAll(d => d.PropertyName.StartsWith(entityPrefix, StringComparison.OrdinalIgnoreCase));

                foreach (var kvp in columnDefaults)
                {
                    var fieldName = $"{entityName}.{kvp.Key}";
                    var (validation, dv) = CreateDefaultValue(editor, fieldName,
                        kvp.Value.isRule ? null : kvp.Value.value,
                        kvp.Value.isRule ? kvp.Value.value : null);

                    if (validation.Flag == Errors.Ok)
                        defaults.Add(dv);
                    else
                        errors.Add($"{kvp.Key}: {validation.Message}");
                }

                var saveResult = SaveDefaults(editor, defaults, dataSourceName);
                if (errors.Any())
                    saveResult.Message += $" Errors: {string.Join("; ", errors)}";
                return saveResult;
            }
            catch (Exception ex) { return CreateError($"Error setting multiple defaults: {ex.Message}"); }
        }

        #endregion

        #region Resolver Management

        public static void RegisterCustomResolver(IDMEEditor editor, IDefaultValueResolver resolver)
        {
            EnsureInitialized(editor);
            _resolverManager.RegisterResolver(resolver);
        }

        public static Dictionary<string, IEnumerable<string>> GetAvailableResolvers(IDMEEditor editor)
        {
            EnsureInitialized(editor);
            return _resolverManager.GetResolvers()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.SupportedRuleTypes);
        }

        public static Dictionary<string, IEnumerable<string>> GetResolverExamples(IDMEEditor editor)
        {
            EnsureInitialized(editor);
            return _resolverManager.GetResolvers()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetExamples());
        }

        #endregion

        #region Utility

        protected static IErrorsInfo CreateError(string message) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = message };

        protected static IErrorsInfo CreateError(Errors flag, string message) =>
            new ErrorsInfo { Flag = flag, Message = message };

        protected static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsAssignableFrom(value.GetType())) return value;
            try
            {
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    targetType = Nullable.GetUnderlyingType(targetType);
                return Convert.ChangeType(value, targetType);
            }
            catch { return value; }
        }

        #endregion

        #region IDefaultsManager implementation (instance delegates to static)

        void IDefaultsManager.Initialize(IDMEEditor editor) => Initialize(editor);

        void IDefaultsManager.RegisterProfile(string ds, string entity, EntityDefaultsProfile profile) =>
            RegisterProfile(ds, entity, profile);

        EntityDefaultsProfile IDefaultsManager.GetProfile(string ds, string entity) =>
            GetProfile(ds, entity);

        void IDefaultsManager.RemoveProfile(string ds, string entity) =>
            RemoveProfile(ds, entity);

        IErrorsInfo IDefaultsManager.Apply(IDMEEditor editor, string ds, string entity,
            Dictionary<string, object> record, IPassedArgs context)
            => ApplyInstance(editor, ds, entity, record, context);

        IErrorsInfo IDefaultsManager.Apply<T>(IDMEEditor editor, string ds, string entity,
            T poco, IPassedArgs context)
            => ApplyInstance(editor, ds, entity, poco, context);

        IErrorsInfo IDefaultsManager.Apply(IDMEEditor editor, string ds, string entity,
            System.Data.DataRow row, IPassedArgs context)
            => ApplyInstance(editor, ds, entity, row, context);

        object IDefaultsManager.Resolve(IDMEEditor editor, string rule, IPassedArgs context) =>
            Resolve(editor, rule, context);

        (IErrorsInfo result, object value) IDefaultsManager.TestRule(IDMEEditor editor, string rule, IPassedArgs parameters) =>
            TestRule(editor, rule, parameters);

        IErrorsInfo IDefaultsManager.ValidateRule(IDMEEditor editor, string rule) =>
            ValidateRule(editor, rule);

        void IDefaultsManager.RegisterCustomResolver(IDMEEditor editor, IDefaultValueResolver resolver) =>
            RegisterCustomResolver(editor, resolver);

        Dictionary<string, IEnumerable<string>> IDefaultsManager.GetAvailableResolvers(IDMEEditor editor) =>
            GetAvailableResolvers(editor);

        List<DefaultValue> IDefaultsManager.GetDefaults(IDMEEditor editor, string ds) =>
            GetDefaults(editor, ds);

        IErrorsInfo IDefaultsManager.SaveDefaults(IDMEEditor editor, List<DefaultValue> defaults, string ds) =>
            SaveDefaults(editor, defaults, ds);

        IErrorsInfo IDefaultsManager.SetColumnDefault(IDMEEditor editor, string ds, string entity,
            string col, string dv, bool isRule) =>
            SetColumnDefault(editor, ds, entity, col, dv, isRule);

        object IDefaultsManager.GetColumnDefault(IDMEEditor editor, string ds, string entity,
            string col, IPassedArgs p) =>
            GetColumnDefault(editor, ds, entity, col, p);

        IErrorsInfo IDefaultsManager.RemoveColumnDefault(IDMEEditor editor, string ds, string entity, string col) =>
            RemoveColumnDefault(editor, ds, entity, col);

        Dictionary<string, DefaultValue> IDefaultsManager.GetEntityDefaults(IDMEEditor editor, string ds, string entity) =>
            GetEntityDefaults(editor, ds, entity);

        IErrorsInfo IDefaultsManager.SetMultipleColumnDefaults(IDMEEditor editor, string ds, string entity,
            Dictionary<string, (string value, bool isRule)> cols) =>
            SetMultipleColumnDefaults(editor, ds, entity, cols);

        object IDefaultsManager.ResolveDefaultValue(IDMEEditor editor, DefaultValue dv, IPassedArgs p) =>
            ResolveDefaultValue(editor, dv, p);

        object IDefaultsManager.ResolveDefaultValue(IDMEEditor editor, string ds, string field, IPassedArgs p) =>
            ResolveDefaultValue(editor, ds, field, p);

        (IErrorsInfo validation, DefaultValue defaultValue) IDefaultsManager.CreateDefaultValue(
            IDMEEditor editor, string field, string value, string rule) =>
            CreateDefaultValue(editor, field, value, rule);

        IErrorsInfo IDefaultsManager.ValidateDefaultValue(IDMEEditor editor, DefaultValue dv) =>
            ValidateDefaultValue(editor, dv);

        #endregion

        #region IDisposable

        private static bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lockObject)
                {
                    _defaultValueHelper = null;
                    _resolverManager    = null;
                    _validationHelper   = null;
                    _initialized        = false;
                    _disposed           = true;
                }
            }
        }

        #endregion
    }
}
