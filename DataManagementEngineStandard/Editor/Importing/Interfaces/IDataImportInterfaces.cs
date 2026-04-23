using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Addin;
// Phase 6-11 type references
using TheTechIdea.Beep.Editor.Importing.Quality;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.History;
using TheTechIdea.Beep.Editor.Importing.Staging;

namespace TheTechIdea.Beep.Editor.Importing.Interfaces
{
    /// <summary>
    /// Interface for managing data import operations
    /// </summary>
    public interface IDataImportManager : IDisposable
    {
        /// <summary>Gets the data validation helper instance</summary>
        IDataImportValidationHelper ValidationHelper { get; }

        /// <summary>Gets the data transformation helper instance</summary>
        IDataImportTransformationHelper TransformationHelper { get; }

        /// <summary>Gets the batch processing helper instance</summary>
        IDataImportBatchHelper BatchHelper { get; }

        /// <summary>Gets the progress monitoring helper instance</summary>
        IDataImportProgressHelper ProgressHelper { get; }

        // --- Phase 2: lifecycle control ---

        /// <summary>Pauses a running import at the next batch boundary.</summary>
        void PauseImport();

        /// <summary>Resumes a paused import.</summary>
        void ResumeImport();

        /// <summary>Requests cancellation of a running or paused import.</summary>
        void CancelImport();

        /// <summary>Returns a detached snapshot of the current import status. Thread-safe.</summary>
        ImportStatus GetImportStatus();

        // --- Phase 2: context-based entry points ---

        /// <summary>Runs an import defined by a typed ImportContext.</summary>
        Task<IErrorsInfo> RunImportAsync(
            ImportContext context,
            IProgress<IPassedArgs> progress,
            CancellationToken token);

        /// <summary>
        /// Builds a DataImportConfiguration from an ImportContext.
        /// Useful for callers that need the concrete config object.
        /// </summary>
        DataImportConfiguration BuildConfigurationFromContext(ImportContext context);

        /// <summary>Creates a new import configuration from the four core identifiers.</summary>
        DataImportConfiguration CreateImportConfiguration(
            string sourceEntityName,
            string sourceDataSourceName,
            string destEntityName,
            string destDataSourceName);

        /// <summary>Tests whether the supplied configuration is executable (connectivity, entity presence, etc.).</summary>
        Task<IErrorsInfo> TestImportConfigurationAsync(DataImportConfiguration config);

        /// <summary>Runs an import defined by an explicit DataImportConfiguration.</summary>
        Task<IErrorsInfo> RunImportAsync(
            DataImportConfiguration config,
            IProgress<IPassedArgs>? progress,
            CancellationToken token);

        // --- Phase 3: preflight and sync-draft (implemented in DataImportManager.Migration.cs) ---

        /// <summary>Validates schema compatibility before running a migration.</summary>
        Task<IErrorsInfo> RunMigrationPreflightAsync(
            DataImportConfiguration config,
            Action<string>? log = null);

        /// <summary>Builds a sync-draft schema without executing the import.</summary>
        Task<DataSyncSchema> BuildSyncDraftAsync(DataImportConfiguration config);

        // --- Phase 9: dead-letter replay ---

        /// <summary>Re-runs records previously quarantined in the error store.</summary>
        Task<IErrorsInfo> ReplayFailedRecordsAsync(
            string contextKey,
            IProgress<IPassedArgs>? progress,
            CancellationToken token);
    }

    /// <summary>
    /// Interface for data import validation operations
    /// </summary>
    public interface IDataImportValidationHelper
    {
        /// <summary>
        /// Validates import configuration before execution
        /// </summary>
        /// <param name="config">Import configuration to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateImportConfiguration(DataImportConfiguration config);

        /// <summary>
        /// Validates entity mapping configuration
        /// </summary>
        /// <param name="mapping">Entity mapping to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateEntityMapping(EntityDataMap mapping);

        /// <summary>
        /// Validates source and destination entity compatibility
        /// </summary>
        /// <param name="sourceEntity">Source entity structure</param>
        /// <param name="destEntity">Destination entity structure</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateEntityCompatibility(EntityStructure sourceEntity, EntityStructure destEntity);

        /// <summary>
        /// Validates data source connections
        /// </summary>
        /// <param name="sourceDataSource">Source data source</param>
        /// <param name="destDataSource">Destination data source</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateDataSources(IDataSource sourceDataSource, IDataSource destDataSource);
    }

