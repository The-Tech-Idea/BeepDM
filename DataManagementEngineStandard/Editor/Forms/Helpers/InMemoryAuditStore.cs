using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Thread-safe in-memory ring-buffer audit store.
    /// Useful for development or when persistence is handled by an external system.
    /// </summary>
    public class InMemoryAuditStore : IAuditStore
    {
        private readonly object _lock = new object();
        private readonly LinkedList<AuditEntry> _entries = new LinkedList<AuditEntry>();
        private readonly int _capacity;

        /// <summary>
        /// Creates an in-memory audit store with the supplied capacity.
        /// </summary>
        /// <param name="capacity">Maximum retained entries before older items are evicted.</param>
        public InMemoryAuditStore(int capacity = 10_000)
        {
            _capacity = capacity > 0 ? capacity : 10_000;
        }

        /// <summary>Saves an audit entry to the in-memory store.</summary>
        public void Save(AuditEntry entry)
        {
            if (entry == null) return;
            lock (_lock)
            {
                _entries.AddLast(entry);
                while (_entries.Count > _capacity)
                    _entries.RemoveFirst();
            }
        }

        /// <summary>Queries entries using the supplied filters.</summary>
        public IReadOnlyList<AuditEntry> Query(
            string blockName         = null,
            AuditOperation? operation = null,
            DateTime? from           = null,
            DateTime? to             = null)
        {
            lock (_lock)
            {
                IEnumerable<AuditEntry> q = _entries;

                if (!string.IsNullOrEmpty(blockName))
                    q = q.Where(e => string.Equals(e.BlockName, blockName, StringComparison.OrdinalIgnoreCase));

                if (operation.HasValue)
                    q = q.Where(e => e.Operation == operation.Value);

                if (from.HasValue)
                    q = q.Where(e => e.Timestamp >= from.Value);

                if (to.HasValue)
                    q = q.Where(e => e.Timestamp <= to.Value);

                return q.ToList();
            }
        }

        /// <summary>Purges entries older than the supplied number of days.</summary>
        public void Purge(int olderThanDays)
        {
            var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(olderThanDays));
            lock (_lock)
            {
                var toRemove = _entries.Where(e => e.Timestamp < cutoff).ToList();
                foreach (var e in toRemove)
                    _entries.Remove(e);
            }
        }

        /// <summary>Clears all retained entries.</summary>
        public void Clear()
        {
            lock (_lock)
                _entries.Clear();
        }
    }
}
