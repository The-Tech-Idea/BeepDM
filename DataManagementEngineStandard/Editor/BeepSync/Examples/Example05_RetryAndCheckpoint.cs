// Example 05 — Retry, Checkpoint & Idempotency
// Demonstrates RetryPolicy with exponential back-off, mid-run checkpoint persistence,
// and how to force a full re-run by clearing the active checkpoint.
//
// Phases covered: Phase 5 (reliability, retry & idempotency)

using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.BeepSync.Examples
{
    /// <summary>
    /// Phase 5 — Large-table sync with retry on transient errors and checkpoint-based resume.
    /// </summary>
    public static class Example05_RetryAndCheckpoint
    {
        public static async Task RunAsync(IDMEEditor editor)
        {
            var syncManager = new BeepSyncManager(editor);
            var fieldHelper = new FieldMappingHelper(editor);

            var schema = new DataSyncSchema
            {
                Id                        = Guid.NewGuid().ToString(),
                SourceDataSourceName      = "SourceDB",
                DestinationDataSourceName = "DestDB",
                SourceEntityName          = "EventLog",
                DestinationEntityName     = "EventLog",
                SourceKeyField            = "EventId",
                DestinationKeyField       = "EventId",
                SyncType                  = "Incremental",
                SyncDirection             = "OneWay",
                BatchSize                 = 2000,

                WatermarkPolicy = new WatermarkPolicy
                {
                    WatermarkMode  = "Sequence",
                    WatermarkField = "EventId",
                    ReplayEnabled  = true,
                },

                // ── Retry policy ───────────────────────────────────────────────────
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts           = 4,
                    BaseDelayMs           = 2000,
                    BackoffMode           = "Exponential",  // delay doubles each retry: 2s, 4s, 8s
                    MaxResumeWindowHours  = 48,             // checkpoint stale after 48 h

                    CheckpointEnabled     = true,           // save checkpoint after each committed batch

                    // Optional: Rule Engine key that classifies errors into categories.
                    // Outputs: category (Transient|Validation|Conflict|Fatal)  action (Retry|Abort|Quarantine)
                    ErrorCategoryRuleKey  = "sync.error.classify",

                    NonRetryableCategories = new System.Collections.Generic.List<string>
                        { "Fatal", "Validation" },
                },
            };

            foreach (var m in fieldHelper.AutoMapFields("SourceDB", "EventLog", "DestDB", "EventLog"))
                schema.MappedFields.Add(m);

            syncManager.AddSyncSchema(schema);

            // ── First run: saves a checkpoint after each batch ─────────────────────
            var progress = new Progress<PassedArgs>(p =>
                Console.WriteLine($"  {p.Messege}"));

            Console.WriteLine("=== First run ===");
            await syncManager.SyncDataAsync(schema, CancellationToken.None, progress);
            Console.WriteLine($"Status:     {schema.SyncStatus}");
            Console.WriteLine($"Checkpoint: offset={schema.ActiveCheckpoint?.LastProcessedKeyValue}");

            // ── Simulate partial failure & resume ──────────────────────────────────
            // On retry the orchestrator reads schema.ActiveCheckpoint and resumes from
            // LastProcessedKeyValue, skipping already-committed records.
            Console.WriteLine("\n=== Retry run (resumes from checkpoint) ===");
            await syncManager.SyncDataAsync(schema, CancellationToken.None, progress);

            // ── Force a complete re-run ────────────────────────────────────────────
            // Clear the checkpoint so the next run processes from the beginning.
            schema.ActiveCheckpoint = null;
            Console.WriteLine("\n=== Full re-run (checkpoint cleared) ===");
            await syncManager.SyncDataAsync(schema, CancellationToken.None, progress);

            await syncManager.SaveSchemasAsync();
        }

        public static void Run(IDMEEditor editor) =>
            RunAsync(editor).GetAwaiter().GetResult();
    }
}
