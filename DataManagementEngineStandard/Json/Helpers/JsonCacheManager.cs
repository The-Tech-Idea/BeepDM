using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal static class JsonCacheManager
    {
        // Key: entityName|filterSignature -> compiled predicates
        private static readonly ConcurrentDictionary<string, List<Func<JObject, bool>>> _predicateCache = new();
        // Key: entityType.FullName -> property map
        private static readonly ConcurrentDictionary<string, Dictionary<string, PropertyInfo>> _propertyMapCache = new(StringComparer.OrdinalIgnoreCase);
        // Child entity list cache
        private static readonly ConcurrentDictionary<string, List<EntityStructure>> _childEntitiesCache = new(StringComparer.OrdinalIgnoreCase);

        public static List<Func<JObject, bool>> GetOrAddPredicates(string entityName, List<AppFilter> filters, EntityStructure entity)
        {
            string signature = BuildFilterSignature(entityName, filters);
            return _predicateCache.GetOrAdd(signature, _ => JsonFilterHelper.CompileFilters(filters, entity));
        }

        public static Dictionary<string, PropertyInfo> GetOrAddPropertyMap(Type t, EntityStructure es)
        {
            if (t == null || es == null) return null;
            return _propertyMapCache.GetOrAdd(t.FullName, _ =>
                es.Fields
                  .GroupBy(f => f.fieldname, StringComparer.OrdinalIgnoreCase)
                  .Select(g => g.First())
                  .Select(f => new { f.fieldname, PI = t.GetProperty(f.fieldname, BindingFlags.Instance | BindingFlags.Public) })
                  .Where(x => x.PI != null && x.PI.CanWrite)
                  .ToDictionary(x => x.fieldname, x => x.PI, StringComparer.OrdinalIgnoreCase)
            );
        }

        public static List<EntityStructure> GetOrAddChildren(EntityStructure parent, List<EntityStructure> all)
        {
            if (parent == null) return new List<EntityStructure>();
            return _childEntitiesCache.GetOrAdd(parent.EntityName, _ =>
                all.Where(e => e.ParentId == parent.Id).ToList());
        }

        public static void InvalidateEntity(string entityName)
        {
            foreach (var key in _predicateCache.Keys.Where(k => k.StartsWith(entityName + "|", StringComparison.OrdinalIgnoreCase)))
                _predicateCache.TryRemove(key, out _);

            _childEntitiesCache.TryRemove(entityName, out _);
        }

        public static void InvalidateAll()
        {
            _predicateCache.Clear();
            _propertyMapCache.Clear();
            _childEntitiesCache.Clear();
        }

        private static string BuildFilterSignature(string entityName, List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0) return entityName + "|*";
            return entityName + "|" + string.Join(";", filters
                .OrderBy(f => f.FieldName, StringComparer.OrdinalIgnoreCase)
                .Select(f => $"{f.FieldName}:{f.Operator}:{f.FilterValue}:{f.FilterValue1}"));
        }
    }
}