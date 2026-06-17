using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Result of mode transition validation (e.g., ENTER_QUERY → EXECUTE_QUERY).
    /// </summary>
    public class ModeTransitionValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public List<string> ValidationIssues { get; set; } = new();
    }

    /// <summary>
    /// Information about a block's runtime mode and state.
    /// Used for mode-transition introspection and dashboard display.
    /// </summary>
    public class BlockModeInfo
    {
        public string BlockName { get; set; }
        public DataBlockMode CurrentMode { get; set; }
        public DateTime LastModeChange { get; set; }
        public bool HasUnsavedChanges { get; set; }
        public int RecordCount { get; set; }
        public bool IsCurrentBlock { get; set; }

        public string Summary =>
            $"{BlockName}: {CurrentMode} mode, {RecordCount} records" +
            (HasUnsavedChanges ? " (unsaved changes)" : "") +
            (IsCurrentBlock ? " (current)" : "");
    }
}
