using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Full pipeline specification: source, transformers, sinks, validators,
    /// scheduler, parameters, and execution policy.
    /// Replaces <see cref="ETLScriptHDR"/> — can be constructed from one for backward compatibility.
    /// </summary>
    public class PipelineDefinition
    {
        public string Id          { get; set; } = Guid.NewGuid().ToString();
        public string Name        { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category    { get; set; } = string.Empty;

        /// <summary>Comma-separated tags for filtering in the Designer.</summary>
        public string Tags        { get; set; } = string.Empty;

        public int  Version   { get; set; } = 1;
        public bool IsEnabled { get; set; } = true;

        // ── Connectivity ──────────────────────────────────────────────────
        public string SourcePluginId    { get; set; } = string.Empty;
        public string SinkPluginId      { get; set; } = string.Empty;

        /// <summary>Sink for records that fail validators. Defaults to the built-in error-log sink.</summary>
        public string ErrorSinkPluginId { get; set; } = string.Empty;

        // ── Execution steps (ordered) ─────────────────────────────────────
        public List<PipelineStepDef> Steps { get; set; } = new();

        // ── Parameters ────────────────────────────────────────────────────
        public Dictionary<string, object> Parameters { get; set; } = new();

        // ── Scheduling ────────────────────────────────────────────────────
        public string? SchedulerPluginId { get; set; }
        public Dictionary<string, object> SchedulerParameters { get; set; } = new();

        // ── Execution policy ──────────────────────────────────────────────
        public int  BatchSize              { get; set; } = 500;
        public int  MaxParallelBatches     { get; set; } = 4;
        public int  MaxRetries             { get; set; } = 3;

        /// <summary>Stop the run after this many consecutive errors. 0 means never stop.</summary>
        public int  StopOnErrorCount       { get; set; } = 0;
        public bool EnableCheckpointing    { get; set; } = true;
        public bool EnableLineageTracking  { get; set; } = true;

        // ── Visual layout (Phase 7 — Designer) ────────────────────────────
        /// <summary>JSON serialized node/edge positions for the visual designer canvas.</summary>
        public string? VisualLayoutJson    { get; set; }

        // ── Run history (denormalized for quick display) ──────────────────
        public DateTime? LastRunAt     { get; set; }
        public string?   LastRunStatus { get; set; }
        public string?   LastRunId     { get; set; }

        // ── Backward compat: migrate from ETLScriptHDR ────────────────────
        /// <summary>
        /// Creates a <see cref="PipelineDefinition"/> from a legacy <see cref="ETLScriptHDR"/>.
        /// Preserves all existing step data — nothing is lost.
        /// </summary>
        public static PipelineDefinition FromLegacyScript(ETLScriptHDR hdr)
        {
            var pd = new PipelineDefinition
            {
                Id            = hdr.GuidId ?? Guid.NewGuid().ToString(),
                Name          = hdr.ScriptName,
                LastRunAt     = hdr.LastRunDateTime,
                LastRunId     = hdr.LastRunCorrelationId,
                LastRunStatus = hdr.LastRunSummary
            };

            foreach (var det in hdr.ScriptDetails ?? new())
                pd.Steps.Add(PipelineStepDef.FromLegacyScriptDet(det));

            return pd;
        }
    }
}
