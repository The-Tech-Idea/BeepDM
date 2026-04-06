using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Thread-safe in-memory ring-buffer of recent trigger execution records.
    /// </summary>
    public class TriggerExecutionLog : ITriggerExecutionLog
    {
        private readonly object _lock = new object();
        private readonly LinkedList<TriggerExecutionLogEntry> _entries = new LinkedList<TriggerExecutionLogEntry>();

        public int Capacity { get; set; }

        public TriggerExecutionLog(int capacity = 500)
        {
            Capacity = capacity;
        }

        public void Record(TriggerExecutionLogEntry entry)
        {
            if (entry == null) return;
            lock (_lock)
            {
                _entries.AddLast(entry);
                while (_entries.Count > Capacity)
                    _entries.RemoveFirst();
            }
        }

        public IReadOnlyList<TriggerExecutionLogEntry> GetAll()
        {
            lock (_lock) return _entries.ToList();
        }

        public IReadOnlyList<TriggerExecutionLogEntry> GetByBlock(string blockName)
        {
            lock (_lock)
                return _entries
                    .Where(e => string.Equals(e.BlockName, blockName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }

        public IReadOnlyList<TriggerExecutionLogEntry> GetByType(TriggerType type)
        {
            lock (_lock)
                return _entries.Where(e => e.TriggerType == type).ToList();
        }

        public void Clear()
        {
            lock (_lock) _entries.Clear();
        }
    }
}
