using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Tracks the state and change history of a single entity in an ObservableBindingList.
    /// </summary>
    public class Tracking
    {
        public Guid UniqueId { get; set; }
        public int OriginalIndex { get; set; }
        public int CurrentIndex { get; set; }
        public EntityState EntityState { get; set; } = EntityState.Unchanged;
        public bool IsSaved { get; set; } = false;
        public bool IsNew { get; set; } = false;
        public string EntityName { get; set; }
        public string PKFieldName { get; set; }
        public string PKFieldValue { get; set; }
        public string PKFieldNameType { get; set; } // Can Int or string or Guid

        /// <summary>
        /// Snapshot of all property values at the time of first modification.
        /// Populated once when EntityState transitions from Unchanged to Modified.
        /// </summary>
        public Dictionary<string, object> OriginalValues { get; set; }

        /// <summary>
        /// List of property names that have been modified since the last accept.
        /// </summary>
        public List<string> ModifiedProperties { get; set; } = new List<string>();

        /// <summary>
        /// UTC timestamp of the most recent modification.
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Identity of the user who last modified this entity.
        /// </summary>
        public string ModifiedBy { get; set; }

        /// <summary>
        /// Incremented on each property change. Useful for optimistic concurrency.
        /// </summary>
        public int Version { get; set; } = 0;

        /// <summary>
        /// True when EntityState is anything other than Unchanged.
        /// </summary>
        public bool IsDirty => EntityState != EntityState.Unchanged;

        public Tracking(Guid uniqueId, int originalIndex)
        {
            UniqueId = uniqueId;
            OriginalIndex = originalIndex;
            CurrentIndex = originalIndex;
        }

        public Tracking(Guid uniqueId, int originalIndex, int currentindex)
        {
            UniqueId = uniqueId;
            OriginalIndex = originalIndex;
            CurrentIndex = currentindex;
        }
    }
}
