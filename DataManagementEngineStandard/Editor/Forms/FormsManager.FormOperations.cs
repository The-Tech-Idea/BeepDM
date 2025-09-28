using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Models;
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
        public event EventHandler<FormTriggerEventArgs> OnFormOpen;
        public event EventHandler<FormTriggerEventArgs> OnFormClose;
        public event EventHandler<FormTriggerEventArgs> OnFormCommit;
        public event EventHandler<FormTriggerEventArgs> OnFormRollback;
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
        /// Commits all changes in all blocks - equivalent to Oracle Forms COMMIT_FORM
        /// </summary>
        public async Task<IErrorsInfo> CommitFormAsync()
        {
            var result = new ErrorsInfo { Flag = Errors.Ok };

            try
            {
                var args = new FormTriggerEventArgs(_currentFormName, "Starting form commit")
                {
                    OperationType = FormOperationType.Commit
                };
                OnFormCommit?.Invoke(this, args);

                if (args.Cancel)
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Commit cancelled by trigger";
                    LogOperation("Form commit cancelled by trigger");
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

                // Get dirty blocks and sort by dependency
                var dirtyBlocks = GetDirtyBlocks();
                if (!dirtyBlocks.Any())
                {
                    result.Message = "No changes to commit";
                    Status = "No changes to commit";
                    return result;
                }

                // Use dirty state manager for the actual commit
                var commitSuccess = await _dirtyStateManager.SaveDirtyBlocksAsync(dirtyBlocks);

                if (commitSuccess)
                {
                    result.Message = "All changes committed successfully";
                    Status = "All changes committed successfully";

                    // Trigger post-commit events
                    var postCommitArgs = new FormTriggerEventArgs(_currentFormName, "Form commit completed")
                    {
                        OperationType = FormOperationType.Commit
                    };
                    OnFormCommit?.Invoke(this, postCommitArgs);

                    LogOperation($"Form commit completed successfully for {dirtyBlocks.Count} blocks");
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
                    result.Flag = Errors.Failed;
                    result.Message = "Rollback cancelled by trigger";
                    LogOperation("Form rollback cancelled by trigger");
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
    }
}