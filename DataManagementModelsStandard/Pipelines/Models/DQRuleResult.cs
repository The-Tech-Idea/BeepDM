using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Per-rule quality statistics accumulated across all records in a pipeline run.
    /// Aggregated into <see cref="DQReport.RuleResults"/>.
    /// </summary>
    public class DQRuleResult
    {
        /// <summary>Human-readable rule name supplied by the validator configuration.</summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>Plugin ID of the validator that produced this result.</summary>
        public string ValidatorPluginId { get; set; } = string.Empty;

        /// <summary>Number of records that received a <c>Reject</c> outcome from this rule.</summary>
        public long FailCount { get; set; }

        /// <summary>Number of records that received a <c>Warn</c> outcome from this rule.</summary>
        public long WarnCount { get; set; }

        /// <summary>
        /// Up to 100 sample records that triggered a Warn or Reject outcome.
        /// Useful for diagnosing data problems without trawling the full error sink.
        /// </summary>
        public List<PipelineRecord> SampleFailures { get; init; } = new();
    }
}
