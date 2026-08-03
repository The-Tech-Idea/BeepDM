using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;

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
            return await _dirtyStateManager.CheckAndHandleUnsavedChangesAsync(blockName).ConfigureAwait(false);
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

            var saved = await _dirtyStateManager.SaveDirtyBlocksAsync(dirtyBlocks).ConfigureAwait(false);

            // Flush pending audit entries on a block-level save too.
            //
            // AuditManager accumulates field changes in a pending buffer and only
            // moves them to the store on a flush, and the sole flush was in
            // CommitFormAsync. The UI saves through the block path — a Save
            // button, F10, WinFormBlockHost.SaveAsync — so audit entries for
            // everything a user actually does were left pending forever and never
            // reached GetAuditLog. The audit panel would show nothing for a
            // normal editing session. (2026-08-02)
            if (saved)
            {
                _auditManager?.FlushPendingToStore(
                    _currentFormName ?? "FORM", AuditOperation.Commit);
            }

            return saved;
        }

        /// <summary>
        /// Rolls back all currently dirty blocks
        /// </summary>
        public async Task<bool> RollbackDirtyBlocksAsync()
        {
            var dirtyBlocks = GetDirtyBlocks();
            if (dirtyBlocks.Count == 0)
                return true;
            return await _dirtyStateManager.RollbackDirtyBlocksAsync(dirtyBlocks).ConfigureAwait(false);
        }

        #endregion
    }
}
