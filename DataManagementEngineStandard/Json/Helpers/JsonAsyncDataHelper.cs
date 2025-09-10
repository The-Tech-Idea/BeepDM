using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal class JsonAsyncDataHelper
    {
        private readonly JToken _root;
        private readonly List<EntityStructure> _entities;
        private readonly Func<string, Type> _typeResolver;

        public JsonAsyncDataHelper(JToken root, List<EntityStructure> entities, Func<string, Type> typeResolver)
        {
            _root = root;
            _entities = entities;
            _typeResolver = typeResolver;
        }

        public async IAsyncEnumerable<object> StreamAsync(
            string entityName,
            List<AppFilter> filters,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var entity = _entities.FirstOrDefault(e => e.EntityName == entityName);
            if (entity == null) yield break;

            var sourceArray = JsonPathNavigator.ResolveArray(_root, entity);
            if (sourceArray == null) yield break;

            var predicates = JsonFilterHelper.CompileFilters(filters, entity);
            Type runtimeType = TryGetType(entityName);
            var propMap = runtimeType != null
                ? BuildPropertyMap(runtimeType, entity)
                : null;

            // Simulate async streaming (I/O placeholder) while enumerating
            foreach (var jt in sourceArray.OfType<JObject>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!predicates.All(p => p(jt))) continue;

                object materialized = runtimeType != null
                    ? TryMaterializeTyped(jt, runtimeType, propMap)
                    : MaterializeDictionary(jt);

                yield return materialized;

                await Task.Yield(); // keep it responsive
            }
        }

        public async Task<List<object>> GetPageAsync(
            string entityName,
            List<AppFilter> filters,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = int.MaxValue;

            var list = new List<object>();
            int skip = (pageNumber - 1) * pageSize;
            int taken = 0;
            int index = 0;

            await foreach (var item in StreamAsync(entityName, filters, cancellationToken))
            {
                if (index++ < skip) continue;
                list.Add(item);
                if (++taken >= pageSize) break;
            }
            return list;
        }

        private Type TryGetType(string entityName)
        {
            try { return _typeResolver(entityName); } catch { return null; }
        }

        private static Dictionary<string, System.Reflection.PropertyInfo> BuildPropertyMap(Type t, EntityStructure es)
            => es.Fields
                .GroupBy(f => f.fieldname, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .Select(f => new { f.fieldname, PI = t.GetProperty(f.fieldname) })
                .Where(x => x.PI != null && x.PI.CanWrite)
                .ToDictionary(x => x.fieldname, x => x.PI, StringComparer.OrdinalIgnoreCase);

        private static object TryMaterializeTyped(JObject obj, Type t, Dictionary<string, System.Reflection.PropertyInfo> map)
        {
            object inst;
            try { inst = Activator.CreateInstance(t); }
            catch { return MaterializeDictionary(obj); }

            foreach (var p in obj.Properties())
            {
                if (!map.TryGetValue(p.Name, out var pi)) continue;
                try
                {
                    object val = ConvertToken(p.Value, pi.PropertyType);
                    pi.SetValue(inst, val);
                }
                catch { }
            }
            return inst;
        }

        private static Dictionary<string, object> MaterializeDictionary(JObject obj)
        {
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in obj.Properties())
                d[p.Name] = ConvertToken(p.Value, null);
            return d;
        }

        private static object ConvertToken(JToken token, Type targetType)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined) return null;
            if (token.Type == JTokenType.Object && token["$oid"] != null) return token["$oid"]?.ToString();

            if (targetType == null)
            {
                return token.Type switch
                {
                    JTokenType.Integer => token.ToObject<long>(),
                    JTokenType.Float => token.ToObject<double>(),
                    JTokenType.Date => token.ToObject<DateTime>(),
                    JTokenType.Boolean => token.ToObject<bool>(),
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