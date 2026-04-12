using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>A single entry in block navigation history.</summary>
    public class NavigationHistoryEntry
    {
        /// <summary>Gets or sets the record index that was visited.</summary>
        public int      RecordIndex { get; set; }

        /// <summary>Gets or sets when the record was visited.</summary>
        public DateTime VisitedAt   { get; set; } = DateTime.Now;
    }
}
