using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        #region Block Registration and Management

        /// <summary>
        /// Registers a data block with the manager using schema metadata already carried by the unit of work.
        /// </summary>
        public void RegisterBlock(string blockName, IUnitofWork unitOfWork,
            string dataSourceName = null, bool isMasterBlock = false)
        {
            RegisterBlock(blockName, unitOfWork, null, dataSourceName, isMasterBlock);
        }

        /// <summary>
        /// Registers a data block with the manager
        /// </summary>
        public void RegisterBlock(string blockName, IUnitofWork unitOfWork, IEntityStructure entityStructure,
            string dataSourceName = null, bool isMasterBlock = false)
        {
            ValidateBlockRegistrationParameters(blockName, unitOfWork);

            // B4 (audit pass 3, 2026-06): re-registration safety.
            // The previous version silently overwrote an existing
            // registration: _blocks[blockName] = blockInfo at L57
            // would replace the previous block, but the old UoW's
            // event subscriptions (ItemChanged, CurrentChanged) were
            // never unsubscribed — the old UoW kept firing events
            // into handlers that now thought they belonged to the
            // new UoW. The _itemChangedHandlers and
            // _mdCurrentChangedHandlers dicts were also overwritten,
            // losing the old handler reference (so UnregisterBlock
            // couldn't unsubscribe even if it tried). Result: stale
            // subscriptions, leaked UoW references, no way to clean
            // up.
            //
            // Fix: if the block is already registered, unregister
            // the old registration first. This is the standard
            // "replace with re-init" pattern. Hosts that want to
            // detect a duplicate registration can check BlockExists
            // before calling RegisterBlock.
            if (_blocks.ContainsKey(blockName))
            {
                LogOperation(
                    $"Block '{blockName}' is already registered; auto-unregistering before re-registration",
                    blockName);
                UnregisterBlock(blockName);
            }

            try
            {
                var resolvedEntityStructure = ResolveBlockEntityStructure(unitOfWork, entityStructure, dataSourceName);
                if (resolvedEntityStructure == null)
                    throw new ArgumentNullException(nameof(entityStructure),
                        $"Block '{blockName}' requires entity metadata. Pass IEntityStructure explicitly or ensure UnitOfWork.EntityStructure is populated.");

                // B6 (audit pass 3, 2026-06): resolve config
                // and build blockInfo BEFORE we touch _blocks or
                // subscribe events. The previous version stored
                // blockInfo in _blocks first, then called
                // ApplyBlockConfiguration. If config failed, the
                // block was half-registered: in _blocks, with
                // event subscriptions, but the success log at
                // L118 had already fired. The catch would then
                // fire, but the block was still in _blocks.
                //
                // New order: validate config first (without
                // committing to _blocks), then commit atomically.
                var blockInfo = CreateBlockInfo(blockName, unitOfWork, resolvedEntityStructure, dataSourceName, isMasterBlock);
                ApplyBlockConfiguration(blockInfo);

                // Commit: cache, store, subscribe events. After
                // this point the block is fully visible to other
                // code paths.
                _performanceManager.CacheBlockInfo(blockName, blockInfo);
                _blocks[blockName] = blockInfo;

                // Subscribe to unit of work events
                if (unitOfWork != null)
                {
                    _eventManager.SubscribeToUnitOfWorkEvents(unitOfWork, blockName);

                    EventHandler<ItemChangedEventArgs<Entity>> handler = (s, e) =>
                    {
                        var idx = unitOfWork.Units != null
                            ? unitOfWork.Units.IndexOf(e.Item)
                            : -1;
                        // Read the new field value off the record. Note: the
                        // previous code used `typeof(Entity).GetProperty(...)`
                        // which assumed every record is an `Entity` and
                        // silently returned null for any non-Entity record
                        // (e.g. anonymous projections, EF Core entities,
                        // POCOs). RecordPropertyAccessor reads from the
                        // actual runtime type of e.Item, with a cached
                        // PropertyInfo lookup and a throttled warning on
                        // miss.
                        var newVal = RecordPropertyAccessor.GetValue(
                            e.Item,
                            e.PropertyName,
                            _dmeEditor);

                        OnBlockFieldChanged?.Invoke(this, new BlockFieldChangedEventArgs
                        {
                            BlockName   = blockName,
                            FieldName   = e.PropertyName,
                            NewValue    = newVal,
                            RecordIndex = idx
                        });

                        if (!string.IsNullOrWhiteSpace(e.PropertyName))
                        {
                            PrepareValidationContext(blockName);
                            _validationManager.ValidateItem(blockName, e.PropertyName, newVal, ValidationTiming.OnChange);

                            if (_lovManager.HasLOV(blockName, e.PropertyName))
                            {
                                _ = _lovManager.ValidateLOVValueAsync(blockName, e.PropertyName, newVal);
                            }
                        }
                    };
                    // B3 (audit pass 3, 2026-06): the B4
                    // auto-unregister at the top of RegisterBlock
                    // has already cleared any stale entry in
                    // _itemChangedHandlers. The current path
                    // reaches here only on a fresh registration,
                    // so the dict write below is unambiguous.
                    // (The previous version's bug was that the
                    // dict write overwrote the old handler
                    // without unsubscribing the old UoW — that
                    // scenario is now impossible because B4 has
                    // already torn down the old UoW's
                    // subscriptions.)
                    unitOfWork.ItemChanged += handler;
                    _itemChangedHandlers[blockName] = handler;

                    // Form-level coordination uses FormsManager-owned relationship state.
                    EventHandler mdHandler = async (s, e) =>
                    {
                        // B7 (audit pass 3, 2026-06): the
                        // previous version was an async-void
                        // event handler with no try/catch. A
                        // throw from SynchronizeDetailBlocksAsync
                        // would be unobserved (the await is in a
                        // fire-and-forget void-returning lambda),
                        // and the event subscriber pipeline would
                        // never see it. Now: try/catch and route
                        // to the error event so the host gets a
                        // diagnostic.
                        try
                        {
                            if (!IsSyncSuppressed(blockName) && GetDetailBlocks(blockName).Any())
                                await SynchronizeDetailBlocksAsync(blockName);
                        }
                        catch (Exception ex)
                        {
                            _eventManager.TriggerError(blockName, ex);
                        }
                    };
                    unitOfWork.CurrentChanged += mdHandler;
                    _mdCurrentChangedHandlers[blockName] = mdHandler;
                }

                Status = $"Block '{blockName}' registered successfully";
                LogOperation($"Block '{blockName}' registered", blockName);

                // Trigger block enter event
                _eventManager.TriggerBlockEnter(blockName);
            }
            catch (Exception ex)
            {
                Status = $"Error registering block '{blockName}': {ex.Message}";
                LogError($"Error registering block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                throw;
            }
        }

        /// <summary>
        /// Registers a block by resolving UoW + EntityStructure from connection name and entity name.
        /// Uses BlockFactory to create the block, then delegates to RegisterBlock.
        /// </summary>
        public async Task<bool> RegisterBlockFromSourceAsync(
            string blockName, string connectionName, string entityName,
            bool isMasterBlock = false, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blockName) ||
                string.IsNullOrWhiteSpace(connectionName) ||
                string.IsNullOrWhiteSpace(entityName))
                return false;

            try
            {
                var (uow, structure) = await _blockFactory
                    .CreateBlockAsync(connectionName, entityName, ct)
                    .ConfigureAwait(false);

                if (uow == null || structure == null)
                {
                    Status = $"Block '{blockName}': could not resolve '{connectionName}.{entityName}'";
                    return false;
                }

                RegisterBlock(blockName, uow, structure, connectionName, isMasterBlock);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"RegisterBlockFromSourceAsync failed for '{blockName}'", ex, blockName);
                return false;
            }
        }

        /// <summary>
        /// Creates a named savepoint for the specified block capturing current record index.
        /// Convenience wrapper around Savepoints.CreateSavepoint (Phase 6).
        /// </summary>
        public string CreateBlockSavepoint(string blockName, string savepointName = null)
        {
            var block = GetBlock(blockName);
            if (block?.UnitOfWork == null)
                return null;

            bool isDirty = block.UnitOfWork.IsDirty;
            int recordCount = block.UnitOfWork.TotalItemCount;
            int recordIndex = 0;
            try
            {
                var current = block.UnitOfWork.CurrentItem;
                if (current != null && block.UnitOfWork.Units != null)
                    recordIndex = block.UnitOfWork.Units.IndexOf(current);
            }
            catch (Exception ex)
            {
                // Units may be null before first query, or the indexer
                // may throw if the backing collection is in an
                // inconsistent state. Either way, the record-index
                // field falls back to 0 (the start of the block).
                LogError($"CreateBlockSavepoint: failed to resolve record index for block '{blockName}'", ex, blockName);
            }

            // Capture the record snapshot. Per-property failures inside
            // RecordPropertyAccessor are caught and logged individually;
            // the catch here is for failures in the dictionary
            // construction itself (e.g. non-generic IDictionary with
            // exotic key types) that the accessor can't handle.
            IDictionary<string, object> snapshot;
            try
            {
                snapshot = CaptureCurrentRecordSnapshot(block.UnitOfWork.CurrentItem);
            }
            catch (Exception ex)
            {
                LogError($"CreateBlockSavepoint: failed to capture record snapshot for block '{blockName}'", ex, blockName);
                snapshot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            return _savepointManager.CreateSavepoint(
                blockName, savepointName, recordIndex, recordCount, isDirty, snapshot);
        }

        /// <summary>
        /// Rolls back a block to a previously created savepoint (Phase 6).
        /// </summary>
        /// <remarks>
        /// Ordering matters here. The savepoint store is mutated LAST,
        /// after the data rollback succeeds, so a partial failure in
        /// the data rollback leaves the savepoint store intact and
        /// the user can retry. If the data rollback fails after the
        /// store was mutated, the user would lose their other
        /// savepoints with no way to recover.
        ///
        /// Flow:
        /// <list type="number">
        ///   <item>Look up the savepoint (read-only).</item>
        ///   <item>Roll back the unit of work (data side).</item>
        ///   <item>Move to the saved record index.</item>
        ///   <item>Restore the record snapshot (best-effort).</item>
        ///   <item>Tell the manager to delete later savepoints (store side).</item>
        /// </list>
        /// </remarks>
        public async Task<bool> RollbackToSavepointAsync(string blockName, string savepointName,
            CancellationToken ct = default)
        {
            // 1. Look up the savepoint. Read-only — no state mutation.
            var savepoint = _savepointManager.ListSavepoints(blockName)
                .FirstOrDefault(sp => string.Equals(sp.Name, savepointName, StringComparison.OrdinalIgnoreCase));

            if (savepoint == null)
                return false;

            var block = GetBlock(blockName);
            var unitOfWork = block?.UnitOfWork;

            if (unitOfWork == null)
            {
                LogError(
                    $"RollbackToSavepointAsync: block '{blockName}' has no unit of work; cannot restore data. " +
                    "Returning true (savepoint will be removed from the store below) is misleading — this is a no-op rollback.",
                    null, blockName);
                // Still proceed to clean the store so the user isn't left
                // with a savepoint they can never roll back to. The
                // alternative (returning false and leaving the store
                // intact) is also defensible; we choose to clean up
                // because a savepoint with no data to roll back to is
                // strictly worse than a silent no-op.
                _ = await _savepointManager.RollbackToSavepointAsync(blockName, savepointName, ct).ConfigureAwait(false);
                return true;
            }

            // 2. Roll back the unit of work. If this fails, we DO NOT
            // touch the savepoint store — the user can retry the
            // rollback.
            var rollbackResult = await unitOfWork.Rollback().ConfigureAwait(false);
            if (rollbackResult?.Flag == Errors.Failed)
            {
                LogError(
                    $"RollbackToSavepointAsync: unit-of-work rollback failed for block '{blockName}' savepoint '{savepointName}'",
                    null, blockName);
                return false;
            }

            // 3. Move to the saved record index. We bound-check first
            // because the index may be out of range if records were
            // deleted between savepoint and rollback. The bound-check
            // is logged but not fatal — a record-count mismatch is a
            // legitimate "the data shape changed" scenario.
            if (savepoint.RecordIndex >= 0 &&
                unitOfWork.TotalItemCount > 0 &&
                savepoint.RecordIndex < unitOfWork.TotalItemCount)
            {
                unitOfWork.MoveTo(savepoint.RecordIndex);
            }
            else if (savepoint.RecordIndex >= 0)
            {
                LogError(
                    $"RollbackToSavepointAsync: saved record index {savepoint.RecordIndex} is out of range for current TotalItemCount {unitOfWork.TotalItemCount} in block '{blockName}'",
                    null, blockName);
            }

            // 4. Restore the record snapshot (best-effort; per-property
            // failures are logged via RecordPropertyAccessor.LogRestoreFailure).
            RestoreCurrentRecordSnapshot(unitOfWork.CurrentItem, savepoint.RecordSnapshot);
            TryUpdateSavepointSystemVariables(blockName, savepoint.RecordIndex, unitOfWork.TotalItemCount);

            // 5. Data rollback succeeded; only NOW delete the later
            // savepoints. If this step throws, the data is in the
            // rolled-back state and the user can manually call
            // ReleaseSavepoint to clean up.
            var rolledBack = await _savepointManager.RollbackToSavepointAsync(blockName, savepointName, ct)
                .ConfigureAwait(false);

            return rolledBack;
        }

        private IDictionary<string, object> CaptureCurrentRecordSnapshot(object currentRecord)
        {
            if (currentRecord == null)
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (currentRecord is IDictionary<string, object> typedDictionary)
                return new Dictionary<string, object>(typedDictionary, StringComparer.OrdinalIgnoreCase);

            if (currentRecord is IDictionary dictionary)
            {
                var snapshot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key == null)
                        continue;

                    snapshot[entry.Key.ToString()] = entry.Value;
                }

                return snapshot;
            }

            // Use RecordPropertyAccessor so the snapshot is built from the
            // same cached PropertyInfo catalog as the rest of FormsManager.
            // The catalog is seeded on first use, so the first snapshot of
            // a record type is a dict walk (not a reflection scan).
            // Promoted from `static` to instance so the accessor can
            // receive `_dmeEditor` for diagnostic logging on read failure.
            // Return type loosened from Dictionary<,> to IDictionary<,>
            // because the accessor returns IDictionary<,> (the catalog
            // produces case-insensitive Dictionary<,> instances, but the
            // caller signature only requires IDictionary).
            return RecordPropertyAccessor.GetAllReadable(currentRecord, _dmeEditor);
        }

        private void RestoreCurrentRecordSnapshot(object currentRecord, IReadOnlyDictionary<string, object> snapshot)
        {
            if (currentRecord == null || snapshot == null || snapshot.Count == 0)
                return;

            if (currentRecord is IDictionary<string, object> typedDictionary)
            {
                foreach (var entry in snapshot)
                    typedDictionary[entry.Key] = entry.Value;

                return;
            }

            if (currentRecord is IDictionary dictionary)
            {
                foreach (var entry in snapshot)
                    dictionary[entry.Key] = entry.Value;

                return;
            }

            // Walk the writable surface of currentRecord through
            // RecordPropertyAccessor so the PropertyInfo cache is shared
            // with get/snapshot. The accessor already filters to
            // non-indexed, publicly-writable properties. The value
            // conversion is delegated to ConvertSnapshotValue (it has
            // special enum/DateTime handling that RecordPropertyAccessor's
            // generic path doesn't replicate).
            //
            // Note: we call property.SetValue directly instead of
            // RecordPropertyAccessor.TrySetValue because TrySetValue uses
            // the generic Convert.ChangeType path, which doesn't have
            // the enum/DateTime special cases. The trade-off is that
            // SetValue failures are caught here and logged via the
            // accessor's diagnostic rather than swallowed silently
            // (audit pass 2026-06: previous version caught silently).
            foreach (var property in RecordPropertyAccessor.EnumerateWritableProperties(currentRecord))
            {
                if (!snapshot.TryGetValue(property.Name, out var value))
                    continue;

                try
                {
                    if (value == null)
                    {
                        if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                            continue;

                        property.SetValue(currentRecord, null);
                        continue;
                    }

                    property.SetValue(currentRecord, ConvertSnapshotValue(value, property.PropertyType));
                }
                catch (Exception ex)
                {
                    // Best-effort restore only. Some projected/read-only
                    // properties may not be writable, or the value in
                    // the snapshot may be incompatible with the property
                    // type after ConvertSnapshotValue's best-effort
                    // conversion. Log via the accessor's diagnostic so
                    // the failure is visible (throttled). Pass the
                    // PropertyInfo so the diagnostic can surface the
                    // target type (otherwise it would print "type ?").
                    RecordPropertyAccessor.LogRestoreFailure(_dmeEditor, currentRecord, property.Name, property, ex);
                }
            }
        }

        private static object ConvertSnapshotValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (effectiveType.IsInstanceOfType(value))
                return value;

            if (effectiveType.IsEnum)
            {
                if (value is string enumName)
                    return Enum.Parse(effectiveType, enumName, ignoreCase: true);

                return Enum.ToObject(effectiveType, value);
            }

            return Convert.ChangeType(value, effectiveType);
        }

        private void TryUpdateSavepointSystemVariables(string blockName, int recordIndex, int totalRecords)
        {
            try
            {
                _systemVariablesManager?.UpdateForRecordChange(blockName, recordIndex, totalRecords);
            }
            catch
            {
                // Savepoint rollback should not fail because system-variable refresh is unavailable.
            }
        }

        /// <summary>
        /// Opens the named datasource if needed, fetches EntityStructure, creates a UnitOfWork,
        /// and registers the block. This is the single-call bootstrap entry point for UI layers
        /// (BeepForms, BeepBlock) that must never touch IDataSource directly.
        /// Delegates to <see cref="RegisterBlockFromSourceAsync"/>.
        /// </summary>
        public Task<bool> SetupBlockAsync(
            string blockName,
            string connectionName,
            string entityName,
            bool isMasterBlock = false,
            CancellationToken cancellationToken = default)
            => RegisterBlockFromSourceAsync(blockName, connectionName, entityName, isMasterBlock, cancellationToken);

        /// <summary>
        /// Unregisters a data block from the manager
        /// </summary>
        public bool UnregisterBlock(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return false;

            try
            {
                if (!_blocks.TryGetValue(blockName, out var blockInfo))
                    return false;

                // Trigger block leave event
                _eventManager.TriggerBlockLeave(blockName);

                // Remove relationships involving this block
                RemoveBlockRelationships(blockName);

                // Unsubscribe from events
                if (blockInfo.UnitOfWork != null)
                {
                    _eventManager.UnsubscribeFromUnitOfWorkEvents(blockInfo.UnitOfWork, blockName);
                }

                // Remove from collections
                _blocks.TryRemove(blockName, out _);
                _navHistoryManager.RemoveBlock(blockName);
                if (_itemChangedHandlers.TryRemove(blockName, out var itemChangedHandler) && blockInfo.UnitOfWork != null)
                    blockInfo.UnitOfWork.ItemChanged -= itemChangedHandler;
                if (_mdCurrentChangedHandlers.TryRemove(blockName, out var mdH) && blockInfo.UnitOfWork != null)
                    blockInfo.UnitOfWork.CurrentChanged -= mdH;
                _syncSuppressCount.TryRemove(blockName, out _);

                // B9 (audit pass 3, 2026-06): invalidate the
                // perf manager cache. RegisterBlock calls
                // _performanceManager.CacheBlockInfo; without
                // this invalidate on unregister, the cache
                // holds a stale DataBlockInfo for a block
                // that's no longer in _blocks. GetBlock checks
                // the cache first and would return a stale
                // entry.
                _performanceManager.InvalidateBlockCache(blockName);

                Status = $"Block '{blockName}' unregistered successfully";
                LogOperation($"Block '{blockName}' unregistered", blockName);
                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error unregistering block '{blockName}': {ex.Message}";
                LogError($"Error unregistering block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a registered block with performance caching
        /// </summary>
        public DataBlockInfo GetBlock(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return null;

            // Try cache first
            var cachedBlock = _performanceManager.GetCachedBlockInfo(blockName);
            if (cachedBlock != null)
                return cachedBlock;

            // Fallback to main collection
            _blocks.TryGetValue(blockName, out var block);
            
            // Cache for future access
            if (block != null)
            {
                _performanceManager.CacheBlockInfo(blockName, block);
            }
            
            return block;
        }

        /// <summary>
        /// Gets the unit of work for a specific block
        /// </summary>
        public IUnitofWork GetUnitOfWork(string blockName)
        {
            return GetBlock(blockName)?.UnitOfWork;
        }

        /// <summary>
        /// Checks if a block exists
        /// </summary>
        public bool BlockExists(string blockName)
        {
            return !string.IsNullOrWhiteSpace(blockName) && _blocks.ContainsKey(blockName);
        }

        #endregion
    }
}
