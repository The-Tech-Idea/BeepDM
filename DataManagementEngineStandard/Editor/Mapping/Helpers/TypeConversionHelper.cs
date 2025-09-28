using System;
using System.Globalization;

namespace TheTechIdea.Beep.Editor.Mapping.Helpers
{
    /// <summary>
    /// Helper class for type conversions in mapping operations
    /// Centralized conversion that understands Nullable types, enums, and invariant culture numerics/DateTimes
    /// </summary>
    public static class TypeConversionHelper
    {
        /// <summary>
        /// Attempts to convert a value to the target type
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="targetType">The target type to convert to</param>
        /// <returns>The converted value</returns>
        public static object TryConvert(object value, Type targetType)
        {
            if (value == null) 
                return GetDefaultValue(targetType);

            var nonNullable = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // If already assignable
            if (nonNullable.IsInstanceOfType(value))
                return value;

            // Handle specific type conversions
            if (TryConvertEnum(value, nonNullable, out var enumResult))
                return enumResult;

            if (TryConvertGuid(value, nonNullable, out var guidResult))
                return guidResult;

            if (TryConvertDateTime(value, nonNullable, out var dateTimeResult))
                return dateTimeResult;

            if (TryConvertString(value, nonNullable, out var stringResult))
                return stringResult;

            if (TryConvertBoolean(value, nonNullable, out var boolResult))
                return boolResult;

            // Fallback to ChangeType with invariant culture
            return TryChangeType(value, nonNullable);
        }

        /// <summary>
        /// Gets the default value for a type
        /// </summary>
        private static object GetDefaultValue(Type targetType)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                return Activator.CreateInstance(targetType);
            return null;
        }

        /// <summary>
        /// Attempts to convert to enum type
        /// </summary>
        private static bool TryConvertEnum(object value, Type targetType, out object result)
        {
            result = null;
            if (!targetType.IsEnum)
                return false;

            try
            {
                if (value is string s)
                {
                    result = Enum.Parse(targetType, s, ignoreCase: true);
                    return true;
                }

                result = Enum.ToObject(targetType, System.Convert.ChangeType(value, Enum.GetUnderlyingType(targetType), CultureInfo.InvariantCulture));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to convert to Guid type
        /// </summary>
        private static bool TryConvertGuid(object value, Type targetType, out object result)
        {
            result = null;
            if (targetType != typeof(Guid))
                return false;

            if (value is Guid g)
            {
                result = g;
                return true;
            }

            if (value is string sg && Guid.TryParse(sg, out var parsed))
            {
                result = parsed;
                return true;
            }

            result = Guid.Empty;
            return true;
        }

        /// <summary>
        /// Attempts to convert to DateTime type
        /// </summary>
        private static bool TryConvertDateTime(object value, Type targetType, out object result)
        {
            result = null;
            if (targetType != typeof(DateTime))
                return false;

            if (value is DateTime dt)
            {
                result = dt;
                return true;
            }

            if (value is string sd && DateTime.TryParse(sd, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDt))
            {
                result = parsedDt;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to convert to string type
        /// </summary>
        private static bool TryConvertString(object value, Type targetType, out object result)
        {
            result = null;
            if (targetType != typeof(string))
                return false;

            result = value.ToString();
            return true;
        }

        /// <summary>
        /// Attempts to convert to boolean type
        /// </summary>
        private static bool TryConvertBoolean(object value, Type targetType, out object result)
        {
            result = null;
            if (targetType != typeof(bool))
                return false;

            if (value is bool b)
            {
                result = b;
                return true;
            }

            if (value is string s)
            {
                if (bool.TryParse(s, out var parsed))
                {
                    result = parsed;
                    return true;
                }

                // Handle common string representations
                var lower = s.ToLowerInvariant();
                if (lower == "1" || lower == "yes" || lower == "y" || lower == "on")
                {
                    result = true;
                    return true;
                }
                if (lower == "0" || lower == "no" || lower == "n" || lower == "off")
                {
                    result = false;
                    return true;
                }
            }

            if (IsNumericType(value.GetType()))
            {
                try
                {
                    var numericValue = System.Convert.ToDouble(value);
                    result = numericValue != 0;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Fallback conversion using ChangeType
        /// </summary>
        private static object TryChangeType(object value, Type targetType)
        {
            try
            {
                return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return value; // last resort, assignment may still work if reference type
            }
        }

        /// <summary>
        /// Checks if a type is numeric
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}