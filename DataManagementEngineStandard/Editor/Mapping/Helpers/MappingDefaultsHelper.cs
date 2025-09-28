using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;

namespace TheTechIdea.Beep.Editor.Mapping.Helpers
{
    /// <summary>
    /// Helper utilities to apply DefaultsManager rules and static defaults to mapped objects.
    /// </summary>
    public static class MappingDefaultsHelper
    {
        /// <summary>
        /// Apply default values (static and rule-based) to a destination object based on DefaultsManager configuration.
        /// Only applies a default when the current value is null (or default for value types).
        /// </summary>
        /// <param name="editor">Editor instance</param>
        /// <param name="destDataSourceName">Destination data source name</param>
        /// <param name="destEntityName">Destination entity name</param>
        /// <param name="destination">Destination object to set values on</param>
        /// <param name="destFields">Destination entity fields metadata</param>
        public static void ApplyDefaultsToObject(IDMEEditor editor, string destDataSourceName, string destEntityName, object destination, IEnumerable<EntityField> destFields)
        {
            if (editor == null || destination == null || string.IsNullOrWhiteSpace(destDataSourceName))
                return;

            List<DefaultValue> defaults = null;
            try
            {
                defaults = DefaultsManager.GetDefaults(editor, destDataSourceName) ?? new List<DefaultValue>();
            }
            catch
            {
                defaults = new List<DefaultValue>();
            }

            if (defaults.Count == 0)
                return;

            var destFieldNames = (destFields ?? Array.Empty<EntityField>())
                .Select(f => f.fieldname)
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            foreach (var def in defaults)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.PropertyName))
                    continue;

                if (destFieldNames.Count > 0 && !destFieldNames.Contains(def.PropertyName))
                    continue; // not part of entity

                try
                {
                    var current = editor.Utilfunction.GetFieldValueFromObject(def.PropertyName, destination);
                    if (!IsNullOrDefault(current))
                        continue; // do not override explicitly set values

                    object valueToSet = null;

                    // If it's explicitly static OR a non-null PropertyValue is provided without a rule, use it as-is
                    if (def.IsStatic || (def.PropertyValue is not null && string.IsNullOrWhiteSpace(def.Rule)))
                    {
                        valueToSet = def.PropertyValue;
                    }
                    else
                    {
                        // Resolve through DefaultsManager rule engine
                        var args = new PassedArgs
                        {
                            ParameterString1 = def.Rule,
                            SentData = def,
                            ObjectName = "Default",
                            CurrentEntity = destEntityName,
                            DatasourceName = destDataSourceName
                        };
                        valueToSet = DefaultsManager.ResolveDefaultValue(editor, destDataSourceName, def.PropertyName, args);
                    }

                    editor.Utilfunction.SetFieldValueFromObject(def.PropertyName, destination, valueToSet);
                }
                catch
                {
                    // Swallow per-field default issues and proceed
                }
            }
        }

        private static bool IsNullOrDefault(object value)
        {
            if (value == null || value == DBNull.Value)
                return true;

            var type = value.GetType();
            if (!type.IsValueType)
                return false; // reference that is not null => not default

            return value.Equals(Activator.CreateInstance(type));
        }
    }
}
