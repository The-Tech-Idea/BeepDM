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

            try
            {
                var resolvedEntityStructure = ResolveBlockEntityStructure(unitOfWork, entityStructure, dataSourceName);
                if (resolvedEntityStructure == null)
                    throw new ArgumentNullException(nameof(entityStructure),
                        $"Block '{blockName}' requires entity metadata. Pass IEntityStructure explicitly or ensure UnitOfWork.EntityStructure is populated.");

                var blockInfo = CreateBlockInfo(blockName, unitOfWork, resolvedEntityStructure, dataSourceName, isMasterBlock);
                
                // Register with performance manager for caching
                _performanceManager.CacheBlockInfo(blockName, blockInfo);
                
                // Store in main collection
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
                        var newVal = e.Item != null
                            ? typeof(Entity).GetProperty(e.PropertyName
                                ?? string.Empty,
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.Instance
                                | System.Reflection.BindingFlags.IgnoreCase)
                                ?.GetValue(e.Item)
                            : null;

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
                    unitOfWork.ItemChanged += handler;
                    _itemChangedHandlers[blockName] = handler;

                    // Form-level coordination uses FormsManager-owned relationship state.
                    EventHandler mdHandler = async (s, e) =>
                    {
                        if (!IsSyncSuppressed(blockName) && GetDetailBlocks(blockName).Any())
                            await SynchronizeDetailBlocksAsync(blockName);
                    };
                    unitOfWork.CurrentChanged += mdHandler;
                    _mdCurrentChangedHandlers[blockName] = mdHandler;
                }

                // Apply configuration defaults
                ApplyBlockConfiguration(blockInfo);

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
            catch { /* Units may be null before first query */ }

            var snapshot = CaptureCurrentRecordSnapshot(block.UnitOfWork.CurrentItem);

            return _savepointManager.CreateSavepoint(
                blockName, savepointName, recordIndex, recordCount, isDirty, snapshot);
        }

        /// <summary>
        /// Rolls back a block to a previously created savepoint (Phase 6).
        /// </summary>
        public async Task<bool> RollbackToSavepointAsync(string blockName, string savepointName,
            CancellationToken ct = default)
        {
            var savepoint = _savepointManager.ListSavepoints(blockName)
                .FirstOrDefault(sp => string.Equals(sp.Name, savepointName, StringComparison.OrdinalIgnoreCase));

            if (savepoint == null)
                return false;

            var rolledBack = await _savepointManager.RollbackToSavepointAsync(blockName, savepointName, ct)
                .ConfigureAwait(false);

            if (!rolledBack)
                return false;

            var block = GetBlock(blockName);
            var unitOfWork = block?.UnitOfWork;
            if (unitOfWork == null)
                return true;

            var rollbackResult = await unitOfWork.Rollback().ConfigureAwait(false);
            if (rollbackResult?.Flag == Errors.Failed)
                return false;

            if (savepoint.RecordIndex >= 0 && unitOfWork.TotalItemCount > 0 && savepoint.RecordIndex < unitOfWork.TotalItemCount)
            {
                unitOfWork.MoveTo(savepoint.RecordIndex);
            }

            RestoreCurrentRecordSnapshot(unitOfWork.CurrentItem, savepoint.RecordSnapshot);
            TryUpdateSavepointSystemVariables(blockName, savepoint.RecordIndex, unitOfWork.TotalItemCount);

            return true;
        }

        private static Dictionary<string, object> CaptureCurrentRecordSnapshot(object currentRecord)
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

            return currentRecord.GetType()
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .ToDictionary(property => property.Name, property => property.GetValue(currentRecord), StringComparer.OrdinalIgnoreCase);
        }

        private static void RestoreCurrentRecordSnapshot(object currentRecord, IReadOnlyDictionary<string, object> snapshot)
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

            foreach (var property in currentRecord.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!property.CanWrite || property.GetIndexParameters().Length != 0)
                    continue;

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
                catch
                {
                    // Best-effort restore only. Some projected/read-only properties may not be writable.
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
