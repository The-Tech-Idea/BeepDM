namespace TheTechIdea.Beep.Editor.Importing
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using TheTechIdea.Beep.DataBase;
    using TheTechIdea.Beep.Editor;
    using TheTechIdea.Beep.ConfigUtil;
    using TheTechIdea.Beep.Report;
    using TheTechIdea.Beep.Workflow.Mapping;

    // ═══════════════════════════════════════════════════════════════════
    //  ENUMS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Defines how a failed batch should be handled.</summary>
    public enum BatchErrorStrategy { Abort, Skip, Retry }

    /// <summary>Lifecycle state of an import run.</summary>
    public enum ImportState { Idle, Running, Paused, Completed, Faulted, Cancelled }

    /// <summary>How the import run handles already-seen records at the destination.</summary>
    public enum SyncMode { FullRefresh, Incremental, Upsert }

    /// <summary>What to do when the source schema changed since the last run.</summary>
    public enum SchemaDriftPolicy { Ignore, AbortOnDrift, AutoAddColumns, AutoDropColumns }

    /// <summary>Import log levels.</summary>
    public enum ImportLogLevel { Info, Warning, Error, Debug, Success }

    /// <summary>What to do when a quality rule fires against a record.</summary>
    public enum DataQualityAction { Block, Quarantine, Warn }

    // ═══════════════════════════════════════════════════════════════════
    //  SHARED POCOs
    // ═══════════════════════════════════════════════════════════════════

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
    /// </summary>
    public class ImportContext
    {
        public string                RunId     { get; set; } = Guid.NewGuid().ToString("N");
        public ImportSelectionContext Selection { get; set; } = new();
        public EntityDataMap?         Mapping  { get; set; }
        public ImportExecutionOptions Options  { get; set; } = new();

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

        public bool IsRunning   { get; set; }
        public bool IsPaused    { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCancelled { get; set; }
        public bool HasErrors   { get; set; }

        public ImportStatus Snapshot() => (ImportStatus)MemberwiseClone();
    }

    /// <summary>Configuration class for data import operations.</summary>
    public class DataImportConfiguration
    {
        public string SourceEntityName { get; set; } = string.Empty;
        public string DestEntityName { get; set; } = string.Empty;
        public string SourceDataSourceName { get; set; } = string.Empty;
        public string DestDataSourceName { get; set; } = string.Empty;
        public EntityStructure? SourceEntityStructure { get; set; }
        public EntityStructure? DestEntityStructure { get; set; }
        public IDataSource? SourceData { get; set; }
        public IDataSource? DestData { get; set; }
        public EntityDataMap? Mapping { get; set; }
        public List<AppFilter> SourceFilters { get; set; } = new();
        public List<string>? SelectedFields { get; set; }
        public List<DefaultValue> DefaultValues { get; set; } = new();
        public Func<object, object>? CustomTransformation { get; set; }
        public int BatchSize { get; set; } = 50;
        public bool CreateDestinationIfNotExists { get; set; } = true;
        public bool ApplyDefaults { get; set; } = true;
        public bool SkipBlanks   { get; set; } = false;
        public bool RunMigrationPreflight   { get; set; }
        public bool AddMissingColumns       { get; set; } = true;
        public bool CreateSyncProfileDraft  { get; set; }
        public BatchErrorStrategy OnBatchError { get; set; } = BatchErrorStrategy.Skip;
        public int  MaxRetries              { get; set; } = 3;
        public SyncMode  SyncMode           { get; set; } = SyncMode.FullRefresh;
        public string    WatermarkColumn    { get; set; } = string.Empty;
        public object?   LastWatermarkValue { get; set; }
        public List<string> UpsertKeyColumns  { get; set; } = new();
        public List<IDataQualityRule> QualityRules { get; set; } = new();
        public SchemaDriftPolicy DriftPolicy { get; set; } = SchemaDriftPolicy.AutoAddColumns;
        public IImportErrorStore? ErrorStore { get; set; }
        public IImportRunHistoryStore? RunHistoryStore { get; set; }
        public StagingOptions? Staging { get; set; }
    }

    /// <summary>Performance metrics for import operations.</summary>
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

    /// <summary>Import log data structure.</summary>
    public class Importlogdata
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Message { get; set; }
        public int RecordNumber { get; set; }
        public ImportLogLevel Level { get; set; } = ImportLogLevel.Info;
        public string Category { get; set; } = "Import";
    }

    /// <summary>Controls whether raw records are written to a staging entity before normalization.</summary>
    public sealed class StagingOptions
    {
        public bool Enabled                  { get; set; } = false;
        public string StagingEntitySuffix    { get; set; } = "_raw";
        public bool DropStagingAfterNormalize { get; set; } = false;
        public bool SkipNormalization        { get; set; } = false;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  QUALITY RULE INTERFACE
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Contract for a single data-quality rule evaluated per-record during import.</summary>
    public interface IDataQualityRule
    {
        string RuleName { get; }
        string FieldName { get; }
        DataQualityAction OnFailure { get; }
        bool Evaluate(object? fieldValue, object record);
        string FailureMessage(object? fieldValue);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ERROR STORE INTERFACES & RECORDS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Full audit record for a single failed / quarantined import record.</summary>
    public sealed class ImportErrorRecord
    {
        public string ContextKey    { get; set; } = string.Empty;
        public DateTime OccurredAt  { get; set; } = DateTime.UtcNow;
        public int BatchNumber      { get; set; }
        public int RecordIndex      { get; set; }
        public string? RuleName     { get; set; }
        public string Reason        { get; set; } = string.Empty;
        public object? RawRecord    { get; set; }
        public bool Replayed        { get; set; }
        public DateTime? ReplayedAt { get; set; }
        public string? TriageNote   { get; set; }
    }

    /// <summary>Abstraction for persisting failed records during import.</summary>
    public interface IImportErrorStore
    {
        Task SaveAsync(ImportErrorRecord record, CancellationToken token = default);
        Task<IReadOnlyList<ImportErrorRecord>> LoadAsync(string contextKey, CancellationToken token = default);
        Task<IReadOnlyList<ImportErrorRecord>> LoadPendingAsync(string contextKey, CancellationToken token = default);
        Task MarkReplayedAsync(string contextKey, int batchNumber, int recordIndex, CancellationToken token = default);
        Task ClearAsync(string contextKey, CancellationToken token = default);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  RUN HISTORY INTERFACES & RECORDS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Full audit record persisted after each import run.</summary>
    public sealed class ImportRunRecord
    {
        public string ContextKey        { get; set; } = string.Empty;
        public string RunId             { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartedAt       { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAt     { get; set; }
        public ImportState FinalState   { get; set; } = ImportState.Idle;
        public SyncMode SyncMode        { get; set; } = SyncMode.FullRefresh;
        public long RecordsRead         { get; set; }
        public long RecordsWritten      { get; set; }
        public long RecordsBlocked      { get; set; }
        public long RecordsQuarantined  { get; set; }
        public long RecordsWarned       { get; set; }
        public int  BatchesProcessed    { get; set; }
        public bool SchemaDriftDetected { get; set; }
        public string? FinalWatermark   { get; set; }
        public string? Summary          { get; set; }
        public List<BatchMetric> BatchMetrics { get; set; } = new();
    }

    /// <summary>Lightweight performance record for a single batch.</summary>
    public sealed class BatchMetric
    {
        public int      BatchNumber { get; set; }
        public int      RecordCount { get; set; }
        public TimeSpan Elapsed     { get; set; }
        public bool     HadErrors   { get; set; }
    }

    /// <summary>Contract for persisting and querying import run history records.</summary>
    public interface IImportRunHistoryStore
    {
        Task SaveRunAsync(ImportRunRecord record, CancellationToken token = default);
        Task<IReadOnlyList<ImportRunRecord>> GetRunsAsync(string contextKey, CancellationToken token = default);
        Task<ImportRunRecord?> GetLastSuccessfulRunAsync(string contextKey, CancellationToken token = default);
        Task ClearAsync(string contextKey, CancellationToken token = default);
    }
}
