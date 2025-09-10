using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Json.Helpers
{
    internal sealed class JsonSchemaPersistenceHelper
    {
        private readonly HashSet<string> _knownFieldKey = new(StringComparer.OrdinalIgnoreCase);
        private bool _dirty;
        private readonly object _lock = new();

        public bool IsDirty => _dirty;

        public void Initialize(IEnumerable<EntityStructure> entities)
        {
            _knownFieldKey.Clear();
            foreach (var e in entities ?? Enumerable.Empty<EntityStructure>())
            {
                foreach (var f in e.Fields ?? new List<EntityField>())
                {
                    _knownFieldKey.Add(Key(e.EntityName, f.fieldname));
                }
            }
            _dirty = false;
        }

        public void RegisterField(EntityStructure es, EntityField field)
        {
            if (es == null || field == null) return;
            lock (_lock)
            {
                var k = Key(es.EntityName, field.fieldname);
                if (_knownFieldKey.Add(k))
                {
                    _dirty = true;
                }
            }
        }

        public void RegisterBulk(EntityStructure es, IEnumerable<EntityField> newFields)
        {
            foreach (var f in newFields ?? Enumerable.Empty<EntityField>())
            {
                RegisterField(es, f);
            }
        }

        public void MarkDirty() => _dirty = true;

        public void FlushIfDirty(IEnumerable<EntityStructure> entities, Action<IEnumerable<EntityStructure>> persistAction)
        {
            if (!_dirty) return;
            persistAction?.Invoke(entities);
            _dirty = false;
        }

        private static string Key(string entity, string field) => $"{entity}::{field}";
    }
}