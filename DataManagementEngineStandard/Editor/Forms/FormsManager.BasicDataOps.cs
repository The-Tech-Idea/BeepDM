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
        #region Data Operations (Required by Interface - Basic Implementation)

        /// <summary>
        /// Inserts a new record in the specified block
        /// Basic implementation - use InsertRecordEnhancedAsync for better functionality
        /// </summary>
        public async Task<bool> InsertRecordAsync(string blockName, object record = null)
        {
            try
            {
                var result = await InsertRecordEnhancedAsync(blockName, record);
                if (result.Flag == Errors.Ok)
                {
                    Status = $"Record inserted successfully in block '{blockName}'";
                    _messageManager?.ShowSuccessMessage(blockName, Status);
                }
                else
                {
                    Status = $"Error inserting record: {result.Message}";
                    _messageManager?.ShowErrorMessage(blockName, Status);
                }
                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Status = $"Error inserting record in block '{blockName}': {ex.Message}";
                LogError($"Error inserting record in block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Deletes the current record in the specified block
        /// </summary>
        public async Task<bool> DeleteCurrentRecordAsync(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    Status = $"Block '{blockName}' not found or has no unit of work";
                    return false;
                }

                // CRUD flag guard (Phase 2)
                if (!blockInfo.DeleteAllowed)
                {
                    Status = $"Delete not allowed for block '{blockName}'";
                    _messageManager?.ShowErrorMessage(blockName, Status);
                    return false;
                }

                // Phase 6: security check
                if (!EnforceBlockSecurity(blockName, SecurityPermission.Delete))
                {
                    Status = $"Security: delete not permitted on block '{blockName}'";
                    return false;
                }

                // Check for unsaved changes in detail blocks
                var detailBlocks = GetDetailBlocks(blockName);
                foreach (var detailBlockName in detailBlocks)
                {
                    if (!await CheckAndHandleUnsavedChangesAsync(detailBlockName))
                        return false;
                }

                // Get current record
                object currentRecord = blockInfo.UnitOfWork.CurrentItem;
                if (currentRecord == null)
                {
                    Status = $"No current record to delete in block '{blockName}'";
                    return false;
                }

                // Auto-lock if needed (Phase 7)
                await _lockManager.AutoLockIfNeededAsync(blockName);

                // Fire WHEN-REMOVE-RECORD trigger (before the record is removed)
                var whenRemoveCtx = TriggerContext.ForBlock(TriggerType.WhenRemoveRecord, blockName, currentRecord, _dmeEditor);
                var whenRemoveResult = await _triggerManager.FireBlockTriggerAsync(TriggerType.WhenRemoveRecord, blockName, whenRemoveCtx);
                if (whenRemoveResult == TriggerResult.Cancelled)
                {
                    Status = $"Delete cancelled by WHEN-REMOVE-RECORD trigger in block '{blockName}'";
                    _messageManager?.ShowWarningMessage(blockName, Status);
                    return false;
                }

                // Fire PRE-DELETE trigger
                var preDeleteCtx = TriggerContext.ForBlock(TriggerType.PreDelete, blockName, currentRecord, _dmeEditor);
                var preDeleteResult = await _triggerManager.FireBlockTriggerAsync(TriggerType.PreDelete, blockName, preDeleteCtx);
                if (preDeleteResult == TriggerResult.Cancelled)
                {
                    Status = $"Delete cancelled by PRE-DELETE trigger in block '{blockName}'";
                    _messageManager?.ShowWarningMessage(blockName, Status);
                    return false;
                }

                // Delete the current record using reflection
                var deleteMethod = blockInfo.UnitOfWork.GetType().GetMethod("DeleteAsync");
                if (deleteMethod != null)
                {
                    SuppressSync(blockName);
                    IErrorsInfo result;
                    try
                    {
                        var task = (Task<IErrorsInfo>)deleteMethod.Invoke(blockInfo.UnitOfWork, new object[] { currentRecord });
                        result = await task;
                    }
                    finally { ResumeSync(blockName); }

                    if (result.Flag == Errors.Ok)
                    {
                        Status = $"Record deleted successfully in block '{blockName}'";
                        _messageManager?.ShowWarningMessage(blockName, Status);

                        // Fire POST-DELETE trigger after successful delete
                        await _triggerManager.FireBlockTriggerAsync(
                            TriggerType.PostDelete, blockName,
                            TriggerContext.ForBlock(TriggerType.PostDelete, blockName, currentRecord, _dmeEditor));

                        await SynchronizeDetailBlocksAsync(blockName);
                        return true;
                    }
                    else
                    {
                        Status = $"Error deleting record: {result.Message}";
                        _messageManager?.ShowErrorMessage(blockName, Status);
                        return false;
                    }
                }

                Status = $"DeleteAsync method not found on unit of work for block '{blockName}'";
                return false;
            }
            catch (Exception ex)
            {
                Status = $"Error deleting record in block '{blockName}': {ex.Message}";
                LogError($"Error deleting record in block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Enters query mode for a block - equivalent to Oracle Forms ENTER_QUERY
        /// </summary>
        public async Task<bool> EnterQueryAsync(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo == null)
                    return false;

                blockInfo.Mode = DataBlockMode.Query;
                _currentBlockName = blockName;
                Status = $"Block '{blockName}' entered query mode";
                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error entering query mode for '{blockName}': {ex.Message}";
                LogError($"Error entering query mode for '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Executes query for a block - equivalent to Oracle Forms EXECUTE_QUERY.
        /// Merges block-level default WHERE clause with caller-supplied filters via QueryBuilder.
        /// </summary>
        public async Task<bool> ExecuteQueryAsync(string blockName, List<AppFilter> filters = null)
        {
            try
            {
                var block = GetBlock(blockName);
                if (block != null && !block.QueryAllowed)
                {
                    Status = $"Query not allowed for block '{blockName}'";
                    return false;
                }

                // Phase 6: security check
                if (!EnforceBlockSecurity(blockName, SecurityPermission.Query))
                {
                    Status = $"Security: query not permitted on block '{blockName}'";
                    return false;
                }

                // Merge default WHERE clause from block metadata
                var finalFilters = filters;
                if (block != null && !string.IsNullOrWhiteSpace(block.DefaultWhereClause))
                {
                    var defaultFilters = _queryBuilderManager.ParseWhereClause(block.DefaultWhereClause);
                    finalFilters = _queryBuilderManager.CombineFiltersAnd(
                        finalFilters ?? new List<AppFilter>(), defaultFilters);
                }

                var result = await ExecuteQueryEnhancedAsync(blockName, finalFilters);
                if (result.Flag == Errors.Ok)
                {
                    Status = $"Query executed successfully for block '{blockName}'";
                    _messageManager?.ShowInfoMessage(blockName, Status);
                }
                else
                {
                    bool warningOutcome = IsQueryWarningOutcome(result);
                    Status = string.IsNullOrWhiteSpace(result.Message)
                        ? (warningOutcome
                            ? $"Query execution stopped for block '{blockName}'"
                            : $"Error executing query for block '{blockName}'")
                        : result.Message;

                    if (warningOutcome)
                    {
                        _messageManager?.ShowWarningMessage(blockName, Status);
                    }
                    else
                    {
                        _messageManager?.ShowErrorMessage(blockName, Status);
                    }
                }
                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Status = $"Error executing query for '{blockName}': {ex.Message}";
                LogError($"Error executing query for '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        private static bool IsQueryWarningOutcome(IErrorsInfo? result)
        {
            if (result == null)
            {
                return false;
            }

            if (result.Flag == Errors.Warning || result.Flag == Errors.Information)
            {
                return true;
            }

            string message = result.Message ?? string.Empty;
            return message.IndexOf("cancelled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("canceled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("validation failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("must be in Query mode", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("already in Query mode", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion
    }
}
