using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Evidence bundle for a pipeline release — encapsulates test evidence, quality gate results,
    /// and rollback readiness. Created by <c>ReleaseManager</c> and persisted for audit.
    /// </summary>
    public class ReleaseManifest
    {
        /// <summary>Unique release identifier.</summary>
        public string ReleaseId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Pipeline definition ID being released.</summary>
        public string PipelineId { get; set; } = string.Empty;

        /// <summary>Pipeline name for display.</summary>
        public string PipelineName { get; set; } = string.Empty;

        /// <summary>Version of the pipeline definition at release time.</summary>
        public int PipelineVersion { get; set; }

        /// <summary>Target environment (e.g. "dev", "staging", "production").</summary>
        public string TargetEnvironment { get; set; } = string.Empty;

        /// <summary>Person or service that initiated the release.</summary>
        public string? ReleasedBy { get; set; }

        /// <summary>When the release was created.</summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Overall release status.</summary>
        public ReleaseStatus Status { get; set; } = ReleaseStatus.Pending;

        // ── Test evidence ──────────────────────────────────────────────────
        /// <summary>Test suite results included as evidence.</summary>
        public List<TestSuiteResult> TestEvidence { get; set; } = new();

        // ── Quality gates ──────────────────────────────────────────────────
        /// <summary>Quality gate evaluations from the most recent validation run.</summary>
        public List<GateEvaluation> GateResults { get; set; } = new();

        /// <summary>True when all blocking quality gates passed.</summary>
        public bool AllGatesPassed { get; set; }

        // ── Rollback proof ─────────────────────────────────────────────────
        /// <summary>Serialized snapshot of the previous pipeline definition for rollback.</summary>
        public string? PreviousDefinitionJson { get; set; }

        /// <summary>Run ID of the rollback validation test (proving rollback works).</summary>
        public string? RollbackTestRunId { get; set; }

        /// <summary>True when rollback has been tested successfully.</summary>
        public bool RollbackVerified { get; set; }

        // ── Sign-off ───────────────────────────────────────────────────────
        /// <summary>Risk assessment notes from the release approver.</summary>
        public string? RiskNotes { get; set; }

        /// <summary>Person who approved the release (null if auto-approved by gates).</summary>
        public string? ApprovedBy { get; set; }

        /// <summary>When the release was approved.</summary>
        public DateTime? ApprovedAtUtc { get; set; }

        /// <summary>Tags for filtering and compliance (e.g. "hotfix", "scheduled", "GDPR").</summary>
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>Lifecycle status of a release.</summary>
    public enum ReleaseStatus
    {
        /// <summary>Release created but not yet validated.</summary>
        Pending,
        /// <summary>Quality gates are being evaluated.</summary>
        Validating,
        /// <summary>All gates passed — ready for approval.</summary>
        Ready,
        /// <summary>Approved and promoted to target environment.</summary>
        Promoted,
        /// <summary>Validation failed — release blocked.</summary>
        Failed,
        /// <summary>Release was rolled back after promotion.</summary>
        RolledBack
    }

    /// <summary>
    /// Aggregated result of running a test suite (one or more <see cref="TestRunConfig"/>).
    /// </summary>
    public class TestSuiteResult
    {
        /// <summary>Suite identifier.</summary>
        public string SuiteId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Suite name (e.g. "smoke", "regression", "performance").</summary>
        public string SuiteName { get; set; } = string.Empty;

        /// <summary>When the suite started.</summary>
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>When the suite finished.</summary>
        public DateTime? FinishedAtUtc { get; set; }

        /// <summary>Individual test results.</summary>
        public List<TestCaseResult> TestCases { get; set; } = new();

        /// <summary>Total tests run.</summary>
        public int TotalTests => TestCases.Count;

        /// <summary>Count of passed tests.</summary>
        public int Passed { get; set; }

        /// <summary>Count of failed tests.</summary>
        public int Failed { get; set; }

        /// <summary>Count of skipped tests.</summary>
        public int Skipped { get; set; }

        /// <summary>Suite-level pass/fail.</summary>
        public bool AllPassed => Failed == 0;
    }

    /// <summary>Result of a single test case within a suite.</summary>
    public class TestCaseResult
    {
        /// <summary>Test ID from <see cref="TestRunConfig.TestId"/>.</summary>
        public string TestId { get; set; } = string.Empty;

        /// <summary>Test name.</summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>Category (smoke, unit, integration, performance).</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Outcome of this test case.</summary>
        public TestOutcome Outcome { get; set; }

        /// <summary>Duration of the test run.</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>Error message if the test failed.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Assertion results.</summary>
        public List<AssertionResult> Assertions { get; set; } = new();
    }

    /// <summary>Outcome of a test case.</summary>
    public enum TestOutcome
    {
        Passed,
        Failed,
        Skipped,
        Timeout
    }

    /// <summary>Result of evaluating a single <see cref="TestAssertion"/>.</summary>
    public class AssertionResult
    {
        public string Description { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? ActualValue { get; set; }
        public string? ExpectedValue { get; set; }
        public string? FailureReason { get; set; }
    }
}
