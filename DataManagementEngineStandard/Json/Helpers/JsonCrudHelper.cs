using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal class JsonCrudHelper
    {
        private readonly List<EntityStructure> _entities;
        private readonly JToken _root;
        private readonly Func<string, Type> _typeResolver;

        public JsonCrudHelper(JToken root, List<EntityStructure> entities, Func<string, Type> typeResolver)
        {
            _root = root;
            _entities = entities;
            _typeResolver = typeResolver;
        }

        public bool Insert(string entityName, object data)
        {
            var es = _entities.FirstOrDefault(e => e.EntityName == entityName);
            if (es == null)
                return false;

            var arr = JsonPathNavigator.ResolveArray(_root, es);
            if (arr == null)
                return false;

            // Create JObject
            var obj = JObject.FromObject(data);
            EnsureSyntheticKey(es, obj);
            AddParentRefIfNeeded(es, obj);
            arr.Add(obj);
            return true;
        }

        public bool Update(string entityName, object data)
        {
            var es = _entities.FirstOrDefault(e => e.EntityName == entityName);
            if (es == null)
                return false;

            var arr = JsonPathNavigator.ResolveArray(_root, es);
            if (arr == null)
                return false;

            var pk = es.PrimaryKeys.FirstOrDefault();
            if (pk == null)
                return false;

            var pkProp = data.GetType().GetProperty(pk.fieldname);
            if (pkProp == null)
                return false;

            var pkVal = pkProp.GetValue(data)?.ToString();

            var target = arr.FirstOrDefault(t =>
                t.Type == JTokenType.Object &&
                t[pk.fieldname]?.ToString() == pkVal) as JObject;

            if (target == null)
                return false;

            var incoming = JObject.FromObject(data);
            foreach (var p in incoming.Properties())
            {
                target[p.Name] = p.Value;
            }
            return true;
        }

        public bool Delete(string entityName, AppFilter keyFilter)
        {
            var es = _entities.FirstOrDefault(e => e.EntityName == entityName);
            if (es == null)
                return false;

            var arr = JsonPathNavigator.ResolveArray(_root, es);
            if (arr == null)
                return false;

            // Simple equality on key field
            if (string.IsNullOrWhiteSpace(keyFilter?.FieldName))
                return false;

            var toRemove = arr
                .Where(t => t.Type == JTokenType.Object &&
                            string.Equals(t[keyFilter.FieldName]?.ToString(), keyFilter.FilterValue, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var r in toRemove)
                r.Remove();

            return toRemove.Count > 0;
        }

        private void EnsureSyntheticKey(EntityStructure es, JObject obj)
        {
            var pk = es.PrimaryKeys.FirstOrDefault();
            if (pk == null)
                return;

            if (obj[pk.fieldname] == null || string.IsNullOrWhiteSpace(obj[pk.fieldname]?.ToString()))
            {
                // Only generate if string key and not auto increment
                if (pk.fieldtype == typeof(string).FullName)
                    obj[pk.fieldname] = Guid.NewGuid().ToString("N");
            }
        }

        private void AddParentRefIfNeeded(EntityStructure es, JObject obj)
        {
            if (es.ParentId == 0)
                return;

            var parent = _entities.FirstOrDefault(p => p.Id == es.ParentId);
            if (parent == null)
                return;

            var parentPk = parent.PrimaryKeys.FirstOrDefault();
            if (parentPk == null)
                return;

            // If parent reference not present, leave it to upper layer to set
            var fkName = "_parentId";
            if (obj[fkName] == null)
            {
                // left null unless relationship context passes value
            }
        }
    }
}