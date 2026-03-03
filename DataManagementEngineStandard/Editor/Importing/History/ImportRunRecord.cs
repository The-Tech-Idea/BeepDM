using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Importing.Interfaces;

namespace TheTechIdea.Beep.Editor.Importing.History
{
    /// <summary>Full audit record persisted after each import run.</summary>
    public sealed class ImportRunRecord
    {
        /// <summary>Pipeline key: "{sourceDS}/{sourceEntity}/{destDS}/{destEntity}".</summary>
        public string ContextKey        { get; set; } = string.Empty;

        /// <summary>Unique run identifier (GUID string).</summary>
        public string RunId             { get; set; } = Guid.NewGuid().ToString();

        /// <summary>UTC start timestamp.</summary>
        public DateTime StartedAt       { get; set; } = DateTime.UtcNow;

        /// <summary>UTC end timestamp (null if still running).</summary>
        public DateTime? FinishedAt     { get; set; }

        /// <summary>Terminal state of the import run.</summary>
        public ImportState FinalState   { get; set; } = ImportState.Idle;

        /// <summary>Sync mode used for this run.</summary>
        public SyncMode SyncMode        { get; set; } = SyncMode.FullRefresh;

        /// <summary>Total records read from the source.</summary>
        public long RecordsRead         { get; set; }

        /// <summary>Records successfully written to the destination.</summary>
        public long RecordsWritten      { get; set; }

        /// <summary>Records blocked by quality rules.</summary>
        public long RecordsBlocked      { get; set; }

        /// <summary>Records quarantined by quality rules.</summary>
        public long RecordsQuarantined  { get; set; }

        /// <summary>Records that triggered a warning quality rule.</summary>
        public long RecordsWarned       { get; set; }

        /// <summary>Number of batches processed.</summary>
        public int BatchesProcessed     { get; set; }

        /// <summary>Whether a schema drift was detected.</summary>
        public bool SchemaDriftDetected { get; set; }

        /// <summary>The watermark value at the end of a successful incremental run (serialised as string).</summary>
        public string? FinalWatermark   { get; set; }

        /// <summary>Human-readable summary or error message.</summary>
        public string? Summary          { get; set; }

        /// <summary>Per-batch performance metrics (optional — only populated when detailed tracking is enabled).</summary>
        public List<BatchMetric> BatchMetrics { get; set; } = new();
    }

    /// <summary>Lightweight performance record for a single batch.</summary>
    public sealed class BatchMetric
    {
        public int      BatchNumber     { get; set; }
        public int      RecordCount     { get; set; }
        public TimeSpan Elapsed         { get; set; }
        public bool     HadErrors       { get; set; }
    }
}
