using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Evaluates <see cref="QualityGateRule"/> instances against a <see cref="PipelineRunResult"/>.
    /// Used by <see cref="PipelineTestHarness"/> for test-time gates and by
    /// <see cref="ReleaseManager"/> for promotion gates.
    /// </summary>
    public class PipelineQualityGate
    {
        /// <summary>
        /// Evaluates all gate rules against the given result.
        /// Returns an evaluation per rule, plus an overall pass/fail flag.
        /// </summary>
        public (IReadOnlyList<GateEvaluation> Evaluations, bool AllBlockingPassed) EvaluateAll(
            IReadOnlyList<QualityGateRule> rules,
            PipelineRunResult result)
        {
            var evaluations = new List<GateEvaluation>(rules.Count);
            bool allBlockingPassed = true;

            foreach (var rule in rules)
            {
                var eval = EvaluateRule(rule, result);
                evaluations.Add(eval);
                if (!eval.Passed && eval.Action == GateAction.Fail)
                    allBlockingPassed = false;
            }

            return (evaluations, allBlockingPassed);
        }

        /// <summary>
        /// Evaluates a single quality gate rule.
        /// </summary>
        public GateEvaluation EvaluateRule(QualityGateRule rule, PipelineRunResult result)
        {
            double actual = ExtractMetric(rule.Metric, result);

            bool passed = rule.Operator switch
            {
                GateOperator.LessThan            => actual < rule.Threshold,
                GateOperator.LessThanOrEqual     => actual <= rule.Threshold,
                GateOperator.GreaterThan         => actual > rule.Threshold,
                GateOperator.GreaterThanOrEqual  => actual >= rule.Threshold,
                GateOperator.Equals              => Math.Abs(actual - rule.Threshold) < 0.001,
                _ => false
            };

            return new GateEvaluation
            {
                RuleId    = rule.Id,
                RuleName  = rule.Name,
                Passed    = passed,
                ActualValue = actual,
                Threshold = rule.Threshold,
                Action    = rule.OnFailure,
                Message   = passed
                    ? null
                    : $"Gate '{rule.Name}': {rule.Metric} = {actual:F2}, expected {rule.Operator} {rule.Threshold:F2}."
            };
        }

        /// <summary>
        /// Extracts the numeric metric value from a pipeline run result.
        /// </summary>
        private static double ExtractMetric(GateMetric metric, PipelineRunResult result)
        {
            return metric switch
            {
                GateMetric.RejectRatePercent => result.RecordsRead > 0
                    ? (double)result.RecordsRejected / result.RecordsRead * 100.0
                    : 0,
                GateMetric.WarnRatePercent => result.RecordsRead > 0
                    ? (double)result.RecordsWarned / result.RecordsRead * 100.0
                    : 0,
                GateMetric.DurationMs => result.Duration?.TotalMilliseconds ?? 0,
                GateMetric.RecordsRejected => result.RecordsRejected,
                GateMetric.MinRecordsWritten => result.RecordsWritten,
                GateMetric.BytesProcessed => result.BytesProcessed,
                GateMetric.FailedStepCount => result.StepResults.Count(s => s.Status == RunStatus.Failed),
                _ => 0
            };
        }
    }
}
