using System;
using System.Collections.Concurrent;
using System.Threading;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Manages data blocks shared across multiple FormsManager instances.
    /// Provides optimistic locking so only one form modifies a shared block at a time.
    /// </summary>
    public class SharedBlockManager : ISharedBlockManager
    {
        private readonly ConcurrentDictionary<string, IUnitofWork> _sharedBlocks
            = new ConcurrentDictionary<string, IUnitofWork>(StringComparer.OrdinalIgnoreCase);

        // value = name of the form that currently holds the lock
        private readonly ConcurrentDictionary<string, string> _locks
            = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public event EventHandler<SharedBlockChangedEventArgs> SharedBlockChanged;

        /// <inheritdoc/>
        public bool CreateSharedBlock(string blockName, IUnitofWork uow)
        {
            if (string.IsNullOrWhiteSpace(blockName) || uow == null) return false;
            return _sharedBlocks.TryAdd(blockName, uow);
        }

        /// <inheritdoc/>
        public IUnitofWork GetSharedBlock(string blockName)
        {
            _sharedBlocks.TryGetValue(blockName ?? string.Empty, out var uow);
            return uow;
        }

        /// <inheritdoc/>
        public bool SharedBlockExists(string blockName)
            => !string.IsNullOrWhiteSpace(blockName) && _sharedBlocks.ContainsKey(blockName);

        /// <inheritdoc/>
        public bool RemoveSharedBlock(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return false;
            _locks.TryRemove(blockName, out _);
            return _sharedBlocks.TryRemove(blockName, out _);
        }

        /// <inheritdoc/>
        public bool TryLockSharedBlock(string blockName, string lockedBy, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(blockName) || string.IsNullOrWhiteSpace(lockedBy))
                return false;
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                // Already held by this caller — idempotent
                if (_locks.TryGetValue(blockName, out var owner) &&
                    string.Equals(owner, lockedBy, StringComparison.OrdinalIgnoreCase))
                    return true;
                // Try to acquire
                if (_locks.TryAdd(blockName, lockedBy)) return true;
                Thread.Sleep(5);
            }
            return false;
        }

        /// <inheritdoc/>
        public void ReleaseSharedBlockLock(string blockName, string lockedBy)
        {
            if (string.IsNullOrWhiteSpace(blockName) || string.IsNullOrWhiteSpace(lockedBy))
                return;
            if (_locks.TryGetValue(blockName, out var owner) &&
                string.Equals(owner, lockedBy, StringComparison.OrdinalIgnoreCase))
                _locks.TryRemove(blockName, out _);
        }

        /// <summary>
        /// Notify subscribers that a shared block's data has changed.
        /// Call this after committing changes to any shared block.
        /// </summary>
        public void NotifySharedBlockChanged(string blockName, string changedBy, object changedRecord = null)
            => SharedBlockChanged?.Invoke(this, new SharedBlockChangedEventArgs
            {
                BlockName = blockName,
                ChangedBy = changedBy,
                ChangedRecord = changedRecord
            });
    }
}
