using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Manager for default value resolvers with extensible resolver registration
    /// Enhanced to support Dictionary parameters and better resolver management
    /// </summary>
    public class DefaultValueResolverManager : IDefaultValueResolverManager
    {
        private readonly Dictionary<string, IDefaultValueResolver> _resolvers;
        private readonly IDMEEditor _editor;

        public DefaultValueResolverManager(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _resolvers = new Dictionary<string, IDefaultValueResolver>(StringComparer.OrdinalIgnoreCase);
            
            // Register built-in resolvers
            RegisterBuiltInResolvers();
        }

        public void RegisterResolver(IDefaultValueResolver resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _resolvers[resolver.ResolverName] = resolver;
            _editor.AddLogMessage("DefaultValueResolverManager", 
                $"Registered resolver '{resolver.ResolverName}'", DateTime.Now, -1, "", Errors.Ok);
        }

        public void UnregisterResolver(string resolverName)
        {
            if (_resolvers.Remove(resolverName))
            {
                _editor.AddLogMessage("DefaultValueResolverManager", 
                    $"Unregistered resolver '{resolverName}'", DateTime.Now, -1, "", Errors.Ok);
            }
        }

        public object ResolveValue(string rule, IPassedArgs parameters)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return null;

            var resolver = GetResolverForRule(rule);
            if (resolver == null)
            {
                _editor.AddLogMessage("DefaultValueResolverManager", 
                    $"No resolver found for rule '{rule}'", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }

            try
            {
                return resolver.ResolveValue(rule, parameters);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("DefaultValueResolverManager", 
                    $"Error resolving rule '{rule}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Enhanced ResolveValue method that accepts Dictionary parameters for better flexibility
        /// </summary>
        /// <param name="rule">Rule to resolve</param>
        /// <param name="parameters">Dictionary of parameters</param>
        /// <returns>Resolved value</returns>
        public object ResolveValue(string rule, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return null;

            var resolver = GetResolverForRule(rule);
            if (resolver == null)
            {
                _editor.AddLogMessage("DefaultValueResolverManager", 
                    $"No resolver found for rule '{rule}'", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }

            try
            {
                // Convert Dictionary to IPassedArgs for compatibility
                var passedArgs = ConvertToPassedArgs(parameters);
                return resolver.ResolveValue(rule, passedArgs);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("DefaultValueResolverManager", 
                    $"Error resolving rule '{rule}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }
        }

        public IReadOnlyDictionary<string, IDefaultValueResolver> GetResolvers()
        {
            return _resolvers;
        }

        public IDefaultValueResolver GetResolverForRule(string rule)
        {
            return _resolvers.Values.FirstOrDefault(r => r.CanHandle(rule));
        }

        private void RegisterBuiltInResolvers()
        {
            try
            {
                // Register built-in resolvers
                RegisterResolver(new DateTimeResolver(_editor));
                RegisterResolver(new UserContextResolver(_editor));
                RegisterResolver(new SystemInfoResolver(_editor));
                RegisterResolver(new GuidResolver(_editor));
                RegisterResolver(new FormulaResolver(_editor));
                RegisterResolver(new DataSourceResolver(_editor));
                RegisterResolver(new ObjectPropertyResolver(_editor));
                RegisterResolver(new ConfigurationResolver(_editor));
                RegisterResolver(new EnvironmentResolver(_editor));
                RegisterResolver(new ExpressionResolver(_editor));

                _editor.AddLogMessage("DefaultValueResolverManager", 
                    $"Registered {_resolvers.Count} built-in resolvers", DateTime.Now, -1, "", Errors.Ok);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("DefaultValueResolverManager", 
                    $"Error registering built-in resolvers: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }
        }

        /// <summary>
        /// Converts Dictionary parameters to IPassedArgs for backward compatibility
        /// </summary>
        /// <param name="parameters">Dictionary parameters</param>
        /// <returns>IPassedArgs instance</returns>
        private IPassedArgs ConvertToPassedArgs(Dictionary<string, object> parameters)
        {
            if (parameters == null)
                return null;

            var passedArgs = new PassedArgs();

            // Map common parameters
            if (parameters.TryGetValue("DataSource", out var dataSource) && dataSource is IDataSource ds)
                passedArgs.DataSource = ds;

            if (parameters.TryGetValue("CurrentEntity", out var entity) && entity is string entityName)
                passedArgs.CurrentEntity = entityName;

            if (parameters.TryGetValue("DatasourceName", out var dsName) && dsName is string dataSourceName)
                passedArgs.DatasourceName = dataSourceName;

            if (parameters.TryGetValue("Record", out var record))
            {
                if (record != null)
                {
                    // Convert to List<ObjectItem> as expected by IPassedArgs
                    passedArgs.Objects = new List<ObjectItem>
                    {
                        new ObjectItem { obj = record, Name = "Record" }
                    };
                }
                else
                {
                    passedArgs.Objects = new List<ObjectItem>();
                }
            }

            if (parameters.TryGetValue("FieldName", out var fieldName) && fieldName is string field)
                passedArgs.ParameterString1 = field;

            if (parameters.TryGetValue("Parameters", out var additionalParams) && additionalParams is Dictionary<string, object> additionalDict)
            {
                // Store additional parameters in available fields
                var keys = additionalDict.Keys.ToArray();
                if (keys.Length > 0 && additionalDict.TryGetValue(keys[0], out var param1))
                {
                    passedArgs.ParameterString2 = param1?.ToString();
                }
                if (keys.Length > 1 && additionalDict.TryGetValue(keys[1], out var param2))
                {
                    passedArgs.ParameterString3 = param2?.ToString();
                }
            }

            return passedArgs;
        }

        /// <summary>
        /// Gets resolver statistics and health information
        /// </summary>
        /// <returns>Dictionary with resolver statistics</returns>
        public Dictionary<string, object> GetResolverStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalResolvers"] = _resolvers.Count,
                ["ResolverNames"] = _resolvers.Keys.ToList(),
                ["RegisteredAt"] = DateTime.Now
            };

            // Group resolvers by type
            var resolverTypes = new Dictionary<string, int>();
            foreach (var resolver in _resolvers.Values)
            {
                var type = resolver.GetType().Name.Replace("Resolver", "");
                resolverTypes[type] = resolverTypes.ContainsKey(type) ? resolverTypes[type] + 1 : 1;
            }
            stats["ResolverTypes"] = resolverTypes;

            return stats;
        }

        /// <summary>
        /// Tests if a rule can be resolved without actually resolving it
        /// </summary>
        /// <param name="rule">Rule to test</param>
        /// <returns>True if the rule can be handled</returns>
        public bool CanResolveRule(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            return GetResolverForRule(rule) != null;
        }

        /// <summary>
        /// Gets all examples from all registered resolvers
        /// </summary>
        /// <returns>Dictionary mapping resolver names to their examples</returns>
        public Dictionary<string, IEnumerable<string>> GetAllExamples()
        {
            var examples = new Dictionary<string, IEnumerable<string>>();
            
            foreach (var kvp in _resolvers)
            {
                try
                {
                    examples[kvp.Key] = kvp.Value.GetExamples();
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("DefaultValueResolverManager", 
                        $"Error getting examples from resolver '{kvp.Key}': {ex.Message}", DateTime.Now, -1, "", Errors.Warning);
                    examples[kvp.Key] = new[] { $"Error getting examples: {ex.Message}" };
                }
            }

            return examples;
        }

        /// <summary>
        /// Validates that all registered resolvers are functioning correctly
        /// </summary>
        /// <returns>Validation results</returns>
        public Dictionary<string, bool> ValidateResolvers()
        {
            var results = new Dictionary<string, bool>();

            foreach (var kvp in _resolvers)
            {
                try
                {
                    var resolver = kvp.Value;
                    
                    // Basic validation checks
                    var isValid = !string.IsNullOrWhiteSpace(resolver.ResolverName) &&
                                  resolver.SupportedRuleTypes != null &&
                                  resolver.GetExamples() != null;

                    results[kvp.Key] = isValid;

                    if (!isValid)
                    {
                        _editor.AddLogMessage("DefaultValueResolverManager", 
                            $"Resolver '{kvp.Key}' failed validation", DateTime.Now, -1, "", Errors.Warning);
                    }
                }
                catch (Exception ex)
                {
                    results[kvp.Key] = false;
                    _editor.AddLogMessage("DefaultValueResolverManager", 
                        $"Error validating resolver '{kvp.Key}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                }
            }

            return results;
        }
    }
}