    /// <summary>
    /// Interface for data import transformation operations
    /// </summary>
    public interface IDataImportTransformationHelper
    {
        /// <summary>
        /// Applies field filtering to a record
        /// </summary>
        /// <param name="record">Source record</param>
        /// <param name="selectedFields">Fields to include</param>
        /// <returns>Filtered record</returns>
        object ApplyFieldFiltering(object record, List<string> selectedFields);

        /// <summary>
        /// Applies entity mapping transformations
        /// </summary>
        /// <param name="record">Source record</param>
        /// <param name="mapping">Entity mapping configuration</param>
        /// <param name="targetEntityName">Target entity name</param>
        /// <returns>Transformed record</returns>
        object ApplyEntityMapping(object record, EntityDataMap mapping, string targetEntityName);

        /// <summary>
        /// Applies default values to a record
        /// </summary>
        /// <param name="record">Target record</param>
        /// <param name="defaultValues">Default values to apply</param>
        /// <param name="entityStructure">Entity structure</param>
        /// <param name="dataSourceName">Data source name for context</param>
        /// <returns>Record with applied defaults</returns>
        object ApplyDefaultValues(object record, List<DefaultValue> defaultValues, EntityStructure entityStructure, string dataSourceName);

        /// <summary>
        /// Applies custom transformation function
        /// </summary>
        /// <param name="record">Source record</param>
        /// <param name="transformationFunction">Custom transformation function</param>
        /// <returns>Transformed record</returns>
        object ApplyCustomTransformation(object record, Func<object, object> transformationFunction);

        /// <summary>
        /// Applies complete transformation pipeline
        /// </summary>
        /// <param name="record">Source record</param>
        /// <param name="config">Import configuration</param>
        /// <returns>Fully transformed record</returns>
        object ApplyTransformationPipeline(object record, DataImportConfiguration config);
    }

    /// <summary>
    /// Interface for batch processing operations
    /// </summary>
    public interface IDataImportBatchHelper
    {
        /// <summary>
        /// Calculates optimal batch size based on data characteristics
        /// </summary>
        /// <param name="totalRecords">Total number of records</param>
        /// <param name="estimatedRecordSize">Estimated size per record in bytes</param>
        /// <param name="availableMemory">Available memory for processing</param>
        /// <returns>Optimal batch size</returns>
        int CalculateOptimalBatchSize(int totalRecords, long estimatedRecordSize, long? availableMemory = null);

        /// <summary>
        /// Processes a batch of records
        /// </summary>
        /// <param name="batch">Records to process</param>
        /// <param name="config">Import configuration</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Batch processing result</returns>
        Task<IErrorsInfo> ProcessBatchAsync(IEnumerable<object> batch, DataImportConfiguration config, 
            IProgress<PassedArgs> progress, CancellationToken token);

        /// <summary>
        /// Splits source data into batches
        /// </summary>
        /// <param name="sourceData">Source data to split</param>
        /// <param name="batchSize">Size of each batch</param>
        /// <returns>Enumerable of batches</returns>
        IEnumerable<IEnumerable<object>> SplitIntoBatches(IEnumerable<object> sourceData, int batchSize);
    }

    /// <summary>
    /// Interface for progress monitoring and logging operations
    /// </summary>
    public interface IDataImportProgressHelper
    {
        /// <summary>
        /// Gets the import log data
        /// </summary>
        List<Importlogdata> ImportLogData { get; }

        /// <summary>
        /// Logs an import operation
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="recordNumber">Associated record number</param>
        void LogImport(string message, int recordNumber);

        /// <summary>
        /// Logs an error
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="exception">Exception details</param>
        void LogError(string message, Exception exception);

        /// <summary>
        /// Reports progress to the progress reporter
        /// </summary>
        /// <param name="progress">Progress reporter</param>
        /// <param name="message">Progress message</param>
        /// <param name="recordsProcessed">Number of records processed</param>
        /// <param name="totalRecords">Total records to process</param>
        void ReportProgress(IProgress<PassedArgs> progress, string message, int recordsProcessed, int? totalRecords = null);

