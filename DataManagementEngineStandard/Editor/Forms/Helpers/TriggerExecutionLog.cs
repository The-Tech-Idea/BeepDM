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

        /// <summary>Gets or sets the maximum retained log capacity.</summary>
        public int Capacity { get; set; }

        /// <summary>
        /// Creates a trigger execution log with the supplied capacity.
        /// </summary>
        /// <param name="capacity">Maximum retained execution entries.</param>
        public TriggerExecutionLog(int capacity = 500)
        {
            Capacity = capacity;
        }

        /// <summary>Records a trigger execution entry.</summary>
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

        /// <summary>Returns all retained trigger execution entries.</summary>
        public IReadOnlyList<TriggerExecutionLogEntry> GetAll()
        {
            lock (_lock) return _entries.ToList();
        }

        /// <summary>Returns retained entries for a specific block.</summary>
        public IReadOnlyList<TriggerExecutionLogEntry> GetByBlock(string blockName)
        {
            lock (_lock)
                return _entries
                    .Where(e => string.Equals(e.BlockName, blockName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }

        /// <summary>Returns retained entries for a trigger type.</summary>
        public IReadOnlyList<TriggerExecutionLogEntry> GetByType(TriggerType type)
        {
            lock (_lock)
                return _entries.Where(e => e.TriggerType == type).ToList();
        }

        /// <summary>Clears all retained trigger execution entries.</summary>
        public void Clear()
        {
            lock (_lock) _entries.Clear();
        }
    }
}
