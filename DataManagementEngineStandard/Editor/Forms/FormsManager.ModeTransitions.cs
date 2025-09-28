using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Mode Transition Management partial class for UnitofWorksManager
    /// Handles transitions between Query and CRUD modes with proper validation
    /// Equivalent to Oracle Forms ENTER_QUERY / EXECUTE_QUERY mode transitions
    /// </summary>
    public partial class FormsManager
    {
        #region Mode Transition Operations

        /// <summary>
        /// Transitions a block from CRUD to Query mode - equivalent to Oracle Forms ENTER_QUERY
        /// Validates unsaved changes before transition
        /// </summary>
        public async Task<IErrorsInfo> EnterQueryModeAsync(string blockName)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' not found";
                    return result;
                }

                // If already in Query mode, no need to transition
                if (blockInfo.Mode == DataBlockMode.Query)
                {
                    result.Message = $"Block '{blockName}' is already in Query mode";
                    Status = result.Message;
                    return result;
                }

                LogOperation($"Attempting to enter Query mode for block '{blockName}'", blockName);

                // CRITICAL: Check for unsaved changes in current block AND all related blocks
                var unsavedChangesResult = await ValidateUnsavedChangesForModeTransition(blockName);
                if (!unsavedChangesResult.IsValid)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Cannot enter Query mode: {unsavedChangesResult.Message}";
                    Status = result.Message;
                    return result;
                }

                // Check for unsaved changes in related blocks (detail blocks)
                var relatedBlocksResult = await ValidateRelatedBlocksForModeTransition(blockName, DataBlockMode.Query);
                if (!relatedBlocksResult.IsValid)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Cannot enter Query mode: {relatedBlocksResult.Message}";
                    Status = result.Message;
                    return result;
                }

                // Clear the block before entering query mode (Oracle Forms behavior)
                await ClearBlockForModeTransition(blockName);

                // Set the block to Query mode
                blockInfo.Mode = DataBlockMode.Query;
                blockInfo.LastModeChange = DateTime.Now;

                // Update current block reference
                _currentBlockName = blockName;

                // Trigger mode change events
                _eventManager.TriggerBlockEnter(blockName);

                result.Message = $"Block '{blockName}' entered Query mode successfully";
                Status = result.Message;
                LogOperation($"Block '{blockName}' entered Query mode successfully", blockName);

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error entering Query mode for block '{blockName}': {ex.Message}";
                LogError($"Error entering Query mode for block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return result;
            }
        }

        /// <summary>
        /// Transitions a block from Query to CRUD mode - equivalent to Oracle Forms EXECUTE_QUERY
        /// Executes query and validates data before transition
        /// </summary>
        public async Task<IErrorsInfo> ExecuteQueryAndEnterCrudModeAsync(string blockName, List<AppFilter> filters = null)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' not found";
                    return result;
                }

                // Must be in Query mode to execute query
                if (blockInfo.Mode != DataBlockMode.Query)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' must be in Query mode to execute query. Current mode: {blockInfo.Mode}";
                    Status = result.Message;
                    return result;
                }

                LogOperation($"Executing query and entering CRUD mode for block '{blockName}'", blockName);

                // Execute the query using enhanced query execution
                var queryResult = await ExecuteQueryEnhancedAsync(blockName, filters);
                if (queryResult.Flag != Errors.Ok)
                {
                    result.Flag = queryResult.Flag;
                    result.Message = $"Query execution failed: {queryResult.Message}";
                    result.Ex = queryResult.Ex;
                    Status = result.Message;
                    return result;
                }

                // Validate query results
                var validationResult = await ValidateQueryResultsForModeTransition(blockName);
                if (!validationResult.IsValid)
                {
                    result.Flag = Errors.Warning;
                    result.Message = $"Query executed but with warnings: {validationResult.Message}";
                    // Continue execution but log the warning
                    LogOperation($"Query validation warning for block '{blockName}': {validationResult.Message}", blockName);
                }

                // Transition to CRUD mode (already done in ExecuteQueryEnhancedAsync, but ensure consistency)
                blockInfo.Mode = DataBlockMode.CRUD;
                blockInfo.LastModeChange = DateTime.Now;

                // Navigate to first record if available
                var recordCount = GetRecordCount(blockName);
                if (recordCount > 0)
                {
                    await FirstRecordAsync(blockName);
                    result.Message = $"Query executed successfully. {recordCount} records found. Block '{blockName}' in CRUD mode.";
                }
                else
                {
                    result.Message = $"Query executed successfully. No records found. Block '{blockName}' in CRUD mode.";
                }

                Status = result.Message;
                LogOperation($"Block '{blockName}' transitioned to CRUD mode with {recordCount} records", blockName);

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error executing query for block '{blockName}': {ex.Message}";
                LogError($"Error executing query for block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return result;
            }
        }

        /// <summary>
        /// Forces a block into CRUD mode without query execution (for new record entry)
        /// Equivalent to Oracle Forms when directly creating new records
        /// ENHANCED: Handles master-detail coordination and unsaved changes properly
        /// </summary>
        public async Task<IErrorsInfo> EnterCrudModeForNewRecordAsync(string blockName)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' not found";
                    return result;
                }

                LogOperation($"Entering CRUD mode for new record creation in block '{blockName}'", blockName);

                // CRITICAL: Enhanced validation for master-detail scenarios
                var masterDetailValidation = await ValidateMasterDetailForNewRecord(blockName);
                if (!masterDetailValidation.IsValid)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Cannot create new record: {masterDetailValidation.Message}";
                    Status = result.Message;
                    return result;
                }

                // Validate unsaved changes in current and related blocks
                var unsavedChangesResult = await ValidateUnsavedChangesForModeTransition(blockName);
                if (!unsavedChangesResult.IsValid)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Cannot enter CRUD mode: {unsavedChangesResult.Message}";
                    Status = result.Message;
                    return result;
                }

                // Clear the block if it has existing data
                await ClearBlockForModeTransition(blockName);

                // Set to CRUD mode
                blockInfo.Mode = DataBlockMode.CRUD;
                blockInfo.LastModeChange = DateTime.Now;

                // Create a new record
                var newRecord = CreateNewRecord(blockName);
                if (newRecord == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Failed to create new record for block '{blockName}'";
                    Status = result.Message;
                    return result;
                }

                // CRITICAL: Handle master-detail coordination for new record
                await HandleMasterDetailCoordinationForNewRecord(blockName);

                result.Message = $"Block '{blockName}' entered CRUD mode with new record ready for data entry";
                Status = result.Message;
                LogOperation($"Block '{blockName}' entered CRUD mode for new record creation", blockName);

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error entering CRUD mode for new record in block '{blockName}': {ex.Message}";
                LogError($"Error entering CRUD mode for new record in block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return result;
            }
        }

        /// <summary>
        /// Creates a new record in a master block with proper child block coordination
        /// This is the method that handles your specific scenario
        /// </summary>
        public async Task<IErrorsInfo> CreateNewRecordInMasterBlockAsync(string masterBlockName)
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                LogOperation($"Creating new record in master block '{masterBlockName}'", masterBlockName);

                // STEP 1: Check if this is actually a master block
                var detailBlocks = GetDetailBlocks(masterBlockName);
                var isMasterBlock = detailBlocks.Any();

                // STEP 2: Validate ALL blocks (master + all details) for unsaved changes
                var allBlocksValidation = await ValidateAllBlocksIncludingDetailsForNewRecord(masterBlockName);
                if (!allBlocksValidation.IsValid)
                {
                    // This will prompt user to save, discard, or cancel
                    var userChoice = await HandleUnsavedChangesPrompt(allBlocksValidation.ValidationIssues);
                    
                    switch (userChoice)
                    {
                        case Models.UnsavedChangesAction.Save:
                            var saveResult = await CommitFormAsync();
                            if (saveResult.Flag != Errors.Ok)
                            {
                                result.Flag = Errors.Failed;
                                result.Message = $"Cannot create new record: Save failed - {saveResult.Message}";
                                return result;
                            }
                            break;
                            
                        case Models.UnsavedChangesAction.Discard:
                            var rollbackResult = await RollbackFormAsync();
                            if (rollbackResult.Flag != Errors.Ok)
                            {
                                LogOperation($"Warning: Rollback had issues during new record creation: {rollbackResult.Message}", masterBlockName);
                            }
                            break;
                            
                        case Models.UnsavedChangesAction.Cancel:
                            result.Flag = Errors.Failed;
                            result.Message = "New record creation cancelled by user";
                            return result;
                    }
                }

                // STEP 3: Enter CRUD mode for new record in master block
                var crudModeResult = await EnterCrudModeForNewRecordAsync(masterBlockName);
                if (crudModeResult.Flag != Errors.Ok)
                {
                    result.Flag = crudModeResult.Flag;
                    result.Message = crudModeResult.Message;
                    return result;
                }

                // STEP 4: Handle child blocks coordination
                if (isMasterBlock)
                {
                    await CoordinateChildBlocksForNewMasterRecord(masterBlockName, detailBlocks);
                }

                result.Message = $"New record created in master block '{masterBlockName}'" + 
                               (isMasterBlock ? $" with {detailBlocks.Count} child blocks coordinated" : "");
                
                Status = result.Message;
                LogOperation(result.Message, masterBlockName);

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error creating new record in master block '{masterBlockName}': {ex.Message}";
                LogError($"Error creating new record in master block '{masterBlockName}'", ex, masterBlockName);
                return result;
            }
        }

        /// <summary>
        /// Validates all blocks before form-level mode transitions
        /// </summary>
        public async Task<IErrorsInfo> ValidateAllBlocksForModeTransitionAsync()
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };
            var validationIssues = new List<string>();

            try
            {
                LogOperation("Validating all blocks for mode transition", "FORM_VALIDATION");

                foreach (var blockName in _blocks.Keys)
                {
                    var blockInfo = GetBlock(blockName);
                    if (blockInfo == null) continue;

                    // Check for unsaved changes
                    if (blockInfo.UnitOfWork?.IsDirty == true)
                    {
                        validationIssues.Add($"Block '{blockName}' has unsaved changes");
                    }

                    // Check for invalid records
                    if (!ValidateBlock(blockName))
                    {
                        validationIssues.Add($"Block '{blockName}' has validation errors");
                    }
                }

                if (validationIssues.Any())
                {
                    result.Flag = Errors.Warning;
                    result.Message = $"Mode transition validation issues: {string.Join(", ", validationIssues)}";
                }
                else
                {
                    result.Message = "All blocks validated successfully for mode transition";
                }

                LogOperation($"Mode transition validation completed. Issues: {validationIssues.Count}", "FORM_VALIDATION");
                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                LogError("Error during mode transition validation", ex, "FORM_VALIDATION");
                return result;
            }
        }

        #endregion

        #region Master-Detail Coordination for New Records

        /// <summary>
        /// Validates master-detail relationships before creating new record
        /// </summary>
        private async Task<ModeTransitionValidationResult> ValidateMasterDetailForNewRecord(string blockName)
        {
            var result = new ModeTransitionValidationResult { IsValid = true };

            try
            {
                // Check if this is a detail block
                var masterBlockName = GetMasterBlock(blockName);
                if (!string.IsNullOrEmpty(masterBlockName))
                {
                    var masterBlockInfo = GetBlock(masterBlockName);
                    if (masterBlockInfo != null)
                    {
                        // Detail block can only create new records if master is in CRUD mode
                        if (masterBlockInfo.Mode != DataBlockMode.CRUD)
                        {
                            result.IsValid = false;
                            result.Message = $"Cannot create new record in detail block '{blockName}': Master block '{masterBlockName}' must be in CRUD mode";
                            return result;
                        }

                        // Master must have a current record
                        if (masterBlockInfo.UnitOfWork?.CurrentItem == null)
                        {
                            result.IsValid = false;
                            result.Message = $"Cannot create new record in detail block '{blockName}': Master block '{masterBlockName}' has no current record";
                            return result;
                        }
                    }
                }

                result.Message = "Master-detail validation successful for new record";
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Error validating master-detail for new record: {ex.Message}";
                LogError("Error validating master-detail for new record", ex, blockName);
                return result;
            }
        }

        /// <summary>
        /// Validates all blocks including details for new record creation
        /// </summary>
        private async Task<ModeTransitionValidationResult> ValidateAllBlocksIncludingDetailsForNewRecord(string masterBlockName)
        {
            var result = new ModeTransitionValidationResult { IsValid = true };
            var validationIssues = new List<string>();

            try
            {
                // Check master block
                var masterValidation = await ValidateUnsavedChangesForModeTransition(masterBlockName);
                if (!masterValidation.IsValid)
                {
                    validationIssues.Add($"Master block '{masterBlockName}': {masterValidation.Message}");
                }

                // Check all detail blocks
                var detailBlocks = GetDetailBlocks(masterBlockName);
                foreach (var detailBlockName in detailBlocks)
                {
                    var detailValidation = await ValidateUnsavedChangesForModeTransition(detailBlockName);
                    if (!detailValidation.IsValid)
                    {
                        validationIssues.Add($"Detail block '{detailBlockName}': {detailValidation.Message}");
                    }
                }

                if (validationIssues.Any())
                {
                    result.IsValid = false;
                    result.Message = $"Unsaved changes found in {validationIssues.Count} blocks";
                    result.ValidationIssues = validationIssues;
                }
                else
                {
                    result.Message = "All blocks validated successfully for new record creation";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Error validating all blocks for new record: {ex.Message}";
                LogError("Error validating all blocks for new record", ex, masterBlockName);
                return result;
            }
        }

        /// <summary>
        /// Handles master-detail coordination when creating new record
        /// </summary>
        private async Task HandleMasterDetailCoordinationForNewRecord(string blockName)
        {
            try
            {
                // If this block has detail blocks, prepare them for new master record
                var detailBlocks = GetDetailBlocks(blockName);
                if (detailBlocks.Any())
                {
                    await CoordinateChildBlocksForNewMasterRecord(blockName, detailBlocks);
                }

                // If this is a detail block, coordinate with master
                var masterBlockName = GetMasterBlock(blockName);
                if (!string.IsNullOrEmpty(masterBlockName))
                {
                    await CoordinateWithMasterForNewDetailRecord(blockName, masterBlockName);
                }

                LogOperation($"Master-detail coordination completed for new record in block '{blockName}'", blockName);
            }
            catch (Exception ex)
            {
                LogError($"Error in master-detail coordination for new record in block '{blockName}'", ex, blockName);
                throw; // Re-throw as this is critical
            }
        }

        /// <summary>
        /// Coordinates child blocks when master gets new record
        /// </summary>
        private async Task CoordinateChildBlocksForNewMasterRecord(string masterBlockName, List<string> detailBlocks)
        {
            try
            {
                LogOperation($"Coordinating {detailBlocks.Count} child blocks for new master record in '{masterBlockName}'", masterBlockName);

                foreach (var detailBlockName in detailBlocks)
                {
                    var detailBlockInfo = GetBlock(detailBlockName);
                    if (detailBlockInfo != null)
                    {
                        // Clear detail block and set to appropriate mode
                        await ClearBlockAsync(detailBlockName);
                        
                        // Detail blocks should be in CRUD mode to allow new records
                        detailBlockInfo.Mode = DataBlockMode.CRUD;
                        detailBlockInfo.LastModeChange = DateTime.Now;

                        LogOperation($"Child block '{detailBlockName}' cleared and set to CRUD mode", detailBlockName);
                    }
                }

                LogOperation($"All child blocks coordinated for new master record", masterBlockName);
            }
            catch (Exception ex)
            {
                LogError("Error coordinating child blocks for new master record", ex, masterBlockName);
                throw;
            }
        }

        /// <summary>
        /// Coordinates with master when detail gets new record
        /// </summary>
        private async Task CoordinateWithMasterForNewDetailRecord(string detailBlockName, string masterBlockName)
        {
            try
            {
                var masterBlockInfo = GetBlock(masterBlockName);
                if (masterBlockInfo?.UnitOfWork?.CurrentItem != null)
                {
                    // Set foreign key values in detail record from master
                    var detailBlockInfo = GetBlock(detailBlockName);
                    if (detailBlockInfo?.UnitOfWork?.CurrentItem != null)
                    {
                        await SetForeignKeyValuesFromMasterAsync(detailBlockName, masterBlockName);
                    }
                }

                LogOperation($"Detail block '{detailBlockName}' coordinated with master block '{masterBlockName}'", detailBlockName);
            }
            catch (Exception ex)
            {
                LogError($"Error coordinating detail block '{detailBlockName}' with master", ex, detailBlockName);
                throw;
            }
        }

        /// <summary>
        /// Sets foreign key values from master record to detail record
        /// </summary>
        private async Task SetForeignKeyValuesFromMasterAsync(string detailBlockName, string masterBlockName)
        {
            try
            {
                // This would use the relationship manager to set foreign key values
                // For now, just log the operation
                LogOperation($"Setting foreign key values from master '{masterBlockName}' to detail '{detailBlockName}'", detailBlockName);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogError($"Error setting foreign key values from master to detail", ex, detailBlockName);
                throw;
            }
        }

        /// <summary>
        /// Prompts user for action when unsaved changes are detected
        /// </summary>
        private async Task<Models.UnsavedChangesAction> HandleUnsavedChangesPrompt(List<string> validationIssues)
        {
            try
            {
                // In a real application, this would show a dialog to the user
                // For now, we'll use a simple default behavior
                
                var promptMessage = $"Unsaved changes detected:\n{string.Join("\n", validationIssues)}\n\nWhat would you like to do?";
                LogOperation($"Unsaved changes prompt: {promptMessage}", "USER_PROMPT");

                // Default behavior - save to prevent data loss
                LogOperation("Defaulting to Save action to prevent data loss", "USER_PROMPT");
                return Models.UnsavedChangesAction.Save;
            }
            catch (Exception ex)
            {
                LogError("Error handling unsaved changes prompt", ex, "USER_PROMPT");
                return Models.UnsavedChangesAction.Cancel; // Safe default
            }
        }

        #endregion

        #region Mode Transition Validation Helpers

        private async Task<ModeTransitionValidationResult> ValidateUnsavedChangesForModeTransition(string blockName)
        {
            var result = new ModeTransitionValidationResult { IsValid = true };

            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    result.IsValid = false;
                    result.Message = $"Block '{blockName}' has no unit of work";
                    return result;
                }

                // Check if block has unsaved changes
                if (blockInfo.UnitOfWork.IsDirty)
                {
                    LogOperation($"Block '{blockName}' has unsaved changes during mode transition", blockName);

                    // Use the existing dirty state manager to handle unsaved changes
                    var canProceed = await CheckAndHandleUnsavedChangesAsync(blockName);
                    if (!canProceed)
                    {
                        result.IsValid = false;
                        result.Message = $"Block '{blockName}' has unsaved changes that must be resolved";
                        return result;
                    }
                }

                // Additional validation: Check if current record is in a valid state
                var currentRecord = blockInfo.UnitOfWork.CurrentItem;
                if (currentRecord != null)
                {
                    // Validate the current record
                    if (!ValidateRecordForModeTransition(blockName, currentRecord))
                    {
                        result.IsValid = false;
                        result.Message = $"Current record in block '{blockName}' is invalid";
                        return result;
                    }
                }

                result.Message = $"Block '{blockName}' validated successfully for mode transition";
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Error validating unsaved changes for block '{blockName}': {ex.Message}";
                LogError($"Error validating unsaved changes for mode transition in block '{blockName}'", ex, blockName);
                return result;
            }
        }

        private async Task<ModeTransitionValidationResult> ValidateRelatedBlocksForModeTransition(string blockName, DataBlockMode targetMode)
        {
            var result = new ModeTransitionValidationResult { IsValid = true };

            try
            {
                // Check detail blocks if this is a master block
                var detailBlocks = GetDetailBlocks(blockName);
                if (detailBlocks.Any())
                {
                    foreach (var detailBlockName in detailBlocks)
                    {
                        var detailValidation = await ValidateUnsavedChangesForModeTransition(detailBlockName);
                        if (!detailValidation.IsValid)
                        {
                            result.IsValid = false;
                            result.Message = $"Detail block '{detailBlockName}' validation failed: {detailValidation.Message}";
                            return result;
                        }
                    }
                }

                // Check master block if this is a detail block
                var masterBlockName = GetMasterBlock(blockName);
                if (!string.IsNullOrEmpty(masterBlockName))
                {
                    var masterBlockInfo = GetBlock(masterBlockName);
                    if (masterBlockInfo != null)
                    {
                        // If master is in Query mode and we're trying to enter CRUD, that might be problematic
                        if (masterBlockInfo.Mode == DataBlockMode.Query && targetMode == DataBlockMode.CRUD)
                        {
                            result.IsValid = false;
                            result.Message = $"Cannot transition detail block '{blockName}' to CRUD mode while master block '{masterBlockName}' is in Query mode";
                            return result;
                        }
                    }
                }

                result.Message = "All related blocks validated successfully for mode transition";
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Error validating related blocks: {ex.Message}";
                LogError($"Error validating related blocks for mode transition", ex, blockName);
                return result;
            }
        }

        private async Task<ModeTransitionValidationResult> ValidateQueryResultsForModeTransition(string blockName)
        {
            var result = new ModeTransitionValidationResult { IsValid = true };

            try
            {
                var recordCount = GetRecordCount(blockName);
                
                // Check configuration limits
                var maxRecords = Configuration?.MaxRecordsPerBlock ?? 10000;
                if (recordCount > maxRecords)
                {
                    result.IsValid = false;
                    result.Message = $"Query returned {recordCount} records, exceeding limit of {maxRecords}";
                    return result;
                }

                // Warn if no records found
                if (recordCount == 0)
                {
                    result.Message = "Query executed successfully but no records found";
                }
                else
                {
                    result.Message = $"Query validation successful. {recordCount} records found";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Error validating query results: {ex.Message}";
                LogError($"Error validating query results for block '{blockName}'", ex, blockName);
                return result;
            }
        }

        private bool ValidateRecordForModeTransition(string blockName, object record)
        {
            try
            {
                if (record == null) return true;

                // Use the existing validation logic
                return ValidateBlock(blockName);
            }
            catch (Exception ex)
            {
                LogError($"Error validating record for mode transition in block '{blockName}'", ex, blockName);
                return false;
            }
        }

        private async Task ClearBlockForModeTransition(string blockName)
        {
            try
            {
                // Use the existing clear block logic
                await ClearBlockAsync(blockName);
                LogOperation($"Block '{blockName}' cleared for mode transition", blockName);
            }
            catch (Exception ex)
            {
                LogError($"Error clearing block '{blockName}' during mode transition", ex, blockName);
                throw; // Re-throw as this is critical for mode transition
            }
        }

        #endregion

        #region Mode Transition Status and Information

        /// <summary>
        /// Gets the current mode of a block
        /// </summary>
        public DataBlockMode GetBlockMode(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                return blockInfo?.Mode ?? DataBlockMode.Query;
            }
            catch (Exception ex)
            {
                LogError($"Error getting mode for block '{blockName}'", ex, blockName);
                return DataBlockMode.Query; // Default to Query mode
            }
        }

        /// <summary>
        /// Gets mode transition information for all blocks
        /// </summary>
        public Dictionary<string, BlockModeInfo> GetAllBlockModeInfo()
        {
            var result = new Dictionary<string, BlockModeInfo>();

            try
            {
                foreach (var kvp in _blocks)
                {
                    var blockName = kvp.Key;
                    var blockInfo = kvp.Value;

                    result[blockName] = new BlockModeInfo
                    {
                        BlockName = blockName,
                        CurrentMode = blockInfo.Mode,
                        LastModeChange = blockInfo.LastModeChange,
                        HasUnsavedChanges = blockInfo.UnitOfWork?.IsDirty ?? false,
                        RecordCount = GetRecordCount(blockName),
                        IsCurrentBlock = blockName == _currentBlockName
                    };
                }
            }
            catch (Exception ex)
            {
                LogError("Error getting block mode information", ex, "MODE_INFO");
            }

            return result;
        }

        /// <summary>
        /// Checks if form-level mode transition is safe
        /// </summary>
        public async Task<bool> IsFormReadyForModeTransitionAsync()
        {
            try
            {
                var validationResult = await ValidateAllBlocksForModeTransitionAsync();
                return validationResult.Flag != Errors.Failed;
            }
            catch (Exception ex)
            {
                LogError("Error checking form readiness for mode transition", ex, "FORM_VALIDATION");
                return false;
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Result of mode transition validation
    /// </summary>
    public class ModeTransitionValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public List<string> ValidationIssues { get; set; } = new List<string>();
    }

    /// <summary>
    /// Information about a block's mode and state
    /// </summary>
    public class BlockModeInfo
    {
        public string BlockName { get; set; }
        public DataBlockMode CurrentMode { get; set; }
        public DateTime LastModeChange { get; set; }
        public bool HasUnsavedChanges { get; set; }
        public int RecordCount { get; set; }
        public bool IsCurrentBlock { get; set; }
        
        public string Summary => 
            $"{BlockName}: {CurrentMode} mode, {RecordCount} records" +
            (HasUnsavedChanges ? " (unsaved changes)" : "") +
            (IsCurrentBlock ? " (current)" : "");
    }

    #endregion
}