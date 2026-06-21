using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Stores information about a locked record within a block.
    /// </summary>
    public class RecordLockInfo
    {
        /// <summary>Gets or sets the block that owns the lock</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets the index of the locked record</summary>
        public int RecordIndex { get; set; }

        /// <summary>Gets or sets when the lock was acquired (UTC)</summary>
        public DateTime LockTime { get; set; }

        /// <summary>Gets or sets the user who holds the lock</summary>
        public string LockedBy { get; set; }

        /// <summary>Gets how long the lock has been held</summary>
        public TimeSpan Duration => DateTime.UtcNow - LockTime;
    }
}
