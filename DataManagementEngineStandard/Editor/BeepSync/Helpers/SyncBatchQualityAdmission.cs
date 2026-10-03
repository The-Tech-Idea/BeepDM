using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    internal sealed class SyncBatchQualityAdmission
    {
        private readonly IRuleEngine _engine;
        private readonly string _key;
        private readonly string _schemaId;
        private readonly QualityFailureMode _mode;
        private readonly double _maxRate;
        private readonly int _depth;
        private readonly int _timeout;

        private SyncBatchQualityAdmission(DataSyncSchema schema, IRuleEngine engine)
        {
            var policy = schema.DqPolicy;
            _engine = engine; _key = policy.BatchThresholdRuleKey; _schemaId = schema.Id;
            _mode = policy.ThresholdFailureMode; _maxRate = policy.MaxRejectRatePercent / 100.0;
            _depth = schema.RulePolicy?.MaxDepth > 0 ? schema.RulePolicy.MaxDepth : 10;
            _timeout = schema.RulePolicy?.MaxExecutionMs > 0 ? schema.RulePolicy.MaxExecutionMs : 5000;
            if (string.IsNullOrWhiteSpace(_key) || _key.Length > 1024 ||
                !Enum.IsDefined(typeof(QualityFailureMode), _mode) || double.IsNaN(_maxRate) ||
                double.IsInfinity(_maxRate) || _maxRate < 0 || _maxRate > 1 ||
                schema.RulePolicy?.MaxDepth < 0 || schema.RulePolicy?.MaxExecutionMs < 0 || _depth > 64 || _timeout > 60000)
                throw new ImportQualityAdmissionException();
        }

        internal static SyncBatchQualityAdmission Capture(DataSyncSchema schema, IRuleEngine engine, CancellationToken token)
        {
            if (schema.DqPolicy?.Enabled != true || !schema.DqPolicy.BatchThresholdEnabled) return null;
            var admission = new SyncBatchQualityAdmission(schema, engine);
            try { admission.CheckRule(token, Stopwatch.StartNew()); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception)
            {
                token.ThrowIfCancellationRequested();
                if (admission._mode == QualityFailureMode.Required) throw new ImportQualityAdmissionException();
            }
            return admission;
        }

        private void CheckRule(CancellationToken token, Stopwatch elapsed)
        {
            token.ThrowIfCancellationRequested();
            if (_engine == null || !_engine.HasRule(_key)) throw new InvalidOperationException();
            token.ThrowIfCancellationRequested();
            if (elapsed.ElapsedMilliseconds > _timeout) throw new TimeoutException();
        }

        internal SyncBatchThresholdResult Evaluate(ImportExecutionResult records, CancellationToken token)
        {
            int attempted = records?.RecordsAttempted ?? 0;
            int rejected = records?.RecordsQualityRejected ?? 0;
            var elapsed = Stopwatch.StartNew();
            try
            {
                CheckRule(token, elapsed);
                var (outputs, _) = _engine.SolveRule(_key, new Dictionary<string, object>
                {
                    ["schemaId"] = _schemaId, ["rejectCount"] = rejected, ["recordCount"] = attempted,
                    ["rejectRate"] = attempted == 0 ? 0.0 : (double)rejected / attempted,
                    ["maxRejectRate"] = _maxRate
                }, new RuleExecutionPolicy { MaxDepth = _depth, MaxExecutionMs = _timeout, AllowDeprecatedExecution = false });
                token.ThrowIfCancellationRequested();
                if (elapsed.ElapsedMilliseconds > _timeout || outputs == null ||
                    !outputs.TryGetValue("action", out var raw) || raw is not string action ||
                    (action != "ContinueRun" && action != "AbortRun")) throw new InvalidOperationException();
                var outcome = action == "AbortRun" || (attempted > 0 && (double)rejected / attempted > _maxRate)
                    ? SyncBatchThresholdOutcome.Rejected : SyncBatchThresholdOutcome.Passed;
                return new SyncBatchThresholdResult(outcome, _mode, attempted, rejected, _maxRate);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception)
            {
                token.ThrowIfCancellationRequested();
                return new SyncBatchThresholdResult(SyncBatchThresholdOutcome.EvaluationFailed, _mode, attempted, rejected, _maxRate);
            }
        }
    }
}
