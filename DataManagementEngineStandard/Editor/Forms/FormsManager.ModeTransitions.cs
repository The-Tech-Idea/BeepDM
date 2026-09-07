using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
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
        /// Maps a <see cref="DataBlockMode"/> transition onto Oracle Forms'
        /// :SYSTEM.MODE vocabulary. Oracle publishes exactly two values for
        /// this variable -- NORMAL and ENTER-QUERY -- so every mode other
        /// than EnterQuery collapses to NORMAL, same as real Oracle Forms.
        /// </summary>
        private static string ToSystemVariableMode(DataBlockMode mode) =>
            mode == DataBlockMode.EnterQuery ? "ENTER-QUERY" : "NORMAL";

        public async void EnteringQueryModeAsync(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            try { await EnterQueryAsync(blockName); }
            catch (Exception ex) { LogError($"Error entering Query mode for block '{blockName}'", ex, blockName); }
        }

        public async void ExitingQueryModeAsync(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo != null && (blockInfo.Mode == DataBlockMode.Query || blockInfo.Mode == DataBlockMode.EnterQuery))
                {
                    await ExecuteQueryAsync(blockName).ConfigureAwait(false);
                }
            }
            catch (Exception ex) { LogError($"Error exiting Query mode for block '{blockName}'", ex, blockName); }
        }

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

                // If the user is already typing criteria, no need to transition.
                //
                // This compared against DataBlockMode.Query until 2026-08-01. A
                // block is registered with Mode = Query (CreateBlockInfo), so
                // the guard matched immediately and ENTER_QUERY became a no-op —
                // it returned success without clearing the block or changing
                // mode. Per DataBlockMode's own documentation, Query is "results
                // are loaded and editable"; EnterQuery is "the user is typing
                // example criteria", which is what this method performs.
                if (blockInfo.Mode == DataBlockMode.EnterQuery)
                {
                    result.Message = $"Block '{blockName}' is already in Enter-Query mode";
                    Status = result.Message;
                    return result;
                }

                LogOperation($"Attempting to enter Query mode for block '{blockName}'", blockName);

                // CRITICAL: Check for unsaved changes in current block AND all related blocks
                var unsavedChangesResult = await ValidateUnsavedChangesForModeTransition(blockName).ConfigureAwait(false);
                if (!unsavedChangesResult.IsValid)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Cannot enter Query mode: {unsavedChangesResult.Message}";
                    Status = result.Message;
                    return result;
                }

                // Check for unsaved changes in related blocks (detail blocks)
                var relatedBlocksResult = await ValidateRelatedBlocksForModeTransition(blockName, DataBlockMode.Query).ConfigureAwait(false);
                if (!relatedBlocksResult.IsValid)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Cannot enter Query mode: {relatedBlocksResult.Message}";
                    Status = result.Message;
                    return result;
                }

                // Clear the block before entering query mode (Oracle Forms behavior)
                await ClearBlockForModeTransition(blockName).ConfigureAwait(false);

                // Set the block to Enter-Query mode — the user is now typing
                // criteria, not looking at results. Nothing in the engine set
                // EnterQuery before this; it was only ever read (three sites
                // treat Query||EnterQuery as query-ish), so hosts deriving
                // "am I in query mode?" from the block mode — as
                // WinFormBlockHost.SyncFromManager does — could never see it and
                // silently dropped out of query mode on the next sync.
                blockInfo.Mode = DataBlockMode.EnterQuery;
                blockInfo.LastModeChange = DateTime.Now;

                // :SYSTEM.MODE -- see G0.36 in gaps.md. blockInfo.Mode has no
                // single choke point (four direct assignment sites across two
                // files); wired individually, same shape as CurrentFormName's
                // three writers.
                _systemVariablesManager?.SetMode(ToSystemVariableMode(DataBlockMode.EnterQuery));

                // Update current block reference
                _currentBlockName = blockName;

                // Trigger mode change events
                _eventManager.TriggerBlockEnter(blockName);

                // TriggerType.EnterQuery (Oracle Forms ENTER_QUERY) existed with
                // no firing code anywhere -- confirmed by grepping the whole
                // engine, and TriggerLibrary.cs only ever registers/fires
                // PreQuery/PostQuery. A trigger registered for it through the
                // standard RegisterBlockTrigger path was correctly stored and
                // never once invoked, since this is the only place a block
                // transitions into enter-query mode. Fired after the mode
                // change and current-block update, matching where PreQuery/
                // PostQuery fire relative to their own state changes. (2026-08-26)
                await _triggerManager.FireBlockTriggerAsync(
                    TriggerType.EnterQuery, blockName,
                    TriggerContext.ForBlock(TriggerType.EnterQuery, blockName, null, _dmeEditor)).ConfigureAwait(false);

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

                // B2: must be in Query mode OR EnterQuery mode to execute query.
                // Oracle Forms allows the user to be in EnterQuery (typing criteria)
                // and then press EXECUTE_QUERY (or F8) to materialize the result
                // without first leaving EnterQuery. The previous strict
                // `!= DataBlockMode.Query` check rejected EnterQuery, blocking
                // that flow and forcing the user to leave EnterQuery first.
                if (blockInfo.Mode != DataBlockMode.Query && blockInfo.Mode != DataBlockMode.EnterQuery)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Block '{blockName}' must be in Query or Enter-Query mode to execute query. Current mode: {blockInfo.Mode}";
                    Status = result.Message;
                    return result;
                }

                // Captured before the transition below, for the EXIT_QUERY fire
                // point further down -- ExitQuery (Oracle Forms EXIT_QUERY) pairs
                // specifically with a block that was actually in enter-query mode,
                // not one that was already sitting in plain Query mode and simply
                // re-executed.
                var wasInEnterQueryMode = blockInfo.Mode == DataBlockMode.EnterQuery;

                LogOperation($"Executing query and entering CRUD mode for block '{blockName}' (source mode={blockInfo.Mode})", blockName);

                // Execute the query using enhanced query execution
                var queryResult = await ExecuteQueryEnhancedAsync(blockName, filters).ConfigureAwait(false);
                if (queryResult.Flag != Errors.Ok)
                {
                    result.Flag = queryResult.Flag;
                    result.Message = $"Query execution failed: {queryResult.Message}";
                    result.Ex = queryResult.Ex;
                    Status = result.Message;
                    return result;
                }

                // Validate query results
                var validationResult = await ValidateQueryResultsForModeTransition(blockName).ConfigureAwait(false);
                if (!validationResult.IsValid)
                {
                    result.Flag = Errors.Warning;
                    result.Message = $"Query executed but with warnings: {validationResult.Message}";
                    // Continue execution but log the warning
                    LogOperation($"Query validation warning for block '{blockName}': {validationResult.Message}", blockName);
                }

                // B3: ExecuteQueryEnhancedAsync has already transitioned the
                // block to CRUD mode on success. The previous "ensure
                // consistency" re-assignment was redundant and masked any
                // inconsistency between the helper and the outer caller.
                // Trust the helper; if the mode is wrong here, the helper is
                // broken and a future audit pass will catch it.
                //
                // We still update LastModeChange so callers can tell that a
                // mode transition happened, even if the target mode was
                // already set by the helper.
                blockInfo.LastModeChange = DateTime.Now;

                // TriggerType.ExitQuery (Oracle Forms EXIT_QUERY) existed with no
                // firing code anywhere, same defect as EnterQuery above. Fired
                // only when the block actually was in enter-query mode --
                // matches Oracle Forms pairing EXIT_QUERY with ENTER_QUERY, not
                // with an ordinary re-query of a block already showing results.
                // (2026-08-26)
                if (wasInEnterQueryMode)
                {
                    await _triggerManager.FireBlockTriggerAsync(
                        TriggerType.ExitQuery, blockName,
                        TriggerContext.ForBlock(TriggerType.ExitQuery, blockName, null, _dmeEditor)).ConfigureAwait(false);
                }

                // Navigate to first record if available
                var recordCount = GetRecordCount(blockName);
                if (recordCount > 0)
                {
                    await FirstRecordAsync(blockName).ConfigureAwait(false);
                }

                // Only overwrite result.Message with the generic success text when
                // there was nothing to warn about. Before this, the "Query executed
                // but with warnings: ..." message set above (result.Flag stayed
                // Warning, correctly) was unconditionally clobbered here on every
                // path with at least one record -- so a caller reading only
                // result.Message (the natural thing to show a user) never learned
                // *why* the flag said Warning, for any validation warning past or
                // future, not just the MaxRecords one this pass added a reader for.
                if (validationResult.IsValid)
                {
                    result.Message = recordCount > 0
                        ? $"Query executed successfully. {recordCount} records found. Block '{blockName}' in CRUD mode."
                        : $"Query executed successfully. No records found. Block '{blockName}' in CRUD mode.";
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
                var masterDetailValidation = await ValidateMasterDetailForNewRecord(blockName).ConfigureAwait(false);
                if (!masterDetailValidation.IsValid)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Cannot create new record: {masterDetailValidation.Message}";
                    Status = result.Message;
                    return result;
                }

                // Validate unsaved changes in current and related blocks
                var unsavedChangesResult = await ValidateUnsavedChangesForModeTransition(blockName).ConfigureAwait(false);
                if (!unsavedChangesResult.IsValid)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Cannot enter CRUD mode: {unsavedChangesResult.Message}";
                    Status = result.Message;
                    return result;
                }

                // Clear the block if it has existing data
                await ClearBlockForModeTransition(blockName).ConfigureAwait(false);

                // Set to CRUD mode
                blockInfo.Mode = DataBlockMode.CRUD;
                blockInfo.LastModeChange = DateTime.Now;
                _systemVariablesManager?.SetMode(ToSystemVariableMode(DataBlockMode.CRUD));

                // Create a new record
                var newRecord = CreateNewRecord(blockName);
                if (newRecord == null)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Failed to create new record for block '{blockName}'";
                    Status = result.Message;
                    return result;
                }

                // :SYSTEM.BLOCK_STATUS / :SYSTEM.RECORD_STATUS -- see G0.36 in gaps.md.
                // A blank record created directly (not from a query) is Oracle Forms'
                // "NEW" status -- distinct from "CHANGED", which the ItemChanged handler
                // sets once the user actually edits a field on it.
                _systemVariablesManager?.SetBlockStatus(blockName, "NEW");
                _systemVariablesManager?.SetRecordStatus(blockName, "NEW");

                // CRITICAL: Handle master-detail coordination for new record
                await HandleMasterDetailCoordinationForNewRecord(blockName).ConfigureAwait(false);

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
                var allBlocksValidation = await ValidateAllBlocksIncludingDetailsForNewRecord(masterBlockName).ConfigureAwait(false);
                if (!allBlocksValidation.IsValid)
                {
                    // This will prompt user to save, discard, or cancel
                    var userChoice = await HandleUnsavedChangesPrompt(allBlocksValidation.ValidationIssues).ConfigureAwait(false);
                    
                    switch (userChoice)
                    {
                        case Models.UnsavedChangesAction.Save:
                            var saveResult = await CommitFormAsync().ConfigureAwait(false);
                            if (saveResult.Flag != Errors.Ok)
                            {
                                result.Flag = Errors.Failed;
                                result.Message = $"Cannot create new record: Save failed - {saveResult.Message}";
                                return result;
                            }
                            break;
                            
                        case Models.UnsavedChangesAction.Discard:
                            var rollbackResult = await RollbackFormAsync().ConfigureAwait(false);
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
                var crudModeResult = await EnterCrudModeForNewRecordAsync(masterBlockName).ConfigureAwait(false);
                if (crudModeResult.Flag != Errors.Ok)
                {
                    result.Flag = crudModeResult.Flag;
                    result.Message = crudModeResult.Message;
                    return result;
                }

                // STEP 4: Handle child blocks coordination
                if (isMasterBlock)
                {
                    await CoordinateChildBlocksForNewMasterRecord(masterBlockName, detailBlocks).ConfigureAwait(false);
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
                var masterValidation = await ValidateUnsavedChangesForModeTransition(masterBlockName).ConfigureAwait(false);
                if (!masterValidation.IsValid)
                {
                    validationIssues.Add($"Master block '{masterBlockName}': {masterValidation.Message}");
                }

                // Check all detail blocks
                var detailBlocks = GetDetailBlocks(masterBlockName);
                foreach (var detailBlockName in detailBlocks)
                {
                    var detailValidation = await ValidateUnsavedChangesForModeTransition(detailBlockName).ConfigureAwait(false);
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
                    await CoordinateChildBlocksForNewMasterRecord(blockName, detailBlocks).ConfigureAwait(false);
                }

                // If this is a detail block, coordinate with master
                var masterBlockName = GetMasterBlock(blockName);
                if (!string.IsNullOrEmpty(masterBlockName))
                {
                    await CoordinateWithMasterForNewDetailRecord(blockName, masterBlockName).ConfigureAwait(false);
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
                        await ClearBlockAsync(detailBlockName).ConfigureAwait(false);
                        
                        // Detail blocks should be in CRUD mode to allow new records
                        detailBlockInfo.Mode = DataBlockMode.CRUD;
                        detailBlockInfo.LastModeChange = DateTime.Now;
                        _systemVariablesManager?.SetMode(ToSystemVariableMode(DataBlockMode.CRUD));

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
                        await SetForeignKeyValuesFromMasterAsync(detailBlockName, masterBlockName).ConfigureAwait(false);
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
                var detailBlockInfo = GetBlock(detailBlockName);
                var masterBlockInfo = GetBlock(masterBlockName);
                if (detailBlockInfo?.UnitOfWork == null || masterBlockInfo?.UnitOfWork == null)
                {
                    return;
                }

                var detailItem = detailBlockInfo.UnitOfWork.CurrentItem;
                var masterItem = masterBlockInfo.UnitOfWork.CurrentItem;
                var fieldMappings = GetRelationshipFieldMappings(new DataBlockRelationship
                {
                    MasterKeyField = detailBlockInfo.MasterKeyField,
                    DetailForeignKeyField = detailBlockInfo.ForeignKeyField
                });

                var appliedAnyValue = false;
                if (detailItem != null && masterItem != null)
                {
                    foreach (var mapping in fieldMappings)
                    {
                        var masterValue = GetPropertyValue(masterItem, mapping.MasterField);
                        if (IsNullOrEmpty(masterValue) || !TrySetPropertyValue(detailItem, mapping.DetailField, masterValue))
                        {
                            continue;
                        }

                        appliedAnyValue = true;
                    }
                }

                if (!appliedAnyValue)
                {
                    LogOperation($"No master value applied from '{masterBlockName}' to '{detailBlockName}'", detailBlockName);
                }
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
        /// <remarks>
        /// Previously unconditionally returned <see cref="Models.UnsavedChangesAction.Save"/>
        /// behind a comment reading "In a real application, this would show a dialog to
        /// the user / For now, we'll use a simple default behavior" -- an honest stub, but
        /// still one that silently auto-saved on every unsaved-changes prompt, in every
        /// caller, forever: a user who wanted to discard an in-progress edit before
        /// creating a new master record got it committed instead, with no chance to say
        /// otherwise. <see cref="ShowAlertAsync"/> (Oracle Forms SHOW_ALERT, already fully
        /// implemented and already the mechanism "Messages and alerts" uses end to end on
        /// both runtime hosts) is exactly the three-button choice this needed. When no
        /// <see cref="IAlertProvider"/> is wired (a headless engine, a test) it returns
        /// <see cref="AlertResult.None"/>, which maps to Cancel -- the same safe default
        /// the exception handler below already used, and the one choice that neither
        /// silently commits data the caller may not have wanted saved nor silently
        /// discards data they may have wanted kept.
        /// </remarks>
        private async Task<Models.UnsavedChangesAction> HandleUnsavedChangesPrompt(List<string> validationIssues)
        {
            try
            {
                var promptMessage = $"Unsaved changes detected:\n{string.Join("\n", validationIssues)}\n\nWhat would you like to do?";
                LogOperation($"Unsaved changes prompt: {promptMessage}", "USER_PROMPT");

                var alertResult = await ShowAlertAsync(
                    "Unsaved Changes",
                    promptMessage,
                    AlertStyle.Question,
                    "Save",
                    "Discard",
                    "Cancel").ConfigureAwait(false);

                var action = alertResult switch
                {
                    AlertResult.Button1 => Models.UnsavedChangesAction.Save,
                    AlertResult.Button2 => Models.UnsavedChangesAction.Discard,
                    AlertResult.Button3 => Models.UnsavedChangesAction.Cancel,
                    _ => Models.UnsavedChangesAction.Cancel
                };

                LogOperation($"Unsaved changes prompt resolved to {action}", "USER_PROMPT");
                return action;
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
                    var canProceed = await CheckAndHandleUnsavedChangesAsync(blockName).ConfigureAwait(false);
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
                    // B20: ValidateRecordForModeTransition no longer takes the
                    // record parameter — it delegates to ValidateBlock which
                    // already inspects the UoW's current record (which is
                    // exactly `currentRecord` here). Passing the record as a
                    // parameter was misleading: the previous implementation
                    // ignored the parameter and re-fetched via the block.
                    if (!ValidateRecordForModeTransition(blockName))
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
                        var detailValidation = await ValidateUnsavedChangesForModeTransition(detailBlockName).ConfigureAwait(false);
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

                // Check configuration limits. A block explicitly registered in
                // Configuration.BlockConfigurations carries its own MaxRecords
                // (BlockConfiguration.cs: "the maximum number of records to
                // load") -- that per-block override existed with no reader
                // anywhere, so setting it had no effect and every block was
                // silently governed by the manager-wide MaxRecordsPerBlock
                // default instead. Only consulted when the block was actually
                // registered in the dictionary (TryGetValue, not
                // GetBlockConfiguration's own "or a fresh default" fallback) --
                // BlockConfiguration.MaxRecords' compile-time default (1000)
                // does not coincide with MaxRecordsPerBlock's (10000), so
                // treating every never-configured block as if it had
                // authored 1000 would silently tighten the limit for every
                // existing block that never touched this API.
                var maxRecords = Configuration?.BlockConfigurations.TryGetValue(blockName, out var blockConfig) == true
                    ? blockConfig.MaxRecords
                    : Configuration?.MaxRecordsPerBlock ?? 10000;
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

        private bool ValidateRecordForModeTransition(string blockName)
        {
            // B20: the previous signature took a `record` parameter that was
            // never used — the function delegated straight to
            // ValidateBlock(blockName), which already inspects the UoW's
            // current record. We dropped the parameter so the call site
            // signature matches the actual behavior. The caller is still
            // responsible for guarding the null-record case (the
            // `currentRecord != null` check at the call site).
            try
            {
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
                await ClearBlockAsync(blockName).ConfigureAwait(false);
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
        /// Gets the current mode of a block.
        /// </summary>
        /// <remarks>
        /// B23: if <paramref name="blockName"/> is null/empty or the block is
        /// not registered, the method returns <see cref="DataBlockMode.Query"/>
        /// as a silent default. The caller cannot distinguish "block is in
        /// Query mode" from "block does not exist". This is intentional for
        /// back-compat with the public <c>IBeepBuiltins</c> contract — host
        /// code that calls <c>GetBlockMode</c> on a block that may not exist
        /// keeps working. Callers that need to distinguish the two cases
        /// should use <see cref="TryGetBlockMode"/> instead, which returns
        /// <c>false</c> when the block is missing and populates an output
        /// parameter with the actual mode when it is present.
        /// </remarks>
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
        /// Try to get the current mode of a block. Returns <c>true</c> when
        /// the block is registered, populating <paramref name="mode"/> with
        /// the block's actual mode. Returns <c>false</c> when the block is
        /// null, empty, or not registered — the caller can then choose
        /// whether to treat that as "block is in Query mode" or as an error.
        /// </summary>
        public bool TryGetBlockMode(string blockName, out DataBlockMode mode)
        {
            if (string.IsNullOrEmpty(blockName))
            {
                mode = DataBlockMode.Query;
                return false;
            }

            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo == null)
                {
                    mode = DataBlockMode.Query;
                    return false;
                }
                mode = blockInfo.Mode;
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error in TryGetBlockMode for block '{blockName}'", ex, blockName);
                mode = DataBlockMode.Query;
                return false;
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
                var validationResult = await ValidateAllBlocksForModeTransitionAsync().ConfigureAwait(false);
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

    // ModeTransitionValidationResult and BlockModeInfo are now in Models\ModeTransitionModels.cs
    // (moved for consistency with the rest of the Models/ catalog).
}