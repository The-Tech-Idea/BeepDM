using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Common.Retry;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Schema;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.History;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Editor.Importing.Helpers;
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
            _diagnosticFailures.Clear();
            LastRunBatchThresholdResult = null;
            LastRunFailureCheckpointStatus = null;
            LastRunCheckpoint = null;
            LastRunReconciliationReport = null;
            if (schema == null)
                return new ErrorsInfo { Flag = Errors.Failed, Message = "Schema cannot be null" };

            var watermarkValidation = ValidateWatermarkPolicy(schema);
            if (watermarkValidation.Flag != Errors.Ok)
            {
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = watermarkValidation.Message;
                return watermarkValidation;
            }

            DataImportConfiguration forwardConfig = null;
            DataImportConfiguration reverseConfig = null;
            ImportDefaultsAdmission forwardDefaults = null, reverseDefaults = null;
            SyncRecordQualityAdmission recordQuality = null;
            SyncBatchQualityAdmission batchQuality = null;
            RetryPolicy runRetryPolicy = null;
            SyncCheckpoint runIntent = null;
            IImportErrorStore runErrorStore = errorStore;
            // Strict destination-acceptance preflight via the schema manager.
            // Catches "destination doesn't have the column" before the import starts.
            // Runs after structural validation and before the schema-governance preflight.
            //
            // CreateDestinationIfNotExists comes from the schema: this preflight is the gate that
            // decides whether a missing destination aborts the run, and pinning it to false made
            // DataSyncSchema.CreateDestinationIfNotExists a dead property — the veto fired before
            // the import config built from it was ever used. It defaults to false, so strict
            // behaviour is still the default; only callers that opt in see a change.
            //
            // AddMissingColumns stays pinned to false, deliberately. Passing true here would only
            // skip the missing-column check (SyncSchemaPreflight) — no code would then add the
            // columns: the import pipeline's sole schema-mutating step is CreateEntityAs in
            // DataImportManager.EnsureDestinationEntityExists, which creates whole entities and
            // never alters one. DataImportConfiguration.AddMissingColumns is read only by the
            // back-compat shims in DataImportManager.Migration.cs, which RunImportAsync does not
            // call. So honouring it would drop the veto and deliver nothing, letting rows move into
            // a destination that cannot hold them. Until an add-column path exists, failing fast is
            // the honest behaviour.
            try
            {
                token.ThrowIfCancellationRequested();
                // ValidateSyncOperation opens providers. Admit catalogs for both directions first.
                forwardConfig = SyncSchemaTranslator.ToImportConfiguration(schema, runErrorStore, historyStore);
                if (string.Equals(schema.SyncDirection, "Bidirectional", StringComparison.OrdinalIgnoreCase))
                    reverseConfig = SyncSchemaTranslator.ToReverseImportConfiguration(schema, runErrorStore, historyStore);
                forwardDefaults = ImportDefaultsAdmission.Capture(_editor, forwardConfig, token);
                if (reverseConfig != null)
                    reverseDefaults = ImportDefaultsAdmission.Capture(_editor, reverseConfig, token, forwardDefaults.Registry);
                var qualityEngine = IntegrationContext?.RuleEngine;
                batchQuality = SyncBatchQualityAdmission.Capture(schema, qualityEngine, token);
                recordQuality = SyncRecordQualityAdmission.Capture(schema, qualityEngine, token);
                if (recordQuality != null)
                    runErrorStore = SyncProviderRejectStore.Capture(_editor, schema, errorStore);
                if (recordQuality?.RequiresRejectStore == true && runErrorStore == null)
                    throw new ImportQualityAdmissionException();
                forwardConfig.ErrorStore = runErrorStore;
                if (reverseConfig != null) reverseConfig.ErrorStore = runErrorStore;
                runRetryPolicy = CaptureRetryPolicy(schema.RetryPolicy);
                runIntent = new SyncCheckpoint
                {
                    SchemaId = schema.Id, SchemaFingerprint = SchemaPersistenceHelper.ComputeExecutionFingerprint(schema),
                    MappingVersion = schema.CurrentSchemaVersion?.Version.ToString(),
                    CompiledMappingPlanId = IntegrationContext?.CorrelationId
                };
                var validation = _validationHelper.ValidateSyncOperation(schema);
                if (validation.Flag == Errors.Failed)
                {
                    schema.SyncStatus = "Failed";
                    schema.SyncStatusMessage = validation.Message ?? "Schema validation failed.";
                    return validation;
                }
                var schemaPre = await SyncSchemaPreflight.RunPreflightAsync(_editor, new SchemaRequest
                {
                    SourceDataSourceName = forwardConfig.SourceDataSourceName,
                    SourceEntityName = forwardConfig.SourceEntityName,
                    DestinationDataSourceName = forwardConfig.DestDataSourceName,
                    DestinationEntityName = forwardConfig.DestEntityName,
                    AddMissingColumns = false,
                    CreateDestinationIfNotExists = forwardConfig.CreateDestinationIfNotExists,
                    Mapping = forwardConfig?.Mapping
                }, msg =>
                {
                    _editor.AddLogMessage("BeepSync", $"Schema preflight: {msg}", DateTime.Now, -1, "", Errors.Ok);
                }, token);

                if (schemaPre?.Status?.Flag != Errors.Ok)
                {
                    schema.SyncStatus = "Failed";
                    schema.SyncStatusMessage = "Schema preflight failed: " + schemaPre?.Status?.Message;
                    return new ErrorsInfo { Flag = Errors.Failed, Message = schema.SyncStatusMessage };
                }

                if (forwardConfig.Mapping != null)
                {
                    if (schemaPre.SourceData == null || schemaPre.DestinationData == null ||
                        !schemaPre.SourceData.CheckEntityExist(forwardConfig.SourceEntityName) ||
                        !schemaPre.DestinationData.CheckEntityExist(forwardConfig.DestEntityName))
                        throw new InvalidOperationException("Mapped sync requires existing source and destination entities.");
                    SyncSchemaTranslator.BindEntityMetadata(forwardConfig, schemaPre.SourceEntityStructure, schemaPre.DestinationEntityStructure);
                    forwardConfig.SourceData = schemaPre.SourceData;
                    forwardConfig.DestData = schemaPre.DestinationData;
                    if (reverseConfig != null)
                    {
                        SyncSchemaTranslator.BindEntityMetadata(reverseConfig, schemaPre.DestinationEntityStructure, schemaPre.SourceEntityStructure);
                        reverseConfig.SourceData = schemaPre.DestinationData;
                        reverseConfig.DestData = schemaPre.SourceData;
                    }
                    var importValidation = new DataImportValidationHelper(_editor);
                    if (importValidation.ValidateImportConfiguration(forwardConfig).Flag != Errors.Ok ||
                        (reverseConfig != null && importValidation.ValidateImportConfiguration(reverseConfig).Flag != Errors.Ok))
                        throw new InvalidOperationException("Mapped import configuration or generated shape was not admitted.");
                    token.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                schema.SyncStatus = "Cancelled";
                schema.SyncStatusMessage = "Sync cancelled before schema admission.";
                return new ErrorsInfo { Flag = Errors.Failed, Message = schema.SyncStatusMessage };
            }
            catch (TheTechIdea.Beep.Editor.Defaults.DefaultCatalogReadException)
            {
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = "Required defaults catalog admission failed; neither sync direction was admitted.";
                var failed = new ImportExecutionResult { TransformationAdmissionFailed = true };
                failed.Complete(ImportOutcome.Failed, schema.SyncStatusMessage);
                return failed;
            }
            catch (Exception)
            {
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = "Schema or mapping admission failed; no import was admitted.";
                return new ErrorsInfo { Flag = Errors.Failed, Message = schema.SyncStatusMessage };
            }

            // Preflight gate
            var ictx = IntegrationContext;
            if (ictx?.RuleEngine != null && schema.RulePolicy?.Enabled == true && schema.RunPreflight)
            {
                var preflight = await RunPreflightAsync(schema, token).ConfigureAwait(false);
                if (!preflight.IsApproved)
                {
                    schema.SyncStatus = "Failed";
                    schema.SyncStatusMessage = "Preflight check failed. " +
                        string.Join("; ", preflight.Issues.Where(i => i.Severity == "Error").Select(i => i.Message));
                    return new ErrorsInfo { Flag = Errors.Failed, Message = schema.SyncStatusMessage };
                }
            }

            // Reset run-state
            LastRunConflicts = new List<ConflictEvidence>();
            LastRunCheckpoint = null;
            LastRunReconciliationReport = null;

            // Phase 6: Mapping quality gate
            int runMappingScore = -1;
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

            int dqRejectCount = 0;
            int dqDefaultsFillCount = 0;
            var dqAllFailures = new List<DqGateResult>();
            bool dqRunAborted = false;

            // Phase 7: Rule-audit telemetry
            int ruleAuditCount = 0;
            EventHandler<RuleAuditEventArgs> ruleAuditHandler = null;
            var auditEngine = IntegrationContext?.RuleEngine;
            if (auditEngine != null)
            {
                ruleAuditHandler = (_, _) => System.Threading.Interlocked.Increment(ref ruleAuditCount);
                auditEngine.RuleEvaluated += ruleAuditHandler;
            }

            var previousWatermark = schema.WatermarkPolicy?.LastWatermarkValue;
            var previousSyncDate = schema.LastSyncDate;
            var forwardSourceFilters = forwardConfig?.SourceFilters?.ToList();
            ImportExecutionResult lastImportOutcome = null;
            bool thresholdBlocked = false;
            bool startAcknowledged = false;
            bool completionAttempted = false;
            int runAttempt = 0;
            try
            {
                // Phase 8: Performance profile
                var perf = schema.PerfProfile;
                var runRulePolicy = SyncRuleExecutionPolicies.Resolve(perf?.RulePolicyMode);
                WarmUpDefaultsProfile(schema, perf);
                InvalidateMappingCacheIfVersionChanged(schema);

                // Phase 5: Retry / Checkpoint setup
                var rp = runRetryPolicy;
                int maxAttempts = rp?.MaxAttempts > 0 ? rp.MaxAttempts : 1;
                int baseDelay = rp?.BaseDelayMs > 0 ? rp.BaseDelayMs : 1000;

                var checkpoint = await TryLoadCheckpointAsync(schema, rp).ConfigureAwait(false);
                if (checkpoint != null) runIntent.RunId = checkpoint.RunId;

                IErrorsInfo lastResult = new ErrorsInfo { Flag = Errors.Failed, Message = "No attempts made." };

                // Closure-captured state used by BeforeAttempt (in-progress checkpoint) and
                // OnGiveUp (failure bookkeeping). The pipeline never sees these directly —
                // they're internal to BeepSync's per-attempt bookkeeping.
                string? lastCategory = null;
                string? lastAction = null;
                bool acknowledgedWrites = false;
                bool uncertainWrites = false;
                int acknowledgedCount = 0;
                Exception checkpointSaveFailure = null;

                var retryResult = await RetryPipeline.ExecuteAsync(new RetryPlan<IErrorsInfo>
                {
                    MaxAttempts = maxAttempts,
                    LoggerTag = "BeepSync",

                    Backoff = attempt => TimeSpan.FromMilliseconds(ComputeBackoffMs(rp, baseDelay, attempt)),

                    Classify = ctx =>
                    {
                        if (checkpointSaveFailure != null || thresholdBlocked) return RetryDecision.GiveUp;
                        // Map the previous attempt's result to a decision.
                        if (ctx.LastResult?.Flag == Errors.Ok)
                            return RetryDecision.Succeed;
                        if (acknowledgedWrites || uncertainWrites)
                            return RetryDecision.GiveUp;
                        if (ctx.LastResult is ImportExecutionResult import &&
                            (import.RecordsSucceeded > 0 || import.HasUncertainWrites || import.Outcome == ImportOutcome.Cancelled ||
                             import.RecordsTransformationFailed > 0 || import.TransformationAdmissionFailed || import.RecordsQualityRejected > 0 ||
                             import.RecordsQualityEvaluationFailed > 0 || import.QualityAdmissionFailed || import.RejectStoreFailures > 0))
                            return RetryDecision.GiveUp;

                        var (category, action) = TryClassifyError(schema, ctx.FailureMessage, ctx.Attempt);
                        bool isNonRetryable = rp?.NonRetryableCategories?.Contains(category, StringComparer.OrdinalIgnoreCase) == true
                                              || string.Equals(action, "Abort", StringComparison.OrdinalIgnoreCase);
                        if (isNonRetryable)
                        {
                            lastCategory = category;
                            lastAction = action;
                            return RetryDecision.GiveUp;
                        }
                        lastCategory = category;
                        lastAction = action;
                        return RetryDecision.Retry;
                    },

                    BeforeAttempt = async (ctx, tok) =>
                    {
                        runAttempt = ctx.Attempt;
                        int delayMs = ComputeBackoffMs(rp, baseDelay, ctx.Attempt);
                        if (ctx.Attempt > 1) _editor.AddLogMessage("BeepSync",
                            $"Retry {ctx.Attempt}/{maxAttempts} for '{schema.Id}'. Cat='{lastCategory}' Action='{lastAction}'. Delay={delayMs}ms.",
                            DateTime.Now, -1, "", Errors.Ok);
                        try
                        {
                            await SaveInProgressCheckpointAsync(schema, rp, runIntent, ctx.Attempt, lastCategory, tok).ConfigureAwait(false);
                            if (rp?.CheckpointEnabled == true) startAcknowledged = true;
                        }
                        catch (Exception ex)
                        {
                            // RetryPipeline isolates hook errors. Carry this mandatory failure into Run instead.
                            checkpointSaveFailure = ex;
                        }
                    },

                    Run = async (ctx, tok) =>
                    {
                        tok.ThrowIfCancellationRequested();
                        if (checkpointSaveFailure != null)
                            return new ErrorsInfo { Flag = Errors.Failed, Message = "Mandatory sync checkpoint was not acknowledged; no import was admitted." };
                        var cdcCtx = BuildCdcFilterContext(schema, tok);
                        var config = forwardConfig;
                        config.ImportRunId = runIntent.RunId;
                        config.RecordAdmission = recordQuality?.ForDirection(config.DestEntityName);
                        if (recordQuality != null)
                        {
                            config.QualityRuleTimeoutMs = recordQuality.TimeoutMs;
                            config.QualityFailureMode = recordQuality.FailureMode;
                        }
                        config.SourceFilters = forwardSourceFilters?.ToList() ?? new List<AppFilter>();

                        if (cdcCtx?.ResolvedFilters?.Count > 0)
                        {
                            config.SourceFilters ??= new List<AppFilter>();
                            config.SourceFilters.AddRange(cdcCtx.ResolvedFilters);
                        }

                        var importProgress = CreateProgressAdapter(progress);
                        using var importMgr = new DataImportManager(_editor);
                        var result = await importMgr.RunImportWithDefaultsAsync(config, importProgress, tok, forwardDefaults).ConfigureAwait(false);
                        if (result is ImportExecutionResult forward)
                        {
                            lastImportOutcome = CombineRecordOutcomes(null, forward);
                            acknowledgedWrites |= forward.RecordsSucceeded > 0;
                            acknowledgedCount = checked(acknowledgedCount + forward.RecordsSucceeded);
                            uncertainWrites |= forward.HasUncertainWrites;
                            dqRejectCount = checked(dqRejectCount + forward.RecordsQualityRejected);
                        }
                        tok.ThrowIfCancellationRequested();

                        if (result?.Flag != Errors.Ok)
                        {
                            schema.SyncStatus = "Failed";
                            schema.SyncStatusMessage = result.Message ?? "Import failed.";
                            return ApplyBatchQuality(batchQuality, lastImportOutcome, result, tok, ref thresholdBlocked);
                        }

                        // Phase 4: Bidirectional reverse-import
                        if (reverseConfig != null)
                        {
                            var conflictGate = TryEvaluateConflictGate(schema);
                            if (conflictGate.Quarantine)
                            {
                                schema.SyncStatus = "Failed";
                                schema.SyncStatusMessage = $"Conflict gate blocked reverse sync: {conflictGate.Reason}";
                                _editor.AddLogMessage("BeepSync", schema.SyncStatusMessage, DateTime.Now, -1, "", Errors.Failed);
                                lastImportOutcome?.Complete(ImportOutcome.Partial, "Conflict gate denied reverse admission after forward import.");
                                return ApplyBatchQuality(batchQuality, lastImportOutcome,
                                    (IErrorsInfo)lastImportOutcome ?? new ErrorsInfo { Flag = Errors.Failed, Message = schema.SyncStatusMessage }, tok, ref thresholdBlocked);
                            }

                            reverseConfig.ImportRunId = runIntent.RunId;
                            reverseConfig.RecordAdmission = recordQuality?.ForDirection(reverseConfig.DestEntityName);
                            if (recordQuality != null)
                            {
                                reverseConfig.QualityRuleTimeoutMs = recordQuality.TimeoutMs;
                                reverseConfig.QualityFailureMode = recordQuality.FailureMode;
                            }
                            var reverseResult = await importMgr.RunImportWithDefaultsAsync(reverseConfig, importProgress, tok, reverseDefaults).ConfigureAwait(false);
                            if (reverseResult is ImportExecutionResult reverse)
                            {
                                lastImportOutcome = CombineRecordOutcomes(lastImportOutcome, reverse);
                                reverseResult = result = lastImportOutcome;
                                acknowledgedWrites |= reverse.RecordsSucceeded > 0;
                                acknowledgedCount = checked(acknowledgedCount + reverse.RecordsSucceeded);
                                uncertainWrites |= reverse.HasUncertainWrites;
                                dqRejectCount = checked(dqRejectCount + reverse.RecordsQualityRejected);
                            }
                            tok.ThrowIfCancellationRequested();
                            if (reverseResult?.Flag != Errors.Ok)
                            {
                                schema.SyncStatus = "Failed";
                                schema.SyncStatusMessage = $"Reverse sync failed: {reverseResult.Message}";
                                return ApplyBatchQuality(batchQuality, lastImportOutcome, reverseResult, tok, ref thresholdBlocked);
                            }
                        }

                        result = ApplyBatchQuality(batchQuality, lastImportOutcome, result, tok, ref thresholdBlocked);
                        dqRunAborted = thresholdBlocked;
                        if (thresholdBlocked) return result;

                        completionAttempted = true;
                        var completion = await FinalizeCheckpointAsync(schema, rp, runIntent, ctx.Attempt, acknowledgedCount, tok).ConfigureAwait(false);
                        if (completion != null && !completion.IsSaved)
                        {
                            checkpointSaveFailure = completion.Error ?? new InvalidOperationException("Completion checkpoint was not saved.");
                            return new SyncCheckpointFailureResult(runIntent.RunId, acknowledgedCount, uncertainWrites, completion.Status);
                        }

                        // Publish success only after mandatory completion persistence. These cursors
                        // still require an explicit schema save; checkpoint and schema are not one transaction.
                        RunDiagnostic("SyncDateNotification", () => schema.LastSyncDate = DateTime.Now);
                        if (cdcCtx?.NewWatermarkValue != null && schema.WatermarkPolicy != null)
                            schema.WatermarkPolicy.LastWatermarkValue = cdcCtx.NewWatermarkValue;
                        RunDiagnostic("SyncStatusNotification", () => schema.SyncStatus = "Success");
                        RunDiagnostic("SyncMessageNotification", () => schema.SyncStatusMessage = $"Synchronization completed for {schema.DestinationEntityName}");

                        RunDiagnostic("Reconciliation", () =>
                        {
                            var reconReport = BuildReconReport(schema, runIntent, lastImportOutcome, dqRejectCount,
                                dqDefaultsFillCount, dqAllFailures, dqRunAborted, runMappingScore, runMappingBand);
                            LastRunReconciliationReport = reconReport;
                            schema.LastReconciliationReport = reconReport;
                        });
                        EmitSloAndAlerts(schema, checkpoint, LastRunReconciliationReport, dqRejectCount, ctx.Attempt,
                            ruleAuditCount, runMappingScore);
                        RunDiagnostic("RunHistory", () => LogSyncRun(schema));
                        return result;
                    },

                    OnSuccess = (ctx, result, tok) =>
                    {
                        lastResult = result;
                        return Task.CompletedTask;
                    },

                    OnGiveUp = (ctx, result, decision, tok) =>
                    {
                        lastResult = result ?? new ErrorsInfo { Flag = Errors.Failed, Message = ctx.FailureMessage ?? "Sync failed." };
                        schema.SyncStatus = "Failed";
                        schema.SyncStatusMessage = lastResult.Message;
                        schema.LastSyncDate = previousSyncDate;
                        if (schema.WatermarkPolicy != null) schema.WatermarkPolicy.LastWatermarkValue = previousWatermark;
                        return Task.CompletedTask;
                    }
                }, token);

                if (retryResult.FinalDecision != RetryDecision.Succeed) token.ThrowIfCancellationRequested();
                if (lastResult.Flag != Errors.Failed && retryResult.FinalDecision != RetryDecision.Succeed)
                {
                    // Pipeline gave up — preserve the most informative message we have.
                    lastResult = new ErrorsInfo
                    {
                        Flag = Errors.Failed,
                        Message = lastResult.Message ?? retryResult.FailureMessage ?? "Sync failed."
                    };
                }

                if (lastResult.Flag != Errors.Ok)
                {
                    RunDiagnostic("Reconciliation", () =>
                    {
                        var report = BuildReconReport(schema, runIntent, lastImportOutcome, dqRejectCount,
                            dqDefaultsFillCount, dqAllFailures, thresholdBlocked, runMappingScore, runMappingBand);
                        LastRunReconciliationReport = report;
                        schema.LastReconciliationReport = report;
                    });
                    if (startAcknowledged && !completionAttempted)
                        return await PublishFailureAsync(schema, rp, runIntent, runAttempt, lastImportOutcome, lastResult,
                            thresholdBlocked ? SyncRunFailureKind.QualityThreshold : SyncRunFailureKind.ImportFailure).ConfigureAwait(false);
                }
                return lastResult;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                schema.SyncStatus = "Cancelled";
                schema.SyncStatusMessage = "Synchronization was cancelled; watermark was not advanced.";
                schema.LastSyncDate = previousSyncDate;
                if (schema.WatermarkPolicy != null) schema.WatermarkPolicy.LastWatermarkValue = previousWatermark;
                try { _editor.AddLogMessage("BeepSync", schema.SyncStatusMessage, DateTime.Now, -1, "", Errors.Failed); }
                catch (Exception loggerError) { System.Diagnostics.Debug.WriteLine($"Sync state failure logging failed ({loggerError.GetType().Name})."); }
                if (lastImportOutcome != null)
                {
                    lastImportOutcome.Complete(ImportOutcome.Cancelled, schema.SyncStatusMessage);
                    if (startAcknowledged && !completionAttempted)
                        return await PublishFailureAsync(schema, runRetryPolicy, runIntent, runAttempt, lastImportOutcome,
                            lastImportOutcome, SyncRunFailureKind.Cancelled).ConfigureAwait(false);
                    return lastImportOutcome;
                }
                var cancelled = new ErrorsInfo { Flag = Errors.Failed, Message = schema.SyncStatusMessage };
                return startAcknowledged && !completionAttempted
                    ? await PublishFailureAsync(schema, runRetryPolicy, runIntent, runAttempt, null, cancelled, SyncRunFailureKind.Cancelled).ConfigureAwait(false)
                    : cancelled;
            }
            catch (Exception ex)
            {
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = $"Sync state could not be validated ({ex.GetType().Name}); preserve persisted evidence and reconcile before replay.";
                schema.LastSyncDate = previousSyncDate;
                if (schema.WatermarkPolicy != null) schema.WatermarkPolicy.LastWatermarkValue = previousWatermark;
                if (lastImportOutcome != null)
                {
                    lastImportOutcome.Complete(lastImportOutcome.RecordsSucceeded > 0 ? ImportOutcome.Partial : ImportOutcome.Failed, schema.SyncStatusMessage);
                    if (startAcknowledged && !completionAttempted)
                        return await PublishFailureAsync(schema, runRetryPolicy, runIntent, runAttempt, lastImportOutcome,
                            lastImportOutcome, SyncRunFailureKind.ExecutionFailure).ConfigureAwait(false);
                    return lastImportOutcome;
                }
                var failed = new ErrorsInfo { Flag = Errors.Failed, Message = schema.SyncStatusMessage };
                return startAcknowledged && !completionAttempted
                    ? await PublishFailureAsync(schema, runRetryPolicy, runIntent, runAttempt, null, failed, SyncRunFailureKind.ExecutionFailure).ConfigureAwait(false)
                    : failed;
            }
            finally
            {
                if (ruleAuditHandler != null)
                    RunDiagnostic("RuleAuditUnsubscribe", () => auditEngine.RuleEvaluated -= ruleAuditHandler);
            }
        }

        /// <summary>
        /// Synchronous overload for backward compatibility.
        /// Task.Run starts the async work on a thread-pool thread with no SynchronizationContext,
        /// so its awaits resume on the pool rather than being posted back to the caller. Without
        /// it, a UI-thread caller blocks in GetResult() waiting for a continuation only that same
        /// blocked thread could run — a deadlock. Prefer <see cref="SyncDataAsync(DataSyncSchema)"/>
        /// and await it; this overload still blocks the caller for the duration.
        /// </summary>
        public void SyncData(DataSyncSchema schema) => Task.Run(() => SyncDataAsync(schema)).GetAwaiter().GetResult();

        /// <summary>Synchronous overload with progress and cancellation. See <see cref="SyncData(DataSyncSchema)"/>.</summary>
        public void SyncData(DataSyncSchema schema, CancellationToken token, IProgress<PassedArgs> progress) =>
            Task.Run(() => SyncDataAsync(schema, token, progress)).GetAwaiter().GetResult();

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
            var current = schema.CurrentSchemaVersion?.Version.ToString();
            var previous = schema.ActiveCheckpoint?.MappingVersion;
            if (previous == null || previous == current) return;

            Mapping.MappingManager.InvalidateMappingCaches(schema.DestinationDataSourceName, schema.DestinationEntityName);
            _editor.AddLogMessage("BeepSync",
                $"Mapping cache invalidated: version {previous} → {current}.", DateTime.Now, -1, "", Errors.Ok);
        }

        private async Task<SyncCheckpoint> TryLoadCheckpointAsync(DataSyncSchema schema, RetryPolicy rp)
        {
            if (rp?.CheckpointEnabled != true) return null;

            var checkpoint = await _persistenceHelper.LoadCheckpointAsync(schema.Id).ConfigureAwait(false);
            if (checkpoint == null) return null;

            if (checkpoint.SchemaFingerprint != SchemaPersistenceHelper.ComputeExecutionFingerprint(schema) ||
                checkpoint.RequiresReconciliation || checkpoint.Status == "Running" || checkpoint.Status == "Failed" || checkpoint.ProcessedOffset > 0 && checkpoint.Status != "Completed")
                throw new InvalidOperationException("Stored sync context or provider progress requires reconciliation; offset replay is not implemented by the translator.");
            if (checkpoint.Status == "Completed") return null;

            if (IsCheckpointResumeSafe(schema, checkpoint))
            {
                schema.ActiveCheckpoint = checkpoint;
                _editor.AddLogMessage("BeepSync",
                    $"Resuming '{schema.Id}' from checkpoint offset {checkpoint.ProcessedOffset}.",
                    DateTime.Now, -1, "", Errors.Ok);
                return checkpoint;
            }

            throw new InvalidOperationException("Stale sync checkpoint is preserved; reconcile before a fresh run.");
        }

        private static int ComputeBackoffMs(RetryPolicy? rp, int baseDelay, int attempt)
        {
            // Single source of truth for the BeepSync backoff formula. The
            // RetryPipeline's Backoff lambda delegates here so the actual sleep
            // and the "Retry N/M ... Delay=Xms" log message in BeforeAttempt
            // can never disagree.
            return rp?.BackoffMode switch
            {
                "Linear" => baseDelay * attempt,
                "Fixed" => baseDelay,
                _ => baseDelay * (1 << (attempt - 1))
            };
        }

        private async Task<PersistenceWriteResult> FinalizeCheckpointAsync(DataSyncSchema schema, RetryPolicy rp, SyncCheckpoint checkpoint, int attempt, int acknowledgedCount, CancellationToken token)
        {
            if (rp?.CheckpointEnabled != true) return null;
            var finalCp = CheckpointFromIntent(checkpoint, "Completed", attempt, acknowledgedCount);
            PersistenceWriteResult result;
            try
            {
                result = _persistenceHelper is ISyncPersistenceAcknowledgement acknowledged
                    ? await acknowledged.SaveCheckpointAcknowledgedAsync(finalCp, token).ConfigureAwait(false)
                    : new PersistenceWriteResult(PersistenceWriteStatus.Unsupported);
                result ??= new PersistenceWriteResult(PersistenceWriteStatus.Failed);
            }
            catch (OperationCanceledException ex) { result = new PersistenceWriteResult(PersistenceWriteStatus.Cancelled, ex); }
            catch (NotSupportedException ex) { result = new PersistenceWriteResult(PersistenceWriteStatus.Unsupported, ex); }
            catch (Exception ex) { result = new PersistenceWriteResult(PersistenceWriteStatus.Failed, ex); }
            if (!result.IsSaved) return result;
            LastRunCheckpoint = finalCp;
            RunDiagnostic("CheckpointNotification", () => schema.ActiveCheckpoint = finalCp);
            return result;
        }

        private async Task SaveInProgressCheckpointAsync(DataSyncSchema schema, RetryPolicy rp, SyncCheckpoint checkpoint, int attempt, string errorCategory, CancellationToken token)
        {
            if (rp?.CheckpointEnabled != true) return;
            var errCp = CheckpointFromIntent(checkpoint, "Running", attempt, 0);
            errCp.LastErrorCategory = errorCategory;
            if (_persistenceHelper is not ISyncPersistenceAcknowledgement acknowledged)
                throw new NotSupportedException("Sync execution requires acknowledged checkpoint storage.");
            (await acknowledged.SaveCheckpointAcknowledgedAsync(errCp, token).ConfigureAwait(false)).ThrowIfNotSaved();
            LastRunCheckpoint = errCp;
            RunDiagnostic("CheckpointNotification", () => schema.ActiveCheckpoint = errCp);
        }

        private SyncReconciliationReport BuildReconReport(
            DataSyncSchema schema, SyncCheckpoint checkpoint, ImportExecutionResult records,
            int dqRejectCount, int dqDefaultsFillCount, List<DqGateResult> dqAllFailures,
            bool dqRunAborted, int mappingScore, string mappingBand) =>
            _progressHelper.BuildReconciliationReport(
                schema, checkpoint?.RunId ?? Guid.NewGuid().ToString(),
                records?.RecordsAttempted ?? 0, records?.RecordsSucceeded ?? 0,
                records?.RecordsSucceeded ?? 0, 0, records?.RecordsSkipped ?? 0,
                dqRejectCount, (records?.RecordsQuarantined ?? 0) + LastRunConflicts.Count, dqDefaultsFillCount,
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

            RunDiagnostic("SloMetrics", () => _progressHelper.EmitSloMetrics(
                schema, runMetrics, reconReport?.RunId ?? checkpoint?.RunId,
                dqRejectCount, LastRunConflicts.Count, attempt - 1,
                ruleAuditCount, IntegrationContext?.CorrelationId ?? "unknown",
                driftDetected, IntegrationContext?.RuleEngine));

            RunDiagnostic("AlertRules", () => schema.LastRunAlerts = _progressHelper.EvaluateAlertRules(
                schema, runMetrics, IntegrationContext?.RuleEngine));
        }
    }
}
