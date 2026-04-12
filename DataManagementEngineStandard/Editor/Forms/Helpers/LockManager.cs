using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Client-side record locking manager.
    /// Tracks which records are locked per block, supports multiple lock modes,
    /// and fires lockable hooks without WinForms dependency.
    /// </summary>
    public class LockManager : ILockManager
    {
        // [blockName] -> LockMode
        private readonly Dictionary<string, LockMode> _modes
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, bool> _lockOnEdit
            = new(StringComparer.OrdinalIgnoreCase);

        // [blockName][recordIndex] -> RecordLockInfo
        private readonly Dictionary<string, Dictionary<int, RecordLockInfo>> _locks
            = new(StringComparer.OrdinalIgnoreCase);

        // Track "current" record index per block (set externally by FormsManager)
        private readonly Dictionary<string, int> _currentIndex
            = new(StringComparer.OrdinalIgnoreCase);

        #region Mode Configuration

        /// <summary>
        /// Returns the configured lock mode for a block.
        /// </summary>
        public LockMode GetLockMode(string blockName)
        {
            _modes.TryGetValue(blockName, out var mode);
            return mode; // defaults to LockMode.None (0)
        }

        /// <summary>
        /// Sets the lock mode for a block.
        /// </summary>
        public void SetLockMode(string blockName, LockMode mode)
        {
            _modes[blockName] = mode;
        }

        /// <summary>
        /// Returns whether automatic lock-on-edit is enabled for a block.
        /// </summary>
        public bool GetLockOnEdit(string blockName)
        {
            _lockOnEdit.TryGetValue(blockName, out var value);
            return value;
        }

        /// <summary>
        /// Sets whether a block should automatically lock the current record when editing begins.
        /// </summary>
        public void SetLockOnEdit(string blockName, bool value)
        {
            _lockOnEdit[blockName] = value;
        }

        #endregion

        #region Current Index Tracking

        /// <summary>
        /// Update the tracked "current" record index for a block.
        /// Called by FormsManager on navigation.
        /// </summary>
        public void SetCurrentRecordIndex(string blockName, int index)
        {
            _currentIndex[blockName] = index;
        }

        private int GetCurrentRecordIndex(string blockName)
        {
            _currentIndex.TryGetValue(blockName, out var idx);
            return idx;
        }

        #endregion

        #region Lock / Unlock

        /// <summary>
        /// Locks the current record for a block when locking is enabled.
        /// </summary>
        public Task<bool> LockCurrentRecordAsync(string blockName, CancellationToken ct = default)
        {
            if (GetLockMode(blockName) == LockMode.None)
                return Task.FromResult(true);

            var idx = GetCurrentRecordIndex(blockName);
            EnsureBlock(blockName);

            if (_locks[blockName].ContainsKey(idx))
                return Task.FromResult(true); // idempotent

            _locks[blockName][idx] = new RecordLockInfo
            {
                BlockName = blockName,
                RecordIndex = idx,
                LockTime = DateTime.UtcNow,
                LockedBy = Environment.UserName
            };

            return Task.FromResult(true);
        }

        /// <summary>
        /// Unlocks the current record for a block.
        /// </summary>
        public bool UnlockCurrentRecord(string blockName)
        {
            var idx = GetCurrentRecordIndex(blockName);
            if (_locks.TryGetValue(blockName, out var blockLocks))
                return blockLocks.Remove(idx);
            return false;
        }

        /// <summary>
        /// Unlocks all tracked records for a block.
        /// </summary>
        public void UnlockAllRecords(string blockName)
        {
            if (_locks.ContainsKey(blockName))
                _locks[blockName].Clear();
        }

        #endregion

        #region Query

        /// <summary>
        /// Returns whether a specific record is currently locked.
        /// </summary>
        public bool IsRecordLocked(string blockName, int recordIndex)
        {
            return _locks.TryGetValue(blockName, out var block) &&
                   block.ContainsKey(recordIndex);
        }

        /// <summary>
        /// Returns whether the current record for a block is currently locked.
        /// </summary>
        public bool IsCurrentRecordLocked(string blockName)
        {
            return IsRecordLocked(blockName, GetCurrentRecordIndex(blockName));
        }

        /// <summary>
        /// Returns lock metadata for a specific record.
        /// </summary>
        public RecordLockInfo GetLockInfo(string blockName, int recordIndex)
        {
            if (_locks.TryGetValue(blockName, out var block) &&
                block.TryGetValue(recordIndex, out var info))
                return info;
            return null;
        }

        /// <summary>
        /// Returns the number of tracked locked records for a block.
        /// </summary>
        public int GetLockedRecordCount(string blockName)
        {
            return _locks.TryGetValue(blockName, out var block) ? block.Count : 0;
        }

        /// <summary>
        /// Returns all tracked locks for a block.
        /// </summary>
        public IReadOnlyList<RecordLockInfo> GetAllLocks(string blockName)
        {
            if (_locks.TryGetValue(blockName, out var block))
                return block.Values.ToList().AsReadOnly();
            return Array.Empty<RecordLockInfo>();
        }

        #endregion

        #region Auto-Lock

        /// <summary>
        /// Locks the current record when the block is configured for automatic lock-on-edit behavior.
        /// </summary>
        public async Task<bool> AutoLockIfNeededAsync(string blockName, CancellationToken ct = default)
        {
            if (!GetLockOnEdit(blockName))
                return true;
            if (GetLockMode(blockName) != LockMode.Automatic)
                return true;
            if (IsCurrentRecordLocked(blockName))
                return true;

            return await LockCurrentRecordAsync(blockName, ct);
        }

        #endregion

        #region Private

        private void EnsureBlock(string blockName)
        {
            if (!_locks.ContainsKey(blockName))
                _locks[blockName] = new Dictionary<int, RecordLockInfo>();
        }

        #endregion
    }
}
