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
}
