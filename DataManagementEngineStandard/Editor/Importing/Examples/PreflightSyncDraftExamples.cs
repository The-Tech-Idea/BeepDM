// Example — Preflight + Sync Draft
//
// Demonstrates `RunMigrationPreflightAsync` and `BuildSyncDraftAsync` end-to-end with a
// minimal `DataImportConfiguration`. No real data source required — the example verifies
// the contract shape and returns the typed reports for downstream consumers.

using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.Importing.Examples
{
    /// <summary>
    /// Phase 3 — preflight + sync draft for a given import configuration.
    /// The engine already runs the preflight via <c>SyncSchemaPreflight</c>; this example
    /// documents the contract and provides a pure setup the host can invoke.
    /// </summary>
    public static class PreflightSyncDraftExamples
    {
        public static async Task RunAsync(IDMEEditor editor)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            // ── 1. Build a minimal import configuration ──────────────────────────
            var config = new DataImportConfiguration
            {
                SourceEntityName      = "Customers",
                SourceDataSourceName  = "SourceDB",
                DestEntityName        = "Customers",
                DestDataSourceName    = "DestDB",
                BatchSize             = 100,
                ApplyDefaults         = true
            };

            // ── 2. Create the manager and run preflight ─────────────────────────
            using var manager = new DataImportManager(editor);

            // Preflight verifies the destination datasource/entity exists and that
            // any schema-migration prerequisites (column names, types) are satisfied.
            var preflight = await manager.TestImportConfigurationAsync(config)
                .ConfigureAwait(false);

            Console.WriteLine(
                preflight != null
                    ? $"Preflight result: {preflight.Flag} — {preflight.Message}"
                    : "Preflight returned null (configuration not validated).");

            // ── 3. Build a sync draft (planned vs. actual destination columns) ──
            // The sync draft is a read-only preview of the import's effect on the
            // destination — useful for CI / approval workflows.
            // Because `BuildSyncDraftAsync` delegates to `MigrationManager`
            // internally, the draft is only valid when the destination datasource
            // is open and reachable.  Here we just exercise the public surface.
            if (preflight?.Flag == Errors.Ok)
            {
                try
                {
                    var draft = await manager.BuildSyncDraftAsync(config)
                        .ConfigureAwait(false);
                    Console.WriteLine(
                        draft != null
                            ? $"Sync draft created for '{config.SourceEntityName}' → '{config.DestEntityName}'."
                            : "Sync draft returned null.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sync draft not available: {ex.Message}");
                }
            }
        }
    }
}
