using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Mapping.Helpers;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping
{
    public enum CollectionMappingMode
    {
        Append,
        Replace,
        MergeByKey
    }

    public enum ReferenceReusePolicy
    {
        None,
        ReuseBySourceReference
    }

    public sealed class ObjectGraphMappingOptions
    {
        public int MaxDepth { get; set; } = 5;
        public bool DetectCycles { get; set; } = true;
        public CollectionMappingMode CollectionMode { get; set; } = CollectionMappingMode.Replace;
        public ReferenceReusePolicy ReferenceReusePolicy { get; set; } = ReferenceReusePolicy.ReuseBySourceReference;
        public string CollectionMergeKeyPropertyName { get; set; } = "Id";
    }

    public sealed class ObjectGraphMappingResult
    {
        public bool Success { get; set; } = true;
        public int Assignments { get; set; }
        public int Skipped { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }

    public static partial class MappingManager
    {
        public static Tuple<object, ObjectGraphMappingResult> MapObjectGraph(
            IDMEEditor editor,
            string destinationEntityName,
            EntityDataMap_DTL selectedMapping,
            object sourceObject,
            ObjectGraphMappingOptions options = null)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (string.IsNullOrWhiteSpace(destinationEntityName)) throw new ArgumentException("Destination entity name is required.", nameof(destinationEntityName));
            if (selectedMapping == null) throw new ArgumentNullException(nameof(selectedMapping));
            if (sourceObject == null) throw new ArgumentNullException(nameof(sourceObject));

            options ??= new ObjectGraphMappingOptions();
            var result = new ObjectGraphMappingResult();

            var destination = GetEntityObject(editor, destinationEntityName, selectedMapping.SelectedDestFields);
            if (destination == null)
                throw new InvalidOperationException($"Failed to create destination object for '{destinationEntityName}'.");

            var visited = new HashSet<object>(ReferenceComparer.Instance);
            var reuseCache = new Dictionary<object, object>(ReferenceComparer.Instance);

            foreach (var map in selectedMapping.FieldMapping ?? new List<Mapping_rep_fields>())
            {
                if (map == null || string.IsNullOrWhiteSpace(map.ToFieldName))
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(map.FromFieldName))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var sourceValue = GetMemberValueByPath(sourceObject, map.FromFieldName);
                    if (sourceValue == null)
                    {
                        result.Skipped++;
                        continue;
                    }

                    var destinationMember = GetMemberByPath(destination.GetType(), map.ToFieldName);
                    if (destinationMember == null)
                    {
                        result.Warnings.Add($"Destination path '{map.ToFieldName}' not found.");
                        result.Skipped++;
                        continue;
                    }

                    var destinationType = GetMemberType(destinationMember);
                    var assignedValue = PrepareValueForDestination(
                        sourceValue,
                        destinationType,
                        selectedMapping.EntityDataSource,
                        destinationEntityName,
                        options,
                        visited,
                        reuseCache,
                        depth: 0,
                        result);

                    if (!TrySetMemberValueByPath(destination, map.ToFieldName, assignedValue))
                    {
                        result.Warnings.Add($"Could not assign value to destination path '{map.ToFieldName}'.");
                        result.Skipped++;
                        continue;
                    }

                    result.Assignments++;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Warnings.Add($"Mapping failed '{map.FromFieldName}' -> '{map.ToFieldName}': {ex.Message}");
                }
            }

            try
            {
                MappingDefaultsHelper.ApplyDefaultsToObject(
                    editor,
                    selectedMapping.EntityDataSource,
                    destinationEntityName,
                    destination,
                    selectedMapping.SelectedDestFields);
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Defaults application warning: {ex.Message}");
            }

            return new Tuple<object, ObjectGraphMappingResult>(destination, result);
        }

        private static object PrepareValueForDestination(
            object sourceValue,
            Type destinationType,
            string destinationDataSource,
            string destinationEntity,
            ObjectGraphMappingOptions options,
            HashSet<object> visited,
            Dictionary<object, object> reuseCache,
            int depth,
            ObjectGraphMappingResult result)
        {
            if (sourceValue == null)
                return null;

            if (depth > options.MaxDepth)
            {
                result.Warnings.Add($"Max depth '{options.MaxDepth}' reached; value skipped.");
                result.Skipped++;
                return null;
            }

            if (IsSimpleType(destinationType))
            {
                var policy = GetConversionPolicy(destinationDataSource, destinationEntity);
                var conversion = ApplyConversionPipeline(sourceValue, destinationType, policy, transformChain: null);
                if (!conversion.Success)
                {
                    result.Warnings.Add($"Conversion warning: {conversion.Message}");
                    result.Skipped++;
                }
                return conversion.Value;
            }

            if (IsCollectionType(destinationType) && sourceValue is IEnumerable enumerable && sourceValue is not string)
            {
                return MapCollection(enumerable, destinationType, destinationDataSource, destinationEntity, options, visited, reuseCache, depth, result);
            }

            if (options.DetectCycles && !visited.Add(sourceValue))
            {
                result.Warnings.Add("Cycle detected; value skipped.");
                result.Skipped++;
                return null;
            }

            if (options.ReferenceReusePolicy == ReferenceReusePolicy.ReuseBySourceReference &&
                reuseCache.TryGetValue(sourceValue, out var cached))
            {
                return cached;
            }

            var target = Activator.CreateInstance(destinationType);
            if (target == null)
                return null;

            if (options.ReferenceReusePolicy == ReferenceReusePolicy.ReuseBySourceReference)
                reuseCache[sourceValue] = target;

            CopyObjectGraphByName(sourceValue, target, destinationDataSource, destinationEntity, options, visited, reuseCache, depth + 1, result);
            return target;
        }

        private static object MapCollection(
            IEnumerable sourceItems,
            Type destinationType,
            string destinationDataSource,
            string destinationEntity,
            ObjectGraphMappingOptions options,
            HashSet<object> visited,
            Dictionary<object, object> reuseCache,
            int depth,
            ObjectGraphMappingResult result)
        {
            var itemType = ResolveCollectionItemType(destinationType) ?? typeof(object);
            var sourceList = sourceItems.Cast<object>().ToList();
            var mappedItems = new List<object>();

            foreach (var item in sourceList)
            {
                var mapped = PrepareValueForDestination(
                    item,
                    itemType,
                    destinationDataSource,
                    destinationEntity,
                    options,
                    visited,
                    reuseCache,
                    depth + 1,
                    result);
                if (mapped != null)
                    mappedItems.Add(mapped);
            }

            var list = CreateListInstance(destinationType, itemType);
            if (list == null)
                return null;

            foreach (var item in mappedItems)
                list.Add(item);

            if (destinationType.IsArray)
            {
                var array = Array.CreateInstance(itemType, list.Count);
                list.CopyTo(array, 0);
                return array;
            }

            return list;
        }

        private static void CopyObjectGraphByName(
            object source,
            object destination,
            string destinationDataSource,
            string destinationEntity,
            ObjectGraphMappingOptions options,
            HashSet<object> visited,
            Dictionary<object, object> reuseCache,
            int depth,
            ObjectGraphMappingResult result)
        {
            var sourceProperties = source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var destinationProperties = destination.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanWrite)
                .ToDictionary(property => property.Name, property => property, StringComparer.OrdinalIgnoreCase);

            foreach (var sourceProperty in sourceProperties)
            {
                if (!sourceProperty.CanRead)
                    continue;

                if (!destinationProperties.TryGetValue(sourceProperty.Name, out var destinationProperty))
                    continue;

                var sourceValue = sourceProperty.GetValue(source);
                var mapped = PrepareValueForDestination(
                    sourceValue,
                    destinationProperty.PropertyType,
                    destinationDataSource,
                    destinationEntity,
                    options,
                    visited,
                    reuseCache,
                    depth + 1,
                    result);

                try
                {
                    if (IsCollectionType(destinationProperty.PropertyType) &&
                        mapped is IEnumerable incomingEnumerable &&
                        destinationProperty.GetValue(destination) is IEnumerable existingEnumerable &&
                        destinationProperty.PropertyType != typeof(string))
                    {
                        var merged = ApplyCollectionMode(existingEnumerable, incomingEnumerable, destinationProperty.PropertyType, options);
                        destinationProperty.SetValue(destination, merged);
                    }
                    else
                    {
                        destinationProperty.SetValue(destination, mapped);
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Nested assign warning '{destinationProperty.Name}': {ex.Message}");
                }
            }
        }

        private static object ApplyCollectionMode(IEnumerable existing, IEnumerable incoming, Type destinationType, ObjectGraphMappingOptions options)
        {
            var itemType = ResolveCollectionItemType(destinationType) ?? typeof(object);
            var existingItems = existing.Cast<object>().ToList();
            var incomingItems = incoming.Cast<object>().ToList();

            List<object> merged = options.CollectionMode switch
            {
                CollectionMappingMode.Append => existingItems.Concat(incomingItems).ToList(),
                CollectionMappingMode.MergeByKey => MergeByKey(existingItems, incomingItems, options.CollectionMergeKeyPropertyName),
                _ => incomingItems
            };

            if (destinationType.IsArray)
            {
                var array = Array.CreateInstance(itemType, merged.Count);
                for (var i = 0; i < merged.Count; i++)
                    array.SetValue(merged[i], i);
                return array;
            }

            var list = CreateListInstance(destinationType, itemType);
            if (list == null)
                return incoming;

            foreach (var item in merged)
                list.Add(item);
            return list;
        }

        private static List<object> MergeByKey(List<object> existingItems, List<object> incomingItems, string keyPropertyName)
        {
            if (string.IsNullOrWhiteSpace(keyPropertyName))
                return incomingItems;

            var result = new List<object>();
            var index = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in existingItems)
            {
                var key = GetMergeKey(item, keyPropertyName);
                if (!string.IsNullOrWhiteSpace(key))
                    index[key] = item;
                else
                    result.Add(item);
            }

            foreach (var item in incomingItems)
            {
                var key = GetMergeKey(item, keyPropertyName);
                if (string.IsNullOrWhiteSpace(key))
                {
                    result.Add(item);
                    continue;
                }

                index[key] = item;
            }

            result.AddRange(index.Values);
            return result;
        }

        private static string GetMergeKey(object item, string keyPropertyName)
        {
            if (item == null || string.IsNullOrWhiteSpace(keyPropertyName))
                return null;

            var property = item.GetType().GetProperty(keyPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            var value = property?.GetValue(item);
            return value?.ToString();
        }

        private static object GetMemberValueByPath(object target, string path)
        {
            var current = target;
            foreach (var segment in SplitPath(path))
            {
                if (current == null)
                    return null;

                var member = GetMember(current.GetType(), segment);
                if (member == null)
                    return null;

                current = GetMemberValue(current, member);
            }
            return current;
        }

        private static bool TrySetMemberValueByPath(object target, string path, object value)
        {
            var segments = SplitPath(path).ToArray();
            if (segments.Length == 0)
                return false;

            var current = target;
            for (var index = 0; index < segments.Length - 1; index++)
            {
                var member = GetMember(current.GetType(), segments[index]);
                if (member == null)
                    return false;

                var next = GetMemberValue(current, member);
                if (next == null)
                {
                    var memberType = GetMemberType(member);
                    next = Activator.CreateInstance(memberType);
                    if (next == null)
                        return false;
                    SetMemberValue(current, member, next);
                }
                current = next;
            }

            var finalMember = GetMember(current.GetType(), segments[^1]);
            if (finalMember == null)
                return false;

            SetMemberValue(current, finalMember, value);
            return true;
        }

        private static MemberInfo GetMemberByPath(Type rootType, string path)
        {
            var currentType = rootType;
            MemberInfo member = null;
            foreach (var segment in SplitPath(path))
            {
                member = GetMember(currentType, segment);
                if (member == null)
                    return null;
                currentType = GetMemberType(member);
            }
            return member;
        }

        private static MemberInfo GetMember(Type type, string name)
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property != null)
                return property;

            return type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        }

        private static object GetMemberValue(object instance, MemberInfo member)
        {
            if (member is PropertyInfo property)
                return property.GetValue(instance);
            if (member is FieldInfo field)
                return field.GetValue(instance);
            return null;
        }

        private static void SetMemberValue(object instance, MemberInfo member, object value)
        {
            if (member is PropertyInfo property && property.CanWrite)
            {
                property.SetValue(instance, value);
                return;
            }

            if (member is FieldInfo field)
                field.SetValue(instance, value);
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => typeof(object)
            };
        }

        private static IEnumerable<string> SplitPath(string path)
        {
            return (path ?? string.Empty)
                .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim());
        }

        private static bool IsSimpleType(Type type)
        {
            var nonNullable = Nullable.GetUnderlyingType(type) ?? type;
            return nonNullable.IsPrimitive ||
                   nonNullable.IsEnum ||
                   nonNullable == typeof(string) ||
                   nonNullable == typeof(decimal) ||
                   nonNullable == typeof(DateTime) ||
                   nonNullable == typeof(DateTimeOffset) ||
                   nonNullable == typeof(TimeSpan) ||
                   nonNullable == typeof(Guid);
        }

        private static bool IsCollectionType(Type type)
        {
            return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
        }

        private static Type ResolveCollectionItemType(Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();

            if (collectionType.IsGenericType)
                return collectionType.GetGenericArguments().FirstOrDefault();

            var enumerableInterface = collectionType.GetInterfaces()
                .FirstOrDefault(item => item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerableInterface?.GetGenericArguments().FirstOrDefault();
        }

        private static IList CreateListInstance(Type destinationType, Type itemType)
        {
            if (destinationType.IsArray)
                return new List<object>();

            if (destinationType.IsInterface || destinationType.IsAbstract)
            {
                var listType = typeof(List<>).MakeGenericType(itemType);
                return Activator.CreateInstance(listType) as IList;
            }

            return Activator.CreateInstance(destinationType) as IList;
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => obj == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
