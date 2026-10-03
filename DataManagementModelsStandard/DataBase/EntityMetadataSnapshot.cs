using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Creates an owned, mutable copy of supported entity metadata without event subscribers.</summary>
    /// <remarks>
    /// Preserves order, identities, null collections and shared references (including primary keys).
    /// Supports the built-in metadata models, scalar option values, one-dimensional arrays,
    /// List&lt;T&gt; and Dictionary&lt;string, object&gt; with built-in comparers. Derived/custom values,
    /// cycles and graphs exceeding 64 levels or 10,000 visited values are rejected.
    /// Callers must synchronize capture against concurrent mutation and keep execution copies private.
    /// </remarks>
    public static class EntityMetadataSnapshot
    {
        public static EntityStructure Capture(EntityStructure source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return (EntityStructure)new CopyContext().Copy(source, 0);
        }

        private sealed class CopyContext
        {
            private readonly Dictionary<object, object> _copies = new(ReferenceEqualityComparer.Instance);
            private readonly HashSet<object> _active = new(ReferenceEqualityComparer.Instance);
            private int _visited;

            public object Copy(object value, int depth)
            {
                if (depth > 64 || ++_visited > 10000)
                    throw new InvalidOperationException("Metadata snapshot exceeds its size or depth limit.");
                if (value == null) return null;
                var type = value.GetType();
                if (IsScalar(type) || type == typeof(string).GetType()) return value;
                if (_active.Contains(value))
                    throw new InvalidOperationException("Metadata snapshot contains a cycle.");
                if (_copies.TryGetValue(value, out var existing)) return existing;
                _active.Add(value);
                try
                {
                    var copy = CopyValue(value, type, depth);
                    _copies.Add(value, copy);
                    return copy;
                }
                finally
                {
                    _active.Remove(value);
                }
            }

            private object CopyValue(object value, Type type, int depth)
            {
                if (type == typeof(EntityStructure))
                {
                    var source = (EntityStructure)value;
                    var copy = (EntityStructure)CopyModel(source, type);
                    copy.Fields = (List<EntityField>)Copy(source.Fields, depth + 1);
                    copy.PrimaryKeys = (List<EntityField>)Copy(source.PrimaryKeys, depth + 1);
                    copy.Parameters = (List<EntityParameters>)Copy(source.Parameters, depth + 1);
                    copy.Relations = (List<RelationShipKeys>)Copy(source.Relations, depth + 1);
                    copy.Indexes = (List<EntityIndex>)Copy(source.Indexes, depth + 1);
                    copy.Filters = (List<AppFilter>)Copy(source.Filters, depth + 1);
                    return copy;
                }
                if (type == typeof(EntityField) || type == typeof(EntityParameters) ||
                    type == typeof(RelationShipKeys) || type == typeof(AppFilter))
                {
                    if (value is AppFilter filter && filter.FieldType != null &&
                        filter.FieldType.GetType() != typeof(string).GetType())
                        throw Unsupported();
                    return CopyModel((Entity)value, type);
                }
                if (type == typeof(EntityIndex))
                {
                    var source = (EntityIndex)value;
                    return new EntityIndex
                    {
                        id = source.id, GuidID = source.GuidID, Name = source.Name,
                        EntityName = source.EntityName, IsUnique = source.IsUnique,
                        IsClustered = source.IsClustered,
                        Columns = (List<string>)Copy(source.Columns, depth + 1),
                        Options = (Dictionary<string, object>)Copy(source.Options, depth + 1)
                    };
                }
                if (type == typeof(Dictionary<string, object>))
                {
                    var source = (Dictionary<string, object>)value;
                    if (source.Comparer.GetType().Assembly != typeof(string).Assembly) throw Unsupported();
                    CheckCollectionSize(source.Count);
                    var copy = new Dictionary<string, object>(source.Count, source.Comparer);
                    foreach (var pair in source) copy.Add(pair.Key, Copy(pair.Value, depth + 1));
                    return copy;
                }
                if (type.IsArray && type.GetArrayRank() == 1 && type == type.GetElementType().MakeArrayType())
                {
                    var source = (Array)value;
                    if (!IsSupportedElement(type.GetElementType())) throw Unsupported();
                    CheckCollectionSize(source.Length);
                    var copy = Array.CreateInstance(type.GetElementType(), source.Length);
                    for (int i = 0; i < source.Length; i++) copy.SetValue(Copy(source.GetValue(i), depth + 1), i);
                    return copy;
                }
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    if (!IsSupportedElement(type.GetGenericArguments()[0])) throw Unsupported();
                    CheckCollectionSize(((IList)value).Count);
                    // Only a closed built-in List<T> is constructed; no provider/custom constructors run.
                    var copy = (IList)Activator.CreateInstance(type);
                    foreach (var item in (IList)value) copy.Add(Copy(item, depth + 1));
                    return copy;
                }
                throw Unsupported();
            }

            private void CheckCollectionSize(int count)
            {
                if (count > 10000 - _visited)
                    throw new InvalidOperationException("Metadata snapshot exceeds its size or depth limit.");
            }

            private static Entity CopyModel(Entity value, Type type)
            {
                // A future mutable member must get an explicit copy path, not silently become shared.
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (IsScalar(field.FieldType) || field.FieldType == typeof(Type)) continue;
                    bool copiedCollection = type == typeof(EntityStructure) && field.Name switch
                    {
                        "_fields" or "_primarykeys" => field.FieldType == typeof(List<EntityField>),
                        "_parameters" => field.FieldType == typeof(List<EntityParameters>),
                        "_relations" => field.FieldType == typeof(List<RelationShipKeys>),
                        "_indexes" => field.FieldType == typeof(List<EntityIndex>),
                        "_filters" => field.FieldType == typeof(List<AppFilter>),
                        _ => false
                    };
                    if (!copiedCollection) throw Unsupported();
                }
                return value.CopyWithoutObservers();
            }

            private static bool IsSupportedElement(Type type) => type == typeof(object) ||
                IsScalar(Nullable.GetUnderlyingType(type) ?? type) || type == typeof(EntityField) ||
                type == typeof(EntityParameters) || type == typeof(RelationShipKeys) ||
                type == typeof(EntityIndex) || type == typeof(AppFilter) || type == typeof(EntityStructure) ||
                (type.IsArray && type.GetArrayRank() == 1 && IsSupportedElement(type.GetElementType())) ||
                type == typeof(Dictionary<string, object>) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) &&
                    IsSupportedElement(type.GetGenericArguments()[0]));

            private static bool IsScalar(Type type) => type.IsEnum || type == typeof(string) ||
                type == typeof(bool) || type == typeof(char) || type == typeof(byte) ||
                type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
                type == typeof(int) || type == typeof(uint) || type == typeof(long) ||
                type == typeof(ulong) || type == typeof(float) || type == typeof(double) ||
                type == typeof(decimal) || type == typeof(Guid) || type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
                type == typeof(DateOnly) || type == typeof(TimeOnly);

            private static NotSupportedException Unsupported() =>
                new("Metadata snapshot contains an unsupported value or comparer.");
        }
    }
}
