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
    /// In-memory savepoint manager. Maintains a dictionary of named savepoints per block.
    /// Oracle Forms equivalent: SAVEPOINT / ROLLBACK TO SAVEPOINT / RELEASE SAVEPOINT.
    /// </summary>
    public class SavepointManager : ISavepointManager
    {
        // [blockName][savepointName] -> SavepointInfo
        private readonly Dictionary<string, Dictionary<string, SavepointInfo>> _store
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, int> _counters
            = new(StringComparer.OrdinalIgnoreCase);

        #region Create

        /// <summary>
        /// Creates a named savepoint for a block using default record metadata.
        /// </summary>
        public string CreateSavepoint(string blockName, string savepointName = null)
        {
            return CreateSavepoint(blockName, savepointName, 0, 0, false, null);
        }

        /// <summary>
        /// Creates a named savepoint for a block with the supplied record metadata and optional snapshot values.
        /// </summary>
        public string CreateSavepoint(
            string blockName,
            string savepointName,
            int recordIndex,
            int recordCount,
            bool isDirty,
            Dictionary<string, object> snapshot = null)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                throw new ArgumentException("blockName is required", nameof(blockName));

            if (string.IsNullOrWhiteSpace(savepointName))
            {
                _counters.TryGetValue(blockName, out var counter);
                savepointName = $"SP_{++counter}_{DateTime.UtcNow.Ticks}";
                _counters[blockName] = counter;
            }

            EnsureBlock(blockName);
            _store[blockName][savepointName] = new SavepointInfo
            {
                Name = savepointName,
                BlockName = blockName,
                Timestamp = DateTime.UtcNow,
                RecordIndex = recordIndex,
                RecordCount = recordCount,
                WasDirty = isDirty,
                RecordSnapshot = snapshot ?? new Dictionary<string, object>()
            };

            return savepointName;
        }

        #endregion

        #region Rollback

        /// <summary>
        /// Rolls back the savepoint stack for a block to the specified savepoint.
        /// </summary>
        public Task<bool> RollbackToSavepointAsync(
            string blockName,
            string savepointName,
            CancellationToken ct = default)
        {
            if (!SavepointExists(blockName, savepointName))
                return Task.FromResult(false);

            var sp = _store[blockName][savepointName];

            // Remove savepoints created AFTER this one
            var toRemove = _store[blockName]
                .Where(kvp => kvp.Value.Timestamp > sp.Timestamp)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
                _store[blockName].Remove(key);

            // Caller (FormsManager) uses sp.RecordIndex / sp.RecordSnapshot
            // to restore actual UOW state.
            return Task.FromResult(true);
        }

        #endregion

        #region Release

        /// <summary>
        /// Releases a single named savepoint for a block.
        /// </summary>
        public bool ReleaseSavepoint(string blockName, string savepointName)
        {
            if (_store.TryGetValue(blockName, out var block))
                return block.Remove(savepointName);
            return false;
        }

        /// <summary>
        /// Releases all savepoints recorded for a block.
        /// </summary>
        public void ReleaseAllSavepoints(string blockName)
        {
            if (_store.ContainsKey(blockName))
                _store[blockName].Clear();
        }

        #endregion

        #region Query

        /// <summary>
        /// Lists the savepoints recorded for a block in timestamp order.
        /// </summary>
        public IReadOnlyList<SavepointInfo> ListSavepoints(string blockName)
        {
            if (_store.TryGetValue(blockName, out var block))
                return block.Values.OrderBy(sp => sp.Timestamp).ToList().AsReadOnly();
            return Array.Empty<SavepointInfo>();
        }

        /// <summary>
        /// Returns whether a named savepoint exists for a block.
        /// </summary>
        public bool SavepointExists(string blockName, string savepointName)
        {
            return _store.TryGetValue(blockName, out var block) &&
                   block.ContainsKey(savepointName);
        }

        #endregion

        #region Private

        private void EnsureBlock(string blockName)
        {
            if (!_store.ContainsKey(blockName))
                _store[blockName] = new Dictionary<string, SavepointInfo>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion
    }
}
