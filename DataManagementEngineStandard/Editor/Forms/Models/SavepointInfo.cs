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

        /// <summary>Gets or sets the record index at savepoint time</summary>
        public int RecordIndex { get; set; }

        /// <summary>Gets or sets the total record count at savepoint time</summary>
        public int RecordCount { get; set; }

        /// <summary>Gets or sets whether the block was dirty at savepoint time</summary>
        public bool WasDirty { get; set; }

        /// <summary>Snapshot of field values at savepoint creation (field -> value)</summary>
        public Dictionary<string, object> RecordSnapshot { get; set; } = new();

        /// <summary>Gets the age of this savepoint</summary>
        public TimeSpan Age => DateTime.UtcNow - Timestamp;
    }
}
