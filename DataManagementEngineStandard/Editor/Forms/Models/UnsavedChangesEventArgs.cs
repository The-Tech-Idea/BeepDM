using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Enhanced event args for handling unsaved changes - Oracle Forms style
    /// </summary>
    public class UnsavedChangesEventArgs : EventArgs
    {
        /// <summary>Gets the name of the block that triggered the event</summary>
        public string BlockName { get; }
        
        /// <summary>Gets the list of all dirty blocks</summary>
        public List<string> DirtyBlocks { get; }
        
        /// <summary>Gets or sets detailed information about dirty blocks</summary>
        public List<DirtyBlockInfo> DirtyBlockDetails { get; set; }
        
        /// <summary>Gets or sets the total number of affected records</summary>
        public int TotalAffectedRecords { get; set; }
        
        /// <summary>Gets or sets the estimated time to save all changes</summary>
        public TimeSpan EstimatedSaveTime { get; set; }
        
        /// <summary>Gets or sets the user's choice for handling unsaved changes</summary>
        public UnsavedChangesAction UserChoice { get; set; } = UnsavedChangesAction.Cancel;
        
        /// <summary>Gets or sets the save options to use if saving</summary>
        public SaveOptions SaveOptions { get; set; } = SaveOptions.Default;
        
        /// <summary>Gets or sets the rollback options to use if discarding</summary>
        public RollbackOptions RollbackOptions { get; set; } = RollbackOptions.Default;
        
        /// <summary>Gets or sets a descriptive message</summary>
        public string Message { get; set; }
        
        /// <summary>Gets or sets whether the operation was cancelled</summary>
        public bool Cancel { get; set; }
        
        /// <summary>Gets the timestamp when the event was created</summary>
        public DateTime Timestamp { get; } = DateTime.Now;
        
        /// <summary>Initializes a new instance of the UnsavedChangesEventArgs class</summary>
        /// <param name="blockName">The name of the block that triggered the event</param>
        /// <param name="dirtyBlocks">The list of all dirty blocks</param>
        public UnsavedChangesEventArgs(string blockName, List<string> dirtyBlocks)
        {
            BlockName = blockName;
            DirtyBlocks = dirtyBlocks ?? new List<string>();
            Message = $"Block '{blockName}' and {DirtyBlocks.Count} related blocks have unsaved changes.";
        }
    }
}