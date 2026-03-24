using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;

namespace TheTechIdea.Beep.Editor.Defaults
{
    /// <summary>
    /// Contract for the defaults manager.
    ///
    /// Design notes:
    /// — All methods that need the DME editor accept <see cref="IDMEEditor"/> as a parameter
    ///   so the static singleton surface (<see cref="DefaultsManager"/>) and a future
    ///   injected instance can share the same interface.
    /// — Rule strings use the ":" prefix convention:
    ///     ":NOW"      → expression, will be parsed and resolved
    ///     "Active"    → literal, used as-is
    /// </summary>
    public interface IDefaultsManager : IDisposable
    {
        // ── Lifecycle ────────────────────────────────────────────────────────────

        /// <summary>Initializes or re-initializes the manager with the supplied editor.</summary>
        void Initialize(IDMEEditor editor);

        // ── Profile registry ─────────────────────────────────────────────────────

        /// <summary>
        /// Registers (or replaces) an <see cref="EntityDefaultsProfile"/> for the given
        /// datasource + entity combination.
        /// </summary>
        void RegisterProfile(string datasourceName, string entityName, EntityDefaultsProfile profile);

        /// <summary>
        /// Returns the registered profile for the given datasource + entity,
        /// or <c>null</c> if none is registered.
        /// </summary>
        EntityDefaultsProfile GetProfile(string datasourceName, string entityName);

        /// <summary>Removes a registered profile (no-op if not found).</summary>
        void RemoveProfile(string datasourceName, string entityName);

        // ── Apply ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies registered profile defaults to a record dictionary.
        /// Fields already populated are left unchanged by default
        /// (see <see cref="FieldDefaultRule.ApplyOnlyIfNull"/>).
        /// </summary>
        IErrorsInfo Apply(IDMEEditor editor, string datasourceName, string entityName,
                          System.Collections.Generic.Dictionary<string, object> record,
                          IPassedArgs context = null);

        /// <summary>
        /// Applies registered profile defaults to a POCO object via reflection.
        /// </summary>
        IErrorsInfo Apply<T>(IDMEEditor editor, string datasourceName, string entityName,
                             T poco, IPassedArgs context = null) where T : class;

        /// <summary>
        /// Applies registered profile defaults to a <see cref="System.Data.DataRow"/>.
        /// </summary>
        IErrorsInfo Apply(IDMEEditor editor, string datasourceName, string entityName,
                          System.Data.DataRow row, IPassedArgs context = null);

        // ── Resolve ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves a single rule string.
        /// Prepend ":" to indicate an expression; omit it for a literal.
        /// </summary>
        object Resolve(IDMEEditor editor, string ruleString, IPassedArgs context = null);

        /// <summary>
        /// Tests a rule string and returns the resolved value together with error info.
        /// Useful for validation UI and testing tooling.
        /// </summary>
        (IErrorsInfo result, object value) TestRule(IDMEEditor editor, string rule, IPassedArgs parameters = null);

        /// <summary>Validates the syntax of a rule string without executing it.</summary>
        IErrorsInfo ValidateRule(IDMEEditor editor, string rule);

        // ── Resolver management ───────────────────────────────────────────────────

        /// <summary>Registers a custom resolver into the resolver pipeline.</summary>
        void RegisterCustomResolver(IDMEEditor editor, IDefaultValueResolver resolver);

        /// <summary>Returns a map of resolver name → supported rule token examples.</summary>
        Dictionary<string, IEnumerable<string>> GetAvailableResolvers(IDMEEditor editor);

        // ── Legacy / persistence API ──────────────────────────────────────────────
        // These methods delegate to DefaultValueHelper and work with persisted
        // DefaultValue entries stored on ConnectionProperties.DatasourceDefaults.

        List<DefaultValue> GetDefaults(IDMEEditor editor, string dataSourceName);
        IErrorsInfo SaveDefaults(IDMEEditor editor, List<DefaultValue> defaults, string dataSourceName);

        IErrorsInfo SetColumnDefault(IDMEEditor editor, string dataSourceName, string entityName,
                                     string columnName, string defaultValue, bool isRule = false);
        object GetColumnDefault(IDMEEditor editor, string dataSourceName, string entityName,
                                string columnName, IPassedArgs parameters = null);
        IErrorsInfo RemoveColumnDefault(IDMEEditor editor, string dataSourceName,
                                        string entityName, string columnName);
        Dictionary<string, DefaultValue> GetEntityDefaults(IDMEEditor editor, string dataSourceName, string entityName);
        IErrorsInfo SetMultipleColumnDefaults(IDMEEditor editor, string dataSourceName, string entityName,
                                              Dictionary<string, (string value, bool isRule)> columnDefaults);

        object ResolveDefaultValue(IDMEEditor editor, DefaultValue defaultValue, IPassedArgs parameters);
        object ResolveDefaultValue(IDMEEditor editor, string dataSourceName, string fieldName, IPassedArgs parameters);

        (IErrorsInfo validation, DefaultValue defaultValue) CreateDefaultValue(
            IDMEEditor editor, string fieldName, string value, string rule = null);
        IErrorsInfo ValidateDefaultValue(IDMEEditor editor, DefaultValue defaultValue);
    }
}
