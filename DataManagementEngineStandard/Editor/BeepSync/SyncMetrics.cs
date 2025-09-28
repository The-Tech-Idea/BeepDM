using System;
using System.Collections.Generic;

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

        public override string ToString()
        {
            return $"SyncMetrics[{SchemaID}]: {SuccessfulRecords}/{TotalRecords} successful ({SuccessRate:F1}%), " +
                   $"Duration: {Duration.TotalMinutes:F1}m";
        }
    }
}