using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Util Methods"
        private T GetItemFromRow(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = new T();
            var properties = temp.GetProperties();

            foreach (var property in properties)
            {
                if (dr.Table.Columns.Contains(property.Name))
                {
                    try
                    {
                        var value = dr[property.Name];
                        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                        if (value == DBNull.Value)
                        {
                            value = property.PropertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
                        }
                        else if (propertyType == typeof(char) && value is string str && str.Length == 1)
                        {
                            value = str[0];
                        }
                        else if (propertyType.IsEnum && value is string enumString)
                        {
                            value = Enum.Parse(propertyType, enumString);
                        }
                        else if (IsNumericType(propertyType))
                        {
                            value = ConvertToNumericType(value, propertyType);
                        }
                        else
                        {
                            value = Convert.ChangeType(value, propertyType);
                        }

                        property.SetValue(obj, value);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidCastException($"Cannot convert column '{property.Name}' value to property '{property.Name}' of type '{property.PropertyType}': {ex.Message}", ex);
                    }
                }
            }

            return obj;
        }

        private object ConvertToNumericType(object value, Type targetType)
        {
            try
            {
                if (targetType == typeof(byte)) return Convert.ToByte(value);
                if (targetType == typeof(sbyte)) return Convert.ToSByte(value);
                if (targetType == typeof(short)) return Convert.ToInt16(value);
                if (targetType == typeof(ushort)) return Convert.ToUInt16(value);
                if (targetType == typeof(int)) return Convert.ToInt32(value);
                if (targetType == typeof(uint)) return Convert.ToUInt32(value);
                if (targetType == typeof(long)) return Convert.ToInt64(value);
                if (targetType == typeof(ulong)) return Convert.ToUInt64(value);
                if (targetType == typeof(float)) return Convert.ToSingle(value);
                if (targetType == typeof(double)) return Convert.ToDouble(value);
                if (targetType == typeof(decimal)) return Convert.ToDecimal(value);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot convert value '{value}' to numeric type '{targetType}': {ex.Message}", ex);
            }
            return value;
        }
        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }
        #endregion
    }
}
