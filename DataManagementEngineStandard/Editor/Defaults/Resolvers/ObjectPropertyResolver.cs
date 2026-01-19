using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Defaults.Resolvers
{
    /// <summary>
    /// Resolver for extracting values from passed objects using property/field names
    /// </summary>
    public class ObjectPropertyResolver : BaseDefaultValueResolver
    {
        public ObjectPropertyResolver(IDMEEditor editor) : base(editor) { }

        public override string ResolverName => "ObjectProperty";

        public override IEnumerable<string> SupportedRuleTypes => new[]
        {
            "PROPERTY", "FIELD", "OBJECTVALUE", "PARENTVALUE", "CHILDVALUE",
            "GETPROPERTY", "GETFIELD", "NESTED", "ARRAYITEM", "DICTVALUE"
        };

        public override object ResolveValue(string rule, IPassedArgs parameters)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return null;

            var upperRule = rule.ToUpperInvariant().Trim();

            try
            {
                return upperRule switch
                {
                    _ when upperRule.StartsWith("PROPERTY(") || upperRule.StartsWith("GETPROPERTY(") => HandleProperty(rule, parameters),
                    _ when upperRule.StartsWith("FIELD(") || upperRule.StartsWith("GETFIELD(") => HandleField(rule, parameters),
                    _ when upperRule.StartsWith("OBJECTVALUE(") => HandleObjectValue(rule, parameters),
                    _ when upperRule.StartsWith("PARENTVALUE(") => HandleParentValue(rule, parameters),
                    _ when upperRule.StartsWith("CHILDVALUE(") => HandleChildValue(rule, parameters),
                    _ when upperRule.StartsWith("NESTED(") => HandleNestedProperty(rule, parameters),
                    _ when upperRule.StartsWith("ARRAYITEM(") => HandleArrayItem(rule, parameters),
                    _ when upperRule.StartsWith("DICTVALUE(") => HandleDictionaryValue(rule, parameters),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving object property rule '{rule}'", ex);
                return null;
            }
        }

        public override bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();
            
            return upperRule.StartsWith("PROPERTY(") ||
                   upperRule.StartsWith("GETPROPERTY(") ||
                   upperRule.StartsWith("FIELD(") ||
                   upperRule.StartsWith("GETFIELD(") ||
                   upperRule.StartsWith("OBJECTVALUE(") ||
                   upperRule.StartsWith("PARENTVALUE(") ||
                   upperRule.StartsWith("CHILDVALUE(") ||
                   upperRule.StartsWith("NESTED(") ||
                   upperRule.StartsWith("ARRAYITEM(") ||
                   upperRule.StartsWith("DICTVALUE(");
        }

        public override IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "PROPERTY(Name) - Get Name property from passed object",
                "PROPERTY(Customer.Name) - Get Name from Customer property",
                "FIELD(ID) - Get ID field from passed object",
                "OBJECTVALUE(UserID) - Get UserID from current object",
                "PARENTVALUE(ParentID) - Get ParentID from parent object",
                "CHILDVALUE(Orders.Count) - Get Count from Orders child collection",
                "NESTED(Address.Street) - Get Street from nested Address object",
                "NESTED(User.Profile.Email) - Get Email from deeply nested object",
                "ARRAYITEM(Tags, 0) - Get first item from Tags array",
                "ARRAYITEM(Orders, -1) - Get last item from Orders array",
                "DICTVALUE(Settings, Theme) - Get Theme value from Settings dictionary"
            };
        }

        #region Handler Methods

        private object HandleProperty(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var propertyPath = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(propertyPath))
                {
                    LogError("PROPERTY requires a property name parameter");
                    return null;
                }

                var targetObject = GetTargetObject(parameters);
                if (targetObject == null)
                {
                    LogWarning("No target object available for property access");
                    return null;
                }

                return GetNestedPropertyValue(targetObject, propertyPath);
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleProperty", ex);
                return null;
            }
        }

        private object HandleField(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var FieldName = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(FieldName))
                {
                    LogError("FIELD requires a field name parameter");
                    return null;
                }

                var targetObject = GetTargetObject(parameters);
                if (targetObject == null)
                {
                    LogWarning("No target object available for field access");
                    return null;
                }

                return GetFieldValue(targetObject, FieldName);
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleField", ex);
                return null;
            }
        }

        private object HandleObjectValue(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var propertyName = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    LogError("OBJECTVALUE requires a property name parameter");
                    return null;
                }

                var targetObject = GetTargetObject(parameters);
                if (targetObject == null)
                {
                    LogWarning("No target object available");
                    return null;
                }

                // Try property first, then field
                var value = GetPropertyValue(targetObject, propertyName);
                if (value == null)
                {
                    value = GetFieldValue(targetObject, propertyName);
                }

                return value;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleObjectValue", ex);
                return null;
            }
        }

        private object HandleParentValue(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var propertyName = RemoveQuotes(content.Trim());

                // Try to get parent object - this would need to be passed in parameters
                var parentObject = GetParameterValue<object>(parameters, "ParentObject");
                if (parentObject == null)
                {
                    LogWarning("No parent object available");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    return parentObject;
                }

                return GetNestedPropertyValue(parentObject, propertyName);
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleParentValue", ex);
                return null;
            }
        }

        private object HandleChildValue(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 1)
                {
                    LogError("CHILDVALUE requires at least a collection name parameter");
                    return null;
                }

                var collectionName = RemoveQuotes(parts[0].Trim());
                var targetObject = GetTargetObject(parameters);

                if (targetObject == null)
                {
                    LogWarning("No target object available for child access");
                    return null;
                }

                var collection = GetPropertyValue(targetObject, collectionName);
                if (collection == null)
                {
                    return null;
                }

                // If additional path specified, navigate into collection
                if (parts.Length > 1)
                {
                    var propertyPath = RemoveQuotes(parts[1].Trim());
                    
                    // Handle collection properties like Count, Length
                    if (propertyPath.Equals("Count", StringComparison.OrdinalIgnoreCase))
                    {
                        if (collection is System.Collections.ICollection coll)
                            return coll.Count;
                        
                        var countProp = collection.GetType().GetProperty("Count");
                        if (countProp != null)
                            return countProp.GetValue(collection);
                    }
                    else if (propertyPath.Equals("Length", StringComparison.OrdinalIgnoreCase))
                    {
                        if (collection is Array arr)
                            return arr.Length;
                        
                        var lengthProp = collection.GetType().GetProperty("Length");
                        if (lengthProp != null)
                            return lengthProp.GetValue(collection);
                    }
                    else
                    {
                        // Try to get from first item in collection
                        if (collection is System.Collections.IEnumerable enumerable)
                        {
                            var firstItem = enumerable.Cast<object>().FirstOrDefault();
                            if (firstItem != null)
                            {
                                return GetNestedPropertyValue(firstItem, propertyPath);
                            }
                        }
                    }
                }

                return collection;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleChildValue", ex);
                return null;
            }
        }

        private object HandleNestedProperty(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var propertyPath = RemoveQuotes(content.Trim());

                if (string.IsNullOrWhiteSpace(propertyPath))
                {
                    LogError("NESTED requires a property path parameter");
                    return null;
                }

                var targetObject = GetTargetObject(parameters);
                if (targetObject == null)
                {
                    LogWarning("No target object available for nested property access");
                    return null;
                }

                return GetNestedPropertyValue(targetObject, propertyPath);
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleNestedProperty", ex);
                return null;
            }
        }

        private object HandleArrayItem(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 2)
                {
                    LogError("ARRAYITEM requires array name and index parameters");
                    return null;
                }

                var arrayName = RemoveQuotes(parts[0].Trim());
                var indexStr = RemoveQuotes(parts[1].Trim());

                if (!TryConvert<int>(indexStr, out int index))
                {
                    LogError($"Invalid array index: {indexStr}");
                    return null;
                }

                var targetObject = GetTargetObject(parameters);
                if (targetObject == null)
                {
                    LogWarning("No target object available for array access");
                    return null;
                }

                var array = GetPropertyValue(targetObject, arrayName);
                if (array == null)
                {
                    return null;
                }

                if (array is Array arr)
                {
                    // Handle negative indices (from end)
                    if (index < 0)
                        index = arr.Length + index;

                    if (index >= 0 && index < arr.Length)
                        return arr.GetValue(index);
                }
                else if (array is System.Collections.IList list)
                {
                    // Handle negative indices (from end)
                    if (index < 0)
                        index = list.Count + index;

                    if (index >= 0 && index < list.Count)
                        return list[index];
                }

                return null;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleArrayItem", ex);
                return null;
            }
        }

        private object HandleDictionaryValue(string rule, IPassedArgs parameters)
        {
            try
            {
                var content = ExtractParenthesesContent(rule);
                var parts = SplitParameters(content);

                if (parts.Length < 2)
                {
                    LogError("DICTVALUE requires dictionary name and key parameters");
                    return null;
                }

                var dictName = RemoveQuotes(parts[0].Trim());
                var key = RemoveQuotes(parts[1].Trim());

                var targetObject = GetTargetObject(parameters);
                if (targetObject == null)
                {
                    LogWarning("No target object available for dictionary access");
                    return null;
                }

                var dictionary = GetPropertyValue(targetObject, dictName);
                if (dictionary == null)
                {
                    return null;
                }

                if (dictionary is Dictionary<string, object> stringDict)
                {
                    return stringDict.TryGetValue(key, out var value) ? value : null;
                }
                else if (dictionary is System.Collections.IDictionary dict)
                {
                    return dict.Contains(key) ? dict[key] : null;
                }

                return null;
            }
            catch (Exception ex)
            {
                LogError($"Error in HandleDictionaryValue", ex);
                return null;
            }
        }

        #endregion

        #region Helper Methods

        private object GetTargetObject(IPassedArgs parameters)
        {
            // Try to get from Objects array first
            var objects = GetParameterValue<object[]>(parameters, "Objects");
            if (objects != null && objects.Length > 0)
                return objects[0];

            // Try to get as direct parameter
            return GetParameterValue<object>(parameters, "Object") ??
                   GetParameterValue<object>(parameters, "Record") ??
                   GetParameterValue<object>(parameters, "Entity");
        }

        private object GetNestedPropertyValue(object obj, string propertyPath)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propertyPath))
                return null;

            var parts = propertyPath.Split('.');
            var current = obj;

            foreach (var part in parts)
            {
                if (current == null)
                    break;

                current = GetPropertyValue(current, part) ?? GetFieldValue(current, part);
            }

            return current;
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            try
            {
                var type = obj.GetType();
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                
                if (property != null && property.CanRead)
                {
                    return property.GetValue(obj);
                }

                // Try case-insensitive search
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                property = properties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                
                if (property != null && property.CanRead)
                {
                    return property.GetValue(obj);
                }

                // Try dictionary access
                if (obj is Dictionary<string, object> dict)
                {
                    return dict.TryGetValue(propertyName, out var value) ? value : null;
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Error getting property '{propertyName}': {ex.Message}");
            }

            return null;
        }

        private object GetFieldValue(object obj, string FieldName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(FieldName))
                return null;

            try
            {
                var type = obj.GetType();
                var field = type.GetField(FieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                
                if (field != null)
                {
                    return field.GetValue(obj);
                }

                // Try case-insensitive search
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                field = fields.FirstOrDefault(f => f.Name.Equals(FieldName, StringComparison.OrdinalIgnoreCase));
                
                if (field != null)
                {
                    return field.GetValue(obj);
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Error getting field '{FieldName}': {ex.Message}");
            }

            return null;
        }

        #endregion
    }
}