        /// <summary>
        /// Calculates and reports performance metrics
        /// </summary>
        /// <param name="startTime">Import start time</param>
        /// <param name="recordsProcessed">Records processed so far</param>
        /// <param name="totalRecords">Total records to process</param>
        /// <returns>Performance metrics</returns>
        ImportPerformanceMetrics CalculatePerformanceMetrics(DateTime startTime, int recordsProcessed, int totalRecords);

        /// <summary>
        /// Clears the import log
        /// </summary>
        void ClearLog();
    }

    // -------------------------------------------------------------------------
    // Phase 1 — Shared context model
    // -------------------------------------------------------------------------

    /// <summary>Defines how a failed batch should be handled.</summary>
    public enum BatchErrorStrategy { Abort, Skip, Retry }

    /// <summary>Lifecycle state of an import run.</summary>
    public enum ImportState { Idle, Running, Paused, Completed, Faulted, Cancelled }

    // ── Phase 6 — Incremental sync ─────────────────────────────────────────
    /// <summary>How the import run handles already-seen records at the destination.</summary>
    public enum SyncMode { FullRefresh, Incremental, Upsert }

    // ── Phase 8 — Schema drift ─────────────────────────────────────────
    /// <summary>What to do when the source schema changed since the last run.</summary>
    public enum SchemaDriftPolicy { Ignore, AbortOnDrift, AutoAddColumns, AutoDropColumns }

