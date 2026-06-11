using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Enhanced operations partial class for UnitofWorksManager
    /// Provides enhanced operations and error recovery with mode transition awareness
    /// </summary>
    public partial class FormsManager
    {
      
        #region Enhanced Data Operations
        /// <summary>
        /// Creates a new record for a block using proper type resolution
        /// Automatically handles mode transition if needed
        /// </summary>
        public object CreateNewRecord(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.EntityStructure == null)
                {
                    Status = $"Block '{blockName}' not found or has no entity structure";
                    return null;
                }

                // CRITICAL: Ensure block is in appropriate mode for new record creation
                if (blockInfo.Mode == DataBlockMode.Query)
                {
                    LogOperation($"Block '{blockName}' is in Query mode, transitioning to CRUD mode for new record", blockName);
                    // Transition to CRUD mode asynchronously - but since this method is sync, 
                    // we'll log and continue with a warning
                    Status = $"Warning: Block '{blockName}' should be in CRUD mode for new record creation";
                }

                // Use class creator to create new instance based on entity structure
                var entityType = blockInfo.EntityType ?? ResolveBlockEntityType(blockInfo.UnitOfWork, blockInfo.EntityStructure, blockInfo.DataSourceName);
                if (entityType != null && blockInfo.EntityType == null)
                    blockInfo.EntityType = entityType;

                if (entityType == null)
                {
                    Status = $"Cannot create new record for block '{blockName}' because no CLR entity type is registered or discoverable";
                    LogOperation(Status, blockName);
                    return null;
                }

                if (entityType.IsAbstract || entityType.IsInterface)
                {
                    Status = $"Cannot create new record for block '{blockName}' because CLR entity type '{entityType.FullName}' is not instantiable";
                    LogOperation(Status, blockName);
                    return null;
                }

                var newRecord = Activator.CreateInstance(entityType);
                _formsSimulationHelper.SetAuditDefaults(newRecord, Environment.UserName);

                // Fire WHEN-CREATE-RECORD trigger after the instance is created
                _triggerManager.FireBlockTrigger(
                    TriggerType.WhenCreateRecord, blockName,
                    TriggerContext.ForBlock(TriggerType.WhenCreateRecord, blockName, newRecord, _dmeEditor));

                LogOperation($"New record created for block '{blockName}' using '{entityType.FullName}'", blockName);
                return newRecord;
            }
            catch (Exception ex)
            {
                Status = $"Error creating new record for block '{blockName}': {ex.Message}";
                LogError($"Error creating new record for block '{blockName}'", ex, blockName);
                return null;
            }
        }

        /// <summary>
        /// Enhanced insert operation with mode transition validation and better type safety
        /// </summary>
        public async Task<IErrorsInfo> InsertRecordEnhancedAsync(string blockName, object record = null)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' not found or has no unit of work";
                    return result;
                }

                // CRUD flag guard (Phase 2)
                if (!blockInfo.InsertAllowed)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Insert not allowed for block '{blockName}'";
                    _messageManager?.ShowErrorMessage(blockName, result.Message);
                    return result;
                }

                // Phase 6: security check
                if (!EnforceBlockSecurity(blockName, SecurityPermission.Insert))
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Security: insert not permitted on block '{blockName}'";
                    return result;
                }

                // CRITICAL: Validate mode transition before insert
                if (blockInfo.Mode != DataBlockMode.CRUD)
                {
                    LogOperation($"Block '{blockName}' not in CRUD mode, attempting transition", blockName);
                    
                    var modeTransitionResult = await EnterCrudModeForNewRecordAsync(blockName);
                    if (modeTransitionResult.Flag != Errors.Ok)
                    {
                        result.Flag = modeTransitionResult.Flag;
                        result.Message = $"Cannot insert: Mode transition failed - {modeTransitionResult.Message}";
                        return result;
                    }
                }

                // Check for unsaved changes in current and related blocks
                if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Insert cancelled due to unsaved changes";
                    return result;
                }

                // Create new record if not provided
                if (record == null)
                {
                    record = CreateNewRecord(blockName);
                    if (record == null)
                    {
                        result.Flag = Errors.Failed;
                        result.Message = $"Cannot create new record for block '{blockName}'";
                        return result;
                    }
                }

                // Validate record before insert
                if (!ValidateRecordForOperation(blockName, record, "INSERT"))
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Record validation failed for insert in block '{blockName}'";
                    return result;
                }

                // Fire PRE-INSERT trigger — abort if cancelled
                var preInsertResult = await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.PreInsert, blockName,
                    TriggerContext.ForBlock(TriggerType.PreInsert, blockName, record, _dmeEditor));
                if (preInsertResult == TriggerResult.Cancelled)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Insert cancelled by PRE-INSERT trigger in block '{blockName}'";
                    return result;
                }

                // IUnitofWork (non-generic) declares InsertAsync(dynamic doc) directly.
                // The previous FindBestInsertMethod(...) reflection was a silent-no-op
                // trap: if the typed overload didn't exist, the engine silently no-op'd
                // the insert. Direct dispatch either compiles or fails loud.
                SuppressSync(blockName);
                IErrorsInfo insertResult;
                try
                {
                    insertResult = await blockInfo.UnitOfWork.InsertAsync(record);
                }
                finally { ResumeSync(blockName); }

                if (insertResult == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"InsertAsync returned null on unit of work for block '{blockName}'";
                    return result;
                }

                if (insertResult.Flag == Errors.Ok)
                {
                    // Fire POST-INSERT trigger after successful insert
                    await _triggerManager.FireBlockTriggerAsync(
                        TriggerType.PostInsert, blockName,
                        TriggerContext.ForBlock(TriggerType.PostInsert, blockName, record, _dmeEditor));

                    await SynchronizeDetailBlocksAsync(blockName);
                    result.Message = "Record inserted successfully";
                    Status = $"Record inserted successfully in block '{blockName}'";
                    LogOperation($"Record inserted successfully in block '{blockName}'", blockName);
                }
                else
                {
                    result.Flag = insertResult.Flag;
                    result.Message = insertResult.Message;
                    result.Ex = insertResult.Ex;
                    Status = $"Error inserting record: {insertResult.Message}";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error inserting record in block '{blockName}': {ex.Message}";
                LogError($"Error inserting record in block '{blockName}'", ex, blockName);
                return result;
            }
        }

        /// <summary>
        /// Enhanced update operation with mode validation for current record
        /// </summary>
        public async Task<IErrorsInfo> UpdateCurrentRecordAsync(string blockName)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' not found or has no unit of work";
                    return result;
                }

                // CRUD flag guard (Phase 2)
                if (!blockInfo.UpdateAllowed)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Update not allowed for block '{blockName}'";
                    _messageManager?.ShowErrorMessage(blockName, result.Message);
                    return result;
                }

                // Phase 6: security check
                if (!EnforceBlockSecurity(blockName, SecurityPermission.Update))
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Security: update not permitted on block '{blockName}'";
                    return result;
                }

                // CRITICAL: Must be in CRUD mode to update
                if (blockInfo.Mode != DataBlockMode.CRUD)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' must be in CRUD mode to update records. Current mode: {blockInfo.Mode}";
                    return result;
                }

                var currentRecord = blockInfo.UnitOfWork.CurrentItem;
                if (currentRecord == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"No current record to update in block '{blockName}'";
                    return result;
                }

                // Auto-lock if needed (Phase 7)
                await _lockManager.AutoLockIfNeededAsync(blockName);

                // Validate record before update
                if (!ValidateRecordForOperation(blockName, currentRecord, "UPDATE"))
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Record validation failed for update in block '{blockName}'";
                    return result;
                }

                // Update audit fields through FormsSimulationHelper
                _formsSimulationHelper.SetFieldValue(currentRecord, "ModifiedDate", DateTime.Now);
                _formsSimulationHelper.SetFieldValue(currentRecord, "ModifiedBy", Environment.UserName);

                // Fire PRE-UPDATE trigger — abort if cancelled
                var preUpdateResult = await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.PreUpdate, blockName,
                    TriggerContext.ForBlock(TriggerType.PreUpdate, blockName, currentRecord, _dmeEditor));
                if (preUpdateResult == TriggerResult.Cancelled)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Update cancelled by PRE-UPDATE trigger in block '{blockName}'";
                    return result;
                }

                // IUnitofWork (non-generic) declares UpdateAsync(dynamic doc) directly.
                // Direct dispatch — same rationale as the InsertAsync change above.
                var updateResult = await blockInfo.UnitOfWork.UpdateAsync(currentRecord);

                if (updateResult == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"UpdateAsync returned null on unit of work for block '{blockName}'";
                    return result;
                }

                if (updateResult.Flag == Errors.Ok)
                {
                    // Fire POST-UPDATE trigger after successful update
                    await _triggerManager.FireBlockTriggerAsync(
                        TriggerType.PostUpdate, blockName,
                        TriggerContext.ForBlock(TriggerType.PostUpdate, blockName, currentRecord, _dmeEditor));

                    await SynchronizeDetailBlocksAsync(blockName);
                    result.Message = "Record updated successfully";
                    Status = $"Record updated successfully in block '{blockName}'";
                    LogOperation($"Record updated successfully in block '{blockName}'", blockName);
                }
                else
                {
                    result.Flag = updateResult.Flag;
                    result.Message = updateResult.Message;
                    result.Ex = updateResult.Ex;
                    Status = $"Error updating record: {updateResult.Message}";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error updating record in block '{blockName}': {ex.Message}";
                LogError($"Error updating record in block '{blockName}'", ex, blockName);
                return result;
            }
        }

        /// <summary>
        /// Enhanced query execution with proper mode transition handling
        /// This method now properly handles Query->Execute->CRUD mode transition
        /// </summary>
        public async Task<IErrorsInfo> ExecuteQueryEnhancedAsync(string blockName, List<AppFilter> filters = null)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' not found or has no unit of work";
                    return result;
                }

                // CRITICAL: This method should handle the Query->CRUD transition
                // If not in Query mode, first transition to Query mode
                if (blockInfo.Mode != DataBlockMode.Query)
                {
                    LogOperation($"Block '{blockName}' not in Query mode, entering Query mode first", blockName);
                    
                    var queryModeResult = await EnterQueryModeAsync(blockName);
                    if (queryModeResult.Flag != Errors.Ok)
                    {
                        result.Flag = queryModeResult.Flag;
                        result.Message = $"Cannot execute query: Failed to enter Query mode - {queryModeResult.Message}";
                        return result;
                    }
                }

                // Fire PRE-QUERY trigger — abort if cancelled
                var preQueryResult = await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.PreQuery, blockName,
                    TriggerContext.ForBlock(TriggerType.PreQuery, blockName, null, _dmeEditor));
                if (preQueryResult == TriggerResult.Cancelled)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Query cancelled by PRE-QUERY trigger in block '{blockName}'";
                    return result;
                }

                // IUnitofWork (non-generic) declares Get(List<AppFilter>) and Get() directly.
                // Direct dynamic dispatch — the previous GetMethod("Get").Invoke(...) was
                // a silent-no-op trap if the method didn't exist.
                if (filters != null && filters.Any())
                {
                    await blockInfo.UnitOfWork.Get(filters);
                }
                else
                {
                    await blockInfo.UnitOfWork.Get();
                }

                // CRITICAL: After successful query execution, transition to CRUD mode
                blockInfo.Mode = DataBlockMode.CRUD;
                blockInfo.LastModeChange = DateTime.Now;

                // Fire POST-QUERY trigger (before returning to caller)
                await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.PostQuery, blockName,
                    TriggerContext.ForBlock(TriggerType.PostQuery, blockName, null, _dmeEditor));

                var recordCount = GetRecordCount(blockName);
                
                result.Message = $"Query executed successfully. {recordCount} records found.";
                Status = $"Query executed successfully for block '{blockName}'. {recordCount} records.";
                LogOperation($"Query executed successfully for block '{blockName}' with {recordCount} records", blockName);

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error executing query for '{blockName}': {ex.Message}";
                LogError($"Error executing query for '{blockName}'", ex, blockName);
                return result;
            }
        }

        /// <summary>
        /// Gets the current record for a block
        /// </summary>
        public object GetCurrentRecord(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                return blockInfo?.UnitOfWork?.CurrentItem;
            }
            catch (Exception ex)
            {
                LogError($"Error getting current record for block '{blockName}'", ex, blockName);
                return null;
            }
        }

        /// <summary>
        /// Gets the record count for a block
        /// </summary>
        public int GetRecordCount(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork?.Units != null)
                {
                    // Units is `dynamic`. The runtime type is ObservableBindingList<T>
                    // which extends BindingList<T> : Collection<T> with a Count property.
                    // The previous GetProperty("Count") reflection was a silent-no-op
                    // trap on a custom Units implementation; dynamic dispatch is louder.
                    dynamic d = blockInfo.UnitOfWork.Units;
                    int? count = d.Count as int?;
                    return count ?? 0;
                }
                return 0;
            }
            catch (Exception ex)
            {
                LogError($"Error getting record count for block '{blockName}'", ex, blockName);
                return 0;
            }
        }
        #endregion

        #region Enhanced Validation Operations

        /// <summary>
        /// Validates a record for a specific operation with mode awareness
        /// </summary>
        private bool ValidateRecordForOperation(string blockName, object record, string operation)
        {
            try
            {
                if (record == null)
                {
                    LogError($"Cannot perform {operation}: Record is null", null, blockName);
                    return false;
                }

                var blockInfo = GetBlock(blockName);
                if (blockInfo == null)
                {
                    LogError($"Cannot perform {operation}: Block info not found", null, blockName);
                    return false;
                }

                // Check mode compatibility
                if (operation == "INSERT" || operation == "UPDATE" || operation == "DELETE")
                {
                    if (blockInfo.Mode != DataBlockMode.CRUD)
                    {
                        LogError($"Cannot perform {operation}: Block must be in CRUD mode", null, blockName);
                        return false;
                    }
                }

                // Use existing validation logic
                var isValid = ValidateBlock(blockName);
                
                if (!isValid)
                {
                    LogError($"Record validation failed for {operation} operation", null, blockName);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                LogError($"Error validating record for {operation} operation", ex, blockName);
                return false;
            }
        }

        #endregion

        #region Field Operations (Using FormsSimulationHelper)
        /// <summary>
        /// Copies field values between records
        /// </summary>
        public bool CopyFields(object sourceRecord, object targetRecord, params string[] FieldNames)
        {
            if (sourceRecord == null || targetRecord == null)
                return false;

            try
            {
                var success = true;
                foreach (var FieldName in FieldNames)
                {
                    var value = _formsSimulationHelper.GetFieldValue(sourceRecord, FieldName);
                    if (!_formsSimulationHelper.SetFieldValue(targetRecord, FieldName, value))
                    {
                        success = false;
                        LogError($"Failed to copy field '{FieldName}'", null);
                    }
                }
                return success;
            }
            catch (Exception ex)
            {
                LogError("Error copying fields between records", ex);
                return false;
            }
        }

        /// <summary>
        /// Applies audit defaults to a record
        /// </summary>
        public void ApplyAuditDefaults(object record, string currentUser = null)
        {
            _formsSimulationHelper.SetAuditDefaults(record, currentUser ?? Environment.UserName);
        }
        #endregion

        #region Private Helper Methods for Enhanced Operations
        // FindBestInsertMethod and FindBestUpdateMethod were removed: the engine now
        // calls IUnitofWork.InsertAsync / UpdateAsync directly via dynamic dispatch.
        // The reflection-based method-resolution helpers were a silent-no-op trap
        // (returning null when the typed overload didn't exist) and have been
        // superseded by the direct call.

        #endregion
    }
}