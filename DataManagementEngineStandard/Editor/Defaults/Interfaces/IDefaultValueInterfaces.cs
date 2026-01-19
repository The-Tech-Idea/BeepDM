using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Interfaces
{
    /// <summary>
    /// Interface for resolving default values using rules and formulas
    /// </summary>
    public interface IDefaultValueResolver
    {
        /// <summary>
        /// Gets the name of the resolver
        /// </summary>
        string ResolverName { get; }

        /// <summary>
        /// Gets the supported rule types for this resolver
        /// </summary>
        IEnumerable<string> SupportedRuleTypes { get; }

        /// <summary>
        /// Resolves a default value based on the rule and parameters
        /// </summary>
        /// <param name="rule">The rule string to resolve</param>
        /// <param name="parameters">Parameters for rule resolution</param>
        /// <returns>The resolved value</returns>
        object ResolveValue(string rule, IPassedArgs parameters);

        /// <summary>
        /// Validates if the rule can be handled by this resolver
        /// </summary>
        /// <param name="rule">The rule string to validate</param>
        /// <returns>True if the rule can be handled</returns>
        bool CanHandle(string rule);

        /// <summary>
        /// Gets example usage for this resolver
        /// </summary>
        /// <returns>List of example rule strings</returns>
        IEnumerable<string> GetExamples();
    }

    /// <summary>
    /// Interface for managing default value operations
    /// </summary>
    public interface IDefaultValueHelper
    {
        /// <summary>
        /// Gets default values for a data source
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <returns>List of default values</returns>
        List<DefaultValue> GetDefaults(string dataSourceName);

        /// <summary>
        /// Saves default values for a data source
        /// </summary>
        /// <param name="defaults">Default values to save</param>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <returns>Operation result</returns>
        IErrorsInfo SaveDefaults(List<DefaultValue> defaults, string dataSourceName);

        /// <summary>
        /// Gets a specific default value by field name
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="FieldName">Name of the field</param>
        /// <returns>Default value or null if not found</returns>
        DefaultValue GetDefaultForField(string dataSourceName, string FieldName);

        /// <summary>
        /// Creates a new default value entry
        /// </summary>
        /// <param name="FieldName">Field name</param>
        /// <param name="value">Default value</param>
        /// <param name="rule">Optional rule</param>
        /// <returns>New default value</returns>
        DefaultValue CreateDefaultValue(string FieldName, string value, string rule = null);

        /// <summary>
        /// Validates a default value configuration
        /// </summary>
        /// <param name="defaultValue">Default value to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateDefaultValue(DefaultValue defaultValue);
    }

    /// <summary>
    /// Interface for resolving default value rules
    /// </summary>
    public interface IDefaultValueResolverManager
    {
        /// <summary>
        /// Registers a resolver
        /// </summary>
        /// <param name="resolver">Resolver to register</param>
        void RegisterResolver(IDefaultValueResolver resolver);

        /// <summary>
        /// Unregisters a resolver
        /// </summary>
        /// <param name="resolverName">Name of resolver to unregister</param>
        void UnregisterResolver(string resolverName);

        /// <summary>
        /// Resolves a value using the appropriate resolver
        /// </summary>
        /// <param name="rule">Rule to resolve</param>
        /// <param name="parameters">Parameters for resolution</param>
        /// <returns>Resolved value</returns>
        object ResolveValue(string rule, IPassedArgs parameters);

        /// <summary>
        /// Gets all registered resolvers
        /// </summary>
        /// <returns>Dictionary of resolvers by name</returns>
        IReadOnlyDictionary<string, IDefaultValueResolver> GetResolvers();

        /// <summary>
        /// Gets resolver for a specific rule
        /// </summary>
        /// <param name="rule">Rule to find resolver for</param>
        /// <returns>Resolver or null if not found</returns>
        IDefaultValueResolver GetResolverForRule(string rule);
    }

    /// <summary>
    /// Interface for validation operations
    /// </summary>
    public interface IDefaultValueValidationHelper
    {
        /// <summary>
        /// Validates a default value configuration
        /// </summary>
        /// <param name="defaultValue">Default value to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateDefaultValue(DefaultValue defaultValue);

        /// <summary>
        /// Validates a rule syntax
        /// </summary>
        /// <param name="rule">Rule to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateRule(string rule);

        /// <summary>
        /// Validates field name for data source
        /// </summary>
        /// <param name="dataSourceName">Data source name</param>
        /// <param name="FieldName">Field name</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateFieldName(string dataSourceName, string FieldName);
    }
}