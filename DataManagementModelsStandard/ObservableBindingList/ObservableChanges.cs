using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Represents a snapshot of all pending changes (added, modified, deleted) in an ObservableBindingList.
    /// </summary>
    public class ObservableChanges<T>
    {
        public List<T> Added { get; set; } = new();
        public List<T> Modified { get; set; } = new();
        public List<T> Deleted { get; set; } = new();

        /// <summary>True if there are any pending changes.</summary>
        public bool HasChanges => Added.Count > 0 || Modified.Count > 0 || Deleted.Count > 0;

        /// <summary>Total count of all pending changes.</summary>
        public int TotalCount => Added.Count + Modified.Count + Deleted.Count;
    }

    /// <summary>
    /// Lightweight summary of pending changes — counts only, no item allocations.
    /// </summary>
    public class ChangeSetSummary
    {
        /// <summary>Number of items with Added state.</summary>
        public int InsertCount { get; set; }

        /// <summary>Number of items with Modified state.</summary>
        public int UpdateCount { get; set; }

        /// <summary>Number of items pending deletion.</summary>
        public int DeleteCount { get; set; }

        /// <summary>True when any pending changes exist.</summary>
        public bool IsDirty => InsertCount > 0 || UpdateCount > 0 || DeleteCount > 0;

        /// <summary>Total count of all changes.</summary>
        public int TotalCount => InsertCount + UpdateCount + DeleteCount;
    }
}
