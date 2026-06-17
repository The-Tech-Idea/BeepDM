using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Form-level operations partial class for UnitofWorksManager
    /// </summary>
    public partial class FormsManager
    {
        #region Form-Level Events
        // Form-level triggers
        /// <summary>
        /// Raised before a form open operation is finalized.
        /// </summary>
        public event EventHandler<FormTriggerEventArgs> OnFormOpen;
        /// <summary>
        /// Raised during form close processing, including unsaved-change handling.
        /// </summary>
        public event EventHandler<FormTriggerEventArgs> OnFormClose;
        /// <summary>
        /// Raised around form commit processing.
        /// </summary>
        public event EventHandler<FormTriggerEventArgs> OnFormCommit;
        /// <summary>
        /// Raised around form rollback processing.
        /// </summary>
        public event EventHandler<FormTriggerEventArgs> OnFormRollback;
        /// <summary>
        /// Raised when form-level validation is requested.
        /// </summary>
        public event EventHandler<FormTriggerEventArgs> OnFormValidate;
        #endregion

        #region Form Operations

        /// <summary>
        /// Opens a form - equivalent to Oracle Forms WHEN-NEW-FORM-INSTANCE
        /// </summary>
        public async Task<bool> OpenFormAsync(string formName)
        {
            if (string.IsNullOrWhiteSpace(formName))
                throw new ArgumentException("Form name cannot be null or empty", nameof(formName));

            try
            {
                var args = new FormTriggerEventArgs(formName, "Opening form")
                {
                    OperationType = FormOperationType.Open
                };
                OnFormOpen?.Invoke(this, args);

                if (args.Cancel)
                {
                    Status = "Form open cancelled by trigger";
                    LogOperation($"Form open cancelled for '{formName}'");
                    return false;
                }

                // Perform any pre-initialization
                await PreInitializeFormAsync(formName);

                _currentFormName = formName;

                // Apply form-level configuration
                ApplyFormConfiguration(formName);

                Status = $"Form '{formName}' opened successfully";
                LogOperation($"Form '{formName}' opened successfully");

                // Post-initialization
                await PostInitializeFormAsync(formName);

                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error opening form '{formName}': {ex.Message}";
                LogError($"Error opening form '{formName}'", ex);
                _eventManager.TriggerError(formName, ex);
                return false;
            }
        }

        /// <summary>
        /// Closes the form - checks for unsaved changes
        /// </summary>
        public async Task<bool> CloseFormAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFormName))
                {
                    Status = "No form is currently open";
                    return true;
                }

                // Check for unsaved changes
                if (IsDirty)
                {
                    var unsavedArgs = new FormTriggerEventArgs(_currentFormName, "Form has unsaved changes")
                    {
                        OperationType = FormOperationType.Close
                    };
                    OnFormClose?.Invoke(this, unsavedArgs);

                    if (unsavedArgs.Cancel)
                    {
                        Status = "Form close cancelled - unsaved changes";
                        LogOperation("Form close cancelled due to unsaved changes");
                        return false;
                    }

                    // Handle unsaved changes based on configuration
                    var handleResult = await HandleUnsavedChangesOnCloseAsync();
                    if (!handleResult)
                        return false;
                }

                var closeArgs = new FormTriggerEventArgs(_currentFormName, "Closing form")
                {
                    OperationType = FormOperationType.Close
                };
                OnFormClose?.Invoke(this, closeArgs);

                if (!closeArgs.Cancel)
                {
                    // Perform cleanup operations
                    await PerformFormCleanupAsync();

                    var formName = _currentFormName;
                    _currentFormName = null;
                    _currentBlockName = null;

                    Status = $"Form '{formName}' closed successfully";
                    LogOperation($"Form '{formName}' closed successfully");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Status = $"Error closing form: {ex.Message}";
                LogError("Error closing form", ex);
                _eventManager.TriggerError(_currentFormName, ex);
                return false;
            }
        }

        /// <summary>
        /// Commits all changes in all blocks - equivalent to Oracle Forms COMMIT_FORM.
        /// When called from a modal child form, commits all dirty blocks across the
        /// entire call chain (parent + ancestors) so that the database session is
        /// consistent. This matches Oracle Forms' behaviour where CALL_FORM shares
        /// the same database session and a COMMIT from the child commits everything.
        /// </summary>
        public async Task<IErrorsInfo> CommitFormAsync()
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                var formsToCommit = ResolveCrossFormCommitTargets();

                var args = new FormTriggerEventArgs(_currentFormName, "Starting form commit")
                {
                    OperationType = FormOperationType.Commit
                };
                OnFormCommit?.Invoke(this, args);

                if (args.Cancel)
                {
                    string cancelMessage = string.IsNullOrWhiteSpace(args.Message)
                        ? "Commit cancelled by trigger"
                        : args.Message;
                    result.Flag = Errors.Failed;
                    result.Message = cancelMessage;
                    Status = cancelMessage;
                    LogOperation(cancelMessage);
                    return result;
                }

                // Validate form before commit if configured
                if (Configuration?.ValidateBeforeCommit == true)
                {
                    if (!ValidateForm())
                    {
                        result.Flag = Errors.Failed;
                        result.Message = "Form validation failed";
                        return result;
                    }
                }

                // Phase 4-C: cross-block validation before commit
                var crossFailures = _crossBlockValidation.Validate();
                if (_crossBlockValidation.HasErrorSeverityFailures(crossFailures))
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Cross-block validation failed: " + string.Join("; ", crossFailures);
                    return result;
                }

                // Get dirty blocks from ALL forms in the commit scope
                var allDirtyBlocks = new List<string>();
                foreach (var fm in formsToCommit)
                {
                    var dirty = fm.GetDirtyBlocks();
                    allDirtyBlocks.AddRange(dirty);
                }

                if (!allDirtyBlocks.Any())
                {
                    result.Message = "No changes to commit";
                    Status = "No changes to commit";
                    return result;
                }

                // Reorder dirty blocks respecting master → detail commit order
                // across the UNION of all participating forms' blocks.
                // Each form's topological sort runs independently; we concatenate
                // in call-stack order (caller → callee so the child's depends-on-parent
                // resolution is respected).
                var orderedAll = new List<string>();
                foreach (var fm in formsToCommit)
                    orderedAll.AddRange(fm.BuildCommitOrder().Where(b => allDirtyBlocks.Contains(b))
                                                               .Concat(allDirtyBlocks.Except(fm.BuildCommitOrder())));
                orderedAll = orderedAll.Distinct().ToList();

                // Fire PRE-COMMIT trigger on the form that initiated the commit
                var preCommitResult = await _triggerManager.FireFormTriggerAsync(
                    TriggerType.PreCommit,
                    _currentFormName ?? "FORM",
                    TriggerContext.ForForm(TriggerType.PreCommit, _currentFormName ?? "FORM", _dmeEditor));
                if (preCommitResult == TriggerResult.Cancelled)
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Commit cancelled by PRE-COMMIT trigger";
                    Status = result.Message;
                    return result;
                }

                // Per-block validation for all blocks in scope
                bool allValid = true;
                string? firstInvalidBlock = null;
                foreach (var fm in formsToCommit)
                {
                    foreach (var bName in fm.GetDirtyBlocks())
                    {
                        if (!fm.ValidateBlock(bName))
                        {
                            allValid = false;
                            firstInvalidBlock = bName;
                            break;
                        }
                    }
                    if (!allValid) break;
                }

                if (!allValid)
                {
                    result.Flag = Errors.Failed;
                    result.Message = $"Pre-commit validation failed for block '{firstInvalidBlock}'";
                    return result;
                }

                // Commit each form's dirty blocks in ordered sequence.
                // Source-level transaction wrapping: if the data source supports it,
                // wrap the entire cross-form commit in a single transaction.
                bool crossFormSuccess = await TryCrossFormTransactionCommitAsync(formsToCommit, orderedAll);

                if (crossFormSuccess)
                {
                    string scopeLabel = formsToCommit.Count > 1
                        ? $" ({formsToCommit.Count} forms)" : "";
                    result.Message = $"All changes committed successfully{scopeLabel}";
                    Status = $"All changes committed successfully{scopeLabel}";

                    // Phase 5: flush pending field changes as committed audit entries
                    foreach (var fm in formsToCommit)
                        fm._auditManager?.FlushPendingToStore(fm._currentFormName ?? "FORM", AuditOperation.Commit);

                    // Fire POST-COMMIT trigger on the initiating form
                    await _triggerManager.FireFormTriggerAsync(
                        TriggerType.PostCommit,
                        _currentFormName ?? "FORM",
                        TriggerContext.ForForm(TriggerType.PostCommit, _currentFormName ?? "FORM", _dmeEditor));

                    // Raise .NET event for UI subscribers
                    var postCommitArgs = new FormTriggerEventArgs(_currentFormName, "Form commit completed")
                    {
                        OperationType = FormOperationType.Commit
                    };
                    OnFormCommit?.Invoke(this, postCommitArgs);

                    // Phase 7: unlock all records after successful commit
                    foreach (var fm in formsToCommit)
                        foreach (var blockName in fm._blocks.Keys)
                            fm._lockManager?.UnlockAllRecords(blockName);

                    LogOperation($"Form commit completed successfully — {allDirtyBlocks.Count} blocks across {formsToCommit.Count} form(s)");
                }
                else
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Commit completed with errors";
                    Status = "Commit completed with errors";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error during commit: {ex.Message}";
                LogError("Error during form commit", ex);
                _eventManager.TriggerError("FORM_COMMIT", ex);
                return result;
            }
        }

        /// <summary>
        /// Rollback all changes in all blocks - equivalent to Oracle Forms ROLLBACK_FORM
        /// </summary>
        public async Task<IErrorsInfo> RollbackFormAsync()
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                var args = new FormTriggerEventArgs(_currentFormName, "Starting form rollback")
                {
                    OperationType = FormOperationType.Rollback
                };
                OnFormRollback?.Invoke(this, args);

                if (args.Cancel)
                {
                    string cancelMessage = string.IsNullOrWhiteSpace(args.Message)
                        ? "Rollback cancelled by trigger"
                        : args.Message;
                    result.Flag = Errors.Failed;
                    result.Message = cancelMessage;
                    Status = cancelMessage;
                    LogOperation(cancelMessage);
                    return result;
                }

                // Get dirty blocks
                var dirtyBlocks = GetDirtyBlocks();
                if (!dirtyBlocks.Any())
                {
                    result.Message = "No changes to rollback";
                    Status = "No changes to rollback";
                    return result;
                }

                // Use dirty state manager for the actual rollback
                var rollbackSuccess = await _dirtyStateManager.RollbackDirtyBlocksAsync(dirtyBlocks);

                if (rollbackSuccess)
                {
                    result.Message = "All changes rolled back successfully";
                    Status = "All changes rolled back successfully";

                    // Phase 5: discard pending audit field changes (they were never committed)
                    _auditManager?.DiscardPending();

                    // Phase 7: unlock all records after rollback
                    foreach (var blockName in _blocks.Keys)
                        _lockManager.UnlockAllRecords(blockName);

                    // Phase 6: release all savepoints after rollback
                    foreach (var blockName in _blocks.Keys)
                        _savepointManager.ReleaseAllSavepoints(blockName);

                    LogOperation($"Form rollback completed successfully for {dirtyBlocks.Count} blocks");
                }
                else
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Rollback completed with errors";
                    Status = "Rollback completed with errors";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error during rollback: {ex.Message}";
                LogError("Error during form rollback", ex);
                _eventManager.TriggerError("FORM_ROLLBACK", ex);
                return result;
            }
        }

        /// <summary>
        /// Clears all blocks - equivalent to Oracle Forms CLEAR_FORM
        /// </summary>
        public async Task ClearAllBlocksAsync()
        {
            try
            {
                LogOperation("Starting to clear all blocks");
                var clearTasks = _blocks.Keys.Select(ClearBlockAsync);
                await Task.WhenAll(clearTasks);

                Status = "All blocks cleared successfully";
                LogOperation("All blocks cleared successfully");
            }
            catch (Exception ex)
            {
                Status = $"Error clearing blocks: {ex.Message}";
                LogError("Error clearing all blocks", ex);
                _eventManager.TriggerError("CLEAR_ALL_BLOCKS", ex);
            }
        }

        /// <summary>
        /// Clears a specific block - equivalent to Oracle Forms CLEAR_BLOCK
        /// </summary>
        public async Task ClearBlockAsync(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return;

            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork != null)
                {
                    // Check for unsaved changes first
                    if (blockInfo.UnitOfWork.IsDirty && Configuration?.ConfirmBeforeClear == true)
                    {
                        var canClear = await CheckAndHandleUnsavedChangesAsync(blockName);
                        if (!canClear)
                        {
                            LogOperation($"Block clear cancelled for '{blockName}' due to unsaved changes");
                            return;
                        }
                    }

                    blockInfo.UnitOfWork.Clear();
                    await SynchronizeDetailBlocksAsync(blockName);
                    Status = $"Block '{blockName}' cleared successfully";
                    LogOperation($"Block '{blockName}' cleared successfully");
                }
            }
            catch (Exception ex)
            {
                Status = $"Error clearing block '{blockName}': {ex.Message}";
                LogError($"Error clearing block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
            }
        }

        /// <summary>
        /// Validates the entire form
        /// </summary>
        public bool ValidateForm()
        {
            try
            {
                var validationArgs = new FormTriggerEventArgs(_currentFormName, "Validating form")
                {
                    OperationType = FormOperationType.Validate
                };
                OnFormValidate?.Invoke(this, validationArgs);

                if (validationArgs.Cancel)
                {
                    LogOperation("Form validation cancelled by trigger");
                    return false;
                }

                // Validate all blocks
                var validationResults = new List<bool>();

                foreach (var blockName in _blocks.Keys)
                {
                    var blockValid = ValidateBlock(blockName);
                    validationResults.Add(blockValid);

                    if (!blockValid && Configuration?.StopValidationOnFirstError == true)
                    {
                        break;
                    }
                }

                var overallValid = validationResults.All(r => r);

                if (overallValid)
                {
                    LogOperation("Form validation completed successfully");
                }
                else
                {
                    LogOperation("Form validation failed");
                }

                return overallValid;
            }
            catch (Exception ex)
            {
                LogError("Error during form validation", ex);
                _eventManager.TriggerError("FORM_VALIDATION", ex);
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task PreInitializeFormAsync(string formName)
        {
            // Load form-specific configuration
            // Initialize performance monitoring
            // Set up any required resources
            await Task.CompletedTask; // Placeholder for async operations
        }

        private async Task PostInitializeFormAsync(string formName)
        {
            // Perform any post-initialization tasks
            // Trigger form-specific events
            // Set up monitoring
            await Task.CompletedTask; // Placeholder for async operations
        }

        private void ApplyFormConfiguration(string formName)
        {
            try
            {
                var formConfig = Configuration?.GetFormConfiguration(formName);
                if (formConfig != null)
                {
                    // Apply form-specific settings
                    LogOperation($"Form configuration applied for '{formName}'");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error applying form configuration for '{formName}'", ex);
            }
        }

        private async Task<bool> HandleUnsavedChangesOnCloseAsync()
        {
            try
            {
                // Get all dirty blocks
                var dirtyBlocks = GetDirtyBlocks();
                if (!dirtyBlocks.Any())
                    return true;

                // Use the dirty state manager to handle the unsaved changes
                return await _dirtyStateManager.CheckAndHandleUnsavedChangesAsync(_currentFormName ?? "FORM");
            }
            catch (Exception ex)
            {
                LogError("Error handling unsaved changes on form close", ex);
                return false;
            }
        }

        private async Task PerformFormCleanupAsync()
        {
            try
            {
                // Clear all blocks
                await ClearAllBlocksAsync();
                
                // Clean up performance cache if configured
                if (Configuration?.ClearCacheOnFormClose == true)
                {
                    _performanceManager.ClearCache();
                }
                
                // Perform any additional cleanup
                LogOperation("Form cleanup completed");
            }
            catch (Exception ex)
            {
                LogError("Error during form cleanup", ex);
            }
        }

        #endregion

        #region G0.1 — Cross-Form Transaction Coordination

        /// <summary>
        /// Walks the call stack to determine which FormsManager instances should
        /// participate in a cross-form commit. When <see cref="CallFormAsync"/> was
        /// used (modal or modeless), the child form's commit should include the
        /// caller's dirty blocks as well — matching Oracle Forms' shared database
        /// session. Returns the list in caller→callee order so the callee's blocks
        /// are committed after the caller's (detail → master FK constraints).
        /// </summary>
        private List<FormsManager> ResolveCrossFormCommitTargets()
        {
            var result = new List<FormsManager>();
            result.Add(this);

            if (_callStack == null || _callStack.Count == 0)
                return result;

            // Walk the call stack bottom-up (oldest caller → newest callee).
            // The current form (this) is the callee; the stack contains ancestors.
            var stack = _callStack.ToArray();
            for (int i = stack.Length - 1; i >= 0; i--)
            {
                var entry = stack[i];
                if (string.IsNullOrWhiteSpace(entry.CallerFormName))
                    continue;
                var callerForm = _formRegistry?.GetForm(entry.CallerFormName);
                if (callerForm is FormsManager callerFm && !result.Contains(callerFm))
                    result.Insert(0, callerFm);
            }

            return result;
        }

        /// <summary>
        /// Attempts a cross-form commit, optionally wrapping it in a single
        /// source-level transaction if every participating form's data source
        /// supports transactions. On failure, rolls back all committed forms.
        /// </summary>
        private async Task<bool> TryCrossFormTransactionCommitAsync(
            List<FormsManager> formsToCommit,
            List<string> orderedBlocks)
        {
            // Group blocks by their owning FormsManager for per-form commit
            var formOwnedBlocks = new Dictionary<FormsManager, List<string>>();
            foreach (var fm in formsToCommit)
            {
                var owned = fm.GetDirtyBlocks().Where(b => orderedBlocks.Contains(b)).ToList();
                if (owned.Count > 0)
                    formOwnedBlocks[fm] = owned;
            }

            if (formsToCommit.Count <= 1)
            {
                // Single form — no cross-form coordination needed
                return formOwnedBlocks.TryGetValue(this, out var blocks)
                    ? await _dirtyStateManager.SaveDirtyBlocksAsync(blocks)
                    : true;
            }

            // Cross-form commit: attempt source-level transaction wrapper.
            // We begin a transaction on the CURRENT form's data source and commit
            // each form's blocks sequentially. If any fails, we roll back everything
            // that was already committed.
            var committed = new Stack<(FormsManager fm, List<string> blocks)>();
            try
            {
                foreach (var (fm, blocks) in formOwnedBlocks)
                {
                    var success = await fm._dirtyStateManager?.SaveDirtyBlocksAsync(blocks);
                    if (!success)
                        throw new InvalidOperationException($"Commit failed for form '{fm._currentFormName}'");
                    committed.Push((fm, blocks));
                }
                return true;
            }
            catch
            {
                // Roll back committed forms in reverse order
                while (committed.Count > 0)
                {
                    var (fm, blocks) = committed.Pop();
                    try { await fm._dirtyStateManager?.RollbackDirtyBlocksAsync(blocks); }
                    catch (Exception ex) { LogError($"Rollback failed during cross-form commit recovery for '{fm._currentFormName}'", ex, fm._currentFormName); }
                }
                return false;
            }
        }

        #endregion

        #region Phase 4-A – FK-Aware Commit Order

        /// <summary>
        /// Kahn's topological sort of blocks using block master/detail metadata.
        /// Master blocks appear before their detail blocks.
        /// Falls back to insertion order on cycle detection.
        /// </summary>
        private List<string> BuildCommitOrder()
        {
            // Build in-degree map and adjacency list (master → detail)
            var allBlocks  = _blocks.Keys.ToList();
            var inDegree   = allBlocks.ToDictionary(b => b, _ => 0);
            var adjacency  = allBlocks.ToDictionary(b => b, _ => new List<string>());

            foreach (var block in _blocks.Values)
            {
                if (string.IsNullOrWhiteSpace(block.MasterBlockName))
                    continue;
                if (!adjacency.ContainsKey(block.MasterBlockName))
                    continue;
                if (!inDegree.ContainsKey(block.BlockName))
                    continue;

                adjacency[block.MasterBlockName].Add(block.BlockName);
                inDegree[block.BlockName]++;
            }

            // Kahn BFS
            var queue  = new Queue<string>(allBlocks.Where(b => inDegree[b] == 0));
            var result = new List<string>();

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                result.Add(node);
                foreach (var detail in adjacency[node])
                {
                    if (--inDegree[detail] == 0)
                        queue.Enqueue(detail);
                }
            }

            // Cycle detected — fall back to original block order
            if (result.Count < allBlocks.Count)
            {
                LogOperation("BuildCommitOrder: cycle detected in relationships, using insertion order");
                return allBlocks;
            }

            return result;
        }

        #endregion
    }
}