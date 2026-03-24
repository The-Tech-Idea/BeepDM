// Example 01 — Basic Full Sync
// Demonstrates creating a DataSyncSchema for a one-way full sync, auto-mapping fields,
// running the sync, and inspecting results.
//
// Phases covered: Phase 1 (contracts & foundation)

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
    /// Phase 1 — Basic full sync from SourceDB.Customers to DestDB.Customers.
    /// </summary>
    public static class Example01_BasicFullSync
    {
        public static async Task RunAsync(IDMEEditor editor)
        {
            // ── 1. Create the manager ──────────────────────────────────────────────
            var syncManager = new BeepSyncManager(editor);

            // ── 2. Build the schema ────────────────────────────────────────────────
            var schema = new DataSyncSchema
            {
                Id                        = Guid.NewGuid().ToString(),
                SourceDataSourceName      = "SourceDB",
                DestinationDataSourceName = "DestDB",
                SourceEntityName          = "Customers",
                DestinationEntityName     = "Customers",
                SourceKeyField            = "CustomerId",
                DestinationKeyField       = "CustomerId",
                SyncType                  = "Full",      // "Full" | "Incremental"
                SyncDirection             = "OneWay",    // "OneWay" | "Bidirectional"
                BatchSize                 = 500,
            };

            // ── 3. Auto-map fields ─────────────────────────────────────────────────
            // FieldMappingHelper matches source/destination fields by name.
            var fieldMappingHelper = new FieldMappingHelper(editor);
            var autoMapped = fieldMappingHelper.AutoMapFields(
                "SourceDB", "Customers",
                "DestDB",   "Customers");

            foreach (var mapping in autoMapped)
                schema.MappedFields.Add(mapping);

            // Manually add a renamed field not picked up by auto-map:
            // schema.MappedFields.Add(new FieldSyncData
            // {
            //     SourceField      = "ContactName",
            //     DestinationField = "FullName",
            // });

            // ── 4. Validate ────────────────────────────────────────────────────────
            var validation = syncManager.ValidateSchema(schema);
            if (validation.Flag == Errors.Failed)
            {
                Console.WriteLine($"[WARN] Schema validation issues: {validation.Message}");
                // Up to the caller whether to proceed or abort.
            }

            // ── 5. Register and run ────────────────────────────────────────────────
            syncManager.AddSyncSchema(schema);

            var progress = new Progress<PassedArgs>(p =>
                Console.WriteLine($"  Progress: {p.Messege}"));

            await syncManager.SyncDataAsync(schema, CancellationToken.None, progress);

            // ── 6. Inspect results ─────────────────────────────────────────────────
            Console.WriteLine($"Status : {schema.SyncStatus}");
            Console.WriteLine($"Message: {schema.SyncStatusMessage}");
            Console.WriteLine($"LastSync: {schema.LastSyncDate:u}");

            // ── 7. Persist schemas ─────────────────────────────────────────────────
            await syncManager.SaveSchemasAsync();
        }

        // Synchronous helper for console / WinForms callers
        public static void Run(IDMEEditor editor) =>
            RunAsync(editor).GetAwaiter().GetResult();
    }
}
