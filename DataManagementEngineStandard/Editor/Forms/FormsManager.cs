using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Enhanced UnitofWorksManager with modular design using helper classes.
    /// Simulates Oracle Forms environment with master-detail relationships,
    /// triggers, and form-level operations management.
    /// 
    /// This is the main coordinator class - complex operations are delegated to:
    /// - Helper classes for specific functionality
    /// - Partial classes for related operations
    /// - The main class stays lean and focused on coordination
    /// </summary>
    public partial class FormsManager : IUnitofWorksManager
    {
        #region Fields
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks = new();
        private readonly ConcurrentDictionary<string, List<DataBlockRelationship>> _relationships = new();
        
        // Helper managers
        private readonly IRelationshipManager _relationshipManager;
        private readonly IDirtyStateManager _dirtyStateManager;
        private readonly IEventManager _eventManager;
        private readonly IFormsSimulationHelper _formsSimulationHelper;
        private readonly IPerformanceManager _performanceManager;
        private readonly IConfigurationManager _configurationManager;
        
        private readonly object _lockObject = new object();
        private bool _disposed;
        private string _currentFormName;
        private string _currentBlockName;
        #endregion

        #region Properties
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

        /// <summary>Gets the relationship manager</summary>
        public IRelationshipManager RelationshipManager => _relationshipManager;

        /// <summary>Gets the dirty state manager</summary>
        public IDirtyStateManager DirtyStateManager => _dirtyStateManager;

        /// <summary>Gets the performance manager</summary>
        public IPerformanceManager PerformanceManager => _performanceManager;

        /// <summary>Gets the configuration</summary>
        public UnitofWorksManagerConfiguration Configuration => _configurationManager?.Configuration;
        #endregion

        #region Constructors
        
        /// <summary>
        /// Constructor with full dependency injection
        /// </summary>
        public FormsManager(
            IDMEEditor dmeEditor,
            IRelationshipManager relationshipManager = null,
            IDirtyStateManager dirtyStateManager = null,
            IEventManager eventManager = null,
            IFormsSimulationHelper formsSimulationHelper = null,
            IPerformanceManager performanceManager = null,
            IConfigurationManager configurationManager = null)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            
            // Initialize helper managers with defaults if not provided
            _relationshipManager = relationshipManager ?? new RelationshipManager(_dmeEditor, _blocks, _relationships);
            _dirtyStateManager = dirtyStateManager ?? new DirtyStateManager(_dmeEditor, _blocks, GetDetailBlocks, GetBlock);
            _eventManager = eventManager ?? new EventManager(_dmeEditor);
            _formsSimulationHelper = formsSimulationHelper ?? new FormsSimulationHelper(_dmeEditor, _blocks);
            _performanceManager = performanceManager ?? new PerformanceManager(_dmeEditor);
            _configurationManager = configurationManager ?? new ConfigurationManager();

            InitializeManager();
        }

        /// <summary>
        /// Simple constructor for backward compatibility
        /// </summary>
        public FormsManager(IDMEEditor dmeEditor) 
            : this(dmeEditor, null, null, null, null, null, null)
        {
        }
        #endregion

        #region Block Registration and Management

        /// <summary>
        /// Registers a data block with the manager
        /// </summary>
        public void RegisterBlock(string blockName, IUnitofWork unitOfWork, IEntityStructure entityStructure, 
            string dataSourceName = null, bool isMasterBlock = false)
        {
            ValidateBlockRegistrationParameters(blockName, unitOfWork, entityStructure);

            try
            {
                var blockInfo = CreateBlockInfo(blockName, unitOfWork, entityStructure, dataSourceName, isMasterBlock);
                
                // Register with performance manager for caching
                _performanceManager.CacheBlockInfo(blockName, blockInfo);
                
                // Store in main collection
                _blocks[blockName] = blockInfo;

                // Subscribe to unit of work events
                if (unitOfWork != null)
                {
                    _eventManager.SubscribeToUnitOfWorkEvents(unitOfWork, blockName);
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
                _relationshipManager.RemoveBlockRelationships(blockName);

                // Unsubscribe from events
                if (blockInfo.UnitOfWork != null)
                {
                    _eventManager.UnsubscribeFromUnitOfWorkEvents(blockInfo.UnitOfWork, blockName);
                }

                // Remove from collections
                _blocks.TryRemove(blockName, out _);

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

        #region Relationship Management (Delegated)

        /// <summary>
        /// Creates a master-detail relationship between blocks
        /// </summary>
        public void CreateMasterDetailRelation(string masterBlockName, string detailBlockName, 
            string masterKeyField, string detailForeignKeyField, RelationshipType relationshipType = RelationshipType.OneToMany)
        {
            _relationshipManager.CreateMasterDetailRelation(masterBlockName, detailBlockName, 
                masterKeyField, detailForeignKeyField, relationshipType);
        }

        /// <summary>
        /// Synchronizes detail blocks when master record changes
        /// </summary>
        public async Task SynchronizeDetailBlocksAsync(string masterBlockName)
        {
            await _relationshipManager.SynchronizeDetailBlocksAsync(masterBlockName);
        }

        /// <summary>
        /// Gets all blocks that are detail blocks of the specified master block
        /// </summary>
        public List<string> GetDetailBlocks(string masterBlockName)
        {
            return _relationshipManager.GetDetailBlocks(masterBlockName);
        }

        /// <summary>
        /// Gets the master block name for a detail block
        /// </summary>
        public string GetMasterBlock(string detailBlockName)
        {
            return _relationshipManager.GetMasterBlock(detailBlockName);
        }

        #endregion

        #region Dirty State Management (Delegated)

        /// <summary>
        /// Checks for unsaved changes in a block and its children, prompts user for action
        /// </summary>
        public async Task<bool> CheckAndHandleUnsavedChangesAsync(string blockName)
        {
            return await _dirtyStateManager.CheckAndHandleUnsavedChangesAsync(blockName);
        }

        /// <summary>
        /// Checks if any blocks have unsaved changes
        /// </summary>
        public bool HasUnsavedChanges()
        {
            return _dirtyStateManager.HasUnsavedChanges();
        }

        /// <summary>
        /// Gets all dirty blocks
        /// </summary>
        public List<string> GetDirtyBlocks()
        {
            return _dirtyStateManager.GetDirtyBlocks();
        }

        #endregion

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
                Status = result.Flag == Errors.Ok ? 
                    $"Record inserted successfully in block '{blockName}'" : 
                    $"Error inserting record: {result.Message}";
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

                // Delete the current record using reflection
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
        /// Executes query for a block - equivalent to Oracle Forms EXECUTE_QUERY
        /// Basic implementation - use ExecuteQueryEnhancedAsync for better functionality
        /// </summary>
        public async Task<bool> ExecuteQueryAsync(string blockName, List<AppFilter> filters = null)
        {
            try
            {
                var result = await ExecuteQueryEnhancedAsync(blockName, filters);
                Status = result.Flag == Errors.Ok ? 
                    $"Query executed successfully for block '{blockName}'" : 
                    $"Error executing query: {result.Message}";
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

        #endregion

        #region Validation (Required by Interface)

        /// <summary>
        /// Validates a specific field in a block
        /// </summary>
        public bool ValidateField(string blockName, string FieldName, object value)
        {
            try
            {
                return _eventManager.TriggerFieldValidation(blockName, FieldName, value);
            }
            catch (Exception ex)
            {
                LogError($"Error validating field '{FieldName}' in block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
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
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                    return true; // No block to validate

                // Get current record if available
                var currentRecord = blockInfo.UnitOfWork.CurrentItem;
                return _eventManager.TriggerRecordValidation(blockName, currentRecord);
            }
            catch (Exception ex)
            {
                LogError($"Error validating block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        #endregion

        #region Oracle Forms Simulation (Delegated)

        /// <summary>
        /// Sets default values for common audit fields when a new record is created
        /// </summary>
        public void SetAuditDefaults(object record, string currentUser = null)
        {
            _formsSimulationHelper.SetAuditDefaults(record, currentUser);
        }

        /// <summary>
        /// Sets a field value on a record using reflection
        /// </summary>
        public bool SetFieldValue(object record, string FieldName, object value)
        {
            return _formsSimulationHelper.SetFieldValue(record, FieldName, value);
        }

        /// <summary>
        /// Gets a field value from a record using reflection
        /// </summary>
        public object GetFieldValue(object record, string FieldName)
        {
            return _formsSimulationHelper.GetFieldValue(record, FieldName);
        }

        /// <summary>
        /// Executes a sequence generator for a field
        /// </summary>
        public bool ExecuteSequence(string blockName, object record, string FieldName, string sequenceName)
        {
            return _formsSimulationHelper.ExecuteSequence(blockName, record, FieldName, sequenceName);
        }

        #endregion

        #region IDisposable Implementation

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
                        _eventManager.UnsubscribeFromUnitOfWorkEvents(blockInfo.UnitOfWork, blockInfo.BlockName);
                    }
                }
                
                // Dispose helper managers
                _performanceManager?.Dispose();
                
                _blocks.Clear();
                _relationships.Clear();
                
                LogOperation("UnitofWorksManager disposed");
            }
            catch (Exception ex)
            {
                LogError("Error during UnitofWorksManager disposal", ex);
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion

        #region Protected Helper Methods (For Partial Classes)

        protected void InitializeManager()
        {
            try
            {
                // Load configuration
                _configurationManager.LoadConfiguration();
                
                // Subscribe to dirty state events
                _dirtyStateManager.OnUnsavedChanges += OnUnsavedChangesHandler;
                
                LogOperation("UnitofWorksManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError("Error initializing UnitofWorksManager", ex);
                throw;
            }
        }

        protected void ValidateBlockRegistrationParameters(string blockName, IUnitofWork unitOfWork, IEntityStructure entityStructure)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                throw new ArgumentException("Block name cannot be null or empty", nameof(blockName));
            
            if (unitOfWork == null)
                throw new ArgumentNullException(nameof(unitOfWork));
                
            if (entityStructure == null)
                throw new ArgumentNullException(nameof(entityStructure));
        }

        protected DataBlockInfo CreateBlockInfo(string blockName, IUnitofWork unitOfWork, 
            IEntityStructure entityStructure, string dataSourceName, bool isMasterBlock)
        {
            return new DataBlockInfo
            {
                BlockName = blockName,
                UnitOfWork = unitOfWork,
                EntityStructure = entityStructure,
                DataSourceName = dataSourceName ?? "Unknown",
                IsMasterBlock = isMasterBlock,
                Mode = DataBlockMode.Query,
                IsRegistered = true,
                RegisteredAt = DateTime.Now,
                Configuration = Configuration?.GetBlockConfiguration(blockName) ?? new BlockConfiguration()
            };
        }

        protected void ApplyBlockConfiguration(DataBlockInfo blockInfo)
        {
            var config = Configuration?.GetBlockConfiguration(blockInfo.BlockName);
            if (config != null)
            {
                blockInfo.Configuration = config;
                // Apply any specific configuration settings
            }
        }

        protected void OnUnsavedChangesHandler(object sender, UnsavedChangesEventArgs e)
        {
            // This can be overridden by derived classes or handled by event subscribers
            // Default behavior could be to show a dialog or log the event
            LogOperation($"Unsaved changes detected in block '{e.BlockName}' with {e.DirtyBlocks.Count} affected blocks");
        }

        protected void LogOperation(string message, string blockName = null)
        {
            var fullMessage = blockName != null ? $"[{blockName}] {message}" : message;
            _dmeEditor?.AddLogMessage("UnitofWorksManager", fullMessage, DateTime.Now, 0, null, Errors.Ok);
        }

        protected void LogError(string message, Exception ex = null, string blockName = null)
        {
            var fullMessage = blockName != null ? $"[{blockName}] {message}" : message;
            _dmeEditor?.AddLogMessage("UnitofWorksManager", fullMessage, DateTime.Now, -1, null, Errors.Failed);
        }

        #endregion
    }
}
