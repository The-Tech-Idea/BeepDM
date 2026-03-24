// Example 03 — Incremental Sync & CDC
// Demonstrates WatermarkPolicy for Timestamp-based incremental and CDC change tracking.
//
// Phases covered: Phase 3 (incremental sync & CDC)

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
    /// Phase 3 — Incremental sync using a timestamp watermark (Upsert mode).
    /// Runs repeated delta syncs, each picking up only records changed since the previous run.
    /// </summary>
    public static class Example03_IncrementalSync
    {
        // ── Scenario A: Timestamp-based incremental (Upsert) ──────────────────────
        public static async Task RunUpsertAsync(IDMEEditor editor)
        {
            var syncManager = new BeepSyncManager(editor);
            var fieldHelper = new FieldMappingHelper(editor);

            var schema = new DataSyncSchema
            {
                Id                        = Guid.NewGuid().ToString(),
                SourceDataSourceName      = "SourceDB",
                DestinationDataSourceName = "DestDB",
                SourceEntityName          = "SalesOrders",
                DestinationEntityName     = "SalesOrders",
                SourceKeyField            = "OrderId",
                DestinationKeyField       = "OrderId",
                SyncType                  = "Incremental",
                SyncDirection             = "OneWay",
                BatchSize                 = 1000,

                // Watermark: use UpdatedAt as the high-water-mark field
                WatermarkPolicy = new WatermarkPolicy
                {
                    WatermarkMode         = "Timestamp",                // "Timestamp" | "Sequence" | "CompositeKey"
                    WatermarkField        = "UpdatedAt",
                    OverlapWindowSeconds  = 300,                       // re-scan last 5 min to catch late arrivals
                    DedupeStrategy        = "LastWrite",               // de-duplicate overlap-window duplicates
                    ReplayEnabled         = true,                      // idempotent re-runs safe
                    // LateArrivalRuleKey = "sync.watermark.late-arrival",  // optional rule override
                },
            };

            foreach (var m in fieldHelper.AutoMapFields("SourceDB", "SalesOrders", "DestDB", "SalesOrders"))
                schema.MappedFields.Add(m);

            // Validate watermark configuration before run
            var validationHelper = new SyncValidationHelper(editor);
            var wmResult = validationHelper.ValidateWatermarkPolicy(schema);
            if (wmResult.Flag == Errors.Failed)
            {
                Console.WriteLine($"[ERROR] Watermark policy invalid: {wmResult.Message}");
                return;
            }

            syncManager.AddSyncSchema(schema);

            // First run — syncs all records from epoch
            Console.WriteLine("=== Run 1 (first delta) ===");
            await syncManager.SyncDataAsync(schema, CancellationToken.None, null);
            Console.WriteLine($"LastSync: {schema.LastSyncDate:u}");

            // Simulate work happening in the source system...
            await Task.Delay(200);

            // Second run — picks up only records changed since Run 1
            Console.WriteLine("=== Run 2 (delta since Run 1) ===");
            await syncManager.SyncDataAsync(schema, CancellationToken.None, null);
            Console.WriteLine($"LastSync: {schema.LastSyncDate:u}");

            await syncManager.SaveSchemasAsync();
        }

        // ── Scenario B: CDC (change-data-capture) mode ────────────────────────────
        // CDC mode requires the source datasource to support change-feed queries.
        // The orchestrator builds a CdcFilterContext and passes it through the translator.
        public static async Task RunCdcAsync(IDMEEditor editor)
        {
            var syncManager = new BeepSyncManager(editor);
            var fieldHelper = new FieldMappingHelper(editor);

            var schema = new DataSyncSchema
            {
                Id                        = Guid.NewGuid().ToString(),
                SourceDataSourceName      = "SourceDB_CDC",
                DestinationDataSourceName = "DestDB",
                SourceEntityName          = "Inventory",
                DestinationEntityName     = "Inventory",
                SourceKeyField            = "ItemId",
                DestinationKeyField       = "ItemId",
                SyncType                  = "Incremental",
                SyncDirection             = "OneWay",

                WatermarkPolicy = new WatermarkPolicy
                {
                    WatermarkMode      = "CDC",
                    WatermarkField     = "RowVersion",   // CDC LSN or row-version column
                    ReplayEnabled      = true,
                    // TombstoneRuleKey: controls how deletes are handled
                    TombstoneRuleKey   = "sync.cdc.tombstone-soft-delete",
                },
            };

            foreach (var m in fieldHelper.AutoMapFields("SourceDB_CDC", "Inventory", "DestDB", "Inventory"))
                schema.MappedFields.Add(m);

            syncManager.AddSyncSchema(schema);
            await syncManager.SyncDataAsync(schema, CancellationToken.None, null);
            await syncManager.SaveSchemasAsync();
        }

        public static void RunUpsert(IDMEEditor editor) =>
            RunUpsertAsync(editor).GetAwaiter().GetResult();

        public static void RunCdc(IDMEEditor editor) =>
            RunCdcAsync(editor).GetAwaiter().GetResult();
    }
}
