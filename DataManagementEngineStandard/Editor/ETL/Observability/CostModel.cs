using System;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// Rate-card based cost estimator for pipeline runs.
    /// Each run's cost = (bytesProcessed × <see cref="CostPerByte"/>) +
    ///                    (durationSeconds × <see cref="CostPerSecond"/>) ×
    ///                    workload-class multiplier.
    /// </summary>
    public sealed class CostModel
    {
        /// <summary>Cost units per byte processed.</summary>
        public double CostPerByte { get; set; } = 0.000_000_01;

        /// <summary>Cost units per second of wall-clock execution.</summary>
        public double CostPerSecond { get; set; } = 0.001;

        /// <summary>Multiplier for critical-class pipelines.</summary>
        public double CriticalMultiplier  { get; set; } = 2.0;

        /// <summary>Multiplier for standard-class pipelines.</summary>
        public double StandardMultiplier  { get; set; } = 1.0;

        /// <summary>Multiplier for backfill-class pipelines.</summary>
        public double BackfillMultiplier  { get; set; } = 0.5;

        /// <summary>Default instance using baseline rates.</summary>
        public static CostModel Default { get; } = new();

        /// <summary>
        /// Estimates cost units for a completed pipeline run.
        /// </summary>
        public double Estimate(PipelineRunLog runLog)
        {
            if (runLog == null) return 0;
            double durationSec = runLog.FinishedAtUtc > runLog.StartedAtUtc
                ? (runLog.FinishedAtUtc - runLog.StartedAtUtc).TotalSeconds
                : 0;

            double baseCost = (runLog.BytesProcessed * CostPerByte)
                            + (durationSec * CostPerSecond);

            double multiplier = ResolveMultiplier(runLog.WorkloadClass);
            return Math.Round(baseCost * multiplier, 6);
        }

        /// <summary>
        /// Estimates cost units from raw values (useful for live tracking).
        /// </summary>
        public double Estimate(long bytesProcessed, double durationSeconds, string? workloadClass)
        {
            double baseCost = (bytesProcessed * CostPerByte)
                            + (durationSeconds * CostPerSecond);
            return Math.Round(baseCost * ResolveMultiplier(workloadClass), 6);
        }

        private double ResolveMultiplier(string? workloadClass)
        {
            if (string.IsNullOrEmpty(workloadClass))
                return StandardMultiplier;

            return workloadClass.ToLowerInvariant() switch
            {
                "critical" => CriticalMultiplier,
                "backfill" => BackfillMultiplier,
                _          => StandardMultiplier
            };
        }
    }
}
