using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Immutable-ish bag of runtime state passed to every plugin during a pipeline run.
    /// Carries correlation ID, resolved services, parameters, and accumulated telemetry.
    /// Created by the engine at the start of each run and disposed when the run ends.
    /// </summary>
    public class PipelineRunContext
    {
        /// <summary>Unique identifier for this specific run instance.</summary>
        public string RunId { get; } = Guid.NewGuid().ToString();

        /// <summary>ID of the pipeline definition being executed.</summary>
        public string PipelineId { get; init; } = string.Empty;

        /// <summary>Display name of the pipeline for progress messages.</summary>
        public string PipelineName { get; init; } = string.Empty;

        /// <summary>UTC time this run started.</summary>
        public DateTime StartedAtUtc { get; } = DateTime.UtcNow;

        /// <summary>Central BeepDM services: data sources, config, logging, etc.</summary>
        public IDMEEditor DMEEditor { get; init; } = null!;

        /// <summary>Progress reporter — call <see cref="ReportProgress"/> instead of using directly.</summary>
        public IProgress<PassedArgs> Progress { get; init; } = new Progress<PassedArgs>();

        /// <summary>Cancellation token signalled when the caller requests a stop.</summary>
        public CancellationToken Token { get; init; }

        /// <summary>
        /// Resolved parameters: pipeline definition defaults merged with trigger-supplied overrides.
        /// </summary>
        public IReadOnlyDictionary<string, object> Parameters { get; init; }
            = new Dictionary<string, object>();

        /// <summary>
        /// Shared state bag for cross-step communication.
        /// Plugins may write to this to pass data to later steps.
        /// </summary>
        public Dictionary<string, object> RuntimeState { get; } = new();

        // ── Telemetry counters (updated by the engine) ────────────────────
        public long TotalRecordsRead     { get; set; }
        public long TotalRecordsWritten  { get; set; }
        public long TotalRecordsRejected { get; set; }
        public long TotalRecordsWarned   { get; set; }
        public long TotalBytesProcessed  { get; set; }
        public int  StepsCompleted       { get; set; }
        public int  StepsFailed          { get; set; }

        /// <summary>Last saved checkpoint ID for resumable runs.</summary>
        public string? CheckpointId      { get; set; }

        // ── Lineage (populated when EnableLineageTracking = true) ─────────
        public List<DataLineageRecord> LineageEntries { get; } = new();

        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Emits a progress update to the calling application.
        /// Can be called by any plugin without blocking the data stream.
        /// </summary>
        /// <param name="message">Human-readable status message.</param>
        /// <param name="pct">Optional completion percentage (0-100). Use -1 if unknown.</param>
        public void ReportProgress(string message, int pct = -1)
        {
            Progress?.Report(new PassedArgs { Messege = message, ParameterInt1 = pct });
        }
    }
}
