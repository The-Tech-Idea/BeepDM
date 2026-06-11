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

        // B3: monotonic sequence number, process-wide, assigned at
        // every CreateSavepoint. Used as a tie-breaker for the
        // "remove savepoints created AFTER this one" filter in
        // RollbackToSavepointAsync, which previously relied solely on
        // DateTime.UtcNow.Ticks and could misorder two savepoints
        // created in the same tick window (~100ns on Windows/Linux).
        private long _sequenceCounter;

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
        /// <param name="blockName">Logical block name (case-insensitive). Required.</param>
        /// <param name="savepointName">
        /// Savepoint name. If null or whitespace, an auto-generated name of the form
        /// <c>SP_{counter}_{utcTicks}</c> is assigned.
        /// </param>
        /// <param name="recordIndex">Record index at savepoint time (0-based).</param>
        /// <param name="recordCount">Total record count at savepoint time.</param>
        /// <param name="isDirty">Whether the block had unsaved changes at savepoint time.</param>
        /// <param name="snapshot">
        /// Field-value snapshot to record with the savepoint. <see cref="IDictionary{TKey, TValue}"/>
        /// is the parameter type so that callers using
        /// <see cref="RecordPropertyAccessor.GetAllReadable(object, IDMEEditor)"/>
        /// can pass the result directly without an explicit cast. The
        /// manager stores a copy in <see cref="SavepointInfo.RecordSnapshot"/>
        /// (a <see cref="Dictionary{TKey, TValue}"/>); if the caller
        /// passes a non-Dictionary implementation, the manager
        /// materializes a new Dictionary with case-insensitive keys.
        /// </param>
        public string CreateSavepoint(
            string blockName,
            string savepointName,
            int recordIndex,
            int recordCount,
            bool isDirty,
            IDictionary<string, object> snapshot = null)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                throw new ArgumentException("blockName is required", nameof(blockName));

            // B4: serialize the counter read+write under a lock so two
            // concurrent CreateSavepoint calls on the same block (with
            // null savepointName) can't both read counter=0, both
            // increment to 1, and produce the same auto-generated name.
            // B3: append a monotonic sequence number so savepoints
            // created in the same DateTime.UtcNow.Ticks window are
            // still ordered correctly.
            string generatedName = null;
            lock (_store)
            {
                if (string.IsNullOrWhiteSpace(savepointName))
                {
                    _counters.TryGetValue(blockName, out var counter);
                    counter++;
                    _counters[blockName] = counter;
                    var seq = Interlocked.Increment(ref _sequenceCounter);
                    generatedName = $"SP_{counter}_{DateTime.UtcNow.Ticks}_{seq}";
                    savepointName = generatedName;
                }

                EnsureBlock(blockName);
                _store[blockName][savepointName] = new SavepointInfo
                {
                    Name = savepointName,
                    BlockName = blockName,
                    Timestamp = DateTime.UtcNow,
                    SequenceNumber = Interlocked.Read(ref _sequenceCounter),
                    RecordIndex = recordIndex,
                    RecordCount = recordCount,
                    WasDirty = isDirty,
                    RecordSnapshot = snapshot == null
                        ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        : (snapshot is Dictionary<string, object> existingDict
                            ? existingDict
                            : new Dictionary<string, object>(snapshot, StringComparer.OrdinalIgnoreCase))
                };
            }

            return savepointName;
        }

        #endregion

        #region Rollback

        /// <summary>
        /// Rolls back the savepoint stack for a block to the specified savepoint.
        /// </summary>
        /// <param name="blockName">Block whose savepoint stack to roll back.</param>
        /// <param name="savepointName">Target savepoint name (case-insensitive).</param>
        /// <param name="ct">
        /// Cancellation token. The method body is fast (in-memory
        /// dict operations only) but the token is observed once at
        /// the start of the body so a caller that cancels
        /// immediately gets a clean <see cref="OperationCanceledException"/>
        /// without any state mutation.
        /// </param>
        /// <returns>
        /// <c>true</c> if the rollback succeeded (i.e. the target
        /// savepoint was found and later savepoints were removed);
        /// <c>false</c> if the savepoint was not found.
        /// </returns>
        /// <remarks>
        /// This method only mutates the savepoint store. The actual
        /// data rollback (restoring the UoW state) is the caller's
        /// responsibility — see
        /// <c>FormsManager.RollbackToSavepointAsync</c>. Callers
        /// should perform the data rollback BEFORE calling this, so
        /// that a partial failure in the data rollback does not leave
        /// the savepoint store in a mutated state.
        ///
        /// "Later" is determined by the savepoint's
        /// <see cref="SavepointInfo.SequenceNumber"/>, which is a
        /// process-wide monotonic counter assigned at creation time
        /// (audit pass 3, 2026-06). This is more reliable than
        /// comparing <see cref="SavepointInfo.Timestamp"/> values,
        /// which can collide for savepoints created in the same
        /// ~100ns tick window.
        /// </remarks>
        public Task<bool> RollbackToSavepointAsync(
            string blockName,
            string savepointName,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!SavepointExists(blockName, savepointName))
                return Task.FromResult(false);

            var sp = _store[blockName][savepointName];

            // Remove savepoints created AFTER this one. We compare on
            // SequenceNumber (monotonic, assigned at creation) rather
            // than Timestamp (which can collide for sub-tick
            // creations). Equal SequenceNumber means "created at the
            // same logical instant" — keep those (don't remove).
            var toRemove = _store[blockName]
                .Where(kvp => kvp.Value.SequenceNumber > sp.SequenceNumber)
                .Select(kvp => kvp.Key)
                .ToList();

            ct.ThrowIfCancellationRequested();

            lock (_store)
            {
                foreach (var key in toRemove)
                    _store[blockName].Remove(key);
            }

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
