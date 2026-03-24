// Example 02 — Schema Governance & Versioning
// Demonstrates attaching a SyncSchemaVersion, promoting approval state,
// persisting versioned snapshots, and diffing against the stored baseline.
//
// Phases covered: Phase 2 (schema governance & versioning)

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
    /// Phase 2 — Schema versioning, approval-state lifecycle, and drift detection.
    /// </summary>
    public static class Example02_SchemaVersioning
    {
        public static async Task RunAsync(IDMEEditor editor)
        {
            var syncManager    = new BeepSyncManager(editor);
            var fieldHelper    = new FieldMappingHelper(editor);
            var persistHelper  = new SchemaPersistenceHelper(editor);

            // ── 1. Build schema (same as Example01) ───────────────────────────────
            var schema = new DataSyncSchema
            {
                Id                        = Guid.NewGuid().ToString(),
                SourceDataSourceName      = "SourceDB",
                DestinationDataSourceName = "DestDB",
                SourceEntityName          = "Orders",
                DestinationEntityName     = "Orders",
                SourceKeyField            = "OrderId",
                DestinationKeyField       = "OrderId",
                SyncType                  = "Full",
                SyncDirection             = "OneWay",
            };

            foreach (var m in fieldHelper.AutoMapFields("SourceDB", "Orders", "DestDB", "Orders"))
                schema.MappedFields.Add(m);

            // ── 2. Attach MappingPolicy (governance settings) ──────────────────────
            schema.MappingPolicy = new SyncMappingPolicy
            {
                Enabled              = true,
                MinQualityScore      = 80,              // block run if quality < 80
                RequiredApprovalState = "Approved",
                OnDriftAction        = "Warn",          // "Warn" | "Block" | "AutoRemapAndReview"
                CacheCompiledPlan    = true,
            };

            // ── 3. Attach an initial version snapshot ──────────────────────────────
            schema.CurrentSchemaVersion = new SyncSchemaVersion
            {
                SchemaId       = schema.Id,
                Version        = 1,
                ApprovalState  = "Draft",     // "Draft" | "Review" | "Approved"
                SavedBy        = "alice",
                SavedAt        = DateTime.UtcNow,
                MappingVersion = "v1.0",
                ChangeNotes    = "Initial mapping — auto-mapped from Orders entity.",
            };

            // ── 4. Persist the versioned snapshot ─────────────────────────────────
            await persistHelper.SaveVersionedSchemaAsync(schema, schema.CurrentSchemaVersion);

            // ── 5. Promote the schema to Approved ─────────────────────────────────
            // PromoteMappingState stamps ApprovalState and logs the transition.
            fieldHelper.PromoteMappingState(schema, "Approved");
            Console.WriteLine($"Approval state: {schema.CurrentSchemaVersion?.ApprovalState}");

            // ── 6. Simulate a field change then detect drift ───────────────────────
            schema.MappedFields.Add(new FieldSyncData
            {
                SourceField      = "Discount",
                DestinationField = "DiscountAmount",
            });

            var diff = await persistHelper.DiffSchemaToPersistedAsync(schema);
            if (!string.IsNullOrEmpty(diff))
                Console.WriteLine($"Drift detected:\n{diff}");
            else
                Console.WriteLine("No drift — schema matches persisted baseline.");

            // ── 7. Save new version after the mapping change ───────────────────────
            schema.CurrentSchemaVersion = new SyncSchemaVersion
            {
                SchemaId       = schema.Id,
                Version        = 2,
                ApprovalState  = "Draft",
                SavedBy        = "bob",
                SavedAt        = DateTime.UtcNow,
                MappingVersion = "v1.1",
                ChangeNotes    = "Added Discount → DiscountAmount field mapping.",
            };
            await persistHelper.SaveVersionedSchemaAsync(schema, schema.CurrentSchemaVersion);

            // ── 8. Load full version history ──────────────────────────────────────
            var history = await persistHelper.LoadSchemaVersionsAsync(schema.Id);
            foreach (var v in history)
                Console.WriteLine($"  v{v.Version} ({v.ApprovalState}) by {v.SavedBy} at {v.SavedAt:u}");

            // ── 9. Register, run, and save ─────────────────────────────────────────
            syncManager.AddSyncSchema(schema);
            await syncManager.SyncDataAsync(schema, CancellationToken.None, null);
            await syncManager.SaveSchemasAsync();
        }

        public static void Run(IDMEEditor editor) =>
            RunAsync(editor).GetAwaiter().GetResult();
    }
}
