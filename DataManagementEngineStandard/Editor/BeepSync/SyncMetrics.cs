using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Importing.History;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Represents synchronization metrics for monitoring sync operations
    /// </summary>
    public class SyncMetrics
    {
        /// <summary>
        /// Unique identifier for the sync schema
        /// </summary>
        public string SchemaID { get; set; }

        /// <summary>
        /// Date and time when the sync operation started
        /// </summary>
        public DateTime SyncDate { get; set; }

        /// <summary>
        /// Total number of records to be synchronized
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Number of records successfully synchronized
        /// </summary>
        public int SuccessfulRecords { get; set; }

        /// <summary>
        /// Number of records that failed to synchronize
        /// </summary>
        public int FailedRecords { get; set; }

        /// <summary>
        /// Number of records that were skipped during synchronization
        /// </summary>
        public int SkippedRecords { get; set; }

        /// <summary>
        /// Number of records inserted during synchronization
        /// </summary>
        public int RecordsInserted { get; set; }

        /// <summary>
        /// Number of records updated during synchronization
        /// </summary>
        public int RecordsUpdated { get; set; }

        /// <summary>
        /// Number of records deleted during synchronization
        /// </summary>
        public int RecordsDeleted { get; set; }

        /// <summary>
        /// Duration of the sync operation
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Average processing time per record
        /// </summary>
        public TimeSpan AverageRecordProcessingTime 
        { 
            get 
            {
                if (TotalRecords > 0)
                    return TimeSpan.FromTicks(Duration.Ticks / TotalRecords);
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Success rate as a percentage
        /// </summary>
        public double SuccessRate
        {
            get
            {
                if (TotalRecords > 0)
                    return (double)SuccessfulRecords / TotalRecords * 100;
                return 0;
            }
        }

        /// <summary>
        /// Indicates if the sync operation completed successfully (no failures)
        /// </summary>
        public bool IsSuccessful => FailedRecords == 0 && TotalRecords > 0;

        /// <summary>
        /// Error messages collected during synchronization
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new List<string>();

        /// <summary>
        /// Additional metadata about the sync operation
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        // ── Phase 7: Observability fields ─────────────────────────────────────────

        /// <summary>Mapping governance version used during this run.</summary>
        public string MappingPlanVersion { get; set; }

        /// <summary>
        /// Composite correlation ID: <c>{SchemaID}.{RunId}.{MappingPlanVersion}</c>.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>Fraction of records rejected by the DQ gate (0.0–1.0).</summary>
        public double RejectRate { get; set; }

        /// <summary>Fraction of records that triggered a conflict (0.0–1.0).</summary>
        public double ConflictRate { get; set; }

        /// <summary>Seconds elapsed since the source data was last modified (freshness lag).</summary>
        public double FreshnessLagSeconds { get; set; }

        /// <summary>Number of retry attempts made during the run (0 = first attempt succeeded).</summary>
        public int RetryCount { get; set; }

        /// <summary>Total number of rule evaluations recorded via <c>RuleEngine.RuleEvaluated</c>.</summary>
        public int RuleEvaluationCount { get; set; }

        /// <summary>SLO compliance tier computed at the end of the run: <c>Green</c>, <c>Yellow</c>, or <c>Red</c>.</summary>
        public string SloComplianceTier { get; set; }

        /// <summary>When <c>true</c>, mapping drift was detected during preflight but the run was allowed to proceed.</summary>
        public bool MappingDriftDetected { get; set; }

        /// <summary>
        /// Creates a SyncMetrics instance from a completed ImportRunRecord.
        /// </summary>
        public static SyncMetrics FromImportRunRecord(ImportRunRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            return new SyncMetrics
            {
                SchemaID          = record.ContextKey,
                SyncDate          = record.StartedAt,
                TotalRecords      = (int)record.RecordsRead,
                SuccessfulRecords = (int)record.RecordsWritten,
                FailedRecords     = (int)record.RecordsBlocked,
                Duration          = record.FinishedAt.HasValue
                                        ? record.FinishedAt.Value - record.StartedAt
                                        : TimeSpan.Zero
            };
        }

        public override string ToString()
        {
            return $"SyncMetrics[{SchemaID}]: {SuccessfulRecords}/{TotalRecords} successful ({SuccessRate:F1}%), " +
                   $"Duration: {Duration.TotalMinutes:F1}m";
        }
    }
}