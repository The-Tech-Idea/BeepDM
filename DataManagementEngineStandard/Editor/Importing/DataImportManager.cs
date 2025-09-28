using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor.Importing
{
    /// <summary>
    /// Enhanced DataImportManager with helper-based architecture and DefaultsManager integration
    /// Supports advanced data import operations with validation, transformation, batch processing, and progress monitoring
    /// </summary>
    public partial class DataImportManager : IDisposable, IDataImportManager
    {
        #region Private Fields

        protected readonly IDMEEditor _editor;
        
        // Helper instances
        protected IDataImportValidationHelper _validationHelper;
        protected IDataImportTransformationHelper _transformationHelper;
        protected IDataImportBatchHelper _batchHelper;
        protected IDataImportProgressHelper _progressHelper;

        // Import configuration
        protected DataImportConfiguration _config;

        // Threading and cancellation
        private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);
        private CancellationTokenSource _internalCancellationTokenSource;
        private Task _importTask;

        protected bool _disposed = false;
        protected static readonly object _lockObject = new object();

        #endregion

        #region IDataImportManager Interface Properties

        /// <summary>
        /// Gets the data validation helper instance
        /// </summary>
        public IDataImportValidationHelper ValidationHelper => _validationHelper;

        /// <summary>
        /// Gets the data transformation helper instance
        /// </summary>
        public IDataImportTransformationHelper TransformationHelper => _transformationHelper;

        /// <summary>
        /// Gets the batch processing helper instance
        /// </summary>
        public IDataImportBatchHelper BatchHelper => _batchHelper;

        /// <summary>
        /// Gets the progress monitoring helper instance
        /// </summary>
        public IDataImportProgressHelper ProgressHelper => _progressHelper;

        #endregion

        #region Backward Compatible Properties

        /// <summary>
        /// Source entity name
        /// </summary>
        public string SourceEntityName 
        { 
            get => _config?.SourceEntityName ?? string.Empty;
            set 
            { 
                EnsureConfigInitialized();
                _config.SourceEntityName = value; 
            }
        }

        /// <summary>
        /// Destination entity name
        /// </summary>
        public string DestEntityName 
        { 
            get => _config?.DestEntityName ?? string.Empty;
            set 
            { 
                EnsureConfigInitialized();
                _config.DestEntityName = value; 
            }
        }

        /// <summary>
        /// Source data source name
        /// </summary>
        public string SourceDataSourceName 
        { 
            get => _config?.SourceDataSourceName ?? string.Empty;
            set 
            { 
                EnsureConfigInitialized();
                _config.SourceDataSourceName = value; 
            }
        }

        /// <summary>
        /// Destination data source name
        /// </summary>
        public string DestDataSourceName 
        { 
            get => _config?.DestDataSourceName ?? string.Empty;
            set 
            { 
                EnsureConfigInitialized();
                _config.DestDataSourceName = value; 
            }
        }

        /// <summary>
        /// Source entity structure
        /// </summary>
        public EntityStructure SourceEntityStructure 
        { 
            get => _config?.SourceEntityStructure;
            set 
            { 
                EnsureConfigInitialized();
                _config.SourceEntityStructure = value; 
            }
        }

        /// <summary>
        /// Destination entity structure
        /// </summary>
        public EntityStructure DestEntityStructure 
        { 
            get => _config?.DestEntityStructure;
            set 
            { 
                EnsureConfigInitialized();
                _config.DestEntityStructure = value; 
            }
        }

        /// <summary>
        /// Source data source instance
        /// </summary>
        public IDataSource SourceData 
        { 
            get => _config?.SourceData;
            set 
            { 
                EnsureConfigInitialized();
                _config.SourceData = value; 
            }
        }

        /// <summary>
        /// Destination data source instance
        /// </summary>
        public IDataSource DestData 
        { 
            get => _config?.DestData;
            set 
            { 
                EnsureConfigInitialized();
                _config.DestData = value; 
            }
        }

        /// <summary>
        /// Entity mapping configuration
        /// </summary>
        public EntityDataMap Mapping 
        { 
            get => _config?.Mapping;
            set 
            { 
                EnsureConfigInitialized();
                _config.Mapping = value; 
            }
        }

        /// <summary>
        /// Source data filters
        /// </summary>
        public List<AppFilter> SourceFilters 
        { 
            get 
            { 
                EnsureConfigInitialized();
                return _config.SourceFilters; 
            }
            set 
            { 
                EnsureConfigInitialized();
                _config.SourceFilters = value ?? new List<AppFilter>(); 
            }
        }

        /// <summary>
        /// Selected fields for import
        /// </summary>
        public List<string> SelectedFields 
        { 
            get => _config?.SelectedFields;
            set 
            { 
                EnsureConfigInitialized();
                _config.SelectedFields = value; 
            }
        }

        /// <summary>
        /// DME Editor instance
        /// </summary>
        public IDMEEditor DMEEditor => _editor;

        /// <summary>
        /// Import log data
        /// </summary>
        public List<Importlogdata> ImportLogData => _progressHelper?.ImportLogData ?? new List<Importlogdata>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of DataImportManager
        /// </summary>
        /// <param name="editor">The DME Editor instance</param>
        public DataImportManager(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            
            InitializeHelpers();
            InitializeConfiguration();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the helper instances
        /// </summary>
        protected virtual void InitializeHelpers()
        {
            _progressHelper = new DataImportProgressHelper(_editor);
            _validationHelper = new DataImportValidationHelper(_editor);
            _transformationHelper = new DataImportTransformationHelper(_editor);
            _batchHelper = new DataImportBatchHelper(_editor, _transformationHelper, _progressHelper);

            _editor.Logger?.WriteLog("DataImportManager helpers initialized successfully");
        }

        /// <summary>
        /// Initializes the import configuration
        /// </summary>
        protected virtual void InitializeConfiguration()
        {
            _config = new DataImportConfiguration();
        }

        /// <summary>
        /// Ensures configuration is initialized
        /// </summary>
        protected void EnsureConfigInitialized()
        {
            if (_config == null)
            {
                InitializeConfiguration();
            }
        }

        #endregion

        #region Core Public API - Enhanced

        /// <summary>
        /// Creates a new import configuration
        /// </summary>
        /// <param name="sourceEntityName">Source entity name</param>
        /// <param name="sourceDataSourceName">Source data source name</param>
        /// <param name="destEntityName">Destination entity name</param>
        /// <param name="destDataSourceName">Destination data source name</param>
        /// <returns>New import configuration</returns>
        public DataImportConfiguration CreateImportConfiguration(string sourceEntityName, string sourceDataSourceName,
            string destEntityName, string destDataSourceName)
        {
            var config = new DataImportConfiguration
            {
                SourceEntityName = sourceEntityName,
                SourceDataSourceName = sourceDataSourceName,
                DestEntityName = destEntityName,
                DestDataSourceName = destDataSourceName
            };

            // Load default values from DefaultsManager
            try
            {
                config.DefaultValues = DefaultsManager.GetDefaults(_editor, destDataSourceName);
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Error loading default values", ex);
            }

            return config;
        }

        /// <summary>
        /// Sets the import configuration
        /// </summary>
        /// <param name="config">Import configuration</param>
        /// <returns>Validation result</returns>
        public IErrorsInfo SetImportConfiguration(DataImportConfiguration config)
        {
            if (config == null)
                return CreateErrorsInfo(Errors.Failed, "Import configuration cannot be null");

            // Validate the configuration
            var validation = _validationHelper.ValidateImportConfiguration(config);
            if (validation.Flag == Errors.Failed)
                return validation;

            _config = config;
            return CreateErrorsInfo(Errors.Ok, "Import configuration set successfully");
        }

        /// <summary>
        /// Gets the current import configuration
        /// </summary>
        /// <returns>Current import configuration</returns>
        public DataImportConfiguration GetImportConfiguration()
        {
            return _config;
        }

        #endregion

        #region Backward Compatible API

        /// <summary>
        /// Loads destination entity structure (backward compatible)
        /// </summary>
        /// <param name="destEntityName">Destination entity name</param>
        /// <param name="destDataSourceName">Destination data source name</param>
        /// <returns>Operation result</returns>
        public IErrorsInfo LoadDestEntityStructure(string destEntityName, string destDataSourceName)
        {
            try
            {
                EnsureConfigInitialized();

                _config.DestEntityName = destEntityName;
                _config.DestDataSourceName = destDataSourceName;
                _config.DestData = _editor.GetDataSource(destDataSourceName);

                if (_config.DestData?.ConnectionStatus == ConnectionState.Open)
                {
                    _config.DestEntityStructure = (EntityStructure)_config.DestData.GetEntityStructure(destEntityName, false)?.Clone();
                    
                    // Load default values using DefaultsManager
                    _config.DefaultValues = DefaultsManager.GetDefaults(_editor, destDataSourceName);
                    
                    return CreateErrorsInfo(Errors.Ok, "Destination entity structure loaded successfully");
                }
                else
                {
                    return CreateErrorsInfo(Errors.Failed, "Destination data source is not connected");
                }
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Error loading destination entity structure", ex);
                return CreateErrorsInfo(Errors.Failed, $"Error loading destination entity structure: {ex.Message}");
            }
        }

        /// <summary>
        /// Runs import asynchronously (backward compatible)
        /// </summary>
        /// <param name="progress">Progress reporter</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="transformation">Custom transformation function</param>
        /// <param name="batchSize">Batch size</param>
        /// <returns>Import result</returns>
        public async Task<IErrorsInfo> RunImportAsync(
            IProgress<IPassedArgs> progress,
            CancellationToken token,
            Func<object, object> transformation = null,
            int batchSize = 50)
        {
            EnsureConfigInitialized();

            _config.CustomTransformation = transformation;
            _config.BatchSize = batchSize;

            return await RunImportAsync(_config, progress, token);
        }

        #endregion

        #region Enhanced Import Methods

        /// <summary>
        /// Runs import with enhanced configuration
        /// </summary>
        /// <param name="config">Import configuration</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Import result</returns>
        public async Task<IErrorsInfo> RunImportAsync(DataImportConfiguration config, 
            IProgress<IPassedArgs> progress, CancellationToken token)
        {
            if (config == null)
                return CreateErrorsInfo(Errors.Failed, "Import configuration is required");

            try
            {
                _progressHelper.LogImport("Starting data import operation", 0);

                // Validate configuration
                var configValidation = _validationHelper.ValidateImportConfiguration(config);
                if (configValidation.Flag == Errors.Failed)
                    return configValidation;

                // Initialize data sources if not already set
                await InitializeDataSources(config);

                // Validate entity mapping if configured
                if (config.Mapping != null)
                {
                    var mappingValidation = _validationHelper.ValidateEntityMapping(config.Mapping);
                    if (mappingValidation.Flag == Errors.Failed)
                        return mappingValidation;
                }

                // Ensure destination entity exists
                await EnsureDestinationEntityExists(config);

                // Fetch source data
                var sourceData = await FetchSourceDataAsync(config, token);
                if (sourceData == null || !sourceData.Any())
                {
                    var message = "No source data found to import";
                    _progressHelper.LogImport(message, 0);
                    return CreateErrorsInfo(Errors.Ok, message);
                }

                // Calculate optimal batch size if not specified
                var sourceList = sourceData.ToList();
                var optimalBatchSize = config.BatchSize > 0 ? config.BatchSize : 
                    _batchHelper.CalculateOptimalBatchSize(sourceList.Count, 1024); // Estimate 1KB per record

                _progressHelper.LogImport($"Processing {sourceList.Count} records in batches of {optimalBatchSize}", 0);

                // Process data in batches
                var totalProcessed = 0;
                var batches = _batchHelper.SplitIntoBatches(sourceList, optimalBatchSize);
                var batchNumber = 0;

                foreach (var batch in batches)
                {
                    batchNumber++;
                    _pauseEvent.Wait(token);
                    token.ThrowIfCancellationRequested();

                    _progressHelper.LogImport($"Processing batch {batchNumber}...", totalProcessed);

                    var batchResult = await _batchHelper.ProcessBatchAsync(batch, config, progress, token);
                    
                    totalProcessed += batch.Count();

                    if (batchResult.Flag == Errors.Failed)
                    {
                        _progressHelper.LogError($"Batch {batchNumber} failed", new Exception(batchResult.Message));
                        // Continue with next batch or stop based on configuration
                    }

                    // Report overall progress
                    _progressHelper.ReportProgress(progress, 
                        $"Completed batch {batchNumber}. Total processed: {totalProcessed}", 
                        totalProcessed, sourceList.Count);
                }

                _progressHelper.LogImport($"Import completed successfully. Total records processed: {totalProcessed}", totalProcessed);
                return CreateErrorsInfo(Errors.Ok, $"Import completed successfully. {totalProcessed} records processed.");
            }
            catch (OperationCanceledException)
            {
                _progressHelper.LogImport("Import operation was cancelled", 0);
                return CreateErrorsInfo(Errors.Ok, "Import operation was cancelled by user");
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Import operation failed", ex);
                return CreateErrorsInfo(Errors.Failed, $"Import operation failed: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Creates an IErrorsInfo object with the specified flag and message
        /// </summary>
        protected IErrorsInfo CreateErrorsInfo(Errors flag, string message)
        {
            return new ErrorsInfo
            {
                Flag = flag,
                Message = message
            };
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the DataImportManager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed resources
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lockObject)
                {
                    try
                    {
                        // Cancel any running import
                        _internalCancellationTokenSource?.Cancel();
                        
                        // Dispose of pause event
                        _pauseEvent?.Dispose();
                        
                        // Dispose of cancellation token source
                        _internalCancellationTokenSource?.Dispose();

                        // Wait for import task to complete
                        if (_importTask != null && !_importTask.IsCompleted)
                        {
                            _importTask.Wait(TimeSpan.FromSeconds(5)); // Wait up to 5 seconds
                        }
                    }
                    catch (Exception ex)
                    {
                        _editor?.Logger?.WriteLog($"Error during DataImportManager disposal: {ex.Message}");
                    }

                    _disposed = true;
                }
            }
        }

        #endregion
    }
}