    /// <summary>Source / destination selection, independent of execution options.</summary>
    public class ImportSelectionContext
    {
        public string SourceDataSourceName      { get; set; } = string.Empty;
        public string SourceEntityName          { get; set; } = string.Empty;
        public string DestinationDataSourceName { get; set; } = string.Empty;
        public string DestinationEntityName     { get; set; } = string.Empty;
        public bool   CreateDestinationIfNotExists { get; set; } = true;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SourceDataSourceName)      &&
            !string.IsNullOrWhiteSpace(SourceEntityName)          &&
            !string.IsNullOrWhiteSpace(DestinationDataSourceName) &&
            !string.IsNullOrWhiteSpace(DestinationEntityName);
    }

    /// <summary>Execution knobs — what the import run should do.</summary>
    public class ImportExecutionOptions
    {
        public bool RunMigrationPreflight  { get; set; }
        public bool AddMissingColumns      { get; set; } = true;
        public bool CreateSyncProfileDraft { get; set; }
        public bool RunImportOnFinish      { get; set; } = true;
        public int  BatchSize              { get; set; } = 100;
        public BatchErrorStrategy OnBatchError { get; set; } = BatchErrorStrategy.Skip;
        public int  MaxRetries             { get; set; } = 3;
    }

    /// <summary>
    /// Typed, serialisable context that replaces the string-key parameter dictionary.
    /// Both core (DataImportManager) and UI (ImportExportOrchestrator) use this.
    /// </summary>
    public class ImportContext
    {
        public string                RunId     { get; set; } = Guid.NewGuid().ToString("N");
        public ImportSelectionContext Selection { get; set; } = new();
        public EntityDataMap?         Mapping  { get; set; }
        public ImportExecutionOptions Options  { get; set; } = new();

        /// <summary>Builds an ImportContext from a legacy DataImportConfiguration.</summary>
        public static ImportContext From(DataImportConfiguration config) => new()
        {
            Selection = new ImportSelectionContext
            {
                SourceDataSourceName      = config.SourceDataSourceName,
                SourceEntityName          = config.SourceEntityName,
                DestinationDataSourceName = config.DestDataSourceName,
                DestinationEntityName     = config.DestEntityName,
                CreateDestinationIfNotExists = config.CreateDestinationIfNotExists
            },
            Mapping = config.Mapping,
            Options = new ImportExecutionOptions
            {
                BatchSize          = config.BatchSize,
                AddMissingColumns  = config.AddMissingColumns,
                OnBatchError       = config.OnBatchError,
                MaxRetries         = config.MaxRetries,
                RunMigrationPreflight  = config.RunMigrationPreflight,
                CreateSyncProfileDraft = config.CreateSyncProfileDraft
            }
        };
    }

    /// <summary>Live snapshot of an import run — safe to read from any thread.</summary>
    public class ImportStatus
    {
        public ImportState State             { get; set; } = ImportState.Idle;
        public int  RecordsProcessed         { get; set; }
        public int  TotalRecords             { get; set; }
        public double PercentComplete        { get; set; }
        public int  CurrentBatch             { get; set; }
        public int  TotalBatches             { get; set; }
        public string LastMessage            { get; set; } = string.Empty;
        public DateTime? StartedAt           { get; set; }
        public DateTime? FinishedAt          { get; set; }
        public int  RecordsBlocked           { get; set; }
        public int  RecordsQuarantined       { get; set; }
        public int  RecordsWarned            { get; set; }
        public ImportPerformanceMetrics? Metrics { get; set; }

        // ── Convenience state flags (can be set directly or derived from State) ──

        /// <summary>True when the import is actively running.</summary>
        public bool IsRunning   { get; set; }

        /// <summary>True when the import has been paused.</summary>
        public bool IsPaused    { get; set; }

        /// <summary>True when the import finished (success or failure).</summary>
        public bool IsCompleted { get; set; }

        /// <summary>True when the import was cancelled by the user.</summary>
        public bool IsCancelled { get; set; }

        /// <summary>True when error-level log entries were recorded.</summary>
        public bool HasErrors   { get; set; }

        /// <summary>Returns a detached copy — callers cannot mutate the live state.</summary>
        public ImportStatus Snapshot() => (ImportStatus)MemberwiseClone();
    }

    /// <summary>
    /// Configuration class for data import operations
    /// </summary>
    public class DataImportConfiguration
    {
        public string SourceEntityName { get; set; } = string.Empty;
        public string DestEntityName { get; set; } = string.Empty;
        public string SourceDataSourceName { get; set; } = string.Empty;
        public string DestDataSourceName { get; set; } = string.Empty;
        public EntityStructure SourceEntityStructure { get; set; }
        public EntityStructure DestEntityStructure { get; set; }
        public IDataSource SourceData { get; set; }
        public IDataSource DestData { get; set; }
        public EntityDataMap Mapping { get; set; }
        public List<AppFilter> SourceFilters { get; set; } = new List<AppFilter>();
        public List<string> SelectedFields { get; set; }
        public List<DefaultValue> DefaultValues { get; set; } = new List<DefaultValue>();
        public Func<object, object> CustomTransformation { get; set; }
        public int BatchSize { get; set; } = 50;
        public bool CreateDestinationIfNotExists { get; set; } = true;
        public bool ApplyDefaults { get; set; } = true;
        public bool SkipBlanks   { get; set; } = false;

        // --- Phase 1 additions ---
        public bool RunMigrationPreflight   { get; set; }
        public bool AddMissingColumns       { get; set; } = true;
        public bool CreateSyncProfileDraft  { get; set; }
        public BatchErrorStrategy OnBatchError { get; set; } = BatchErrorStrategy.Skip;
        public int  MaxRetries              { get; set; } = 3;

        // --- Phase 6: incremental sync ---
        public SyncMode  SyncMode             { get; set; } = SyncMode.FullRefresh;
        public string    WatermarkColumn      { get; set; } = string.Empty;
        public object?   LastWatermarkValue   { get; set; }
        public List<string> UpsertKeyColumns  { get; set; } = new();

        // --- Phase 7: data quality ---
        public List<IDataQualityRule> QualityRules { get; set; } = new();

        // --- Phase 8: schema drift ---
        public SchemaDriftPolicy DriftPolicy { get; set; } = SchemaDriftPolicy.AutoAddColumns;

        // --- Phase 9: error output ---
        public IImportErrorStore? ErrorStore { get; set; }

        // --- Phase 10: run history ---
        public IImportRunHistoryStore? RunHistoryStore { get; set; }

        // --- Phase 11: staging ---
        public StagingOptions? Staging { get; set; }
    }

    /// <summary>
    /// Performance metrics for import operations
    /// </summary>
    public class ImportPerformanceMetrics
    {
        public TimeSpan ElapsedTime { get; set; }
        public double RecordsPerSecond { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public double PercentageComplete { get; set; }
        public int RecordsProcessed { get; set; }
        public int TotalRecords { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Import log data structure
    /// </summary>
    public class Importlogdata
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Message { get; set; }
        public int RecordNumber { get; set; }
        public ImportLogLevel Level { get; set; } = ImportLogLevel.Info;
        public string Category { get; set; } = "Import";
    }

    /// <summary>
    /// Import log levels
    /// </summary>
    public enum ImportLogLevel
    {
        Info,
        Warning,
        Error,
        Debug,
        Success
    }
}