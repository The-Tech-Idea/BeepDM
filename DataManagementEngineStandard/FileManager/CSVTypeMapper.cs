// Create a new file: ../BeepDM/DataManagementEngineStandard/FileManager/CSVTypeMapper.cs
using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Provides type conversion utilities for CSV data
    /// </summary>
    public static class CSVTypeMapper
    {
        private static readonly Dictionary<Type, string> _typeToCSVType = new Dictionary<Type, string>
        {
            { typeof(byte), "integer" },
            { typeof(sbyte), "integer" },
            { typeof(short), "integer" },
            { typeof(ushort), "integer" },
            { typeof(int), "integer" },
            { typeof(uint), "integer" },
            { typeof(long), "integer" },
            { typeof(ulong), "integer" },
            { typeof(float), "number" },
            { typeof(double), "number" },
            { typeof(decimal), "number" },
            { typeof(bool), "boolean" },
            { typeof(DateTime), "datetime" },
            { typeof(DateTimeOffset), "datetime" },
            { typeof(TimeSpan), "timespan" },
            { typeof(Guid), "guid" },
            { typeof(string), "string" },
            { typeof(char), "char" }
        };

        private static readonly Dictionary<string, Type> _csvTypeToNetType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "integer", typeof(int) },
            { "int", typeof(int) },
            { "number", typeof(double) },
            { "decimal", typeof(decimal) },
            { "float", typeof(float) },
            { "double", typeof(double) },
            { "boolean", typeof(bool) },
            { "bool", typeof(bool) },
            { "datetime", typeof(DateTime) },
            { "date", typeof(DateTime) },
            { "time", typeof(DateTime) },
            { "guid", typeof(Guid) },
            { "string", typeof(string) },
            { "char", typeof(char) },
            { "timespan", typeof(TimeSpan) }
        };

        /// <summary>
        /// Maps a .NET type to a CSV field type string
        /// </summary>
        /// <param name="dotNetType">The .NET type to map</param>
        /// <returns>CSV type string</returns>
        public static string MapToCSVType(Type dotNetType)
        {
            if (dotNetType == null)
                return "string";

            if (_typeToCSVType.TryGetValue(dotNetType, out string csvType))
                return csvType;

            if (dotNetType.IsEnum)
                return "string";

            return "string";
        }

        /// <summary>
        /// Maps a CSV field type string to a .NET type
        /// </summary>
        /// <param name="csvType">CSV type string</param>
        /// <returns>.NET type</returns>
        public static Type MapToNetType(string csvType)
        {
            if (string.IsNullOrEmpty(csvType))
                return typeof(string);

            if (_csvTypeToNetType.TryGetValue(csvType, out Type netType))
                return netType;

            return typeof(string);
        }

        /// <summary>
        /// Converts a string value to the appropriate .NET type
        /// </summary>
        /// <param name="value">String value to convert</param>
        /// <param name="targetType">Target .NET type</param>
        /// <returns>Converted value</returns>
        public static object ConvertValue(string value, Type targetType)
        {
            // 1) Normalize input
            value = value?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                // empty → default(T) or null
                if (Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType)
                    return null;
                return Activator.CreateInstance(targetType);
            }

            // 2) Unwrap Nullable<T>
            var underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                var conv = ConvertValue(value, underlying);
                return conv;
            }

            // 3) Common strong types
            if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out var b)) return b;
                var v = value.ToLower();
                if (v == "1" || v == "yes" || v == "y" || v == "true") return true;
                if (v == "0" || v == "no" || v == "n" || v == "false") return false;
                return false;
            }

            if (targetType == typeof(DateTime))
            {
                if (DateTime.TryParse(value, out var dt)) return dt;
                return DateTime.MinValue;
            }

            if (targetType == typeof(TimeSpan))
            {
                if (TimeSpan.TryParse(value, out var ts)) return ts;
                return TimeSpan.Zero;
            }

            if (targetType == typeof(Guid))
            {
                if (Guid.TryParse(value, out var g)) return g;
                return Guid.Empty;
            }

            // 4) Numeric types via TypeCode
            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.Byte:
                    if (byte.TryParse(value, out var b1)) return b1;
                    if (decimal.TryParse(value, out var d1)) return (byte)Math.Truncate(d1);
                    return default(byte);

                case TypeCode.SByte:
                    if (sbyte.TryParse(value, out var sb)) return sb;
                    if (decimal.TryParse(value, out var d2)) return (sbyte)Math.Truncate(d2);
                    return default(sbyte);

                case TypeCode.Int16:
                    if (short.TryParse(value, out var s2)) return s2;
                    if (decimal.TryParse(value, out var d3)) return (short)Math.Truncate(d3);
                    return default(short);

                case TypeCode.UInt16:
                    if (ushort.TryParse(value, out var us)) return us;
                    if (decimal.TryParse(value, out var d4)) return (ushort)Math.Truncate(d4);
                    return default(ushort);

                case TypeCode.Int32:
                    if (int.TryParse(value, out var i)) return i;
                    if (decimal.TryParse(value, out var d5)) return (int)Math.Truncate(d5);
                    return default(int);

                case TypeCode.UInt32:
                    if (uint.TryParse(value, out var ui)) return ui;
                    if (decimal.TryParse(value, out var d6)) return (uint)Math.Truncate(d6);
                    return default(uint);

                case TypeCode.Int64:
                    if (long.TryParse(value, out var l)) return l;
                    if (decimal.TryParse(value, out var d7)) return (long)Math.Truncate(d7);
                    return default(long);

                case TypeCode.UInt64:
                    if (ulong.TryParse(value, out var ul)) return ul;
                    if (decimal.TryParse(value, out var d8)) return (ulong)Math.Truncate(d8);
                    return default(ulong);

                case TypeCode.Single:
                    if (float.TryParse(value, out var f)) return f;
                    return default(float);

                case TypeCode.Double:
                    if (double.TryParse(value, out var dbl)) return dbl;
                    return default(double);

                case TypeCode.Decimal:
                    if (decimal.TryParse(value, out var dec)) return dec;
                    return default(decimal);
            }

            // 5) Enumerations
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value, ignoreCase: true);
            }

            // 6) Strings
            if (targetType == typeof(string))
            {
                return value;
            }

            // 7) Fallback
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                // give up and return default(T)
                return Activator.CreateInstance(targetType);
            }
        }


        /// <summary>
        /// Formats a value for CSV output
        /// </summary>
        /// <param name="value">The value to format</param>
        /// <param name="sourceType">The source type</param>
        /// <returns>Formatted string value</returns>
        public static string FormatValueForCSV(object value, Type sourceType = null)
        {
            if (value == null)
                return string.Empty;

            if (sourceType == null)
                sourceType = value.GetType();

            // Handle special cases
            if (sourceType == typeof(DateTime))
            {
                // ISO 8601 format (preferred for data interchange)
                return ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss");
            }

            if (sourceType == typeof(DateTimeOffset))
            {
                // ISO 8601 format with offset
                return ((DateTimeOffset)value).ToString("yyyy-MM-ddTHH:mm:sszzz");
            }

            if (sourceType == typeof(bool))
            {
                // Use TRUE/FALSE for boolean values
                return ((bool)value) ? "TRUE" : "FALSE";
            }

            return value.ToString();
        }

        /// <summary>
        /// Returns the appropriate C# type name for a given CSV field type
        /// </summary>
        /// <param name="csvType">CSV field type</param>
        /// <returns>C# type name</returns>
        public static string GetCSharpTypeName(string csvType)
        {
            var netType = MapToNetType(csvType);
            return netType.ToString();
        }
    }
}
