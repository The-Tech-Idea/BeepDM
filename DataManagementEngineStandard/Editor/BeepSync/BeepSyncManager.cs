using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
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

            // Load existing schemas
            LoadSchemasAsync().ConfigureAwait(false);

            _progressHelper.LogMessage("BeepSyncManager initialized successfully");
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
    }
}
