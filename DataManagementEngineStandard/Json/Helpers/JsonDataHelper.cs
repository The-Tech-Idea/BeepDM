﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal class JsonDataHelper
    {
        private readonly Func<string, Type> _entityTypeResolver;
        private readonly List<EntityStructure> _entitiesRef;
        private readonly JArray _root;
        private readonly EntityStructure _rootEntity;

        public JsonDataHelper(
            JArray rootArray,
            EntityStructure rootEntity,
            List<EntityStructure> entities,
            Func<string, Type> entityTypeResolver)
        {
            _root = rootArray;
            _rootEntity = rootEntity;
            _entitiesRef = entities;
            _entityTypeResolver = entityTypeResolver;
        }

        public IEnumerable<object> GetEntities(string entityName, List<AppFilter> filters)
        {
            var es = _entitiesRef.FirstOrDefault(e => e.EntityName == entityName);
            if (es == null || _root == null) return Enumerable.Empty<object>();

            var predicates = JsonFilterHelper.CompileFilters(filters, es);
            var list = new List<object>();

            // Decide source slice (only root supported for now; deeper paths could map logically later)
            IEnumerable<JObject> source = _root.OfType<JObject>();

            // Resolve type
            Type runtimeType = SafeGetType(entityName);
            Dictionary<string, PropertyInfo> propMap = runtimeType != null
                ? es.Fields
                    .GroupBy(f => f.fieldname, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .Select(f => new { f.fieldname, PI = runtimeType.GetProperty(f.fieldname, BindingFlags.Public | BindingFlags.Instance) })
                    .Where(x => x.PI != null && x.PI.CanWrite)
                    .ToDictionary(x => x.fieldname, x => x.PI, StringComparer.OrdinalIgnoreCase)
                : null;

            foreach (var obj in source)
            {
                if (!predicates.All(p => p(obj))) continue;

                if (runtimeType != null)
                {
                    object inst;
                    try { inst = Activator.CreateInstance(runtimeType); }
                    catch { inst = null; }

                    if (inst != null)
                    {
                        foreach (var p in obj.Properties())
                        {
                            if (!propMap.TryGetValue(p.Name, out var pi)) continue;
                            try
                            {
                                var targetType = pi.PropertyType;
                                object converted = ConvertToken(p.Value, targetType);
                                pi.SetValue(inst, converted);
                            }
                            catch { /* ignore */ }
                        }
                        list.Add(inst);
                        continue;
                    }
                }

                // fallback dictionary
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in obj.Properties())
                {
                    dict[p.Name] = ConvertToken(p.Value, null);
                }
                list.Add(dict);
            }

            return list;
        }

        public PagedResult GetEntitiesPaged(string entityName, List<AppFilter> filters, int page, int size)
        {
            if (page < 1) page = 1;
            if (size <= 0) size = int.MaxValue;
            var all = GetEntities(entityName, filters).ToList();
            int total = all.Count;
            int skip = (page - 1) * size;
            var pageItems = (skip < total ? all.Skip(skip).Take(size) : Enumerable.Empty<object>()).ToList();

            return new PagedResult
            {
                PageNumber = page,
                PageSize = size,
                TotalRecords = total,
                TotalPages = total > 0 && size > 0 ? (int)Math.Ceiling((double)total / size) : 0,
                HasNextPage = page * size < total,
                HasPreviousPage = page > 1,
                Data = pageItems
            };
        }

        private Type SafeGetType(string entityName)
        {
            try { return _entityTypeResolver(entityName); }
            catch { return null; }
        }

        private static object ConvertToken(JToken token, Type targetType)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined) return null;

            if (token.Type == JTokenType.Object && token["$oid"] != null)
                return token["$oid"].ToString();

            if (targetType == null)
            {
                return token.Type switch
                {
                    JTokenType.Integer => token.ToObject<long>(),
                    JTokenType.Float => token.ToObject<double>(),
                    JTokenType.Boolean => token.ToObject<bool>(),
                    JTokenType.Date => token.ToObject<DateTime>(),
                    _ => token.ToString()
                };
            }

            try
            {
                if (targetType == typeof(string)) return token.ToString();
                if (targetType.IsEnum) return Enum.Parse(targetType, token.ToString(), true);
                return token.ToObject(targetType);
            }
            catch { return null; }
        }
    }
}