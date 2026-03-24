using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor.BeepSync.Interfaces
{
    /// <summary>
    /// Interface for data source operations in sync processes
    /// </summary>
    public interface IDataSourceHelper
    {
        /// <summary>
        /// Get data source by name with validation
        /// </summary>
        IDataSource GetDataSource(string dataSourceName);
        
        /// <summary>
        /// Validate that a data source exists and is accessible
        /// </summary>
        bool ValidateDataSourceConnection(string dataSourceName);
        
        /// <summary>
        /// Get entity data from source with filters
        /// </summary>
        Task<object> GetEntityDataAsync(string dataSourceName, string entityName, List<AppFilter> filters = null);
        
        /// <summary>
        /// Insert entity data into destination
        /// </summary>
        Task<IErrorsInfo> InsertEntityAsync(string dataSourceName, string entityName, object entity);
        
        /// <summary>
        /// Update entity data in destination
        /// </summary>
        Task<IErrorsInfo> UpdateEntityAsync(string dataSourceName, string entityName, object entity);
        
        /// <summary>
        /// Check if entity exists in destination
        /// </summary>
        Task<bool> EntityExistsAsync(string dataSourceName, string entityName, List<AppFilter> filters);
    }

    /// <summary>
    /// Interface for field mapping operations
    /// </summary>
    public interface IFieldMappingHelper
    {
        /// <summary>
        /// Auto-map fields based on name matching between source and destination entity structures.
        /// Use to build schema.MappedFields before a sync run.
        /// </summary>
        List<FieldSyncData> AutoMapFields(string sourceDataSource, string sourceEntity, string destDataSource, string destEntity);

        /// <summary>
        /// Validate field mappings for missing or duplicate field names.
        /// </summary>
        IErrorsInfo ValidateFieldMappings(IEnumerable<FieldSyncData> mappedFields);

        /// <summary>
        /// Return the governed field mappings embedded in <paramref name="schema"/>'s MappedFields.
        /// These are the baseline mappings to compare drift against.
        /// </summary>
        IEnumerable<FieldSyncData> LoadGovernedMapping(DataSyncSchema schema);

        /// <summary>
        /// Returns <c>true</c> when the live <paramref name="schema"/>.MappedFields differ
        /// from the supplied <paramref name="baseline"/> (field-name pairs differ).
        /// </summary>
        bool CheckMappingDrift(DataSyncSchema schema, IEnumerable<FieldSyncData> baseline);

        /// <summary>
        /// Stamps <paramref name="schema"/>'s <see cref="SyncSchemaVersion.ApprovalState"/> with
        /// <paramref name="targetState"/> and logs the change.
        /// </summary>
        IErrorsInfo PromoteMappingState(DataSyncSchema schema, string targetState);
    }

    /// <summary>
    /// Interface for sync validation operations
    /// </summary>
    public interface ISyncValidationHelper
    {
        /// <summary>
        /// Validate complete sync schema
        /// </summary>
        IErrorsInfo ValidateSchema(DataSyncSchema schema);
        
        /// <summary>
        /// Validate data source configuration
        /// </summary>
        IErrorsInfo ValidateDataSource(string dataSourceName);
        
        /// <summary>
        /// Validate entity exists in data source
        /// </summary>
        IErrorsInfo ValidateEntity(string dataSourceName, string entityName);
        
        /// <summary>
        /// Validate sync operation before execution
        /// </summary>
        IErrorsInfo ValidateSyncOperation(DataSyncSchema schema);

        /// <summary>
        /// Validate the <see cref="WatermarkPolicy"/> on <paramref name="schema"/>:
        /// ensures the watermark field exists on the source entity and that the
        /// <see cref="WatermarkPolicy.WatermarkMode"/> is a recognised value.
        /// Returns <see cref="Errors.Ok"/> when the policy is null (full-load mode).
        /// </summary>
        IErrorsInfo ValidateWatermarkPolicy(DataSyncSchema schema);

        // ── Phase 6: DQ gate helpers ──────────────────────────────────────────────

        /// <summary>
        /// Evaluates all DQ gate rules in <paramref name="schema"/>.<see cref="DqPolicy.RuleKeys"/>
        /// against the supplied <paramref name="record"/> dictionary.
        /// Returns a list of <see cref="DqGateResult"/> entries for every rule that failed.
        /// An empty list means all rules passed.
        /// </summary>
        /// <param name="schema">Schema whose <see cref="DqPolicy"/> is evaluated.</param>
        /// <param name="record">Destination record (after mapping and defaults fill) to validate.</param>
        /// <param name="ruleEngine">Optional Rule Engine from the integration context.</param>
        List<DqGateResult> EvaluateDqGateRules(
            DataSyncSchema schema,
            System.Collections.Generic.Dictionary<string, object> record,
            TheTechIdea.Beep.Rules.IRuleEngine ruleEngine = null);

        /// <summary>
        /// Applies the <see cref="IDefaultsManager"/> profile for the destination entity to
        /// <paramref name="record"/>, filling any null/missing fields before DQ evaluation.
        /// Returns the number of fields that were filled.
        /// </summary>
        int FillMissingFieldsWithDefaults(
            DataSyncSchema schema,
            System.Collections.Generic.Dictionary<string, object> record,
            TheTechIdea.Beep.Editor.Defaults.IDefaultsManager defaultsManager);

        /// <summary>
        /// Checks the mapping quality score via the Mapping Manager.  Returns an error when
        /// the score is below <see cref="SyncMappingPolicy.MinQualityScore"/>.
        /// Populates <paramref name="qualityScore"/> and <paramref name="qualityBand"/> so
        /// the caller can stamp the reconciliation report.
        /// </summary>
        IErrorsInfo CheckMappingQualityGate(
            DataSyncSchema schema,
            out int qualityScore,
            out string qualityBand);
    }

    /// <summary>
    /// Interface for sync progress and logging
    /// </summary>
    public interface ISyncProgressHelper
    {
        /// <summary>
        /// Report progress with message
        /// </summary>
        void ReportProgress(IProgress<PassedArgs> progress, string message, int current = 0, int total = 0);

        /// <summary>
        /// Build a <see cref="SyncReconciliationReport"/> from the accumulated run counters.
        /// </summary>
        SyncReconciliationReport BuildReconciliationReport(
            DataSyncSchema schema,
            string runId,
            int sourceRowsScanned,
            int destRowsWritten,
            int destRowsInserted,
            int destRowsUpdated,
            int destRowsSkipped,
            int rejectCount,
            int quarantineCount,
            int defaultsFillCount,
            int conflictCount,
            bool runAbortedByThreshold,
            List<DqGateResult> dqFailures,
            int mappingQualityScore = -1,
            string mappingQualityBand = null,
            List<string> unmappedRequiredFields = null);
        
        /// <summary>
        /// Log sync operation message
        /// </summary>
        void LogMessage(string message, Errors errorLevel = Errors.Ok);
        
        /// <summary>
        /// Log sync run details
        /// </summary>
        void LogSyncRun(DataSyncSchema schema);
        
        /// <summary>
        /// Handle and log sync errors
        /// </summary>
        void LogError(DataSyncSchema schema, string message, Exception ex = null);
        
        /// <summary>
        /// Log cancellation of sync operation
        /// </summary>
        void LogCancellation(DataSyncSchema schema, IProgress<PassedArgs> progress);
        
        /// <summary>
        /// Log successful completion of sync operation
        /// </summary>
        void LogSuccess(DataSyncSchema schema, int recordsProcessed, IProgress<PassedArgs> progress);

        // ── Phase 7: SLO + alerting ───────────────────────────────────────────────

        /// <summary>
        /// Populate Phase-7 observability fields on <paramref name="metrics"/> from the run
        /// context (reject/conflict counts, retry count, mapping plan version, etc.) and
        /// optionally evaluate the <c>sync.slo.classify-run</c> rule to set
        /// <see cref="SyncMetrics.SloComplianceTier"/>.
        /// </summary>
        void EmitSloMetrics(
            DataSyncSchema schema,
            SyncMetrics metrics,
            string runId,
            int rejectCount,
            int conflictCount,
            int retryCount,
            int ruleEvaluationCount,
            string mappingPlanVersion,
            bool mappingDriftDetected,
            IRuleEngine ruleEngine = null);

        /// <summary>
        /// Evaluates each alert rule key from <see cref="SloProfile.AlertRuleKeys"/> against
        /// <paramref name="metrics"/> and returns the <see cref="SyncAlertRecord"/> list for
        /// any rules that fired.  Never throws; errors are logged.
        /// </summary>
        List<SyncAlertRecord> EvaluateAlertRules(
            DataSyncSchema schema,
            SyncMetrics metrics,
            IRuleEngine ruleEngine);

        /// <summary>
        /// Builds a <see cref="SyncAlertRecord"/> from a fired alert rule result.
        /// </summary>
        SyncAlertRecord BuildAlertPayload(
            DataSyncSchema schema,
            SyncMetrics metrics,
            string ruleKey,
            Dictionary<string, object> ruleOutputs);
    }

    /// <summary>
    /// Interface for sync schema persistence
    /// </summary>
    public interface ISchemaPersistenceHelper
    {
        /// <summary>
        /// Save sync schemas to storage
        /// </summary>
        Task SaveSchemasAsync(IEnumerable<DataSyncSchema> schemas);
        
        /// <summary>
        /// Load sync schemas from storage
        /// </summary>
        Task<ObservableBindingList<DataSyncSchema>> LoadSchemasAsync();
        
        /// <summary>
        /// Save single schema
        /// </summary>
        Task SaveSchemaAsync(DataSyncSchema schema);
        
        /// <summary>
        /// Delete schema from storage
        /// </summary>
        Task DeleteSchemaAsync(string schemaId);

        /// <summary>
        /// Persist a versioned snapshot of the schema alongside the main schema store.
        /// Creates a per-schema version directory if it does not already exist.
        /// </summary>
        Task SaveVersionedSchemaAsync(DataSyncSchema schema, SyncSchemaVersion version);

        /// <summary>
        /// Return all stored version artifacts for a schema, newest first.
        /// Returns an empty list when no versions have been saved yet.
        /// </summary>
        Task<List<SyncSchemaVersion>> LoadSchemaVersionsAsync(string schemaId);

        /// <summary>
        /// Compare the live schema against its most recently persisted version.
        /// Returns an empty string when unchanged, or a human-readable diff summary.
        /// </summary>
        Task<string> DiffSchemaToPersistedAsync(DataSyncSchema schema);

        // ── Phase 5: Checkpoint persistence ──────────────────────────────────────

        /// <summary>
        /// Persist a <see cref="SyncCheckpoint"/> for the given schema so a retry
        /// can resume from the last known good offset.
        /// Stored at <c>{directoryPath}/checkpoints/{checkpoint.SchemaId}.json</c>.
        /// </summary>
        Task SaveCheckpointAsync(SyncCheckpoint checkpoint);

        /// <summary>
        /// Load the most recently saved checkpoint for <paramref name="schemaId"/>.
        /// Returns <c>null</c> when no checkpoint file exists.
        /// </summary>
        Task<SyncCheckpoint> LoadCheckpointAsync(string schemaId);

        /// <summary>
        /// Delete the checkpoint file for <paramref name="schemaId"/> once a run
        /// has completed successfully.
        /// </summary>
        Task ClearCheckpointAsync(string schemaId);
    }
}
