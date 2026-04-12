using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>Snapshot of the complete form state for save/restore.</summary>
    public class FormStateSnapshot
    {
        /// <summary>Gets or sets the form name captured in the snapshot.</summary>
        public string FormName      { get; set; }

        /// <summary>Gets or sets when the snapshot was captured.</summary>
        public DateTime CapturedAt  { get; set; } = DateTime.Now;

        /// <summary>Gets or sets the current block name captured in the snapshot.</summary>
        public string CurrentBlock  { get; set; }

        /// <summary>Gets or sets the per-block state snapshots keyed by block name.</summary>
        public Dictionary<string, BlockStateSnapshot> BlockStates { get; set; } = new();
    }

    /// <summary>Snapshot of an individual block's navigation and filter state.</summary>
    public class BlockStateSnapshot
    {
        /// <summary>Gets or sets the block name captured in the snapshot.</summary>
        public string BlockName        { get; set; }

        /// <summary>Gets or sets the cursor position captured for the block.</summary>
        public int    CursorPosition   { get; set; }

        /// <summary>Gets or sets the block mode captured in the snapshot.</summary>
        public string Mode             { get; set; }

        /// <summary>Gets or sets the filter expression captured for the block.</summary>
        public string FilterExpression { get; set; }

        /// <summary>Gets or sets whether the block was dirty when captured.</summary>
        public bool   IsDirty          { get; set; }

        /// <summary>Gets or sets the record count captured for the block.</summary>
        public int    RecordCount      { get; set; }
    }
}
