using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Defines resource presets for a workload class.
    /// Profiles control batch sizes, concurrency, memory caps, and retry
    /// behaviour so that operators can assign a class to a pipeline and
    /// have sensible defaults applied automatically.
    /// </summary>
    public class WorkloadProfile
    {
        /// <summary>Workload class name: "critical", "standard", "backfill".</summary>
        public string ClassName { get; set; } = "standard";

        /// <summary>Default batch size for pipelines in this class.</summary>
        public int BatchSize { get; set; } = 500;

        /// <summary>Maximum parallel batches within a single pipeline run.</summary>
        public int MaxParallelBatches { get; set; } = 4;

        /// <summary>Maximum concurrent runs of the same pipeline.</summary>
        public int MaxConcurrentRuns { get; set; } = 2;

        /// <summary>
        /// Soft memory cap in MB for in-memory transforms (aggregate, dedup).
        /// When exceeded, transforms should apply their overflow strategy.
        /// 0 = no limit.
        /// </summary>
        public int MemoryCapMB { get; set; }

        /// <summary>Max retry attempts on failure.</summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>Base retry delay in milliseconds.</summary>
        public int RetryBaseDelayMs { get; set; } = 5000;

        /// <summary>
        /// Cost budget per run in abstract cost units.
        /// 0 = no budget enforcement.
        /// </summary>
        public double CostBudgetPerRun { get; set; }

        /// <summary>Returns the built-in profiles keyed by class name.</summary>
        public static IReadOnlyDictionary<string, WorkloadProfile> Defaults { get; }
            = new Dictionary<string, WorkloadProfile>
            {
                ["critical"] = new WorkloadProfile
                {
                    ClassName          = "critical",
                    BatchSize          = 200,
                    MaxParallelBatches = 2,
                    MaxConcurrentRuns  = 1,
                    MemoryCapMB        = 0,       // no cap — correctness first
                    MaxRetries         = 5,
                    RetryBaseDelayMs   = 2000,
                    CostBudgetPerRun   = 0
                },
                ["standard"] = new WorkloadProfile
                {
                    ClassName          = "standard",
                    BatchSize          = 500,
                    MaxParallelBatches = 4,
                    MaxConcurrentRuns  = 2,
                    MemoryCapMB        = 512,
                    MaxRetries         = 3,
                    RetryBaseDelayMs   = 5000,
                    CostBudgetPerRun   = 0
                },
                ["backfill"] = new WorkloadProfile
                {
                    ClassName          = "backfill",
                    BatchSize          = 2000,
                    MaxParallelBatches = 8,
                    MaxConcurrentRuns  = 2,
                    MemoryCapMB        = 1024,
                    MaxRetries         = 1,
                    RetryBaseDelayMs   = 30000,
                    CostBudgetPerRun   = 0
                }
            };

        /// <summary>
        /// Resolves a profile by class name, falling back to "standard" if not found.
        /// </summary>
        public static WorkloadProfile Resolve(string? className)
        {
            if (className != null && Defaults.TryGetValue(className, out var profile))
                return profile;
            return Defaults["standard"];
        }
    }
}
