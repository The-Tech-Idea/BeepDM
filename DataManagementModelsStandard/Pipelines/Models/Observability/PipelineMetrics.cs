using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// Aggregated KPI summary for a pipeline over a time window.
    /// Computed by <c>MetricsEngine</c> from stored <see cref="PipelineRunLog"/> records.
    /// </summary>
    public class PipelineMetrics
    {
        public string   PipelineId   { get; set; } = string.Empty;
        public string   PipelineName { get; set; } = string.Empty;
        public DateTime PeriodStart  { get; set; }
        public DateTime PeriodEnd    { get; set; }

        // ── Run counts ───────────────────────────────────────────────────────
        public int    TotalRuns       { get; set; }
        public int    SuccessfulRuns  { get; set; }
        public int    FailedRuns      { get; set; }
        public int    CancelledRuns   { get; set; }
        public double SuccessRate     => TotalRuns == 0 ? 0.0 : (double)SuccessfulRuns / TotalRuns;

        // ── Latency ──────────────────────────────────────────────────────────
        public TimeSpan AvgDuration  { get; set; }
        public TimeSpan MinDuration  { get; set; }
        public TimeSpan MaxDuration  { get; set; }
        /// <summary>95th-percentile run duration.</summary>
        public TimeSpan P95Duration  { get; set; }

        // ── Throughput ───────────────────────────────────────────────────────
        public long   TotalRecordsProcessed { get; set; }
        public double AvgRowsPerSecond      { get; set; }
        public long   TotalBytesProcessed   { get; set; }

        // ── Data Quality ─────────────────────────────────────────────────────
        public double AvgDQPassRate { get; set; }
        public long   TotalRejected { get; set; }
        public long   TotalWarned   { get; set; }

        // ── Cost & resource ─────────────────────────────────────────────────
        /// <summary>Total estimated cost units across all runs in the period.</summary>
        public double TotalCostUnits { get; set; }
        /// <summary>Average cost per run.</summary>
        public double AvgCostPerRun  { get; set; }
        /// <summary>Maximum peak memory (bytes) among all runs in the period.</summary>
        public long MaxMemoryPeakBytes { get; set; }
        /// <summary>Average peak memory (bytes) per run.</summary>
        public long AvgMemoryPeakBytes { get; set; }
        /// <summary>Workload class for per-class breakdown queries.</summary>
        public string WorkloadClass { get; set; } = string.Empty;

        // ── Time-series ──────────────────────────────────────────────────────
        /// <summary>Daily run counts over the period.</summary>
        public List<MetricDataPoint> RunsOverTime { get; set; } = new();
        /// <summary>Daily rows-processed totals.</summary>
        public List<MetricDataPoint> RowsOverTime { get; set; } = new();
        /// <summary>Daily cost units.</summary>
        public List<MetricDataPoint> CostOverTime { get; set; } = new();
        /// <summary>Most frequent error messages across runs.</summary>
        public List<string> TopErrors { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>A single time-series data point used in metric charts.</summary>
    public record MetricDataPoint(DateTime DateUtc, double Value, string Label = "");
}
