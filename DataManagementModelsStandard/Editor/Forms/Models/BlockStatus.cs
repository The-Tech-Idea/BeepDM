namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Snapshot of a block's runtime status for IDE display.
    /// Read from IUnitofWorksManager without reflection.
    /// </summary>
    public class BlockStatus
    {
        public string BlockName { get; set; }
        public int RecordCount { get; set; }
        public int CurrentRecordIndex { get; set; }
        public bool IsInQueryMode { get; set; }
        public string CurrentMode { get; set; }
        public bool HasUnsavedChanges { get; set; }
    }
}
