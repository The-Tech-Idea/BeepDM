using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Json.Helpers
{
    /// <summary>
    /// Tracks known entity fields and persists schema changes when new fields are discovered.
    /// Thread-safe via consistent locking on all mutable state access.
    /// </summary>
    internal sealed class JsonSchemaPersistenceHelper
    {
        private readonly HashSet<string> _knownFieldKey = new(StringComparer.OrdinalIgnoreCase);
        private bool _dirty;
        private readonly object _lock = new();

        public bool IsDirty { get { lock (_lock) { return _dirty; } } }

        public void Initialize(IEnumerable<EntityStructure> entities)
        {
            lock (_lock)
            {
                _knownFieldKey.Clear();
                foreach (var e in entities ?? Enumerable.Empty<EntityStructure>())
                {
                    foreach (var f in e.Fields ?? Enumerable.Empty<EntityField>())
                        _knownFieldKey.Add(Key(e.EntityName, f.FieldName));
                }
                _dirty = false;
            }
        }

        public void RegisterField(EntityStructure es, EntityField field)
        {
            if (es == null || field == null) return;
            lock (_lock)
            {
                if (_knownFieldKey.Add(Key(es.EntityName, field.FieldName)))
                    _dirty = true;
            }
        }

        public void RegisterBulk(EntityStructure es, IEnumerable<EntityField> newFields)
        {
            if (es == null || newFields == null) return;
            lock (_lock)
            {
                foreach (var f in newFields)
                {
                    if (f != null && _knownFieldKey.Add(Key(es.EntityName, f.FieldName)))
                        _dirty = true;
                }
            }
        }

        public void MarkDirty() { lock (_lock) { _dirty = true; } }

        public void FlushIfDirty(IEnumerable<EntityStructure> entities, Action<IEnumerable<EntityStructure>> persistAction)
        {
            bool wasDirty;
            lock (_lock)
            {
                wasDirty = _dirty;
                if (!wasDirty) return;
                _dirty = false;
            }
            persistAction?.Invoke(entities);
        }

        private static string Key(string entity, string field) => $"{entity}::{field}";
    }
}
