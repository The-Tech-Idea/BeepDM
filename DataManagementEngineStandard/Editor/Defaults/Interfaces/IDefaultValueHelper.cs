using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Interfaces
{
    /// <summary>
    /// CRUD helper for <see cref="DefaultValue"/> objects tied to a named data source.
    /// Provides load, persist, search, factory, and lightweight validation operations.
    /// </summary>
    public interface IDefaultValueHelper
    {
        /// <summary>Returns all default definitions stored for the given data source.</summary>
        List<DefaultValue> GetDefaults(string dataSourceName);

        /// <summary>Persists the full list of defaults for the given data source.</summary>
        IErrorsInfo SaveDefaults(List<DefaultValue> defaults, string dataSourceName);

        /// <summary>Finds the default definition for a specific field, or null when absent.</summary>
        DefaultValue GetDefaultForField(string dataSourceName, string fieldName);

        /// <summary>
        /// Creates an in-memory <see cref="DefaultValue"/> without persisting it.
        /// <paramref name="rule"/> is optional; when null the literal <paramref name="value"/> is used.
        /// </summary>
        DefaultValue CreateDefaultValue(string fieldName, string value, string rule = null);

        /// <summary>Validates a single default definition and returns any errors found.</summary>
        IErrorsInfo ValidateDefaultValue(DefaultValue defaultValue);
    }
}
