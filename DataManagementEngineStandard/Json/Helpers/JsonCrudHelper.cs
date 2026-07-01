using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Json.Helpers
{
    /// <summary>
    /// CRUD operations for JSON-backed entities. Operates on a shared JToken root
    /// and resolves entity paths via JsonPathNavigator.
    /// </summary>
    internal class JsonCrudHelper
    {
        private readonly List<EntityStructure> _entities;
        private readonly JToken _root;

        public JsonCrudHelper(JToken root, List<EntityStructure> entities)
        {
            _root = root;
            _entities = entities;
        }

        /// <summary>Inserts a record into the target entity's JSON array.</summary>
        public bool Insert(string entityName, object data)
        {
            var es = _entities.FirstOrDefault(e => e.EntityName == entityName);
            if (es == null) return false;

            var arr = JsonPathNavigator.ResolveArray(_root, es);
            if (arr == null) return false;

            try
            {
                var obj = data is JObject jo ? jo : JObject.FromObject(data);
                EnsureSyntheticKey(es, obj);
                arr.Add(obj);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Updates a record matched by primary key.</summary>
        public bool Update(string entityName, object data)
        {
            var es = _entities.FirstOrDefault(e => e.EntityName == entityName);
            if (es == null) return false;

            var arr = JsonPathNavigator.ResolveArray(_root, es);
            if (arr == null) return false;

            var pk = es.PrimaryKeys?.FirstOrDefault();
            if (pk == null) return false;

            // Get PK value from either typed object property or dictionary key
            string? pkVal = null;
            if (data is JObject jo)
                pkVal = jo[pk.FieldName]?.ToString();
            else if (data is IDictionary<string, object> dict)
                pkVal = dict.TryGetValue(pk.FieldName, out var v) ? v?.ToString() : null;
            else
            {
                var pkProp = data.GetType().GetProperty(pk.FieldName);
                if (pkProp != null)
                    pkVal = pkProp.GetValue(data)?.ToString();
            }

            if (string.IsNullOrEmpty(pkVal)) return false;

            var target = arr.OfType<JObject>().FirstOrDefault(t =>
                string.Equals(t[pk.FieldName]?.ToString(), pkVal, StringComparison.OrdinalIgnoreCase));
            if (target == null) return false;

            var incoming = data is JObject already ? already : JObject.FromObject(data);
            foreach (var p in incoming.Properties())
                target[p.Name] = p.Value;
            return true;
        }

        /// <summary>Deletes records matching a key filter.</summary>
        public bool Delete(string entityName, AppFilter? keyFilter)
        {
            var es = _entities.FirstOrDefault(e => e.EntityName == entityName);
            if (es == null) return false;

            var arr = JsonPathNavigator.ResolveArray(_root, es);
            if (arr == null) return false;

            if (string.IsNullOrWhiteSpace(keyFilter?.FieldName))
                return false;

            var toRemove = arr.OfType<JObject>()
                .Where(t => string.Equals(t[keyFilter.FieldName]?.ToString(), keyFilter.FilterValue, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var r in toRemove)
                r.Remove();

            return toRemove.Count > 0;
        }

        /// <summary>Generates a synthetic GUID primary key if the entity's PK is a string and is missing.</summary>
        private static void EnsureSyntheticKey(EntityStructure es, JObject obj)
        {
            var pk = es.PrimaryKeys?.FirstOrDefault();
            if (pk == null) return;

            if (obj[pk.FieldName] == null || string.IsNullOrWhiteSpace(obj[pk.FieldName]?.ToString()))
            {
                if (pk.Fieldtype == typeof(string).FullName)
                    obj[pk.FieldName] = Guid.NewGuid().ToString("N");
                else if (pk.Fieldtype == typeof(int).FullName || pk.Fieldtype == typeof(long).FullName)
                    obj[pk.FieldName] = 0; // Numeric PK — caller should provide real value
            }
        }
    }
}
