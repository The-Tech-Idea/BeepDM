using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOW.Models
{
    /// <summary>Lightweight summary of pending changes in a UnitofWork.</summary>
    public class ChangeSummary
    {
        public int InsertCount  { get; set; }
        public int UpdateCount  { get; set; }
        public int DeleteCount  { get; set; }
        public bool IsDirty     => InsertCount > 0 || UpdateCount > 0 || DeleteCount > 0;
        public int TotalChanges => InsertCount + UpdateCount + DeleteCount;
    }

    /// <summary>Progress report issued by CommitBatchAsync.</summary>
    public class CommitBatchProgress
    {
        public int TotalBatches     { get; set; }
        public int CurrentBatch     { get; set; }
        public int RecordsCommitted { get; set; }
        /// <summary>"Insert" | "Update" | "Delete"</summary>
        public string CurrentOperation { get; set; }
    }

    /// <summary>Final result of a CommitBatchAsync call.</summary>
    public class CommitBatchResult
    {
        public bool Success       { get; set; }
        public int TotalCommitted { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
