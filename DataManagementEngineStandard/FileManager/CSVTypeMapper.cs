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
            if (string.IsNullOrEmpty(value))
            {
                // Handle special case for value types
                if (targetType.IsValueType)
                    return Activator.CreateInstance(targetType);
                return null;
            }

            try
            {
                // Handle common types with special cases
                if (targetType == typeof(bool))
                {
                    if (bool.TryParse(value, out bool result))
                        return result;

                    // Handle common boolean text representations
                    string lowerValue = value.ToLower().Trim();
                    if (lowerValue == "1" || lowerValue == "yes" || lowerValue == "y" || lowerValue == "true" || lowerValue == "t")
                        return true;
                    if (lowerValue == "0" || lowerValue == "no" || lowerValue == "n" || lowerValue == "false" || lowerValue == "f")
                        return false;

                    return false; // Default
                }

                if (targetType == typeof(DateTime))
                {
                    if (DateTime.TryParse(value, out DateTime result))
                        return result;
                    return DateTime.MinValue;
                }

                if (targetType == typeof(Guid))
                {
                    if (Guid.TryParse(value, out Guid result))
                        return result;
                    return Guid.Empty;
                }

                if (targetType == typeof(TimeSpan))
                {
                    if (TimeSpan.TryParse(value, out TimeSpan result))
                        return result;
                    return TimeSpan.Zero;
                }

                // Handle Nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(targetType);
                    return ConvertValue(value, underlyingType);
                }

                // Handle enumerations
                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, value, true);
                }

                // General case - use Convert
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                // Return default value on error
                if (targetType.IsValueType)
                    return Activator.CreateInstance(targetType);
                return null;
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
