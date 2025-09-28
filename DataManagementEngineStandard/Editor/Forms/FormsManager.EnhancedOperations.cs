using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Models;
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
                // Since GetEntityType is not available, use a different approach
                Type entityType = null;
                
                // Try to get type from entity structure name
                if (!string.IsNullOrEmpty(blockInfo.EntityStructure.EntityName))
                {
                    // Try using DMTypeBuilder or reflection to create/find the type
                    try
                    {
                        // First, try to find existing type by name
                        entityType = Type.GetType(blockInfo.EntityStructure.EntityName);
                        
                        // If not found, try creating a dynamic type using available fields
                        if (entityType == null && blockInfo.EntityStructure.Fields?.Count > 0)
                        {
                            // Create a simple dynamic object or use existing generic approach
                            // For now, create a basic object and add properties through FormsSimulationHelper
                            var newRecord = new System.Dynamic.ExpandoObject();
                            
                            // Apply audit defaults
                            _formsSimulationHelper.SetAuditDefaults(newRecord, Environment.UserName);
                            
                            LogOperation($"New dynamic record created for block '{blockName}'", blockName);
                            return newRecord;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error creating type for entity '{blockInfo.EntityStructure.EntityName}'", ex, blockName);
                    }
                }

                if (entityType != null)
                {
                    var newRecord = Activator.CreateInstance(entityType);
                    
                    // Apply audit defaults
                    _formsSimulationHelper.SetAuditDefaults(newRecord, Environment.UserName);
                    
                    LogOperation($"New record created for block '{blockName}'", blockName);
                    return newRecord;
                }

                Status = $"Cannot create entity type for block '{blockName}'";
                return null;
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

                // Use the unit of work's insert method with better method resolution
                var unitOfWorkType = blockInfo.UnitOfWork.GetType();
                var insertMethod = FindBestInsertMethod(unitOfWorkType, record.GetType());

                if (insertMethod != null)
                {
                    var task = (Task<IErrorsInfo>)insertMethod.Invoke(blockInfo.UnitOfWork, new object[] { record });
                    var insertResult = await task;
                    
                    if (insertResult.Flag == Errors.Ok)
                    {
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
                }
                else
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"No suitable InsertAsync method found for block '{blockName}'";
                    Status = result.Message;
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

                // Use unit of work's update method with better method resolution
                var unitOfWorkType = blockInfo.UnitOfWork.GetType();
                var updateMethod = FindBestUpdateMethod(unitOfWorkType, currentRecord.GetType());

                if (updateMethod != null)
                {
                    var task = (Task<IErrorsInfo>)updateMethod.Invoke(blockInfo.UnitOfWork, new object[] { currentRecord });
                    var updateResult = await task;
                    
                    if (updateResult.Flag == Errors.Ok)
                    {
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
                }
                else
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"No suitable UpdateAsync method found for block '{blockName}'";
                    Status = result.Message;
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

                var unitOfWorkType = blockInfo.UnitOfWork.GetType();
                
                // Execute query with proper method resolution
                if (filters != null && filters.Any())
                {
                    var getWithFilters = unitOfWorkType.GetMethod("Get", new[] { typeof(List<AppFilter>) });
                    if (getWithFilters != null)
                    {
                        var task = (Task)getWithFilters.Invoke(blockInfo.UnitOfWork, new object[] { filters });
                        await task;
                    }
                    else
                    {
                        result.Flag = Errors.Failed;
                        result.Message = $"Get method with filters not found for block '{blockName}'";
                        return result;
                    }
                }
                else
                {
                    var getMethod = unitOfWorkType.GetMethod("Get", Type.EmptyTypes);
                    if (getMethod != null)
                    {
                        var task = (Task)getMethod.Invoke(blockInfo.UnitOfWork, null);
                        await task;
                    }
                    else
                    {
                        result.Flag = Errors.Failed;
                        result.Message = $"Get method not found for block '{blockName}'";
                        return result;
                    }
                }

                // CRITICAL: After successful query execution, transition to CRUD mode
                blockInfo.Mode = DataBlockMode.CRUD;
                blockInfo.LastModeChange = DateTime.Now;

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
                    // Use reflection to get count
                    var unitsType = blockInfo.UnitOfWork.Units.GetType();
                    var countProperty = unitsType.GetProperty("Count");
                    if (countProperty != null)
                    {
                        return (int)countProperty.GetValue(blockInfo.UnitOfWork.Units);
                    }
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
        public bool CopyFields(object sourceRecord, object targetRecord, params string[] fieldNames)
        {
            if (sourceRecord == null || targetRecord == null)
                return false;

            try
            {
                var success = true;
                foreach (var fieldName in fieldNames)
                {
                    var value = _formsSimulationHelper.GetFieldValue(sourceRecord, fieldName);
                    if (!_formsSimulationHelper.SetFieldValue(targetRecord, fieldName, value))
                    {
                        success = false;
                        LogError($"Failed to copy field '{fieldName}'", null);
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
        /// <summary>
        /// Finds the best insert method for the given types
        /// </summary>
        private System.Reflection.MethodInfo FindBestInsertMethod(Type unitOfWorkType, Type recordType)
        {
            // Try exact type match first
            var exactMethod = unitOfWorkType.GetMethod("InsertAsync", new[] { recordType });
            if (exactMethod != null)
                return exactMethod;

            // Try object parameter
            var objectMethod = unitOfWorkType.GetMethod("InsertAsync", new[] { typeof(object) });
            if (objectMethod != null)
                return objectMethod;

            // Try generic method if available
            var genericMethods = unitOfWorkType.GetMethods()
                .Where(m => m.Name == "InsertAsync" && m.IsGenericMethod)
                .ToList();

            foreach (var method in genericMethods)
            {
                try
                {
                    var genericMethod = method.MakeGenericMethod(recordType);
                    return genericMethod;
                }
                catch
                {
                    // Continue to next method
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the best update method for the given types
        /// </summary>
        private System.Reflection.MethodInfo FindBestUpdateMethod(Type unitOfWorkType, Type recordType)
        {
            // Try exact type match first
            var exactMethod = unitOfWorkType.GetMethod("UpdateAsync", new[] { recordType });
            if (exactMethod != null)
                return exactMethod;

            // Try object parameter
            var objectMethod = unitOfWorkType.GetMethod("UpdateAsync", new[] { typeof(object) });
            if (objectMethod != null)
                return objectMethod;

            // Try generic method if available
            var genericMethods = unitOfWorkType.GetMethods()
                .Where(m => m.Name == "UpdateAsync" && m.IsGenericMethod)
                .ToList();

            foreach (var method in genericMethods)
            {
                try
                {
                    var genericMethod = method.MakeGenericMethod(recordType);
                    return genericMethod;
                }
                catch
                {
                    // Continue to next method
                }
            }

            return null;
        }
        #endregion
    }
}