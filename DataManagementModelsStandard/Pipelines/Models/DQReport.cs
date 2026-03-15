using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Aggregated data-quality report produced by the validation layer after a pipeline run.
    /// One <see cref="DQReport"/> is appended to <see cref="PipelineRunResult"/> when
    /// at least one validator step is present in the pipeline definition.
    /// </summary>
    public class DQReport
    {
        /// <summary>Correlation identifier of the pipeline run this report belongs to.</summary>
        public string RunId { get; init; } = string.Empty;

        public string PipelineId   { get; init; } = string.Empty;
        public string PipelineName { get; init; } = string.Empty;

        /// <summary>UTC timestamp when the report was assembled.</summary>
        public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;

        // ── Record-level counters ─────────────────────────────────────────

        public long TotalRecords    { get; set; }
        public long PassedRecords   { get; set; }
        public long WarnedRecords   { get; set; }
        public long RejectedRecords { get; set; }

        /// <summary>
        /// Percentage of records that passed all validation rules (0–100).
        /// Returns 0 when <see cref="TotalRecords"/> is 0 (no records processed yet).
        /// </summary>
        public double PassRate =>
            TotalRecords == 0 ? 0.0 : (double)PassedRecords / TotalRecords * 100.0;

        // ── Per-rule breakdown ────────────────────────────────────────────

        /// <summary>
        /// One entry per validator + rule combination that fired during the run.
        /// Empty when all records pass all rules.
        /// </summary>
        public List<DQRuleResult> RuleResults { get; init; } = new();
    }
}
