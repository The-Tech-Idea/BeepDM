// Example 07 — Observability, SLO & Alerting
// Demonstrates SloProfile configuration, SyncMetrics inspection, SLO compliance tier,
// and consuming the alert records generated at the end of a run.
//
// Phases covered: Phase 7 (observability, SLO & alerting)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor.BeepSync.Examples
{
    /// <summary>
    /// Phase 7 — SLO-enforced sync with alert rules and structured run metrics.
    /// Shows how to wire a Rule Engine via SyncIntegrationContext so alert rules fire.
    /// </summary>
    public static class Example07_SloAndAlerting
    {
        public static async Task RunAsync(IDMEEditor editor, IRuleEngine ruleEngine = null)
        {
            // ── 1. Build integration context with optional Rule Engine ─────────────
            // Providing a Rule Engine enables alert rule evaluation and SLO classification.
            var context = new SyncIntegrationContext
            {
                RuleEngine = ruleEngine,  // null = SLO rules evaluated without RE (degraded mode)
            };

            var syncManager = new BeepSyncManager(editor, context);
            var fieldHelper = new FieldMappingHelper(editor);

            // ── 2. Schema ──────────────────────────────────────────────────────────
            var schema = new DataSyncSchema
            {
                Id                        = Guid.NewGuid().ToString(),
                SourceDataSourceName      = "SourceDB",
                DestinationDataSourceName = "DestDB",
                SourceEntityName          = "Transactions",
                DestinationEntityName     = "Transactions",
                SourceKeyField            = "TxId",
                DestinationKeyField       = "TxId",
                SyncType                  = "Incremental",
                SyncDirection             = "OneWay",
                BatchSize                 = 500,

                WatermarkPolicy = new WatermarkPolicy
                {
                    WatermarkMode  = "Timestamp",
                    WatermarkField = "CreatedAt",
                    ReplayEnabled  = true,
                },

                // ── SLO profile ────────────────────────────────────────────────────
                SloProfile = new SloProfile
                {
                    ProfileName           = "Critical",
                    MinSuccessRate        = 0.99,      // alert if success rate drops below 99 %
                    MaxDurationMs         = 60_000,    // flag if run takes > 60 s
                    MaxFreshnessLagSeconds = 300,       // flag if data is > 5 min stale
                    MaxConflictRate       = 0.01,      // flag if > 1 % of records conflict
                    MaxRejectRate         = 0.02,      // flag if > 2 % of records rejected

                    // Alert rule keys evaluated against SyncMetrics at end of run.
                    // Rule outputs: severity (Warning|Critical), message, metadata dict.
                    AlertRuleKeys = new List<string>
                    {
                        "sync.alert.low-success-rate",
                        "sync.alert.high-reject-rate",
                        "sync.alert.freshness-lag",
                        "sync.alert.long-duration",
                    },
                },

                // Rule policy for schema-level rules (separate from alert rules)
                RulePolicy = new SyncRulePolicy
                {
                    Enabled        = ruleEngine != null,
                    MaxDepth       = 10,
                    MaxExecutionMs = 5000,
                },
            };

            foreach (var m in fieldHelper.AutoMapFields("SourceDB", "Transactions", "DestDB", "Transactions"))
                schema.MappedFields.Add(m);

            syncManager.AddSyncSchema(schema);

            // ── 3. Run ─────────────────────────────────────────────────────────────
            await syncManager.SyncDataAsync(schema, CancellationToken.None, null);

            // ── 4. Inspect SyncMetrics (Phase 7 fields) ────────────────────────────
            // SyncMetrics are stored on schema.LastSyncRunData.Metadata or via reconciliation.
            // The orchestrator stamps SloComplianceTier via EmitSloMetrics().
            var report = syncManager.LastRunReconciliationReport;
            if (report != null)
            {
                Console.WriteLine("=== Run Metrics ===");
                Console.WriteLine($"  Records      : {report.SourceRowsScanned}");
                Console.WriteLine($"  Reject rate  : {report.RejectCount * 100.0 / Math.Max(1, report.SourceRowsScanned):F2} %");
                Console.WriteLine($"  Conflicts    : {report.ConflictCount}");
                Console.WriteLine($"  Aborted      : {report.RunAbortedByThreshold}");
            }

            // ── 5. Inspect alert records ───────────────────────────────────────────
            // schema.LastRunAlerts is populated by EvaluateAlertRules() inside the orchestrator.
            var alerts = schema.LastRunAlerts;
            if (alerts == null || alerts.Count == 0)
            {
                Console.WriteLine("No alerts fired — run within SLO.");
            }
            else
            {
                Console.WriteLine($"=== {alerts.Count} Alert(s) ===");
                foreach (var alert in alerts)
                {
                    Console.WriteLine($"  [{alert.Severity}] {alert.RuleKey}");
                    Console.WriteLine($"    {alert.Reason}");
                    Console.WriteLine($"    Schema: {alert.SchemaId}  Run: {alert.RunId}  At: {alert.EmittedAt:u}");
                }
            }

            await syncManager.SaveSchemasAsync();
        }

        public static void Run(IDMEEditor editor, IRuleEngine ruleEngine = null) =>
            RunAsync(editor, ruleEngine).GetAwaiter().GetResult();
    }
}
