using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Defaults
{
    public partial class DefaultsManager
    {
        /// <summary>Applies the registered profile to a Dictionary&lt;string,object&gt; record.</summary>
        private static IErrorsInfo ApplyInstance(IDMEEditor editor, string datasource, string entity,
            Dictionary<string, object> record, IPassedArgs context)
        {
            EnsureInitialized(editor);
            var profile = GetProfile(datasource, entity);
            if (profile == null)
                return new ErrorsInfo { Flag = Errors.Ok, Message = "No profile registered for this entity." };

            var errors = new List<string>();
            foreach (var rule in profile.Rules)
            {
                try
                {
                    if (rule.ApplyOnlyIfNull)
                    {
                        if (record.TryGetValue(rule.FieldName, out var existing)
                            && existing != null
                            && !string.IsNullOrEmpty(existing.ToString()))
                            continue;
                    }
                    record[rule.FieldName] = Resolve(editor, rule.RuleString, context);
                }
                catch (Exception ex)
                {
                    errors.Add($"{rule.FieldName}: {ex.Message}");
                }
            }

            return errors.Any()
                ? new ErrorsInfo { Flag = Errors.Warning, Message = string.Join("; ", errors) }
                : new ErrorsInfo { Flag = Errors.Ok };
        }

        /// <summary>Applies the registered profile to a POCO via reflection.</summary>
        private static IErrorsInfo ApplyInstance<T>(IDMEEditor editor, string datasource, string entity,
            T poco, IPassedArgs context) where T : class
        {
            EnsureInitialized(editor);
            if (poco == null) return CreateError("POCO instance cannot be null.");

            var profile = GetProfile(datasource, entity);
            if (profile == null)
                return new ErrorsInfo { Flag = Errors.Ok, Message = "No profile registered for this entity." };

            var props = typeof(T)
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            var errors = new List<string>();
            foreach (var rule in profile.Rules)
            {
                if (!props.TryGetValue(rule.FieldName, out var prop))
                    continue;
                try
                {
                    if (rule.ApplyOnlyIfNull)
                    {
                        var current = prop.GetValue(poco);
                        if (current != null && !(current is string s && string.IsNullOrEmpty(s)))
                            continue;
                    }
                    var value = Resolve(editor, rule.RuleString, context);
                    prop.SetValue(poco, ConvertValue(value, prop.PropertyType));
                }
                catch (Exception ex)
                {
                    errors.Add($"{rule.FieldName}: {ex.Message}");
                }
            }

            return errors.Any()
                ? new ErrorsInfo { Flag = Errors.Warning, Message = string.Join("; ", errors) }
                : new ErrorsInfo { Flag = Errors.Ok };
        }

        /// <summary>Applies the registered profile to a DataRow.</summary>
        private static IErrorsInfo ApplyInstance(IDMEEditor editor, string datasource, string entity,
            System.Data.DataRow row, IPassedArgs context)
        {
            EnsureInitialized(editor);
            if (row == null) return CreateError("DataRow cannot be null.");

            var profile = GetProfile(datasource, entity);
            if (profile == null)
                return new ErrorsInfo { Flag = Errors.Ok, Message = "No profile registered for this entity." };

            var errors = new List<string>();
            foreach (var rule in profile.Rules)
            {
                if (!row.Table.Columns.Contains(rule.FieldName))
                    continue;
                try
                {
                    if (rule.ApplyOnlyIfNull
                        && row[rule.FieldName] != DBNull.Value
                        && !string.IsNullOrEmpty(row[rule.FieldName]?.ToString()))
                        continue;

                    var value = Resolve(editor, rule.RuleString, context);
                    row[rule.FieldName] = value ?? DBNull.Value;
                }
                catch (Exception ex)
                {
                    errors.Add($"{rule.FieldName}: {ex.Message}");
                }
            }

            return errors.Any()
                ? new ErrorsInfo { Flag = Errors.Warning, Message = string.Join("; ", errors) }
                : new ErrorsInfo { Flag = Errors.Ok };
        }
    }
}
