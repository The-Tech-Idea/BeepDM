using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;

namespace TheTechIdea.Beep.Editor.Importing.Quality
{
    internal sealed class ImportQualityAdmission
    {
        private sealed record RuleSlot(IDataQualityRule Rule, string Field, DataQualityAction Action);
        private readonly RuleSlot[] _rules;
        private readonly IImportRecordAdmission _gate;
        private readonly IImportErrorStore _store;
        private readonly QualityFailureMode _mode;
        private readonly int _timeoutMs;
        private readonly string _context;
        private readonly ImportRejectRecovery _identity;
        private readonly bool _persistRejects;
        public string RunId { get; }
        private int _recordIndex;
        public bool IsConfigured => _rules.Length > 0 || _gate != null;

        private ImportQualityAdmission(DataImportConfiguration config, CancellationToken token, bool persistRejects)
        {
            _mode = config.QualityFailureMode;
            _timeoutMs = config.QualityRuleTimeoutMs;
            _gate = config.RecordAdmission;
            _store = config.ErrorStore;
            _persistRejects = persistRejects;
            RunId = string.IsNullOrWhiteSpace(config.ImportRunId) ? Guid.NewGuid().ToString("N") : config.ImportRunId;
            if (RunId.Length > 1024) throw new ImportQualityAdmissionException();
            _identity = new ImportRejectRecovery
            {
                RunId = RunId, SourceDataSourceName = config.SourceDataSourceName, SourceEntityName = config.SourceEntityName,
                DestinationDataSourceName = config.DestDataSourceName, DestinationEntityName = config.DestEntityName,
                DestinationGuid = config.DestData?.GuidID
            };
            _context = config.SourceDataSourceName + "/" + config.SourceEntityName + "->" + config.DestDataSourceName + "/" + config.DestEntityName;
            if (config.QualityRules?.Count > 128) throw new ImportQualityAdmissionException();
            var rules = config.QualityRules?.ToArray() ?? Array.Empty<IDataQualityRule>();
            if (rules.Length > 128) throw new ImportQualityAdmissionException();
            _rules = rules.Select(rule => rule == null ? throw new ImportQualityAdmissionException() :
                new RuleSlot(rule, rule.FieldName, rule.OnFailure)).ToArray();
            if (!IsConfigured) return;
            if ((_mode != QualityFailureMode.Required && _mode != QualityFailureMode.Advisory) ||
                _timeoutMs < 1 || _timeoutMs > 60000 || _rules.Any(rule => string.IsNullOrWhiteSpace(rule.Field) ||
                    !Enum.IsDefined(typeof(DataQualityAction), rule.Action)))
                throw new ImportQualityAdmissionException();
            if (_store == null && (_rules.Any(rule => rule.Action == DataQualityAction.Quarantine) || _gate?.RequiresRejectStore == true))
                throw new ImportQualityAdmissionException();
            token.ThrowIfCancellationRequested();
            _gate?.Validate(token);
            token.ThrowIfCancellationRequested();
        }

        internal static ImportQualityAdmission Capture(DataImportConfiguration config, CancellationToken token, bool persistRejects = true)
        {
            try { return new ImportQualityAdmission(config, token, persistRejects); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception) { throw new ImportQualityAdmissionException(); }
        }

        internal async Task ValidateStoreAsync(CancellationToken token)
        {
            if (!IsConfigured || _store is not IImportRejectRecoveryStore recovery) return;
            try { await recovery.ValidateRecoveryAsync(_context, token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception) { throw new ImportQualityAdmissionException(); }
        }

        internal async Task<bool> AdmitAsync(object record, ImportExecutionResult result, CancellationToken token)
        {
            if (!IsConfigured) return true;
            int index = ++_recordIndex;
            result.RecordsQualityEvaluated++;
            bool warned = false;
            bool evaluationFailed = false;
            ImportRecordAdmissionResult outcome = ImportRecordAdmissionResult.Pass();
            var elapsed = Stopwatch.StartNew();
            foreach (var rule in _rules)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var value = RecordFieldAccess.ReadRequired(record, rule.Field);
                    bool passed = rule.Rule.Evaluate(value, record);
                    token.ThrowIfCancellationRequested();
                    if (elapsed.ElapsedMilliseconds > _timeoutMs) throw new TimeoutException();
                    if (!passed) outcome = ImportRecordAdmissionResult.Reject(rule.Action);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception) { outcome = _mode == QualityFailureMode.Advisory ? ImportRecordAdmissionResult.Warn(true) : ImportRecordAdmissionResult.Fail(); }
                warned |= outcome.HasWarning;
                evaluationFailed |= outcome.HasEvaluationFailure;
                if (!outcome.ShouldWrite) break;
            }
            if (outcome.ShouldWrite && _gate != null)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    outcome = _gate.Evaluate(record, token) ?? ImportRecordAdmissionResult.Fail();
                    if (elapsed.ElapsedMilliseconds > _timeoutMs) throw new TimeoutException();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception) { outcome = _mode == QualityFailureMode.Advisory ? ImportRecordAdmissionResult.Warn(true) : ImportRecordAdmissionResult.Fail(); }
                token.ThrowIfCancellationRequested();
                warned |= outcome.HasWarning;
                evaluationFailed |= outcome.HasEvaluationFailure;
            }
            if (warned) result.RecordsWarned++;
            if (evaluationFailed) result.RecordsQualityEvaluationFailed++;
            if (outcome.ShouldWrite) return true;
            result.RecordsFailed++;
            if (outcome.Outcome == ImportRecordAdmissionOutcome.Rejected)
            {
                result.RecordsQualityRejected++;
                if (outcome.Action == DataQualityAction.Block) result.RecordsBlocked++;
                bool saved = false;
                if (_store != null && _persistRejects)
                {
                    try
                    {
                        await _store.SaveAsync(new ImportErrorRecord
                        {
                            ContextKey = _context, BatchNumber = 0, RecordIndex = index,
                            Reason = "Record rejected by configured quality admission.", RawRecord = record,
                            Recovery = new ImportRejectRecovery
                            {
                                RejectId = Guid.NewGuid().ToString("N"), RunId = RunId,
                                SourceDataSourceName = _identity.SourceDataSourceName, SourceEntityName = _identity.SourceEntityName,
                                DestinationDataSourceName = _identity.DestinationDataSourceName, DestinationEntityName = _identity.DestinationEntityName,
                                DestinationGuid = _identity.DestinationGuid,
                                OriginalDestinationPayload = ImportRejectPayload.Capture(record)
                            }
                        }, token).ConfigureAwait(false);
                        saved = true;
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { result.RejectStoreFailures++; throw; }
                    catch (Exception) { result.RejectStoreFailures++; }
                }
                else if (_persistRejects && outcome.Action == DataQualityAction.Quarantine) result.RejectStoreFailures++;
                if (outcome.Action == DataQualityAction.Quarantine && saved) result.RecordsQuarantined++;
            }
            result.Errors.Add(new TheTechIdea.Beep.ConfigUtil.ErrorsInfo
            {
                Flag = TheTechIdea.Beep.ConfigUtil.Errors.Failed,
                Message = outcome.HasEvaluationFailure ? "Required record-quality evaluation failed." : "Record rejected by configured quality admission."
            });
            token.ThrowIfCancellationRequested();
            return false;
        }
    }
}
