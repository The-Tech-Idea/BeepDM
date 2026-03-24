// Example 04 — Bidirectional Conflict Resolution
// Demonstrates SyncDirection = "Bidirectional" with ConflictPolicy, conflict evidence
// capture, and quarantine routing for unresolvable records.
//
// Phases covered: Phase 4 (bidirectional conflict resolution)

using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;

namespace TheTechIdea.Beep.Editor.BeepSync.Examples
{
    /// <summary>
    /// Phase 4 — Two-way sync of a Products entity with last-write-wins conflict resolution.
    /// After the run the conflict evidence list is inspected for audit purposes.
    /// </summary>
    public static class Example04_BidirectionalConflict
    {
        public static async Task RunAsync(IDMEEditor editor)
        {
            var syncManager = new BeepSyncManager(editor);
            var fieldHelper = new FieldMappingHelper(editor);

            // ── 1. Schema: bidirectional ───────────────────────────────────────────
            var schema = new DataSyncSchema
            {
                Id                        = Guid.NewGuid().ToString(),
                SourceDataSourceName      = "RegionA_DB",
                DestinationDataSourceName = "RegionB_DB",
                SourceEntityName          = "Products",
                DestinationEntityName     = "Products",
                SourceKeyField            = "ProductId",
                DestinationKeyField       = "ProductId",
                SyncType                  = "Incremental",
                SyncDirection             = "Bidirectional",   // ← key setting
                BatchSize                 = 200,

                // Include a timestamp watermark so bidirectional logic has change context
                WatermarkPolicy = new WatermarkPolicy
                {
                    WatermarkMode    = "Timestamp",
                    WatermarkField   = "ModifiedAt",
                    ReplayEnabled    = true,
                },

                // ── Conflict policy ────────────────────────────────────────────────
                ConflictPolicy = new ConflictPolicy
                {
                    // Built-in rule keys:
                    //   "sync.conflict.source-wins"
                    //   "sync.conflict.destination-wins"
                    //   "sync.conflict.latest-timestamp-wins"
                    //   "sync.conflict.fail-on-conflict"
                    ResolutionRuleKey  = "sync.conflict.latest-timestamp-wins",
                    CaptureEvidence    = true,   // populate BeepSyncManager.LastRunConflicts

                    // Quarantine rows whose conflict couldn't be resolved
                    QuarantineDsName   = "QuarantineDB",
                    QuarantineEntity   = "ConflictQuarantine",

                    MaxConflictsPerRun    = 500,
                    OnMaxExceededAction   = "QuarantineRest",  // "Abort" | "Continue" | "QuarantineRest"
                },
            };

            foreach (var m in fieldHelper.AutoMapFields("RegionA_DB", "Products", "RegionB_DB", "Products"))
                schema.MappedFields.Add(m);

            syncManager.AddSyncSchema(schema);

            // ── 2. Run ─────────────────────────────────────────────────────────────
            await syncManager.SyncDataAsync(schema, CancellationToken.None, null);

            // ── 3. Inspect conflict evidence ───────────────────────────────────────
            var conflicts = syncManager.LastRunConflicts;
            Console.WriteLine($"Conflicts this run: {conflicts.Count}");

            foreach (var c in conflicts)
            {
                Console.WriteLine($"  Record  : {c.RecordKey}");
                Console.WriteLine($"  Rule    : {c.RuleKey}");
                Console.WriteLine($"  Winner  : {c.Winner}");
                Console.WriteLine($"  Reason  : {c.ReasonCode}");
                Console.WriteLine($"  At      : {c.DetectedAt:u}");
                Console.WriteLine();
            }

            // ── 4. Reconciliation summary ──────────────────────────────────────────
            var report = syncManager.LastRunReconciliationReport;
            if (report != null)
                Console.WriteLine($"Reconciliation — Source: {report.SourceRowsScanned}  " +
                                  $"Dest: {report.DestRowsWritten}  Conflicts: {report.ConflictCount}");

            await syncManager.SaveSchemasAsync();
        }

        public static void Run(IDMEEditor editor) =>
            RunAsync(editor).GetAwaiter().GetResult();
    }
}
