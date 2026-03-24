using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Interfaces
{
    /// <summary>
    /// Validation contract for default-value rules, field names, and full definitions.
    /// </summary>
    public interface IDefaultValueValidationHelper
    {
        /// <summary>Validates a full <see cref="DefaultValue"/> definition (field name + rule + value).</summary>
        IErrorsInfo ValidateDefaultValue(DefaultValue defaultValue);

        /// <summary>Validates that <paramref name="rule"/> is syntactically and semantically correct.</summary>
        IErrorsInfo ValidateRule(string rule);

        /// <summary>Validates that <paramref name="fieldName"/> exists in <paramref name="dataSourceName"/>.</summary>
        IErrorsInfo ValidateFieldName(string dataSourceName, string fieldName);
    }
}
