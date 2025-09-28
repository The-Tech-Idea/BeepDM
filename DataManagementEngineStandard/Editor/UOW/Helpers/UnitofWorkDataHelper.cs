using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Interfaces;

namespace TheTechIdea.Beep.Editor.UOW.Helpers
{
    /// <summary>
    /// Helper class for data operations in UnitofWork
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class UnitofWorkDataHelper<T> : IUnitofWorkDataHelper<T> where T : Entity, new()
    {
        #region Private Fields

        private readonly IDMEEditor _editor;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of UnitofWorkDataHelper
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        public UnitofWorkDataHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clones an entity
        /// </summary>
        /// <param name="entity">Entity to clone</param>
        /// <returns>Cloned entity</returns>
        public T CloneEntity(T entity)
        {
            if (entity == null) return null;

            try
            {
                var clonedEntity = new T();
                var entityType = typeof(T);

                foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanRead && property.CanWrite)
                    {
                        var value = property.GetValue(entity);
                        
                        // Handle deep cloning for complex types if needed
                        if (value != null && IsComplexType(property.PropertyType))
                        {
                            value = CloneComplexValue(value);
                        }
                        
                        property.SetValue(clonedEntity, value);
                    }
                }

                return clonedEntity;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDataHelper", 
                    $"Error cloning entity: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Converts an object to the entity type
        /// </summary>
        /// <param name="source">Source object</param>
        /// <returns>Converted entity</returns>
        public T ConvertToEntity(object source)
        {
            if (source == null) return null;

            try
            {
                // If source is already of type T, return it
                if (source is T entity)
                {
                    return entity;
                }

                var targetEntity = new T();
                var sourceType = source.GetType();
                var targetType = typeof(T);

                // Map properties from source to target
                foreach (var targetProperty in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!targetProperty.CanWrite) continue;

                    var sourceProperty = sourceType.GetProperty(targetProperty.Name, 
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if (sourceProperty != null && sourceProperty.CanRead)
                    {
                        var sourceValue = sourceProperty.GetValue(source);
                        if (sourceValue != null)
                        {
                            var convertedValue = ConvertValue(sourceValue, targetProperty.PropertyType);
                            if (convertedValue != null)
                            {
                                targetProperty.SetValue(targetEntity, convertedValue);
                            }
                        }
                    }
                }

                return targetEntity;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDataHelper", 
                    $"Error converting object to entity: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Gets entity values as dictionary
        /// </summary>
        /// <param name="entity">Entity to extract values from</param>
        /// <returns>Dictionary of property names and values</returns>
        public Dictionary<string, object> GetEntityValues(T entity)
        {
            var values = new Dictionary<string, object>();

            if (entity == null) return values;

            try
            {
                var entityType = typeof(T);
                
                foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanRead)
                    {
                        var value = property.GetValue(entity);
                        values[property.Name] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDataHelper", 
                    $"Error getting entity values: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            return values;
        }

        /// <summary>
        /// Sets entity values from dictionary
        /// </summary>
        /// <param name="entity">Entity to set values on</param>
        /// <param name="values">Dictionary of property names and values</param>
        public void SetEntityValues(T entity, Dictionary<string, object> values)
        {
            if (entity == null || values == null) return;

            try
            {
                var entityType = typeof(T);

                foreach (var kvp in values)
                {
                    var property = entityType.GetProperty(kvp.Key, 
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if (property != null && property.CanWrite)
                    {
                        var convertedValue = ConvertValue(kvp.Value, property.PropertyType);
                        if (convertedValue != null || property.PropertyType.IsNullableType())
                        {
                            property.SetValue(entity, convertedValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDataHelper", 
                    $"Error setting entity values: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Compares two entities for changes
        /// </summary>
        /// <param name="original">Original entity</param>
        /// <param name="current">Current entity</param>
        /// <returns>Dictionary of changed fields</returns>
        public Dictionary<string, (object oldValue, object newValue)> CompareEntities(T original, T current)
        {
            var changes = new Dictionary<string, (object oldValue, object newValue)>();

            if (original == null && current == null) return changes;
            if (original == null || current == null)
            {
                // Handle case where one entity is null
                var entityType = typeof(T);
                foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanRead)
                    {
                        var originalValue = original?.GetType().GetProperty(property.Name)?.GetValue(original);
                        var currentValue = current?.GetType().GetProperty(property.Name)?.GetValue(current);
                        changes[property.Name] = (originalValue, currentValue);
                    }
                }
                return changes;
            }

            try
            {
                var entityType = typeof(T);

                foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanRead)
                    {
                        var originalValue = property.GetValue(original);
                        var currentValue = property.GetValue(current);

                        if (!AreValuesEqual(originalValue, currentValue))
                        {
                            changes[property.Name] = (originalValue, currentValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDataHelper", 
                    $"Error comparing entities: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            return changes;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if a type is a complex type that needs deep cloning
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if complex type</returns>
        private bool IsComplexType(Type type)
        {
            // Consider complex types as classes (excluding strings and primitives)
            return type.IsClass && 
                   type != typeof(string) && 
                   !type.IsPrimitive && 
                   !type.IsEnum &&
                   type != typeof(DateTime) &&
                   type != typeof(decimal);
        }

        /// <summary>
        /// Clones a complex value
        /// </summary>
        /// <param name="value">Value to clone</param>
        /// <returns>Cloned value</returns>
        private object CloneComplexValue(object value)
        {
            if (value == null) return null;

            try
            {
                var valueType = value.GetType();

                // Handle collections
                if (value is System.Collections.IEnumerable enumerable && valueType != typeof(string))
                {
                    return CloneEnumerable(enumerable, valueType);
                }

                // Handle objects with parameterless constructor
                if (valueType.GetConstructor(Type.EmptyTypes) != null)
                {
                    var clonedObject = Activator.CreateInstance(valueType);
                    
                    foreach (var property in valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (property.CanRead && property.CanWrite)
                        {
                            var propertyValue = property.GetValue(value);
                            if (IsComplexType(property.PropertyType))
                            {
                                propertyValue = CloneComplexValue(propertyValue);
                            }
                            property.SetValue(clonedObject, propertyValue);
                        }
                    }
                    
                    return clonedObject;
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDataHelper", 
                    $"Error cloning complex value: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            // If cloning fails, return original value
            return value;
        }

        /// <summary>
        /// Clones an enumerable collection
        /// </summary>
        /// <param name="enumerable">Enumerable to clone</param>
        /// <param name="enumerableType">Type of enumerable</param>
        /// <returns>Cloned enumerable</returns>
        private object CloneEnumerable(System.Collections.IEnumerable enumerable, Type enumerableType)
        {
            try
            {
                // Handle List<T>
                if (enumerableType.IsGenericType && enumerableType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = enumerableType.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var clonedList = Activator.CreateInstance(listType);
                    var addMethod = listType.GetMethod("Add");

                    foreach (var item in enumerable)
                    {
                        var clonedItem = IsComplexType(elementType) ? CloneComplexValue(item) : item;
                        addMethod.Invoke(clonedList, new[] { clonedItem });
                    }

                    return clonedList;
                }

                // Handle arrays
                if (enumerableType.IsArray)
                {
                    var elementType = enumerableType.GetElementType();
                    var items = enumerable.Cast<object>().ToArray();
                    var clonedArray = Array.CreateInstance(elementType, items.Length);

                    for (int i = 0; i < items.Length; i++)
                    {
                        var clonedItem = IsComplexType(elementType) ? CloneComplexValue(items[i]) : items[i];
                        clonedArray.SetValue(clonedItem, i);
                    }

                    return clonedArray;
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDataHelper", 
                    $"Error cloning enumerable: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }

            return enumerable; // Return original if cloning fails
        }

        /// <summary>
        /// Converts a value to the target type
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="targetType">Target type</param>
        /// <returns>Converted value</returns>
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsAssignableFrom(value.GetType())) return value;

            try
            {
                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
                }

                // Handle specific conversions
                if (targetType == typeof(Guid))
                {
                    if (value is string stringValue)
                    {
                        return Guid.TryParse(stringValue, out var guid) ? guid : Guid.Empty;
                    }
                }
                else if (targetType.IsEnum)
                {
                    if (value is string enumString)
                    {
                        return Enum.TryParse(targetType, enumString, true, out var enumValue) ? enumValue : null;
                    }
                    else if (value.GetType().IsNumericType())
                    {
                        return Enum.ToObject(targetType, value);
                    }
                }
                else if (targetType == typeof(DateTime))
                {
                    if (value is string dateString)
                    {
                        return DateTime.TryParse(dateString, out var dateValue) ? dateValue : DateTime.MinValue;
                    }
                }

                // Default conversion
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("UnitofWorkDataHelper", 
                    $"Error converting value '{value}' to type '{targetType}': {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Checks if two values are equal
        /// </summary>
        /// <param name="value1">First value</param>
        /// <param name="value2">Second value</param>
        /// <returns>True if values are equal</returns>
        private bool AreValuesEqual(object value1, object value2)
        {
            if (ReferenceEquals(value1, value2)) return true;
            if (value1 == null || value2 == null) return false;

            // Handle special cases
            if (value1 is DateTime date1 && value2 is DateTime date2)
            {
                // Compare dates with millisecond precision
                return Math.Abs((date1 - date2).TotalMilliseconds) < 1;
            }

            if (value1 is decimal dec1 && value2 is decimal dec2)
            {
                // Compare decimals with precision handling
                return Math.Abs(dec1 - dec2) < 0.0001m;
            }

            if (value1 is double double1 && value2 is double double2)
            {
                // Compare doubles with epsilon
                return Math.Abs(double1 - double2) < double.Epsilon;
            }

            if (value1 is float float1 && value2 is float float2)
            {
                // Compare floats with epsilon
                return Math.Abs(float1 - float2) < float.Epsilon;
            }

            // Default comparison
            return value1.Equals(value2);
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for type checking
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Checks if a type is nullable
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if nullable</returns>
        public static bool IsNullableType(this Type type)
        {
            return !type.IsValueType || 
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        /// <summary>
        /// Checks if a type is numeric
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if numeric</returns>
        public static bool IsNumericType(this Type type)
        {
            var numericTypes = new[]
            {
                typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
                typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(decimal)
            };

            return numericTypes.Contains(type) || 
                   numericTypes.Contains(Nullable.GetUnderlyingType(type));
        }
    }
}