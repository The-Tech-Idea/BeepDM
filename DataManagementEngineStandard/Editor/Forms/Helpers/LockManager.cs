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

        public LockMode GetLockMode(string blockName)
        {
            _modes.TryGetValue(blockName, out var mode);
            return mode; // defaults to LockMode.None (0)
        }

        public void SetLockMode(string blockName, LockMode mode)
        {
            _modes[blockName] = mode;
        }

        public bool GetLockOnEdit(string blockName)
        {
            _lockOnEdit.TryGetValue(blockName, out var value);
            return value;
        }

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

        public bool UnlockCurrentRecord(string blockName)
        {
            var idx = GetCurrentRecordIndex(blockName);
            if (_locks.TryGetValue(blockName, out var blockLocks))
                return blockLocks.Remove(idx);
            return false;
        }

        public void UnlockAllRecords(string blockName)
        {
            if (_locks.ContainsKey(blockName))
                _locks[blockName].Clear();
        }

        #endregion

        #region Query

        public bool IsRecordLocked(string blockName, int recordIndex)
        {
            return _locks.TryGetValue(blockName, out var block) &&
                   block.ContainsKey(recordIndex);
        }

        public bool IsCurrentRecordLocked(string blockName)
        {
            return IsRecordLocked(blockName, GetCurrentRecordIndex(blockName));
        }

        public RecordLockInfo GetLockInfo(string blockName, int recordIndex)
        {
            if (_locks.TryGetValue(blockName, out var block) &&
                block.TryGetValue(recordIndex, out var info))
                return info;
            return null;
        }

        public int GetLockedRecordCount(string blockName)
        {
            return _locks.TryGetValue(blockName, out var block) ? block.Count : 0;
        }

        public IReadOnlyList<RecordLockInfo> GetAllLocks(string blockName)
        {
            if (_locks.TryGetValue(blockName, out var block))
                return block.Values.ToList().AsReadOnly();
            return Array.Empty<RecordLockInfo>();
        }

        #endregion

        #region Auto-Lock

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
