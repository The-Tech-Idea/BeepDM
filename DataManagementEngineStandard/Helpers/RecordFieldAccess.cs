using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Helpers
{
    internal static class RecordFieldAccess
    {
        public static object ReadRequired(object record, string field)
        {
            if (record is IDictionary<string, object> values)
            {
                var key = FindKey(values, field);
                if (key == null) throw new InvalidOperationException("Required field is missing.");
                return values[key] == DBNull.Value ? null : values[key];
            }
            if (record is DataRow row)
            {
                var columns = row.Table.Columns.Cast<DataColumn>().Where(column =>
                    string.Equals(column.ColumnName, field, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (columns.Length != 1) throw new InvalidOperationException("Required field is missing or ambiguous.");
                return row[columns[0]] == DBNull.Value ? null : row[columns[0]];
            }
            return Read(record, field);
        }

        public static object Read(object record, string field)
        {
            if (record is IDictionary<string, object> values)
            {
                var key = FindKey(values, field);
                return key == null ? null : values[key];
            }
            if (record is DataRow row) return row[field] == DBNull.Value ? null : row[field];
            return Property(record, field).GetValue(record);
        }

        public static void Write(object record, string field, object value)
        {
            if (record is IDictionary<string, object> values)
            {
                values[FindKey(values, field) ?? field] = value;
                return;
            }
            if (record is DataRow row)
            {
                row[field] = ConvertValue(value, row.Table.Columns[field].DataType) ?? DBNull.Value;
                return;
            }
            var property = Property(record, field);
            if (property.SetMethod?.IsPublic != true)
                throw new InvalidOperationException("Required field is not writable.");
            property.SetValue(record, ConvertValue(value, property.PropertyType));
        }

        private static string FindKey(IDictionary<string, object> values, string field)
        {
            if (string.IsNullOrWhiteSpace(field)) throw new InvalidOperationException("Required field is missing.");
            var keys = values.Keys.Where(key => string.Equals(key, field, StringComparison.OrdinalIgnoreCase)).ToList();
            if (keys.Count > 1) throw new InvalidOperationException("Required field is ambiguous.");
            return keys.SingleOrDefault();
        }

        private static PropertyInfo Property(object record, string field)
        {
            if (record == null || string.IsNullOrWhiteSpace(field))
                throw new InvalidOperationException("Required field is missing.");
            var property = record.GetType().GetProperty(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property?.GetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0)
                throw new InvalidOperationException("Required field is not readable.");
            return property;
        }

        private static object ConvertValue(object value, Type target)
        {
            if (value == null || value == DBNull.Value)
            {
                if (target.IsValueType && Nullable.GetUnderlyingType(target) == null)
                    throw new InvalidOperationException("Required value cannot be null.");
                return null;
            }
            target = Nullable.GetUnderlyingType(target) ?? target;
            if (target.IsInstanceOfType(value)) return value;
            if (target == typeof(Guid)) return Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture));
            if (target.IsEnum) return value is string text ? Enum.Parse(target, text, true) : Enum.ToObject(target, value);
            return Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
    }
}
