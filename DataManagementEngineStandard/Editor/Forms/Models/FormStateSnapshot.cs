using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>Snapshot of the complete form state for save/restore.</summary>
    public class FormStateSnapshot
    {
        public string FormName      { get; set; }
        public DateTime CapturedAt  { get; set; } = DateTime.Now;
        public string CurrentBlock  { get; set; }
        public Dictionary<string, BlockStateSnapshot> BlockStates { get; set; } = new();
    }

    /// <summary>Snapshot of an individual block's navigation and filter state.</summary>
    public class BlockStateSnapshot
    {
        public string BlockName        { get; set; }
        public int    CursorPosition   { get; set; }
        public string Mode             { get; set; }
        public string FilterExpression { get; set; }
        public bool   IsDirty          { get; set; }
        public int    RecordCount      { get; set; }
    }
}
