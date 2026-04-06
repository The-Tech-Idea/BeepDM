using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>A single entry in block navigation history.</summary>
    public class NavigationHistoryEntry
    {
        public int      RecordIndex { get; set; }
        public DateTime VisitedAt   { get; set; } = DateTime.Now;
    }
}
