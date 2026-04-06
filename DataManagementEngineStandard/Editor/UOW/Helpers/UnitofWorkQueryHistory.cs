using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.UOW.Models;

namespace TheTechIdea.Beep.Editor.UOW.Helpers
{
    /// <summary>
    /// Maintains a fixed-size FIFO log of query executions for a UnitofWork instance.
    /// </summary>
    public class UnitofWorkQueryHistory
    {
        private readonly List<QueryHistoryEntry> _entries = new List<QueryHistoryEntry>();

        public int MaxSize { get; set; } = 20;

        /// <summary>Read-only view of recent query history.</summary>
        public IReadOnlyList<QueryHistoryEntry> Entries => _entries;

        /// <summary>Appends an entry, trimming oldest records when over <see cref="MaxSize"/>.</summary>
        public void Push(QueryHistoryEntry entry)
        {
            if (entry == null) return;
            _entries.Add(entry);
            while (_entries.Count > MaxSize)
                _entries.RemoveAt(0);
        }

        /// <summary>Removes all recorded entries.</summary>
        public void Clear() => _entries.Clear();
    }
}
