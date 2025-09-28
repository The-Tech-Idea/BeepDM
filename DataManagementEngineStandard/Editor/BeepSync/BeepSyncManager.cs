using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// BeepSyncManager - A clean, modern sync manager based on best practices
    /// Consolidates functionality from DataSyncManager and DataSyncService with helper-based architecture
    /// </summary>
    public class BeepSyncManager : IDisposable
    {
        #region Private Fields
        
        private readonly IDMEEditor _editor;
        private readonly IDataSourceHelper _dataSourceHelper;
        private readonly IFieldMappingHelper _fieldMappingHelper;
        private readonly ISyncValidationHelper _validationHelper;
        private readonly ISyncProgressHelper _progressHelper;
        private readonly ISchemaPersistenceHelper _persistenceHelper;

        private ObservableBindingList<DataSyncSchema> _syncSchemas;
        private ManualResetEventSlim _pauseEvent;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        #endregion

        #region Public Properties

        /// <summary>
        /// DME Editor instance
        /// </summary>
        public IDMEEditor Editor => _editor;

        /// <summary>
        /// Collection of sync schemas
        /// </summary>
        public ObservableBindingList<DataSyncSchema> SyncSchemas => _syncSchemas;

        /// <summary>
        /// Indicates if sync operations are currently paused
        /// </summary>
        public bool IsPaused => !_pauseEvent.IsSet;

        /// <summary>
        /// Indicates if sync operations have been cancelled
        /// </summary>
        public bool IsCancelled => _cancellationTokenSource.Token.IsCancellationRequested;

        /// <summary>
        /// Enable automatic application of default values during sync operations
        /// </summary>
        public bool ApplyDefaultsOnSync { get; set; } = true;

        /// <summary>
        /// Apply defaults only to empty/null fields (true) or overwrite existing values (false)
        /// </summary>
        public bool ApplyDefaultsOnlyToEmptyFields { get; set; } = true;

        /// <summary>
        /// Enable automatic creation of audit field defaults for sync operations
        /// </summary>
        public bool AutoCreateAuditDefaults { get; set; } = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize BeepSyncManager with editor and create helper instances
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        public BeepSyncManager(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));

            // Initialize helper classes
            _dataSourceHelper = new DataSourceHelper(_editor);
            _fieldMappingHelper = new FieldMappingHelper(_editor, _dataSourceHelper);
            _validationHelper = new SyncValidationHelper(_editor, _dataSourceHelper);
            _progressHelper = new SyncProgressHelper(_editor);
            _persistenceHelper = new SchemaPersistenceHelper(_editor);

            // Initialize control objects
            _pauseEvent = new ManualResetEventSlim(true);
            _cancellationTokenSource = new CancellationTokenSource();
            _syncSchemas = new ObservableBindingList<DataSyncSchema>();

            // Initialize DefaultsManager for sync operations
            InitializeDefaultsManager();

            // Load existing schemas
            LoadSchemasAsync().ConfigureAwait(false);

            _progressHelper.LogMessage("BeepSyncManager initialized successfully");
        }

        /// <summary>
        /// Initialize DefaultsManager and configure sync-specific defaults
        /// </summary>
        private void InitializeDefaultsManager()
        {
            try
            {
                // Initialize DefaultsManager
                DefaultsManager.Initialize(_editor);
                _progressHelper.LogMessage("DefaultsManager integration initialized successfully");
                
                // Log information about existing defaults
                LogExistingDefaults();
            }
            catch (Exception ex)
            {
                _progressHelper.LogMessage($"Warning: DefaultsManager initialization failed: {ex.Message}", Errors.Failed);
                // Continue without defaults integration if initialization fails
                ApplyDefaultsOnSync = false;
                _progressHelper.LogMessage("Defaults application has been disabled due to initialization failure", Errors.Failed);
            }
        }

        /// <summary>
        /// Log information about existing defaults across all data sources
        /// </summary>
        private void LogExistingDefaults()
        {
            try
            {
                // This would require access to all data sources to check for defaults
                // For now, we'll just log that defaults checking is available
                _progressHelper.LogMessage("Defaults checking is available. Use HasDefaultsConfigured() to check specific data sources.");
            }
            catch (Exception ex)
            {
                _progressHelper.LogMessage($"Error logging existing defaults: {ex.Message}", Errors.Failed);
            }
        }

        #endregion

        #region Core Sync Operations

        /// <summary>
        /// Synchronize data based on schema - async version with full error handling and progress reporting
        /// </summary>
        /// <param name="schema">Sync schema to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>Operation result</returns>
        public async Task<IErrorsInfo> SyncDataAsync(DataSyncSchema schema, CancellationToken cancellationToken = default, IProgress<PassedArgs> progress = null)
        {
            if (schema == null)
                return CreateErrorResult("Schema cannot be null");

            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _cancellationTokenSource.Token).Token;

            try
            {
                // Pre-sync validation
                _progressHelper.ReportProgress(progress, $"Validating sync schema '{schema.ID}'...");
                var validationResult = _validationHelper.ValidateSyncOperation(schema);
                if (validationResult.Flag == Errors.Failed)
                {
                    _progressHelper.LogError(schema, "Schema validation failed", null);
                    return validationResult;
                }

                // Validate defaults configuration if enabled
                if (ApplyDefaultsOnSync)
                {
                    _progressHelper.ReportProgress(progress, "Validating defaults configuration...");
                    var defaultsValidation = ValidateSchemaDefaults(schema);
                    if (defaultsValidation.Flag == Errors.Failed)
                    {
                        _progressHelper.LogError(schema, "Defaults validation failed", null);
                        // Continue with sync but log the warning
                        _progressHelper.LogMessage($"Continuing sync despite defaults validation issues: {defaultsValidation.Message}", Errors.Failed);
                    }
                    else if (!string.IsNullOrEmpty(defaultsValidation.Message))
                    {
                        _progressHelper.LogMessage($"Defaults validation: {defaultsValidation.Message}", Errors.Ok);
                    }
                }

                // Get source data
                _progressHelper.ReportProgress(progress, $"Retrieving data from {schema.SourceDataSourceName}.{schema.SourceEntityName}...");
                var sourceData = await _dataSourceHelper.GetEntityDataAsync(
                    schema.SourceDataSourceName, 
                    schema.SourceEntityName, 
                    schema.Filters?.ToList());

                if (sourceData == null)
                {
                    var error = "Source data retrieval failed";
                    _progressHelper.LogError(schema, error);
                    return CreateErrorResult(error);
                }

                // Process records
                var records = NormalizeToEnumerable(sourceData);
                var recordsList = records.ToList();
                int totalRecords = recordsList.Count;
                int processedCount = 0;
                int errorCount = 0;

                _progressHelper.ReportProgress(progress, $"Processing {totalRecords} records...", 0, totalRecords);

                foreach (var record in recordsList)
                {
                    try
                    {
                        // Check for pause/cancellation
                        _pauseEvent.Wait(combinedToken);
                        combinedToken.ThrowIfCancellationRequested();

                        // Process single record
                        var recordResult = await ProcessSingleRecordAsync(schema, record, progress, combinedToken);
                        if (recordResult.Flag == Errors.Ok)
                            processedCount++;
                        else
                            errorCount++;

                        // Report progress
                        if ((processedCount + errorCount) % 10 == 0)
                        {
                            _progressHelper.ReportProgress(progress, 
                                $"Processed {processedCount + errorCount}/{totalRecords} records...", 
                                processedCount + errorCount, totalRecords);
                        }

                        // Stop if too many errors
                        if (errorCount > 10)
                        {
                            var errorMsg = $"Too many errors ({errorCount}). Stopping sync.";
                            _progressHelper.LogError(schema, errorMsg);
                            return CreateErrorResult(errorMsg);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _progressHelper.LogCancellation(schema, progress);
                        return CreateErrorResult("Sync operation was cancelled");
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _progressHelper.LogMessage($"Error processing record: {ex.Message}", Errors.Failed);
                    }
                }

                // Complete sync
                _progressHelper.LogSuccess(schema, processedCount, progress);
                return CreateSuccessResult($"Sync completed. Processed {processedCount} records with {errorCount} errors.");

            }
            catch (Exception ex)
            {
                _progressHelper.LogError(schema, "Sync operation failed", ex);
                return CreateErrorResult($"Sync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Synchronize data - synchronous version
        /// </summary>
        /// <param name="schema">Sync schema to execute</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>Operation result</returns>
        public IErrorsInfo SyncData(DataSyncSchema schema, IProgress<PassedArgs> progress = null)
        {
            return SyncDataAsync(schema, CancellationToken.None, progress).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronize all schemas
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>Operation result</returns>
        public async Task<IErrorsInfo> SyncAllSchemasAsync(CancellationToken cancellationToken = default, IProgress<PassedArgs> progress = null)
        {
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _cancellationTokenSource.Token).Token;

            int successCount = 0;
            int errorCount = 0;
            int totalSchemas = _syncSchemas.Count;

            _progressHelper.ReportProgress(progress, $"Starting sync for {totalSchemas} schemas...", 0, totalSchemas);

            foreach (var schema in _syncSchemas)
            {
                try
                {
                    combinedToken.ThrowIfCancellationRequested();
                    _pauseEvent.Wait(combinedToken);

                    var result = await SyncDataAsync(schema, combinedToken, progress);
                    if (result.Flag == Errors.Ok)
                        successCount++;
                    else
                        errorCount++;

                    _progressHelper.ReportProgress(progress, 
                        $"Completed {successCount + errorCount}/{totalSchemas} schemas...", 
                        successCount + errorCount, totalSchemas);
                }
                catch (OperationCanceledException)
                {
                    _progressHelper.LogMessage("Sync all schemas operation was cancelled", Errors.Ok);
                    return CreateErrorResult("Operation was cancelled");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _progressHelper.LogMessage($"Error syncing schema '{schema.ID}': {ex.Message}", Errors.Failed);
                }
            }

            var message = $"Sync all completed. Success: {successCount}, Errors: {errorCount}";
            _progressHelper.LogMessage(message, errorCount == 0 ? Errors.Ok : Errors.Failed);
            return errorCount == 0 ? CreateSuccessResult(message) : CreateErrorResult(message);
        }

        #endregion

        #region Schema Management

        /// <summary>
        /// Add sync schema to collection
        /// </summary>
        /// <param name="schema">Schema to add</param>
        public void AddSyncSchema(DataSyncSchema schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            _syncSchemas.Add(schema);
            _progressHelper.LogMessage($"Added sync schema '{schema.ID}'");
        }

        /// <summary>
        /// Remove sync schema from collection
        /// </summary>
        /// <param name="schemaId">ID of schema to remove</param>
        public void RemoveSyncSchema(string schemaId)
        {
            var schema = _syncSchemas.FirstOrDefault(s => s.ID == schemaId);
            if (schema != null)
            {
                _syncSchemas.Remove(schema);
                _progressHelper.LogMessage($"Removed sync schema '{schemaId}'");
            }
        }

        /// <summary>
        /// Update existing sync schema
        /// </summary>
        /// <param name="schema">Updated schema</param>
        public void UpdateSyncSchema(DataSyncSchema schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var existing = _syncSchemas.FirstOrDefault(s => s.ID == schema.ID);
            if (existing != null)
            {
                var index = _syncSchemas.IndexOf(existing);
                _syncSchemas[index] = schema;
                _progressHelper.LogMessage($"Updated sync schema '{schema.ID}'");
            }
        }

        /// <summary>
        /// Get schema by ID
        /// </summary>
        /// <param name="schemaId">Schema ID</param>
        /// <returns>Schema or null if not found</returns>
        public DataSyncSchema GetSchema(string schemaId)
        {
            return _syncSchemas.FirstOrDefault(s => s.ID == schemaId);
        }

        /// <summary>
        /// Validate schema using validation helper
        /// </summary>
        /// <param name="schema">Schema to validate</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo ValidateSchema(DataSyncSchema schema)
        {
            return _validationHelper.ValidateSchema(schema);
        }

        #endregion

        #region Schema Persistence

        /// <summary>
        /// Save all schemas to storage
        /// </summary>
        public async Task SaveSchemasAsync()
        {
            await _persistenceHelper.SaveSchemasAsync(_syncSchemas);
        }

        /// <summary>
        /// Load schemas from storage
        /// </summary>
        public async Task LoadSchemasAsync()
        {
            _syncSchemas = await _persistenceHelper.LoadSchemasAsync();
        }

        /// <summary>
        /// Save schemas synchronously
        /// </summary>
        public void SaveSchemas()
        {
            SaveSchemasAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Load schemas synchronously
        /// </summary>
        public void LoadSchemas()
        {
            LoadSchemasAsync().GetAwaiter().GetResult();
        }

        #endregion

        #region Control Operations

        /// <summary>
        /// Pause sync operations
        /// </summary>
        public void PauseSync()
        {
            _pauseEvent.Reset();
            _progressHelper.LogMessage("Sync operations paused");
        }

        /// <summary>
        /// Resume sync operations
        /// </summary>
        public void ResumeSync()
        {
            _pauseEvent.Set();
            _progressHelper.LogMessage("Sync operations resumed");
        }

        /// <summary>
        /// Cancel all sync operations
        /// </summary>
        public void CancelSync()
        {
            _cancellationTokenSource.Cancel();
            _progressHelper.LogMessage("Sync operations cancelled");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Process a single record through the sync pipeline
        /// </summary>
        private async Task<IErrorsInfo> ProcessSingleRecordAsync(DataSyncSchema schema, object sourceRecord, 
            IProgress<PassedArgs> progress, CancellationToken cancellationToken)
        {
            try
            {
                // Check if defaults should be applied and if defaults exist for the destination data source
                if (ApplyDefaultsOnSync)
                {
                    // Check if there are any defaults configured for the destination data source
                    if (HasDefaultsForDataSource(schema.DestinationDataSourceName))
                    {
                        _progressHelper.ReportProgress(progress, "Applying default values...");
                        sourceRecord = ApplyDefaultsToRecord(schema.DestinationDataSourceName, schema.DestinationEntityName, sourceRecord);
                    }
                    else
                    {
                        // Log that no defaults were found but continue processing
                        _progressHelper.ReportProgress(progress, $"No defaults configured for {schema.DestinationDataSourceName}, skipping defaults application");
                    }
                }

                // Create filters to check if record exists in destination
                var keyValue = GetSyncKeyValue(sourceRecord, schema.SourceSyncDataField);
                if (keyValue == null)
                    return CreateErrorResult("Source sync key value is null");

                var filters = new List<AppFilter>
                {
                    new AppFilter
                    {
                        FieldName = schema.DestinationSyncDataField,
                        Operator = "=",
                        FilterValue = keyValue
                    }
                };

                // Check if record exists in destination
                bool recordExists = await _dataSourceHelper.EntityExistsAsync(
                    schema.DestinationDataSourceName, 
                    schema.DestinationEntityName, 
                    filters);

                // Create destination entity
                var destEntity = _fieldMappingHelper.CreateDestinationEntity(
                    schema.DestinationDataSourceName, 
                    schema.DestinationEntityName);

                if (destEntity == null)
                    return CreateErrorResult("Failed to create destination entity");

                // Map fields
                _fieldMappingHelper.MapFields(sourceRecord, destEntity, schema.MappedFields);

                // Insert or update
                IErrorsInfo result;
                if (recordExists)
                {
                    result = await _dataSourceHelper.UpdateEntityAsync(
                        schema.DestinationDataSourceName, 
                        schema.DestinationEntityName, 
                        destEntity);
                }
                else
                {
                    result = await _dataSourceHelper.InsertEntityAsync(
                        schema.DestinationDataSourceName, 
                        schema.DestinationEntityName, 
                        destEntity);
                }

                return result ?? CreateSuccessResult("Record processed successfully");
            }
            catch (Exception ex)
            {
                return CreateErrorResult($"Error processing record: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply default values to a record during sync operations
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="record">Record to apply defaults to</param>
        /// <param name="context">Additional context for rule resolution</param>
        /// <returns>Record with defaults applied</returns>
        private object ApplyDefaultsToRecord(string dataSourceName, string entityName, object record, 
            Dictionary<string, object> context = null)
        {
            if (!ApplyDefaultsOnSync || record == null)
                return record;

            try
            {
                // First check if there are any defaults configured for this data source
                var defaults = DefaultsManager.GetDefaults(_editor, dataSourceName);
                if (defaults == null || !defaults.Any())
                {
                    // No defaults configured for this data source, skip processing
                    return record;
                }

                // Create parameters for default resolution
                var parameters = new PassedArgs
                {
                    DatasourceName = dataSourceName,
                    CurrentEntity = entityName,
                    ObjectName = entityName,
                    SentData = record
                };

                // Add context data if provided
                if (context != null)
                {
                    var objects = new List<object>();
                    foreach (var kvp in context)
                    {
                        objects.Add(new { Name = kvp.Key, obj = kvp.Value });
                    }
                    parameters.SentData = objects;
                }

                // Apply defaults using DefaultsManager
                var updatedRecord = DefaultsManager.ApplyDefaultsToRecord(_editor, dataSourceName, entityName, record, parameters);
                
                return updatedRecord ?? record;
            }
            catch (Exception ex)
            {
                _progressHelper.LogMessage($"Error applying defaults to record: {ex.Message}", Errors.Failed);
                return record; // Return original record if defaults application fails
            }
        }

        /// <summary>
        /// Check if there are any defaults configured for the specified data source
        /// </summary>
        /// <param name="dataSourceName">Name of the data source to check</param>
        /// <returns>True if defaults exist, false otherwise</returns>
        private bool HasDefaultsForDataSource(string dataSourceName)
        {
            if (!ApplyDefaultsOnSync || string.IsNullOrWhiteSpace(dataSourceName))
                return false;

            try
            {
                var defaults = DefaultsManager.GetDefaults(_editor, dataSourceName);
                return defaults != null && defaults.Any();
            }
            catch (Exception ex)
            {
                _progressHelper.LogMessage($"Error checking defaults for data source {dataSourceName}: {ex.Message}", Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Check if there are defaults configured for a specific data source and entity
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity (optional, for logging purposes)</param>
        /// <returns>True if defaults exist, false otherwise</returns>
        public bool HasDefaultsConfigured(string dataSourceName, string entityName = null)
        {
            if (!ApplyDefaultsOnSync || string.IsNullOrWhiteSpace(dataSourceName))
                return false;

            try
            {
                var defaults = DefaultsManager.GetDefaults(_editor, dataSourceName);
                var hasDefaults = defaults != null && defaults.Any();
                
                if (hasDefaults)
                {
                    var entityInfo = !string.IsNullOrEmpty(entityName) ? $" for entity {entityName}" : "";
                    _progressHelper.LogMessage($"Found {defaults.Count} default values configured for data source {dataSourceName}{entityInfo}");
                }
                else
                {
                    var entityInfo = !string.IsNullOrEmpty(entityName) ? $" for entity {entityName}" : "";
                    _progressHelper.LogMessage($"No default values configured for data source {dataSourceName}{entityInfo}");
                }

                return hasDefaults;
            }
            catch (Exception ex)
            {
                _progressHelper.LogMessage($"Error checking defaults for data source {dataSourceName}: {ex.Message}", Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Get sync key value from source record
        /// </summary>
        private string GetSyncKeyValue(object record, string fieldName)
        {
            try
            {
                var property = record.GetType().GetProperty(fieldName);
                return property?.GetValue(record)?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Normalize data to enumerable for consistent processing
        /// </summary>
        private IEnumerable<object> NormalizeToEnumerable(object data)
        {
            if (data is IEnumerable<object> enumerable)
                return enumerable;
            
            return new List<object> { data };
        }

        /// <summary>
        /// Create error result
        /// </summary>
        private IErrorsInfo CreateErrorResult(string message)
        {
            return new ErrorsInfo { Flag = Errors.Failed, Message = message };
        }

        /// <summary>
        /// Create success result
        /// </summary>
        private IErrorsInfo CreateSuccessResult(string message)
        {
            return new ErrorsInfo { Flag = Errors.Ok, Message = message };
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose managed resources
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    // Save schemas before disposing
                    SaveSchemas();
                }
                catch (Exception ex)
                {
                    _progressHelper?.LogMessage($"Error saving schemas during dispose: {ex.Message}", Errors.Failed);
                }

                _pauseEvent?.Dispose();
                _cancellationTokenSource?.Dispose();
                _syncSchemas?.Clear();
                
                _disposed = true;
                _progressHelper?.LogMessage("BeepSyncManager disposed");
            }
        }

        /// <summary>
        /// Dispose the manager and clean up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Backward Compatibility Methods (from DataSyncManager)

        /// <summary>
        /// Synchronize data by schema ID - backward compatibility
        /// </summary>
        /// <param name="schemaId">Schema ID</param>
        public void SyncData(string schemaId)
        {
            var schema = GetSchema(schemaId);
            if (schema != null)
            {
                SyncData(schema);
            }
            else
            {
                _progressHelper.LogMessage($"Schema with ID '{schemaId}' not found", Errors.Failed);
            }
        }

        /// <summary>
        /// Synchronize data with parameters - backward compatibility
        /// </summary>
        public void SyncData(string schemaId, string sourceEntityName, string destinationEntityName)
        {
            var schema = GetSchema(schemaId);
            if (schema != null)
            {
                schema.SourceEntityName = sourceEntityName;
                schema.DestinationEntityName = destinationEntityName;
                SyncData(schema);
            }
        }

        /// <summary>
        /// Synchronize data with sync field parameters - backward compatibility
        /// </summary>
        public void SyncData(string schemaId, string sourceEntityName, string destinationEntityName, string sourceSyncDataField)
        {
            var schema = GetSchema(schemaId);
            if (schema != null)
            {
                schema.SourceEntityName = sourceEntityName;
                schema.DestinationEntityName = destinationEntityName;
                schema.SourceSyncDataField = sourceSyncDataField;
                SyncData(schema);
            }
        }

        /// <summary>
        /// Synchronize data with full parameters - backward compatibility
        /// </summary>
        public void SyncData(string schemaId, string sourceEntityName, string destinationEntityName, 
            string sourceSyncDataField, string destinationSyncDataField)
        {
            var schema = GetSchema(schemaId);
            if (schema != null)
            {
                schema.SourceEntityName = sourceEntityName;
                schema.DestinationEntityName = destinationEntityName;
                schema.SourceSyncDataField = sourceSyncDataField;
                schema.DestinationSyncDataField = destinationSyncDataField;
                SyncData(schema);
            }
        }

        /// <summary>
        /// Add filter to schema - backward compatibility
        /// </summary>
        public void AddFilter(string schemaId, AppFilter filter)
        {
            var schema = GetSchema(schemaId);
            if (schema != null)
            {
                schema.Filters ??= new ObservableBindingList<AppFilter>();
                schema.Filters.Add(filter);
                _progressHelper.LogMessage($"Added filter to schema '{schemaId}'");
            }
        }

        /// <summary>
        /// Remove filter from schema - backward compatibility
        /// </summary>
        public void RemoveFilter(string schemaId, string fieldName)
        {
            var schema = GetSchema(schemaId);
            if (schema != null)
            {
                var filter = schema.Filters?.FirstOrDefault(f => f.FieldName == fieldName);
                if (filter != null)
                {
                    schema.Filters.Remove(filter);
                    _progressHelper.LogMessage($"Removed filter '{fieldName}' from schema '{schemaId}'");
                }
            }
        }

        /// <summary>
        /// Add field mapping to schema - backward compatibility
        /// </summary>
        public void AddFieldMapping(string schemaId, FieldSyncData field)
        {
            var schema = GetSchema(schemaId);
            if (schema != null)
            {
                schema.MappedFields ??= new ObservableBindingList<FieldSyncData>();
                schema.MappedFields.Add(field);
                _progressHelper.LogMessage($"Added field mapping to schema '{schemaId}'");
            }
        }

        /// <summary>
        /// Synchronize all schemas - backward compatibility (sync version)
        /// </summary>
        public void SyncAllData()
        {
            SyncAllSchemasAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get records from source with filter operator - enhanced version from DataSyncManager
        /// </summary>
        /// <param name="schema">Sync schema</param>
        /// <param name="filterOperator">Filter operator (>, >=, <, <=, =)</param>
        /// <returns>Records from source</returns>
        public async Task<object> GetRecordsFromSourceData(DataSyncSchema schema, string filterOperator)
        {
            if (schema?.LastSyncDate == null || schema.LastSyncDate == DateTime.MinValue)
                return null;

            var filters = schema.Filters?.ToList() ?? new List<AppFilter>();
            filters.Add(new AppFilter
            {
                FieldName = schema.SourceSyncDataField,
                Operator = filterOperator,
                FilterValue = schema.LastSyncDate.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return await _dataSourceHelper.GetEntityDataAsync(
                schema.SourceDataSourceName, 
                schema.SourceEntityName, 
                filters);
        }

        /// <summary>
        /// Get new records since last sync - enhanced version from DataSyncManager
        /// </summary>
        /// <param name="schema">Sync schema</param>
        /// <returns>New records</returns>
        public Task<object> GetNewRecordsFromSourceData(DataSyncSchema schema)
        {
            return GetRecordsFromSourceData(schema, ">");
        }

        /// <summary>
        /// Get updated records since last sync - enhanced version from DataSyncManager
        /// </summary>
        /// <param name="schema">Sync schema</param>
        /// <returns>Updated records</returns>
        public Task<object> GetUpdatedRecordsFromSourceData(DataSyncSchema schema)
        {
            return GetRecordsFromSourceData(schema, ">=");
        }

        #endregion

        #region Enhanced Sync Operations (New Features)

        /// <summary>
        /// Bulk synchronize multiple records with enhanced error handling and metrics
        /// </summary>
        /// <param name="schema">Sync schema</param>
        /// <param name="batchSize">Number of records to process in each batch</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>Sync metrics and results</returns>
        public async Task<SyncMetrics> BulkSyncAsync(DataSyncSchema schema, int batchSize = 100, 
            CancellationToken cancellationToken = default, IProgress<PassedArgs> progress = null)
        {
            var metrics = new SyncMetrics
            {
                SchemaID = schema.ID,
                SyncDate = DateTime.Now
            };

            try
            {
                var sourceData = await _dataSourceHelper.GetEntityDataAsync(
                    schema.SourceDataSourceName, 
                    schema.SourceEntityName, 
                    schema.Filters?.ToList());

                if (sourceData == null)
                {
                    metrics.FailedRecords = 1;
                    return metrics;
                }

                var records = NormalizeToEnumerable(sourceData).ToList();
                metrics.TotalRecords = records.Count;

                // Process in batches
                for (int i = 0; i < records.Count; i += batchSize)
                {
                    var batch = records.Skip(i).Take(batchSize);
                    var batchResults = await ProcessBatchAsync(schema, batch, cancellationToken);
                    
                    metrics.SuccessfulRecords += batchResults.successCount;
                    metrics.FailedRecords += batchResults.errorCount;

                    _progressHelper.ReportProgress(progress, 
                        $"Processed batch {(i / batchSize) + 1}, Records: {metrics.SuccessfulRecords + metrics.FailedRecords}/{metrics.TotalRecords}",
                        metrics.SuccessfulRecords + metrics.FailedRecords, metrics.TotalRecords);

                    // Check for cancellation between batches
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return metrics;
            }
            catch (Exception ex)
            {
                _progressHelper.LogError(schema, "Bulk sync failed", ex);
                metrics.FailedRecords = metrics.TotalRecords - metrics.SuccessfulRecords;
                return metrics;
            }
        }

        /// <summary>
        /// Process a batch of records
        /// </summary>
        /// <param name="schema">Sync schema</param>
        /// <param name="records">Records to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tuple with success count and error count</returns>
        private async Task<(int successCount, int errorCount)> ProcessBatchAsync(
            DataSyncSchema schema, IEnumerable<object> records, CancellationToken cancellationToken)
        {
            int successCount = 0;
            int errorCount = 0;

            var tasks = records.Select(async record =>
            {
                try
                {
                    var result = await ProcessSingleRecordAsync(schema, record, null, cancellationToken);
                    return result.Flag == Errors.Ok;
                }
                catch
                {
                    return false;
                }
            });

            var results = await Task.WhenAll(tasks);
            successCount = results.Count(r => r);
            errorCount = results.Count(r => !r);

            return (successCount, errorCount);
        }

        #endregion

        #region DefaultsManager Integration

        /// <summary>
        /// Configure default values for a data source used in sync operations
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="defaults">Dictionary of field names and default configurations</param>
        /// <returns>Configuration result</returns>
        public IErrorsInfo ConfigureSyncDefaults(string dataSourceName, string entityName, 
            Dictionary<string, (string value, bool isRule)> defaults)
        {
            if (!ApplyDefaultsOnSync)
            {
                return CreateErrorResult("DefaultsManager integration is disabled");
            }

            try
            {
                var result = DefaultsManager.SetMultipleColumnDefaults(_editor, dataSourceName, entityName, defaults);
                
                if (result.Flag == Errors.Ok)
                {
                    _progressHelper.LogMessage($"Configured {defaults.Count} default values for {dataSourceName}.{entityName}");
                }
                else
                {
                    _progressHelper.LogMessage($"Failed to configure defaults for {dataSourceName}.{entityName}: {result.Message}", Errors.Failed);
                }

                return result;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error configuring sync defaults: {ex.Message}";
                _progressHelper.LogMessage(errorMsg, Errors.Failed);
                return CreateErrorResult(errorMsg);
            }
        }

        /// <summary>
        /// Set up standard audit defaults for sync operations
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <returns>Configuration result</returns>
        public IErrorsInfo SetupAuditDefaults(string dataSourceName, string entityName)
        {
            if (!ApplyDefaultsOnSync)
            {
                return CreateErrorResult("DefaultsManager integration is disabled");
            }

            try
            {
                var auditDefaults = new Dictionary<string, (string value, bool isRule)>
                {
                    { "CreatedBy", ("USERNAME", true) },
                    { "CreatedDate", ("NOW", true) },
                    { "ModifiedBy", ("USERNAME", true) },
                    { "ModifiedDate", ("NOW", true) },
                    { "SyncedBy", ("USERNAME", true) },
                    { "SyncedDate", ("NOW", true) },
                    { "SyncSource", ("BeepSyncManager", false) }
                };

                var result = ConfigureSyncDefaults(dataSourceName, entityName, auditDefaults);
                
                if (result.Flag == Errors.Ok)
                {
                    _progressHelper.LogMessage($"Configured audit defaults for {dataSourceName}.{entityName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error setting up audit defaults: {ex.Message}";
                _progressHelper.LogMessage(errorMsg, Errors.Failed);
                return CreateErrorResult(errorMsg);
            }
        }

        /// <summary>
        /// Get configured default values for an entity
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <returns>Dictionary of configured defaults</returns>
        public Dictionary<string, DefaultValue> GetEntityDefaults(string dataSourceName, string entityName)
        {
            if (!ApplyDefaultsOnSync)
                return new Dictionary<string, DefaultValue>();

            try
            {
                // Get defaults for the data source
                var defaults = DefaultsManager.GetDefaults(_editor, dataSourceName);
                if (defaults == null || !defaults.Any())
                {
                    return new Dictionary<string, DefaultValue>();
                }

                // Convert to dictionary for easier access
                return defaults.ToDictionary(d => d.PropertyName, d => d);
            }
            catch (Exception ex)
            {
                _progressHelper.LogMessage($"Error getting entity defaults: {ex.Message}", Errors.Failed);
                return new Dictionary<string, DefaultValue>();
            }
        }

        /// <summary>
        /// Auto-configure defaults for a sync schema
        /// </summary>
        /// <param name="schema">Sync schema to configure defaults for</param>
        /// <returns>Configuration result</returns>
        public IErrorsInfo AutoConfigureSchemaDefaults(DataSyncSchema schema)
        {
            if (!ApplyDefaultsOnSync || schema == null)
            {
                return CreateSuccessResult("Defaults integration disabled or invalid schema");
            }

            try
            {
                var results = new List<IErrorsInfo>();

                // Configure defaults for destination entity if enabled
                if (AutoCreateAuditDefaults)
                {
                    var auditResult = SetupAuditDefaults(schema.DestinationDataSourceName, schema.DestinationEntityName);
                    results.Add(auditResult);
                }

                // Add common sync defaults
                var syncDefaults = new Dictionary<string, (string value, bool isRule)>
                {
                    { "LastSyncDate", ("NOW", true) },
                    { "SyncSchemaId", (schema.ID, false) },
                    { "SyncStatus", ("Synced", false) }
                };

                var defaultsResult = ConfigureSyncDefaults(schema.DestinationDataSourceName, schema.DestinationEntityName, syncDefaults);
                results.Add(defaultsResult);

                // Determine overall result
                var hasErrors = results.Any(r => r.Flag == Errors.Failed);
                var message = hasErrors 
                    ? "Some defaults configuration failed" 
                    : "Schema defaults configured successfully";

                return new ErrorsInfo
                {
                    Flag = hasErrors ? Errors.Failed : Errors.Ok,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error auto-configuring schema defaults: {ex.Message}";
                _progressHelper.LogMessage(errorMsg, Errors.Failed);
                return CreateErrorResult(errorMsg);
            }
        }

        /// <summary>
        /// Validate that defaults are properly configured for a sync schema
        /// </summary>
        /// <param name="schema">Sync schema to validate</param>
        /// <returns>Validation result with details about defaults configuration</returns>
        public IErrorsInfo ValidateSchemaDefaults(DataSyncSchema schema)
        {
            if (schema == null)
                return CreateErrorResult("Schema cannot be null");

            if (!ApplyDefaultsOnSync)
                return CreateSuccessResult("Defaults integration is disabled - no validation needed");

            try
            {
                var issues = new List<string>();
                var warnings = new List<string>();

                // Check source data source defaults
                if (!HasDefaultsConfigured(schema.SourceDataSourceName, schema.SourceEntityName))
                {
                    warnings.Add($"No defaults configured for source data source: {schema.SourceDataSourceName}");
                }

                // Check destination data source defaults
                if (!HasDefaultsConfigured(schema.DestinationDataSourceName, schema.DestinationEntityName))
                {
                    warnings.Add($"No defaults configured for destination data source: {schema.DestinationDataSourceName}");
                }

                // Check if audit defaults are expected but not configured
                if (AutoCreateAuditDefaults)
                {
                    var destDefaults = GetEntityDefaults(schema.DestinationDataSourceName, schema.DestinationEntityName);
                    var auditFields = new[] { "CreatedBy", "CreatedDate", "ModifiedBy", "ModifiedDate" };
                    
                    foreach (var auditField in auditFields)
                    {
                        if (!destDefaults.ContainsKey(auditField))
                        {
                            warnings.Add($"Expected audit field '{auditField}' not configured with defaults");
                        }
                    }
                }

                // Compile results
                var message = "";
                if (issues.Any())
                {
                    message = $"Validation failed: {string.Join("; ", issues)}";
                    if (warnings.Any())
                    {
                        message += $". Warnings: {string.Join("; ", warnings)}";
                    }
                    return CreateErrorResult(message);
                }
                else if (warnings.Any())
                {
                    message = $"Validation passed with warnings: {string.Join("; ", warnings)}";
                    _progressHelper.LogMessage(message, Errors.Ok);
                    return CreateSuccessResult(message);
                }
                else
                {
                    message = "Schema defaults validation passed successfully";
                    return CreateSuccessResult(message);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error validating schema defaults: {ex.Message}";
                _progressHelper.LogMessage(errorMsg, Errors.Failed);
                return CreateErrorResult(errorMsg);
            }
        }

        #endregion
    }
}
