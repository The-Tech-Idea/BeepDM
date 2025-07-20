using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.ComponentModel;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// UnitofWorksManager simulates Oracle Forms environment with master-detail relationships,
    /// triggers, and form-level operations management. This is a higher-level manager that
    /// visual controls like BeepDataBlock will use.
    /// </summary>
    public class UnitofWorksManager : IDisposable
    {
        #region "Fields"
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks = new();
        private readonly ConcurrentDictionary<string, List<DataBlockRelationship>> _relationships = new();
        private readonly object _lockObject = new object();
        private bool _disposed;
        private string _currentFormName;
        private string _currentBlockName;
        #endregion "Fields"

        #region "Properties"
        /// <summary>Gets the DME Editor instance</summary>
        public IDMEEditor DMEEditor => _dmeEditor;

        /// <summary>Gets or sets the current form name</summary>
        public string CurrentFormName 
        { 
            get => _currentFormName; 
            set => _currentFormName = value; 
        }

        /// <summary>Gets or sets the current active block name</summary>
        public string CurrentBlockName
        {
            get => _currentBlockName;
            set => _currentBlockName = value;
        }

        /// <summary>Gets all registered blocks</summary>
        public IReadOnlyDictionary<string, DataBlockInfo> Blocks => _blocks;

        /// <summary>Gets whether any block has unsaved changes</summary>
        public bool IsDirty => _blocks.Values.Any(block => block.UnitOfWork?.IsDirty == true);

        /// <summary>Gets the current status message</summary>
        public string Status { get; private set; } = "Ready";

        /// <summary>Gets the count of registered blocks</summary>
        public int BlockCount => _blocks.Count;
        #endregion "Properties"

        #region "Events - Oracle Forms Style Triggers"
        // Form-level triggers
        public event EventHandler<FormTriggerEventArgs> OnFormOpen;
        public event EventHandler<FormTriggerEventArgs> OnFormClose;
        public event EventHandler<FormTriggerEventArgs> OnFormCommit;
        public event EventHandler<FormTriggerEventArgs> OnFormRollback;
        public event EventHandler<FormTriggerEventArgs> OnFormValidate;

        // Block-level triggers
        public event EventHandler<BlockTriggerEventArgs> OnBlockEnter;
        public event EventHandler<BlockTriggerEventArgs> OnBlockLeave;
        public event EventHandler<BlockTriggerEventArgs> OnBlockClear;
        public event EventHandler<BlockTriggerEventArgs> OnBlockValidate;

        // Record-level triggers
        public event EventHandler<RecordTriggerEventArgs> OnRecordEnter;
        public event EventHandler<RecordTriggerEventArgs> OnRecordLeave;
        public event EventHandler<RecordTriggerEventArgs> OnRecordValidate;

        // DML triggers
        public event EventHandler<DMLTriggerEventArgs> OnPreQuery;
        public event EventHandler<DMLTriggerEventArgs> OnPostQuery;
        public event EventHandler<DMLTriggerEventArgs> OnPreInsert;
        public event EventHandler<DMLTriggerEventArgs> OnPostInsert;
        public event EventHandler<DMLTriggerEventArgs> OnPreUpdate;
        public event EventHandler<DMLTriggerEventArgs> OnPostUpdate;
        public event EventHandler<DMLTriggerEventArgs> OnPreDelete;
        public event EventHandler<DMLTriggerEventArgs> OnPostDelete;
        public event EventHandler<DMLTriggerEventArgs> OnPreCommit;
        public event EventHandler<DMLTriggerEventArgs> OnPostCommit;

        // Navigation triggers
        public event EventHandler<NavigationTriggerEventArgs> OnNavigate;
        public event EventHandler<NavigationTriggerEventArgs> OnCurrentChanged;

        // Validation triggers
        public event EventHandler<ValidationTriggerEventArgs> OnValidateField;
        public event EventHandler<ValidationTriggerEventArgs> OnValidateRecord;
        public event EventHandler<ValidationTriggerEventArgs> OnValidateForm;

        // Error handling
        public event EventHandler<ErrorTriggerEventArgs> OnError;
        #endregion "Events"

        #region "Constructors"
        public UnitofWorksManager(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }
        #endregion "Constructors"

        #region "Block Registration and Management"
        /// <summary>
        /// Registers a data block with the manager
        /// </summary>
        public void RegisterBlock(string blockName, IUnitofWork unitOfWork, IEntityStructure entityStructure, 
            string dataSourceName = null, bool isMasterBlock = false)
        {
            try
            {
                var blockInfo = new DataBlockInfo
                {
                    BlockName = blockName,
                    UnitOfWork = unitOfWork,
                    EntityStructure = entityStructure,
                    DataSourceName = dataSourceName ?? "Unknown",
                    IsMasterBlock = isMasterBlock,
                    Mode = DataBlockMode.Query,
                    IsRegistered = true
                };

                _blocks[blockName] = blockInfo;

                // Subscribe to unit of work events
                if (unitOfWork != null)
                {
                    SubscribeToUnitOfWorkEvents(unitOfWork, blockName);
                }

                Status = $"Block '{blockName}' registered successfully";

                // Trigger block enter event
                TriggerBlockEnter(blockName);
            }
            catch (Exception ex)
            {
                Status = $"Error registering block '{blockName}': {ex.Message}";
                TriggerError(blockName, ex);
            }
        }

        /// <summary>
        /// Unregisters a data block from the manager
        /// </summary>
        public bool UnregisterBlock(string blockName)
        {
            try
            {
                if (!_blocks.TryGetValue(blockName, out var blockInfo))
                    return false;

                // Trigger block leave event
                TriggerBlockLeave(blockName);

                // Remove relationships involving this block
                RemoveBlockRelationships(blockName);

                // Unsubscribe from events
                if (blockInfo.UnitOfWork != null)
                {
                    UnsubscribeFromUnitOfWorkEvents(blockInfo.UnitOfWork, blockName);
                }

                // Remove the block
                _blocks.TryRemove(blockName, out _);

                Status = $"Block '{blockName}' unregistered successfully";
                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error unregistering block '{blockName}': {ex.Message}";
                TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a registered block
        /// </summary>
        public DataBlockInfo GetBlock(string blockName)
        {
            _blocks.TryGetValue(blockName, out var block);
            return block;
        }

        /// <summary>
        /// Gets the unit of work for a specific block
        /// </summary>
        public IUnitofWork GetUnitOfWork(string blockName)
        {
            return GetBlock(blockName)?.UnitOfWork;
        }
        #endregion "Block Registration and Management"

        #region "Relationship Management"
        /// <summary>
        /// Creates a master-detail relationship between blocks
        /// </summary>
        public void CreateMasterDetailRelation(string masterBlockName, string detailBlockName, 
            string masterKeyField, string detailForeignKeyField, RelationshipType relationshipType = RelationshipType.OneToMany)
        {
            try
            {
                var relationship = new DataBlockRelationship
                {
                    MasterBlockName = masterBlockName,
                    DetailBlockName = detailBlockName,
                    MasterKeyField = masterKeyField,
                    DetailForeignKeyField = detailForeignKeyField,
                    RelationshipType = relationshipType
                };

                if (!_relationships.ContainsKey(masterBlockName))
                    _relationships[masterBlockName] = new List<DataBlockRelationship>();

                _relationships[masterBlockName].Add(relationship);

                // Update detail block info
                if (_blocks.TryGetValue(detailBlockName, out var detailBlock))
                {
                    detailBlock.MasterBlockName = masterBlockName;
                    detailBlock.MasterKeyField = masterKeyField;
                    detailBlock.ForeignKeyField = detailForeignKeyField;
                    detailBlock.IsMasterBlock = false;
                }

                Status = $"Relationship created: {masterBlockName}.{masterKeyField} -> {detailBlockName}.{detailForeignKeyField}";
            }
            catch (Exception ex)
            {
                Status = $"Error creating relationship: {ex.Message}";
                TriggerError(masterBlockName, ex);
            }
        }

        /// <summary>
        /// Removes all relationships for a block
        /// </summary>
        private void RemoveBlockRelationships(string blockName)
        {
            _relationships.TryRemove(blockName, out _);
            
            // Remove as detail block from other relationships
            foreach (var kvp in _relationships.ToList())
            {
                var relationships = kvp.Value;
                var toRemove = relationships.Where(r => r.DetailBlockName == blockName).ToList();
                foreach (var rel in toRemove)
                {
                    relationships.Remove(rel);
                }
            }
        }

        /// <summary>
        /// Synchronizes detail blocks when master record changes
        /// </summary>
        public async Task SynchronizeDetailBlocksAsync(string masterBlockName)
        {
            try
            {
                if (!_relationships.TryGetValue(masterBlockName, out var relationships))
                    return;

                var masterBlock = GetBlock(masterBlockName);
                if (masterBlock?.UnitOfWork?.Units.Current == null)
                    return;

                foreach (var relationship in relationships)
                {
                    var detailBlock = GetBlock(relationship.DetailBlockName);
                    if (detailBlock?.UnitOfWork == null)
                        continue;

                    // Get master key value
                    var masterValue = GetPropertyValue(masterBlock.UnitOfWork.Units.Current, relationship.MasterKeyField);
                    
                    if (masterValue != null)
                    {
                        // Apply filter to detail block
                        var filters = new List<AppFilter>
                        {
                            new AppFilter
                            {
                                FieldName = relationship.DetailForeignKeyField,
                                Operator = "=",
                                FilterValue = masterValue.ToString()
                            }
                        };

                        // Execute query on detail block
                        await ExecuteQueryOnBlock(relationship.DetailBlockName, filters);
                    }
                    else
                    {
                        // Clear detail block if no master value
                        detailBlock.UnitOfWork.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("UnitofWorksManager", $"Error synchronizing detail blocks for {masterBlockName}: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
                TriggerError(masterBlockName, ex);
            }
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName, 
                    System.Reflection.BindingFlags.IgnoreCase | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                return property?.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }
        #endregion "Relationship Management"

        #region "Form-Level Operations"
        /// <summary>
        /// Opens a form - equivalent to Oracle Forms WHEN-NEW-FORM-INSTANCE
        /// </summary>
        public async Task<bool> OpenFormAsync(string formName)
        {
            try
            {
                var args = new FormTriggerEventArgs(formName, "Opening form");
                OnFormOpen?.Invoke(this, args);
                
                if (args.Cancel)
                {
                    Status = "Form open cancelled by trigger";
                    return false;
                }

                _currentFormName = formName;
                Status = $"Form '{formName}' opened successfully";
                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error opening form '{formName}': {ex.Message}";
                TriggerError(formName, ex);
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
                if (IsDirty)
                {
                    var args = new FormTriggerEventArgs(_currentFormName, "Form has unsaved changes");
                    OnFormClose?.Invoke(this, args);
                    
                    if (args.Cancel)
                    {
                        Status = "Form close cancelled - unsaved changes";
                        return false;
                    }
                }

                var closeArgs = new FormTriggerEventArgs(_currentFormName, "Closing form");
                OnFormClose?.Invoke(this, closeArgs);

                if (!closeArgs.Cancel)
                {
                    // Clear all blocks
                    await ClearAllBlocksAsync();
                    _currentFormName = null;
                    _currentBlockName = null;
                    Status = "Form closed successfully";
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Status = $"Error closing form: {ex.Message}";
                TriggerError(_currentFormName, ex);
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
                var args = new FormTriggerEventArgs(_currentFormName, "Starting form commit");
                OnFormCommit?.Invoke(this, args);
                
                if (args.Cancel)
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Commit cancelled by trigger";
                    return result;
                }

                // Trigger pre-commit events
                var preCommitArgs = new DMLTriggerEventArgs("ALL", _currentFormName, DMLOperation.Commit);
                OnPreCommit?.Invoke(this, preCommitArgs);

                if (preCommitArgs.Cancel)
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Commit cancelled by pre-commit trigger";
                    return result;
                }

                // Commit all dirty blocks
                foreach (var kvp in _blocks)
                {
                    var blockInfo = kvp.Value;
                    if (blockInfo.UnitOfWork?.IsDirty == true)
                    {
                        var commitResult = await blockInfo.UnitOfWork.Commit();
                        if (commitResult.Flag != Errors.Ok)
                        {
                            result.Flag = Errors.Failed;
                            result.Message += $"Block '{kvp.Key}': {commitResult.Message}; ";
                        }
                    }
                }

                // Trigger post-commit events
                var postCommitArgs = new DMLTriggerEventArgs("ALL", _currentFormName, DMLOperation.Commit);
                OnPostCommit?.Invoke(this, postCommitArgs);

                Status = result.Flag == Errors.Ok ? "All changes committed successfully" : "Commit completed with errors";
                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error during commit: {ex.Message}";
                TriggerError("ALL", ex);
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
                var args = new FormTriggerEventArgs(_currentFormName, "Starting form rollback");
                OnFormRollback?.Invoke(this, args);
                
                if (args.Cancel)
                {
                    result.Flag = Errors.Failed;
                    result.Message = "Rollback cancelled by trigger";
                    return result;
                }

                // Rollback all dirty blocks
                foreach (var kvp in _blocks)
                {
                    var blockInfo = kvp.Value;
                    if (blockInfo.UnitOfWork?.IsDirty == true)
                    {
                        var rollbackResult = await blockInfo.UnitOfWork.Rollback();
                        if (rollbackResult.Flag != Errors.Ok)
                        {
                            result.Flag = Errors.Failed;
                            result.Message += $"Block '{kvp.Key}': {rollbackResult.Message}; ";
                        }
                    }
                }

                Status = result.Flag == Errors.Ok ? "All changes rolled back successfully" : "Rollback completed with errors";
                return result;
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
                result.Ex = ex;
                Status = $"Error during rollback: {ex.Message}";
                TriggerError("ALL", ex);
                return result;
            }
        }

        /// <summary>
        /// Clears all blocks - equivalent to Oracle Forms CLEAR_FORM
        /// </summary>
        public async Task ClearAllBlocksAsync()
        {
            foreach (var kvp in _blocks)
            {
                await ClearBlockAsync(kvp.Key);
            }
        }

        /// <summary>
        /// Clears a specific block - equivalent to Oracle Forms CLEAR_BLOCK
        /// </summary>
        public async Task ClearBlockAsync(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork != null)
                {
                    var args = new BlockTriggerEventArgs(blockName, _currentFormName, "Clearing block");
                    OnBlockClear?.Invoke(this, args);
                    
                    if (!args.Cancel)
                    {
                        blockInfo.UnitOfWork.Clear();
                        Status = $"Block '{blockName}' cleared successfully";
                    }
                }
            }
            catch (Exception ex)
            {
                Status = $"Error clearing block '{blockName}': {ex.Message}";
                TriggerError(blockName, ex);
            }
        }
        #endregion "Form-Level Operations"

        #region "Navigation Operations"
        /// <summary>
        /// Navigates to first record in block
        /// </summary>
        public async Task<bool> FirstRecordAsync(string blockName)
        {
            // Check for unsaved changes before navigation
            if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                return false;
                
            return await NavigateAsync(blockName, NavigationType.First);
        }

        /// <summary>
        /// Navigates to next record in block
        /// </summary>
        public async Task<bool> NextRecordAsync(string blockName)
        {
            // Check for unsaved changes before navigation
            if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                return false;
                
            return await NavigateAsync(blockName, NavigationType.Next);
        }

        /// <summary>
        /// Navigates to previous record in block
        /// </summary>
        public async Task<bool> PreviousRecordAsync(string blockName)
        {
            // Check for unsaved changes before navigation
            if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                return false;
                
            return await NavigateAsync(blockName, NavigationType.Previous);
        }

        /// <summary>
        /// Navigates to last record in block
        /// </summary>
        public async Task<bool> LastRecordAsync(string blockName)
        {
            // Check for unsaved changes before navigation
            if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                return false;
                
            return await NavigateAsync(blockName, NavigationType.Last);
        }

        private async Task<bool> NavigateAsync(string blockName, NavigationType navigationType)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                    return false;

                var args = new NavigationTriggerEventArgs(blockName, _currentFormName, navigationType);
                OnNavigate?.Invoke(this, args);
                
                if (args.Cancel)
                    return false;

                switch (navigationType)
                {
                    case NavigationType.First:
                        blockInfo.UnitOfWork.MoveFirst();
                        break;
                    case NavigationType.Next:
                        blockInfo.UnitOfWork.MoveNext();
                        break;
                    case NavigationType.Previous:
                        blockInfo.UnitOfWork.MovePrevious();
                        break;
                    case NavigationType.Last:
                        blockInfo.UnitOfWork.MoveLast();
                        break;
                }

                await SynchronizeDetailBlocksAsync(blockName);
                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error navigating in block '{blockName}': {ex.Message}";
                TriggerError(blockName, ex);
                return false;
            }
        }
        #endregion "Navigation Operations"

        #region "Block Operations with Dirty Check"
        /// <summary>
        /// Switches to a different block, checking for unsaved changes first
        /// </summary>
        public async Task<bool> SwitchToBlockAsync(string blockName)
        {
            try
            {
                // Check for unsaved changes in current block and its children
                if (!string.IsNullOrEmpty(_currentBlockName) && _currentBlockName != blockName)
                {
                    if (!await CheckAndHandleUnsavedChangesAsync(_currentBlockName))
                        return false;
                }

                var blockInfo = GetBlock(blockName);
                if (blockInfo == null)
                {
                    Status = $"Block '{blockName}' not found";
                    return false;
                }

                // Trigger block leave for current block
                if (!string.IsNullOrEmpty(_currentBlockName) && _currentBlockName != blockName)
                {
                    TriggerBlockLeave(_currentBlockName);
                }

                // Set new current block
                _currentBlockName = blockName;

                // Trigger block enter for new block
                TriggerBlockEnter(blockName);

                Status = $"Switched to block '{blockName}'";
                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error switching to block '{blockName}': {ex.Message}";
                TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Inserts a new record in the specified block
        /// </summary>
        public async Task<bool> InsertRecordAsync(string blockName, object record = null)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                {
                    Status = $"Block '{blockName}' not found or has no unit of work";
                    return false;
                }

                // Check for unsaved changes before insert
                if (!await CheckAndHandleUnsavedChangesAsync(blockName))
                    return false;

                // Create new record if not provided
                if (record == null)
                {
                    // Use reflection to create new instance of entity type
                    var entityType = blockInfo.UnitOfWork.GetType().GetGenericArguments().FirstOrDefault();
                    if (entityType != null)
                    {
                        record = Activator.CreateInstance(entityType);
                    }
                    else
                    {
                        Status = $"Cannot determine entity type for block '{blockName}'";
                        return false;
                    }
                }

                // Insert the record
                var insertMethod = blockInfo.UnitOfWork.GetType().GetMethod("InsertAsync");
                if (insertMethod != null)
                {
                    var task = (Task<IErrorsInfo>)insertMethod.Invoke(blockInfo.UnitOfWork, new object[] { record });
                    var result = await task;
                    
                    if (result.Flag == Errors.Ok)
                    {
                        Status = $"Record inserted successfully in block '{blockName}'";
                        await SynchronizeDetailBlocksAsync(blockName);
                        return true;
                    }
                    else
                    {
                        Status = $"Error inserting record: {result.Message}";
                        return false;
                    }
                }

                Status = $"InsertAsync method not found on unit of work for block '{blockName}'";
                return false;
            }
            catch (Exception ex)
            {
                Status = $"Error inserting record in block '{blockName}': {ex.Message}";
                TriggerError(blockName, ex);
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

                // Check for unsaved changes in detail blocks
                var detailBlocks = GetDetailBlocks(blockName);
                foreach (var detailBlockName in detailBlocks)
                {
                    if (!await CheckAndHandleUnsavedChangesAsync(detailBlockName))
                        return false;
                }

                // Get current record
                var currentRecord = blockInfo.UnitOfWork.CurrentItem;
                if (currentRecord == null)
                {
                    Status = $"No current record to delete in block '{blockName}'";
                    return false;
                }

                // Delete the current record
                var deleteMethod = blockInfo.UnitOfWork.GetType().GetMethod("DeleteAsync");
                if (deleteMethod != null)
                {
                    var task = (Task<IErrorsInfo>)deleteMethod.Invoke(blockInfo.UnitOfWork, new object[] { currentRecord });
                    var result = await task;
                    
                    if (result.Flag == Errors.Ok)
                    {
                        Status = $"Record deleted successfully in block '{blockName}'";
                        await SynchronizeDetailBlocksAsync(blockName);
                        return true;
                    }
                    else
                    {
                        Status = $"Error deleting record: {result.Message}";
                        return false;
                    }
                }

                Status = $"DeleteAsync method not found on unit of work for block '{blockName}'";
                return false;
            }
            catch (Exception ex)
            {
                Status = $"Error deleting record in block '{blockName}': {ex.Message}";
                TriggerError(blockName, ex);
                return false;
            }
        }
        #endregion "Block Operations with Dirty Check"

        #region "Query Operations"
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

                var args = new DMLTriggerEventArgs(blockName, _currentFormName, DMLOperation.Query);
                OnPreQuery?.Invoke(this, args);
                
                if (!args.Cancel)
                {
                    blockInfo.Mode = DataBlockMode.Query;
                    _currentBlockName = blockName;
                    Status = $"Block '{blockName}' entered query mode";
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Status = $"Error entering query mode for '{blockName}': {ex.Message}";
                TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Executes query for a block - equivalent to Oracle Forms EXECUTE_QUERY
        /// </summary>
        public async Task<bool> ExecuteQueryAsync(string blockName, List<AppFilter> filters = null)
        {
            return await ExecuteQueryOnBlock(blockName, filters);
        }

        /// <summary>
        /// Private method to execute query on a specific block
        /// </summary>
        private async Task<bool> ExecuteQueryOnBlock(string blockName, List<AppFilter> filters = null)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                    return false;

                var args = new DMLTriggerEventArgs(blockName, _currentFormName, DMLOperation.Query);
                OnPreQuery?.Invoke(this, args);
                
                if (args.Cancel)
                    return false;

                // Execute query using reflection
                if (filters != null && filters.Any())
                {
                    var getWithFilters = blockInfo.UnitOfWork.GetType().GetMethod("Get", new[] { typeof(List<AppFilter>) });
                    if (getWithFilters != null)
                    {
                        var task = (Task)getWithFilters.Invoke(blockInfo.UnitOfWork, new object[] { filters });
                        await task;
                    }
                }
                else
                {
                    var getMethod = blockInfo.UnitOfWork.GetType().GetMethod("Get", Type.EmptyTypes);
                    if (getMethod != null)
                    {
                        var task = (Task)getMethod.Invoke(blockInfo.UnitOfWork, null);
                        await task;
                    }
                }

                blockInfo.Mode = DataBlockMode.CRUD;
                
                var postArgs = new DMLTriggerEventArgs(blockName, _currentFormName, DMLOperation.Query);
                OnPostQuery?.Invoke(this, postArgs);

                Status = $"Query executed successfully for block '{blockName}'";
                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error executing query for '{blockName}': {ex.Message}";
                TriggerError(blockName, ex);
                return false;
            }
        }
        #endregion "Query Operations"

        #region "Dirty State Management"
        /// <summary>
        /// Event raised when unsaved changes are detected before a critical operation
        /// </summary>
        public event EventHandler<UnsavedChangesEventArgs> OnUnsavedChanges;

        /// <summary>
        /// Checks for unsaved changes in a block and its children, prompts user for action
        /// </summary>
        /// <param name="blockName">Name of the block to check</param>
        /// <returns>True if operation should continue, false if cancelled</returns>
        public async Task<bool> CheckAndHandleUnsavedChangesAsync(string blockName)
        {
            try
            {
                var dirtyBlocks = new List<string>();
                
                // Check the specified block
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork?.IsDirty == true)
                {
                    dirtyBlocks.Add(blockName);
                }

                // Check all detail blocks recursively
                CollectDirtyDetailBlocks(blockName, dirtyBlocks);

                // If no dirty blocks, continue
                if (dirtyBlocks.Count == 0)
                    return true;

                // Raise event to let user decide what to do
                var args = new UnsavedChangesEventArgs(blockName, dirtyBlocks);
                OnUnsavedChanges?.Invoke(this, args);

                // Handle user's choice
                switch (args.UserChoice)
                {
                    case UnsavedChangesAction.Save:
                        return await SaveDirtyBlocksAsync(dirtyBlocks);
                        
                    case UnsavedChangesAction.Discard:
                        return await RollbackDirtyBlocksAsync(dirtyBlocks);
                        
                    case UnsavedChangesAction.Cancel:
                    default:
                        Status = "Operation cancelled due to unsaved changes";
                        return false;
                }
            }
            catch (Exception ex)
            {
                Status = $"Error checking unsaved changes: {ex.Message}";
                TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Collects all dirty detail blocks recursively
        /// </summary>
        private void CollectDirtyDetailBlocks(string blockName, List<string> dirtyBlocks)
        {
            var detailBlocks = GetDetailBlocks(blockName);
            foreach (var detailBlockName in detailBlocks)
            {
                var detailBlockInfo = GetBlock(detailBlockName);
                if (detailBlockInfo?.UnitOfWork?.IsDirty == true)
                {
                    dirtyBlocks.Add(detailBlockName);
                }
                
                // Recursively check detail blocks of this detail block
                CollectDirtyDetailBlocks(detailBlockName, dirtyBlocks);
            }
        }

        /// <summary>
        /// Saves all dirty blocks
        /// </summary>
        private async Task<bool> SaveDirtyBlocksAsync(List<string> dirtyBlocks)
        {
            try
            {
                foreach (var blockName in dirtyBlocks)
                {
                    var blockInfo = GetBlock(blockName);
                    if (blockInfo?.UnitOfWork != null)
                    {
                        var result = await blockInfo.UnitOfWork.Commit();
                        if (result.Flag != Errors.Ok)
                        {
                            Status = $"Error saving block '{blockName}': {result.Message}";
                            return false;
                        }
                    }
                }
                
                Status = $"Successfully saved {dirtyBlocks.Count} dirty blocks";
                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error saving dirty blocks: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Rolls back all dirty blocks
        /// </summary>
        private async Task<bool> RollbackDirtyBlocksAsync(List<string> dirtyBlocks)
        {
            try
            {
                foreach (var blockName in dirtyBlocks)
                {
                    var blockInfo = GetBlock(blockName);
                    if (blockInfo?.UnitOfWork != null)
                    {
                        var result = await blockInfo.UnitOfWork.Rollback();
                        if (result.Flag != Errors.Ok)
                        {
                            Status = $"Error rolling back block '{blockName}': {result.Message}";
                            return false;
                        }
                    }
                }
                
                Status = $"Successfully rolled back {dirtyBlocks.Count} dirty blocks";
                return true;
            }
            catch (Exception ex)
            {
                Status = $"Error rolling back dirty blocks: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Checks if any blocks have unsaved changes
        /// </summary>
        public bool HasUnsavedChanges()
        {
            return IsDirty;
        }

        /// <summary>
        /// Gets all dirty blocks
        /// </summary>
        public List<string> GetDirtyBlocks()
        {
            return _blocks.Where(kvp => kvp.Value.UnitOfWork?.IsDirty == true)
                         .Select(kvp => kvp.Key)
                         .ToList();
        }
        #endregion "Dirty State Management"

        #region "Event Handling"
        private void SubscribeToUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName)
        {
            unitOfWork.PreInsert += (sender, e) => {
                var args = new DMLTriggerEventArgs(blockName, _currentFormName, DMLOperation.Insert, e);
                args.CurrentRecord = sender; // The sender is the record being inserted
                args.EntityStructure = GetBlock(blockName)?.EntityStructure;
                OnPreInsert?.Invoke(this, args);
                // Apply any changes back to the UnitofWorkParams
                if (args.CurrentRecord != null)
                {
                    e.Record = args.CurrentRecord;
                }
                e.Cancel = args.Cancel;
            };
            
            unitOfWork.PostInsert += (sender, e) => {
                var args = new DMLTriggerEventArgs(blockName, _currentFormName, DMLOperation.Insert, e);
                args.CurrentRecord = sender;
                args.EntityStructure = GetBlock(blockName)?.EntityStructure;
                OnPostInsert?.Invoke(this, args);
            };
            
            unitOfWork.PreUpdate += (sender, e) => {
                var args = new DMLTriggerEventArgs(blockName, _currentFormName, DMLOperation.Update, e);
                args.CurrentRecord = sender; // The sender is the record being updated
                args.EntityStructure = GetBlock(blockName)?.EntityStructure;
                OnPreUpdate?.Invoke(this, args);
                // Apply any changes back to the UnitofWorkParams
                if (args.CurrentRecord != null)
                {
                    e.Record = args.CurrentRecord;
                }
                e.Cancel = args.Cancel;
            };
            
            unitOfWork.PostUpdate += (sender, e) => {
                var args = new DMLTriggerEventArgs(blockName, _currentFormName, DMLOperation.Update, e);
                args.CurrentRecord = sender;
                args.EntityStructure = GetBlock(blockName)?.EntityStructure;
                OnPostUpdate?.Invoke(this, args);
            };
            
            unitOfWork.PreDelete += (sender, e) => {
                var args = new DMLTriggerEventArgs(blockName, _currentFormName, DMLOperation.Delete, e);
                args.CurrentRecord = sender; // The sender is the record being deleted
                args.EntityStructure = GetBlock(blockName)?.EntityStructure;
                OnPreDelete?.Invoke(this, args);
                // Note: For delete, we don't usually modify the record, but we allow cancellation
                e.Cancel = args.Cancel;
            };
            
            unitOfWork.PostDelete += (sender, e) => {
                var args = new DMLTriggerEventArgs(blockName, _currentFormName, DMLOperation.Delete, e);
                args.CurrentRecord = sender;
                args.EntityStructure = GetBlock(blockName)?.EntityStructure;
                OnPostDelete?.Invoke(this, args);
            };

            if (unitOfWork.Units != null)
            {
                unitOfWork.Units.CurrentChanged += async (sender, e) =>
                {
                    var args = new NavigationTriggerEventArgs(blockName, _currentFormName, NavigationType.CurrentChanged);
                    OnCurrentChanged?.Invoke(this, args);
                    
                    if (!args.Cancel)
                    {
                        await SynchronizeDetailBlocksAsync(blockName);
                    }
                };
            }
        }

        private void UnsubscribeFromUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName)
        {
            // Note: In a real implementation, you would need to store event handler references to properly unsubscribe
            // This is a simplified version
        }

        private void TriggerBlockEnter(string blockName)
        {
            var args = new BlockTriggerEventArgs(blockName, _currentFormName, "Block entered");
            OnBlockEnter?.Invoke(this, args);
        }

        private void TriggerBlockLeave(string blockName)
        {
            var args = new BlockTriggerEventArgs(blockName, _currentFormName, "Block leaving");
            OnBlockLeave?.Invoke(this, args);
        }

        private void TriggerError(string blockName, Exception ex)
        {
            var args = new ErrorTriggerEventArgs(blockName, _currentFormName, ex.Message, ex);
            OnError?.Invoke(this, args);
        }
        #endregion "Event Handling"

        #region "Validation"
        /// <summary>
        /// Validates a specific field in a block
        /// </summary>
        public bool ValidateField(string blockName, string fieldName, object value)
        {
            try
            {
                var args = new ValidationTriggerEventArgs(blockName, _currentFormName, fieldName, value);
                OnValidateField?.Invoke(this, args);
                return args.IsValid;
            }
            catch (Exception ex)
            {
                TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Validates all records in a block
        /// </summary>
        public bool ValidateBlock(string blockName)
        {
            try
            {
                var args = new ValidationTriggerEventArgs(blockName, _currentFormName);
                OnValidateRecord?.Invoke(this, args);
                return args.IsValid;
            }
            catch (Exception ex)
            {
                TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Validates the entire form
        /// </summary>
        public bool ValidateForm()
        {
            try
            {
                var args = new ValidationTriggerEventArgs("ALL", _currentFormName);
                OnValidateForm?.Invoke(this, args);
                return args.IsValid;
            }
            catch (Exception ex)
            {
                TriggerError("ALL", ex);
                return false;
            }
        }
        #endregion "Validation"

        #region "Utility Methods"
        /// <summary>
        /// Gets all blocks that are detail blocks of the specified master block
        /// </summary>
        public List<string> GetDetailBlocks(string masterBlockName)
        {
            if (_relationships.TryGetValue(masterBlockName, out var relationships))
            {
                return relationships.Select(r => r.DetailBlockName).ToList();
            }
            return new List<string>();
        }

        /// <summary>
        /// Gets the master block name for a detail block
        /// </summary>
        public string GetMasterBlock(string detailBlockName)
        {
            var detailBlock = GetBlock(detailBlockName);
            return detailBlock?.MasterBlockName;
        }

        /// <summary>
        /// Checks if a block exists
        /// </summary>
        public bool BlockExists(string blockName)
        {
            return _blocks.ContainsKey(blockName);
        }
        #endregion "Utility Methods"

        #region "Oracle Forms Simulation Helpers"
        /// <summary>
        /// Sets default values for common audit fields when a new record is created
        /// Similar to Oracle Forms default value triggers
        /// </summary>
        /// <param name="record">The record to set defaults on</param>
        /// <param name="currentUser">Current user name</param>
        public void SetAuditDefaults(object record, string currentUser = null)
        {
            if (record == null) return;

            var recordType = record.GetType();
            var now = DateTime.Now;

            // Common audit field patterns
            var auditFields = new Dictionary<string, object>
            {
                { "CreatedDate", now },
                { "Created_Date", now },
                { "CreateDate", now },
                { "DateCreated", now },
                { "ModifiedDate", now },
                { "Modified_Date", now },
                { "ModifyDate", now },
                { "DateModified", now },
                { "LastUpdated", now },
                { "UpdatedDate", now }
            };

            if (!string.IsNullOrEmpty(currentUser))
            {
                auditFields.Add("CreatedBy", currentUser);
                auditFields.Add("Created_By", currentUser);
                auditFields.Add("CreateUser", currentUser);
                auditFields.Add("ModifiedBy", currentUser);
                auditFields.Add("Modified_By", currentUser);
                auditFields.Add("ModifyUser", currentUser);
                auditFields.Add("LastUpdatedBy", currentUser);
                auditFields.Add("UpdatedBy", currentUser);
            }

            foreach (var field in auditFields)
            {
                SetFieldValue(record, field.Key, field.Value);
            }
        }

        /// <summary>
        /// Sets a field value on a record using reflection
        /// </summary>
        /// <param name="record">The record object</param>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if successful</returns>
        public bool SetFieldValue(object record, string fieldName, object value)
        {
            if (record == null) return false;

            try
            {
                var property = record.GetType().GetProperty(fieldName,
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (property != null && property.CanWrite)
                {
                    // Convert value to the correct type if needed
                    object convertedValue = value;
                    if (value != null && property.PropertyType != value.GetType())
                    {
                        // Handle nullable types
                        Type targetType = property.PropertyType;
                        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            targetType = Nullable.GetUnderlyingType(targetType);
                        }

                        convertedValue = Convert.ChangeType(value, targetType);
                    }

                    property.SetValue(record, convertedValue);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _dmeEditor?.AddLogMessage("UnitofWorksManager", $"Error setting field '{fieldName}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }

            return false;
        }

        /// <summary>
        /// Gets a field value from a record using reflection
        /// </summary>
        /// <param name="record">The record object</param>
        /// <param name="fieldName">Name of the field</param>
        /// <returns>Field value or null</returns>
        public object GetFieldValue(object record, string fieldName)
        {
            if (record == null) return null;

            try
            {
                var property = record.GetType().GetProperty(fieldName,
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                return property?.GetValue(record);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Executes a sequence generator for a field (Oracle sequence simulation)
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="record">The record to set the sequence value on</param>
        /// <param name="fieldName">Name of the field to set</param>
        /// <param name="sequenceName">Name of the sequence</param>
        /// <returns>True if successful</returns>
        public bool ExecuteSequence(string blockName, object record, string fieldName, string sequenceName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null) return false;

                // Try to get sequence using the UnitOfWork's GetSeq method
                var unitOfWorkType = blockInfo.UnitOfWork.GetType();
                var getSeqMethod = unitOfWorkType.GetMethod("GetSeq");
                
                if (getSeqMethod != null)
                {
                    var sequenceValue = getSeqMethod.Invoke(blockInfo.UnitOfWork, new object[] { sequenceName });
                    if (sequenceValue != null && (int)sequenceValue > 0)
                    {
                        return SetFieldValue(record, fieldName, sequenceValue);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _dmeEditor?.AddLogMessage("UnitofWorksManager", $"Error executing sequence '{sequenceName}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }
        #endregion "Oracle Forms Simulation Helpers"

        #region "Dispose"
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                // Unsubscribe from all events and dispose resources
                foreach (var blockInfo in _blocks.Values)
                {
                    if (blockInfo.UnitOfWork != null)
                    {
                        UnsubscribeFromUnitOfWorkEvents(blockInfo.UnitOfWork, blockInfo.BlockName);
                    }
                }
                
                _blocks.Clear();
                _relationships.Clear();
            }
            catch (Exception ex)
            {
                _dmeEditor?.AddLogMessage("UnitofWorksManager", $"Error during dispose: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }
            finally
            {
                _disposed = true;
            }
        }
        #endregion "Dispose"
    }

    #region "Supporting Classes and Enums"
    /// <summary>
    /// Information about a registered data block
    /// </summary>
    public class DataBlockInfo
    {
        public string BlockName { get; set; }
        public IUnitofWork UnitOfWork { get; set; }
        public IEntityStructure EntityStructure { get; set; }
        public string DataSourceName { get; set; }
        public bool IsMasterBlock { get; set; }
        public DataBlockMode Mode { get; set; } = DataBlockMode.Query;
        public string MasterBlockName { get; set; }
        public string MasterKeyField { get; set; }
        public string ForeignKeyField { get; set; }
        public bool IsRegistered { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Represents a relationship between data blocks
    /// </summary>
    public class DataBlockRelationship
    {
        public string MasterBlockName { get; set; }
        public string DetailBlockName { get; set; }
        public string MasterKeyField { get; set; }
        public string DetailForeignKeyField { get; set; }
        public RelationshipType RelationshipType { get; set; } = RelationshipType.OneToMany;
    }

    /// <summary>
    /// Data block modes similar to Oracle Forms
    /// </summary>
    public enum DataBlockMode
    {
        Query,
        CRUD
    }

    /// <summary>
    /// Types of relationships between blocks
    /// </summary>
    public enum RelationshipType
    {
        OneToOne,
        OneToMany,
        ManyToOne,
        ManyToMany
    }

    /// <summary>
    /// Navigation types for triggers
    /// </summary>
    public enum NavigationType
    {
        First,
        Next,
        Previous,
        Last,
        CurrentChanged
    }

    /// <summary>
    /// DML operation types
    /// </summary>
    public enum DMLOperation
    {
        Query,
        Insert,
        Update,
        Delete,
        Commit,
        Rollback
    }

    #region "Event Args Classes"
    public class FormTriggerEventArgs : EventArgs
    {
        public string FormName { get; }
        public string Message { get; set; }
        public bool Cancel { get; set; }

        public FormTriggerEventArgs(string formName, string message = null)
        {
            FormName = formName;
            Message = message;
        }
    }

    public class BlockTriggerEventArgs : EventArgs
    {
        public string BlockName { get; }
        public string FormName { get; }
        public string Message { get; set; }
        public bool Cancel { get; set; }

        public BlockTriggerEventArgs(string blockName, string formName, string message = null)
        {
            BlockName = blockName;
            FormName = formName;
            Message = message;
        }
    }

    public class RecordTriggerEventArgs : EventArgs
    {
        public string BlockName { get; }
        public string FormName { get; }
        public object CurrentRecord { get; }
        public string Message { get; set; }
        public bool Cancel { get; set; }

        public RecordTriggerEventArgs(string blockName, string formName, object currentRecord, string message = null)
        {
            BlockName = blockName;
            FormName = formName;
            CurrentRecord = currentRecord;
            Message = message;
        }
    }

    public class DMLTriggerEventArgs : EventArgs
    {
        public string BlockName { get; }
        public string FormName { get; }
        public DMLOperation Operation { get; }
        public UnitofWorkParams UnitOfWorkParams { get; }
        
        /// <summary>
        /// Gets or sets the current record being processed. 
        /// Can be modified in Pre-triggers to set default values or apply business logic.
        /// </summary>
        public object CurrentRecord { get; set; }
        
        /// <summary>
        /// Gets the entity structure for the current block to help with field access
        /// </summary>
        public IEntityStructure EntityStructure { get; set; }
        
        public string Message { get; set; }
        public bool Cancel { get; set; }

        public DMLTriggerEventArgs(string blockName, string formName, DMLOperation operation, UnitofWorkParams unitOfWorkParams = null)
        {
            BlockName = blockName;
            FormName = formName;
            Operation = operation;
            UnitOfWorkParams = unitOfWorkParams;
            
            // Extract the current record from UnitofWorkParams if available
            if (unitOfWorkParams?.Record != null)
            {
                CurrentRecord = unitOfWorkParams.Record;
            }
        }

        /// <summary>
        /// Sets a field value in the current record using reflection
        /// </summary>
        /// <param name="fieldName">Name of the field to set</param>
        /// <param name="value">Value to assign</param>
        public void SetFieldValue(string fieldName, object value)
        {
            if (CurrentRecord == null) return;
            
            try
            {
                var property = CurrentRecord.GetType().GetProperty(fieldName, 
                    System.Reflection.BindingFlags.IgnoreCase | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                
                if (property != null && property.CanWrite)
                {
                    // Convert value to the correct type if needed
                    object convertedValue = value;
                    if (value != null && property.PropertyType != value.GetType())
                    {
                        // Handle nullable types
                        Type targetType = property.PropertyType;
                        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            targetType = Nullable.GetUnderlyingType(targetType);
                        }
                        
                        convertedValue = Convert.ChangeType(value, targetType);
                    }
                    
                    property.SetValue(CurrentRecord, convertedValue);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking the trigger chain
                Message += $"Error setting field '{fieldName}': {ex.Message}; ";
            }
        }

        /// <summary>
        /// Gets a field value from the current record using reflection
        /// </summary>
        /// <param name="fieldName">Name of the field to get</param>
        /// <returns>Field value or null if not found</returns>
        public object GetFieldValue(string fieldName)
        {
            if (CurrentRecord == null) return null;
            
            try
            {
                var property = CurrentRecord.GetType().GetProperty(fieldName, 
                    System.Reflection.BindingFlags.IgnoreCase | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                
                return property?.GetValue(CurrentRecord);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the current date/time to a field - common Oracle Forms pattern
        /// </summary>
        /// <param name="fieldName">Name of the date field</param>
        public void SetCurrentDateTime(string fieldName)
        {
            SetFieldValue(fieldName, DateTime.Now);
        }

        /// <summary>
        /// Sets the current user to a field - common Oracle Forms pattern
        /// </summary>
        /// <param name="fieldName">Name of the user field</param>
        /// <param name="currentUser">Current user name</param>
        public void SetCurrentUser(string fieldName, string currentUser)
        {
            SetFieldValue(fieldName, currentUser);
        }

        /// <summary>
        /// Checks if a field is null or empty
        /// </summary>
        /// <param name="fieldName">Name of the field to check</param>
        /// <returns>True if field is null or empty</returns>
        public bool IsFieldNullOrEmpty(string fieldName)
        {
            var value = GetFieldValue(fieldName);
            return value == null || (value is string str && string.IsNullOrWhiteSpace(str));
        }
    }

    public class NavigationTriggerEventArgs : EventArgs
    {
        public string BlockName { get; }
        public string FormName { get; }
        public NavigationType NavigationType { get; }
        public string Message { get; set; }
        public bool Cancel { get; set; }

        public NavigationTriggerEventArgs(string blockName, string formName, NavigationType navigationType)
        {
            BlockName = blockName;
            FormName = formName;
            NavigationType = navigationType;
        }
    }

    public class ValidationTriggerEventArgs : EventArgs
    {
        public string BlockName { get; }
        public string FormName { get; }
        public string FieldName { get; set; }
        public object Value { get; set; }
        public string ValidationMessage { get; set; }
        public bool IsValid { get; set; } = true;
        public bool Cancel { get; set; }

        public ValidationTriggerEventArgs(string blockName, string formName, string fieldName = null, object value = null)
        {
            BlockName = blockName;
            FormName = formName;
            FieldName = fieldName;
            Value = value;
        }
    }

    public class ErrorTriggerEventArgs : EventArgs
    {
        public string BlockName { get; }
        public string FormName { get; }
        public string ErrorMessage { get; }
        public Exception Exception { get; }

        public ErrorTriggerEventArgs(string blockName, string formName, string errorMessage, Exception exception = null)
        {
            BlockName = blockName;
            FormName = formName;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }

    /// <summary>
    /// Event args for handling unsaved changes - Oracle Forms style
    /// </summary>
    public class UnsavedChangesEventArgs : EventArgs
    {
        public string BlockName { get; }
        public List<string> DirtyBlocks { get; }
        public UnsavedChangesAction UserChoice { get; set; } = UnsavedChangesAction.Cancel;
        public string Message { get; set; }

        public UnsavedChangesEventArgs(string blockName, List<string> dirtyBlocks)
        {
            BlockName = blockName;
            DirtyBlocks = dirtyBlocks ?? new List<string>();
            Message = $"Block '{blockName}' and {DirtyBlocks.Count} related blocks have unsaved changes.";
        }
    }

    /// <summary>
    /// Actions that can be taken when unsaved changes are detected
    /// </summary>
    public enum UnsavedChangesAction
    {
        Save,       // Save all changes and continue
        Discard,    // Discard all changes and continue  
        Cancel      // Cancel the operation
    }
    #endregion "Event Args Classes"

    #endregion "Supporting Classes and Enums"
}
