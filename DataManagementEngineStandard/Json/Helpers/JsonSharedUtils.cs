using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TheTechIdea.Beep.Json.Helpers
{
    /// <summary>
    /// Shared static utilities for JSON helper classes.
    /// Eliminates duplicated ConvertToken, InferClrType, and property-map building logic.
    /// </summary>
    internal static class JsonSharedUtils
    {
        /// <summary>
        /// Converts a JToken value to the target CLR type. Handles $oid unwrapping, nulls, and arrays.
        /// </summary>
        public static object? ConvertToken(JToken token, Type? targetType)
        {
            if (token == null) return null;

            // MongoDB-style ObjectId unwrapping
            if (token.Type == JTokenType.Object && token["$oid"] != null)
                return token["$oid"]!.ToString();

            if (token.Type == JTokenType.Null)
                return targetType != null && targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (targetType == null || targetType == typeof(object))
                return token.Type switch
                {
                    JTokenType.Integer => token.Value<long>(),
                    JTokenType.Float => token.Value<double>(),
                    JTokenType.Boolean => token.Value<bool>(),
                    JTokenType.Date => token.Value<DateTime>(),
                    JTokenType.Null => null,
                    _ => token.ToString()
                };

            try
            {
                return token.ToObject(targetType);
            }
            catch
            {
                // Fallback: try string conversion for non-primitive targets
                if (targetType == typeof(string))
                    return token.ToString();
                return null;
            }
        }

        /// <summary>
        /// Converts a JToken to its primitive representation (for graph hydration).
        /// </summary>
        public static object? PrimitiveOrString(JToken token)
        {
            return ConvertToken(token, null);
        }

        /// <summary>
        /// Infers a CLR type string from a JToken value. Used for schema field type detection.
        /// </summary>
        public static string InferClrType(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Integer => typeof(long).FullName!,
                JTokenType.Float => typeof(double).FullName!,
                JTokenType.String => typeof(string).FullName!,
                JTokenType.Boolean => typeof(bool).FullName!,
                JTokenType.Date => typeof(DateTime).FullName!,
                JTokenType.Guid => typeof(Guid).FullName!,
                JTokenType.Uri => typeof(Uri).FullName!,
                JTokenType.TimeSpan => typeof(TimeSpan).FullName!,
                JTokenType.Bytes => typeof(byte[]).FullName!,
                JTokenType.Null or JTokenType.Undefined => typeof(string).FullName!,
                JTokenType.Object => typeof(string).FullName!,
                JTokenType.Array => typeof(string).FullName!,
                _ => typeof(string).FullName!
            };
        }

        /// <summary>
        /// Builds a case-insensitive property-name-to-PropertyInfo map for a given type and entity structure.
        /// </summary>
        public static Dictionary<string, PropertyInfo> BuildPropertyMap(Type type, EntityStructure entity)
        {
            var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            if (type == null || entity?.Fields == null) return map;

            foreach (var field in entity.Fields)
            {
                if (string.IsNullOrEmpty(field.FieldName)) continue;
                var prop = type.GetProperty(field.FieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                    map[field.FieldName] = prop;
            }
            return map;
        }
    }
}
