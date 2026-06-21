using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Represents a savepoint captured at a moment in time for a block.
    /// Used by ISavepointManager to snapshot and restore block state.
    /// </summary>
    public class SavepointInfo
    {
        /// <summary>Gets or sets the savepoint name</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the block this savepoint belongs to</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets when the savepoint was created (UTC)</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Process-wide monotonic sequence number assigned at
        /// creation time. Used by <c>SavepointManager.RollbackToSavepointAsync</c>
        /// to order savepoints reliably across sub-tick creations
        /// (audit pass 3, 2026-06). Two savepoints created in the
        /// same <see cref="DateTime.UtcNow.Ticks"/> window can have
        /// distinct SequenceNumber values.
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>Gets or sets the record index at savepoint time</summary>
        public int RecordIndex { get; set; }

        /// <summary>Gets or sets the total record count at savepoint time</summary>
        public int RecordCount { get; set; }

        /// <summary>Gets or sets whether the block was dirty at savepoint time</summary>
        public bool WasDirty { get; set; }

        /// <summary>
        /// Snapshot of field values at savepoint creation (field -> value).
        /// Marked <c>init</c>-only to prevent callers from mutating
        /// the snapshot after the savepoint is recorded (audit pass 3,
        /// 2026-06). The dictionary itself is still mutable; the
        /// <c>init</c> restriction applies to the property reference.
        /// </summary>
        public Dictionary<string, object> RecordSnapshot { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Gets the age of this savepoint</summary>
        public TimeSpan Age => DateTime.UtcNow - Timestamp;
    }
}
