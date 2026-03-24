using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.History;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        /// <summary>Synchronizes data for the given schema.</summary>
        public async Task<IErrorsInfo> SyncDataAsync(
            DataSyncSchema schema,
            CancellationToken token = default,
            IProgress<PassedArgs> progress = null,
            IImportErrorStore errorStore = null,
            IImportRunHistoryStore historyStore = null)
        {
            var validation = _validationHelper.ValidateSyncOperation(schema);
            if (validation.Flag == Errors.Failed)
            {
                if (schema != null) { schema.SyncStatus = "Failed"; schema.SyncStatusMessage = validation.Message ?? "Schema validation failed."; }
                return validation;
            }

            // Preflight gate
            var ictx = IntegrationContext;
            if (ictx?.RuleEngine != null && schema.RulePolicy?.Enabled == true && schema.RunPreflight)
            {
                var preflight = await RunPreflightAsync(schema, token);
                if (!preflight.IsApproved)
                {
                    schema.SyncStatus = "Failed";
                    schema.SyncStatusMessage = "Preflight check failed. " +
                        string.Join("; ", preflight.Issues.Where(i => i.Severity == "Error").Select(i => i.Message));
                    return new ErrorsInfo { Flag = Errors.Failed, Message = schema.SyncStatusMessage };
                }
            }

            // Reset run-state
            LastRunConflicts            = new List<ConflictEvidence>();
            LastRunCheckpoint           = null;
            LastRunReconciliationReport = null;

            // Phase 6: Mapping quality gate
            int runMappingScore   = -1;
            string runMappingBand = null;
            if (schema.DqPolicy?.Enabled == true && schema.MappingPolicy?.Enabled == true
                && schema.MappingPolicy.MinQualityScore > 0)
            {
                var mqGate = _validationHelper.CheckMappingQualityGate(schema, out runMappingScore, out runMappingBand);
                if (mqGate.Flag == Errors.Failed)
                {
                    schema.SyncStatus = schema.SyncStatusMessage = mqGate.Message; // intentional re-use for brevity
                    schema.SyncStatus = "Failed";
                    schema.SyncStatusMessage = mqGate.Message;
                    return new ErrorsInfo { Flag = Errors.Failed, Message = mqGate.Message };
                }
            }

            int  dqRejectCount       = 0;
            int  dqDefaultsFillCount = 0;
            var  dqAllFailures       = new List<DqGateResult>();
            bool dqRunAborted        = false;

            // Phase 7: Rule-audit telemetry
            int ruleAuditCount = 0;
            EventHandler<RuleAuditEventArgs> ruleAuditHandler = null;
            if (IntegrationContext?.RuleEngine != null)
            {
                ruleAuditHandler = (_, _) => System.Threading.Interlocked.Increment(ref ruleAuditCount);
                IntegrationContext.RuleEngine.RuleEvaluated += ruleAuditHandler;
            }

            // Phase 8: Performance profile
            var perf         = schema.PerfProfile;
            var runRulePolicy = SyncRuleExecutionPolicies.Resolve(perf?.RulePolicyMode);
            WarmUpDefaultsProfile(schema, perf);
            InvalidateMappingCacheIfVersionChanged(schema);

            // Phase 5: Retry / Checkpoint setup
            var rp          = schema.RetryPolicy;
            int maxAttempts = rp?.MaxAttempts > 0 ? rp.MaxAttempts : 1;
            int baseDelay   = rp?.BaseDelayMs  > 0 ? rp.BaseDelayMs  : 1000;

            var checkpoint = await TryLoadCheckpointAsync(schema, rp);

            IErrorsInfo lastResult = new ErrorsInfo { Flag = Errors.Failed, Message = "No attempts made." };

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (token.IsCancellationRequested) break;

                lastResult = await ErrorHandlingHelper.ExecuteWithErrorHandlingAsync(async () =>
                {
                    var cdcCtx = BuildCdcFilterContext(schema, token);
                    var config = SyncSchemaTranslator.ToImportConfiguration(schema, errorStore, historyStore);

                    if (cdcCtx?.ResolvedFilters?.Count > 0)
                    {
                        config.SourceFilters ??= new List<AppFilter>();
                        config.SourceFilters.AddRange(cdcCtx.ResolvedFilters);
                    }

                    var importProgress = CreateProgressAdapter(progress);
                    using var importMgr = new DataImportManager(_editor);
                    var result = await importMgr.RunImportAsync(config, importProgress, token);

                    if (result.Flag == Errors.Failed)
                    {
                        schema.SyncStatus = "Failed";
                        schema.SyncStatusMessage = result.Message ?? "Import failed.";
                        return result;
                    }

                    // Phase 4: Bidirectional reverse-import
                    if (string.Equals(schema.SyncDirection, "Bidirectional", StringComparison.OrdinalIgnoreCase))
                    {
                        var conflictGate = TryEvaluateConflictGate(schema);
                        if (conflictGate.Quarantine)
                        {
                            schema.SyncStatus = "Failed";
                            schema.SyncStatusMessage = $"Conflict gate blocked reverse sync: {conflictGate.Reason}";
                            _editor.AddLogMessage("BeepSync", schema.SyncStatusMessage, DateTime.Now, -1, "", Errors.Failed);
                            return new ErrorsInfo { Flag = Errors.Failed, Message = schema.SyncStatusMessage };
                        }

                        var reverseConfig  = SyncSchemaTranslator.ToReverseImportConfiguration(schema, errorStore, historyStore);
                        var reverseResult  = await importMgr.RunImportAsync(reverseConfig, importProgress, token);
                        if (reverseResult.Flag == Errors.Failed)
                        {
                            schema.SyncStatus = "Failed";
                            schema.SyncStatusMessage = $"Reverse sync failed: {reverseResult.Message}";
                            return reverseResult;
                        }
                    }

                    // Phase 6: DQ batch-threshold gate
                    var dqAbortResult = TryCheckDqBatchThreshold(schema, dqRejectCount, ref dqRunAborted,
                        ref dqAllFailures, ref dqDefaultsFillCount, checkpoint, runMappingScore, runMappingBand);
                    if (dqAbortResult != null) return dqAbortResult;

                    schema.LastSyncDate       = DateTime.Now;
                    schema.SyncStatus         = "Success";
                    schema.SyncStatusMessage  = $"Synchronization completed for {schema.DestinationEntityName}";

                    // Phase 3: Advance watermark
                    if (cdcCtx?.NewWatermarkValue != null && schema.WatermarkPolicy != null)
                    {
                        schema.WatermarkPolicy.LastWatermarkValue = cdcCtx.NewWatermarkValue;
                        _editor.AddLogMessage("BeepSync",
                            $"Watermark advanced to '{cdcCtx.NewWatermarkValue}'.", DateTime.Now, -1, "", Errors.Ok);
                    }

                    await FinalizeCheckpointAsync(schema, rp, checkpoint, attempt);

                    var reconReport = BuildReconReport(schema, checkpoint, dqRejectCount,
                        dqDefaultsFillCount, dqAllFailures, dqRunAborted, runMappingScore, runMappingBand);
                    LastRunReconciliationReport     = reconReport;
                    schema.LastReconciliationReport = reconReport;

                    EmitSloAndAlerts(schema, checkpoint, reconReport, dqRejectCount, attempt,
                        ruleAuditCount, runMappingScore);

                    LogSyncRun(schema);
                    return result;
                }, $"SyncDataAsync:{schema?.SourceEntityName}", _editor, new ErrorsInfo { Flag = Errors.Failed });

                if (lastResult.Flag == Errors.Ok) break;

                var (category, action) = TryClassifyError(schema, lastResult.Message, attempt);
                bool isNonRetryable = rp?.NonRetryableCategories?.Contains(category, StringComparer.OrdinalIgnoreCase) == true
                    || string.Equals(action, "Abort", StringComparison.OrdinalIgnoreCase);
                if (isNonRetryable || attempt >= maxAttempts) break;

                await SaveInProgressCheckpointAsync(schema, rp, checkpoint, attempt, category);

                int delay = rp?.BackoffMode switch
                {
                    "Linear" => baseDelay * attempt,
                    "Fixed"  => baseDelay,
                    _        => baseDelay * (1 << (attempt - 1))
                };
                _editor.AddLogMessage("BeepSync",
                    $"Retry {attempt}/{maxAttempts} for '{schema.Id}'. Cat='{category}' Action='{action}'. Delay={delay}ms.",
                    DateTime.Now, -1, "", Errors.Ok);
                await Task.Delay(delay, token);
            }

            // Phase 7: Unsubscribe rule-audit
            if (ruleAuditHandler != null && IntegrationContext?.RuleEngine != null)
                IntegrationContext.RuleEngine.RuleEvaluated -= ruleAuditHandler;

            return lastResult;
        }

        /// <summary>Synchronous overload for backward compatibility.</summary>
        public void SyncData(DataSyncSchema schema) => SyncDataAsync(schema).GetAwaiter().GetResult();

        /// <summary>Synchronous overload with progress and cancellation.</summary>
        public void SyncData(DataSyncSchema schema, CancellationToken token, IProgress<PassedArgs> progress) =>
            SyncDataAsync(schema, token, progress).GetAwaiter().GetResult();

        // ── Private helpers for SyncDataAsync ─────────────────────────────────────

        private void WarmUpDefaultsProfile(DataSyncSchema schema, SyncPerformanceProfile perf)
        {
            if (perf?.WarmUpDefaultsProfileOnRun == false || IntegrationContext?.DefaultsManager == null) return;
            if (string.IsNullOrWhiteSpace(schema.DestinationDataSourceName) || string.IsNullOrWhiteSpace(schema.DestinationEntityName)) return;
            try
            {
                var profile = Defaults.DefaultsManager.GetProfile(schema.DestinationDataSourceName, schema.DestinationEntityName);
                if (profile != null)
                    _editor.AddLogMessage("BeepSync",
                        $"Defaults profile cached for '{schema.DestinationDataSourceName}.{schema.DestinationEntityName}' ({profile.Rules.Count} rules).",
                        DateTime.Now, -1, "", Errors.Ok);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Defaults profile warm-up failed: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }
        }

        private void InvalidateMappingCacheIfVersionChanged(DataSyncSchema schema)
        {
            if (string.IsNullOrWhiteSpace(schema.DestinationDataSourceName)) return;
            var current  = schema.CurrentSchemaVersion?.Version.ToString();
            var previous = schema.ActiveCheckpoint?.MappingVersion;
            if (previous == null || previous == current) return;

            Mapping.MappingManager.InvalidateMappingCaches(schema.DestinationDataSourceName, schema.DestinationEntityName);
            _editor.AddLogMessage("BeepSync",
                $"Mapping cache invalidated: version {previous} → {current}.", DateTime.Now, -1, "", Errors.Ok);
        }

        private async Task<SyncCheckpoint> TryLoadCheckpointAsync(DataSyncSchema schema, RetryPolicy rp)
        {
            if (rp?.CheckpointEnabled != true) return null;

            var checkpoint = await _persistenceHelper.LoadCheckpointAsync(schema.Id);
            if (checkpoint == null) return null;

            if (IsCheckpointResumeSafe(schema, checkpoint))
            {
                schema.ActiveCheckpoint = checkpoint;
                _editor.AddLogMessage("BeepSync",
                    $"Resuming '{schema.Id}' from checkpoint offset {checkpoint.ProcessedOffset}.",
                    DateTime.Now, -1, "", Errors.Ok);
                return checkpoint;
            }

            await _persistenceHelper.ClearCheckpointAsync(schema.Id);
            _editor.AddLogMessage("BeepSync", $"Stale checkpoint discarded for '{schema.Id}'.", DateTime.Now, -1, "", Errors.Ok);
            return null;
        }

        private IErrorsInfo TryCheckDqBatchThreshold(
            DataSyncSchema schema,
            int dqRejectCount,
            ref bool dqRunAborted,
            ref List<DqGateResult> dqAllFailures,
            ref int dqDefaultsFillCount,
            SyncCheckpoint checkpoint,
            int mappingScore,
            string mappingBand)
        {
            if (schema.DqPolicy?.Enabled != true || IntegrationContext?.RuleEngine == null) return null;
            var thresholdKey = schema.DqPolicy.BatchThresholdRuleKey ?? "sync.dq.batch-threshold";
            if (!IntegrationContext.RuleEngine.HasRule(thresholdKey)) return null;

            try
            {
                var (outputs, _) = IntegrationContext.RuleEngine.SolveRule(
                    thresholdKey,
                    new Dictionary<string, object>
                    {
                        ["rejectCount"]   = dqRejectCount,
                        ["maxRejectRate"] = schema.DqPolicy.MaxRejectRatePercent / 100.0
                    },
                    new RuleExecutionPolicy { MaxDepth = 10, MaxExecutionMs = 5000 });

                var action = outputs?.TryGetValue("action", out var ta) == true ? ta?.ToString() : null;
                if (!string.Equals(action, "AbortRun", StringComparison.OrdinalIgnoreCase)) return null;

                dqRunAborted = true;
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = $"DQ batch threshold exceeded: {dqRejectCount} record(s) rejected.";
                _editor.AddLogMessage("BeepSync", schema.SyncStatusMessage, DateTime.Now, -1, "", Errors.Failed);

                var abortReport = _progressHelper.BuildReconciliationReport(
                    schema, checkpoint?.RunId ?? Guid.NewGuid().ToString(),
                    0, 0, 0, 0, 0, dqRejectCount, LastRunConflicts.Count,
                    dqDefaultsFillCount, LastRunConflicts.Count, true,
                    dqAllFailures, mappingScore, mappingBand);
                LastRunReconciliationReport = schema.LastReconciliationReport = abortReport;
                return new ErrorsInfo { Flag = Errors.Failed, Message = schema.SyncStatusMessage };
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"DQ threshold rule threw: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }
        }

        private async Task FinalizeCheckpointAsync(DataSyncSchema schema, RetryPolicy rp, SyncCheckpoint checkpoint, int attempt)
        {
            if (rp?.CheckpointEnabled != true) return;
            var finalCp = new SyncCheckpoint
            {
                RunId                 = checkpoint?.RunId ?? Guid.NewGuid().ToString(),
                SchemaId              = schema.Id,
                Status                = "Completed",
                AttemptCount          = attempt,
                MappingVersion        = schema.CurrentSchemaVersion?.Version.ToString(),
                CompiledMappingPlanId = IntegrationContext?.CorrelationId
            };
            LastRunCheckpoint       = finalCp;
            schema.ActiveCheckpoint = finalCp;
            await _persistenceHelper.ClearCheckpointAsync(schema.Id);
        }

        private async Task SaveInProgressCheckpointAsync(DataSyncSchema schema, RetryPolicy rp, SyncCheckpoint checkpoint, int attempt, string errorCategory)
        {
            if (rp?.CheckpointEnabled != true) return;
            var errCp = new SyncCheckpoint
            {
                RunId                 = checkpoint?.RunId ?? Guid.NewGuid().ToString(),
                SchemaId              = schema.Id,
                Status                = "InProgress",
                AttemptCount          = attempt,
                LastErrorCategory     = errorCategory,
                MappingVersion        = schema.CurrentSchemaVersion?.Version.ToString(),
                CompiledMappingPlanId = IntegrationContext?.CorrelationId
            };
            LastRunCheckpoint       = errCp;
            schema.ActiveCheckpoint = errCp;
            await _persistenceHelper.SaveCheckpointAsync(errCp);
        }

        private SyncReconciliationReport BuildReconReport(
            DataSyncSchema schema, SyncCheckpoint checkpoint,
            int dqRejectCount, int dqDefaultsFillCount, List<DqGateResult> dqAllFailures,
            bool dqRunAborted, int mappingScore, string mappingBand) =>
            _progressHelper.BuildReconciliationReport(
                schema, checkpoint?.RunId ?? Guid.NewGuid().ToString(),
                0, 0, 0, 0, 0,
                dqRejectCount, LastRunConflicts.Count, dqDefaultsFillCount,
                LastRunConflicts.Count, dqRunAborted,
                dqAllFailures, mappingScore, mappingBand);

        private void EmitSloAndAlerts(
            DataSyncSchema schema, SyncCheckpoint checkpoint, SyncReconciliationReport reconReport,
            int dqRejectCount, int attempt, int ruleAuditCount, int mappingScore)
        {
            var runMetrics = new SyncMetrics
            {
                SchemaID = schema.Id,
                SyncDate = DateTime.UtcNow,
                Duration = TimeSpan.Zero
            };
            bool driftDetected = schema.ActiveCheckpoint?.MappingVersion != null
                && schema.CurrentSchemaVersion?.Version.ToString() != null
                && schema.ActiveCheckpoint.MappingVersion != schema.CurrentSchemaVersion.Version.ToString();

            _progressHelper.EmitSloMetrics(
                schema, runMetrics, reconReport.RunId,
                dqRejectCount, LastRunConflicts.Count, attempt - 1,
                ruleAuditCount, IntegrationContext?.CorrelationId ?? "unknown",
                driftDetected, IntegrationContext?.RuleEngine);

            schema.LastRunAlerts = _progressHelper.EvaluateAlertRules(
                schema, runMetrics, IntegrationContext?.RuleEngine);
        }
    }
}
