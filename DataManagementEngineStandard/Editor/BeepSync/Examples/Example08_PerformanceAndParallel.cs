// Example 08 — Performance, Scale & Parallel Execution
// Demonstrates SyncPerformanceProfile, SyncRuleExecutionPolicies, and running multiple
// schemas concurrently via SyncAllDataParallelAsync.
//
// Phases covered: Phase 8 (performance & scale)

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Phase 8 — High-throughput sync configuration and parallel fan-out of multiple schemas.
    /// </summary>
    public static class Example08_PerformanceAndParallel
    {
        // ── Scenario A: Single schema tuned for throughput ─────────────────────────
        public static async Task RunHighThroughputAsync(IDMEEditor editor)
        {
            var syncManager = new BeepSyncManager(editor);
            var fieldHelper = new FieldMappingHelper(editor);

            var schema = new DataSyncSchema
            {
                Id                        = Guid.NewGuid().ToString(),
                SourceDataSourceName      = "SourceDB",
                DestinationDataSourceName = "DestDB",
                SourceEntityName          = "Metrics",
                DestinationEntityName     = "Metrics",
                SourceKeyField            = "MetricId",
                DestinationKeyField       = "MetricId",
                SyncType                  = "Incremental",
                SyncDirection             = "OneWay",

                WatermarkPolicy = new WatermarkPolicy
                {
                    WatermarkMode  = "Timestamp",
                    WatermarkField = "RecordedAt",
                    ReplayEnabled  = true,
                },

                // ── Performance profile ────────────────────────────────────────────
                PerfProfile = new SyncPerformanceProfile
                {
                    BatchSize                  = 5000,      // large batch — fewer round-trips
                    MaxParallelism             = 8,         // for SyncAllDataParallelAsync
                    RulePolicyMode             = "FastPath", // reduced depth, 2 s timeout
                    DefaultsCacheTtlSeconds    = 600,        // cache defaults profile 10 min
                    WarmUpDefaultsProfileOnRun = true,       // pre-load profile before retry loop
                    SkipRulesOnCleanBatch      = false,      // always evaluate rules (safe default)
                    UseParallelBatches         = true,
                    ParallelBatchQueueDepth    = 16,
                },
            };

            foreach (var m in fieldHelper.AutoMapFields("SourceDB", "Metrics", "DestDB", "Metrics"))
                schema.MappedFields.Add(m);

            syncManager.AddSyncSchema(schema);

            // Show the resolved rule policy (orchestrator uses this automatically)
            var policy = SyncRuleExecutionPolicies.Resolve(schema.PerfProfile.RulePolicyMode);
            Console.WriteLine($"Rule policy: MaxDepth={policy.MaxDepth}  MaxMs={policy.MaxExecutionMs}");

            var sw = Stopwatch.StartNew();
            await syncManager.SyncDataAsync(schema, CancellationToken.None, null);
            sw.Stop();

            Console.WriteLine($"Status   : {schema.SyncStatus}");
            Console.WriteLine($"Duration : {sw.ElapsedMilliseconds} ms");

            await syncManager.SaveSchemasAsync();
        }

        // ── Scenario B: Parallel fan-out — multiple schemas concurrently ───────────
        public static async Task RunParallelSchemasAsync(IDMEEditor editor)
        {
            var syncManager = new BeepSyncManager(editor);
            var fieldHelper = new FieldMappingHelper(editor);

            // Build several independent schemas for different entities
            var entities = new[] { "Orders", "Products", "Customers", "Inventory", "Transactions" };

            foreach (var entity in entities)
            {
                var schema = new DataSyncSchema
                {
                    Id                        = Guid.NewGuid().ToString(),
                    SourceDataSourceName      = "SourceDB",
                    DestinationDataSourceName = "DestDB",
                    SourceEntityName          = entity,
                    DestinationEntityName     = entity,
                    SourceKeyField            = entity.TrimEnd('s') + "Id",
                    DestinationKeyField       = entity.TrimEnd('s') + "Id",
                    SyncType                  = "Full",
                    SyncDirection             = "OneWay",

                    PerfProfile = new SyncPerformanceProfile
                    {
                        BatchSize          = 2000,
                        MaxParallelism     = 4,           // each schema contributes to max calc
                        RulePolicyMode     = "Safe",
                        UseParallelBatches = true,
                    },
                };

                foreach (var m in fieldHelper.AutoMapFields("SourceDB", entity, "DestDB", entity))
                    schema.MappedFields.Add(m);

                syncManager.AddSyncSchema(schema);
            }

            // SyncAllDataParallelAsync runs all registered schemas concurrently,
            // bounded by a SemaphoreSlim whose max degree = highest MaxParallelism across schemas.
            Console.WriteLine($"Running {entities.Length} schemas in parallel...");
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            var progress = new Progress<PassedArgs>(p =>
                Console.WriteLine($"  [{DateTime.Now:HH:mm:ss}] {p.Messege}"));

            var sw = Stopwatch.StartNew();
            await syncManager.SyncAllDataParallelAsync(cts.Token, progress);
            sw.Stop();

            Console.WriteLine($"All schemas completed in {sw.ElapsedMilliseconds} ms");

            // Summary
            foreach (var s in syncManager.SyncSchemas)
                Console.WriteLine($"  {s.DestinationEntityName,-15} → {s.SyncStatus}");

            await syncManager.SaveSchemasAsync();
        }

        // ── Scenario C: Sequential fallback ───────────────────────────────────────
        // Use SyncAllDataAsync when ordering matters or schemas share a destination.
        public static async Task RunSequentialAsync(IDMEEditor editor)
        {
            var syncManager = new BeepSyncManager(editor);
            // ... build and add schemas as needed ...
            await syncManager.SyncAllDataAsync(CancellationToken.None, null);
        }

        public static void RunHighThroughput(IDMEEditor editor) =>
            RunHighThroughputAsync(editor).GetAwaiter().GetResult();

        public static void RunParallelSchemas(IDMEEditor editor) =>
            RunParallelSchemasAsync(editor).GetAwaiter().GetResult();
    }
}
