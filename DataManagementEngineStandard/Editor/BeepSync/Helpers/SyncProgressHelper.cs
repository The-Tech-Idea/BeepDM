using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    /// <summary>
    /// Helper class for sync progress reporting and logging operations
    /// Based on progress patterns from DataSyncManager.SendMessage and LogSyncRun
    /// </summary>
    public class SyncProgressHelper : ISyncProgressHelper
    {
        private readonly IDMEEditor _editor;
        private const string LoggerName = "BeepSync";

        /// <summary>
        /// Initializes a new instance of the SyncProgressHelper class
        /// </summary>
        /// <param name="editor">The DME editor instance</param>
        public SyncProgressHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Report progress with message and optional progress counters
        /// Based on DataSyncManager.SendMessage pattern
        /// </summary>
        /// <param name="progress">Progress reporter</param>
        /// <param name="message">Progress message</param>
        /// <param name="current">Current progress count</param>
        /// <param name="total">Total progress count</param>
        public void ReportProgress(IProgress<PassedArgs> progress, string message, int current = 0, int total = 0)
        {
            if (progress == null)
                return;

            try
            {
                var args = new PassedArgs
                {
                    EventType = "Update",
                    Messege = message,
                    ParameterInt1 = current,
                    ParameterInt2 = total
                };

                progress.Report(args);

                // Also log the progress message
                LogMessage(message, Errors.Ok);
            }
            catch (Exception ex)
            {
                LogMessage($"Error reporting progress: {ex.Message}", Errors.Failed);
            }
        }

        /// <summary>
        /// Log sync operation message with specified error level
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="errorLevel">Error level (Ok, Failed, etc.)</param>
        public void LogMessage(string message, Errors errorLevel = Errors.Ok)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                _editor.AddLogMessage(LoggerName, message, DateTime.Now, -1, "", errorLevel);
            }
            catch (Exception ex)
            {
                // Fallback logging if main logging fails
                try
                {
                    _editor.AddLogMessage("System", $"Logging error in {LoggerName}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                }
                catch
                {
                    // Silent fail if even fallback logging fails
                }
            }
        }

        /// <summary>
        /// Log sync run details and update schema status
        /// Based on DataSyncManager.LogSyncRun method
        /// </summary>
        /// <param name="schema">The sync schema to log</param>
        public void LogSyncRun(DataSyncSchema schema)
        {
            if (schema == null)
            {
                LogMessage("Cannot log sync run: schema is null", Errors.Failed);
                return;
            }

            try
            {
                // Create sync run data entry
                var syncRunData = new SyncRunData
                {
                    Id = Guid.NewGuid().ToString(),
                    SyncSchemaId = schema.Id,
                    SyncDate = schema.LastSyncDate != default ? schema.LastSyncDate : DateTime.Now,
                    SyncStatus = schema.SyncStatus,
                    SyncStatusMessage = schema.SyncStatusMessage
                };

                // Add to schema's sync runs collection
                if (schema.SyncRuns == null)
                    schema.SyncRuns = new ObservableBindingList<SyncRunData>();

                schema.SyncRuns.Add(syncRunData);
                schema.LastSyncRunData = syncRunData;

                // Log the sync run
                var logLevel = schema.SyncStatus == "Success" ? Errors.Ok : Errors.Failed;
                LogMessage($"Sync run logged for schema '{schema.Id}': {schema.SyncStatus} - {schema.SyncStatusMessage}", logLevel);
            }
            catch (Exception ex)
            {
                LogMessage($"Error logging sync run for schema '{schema?.Id}': {ex.Message}", Errors.Failed);
            }
        }

        /// <summary>
        /// Handle and log sync errors with detailed information
        /// </summary>
        /// <param name="schema">The sync schema where error occurred</param>
        /// <param name="message">Error message</param>
        /// <param name="ex">Optional exception for detailed error information</param>
        public void LogError(DataSyncSchema schema, string message, Exception ex = null)
        {
            if (schema == null)
            {
                LogMessage("Cannot log sync error: schema is null", Errors.Failed);
                return;
            }

            try
            {
                // Update schema status
                schema.SyncStatus = "Failed";
                schema.LastSyncDate = DateTime.Now;

                // Build comprehensive error message
                var fullMessage = message;
                if (ex != null)
                {
                    fullMessage = $"{message}. Exception: {ex.Message}";
                    if (ex.InnerException != null)
                        fullMessage += $" Inner Exception: {ex.InnerException.Message}";
                }

                schema.SyncStatusMessage = fullMessage;

                // Log the error
                LogMessage($"Sync error for schema '{schema.Id}' ({schema.SourceEntityName} -> {schema.DestinationEntityName}): {fullMessage}", Errors.Failed);

                // Log the sync run with error details
                LogSyncRun(schema);
            }
            catch (Exception logEx)
            {
                // Fallback error logging
                LogMessage($"Error while logging sync error for schema '{schema?.Id}': {logEx.Message}", Errors.Failed);
            }
        }

        /// <summary>
        /// Report successful sync completion
        /// </summary>
        /// <param name="schema">The sync schema that completed successfully</param>
        /// <param name="recordsProcessed">Number of records processed</param>
        /// <param name="progress">Optional progress reporter</param>
        public void LogSuccess(DataSyncSchema schema, int recordsProcessed, IProgress<PassedArgs> progress = null)
        {
            if (schema == null)
            {
                LogMessage("Cannot log sync success: schema is null", Errors.Failed);
                return;
            }

            try
            {
                // Update schema status
                schema.SyncStatus = "Success";
                schema.LastSyncDate = DateTime.Now;
                schema.SyncStatusMessage = $"Synchronization completed successfully. Processed {recordsProcessed} record(s).";

                // Log success message
                LogMessage($"Sync completed for schema '{schema.Id}' ({schema.SourceEntityName} -> {schema.DestinationEntityName}): {recordsProcessed} record(s) processed", Errors.Ok);

                // Report progress if available
                ReportProgress(progress, schema.SyncStatusMessage);

                // Log the sync run
                LogSyncRun(schema);
            }
            catch (Exception ex)
            {
                LogMessage($"Error while logging sync success for schema '{schema?.Id}': {ex.Message}", Errors.Failed);
            }
        }

        /// <summary>
        /// Report sync cancellation
        /// </summary>
        /// <param name="schema">The sync schema that was cancelled</param>
        /// <param name="progress">Optional progress reporter</param>
        public void LogCancellation(DataSyncSchema schema, IProgress<PassedArgs> progress = null)
        {
            if (schema == null)
            {
                LogMessage("Cannot log sync cancellation: schema is null", Errors.Failed);
                return;
            }

            try
            {
                // Update schema status
                schema.SyncStatus = "Cancelled";
                schema.LastSyncDate = DateTime.Now;
                schema.SyncStatusMessage = "Synchronization was cancelled by user.";

                // Log cancellation message
                LogMessage($"Sync cancelled for schema '{schema.Id}' ({schema.SourceEntityName} -> {schema.DestinationEntityName})", Errors.Ok);

                // Report progress if available
                if (progress != null)
                {
                    var args = new PassedArgs
                    {
                        EventType = "Cancel",
                        Messege = schema.SyncStatusMessage
                    };
                    progress.Report(args);
                }

                // Log the sync run
                LogSyncRun(schema);
            }
            catch (Exception ex)
            {
                LogMessage($"Error while logging sync cancellation for schema '{schema?.Id}': {ex.Message}", Errors.Failed);
            }
        }

        // ── Phase 6: Reconciliation report builder ────────────────────────────────

        /// <inheritdoc cref="ISyncProgressHelper.BuildReconciliationReport"/>
        public SyncReconciliationReport BuildReconciliationReport(
            DataSyncSchema schema,
            string runId,
            int sourceRowsScanned,
            int destRowsWritten,
            int destRowsInserted,
            int destRowsUpdated,
            int destRowsSkipped,
            int rejectCount,
            int quarantineCount,
            int defaultsFillCount,
            int conflictCount,
            bool runAbortedByThreshold,
            List<DqGateResult> dqFailures,
            int mappingQualityScore = -1,
            string mappingQualityBand = null,
            List<string> unmappedRequiredFields = null)
        {
            double rejectRate = sourceRowsScanned > 0
                ? (double)rejectCount / sourceRowsScanned
                : 0.0;

            var report = new SyncReconciliationReport
            {
                SchemaId               = schema?.Id,
                RunId                  = runId ?? Guid.NewGuid().ToString(),
                GeneratedAt            = DateTime.UtcNow,
                GeneratedBy            = Environment.UserName,
                SourceRowsScanned      = sourceRowsScanned,
                DestRowsWritten        = destRowsWritten,
                DestRowsInserted       = destRowsInserted,
                DestRowsUpdated        = destRowsUpdated,
                DestRowsSkipped        = destRowsSkipped,
                RejectCount            = rejectCount,
                QuarantineCount        = quarantineCount,
                DefaultsFillCount      = defaultsFillCount,
                ConflictCount          = conflictCount,
                RejectRate             = rejectRate,
                RunAbortedByThreshold  = runAbortedByThreshold,
                DqFailures             = dqFailures ?? new List<DqGateResult>(),
                MappingQualityScore    = mappingQualityScore,
                MappingQualityBand     = mappingQualityBand,
                UnmappedRequiredFields = unmappedRequiredFields ?? new List<string>()
            };

            LogMessage(
                $"Reconciliation report for schema '{schema?.Id}': " +
                $"scanned={sourceRowsScanned}, written={destRowsWritten}, " +
                $"rejected={rejectCount} ({rejectRate:P1}), " +
                $"DQ failures={report.DqFailures.Count}, " +
                $"mappingScore={mappingQualityScore}.",
                runAbortedByThreshold ? Errors.Failed : Errors.Ok);

            return report;
        }

        // ── Phase 7: SLO metrics + alert evaluation ───────────────────────────────

        /// <inheritdoc cref="ISyncProgressHelper.EmitSloMetrics"/>
        public void EmitSloMetrics(
            DataSyncSchema schema,
            SyncMetrics metrics,
            string runId,
            int rejectCount,
            int conflictCount,
            int retryCount,
            int ruleEvaluationCount,
            string mappingPlanVersion,
            bool mappingDriftDetected,
            IRuleEngine ruleEngine = null)
        {
            if (metrics == null) return;

            int total = metrics.TotalRecords > 0 ? metrics.TotalRecords : 1;

            metrics.RejectRate           = (double)rejectCount / total;
            metrics.ConflictRate         = (double)conflictCount / total;
            metrics.RetryCount           = retryCount;
            metrics.RuleEvaluationCount  = ruleEvaluationCount;
            metrics.MappingPlanVersion   = mappingPlanVersion ?? "unknown";
            metrics.MappingDriftDetected = mappingDriftDetected;
            metrics.CorrelationId        = $"{schema?.Id}.{runId}.{metrics.MappingPlanVersion}";

            // Try SLO classify-run rule
            if (ruleEngine != null && ruleEngine.HasRule("sync.slo.classify-run"))
            {
                try
                {
                    var slo = schema?.SloProfile;
                    var classifyPolicy = new TheTechIdea.Beep.Rules.RuleExecutionPolicy
                    {
                        MaxDepth = 10, MaxExecutionMs = 5000
                    };
                    var (classifyOutputs, _) = ruleEngine.SolveRule(
                        "sync.slo.classify-run",
                        new Dictionary<string, object>
                        {
                            ["successRate"]         = metrics.SuccessRate,
                            ["runDurationMs"]       = metrics.Duration.TotalMilliseconds,
                            ["rejectRate"]          = metrics.RejectRate,
                            ["conflictRate"]        = metrics.ConflictRate,
                            ["freshnessLagSeconds"] = metrics.FreshnessLagSeconds,
                            ["minSuccessRate"]      = slo?.MinSuccessRate ?? 0.95,
                            ["maxDurationMs"]       = slo?.MaxDurationMs ?? 300_000,
                            ["maxRejectRate"]       = slo?.MaxRejectRate ?? 0.05,
                            ["maxConflictRate"]     = slo?.MaxConflictRate ?? 0.05
                        },
                        classifyPolicy);

                    if (classifyOutputs?.TryGetValue("tier", out var tier) == true)
                        metrics.SloComplianceTier = tier?.ToString() ?? "Unknown";
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("BeepSync",
                        $"SLO classify-run rule threw: {ex.Message}",
                        DateTime.Now, -1, "", Errors.Failed);
                }
            }

            LogMessage(
                $"[SLO] schema='{schema?.Id}' run='{runId}' tier='{metrics.SloComplianceTier}' " +
                $"successRate={metrics.SuccessRate:P1} rejectRate={metrics.RejectRate:P1} " +
                $"conflictRate={metrics.ConflictRate:P1} retries={retryCount} ruleEvals={ruleEvaluationCount}");
        }

        /// <inheritdoc cref="ISyncProgressHelper.EvaluateAlertRules"/>
        public List<SyncAlertRecord> EvaluateAlertRules(
            DataSyncSchema schema,
            SyncMetrics metrics,
            IRuleEngine ruleEngine)
        {
            var alerts = new List<SyncAlertRecord>();
            if (ruleEngine == null || schema?.SloProfile?.AlertRuleKeys == null) return alerts;

            var alertContext = new Dictionary<string, object>
            {
                ["successRate"]         = metrics.SuccessRate,
                ["runDurationMs"]       = metrics.Duration.TotalMilliseconds,
                ["rejectRate"]          = metrics.RejectRate,
                ["conflictRate"]        = metrics.ConflictRate,
                ["freshnessLagSeconds"] = metrics.FreshnessLagSeconds,
                ["retryCount"]         = metrics.RetryCount,
                ["sloProfile"]         = schema.SloProfile.ProfileName
            };

            var alertPolicy = new TheTechIdea.Beep.Rules.RuleExecutionPolicy
            {
                MaxDepth = 10, MaxExecutionMs = 5000
            };

            foreach (var ruleKey in schema.SloProfile.AlertRuleKeys)
            {
                if (string.IsNullOrWhiteSpace(ruleKey)) continue;
                if (!ruleEngine.HasRule(ruleKey)) continue;

                try
                {
                    var (outputs, _) = ruleEngine.SolveRule(ruleKey, alertContext, alertPolicy);

                    bool triggered = outputs?.TryGetValue("triggered", out var t) == true
                        && t is bool b && b;
                    if (!triggered) continue;

                    var record = BuildAlertPayload(schema, metrics, ruleKey, outputs);
                    alerts.Add(record);
                    _editor.AddLogMessage("BeepSync",
                        $"[Alert] rule='{ruleKey}' severity='{record.Severity}' reason='{record.Reason}'",
                        DateTime.Now, -1, "", Errors.Failed);
                }
                catch (Exception ex)
                {
                    _editor.AddLogMessage("BeepSync",
                        $"Alert rule '{ruleKey}' threw: {ex.Message}",
                        DateTime.Now, -1, "", Errors.Failed);
                }
            }

            return alerts;
        }

        /// <inheritdoc cref="ISyncProgressHelper.BuildAlertPayload"/>
        public SyncAlertRecord BuildAlertPayload(
            DataSyncSchema schema,
            SyncMetrics metrics,
            string ruleKey,
            Dictionary<string, object> ruleOutputs)
        {
            string Get(string key) =>
                ruleOutputs?.TryGetValue(key, out var v) == true ? v?.ToString() : null;

            return new SyncAlertRecord
            {
                SchemaId         = schema?.Id,
                RunId            = metrics?.CorrelationId?.Split('.') is { Length: > 1 } parts
                                       ? parts[1] : null,
                CorrelationId    = metrics?.CorrelationId,
                RuleKey          = ruleKey,
                Severity         = Get("severity") ?? "Warning",
                Reason           = Get("reason")  ?? $"Alert rule '{ruleKey}' fired.",
                RemediationHint  = Get("remediation"),
                EmittedAt        = DateTime.UtcNow,
                EmittedBy        = Environment.UserName,
                Status           = "Open",
                AdditionalContext = new Dictionary<string, object>
                {
                    ["sloSchema"]         = schema?.Id,
                    ["successRate"]       = metrics?.SuccessRate,
                    ["rejectRate"]        = metrics?.RejectRate,
                    ["conflictRate"]      = metrics?.ConflictRate,
                    ["runDurationMs"]     = metrics?.Duration.TotalMilliseconds,
                    ["mappingDrift"]      = metrics?.MappingDriftDetected == true
                                               ? "Detected — review mapping before next run"
                                               : "None"
                }
            };
        }
    }
}
