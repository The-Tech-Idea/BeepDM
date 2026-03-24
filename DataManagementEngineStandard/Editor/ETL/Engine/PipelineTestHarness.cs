using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Observability;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Lightweight test runner for pipeline definitions.
    /// Runs pipelines with optional inline source data and evaluates
    /// assertions and quality gates against the result — no live data
    /// connections required when using inline data.
    /// </summary>
    public class PipelineTestHarness
    {
        private readonly PipelineManager _manager;

        public PipelineTestHarness(PipelineManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        /// <summary>
        /// Runs a single test case and returns the evaluated result.
        /// </summary>
        public async Task<TestCaseResult> RunTestAsync(
            TestRunConfig config,
            PipelineDefinition? definitionOverride = null,
            CancellationToken token = default)
        {
            var sw = Stopwatch.StartNew();
            var testResult = new TestCaseResult
            {
                TestId   = config.TestId,
                TestName = config.Name,
                Category = config.Category,
                Outcome  = TestOutcome.Passed
            };

            try
            {
                using var cts = config.Timeout.HasValue
                    ? CancellationTokenSource.CreateLinkedTokenSource(token)
                    : null;
                if (cts != null) cts.CancelAfter(config.Timeout!.Value);
                var effectiveToken = cts?.Token ?? token;

                // Load or use provided definition
                PipelineDefinition def;
                if (definitionOverride != null)
                    def = definitionOverride;
                else
                {
                    var loaded = await _manager.LoadAsync(config.PipelineId);
                    if (loaded == null)
                    {
                        testResult.Outcome = TestOutcome.Failed;
                        testResult.ErrorMessage = $"Pipeline '{config.PipelineId}' not found.";
                        sw.Stop();
                        testResult.Duration = sw.Elapsed;
                        return testResult;
                    }
                    def = loaded;
                }

                // Run the pipeline
                PipelineRunResult runResult;
                if (config.InlineSourceData != null && config.InlineSchemaFields != null)
                {
                    // Inject inline data via override parameters
                    var overrides = new Dictionary<string, object>(config.OverrideParameters)
                    {
                        ["__testHarness_inlineData"]   = config.InlineSourceData,
                        ["__testHarness_schemaFields"] = config.InlineSchemaFields
                    };
                    runResult = await _manager.RunDefinitionAsync(def, null, effectiveToken);
                }
                else
                {
                    runResult = await _manager.RunDefinitionAsync(def, null, effectiveToken);
                }

                // Evaluate assertions
                foreach (var assertion in config.Assertions)
                {
                    var ar = EvaluateAssertion(assertion, runResult);
                    testResult.Assertions.Add(ar);
                    if (!ar.Passed)
                        testResult.Outcome = TestOutcome.Failed;
                }

                // Evaluate quality gates
                var gateEvaluator = new PipelineQualityGate();
                foreach (var gate in config.QualityGates)
                {
                    var eval = gateEvaluator.EvaluateRule(gate, runResult);
                    if (!eval.Passed && eval.Action == GateAction.Fail)
                    {
                        testResult.Outcome = TestOutcome.Failed;
                        testResult.ErrorMessage ??= $"Quality gate failed: {eval.Message}";
                    }
                }

                if (testResult.Outcome == TestOutcome.Passed && runResult.Status == RunStatus.Failed)
                {
                    // Pipeline itself failed and no assertion expected it
                    bool statusAsserted = config.Assertions.Any(a => a.Target == AssertionTarget.Status);
                    if (!statusAsserted)
                    {
                        testResult.Outcome = TestOutcome.Failed;
                        testResult.ErrorMessage = $"Pipeline failed: {runResult.ErrorMessage}";
                    }
                }
            }
            catch (OperationCanceledException) when (config.Timeout.HasValue)
            {
                testResult.Outcome = TestOutcome.Timeout;
                testResult.ErrorMessage = $"Test timed out after {config.Timeout.Value.TotalSeconds:F0}s.";
            }
            catch (Exception ex)
            {
                testResult.Outcome = TestOutcome.Failed;
                testResult.ErrorMessage = ex.Message;
            }

            sw.Stop();
            testResult.Duration = sw.Elapsed;
            return testResult;
        }

        /// <summary>
        /// Runs a suite of test cases and returns aggregated results.
        /// </summary>
        public async Task<TestSuiteResult> RunSuiteAsync(
            string suiteName,
            IReadOnlyList<TestRunConfig> configs,
            CancellationToken token = default)
        {
            var suite = new TestSuiteResult
            {
                SuiteName    = suiteName,
                StartedAtUtc = DateTime.UtcNow
            };

            foreach (var config in configs)
            {
                if (token.IsCancellationRequested)
                {
                    suite.TestCases.Add(new TestCaseResult
                    {
                        TestId   = config.TestId,
                        TestName = config.Name,
                        Outcome  = TestOutcome.Skipped
                    });
                    suite.Skipped++;
                    continue;
                }

                var result = await RunTestAsync(config, token: token);
                suite.TestCases.Add(result);

                switch (result.Outcome)
                {
                    case TestOutcome.Passed:  suite.Passed++;  break;
                    case TestOutcome.Failed:  suite.Failed++;  break;
                    case TestOutcome.Skipped: suite.Skipped++; break;
                    case TestOutcome.Timeout: suite.Failed++;  break;
                }
            }

            suite.FinishedAtUtc = DateTime.UtcNow;
            return suite;
        }

        // ── Assertion evaluation ──────────────────────────────────────────

        private static AssertionResult EvaluateAssertion(TestAssertion assertion, PipelineRunResult result)
        {
            var ar = new AssertionResult { Description = assertion.Description };

            object? actual = assertion.Target switch
            {
                AssertionTarget.Status         => result.Status.ToString(),
                AssertionTarget.RecordsRead    => result.RecordsRead,
                AssertionTarget.RecordsWritten => result.RecordsWritten,
                AssertionTarget.RecordsRejected => result.RecordsRejected,
                AssertionTarget.RecordsWarned  => result.RecordsWarned,
                AssertionTarget.ErrorMessage   => result.ErrorMessage,
                AssertionTarget.DurationMs     => result.Duration?.TotalMilliseconds ?? 0,
                AssertionTarget.StepCount      => result.StepResults.Count,
                _ => null
            };

            ar.ActualValue = actual?.ToString();
            ar.ExpectedValue = assertion.ExpectedValue?.ToString();

            ar.Passed = assertion.Operator switch
            {
                AssertionOperator.Equals             => Equals(actual?.ToString(), assertion.ExpectedValue?.ToString()),
                AssertionOperator.NotEquals           => !Equals(actual?.ToString(), assertion.ExpectedValue?.ToString()),
                AssertionOperator.GreaterThan         => ToDouble(actual) > ToDouble(assertion.ExpectedValue),
                AssertionOperator.GreaterThanOrEqual  => ToDouble(actual) >= ToDouble(assertion.ExpectedValue),
                AssertionOperator.LessThan            => ToDouble(actual) < ToDouble(assertion.ExpectedValue),
                AssertionOperator.LessThanOrEqual     => ToDouble(actual) <= ToDouble(assertion.ExpectedValue),
                AssertionOperator.Contains            => actual?.ToString()?.Contains(assertion.ExpectedValue?.ToString() ?? "", StringComparison.OrdinalIgnoreCase) ?? false,
                AssertionOperator.IsNull              => actual == null,
                AssertionOperator.IsNotNull           => actual != null,
                _ => false
            };

            if (!ar.Passed)
                ar.FailureReason = $"Expected {assertion.Target} {assertion.Operator} '{ar.ExpectedValue}' but got '{ar.ActualValue}'.";

            return ar;
        }

        private static double ToDouble(object? value)
        {
            if (value == null) return 0;
            if (value is double d) return d;
            if (value is long l) return l;
            if (value is int i) return i;
            if (double.TryParse(value.ToString(), out var parsed)) return parsed;
            return 0;
        }
    }
}
