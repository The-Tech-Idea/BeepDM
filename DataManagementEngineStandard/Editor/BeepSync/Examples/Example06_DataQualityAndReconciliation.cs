// Example 06 — Data Quality Gates & Reconciliation
// Demonstrates DqPolicy configuration, reject/quarantine routing, batch-threshold abort,
// and reading the SyncReconciliationReport after the run.
//
// Phases covered: Phase 6 (data quality & reconciliation)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor.BeepSync.Examples
{
    /// <summary>
    /// Phase 6 — Sync with DQ gate rules, reject routing, and reconciliation reporting.
    /// </summary>
    public static class Example06_DataQualityAndReconciliation
    {
        public static Task RunAsync(IDMEEditor editor) => RunAsync(editor, null);

        public static async Task RunAsync(IDMEEditor editor, IRuleEngine ruleEngine)
        {
            ArgumentNullException.ThrowIfNull(ruleEngine);
            // The host registers the named Boolean record rules and strict threshold action rule.
            using var syncManager = new BeepSyncManager(editor, new SyncIntegrationContext { RuleEngine = ruleEngine });
            var fieldHelper = new FieldMappingHelper(editor);

            var schema = new DataSyncSchema
            {
                Id                        = Guid.NewGuid().ToString(),
                SourceDataSourceName      = "CRM_DB",
                DestinationDataSourceName = "Warehouse_DB",
                SourceEntityName          = "Contacts",
                DestinationEntityName     = "Contacts",
                SourceKeyField            = "ContactId",
                DestinationKeyField       = "ContactId",
                SyncType                  = "Full",
                SyncDirection             = "OneWay",
                BatchSize                 = 1000,

                // ── DQ policy ──────────────────────────────────────────────────────
                DqPolicy = new DqPolicy
                {
                    Enabled = true,

                    // Rule keys evaluated per record in order.
                    // Required rejection denies the row and fails the run without cursor advancement.
                    RuleKeys = new List<string>
                    {
                        "sync.dq.required-fields",   // ContactId, Email must be non-null
                        "sync.dq.email-format",       // Email must match regex
                        "sync.dq.type-validity",      // field types must coerce cleanly
                    },
                    RecordFailureMode = QualityFailureMode.Required,
                    OnRecordFailure = DataQualityAction.Quarantine,

                    // Once per attempt, including partial failure; action is ContinueRun or AbortRun.
                    // ContinueRun cannot override the numeric limit or required record rejection.
                    BatchThresholdEnabled = true,
                    ThresholdFailureMode = QualityFailureMode.Required,
                    BatchThresholdRuleKey  = "sync.dq.batch-threshold",
                    MaxRejectRatePercent   = 5.0,      // reject completion if > 5 % of attempted rows

                    // This provider/entity must already be open/provisioned; write-only, not automatic replay.
                    RejectChannelDataSourceName = "Quarantine_DB",
                    RejectChannelEntityName     = "ContactRejects",

                    // Compatibility flag, not a separate defaults stage; configure actual import defaults.
                    FillDefaultsBeforeEval = true,
                },
                RetryPolicy = new RetryPolicy { MaxAttempts = 1, CheckpointEnabled = true },

                // ── Mapping quality gate ───────────────────────────────────────────
                MappingPolicy = new SyncMappingPolicy
                {
                    Enabled          = true,
                    MinQualityScore  = 75,    // preflight fails if Mapping Manager scores < 75
                    OnDriftAction    = "Warn",
                },
            };

            foreach (var m in fieldHelper.AutoMapFields("CRM_DB", "Contacts", "Warehouse_DB", "Contacts"))
                schema.MappedFields.Add(m);

            // ── Run preflight to catch issues before moving data ───────────────────
            var preflightReport = await syncManager.RunPreflightAsync(schema).ConfigureAwait(false);
            if (!preflightReport.IsApproved)
            {
                Console.WriteLine("[PREFLIGHT FAILED]");
                foreach (var issue in preflightReport.Issues)
                    Console.WriteLine($"  [{issue.Severity}] {issue.Code}: {issue.Message}");
                return;
            }

            syncManager.AddSyncSchema(schema);
            await syncManager.SyncDataAsync(schema, CancellationToken.None, null).ConfigureAwait(false);

            // ── Inspect reconciliation report ──────────────────────────────────────
            var report = syncManager.LastRunReconciliationReport;
            if (report != null)
            {
                Console.WriteLine($"--- Reconciliation Report ({report.RunId}) ---");
                Console.WriteLine($"  Source rows scanned : {report.SourceRowsScanned}");
                Console.WriteLine($"  Destination written : {report.DestRowsWritten}");
                Console.WriteLine($"  Inserted            : {report.DestRowsInserted}");
                Console.WriteLine($"  Updated             : {report.DestRowsUpdated}");
                Console.WriteLine($"  Skipped             : {report.DestRowsSkipped}");
                Console.WriteLine($"  DQ rejects          : {report.RejectCount}");
                Console.WriteLine($"  Defaults filled     : {report.DefaultsFillCount}");
                Console.WriteLine($"  Run aborted by DQ   : {report.RunAbortedByThreshold}");
                Console.WriteLine($"  Mapping quality     : {report.MappingQualityBand} ({report.MappingQualityScore})");

                // Detail per-record DQ failures (up to first 10 for brevity)
                int shown = 0;
                foreach (var failure in report.DqFailures)
                {
                    if (shown++ >= 10) { Console.WriteLine("  ... (truncated)"); break; }
                    Console.WriteLine($"  DQ fail: rule={failure.RuleKey} field={failure.FieldName}");
                }
            }

            await syncManager.SaveSchemasAsync().ConfigureAwait(false);
        }

        public static void Run(IDMEEditor editor) => RunAsync(editor).GetAwaiter().GetResult();

        public static void Run(IDMEEditor editor, IRuleEngine ruleEngine) =>
            RunAsync(editor, ruleEngine).GetAwaiter().GetResult();
    }
}
