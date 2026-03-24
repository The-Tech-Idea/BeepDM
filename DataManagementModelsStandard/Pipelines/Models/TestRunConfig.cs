using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Configuration for a pipeline test run executed through <c>PipelineTestHarness</c>.
    /// Allows callers to define synthetic source data, expected outcomes, and quality gates
    /// — all without requiring a live data source or sink connection.
    /// </summary>
    public class TestRunConfig
    {
        /// <summary>Unique test run identifier.</summary>
        public string TestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Human-readable name for this test scenario.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional description of what the test validates.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>The pipeline definition to test.</summary>
        public string PipelineId { get; set; } = string.Empty;

        /// <summary>Override parameters injected into the test run.</summary>
        public Dictionary<string, object> OverrideParameters { get; set; } = new();

        /// <summary>
        /// When set, the harness uses these records as the source stream instead of
        /// resolving the configured <c>SourcePluginId</c>.
        /// </summary>
        public List<Dictionary<string, object?>>? InlineSourceData { get; set; }

        /// <summary>Schema field names for inline source data. Required when <see cref="InlineSourceData"/> is set.</summary>
        public List<string>? InlineSchemaFields { get; set; }

        /// <summary>Assertions evaluated after the run completes.</summary>
        public List<TestAssertion> Assertions { get; set; } = new();

        /// <summary>Quality gates that must pass for the test to succeed.</summary>
        public List<QualityGateRule> QualityGates { get; set; } = new();

        /// <summary>Maximum allowed duration before the test run is cancelled.</summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>Category tag for grouping tests (e.g. "smoke", "regression", "performance").</summary>
        public string Category { get; set; } = "unit";
    }

    /// <summary>
    /// A single assertion evaluated against the <see cref="PipelineRunResult"/>.
    /// </summary>
    public class TestAssertion
    {
        /// <summary>What aspect of the result to check.</summary>
        public AssertionTarget Target { get; set; }

        /// <summary>Operator for comparison.</summary>
        public AssertionOperator Operator { get; set; } = AssertionOperator.Equals;

        /// <summary>Expected value (type depends on target).</summary>
        public object? ExpectedValue { get; set; }

        /// <summary>Human-readable description of this assertion.</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>Aspect of a pipeline run result targeted by an assertion.</summary>
    public enum AssertionTarget
    {
        Status,
        RecordsRead,
        RecordsWritten,
        RecordsRejected,
        RecordsWarned,
        ErrorMessage,
        DurationMs,
        StepCount
    }

    /// <summary>Comparison operator for test assertions.</summary>
    public enum AssertionOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        IsNull,
        IsNotNull
    }
}
