using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Defines a quality gate rule evaluated against a <see cref="PipelineRunResult"/>.
    /// Used by both <c>PipelineTestHarness</c> and <c>PipelineQualityGate</c> for CI/CD gating.
    /// </summary>
    public class QualityGateRule
    {
        /// <summary>Unique identifier for this gate rule.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Human-readable name (e.g. "Max reject rate").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The metric to evaluate.</summary>
        public GateMetric Metric { get; set; }

        /// <summary>Comparison operator.</summary>
        public GateOperator Operator { get; set; } = GateOperator.LessThanOrEqual;

        /// <summary>Threshold value. Type depends on <see cref="Metric"/>.</summary>
        public double Threshold { get; set; }

        /// <summary>What happens when the gate fails.</summary>
        public GateAction OnFailure { get; set; } = GateAction.Fail;

        /// <summary>Optional description of what this gate protects against.</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>Metric extracted from a pipeline run result for quality gate evaluation.</summary>
    public enum GateMetric
    {
        /// <summary>Percentage of records rejected (0-100).</summary>
        RejectRatePercent,
        /// <summary>Percentage of records warned (0-100).</summary>
        WarnRatePercent,
        /// <summary>Total duration in milliseconds.</summary>
        DurationMs,
        /// <summary>Absolute number of rejected records.</summary>
        RecordsRejected,
        /// <summary>Absolute number of records that must be written.</summary>
        MinRecordsWritten,
        /// <summary>Total bytes processed.</summary>
        BytesProcessed,
        /// <summary>Number of failed steps.</summary>
        FailedStepCount
    }

    /// <summary>Comparison operator for gate evaluation.</summary>
    public enum GateOperator
    {
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        Equals
    }

    /// <summary>Action taken when a quality gate rule fails.</summary>
    public enum GateAction
    {
        /// <summary>Fail the test / block the release.</summary>
        Fail,
        /// <summary>Emit a warning but allow proceeding.</summary>
        Warn,
        /// <summary>Log only, no action.</summary>
        Log
    }

    /// <summary>Result of evaluating a single quality gate rule.</summary>
    public class GateEvaluation
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public double ActualValue { get; set; }
        public double Threshold { get; set; }
        public GateAction Action { get; set; }
        public string? Message { get; set; }
    }
}
