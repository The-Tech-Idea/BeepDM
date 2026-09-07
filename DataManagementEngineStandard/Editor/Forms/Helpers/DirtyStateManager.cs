using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Helper class for managing dirty state and unsaved changes in data blocks
    /// </summary>
    public class DirtyStateManager : IDirtyStateManager
    {
        #region Fields
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks;
        private readonly Func<string, List<string>> _getDetailBlocksFunc;
        private readonly Func<string, DataBlockInfo> _getBlockFunc;
        private readonly Func<string, List<DataBlockRelationship>> _getRelationshipsFunc;
        private readonly Func<SaveOptions> _getDefaultSaveOptionsFunc;
        private readonly Func<string, bool> _hasValidationErrorsFunc;
        private static bool IsNullOrEmpty(object value) =>
            value == null || value == DBNull.Value || (value is string text && string.IsNullOrWhiteSpace(text));

        #endregion

        #region Events
        /// <summary>
        /// Raised when an operation encounters unsaved changes and needs a caller decision.
        /// </summary>
        public event EventHandler<UnsavedChangesEventArgs> OnUnsavedChanges;

        #endregion

        #region Constructor
        /// <summary>
        /// Creates a dirty-state manager for the registered blocks owned by a FormsManager instance.
        /// </summary>
        /// <param name="dmeEditor">Editor used for logging and datasource access.</param>
        /// <param name="blocks">Registered block metadata keyed by block name.</param>
        /// <param name="getDetailBlocksFunc">Resolver for child blocks of a given master block.</param>
        /// <param name="getBlockFunc">Resolver for a block metadata record by name.</param>
        /// <param name="getRelationshipsFunc">Resolver for a block's declared master-detail relationships.</param>
        /// <param name="getDefaultSaveOptionsFunc">
        /// Resolver for the manager-configured default <see cref="SaveOptions"/> (typically
        /// <c>() =&gt; Configuration?.DefaultSaveOptions</c>). Optional; when null or when it
        /// returns null, <see cref="SaveOptions.Default"/> is used, matching prior behavior.
        /// </param>
        /// <param name="hasValidationErrorsFunc">
        /// Resolver for whether a named block currently has any item in an error state
        /// (typically <c>blockName =&gt; ItemProperties.GetItemsWithErrors(blockName).Count &gt; 0</c>,
        /// the same live state <see cref="Editor.UOWManager.Interfaces.IItemPropertyManager"/>
        /// tracks from real validation-rule failures via <c>SetItemError</c>/<c>ClearItemError</c>).
        /// Optional; when null, <see cref="HasValidationErrors"/> conservatively reports false
        /// (no known errors) rather than fabricating a state it cannot observe.
        /// </param>
        public DirtyStateManager(
            IDMEEditor dmeEditor,
            ConcurrentDictionary<string, DataBlockInfo> blocks,
            Func<string, List<string>> getDetailBlocksFunc,
            Func<string, DataBlockInfo> getBlockFunc,
            Func<string, List<DataBlockRelationship>> getRelationshipsFunc,
            Func<SaveOptions> getDefaultSaveOptionsFunc = null,
            Func<string, bool> hasValidationErrorsFunc = null)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
            _getDetailBlocksFunc = getDetailBlocksFunc ?? throw new ArgumentNullException(nameof(getDetailBlocksFunc));
            _getBlockFunc = getBlockFunc ?? throw new ArgumentNullException(nameof(getBlockFunc));
            _getRelationshipsFunc = getRelationshipsFunc ?? throw new ArgumentNullException(nameof(getRelationshipsFunc));
            _getDefaultSaveOptionsFunc = getDefaultSaveOptionsFunc;
            _hasValidationErrorsFunc = hasValidationErrorsFunc;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks for unsaved changes in a block and its children, prompts user for action
        /// </summary>
        public async Task<bool> CheckAndHandleUnsavedChangesAsync(string blockName)
        {
            try
            {
                var dirtyBlocksInfo = await AnalyzeDirtyStateAsync(blockName).ConfigureAwait(false);
                
                // If no dirty blocks, continue
                if (!dirtyBlocksInfo.Any())
                    return true;

                // Raise event to let user decide what to do
                var args = new UnsavedChangesEventArgs(blockName, dirtyBlocksInfo.Select(db => db.BlockName).ToList())
                {
                    DirtyBlockDetails = dirtyBlocksInfo,
                    TotalAffectedRecords = dirtyBlocksInfo.Sum(db => db.DirtyRecordCount),
                    EstimatedSaveTime = EstimateSaveTime(dirtyBlocksInfo)
                };

                OnUnsavedChanges?.Invoke(this, args);

                // Handle user's choice
                switch (args.UserChoice)
                {
                    case UnsavedChangesAction.Save:
                        return await SaveDirtyBlocksAsync(args.DirtyBlocks).ConfigureAwait(false);
                        
                    case UnsavedChangesAction.Discard:
                        return await RollbackDirtyBlocksAsync(args.DirtyBlocks).ConfigureAwait(false);
                        
                    case UnsavedChangesAction.Cancel:
                    default:
                        LogOperation("Operation cancelled due to unsaved changes", blockName);
                        return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error checking unsaved changes for block {blockName}", ex);
                return false;
            }
        }

        /// <summary>
        /// Checks if any blocks have unsaved changes
        /// </summary>
        public bool HasUnsavedChanges()
        {
            return _blocks.Values.Any(block => block.UnitOfWork?.IsDirty == true);
        }

        /// <summary>
        /// Gets all dirty blocks with detailed information
        /// </summary>
        public List<string> GetDirtyBlocks()
        {
            return _blocks.Where(kvp => kvp.Value.UnitOfWork?.IsDirty == true)
                         .Select(kvp => kvp.Key)
                         .ToList();
        }

        /// <summary>
        /// Gets detailed information about dirty blocks
        /// </summary>
        public List<DirtyBlockInfo> GetDirtyBlocksWithDetails()
        {
            return _blocks.Values
                .Where(block => block.UnitOfWork?.IsDirty == true)
                .Select(block => new DirtyBlockInfo
                {
                    BlockName = block.BlockName,
                    EntityName = block.EntityStructure?.EntityName ?? "Unknown",
                    DirtyRecordCount = GetDirtyRecordCount(block),
                    LastModified = GetLastModifiedTime(block),
                    HasErrors = HasValidationErrors(block),
                    IsMasterBlock = block.IsMasterBlock
                })
                .ToList();
        }

        /// <summary>
        /// Collects all dirty detail blocks recursively
        /// </summary>
        public void CollectDirtyDetailBlocks(string blockName, List<string> dirtyBlocks)
        {
            var detailBlocks = _getDetailBlocksFunc(blockName);
            foreach (var detailBlockName in detailBlocks)
            {
                var detailBlockInfo = _getBlockFunc(detailBlockName);
                if (detailBlockInfo?.UnitOfWork?.IsDirty == true && !dirtyBlocks.Contains(detailBlockName))
                {
                    dirtyBlocks.Add(detailBlockName);
                }
                
                // Recursively check detail blocks of this detail block
                CollectDirtyDetailBlocks(detailBlockName, dirtyBlocks);
            }
        }

        /// <summary>
        /// Saves all dirty blocks with progress reporting and error handling
        /// </summary>
        public async Task<bool> SaveDirtyBlocksAsync(List<string> dirtyBlocks)
        {
            // SaveOptions.Default's own properties (ValidateBeforeSave, MaxRetries, ...) are
            // genuinely read below and by SaveBlockWithRetryAsync -- this always used the bare
            // type default, ignoring UnitofWorksManagerConfiguration.DefaultSaveOptions entirely,
            // so a developer who configured Configuration.DefaultSaveOptions (e.g. MaxRetries = 5,
            // or ValidateBeforeSave = false to skip the validation pass below) had that setting
            // silently discarded on every save.
            var saveOptions = _getDefaultSaveOptionsFunc?.Invoke() ?? SaveOptions.Default;
            var results = new List<SaveResult>();
            
            try
            {
                LogOperation($"Starting save operation for {dirtyBlocks.Count} dirty blocks");

                // Validate blocks before saving if required
                if (saveOptions.ValidateBeforeSave)
                {
                    var validationResults = await ValidateBlocksAsync(dirtyBlocks).ConfigureAwait(false);
                    if (validationResults.Any(vr => !vr.IsValid))
                    {
                        LogError("Validation failed for one or more blocks", null);
                        return false;
                    }
                }

                // Sort blocks by dependency order (master blocks first)
                var sortedBlocks = SortBlocksByDependency(dirtyBlocks);
                
                var successCount = 0;
                var totalBlocks = sortedBlocks.Count;

                foreach (var blockName in sortedBlocks)
                {
                    try
                    {
                        var blockInfo = _getBlockFunc(blockName);
                        if (blockInfo?.UnitOfWork != null)
                        {
                            var result = await SaveBlockWithRetryAsync(blockInfo, saveOptions).ConfigureAwait(false);
                            results.Add(result);
                            
                            if (result.Success)
                            {
                                successCount++;
                                LogOperation($"Successfully saved block '{blockName}' ({successCount}/{totalBlocks})");

                                // G0.1/G1.1: After a master block is committed, propagate its
                                // newly-generated key (e.g. auto-increment ID assigned by the DB)
                                // to all dirty detail records before they are committed.
                                if (blockInfo.IsMasterBlock)
                                    PropagateMasterKeyToDetails(blockName, sortedBlocks);
                            }
                            else
                            {
                                LogError($"Failed to save block '{blockName}': {result.ErrorMessage}", result.Exception);
                                
                                if (saveOptions.StopOnFirstError)
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Exception saving block '{blockName}'", ex);
                        
                        if (saveOptions.StopOnFirstError)
                            break;
                    }
                }

                var overallSuccess = results.All(r => r.Success);
                LogOperation($"Save operation completed. Success: {successCount}/{totalBlocks}");
                
                return overallSuccess;
            }
            catch (Exception ex)
            {
                LogError("Error in save operation", ex);
                return false;
            }
        }

        /// <summary>
        /// Rolls back all dirty blocks with error handling
        /// </summary>
        public async Task<bool> RollbackDirtyBlocksAsync(List<string> dirtyBlocks)
        {
            var rollbackOptions = RollbackOptions.Default;
            
            try
            {
                LogOperation($"Starting rollback operation for {dirtyBlocks.Count} dirty blocks");
                
                var successCount = 0;
                var totalBlocks = dirtyBlocks.Count;

                foreach (var blockName in dirtyBlocks)
                {
                    try
                    {
                        var blockInfo = _getBlockFunc(blockName);
                        if (blockInfo?.UnitOfWork != null)
                        {
                            var result = await blockInfo.UnitOfWork.Rollback().ConfigureAwait(false);
                            
                            if (result.Flag == Errors.Ok)
                            {
                                successCount++;
                                LogOperation($"Successfully rolled back block '{blockName}' ({successCount}/{totalBlocks})");
                            }
                            else
                            {
                                LogError($"Failed to rollback block '{blockName}': {result.Message}", result.Ex);
                                
                                if (rollbackOptions.StopOnFirstError)
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Exception rolling back block '{blockName}'", ex);
                        
                        if (rollbackOptions.StopOnFirstError)
                            break;
                    }
                }

                LogOperation($"Rollback operation completed. Success: {successCount}/{totalBlocks}");
                return successCount == totalBlocks;
            }
            catch (Exception ex)
            {
                LogError("Error in rollback operation", ex);
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task<List<DirtyBlockInfo>> AnalyzeDirtyStateAsync(string blockName)
        {
            var dirtyBlocks = new List<string>();
            var dirtyBlocksInfo = new List<DirtyBlockInfo>();
            
            // Check the specified block
            var blockInfo = _getBlockFunc(blockName);
            if (blockInfo?.UnitOfWork?.IsDirty == true)
            {
                dirtyBlocks.Add(blockName);
            }

            // Check all detail blocks recursively
            CollectDirtyDetailBlocks(blockName, dirtyBlocks);

            // Create detailed information for each dirty block
            foreach (var dirtyBlockName in dirtyBlocks)
            {
                var block = _getBlockFunc(dirtyBlockName);
                if (block != null)
                {
                    dirtyBlocksInfo.Add(new DirtyBlockInfo
                    {
                        BlockName = dirtyBlockName,
                        EntityName = block.EntityStructure?.EntityName ?? "Unknown",
                        DirtyRecordCount = GetDirtyRecordCount(block),
                        LastModified = GetLastModifiedTime(block),
                        HasErrors = HasValidationErrors(block),
                        IsMasterBlock = block.IsMasterBlock
                    });
                }
            }

            return dirtyBlocksInfo;
        }

        private async Task<SaveResult> SaveBlockWithRetryAsync(DataBlockInfo blockInfo, SaveOptions options)
        {
            var maxRetries = options.MaxRetries;
            var retryCount = 0;
            
            while (retryCount <= maxRetries)
            {
                try
                {
                    var result = await blockInfo.UnitOfWork.Commit().ConfigureAwait(false);
                    
                    if (result.Flag == Errors.Ok)
                    {
                        return new SaveResult
                        {
                            BlockName = blockInfo.BlockName,
                            Success = true,
                            RetryCount = retryCount
                        };
                    }
                    else
                    {
                        if (retryCount < maxRetries && IsRetryableError(result))
                        {
                            retryCount++;
                            await Task.Delay(options.RetryDelayMs * retryCount); // Exponential backoff
                            continue;
                        }
                        
                        return new SaveResult
                        {
                            BlockName = blockInfo.BlockName,
                            Success = false,
                            ErrorMessage = result.Message,
                            Exception = result.Ex,
                            RetryCount = retryCount
                        };
                    }
                }
                catch (Exception ex)
                {
                    if (retryCount < maxRetries && IsRetryableException(ex))
                    {
                        retryCount++;
                        await Task.Delay(options.RetryDelayMs * retryCount).ConfigureAwait(false);
                        continue;
                    }
                    
                    return new SaveResult
                    {
                        BlockName = blockInfo.BlockName,
                        Success = false,
                        ErrorMessage = ex.Message,
                        Exception = ex,
                        RetryCount = retryCount
                    };
                }
            }
            
            return new SaveResult
            {
                BlockName = blockInfo.BlockName,
                Success = false,
                ErrorMessage = "Max retries exceeded",
                RetryCount = retryCount
            };
        }

        private List<string> SortBlocksByDependency(List<string> blockNames)
        {
            // Sort so that master blocks are saved before detail blocks
            var masterBlocks = new List<string>();
            var detailBlocks = new List<string>();
            
            foreach (var blockName in blockNames)
            {
                var block = _getBlockFunc(blockName);
                if (block?.IsMasterBlock == true)
                    masterBlocks.Add(blockName);
                else
                    detailBlocks.Add(blockName);
            }
            
            masterBlocks.AddRange(detailBlocks);
            return masterBlocks;
        }

        private async Task<List<ValidationResult>> ValidateBlocksAsync(List<string> blockNames)
        {
            var results = new List<ValidationResult>();
            
            foreach (var blockName in blockNames)
            {
                var block = _getBlockFunc(blockName);
                if (block != null)
                {
                    results.Add(new ValidationResult
                    {
                        BlockName = blockName,
                        IsValid = !HasValidationErrors(block),
                        // Add more specific validation logic as needed
                    });
                }
            }
            
            return results;
        }

        private int GetDirtyRecordCount(DataBlockInfo block)
        {
            try
            {
                // IUnitofWork.GetModifiedEntities() already exists and is a real,
                // working read of ObservableBindingList's own tracking state
                // (EntityState.Modified per record) -- this used to hardcode 1
                // whenever the block was dirty at all, so
                // UnsavedChangesEventArgs.TotalAffectedRecords (the number the
                // HandleUnsavedChangesPrompt alert actually shows the user) always
                // read "1 record" regardless of how many records were really dirty.
                // GetModifiedEntities() only covers EntityState.Modified rows, not a
                // block dirtied by a new or deleted record, so floor at 1 whenever
                // IsDirty is true (matching the old behavior's own floor) rather
                // than ever reporting 0 for a block the caller was just told is dirty.
                var uow = block.UnitOfWork;
                if (uow?.IsDirty != true) return 0;
                var modifiedCount = uow.GetModifiedEntities()?.Count() ?? 0;
                return Math.Max(1, modifiedCount);
            }
            catch
            {
                return 0;
            }
        }

        private DateTime? GetLastModifiedTime(DataBlockInfo block)
        {
            try
            {
                // GetChangeLog() already exists and is populated with a real,
                // per-edit Timestamp by RecordChange -- this always returned
                // DateTime.Now regardless of when the block was actually last
                // touched, so a "last modified" display always read "just now."
                var uow = block.UnitOfWork;
                if (uow == null) return null;
                var lastChange = uow.GetChangeLog()?.LastOrDefault();
                return lastChange?.Timestamp;
            }
            catch
            {
                return null;
            }
        }

        private bool HasValidationErrors(DataBlockInfo block)
        {
            try
            {
                // Always returned false regardless of the block's real state (gaps.md
                // G0.53) -- DirtyBlockInfo.HasErrors/IsValid fed straight from here into
                // the HandleUnsavedChangesPrompt alert, so a block with genuinely failing
                // validation still told the user "no errors" when asking Save/Discard/
                // Cancel. _hasValidationErrorsFunc is the live per-item error state
                // ItemPropertyManager already tracks from real validation-rule failures
                // (SetItemError/ClearItemError, wired in FormsManager.Validation.cs) --
                // the same state an on-screen item error indicator reads. No resolver
                // means no known source of truth: report false rather than guess true,
                // since a false positive would block every save/discard/cancel decision
                // for a form that never wired one.
                return _hasValidationErrorsFunc?.Invoke(block.BlockName) ?? false;
            }
            catch
            {
                return true;
            }
        }

        private TimeSpan EstimateSaveTime(List<DirtyBlockInfo> dirtyBlocks)
        {
            // Simple estimation based on number of records and blocks
            var totalRecords = dirtyBlocks.Sum(db => db.DirtyRecordCount);
            var estimatedSeconds = Math.Max(1, totalRecords * 0.1); // 100ms per record
            return TimeSpan.FromSeconds(estimatedSeconds);
        }

        private bool IsRetryableError(IErrorsInfo result)
        {
            // Define what errors are retryable (e.g., timeout, connection issues)
            return result.Message?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true ||
                   result.Message?.Contains("connection", StringComparison.OrdinalIgnoreCase) == true;
        }

        private bool IsRetryableException(Exception ex)
        {
            // Define what exceptions are retryable
            return ex is TimeoutException ||
                   ex.Message?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true;
        }

        private void LogOperation(string message, string blockName = null)
        {
            var fullMessage = blockName != null ? $"[{blockName}] {message}" : message;
            _dmeEditor.AddLogMessage("DirtyStateManager", fullMessage, DateTime.Now, 0, null, Errors.Ok);
        }

        private void LogError(string message, Exception ex = null, string blockName = null)
        {
            var fullMessage = blockName != null ? $"[{blockName}] {message}" : message;
            _dmeEditor.AddLogMessage("DirtyStateManager", fullMessage, DateTime.Now, -1, null, Errors.Failed);
        }

        /// <summary>
        /// After a master block is committed, the database may have generated the real key
        /// (e.g. auto-increment identity). This method propagates the master's current key
        /// value to all dirty detail records so they carry the correct FK when committed.
        /// Matches the Oracle Forms COPY / WHEN-VALIDATE-RECORD key-propagation contract.
        ///
        /// CONTRACT: UoW implementations MUST update the record's key property after Commit()
        /// for auto-generated keys. ADO.NET sources do this via SCOPE_IDENTITY() / RETURNING.
        /// For NoSQL/File/WebAPI sources, the key is typically generated before commit
        /// (client-side ObjectId, counter, or API-returned ID). If the key is still null/empty
        /// after commit, a warning is logged and propagation is skipped — the caller should
        /// re-query the data source or use a PostCommitRefresh hook.
        /// </summary>
        private void PropagateMasterKeyToDetails(string masterBlockName, List<string> dirtyBlockNames)
        {
            var relationships = _getRelationshipsFunc(masterBlockName);
            if (relationships == null || !relationships.Any())
                return;

            var masterBlock = _getBlockFunc(masterBlockName);
            var masterCurrentItem = masterBlock?.UnitOfWork?.CurrentItem;
            if (masterCurrentItem == null) return;

            foreach (var relationship in relationships.Where(r => r.IsActive))
            {
                if (!dirtyBlockNames.Contains(relationship.DetailBlockName))
                    continue;

                var detailBlock = _getBlockFunc(relationship.DetailBlockName);
                if (detailBlock?.UnitOfWork == null) continue;

                var masterFieldMappings = MasterDetailKeyResolver.TryParseMappings(
                    relationship.MasterKeyField, relationship.DetailForeignKeyField,
                    out var mappings, out _);

                if (mappings == null || mappings.Count == 0) continue;

                foreach (var mapping in mappings)
                {
                    var masterKeyValue = RecordPropertyAccessor.GetValue(
                        masterCurrentItem, mapping.MasterField, _dmeEditor);

                    // Safety-net: if the key is still null/empty after commit,
                    // the UoW implementation did not populate the auto-generated
                    // value (common in non-ADO NoSQL/File/WebAPI sources). Log
                    // a warning and skip — do not propagate null/empty FK values.
                    if (IsNullOrEmpty(masterKeyValue))
                    {
                        LogError(
                            $"PropagateMasterKeyToDetails: master key '{relationship.MasterKeyField}' " +
                            $"on block '{masterBlockName}' is null/empty after commit. " +
                            $"The UoW implementation may not have captured the generated key. " +
                            $"Key propagation to detail '{relationship.DetailBlockName}' was skipped. " +
                            $"UoW implementations MUST update the record's key property after Commit() " +
                            $"for sources that generate keys (ADO.NET SCOPE_IDENTITY, WebAPI POST response, etc.).",
                            null, relationship.DetailBlockName);
                        continue;
                    }

                    // Propagate the master key to ALL dirtied detail records — both
                    // newly inserted (GetInsertedItems) and existing-but-modified
                    // (GetUpdatedItems). This covers: DB-generated identity after
                    // master insert, AND manual key changes on the master that must
                    // cascade to existing child records. Oracle Forms COPY contract.
                    System.Collections.IList? inserteds = null;
                    System.Collections.IList? updateds = null;
                    try
                    {
                        dynamic dynUoW = detailBlock.UnitOfWork;
                        inserteds = ((System.Collections.IList?)dynUoW.GetInsertedItems() ?? null);
                        updateds  = ((System.Collections.IList?)dynUoW.GetUpdatedItems() ?? null);
                    }
                    catch { /* Optional — skip if UoW doesn't expose these */ }

                    if (inserteds != null)
                    {
                        foreach (var detailRecord in inserteds)
                        {
                            RecordPropertyAccessor.TrySetValue(
                                detailRecord, mapping.DetailField, masterKeyValue, _dmeEditor);
                        }
                    }

                    if (updateds != null)
                    {
                        foreach (var detailRecord in updateds)
                        {
                            RecordPropertyAccessor.TrySetValue(
                                detailRecord, mapping.DetailField, masterKeyValue, _dmeEditor);
                        }
                    }

                    // Also update the current item
                    var currentDetail = detailBlock.UnitOfWork.CurrentItem;
                    if (currentDetail != null)
                    {
                        RecordPropertyAccessor.TrySetValue(
                            currentDetail, mapping.DetailField, masterKeyValue, _dmeEditor);
                    }
                }

                LogOperation(
                    $"Propagated master key from '{masterBlockName}.{relationship.MasterKeyField}' " +
                    $"to detail '{relationship.DetailBlockName}.{relationship.DetailForeignKeyField}'");
            }
        }

        #endregion
    }
}