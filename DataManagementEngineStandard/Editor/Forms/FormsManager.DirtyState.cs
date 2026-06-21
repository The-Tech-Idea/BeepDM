using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        #region Dirty State Management (Delegated)

        /// <summary>
        /// Checks for unsaved changes in a block and its children, prompts user for action
        /// </summary>
        public async Task<bool> CheckAndHandleUnsavedChangesAsync(string blockName)
        {
            return await _dirtyStateManager.CheckAndHandleUnsavedChangesAsync(blockName);
        }

        /// <summary>
        /// Checks if any blocks have unsaved changes
        /// </summary>
        public bool HasUnsavedChanges()
        {
            return _dirtyStateManager.HasUnsavedChanges();
        }

        /// <summary>
        /// Gets all dirty blocks
        /// </summary>
        public List<string> GetDirtyBlocks()
        {
            return _dirtyStateManager?.GetDirtyBlocks() ?? new List<string>();
        }

        /// <summary>
        /// Saves all currently dirty blocks
        /// </summary>
        public async Task<bool> SaveDirtyBlocksAsync()
        {
            var dirtyBlocks = GetDirtyBlocks();
            if (dirtyBlocks.Count == 0)
                return true;
            return await _dirtyStateManager.SaveDirtyBlocksAsync(dirtyBlocks);
        }

        /// <summary>
        /// Rolls back all currently dirty blocks
        /// </summary>
        public async Task<bool> RollbackDirtyBlocksAsync()
        {
            var dirtyBlocks = GetDirtyBlocks();
            if (dirtyBlocks.Count == 0)
                return true;
            return await _dirtyStateManager.RollbackDirtyBlocksAsync(dirtyBlocks);
        }

        #endregion
    }
}
