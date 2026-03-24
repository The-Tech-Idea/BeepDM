using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        /// <summary>Runs all registered schemas sequentially.</summary>
        public async Task SyncAllDataAsync(CancellationToken token, IProgress<PassedArgs> progress)
        {
            foreach (var schema in SyncSchemas.ToList())
            {
                await SyncDataAsync(schema, token, progress);
                if (token.IsCancellationRequested) break;
            }
        }

        /// <summary>
        /// Runs all registered schemas concurrently, bounded by the highest
        /// <see cref="SyncPerformanceProfile.MaxParallelism"/> across all schemas (floor = 1).
        /// </summary>
        public async Task SyncAllDataParallelAsync(
            CancellationToken token = default,
            IProgress<PassedArgs> progress = null)
        {
            var schemas = SyncSchemas.ToList();
            int maxDegree = Math.Max(1,
                schemas.Select(s => s.PerfProfile?.MaxParallelism ?? 1)
                       .DefaultIfEmpty(4)
                       .Max());

            using var semaphore = new SemaphoreSlim(maxDegree, maxDegree);
            var tasks = schemas.Select(async schema =>
            {
                await semaphore.WaitAsync(token).ConfigureAwait(false);
                try   { await SyncDataAsync(schema, token, progress).ConfigureAwait(false); }
                finally { semaphore.Release(); }
            });
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>Synchronous sequential run of all schemas.</summary>
        public void SyncAllData()
        {
            foreach (var schema in SyncSchemas.ToList())
                SyncData(schema);
        }

        // ── Convenience overloads ─────────────────────────────────────────────────

        public void SyncData(string schemaId) => WithSchema(schemaId, SyncData);

        public void SyncData(string schemaId, string sourceEntity, string destEntity) =>
            WithSchema(schemaId, s => { s.SourceEntityName = sourceEntity; s.DestinationEntityName = destEntity; SyncData(s); });

        public void SyncData(string schemaId, string sourceEntity, string destEntity, string sourceSyncField) =>
            WithSchema(schemaId, s => { s.SourceEntityName = sourceEntity; s.DestinationEntityName = destEntity; s.SourceSyncDataField = sourceSyncField; SyncData(s); });

        public void SyncData(string schemaId, string sourceEntity, string destEntity, string sourceSyncField, string destSyncField) =>
            WithSchema(schemaId, s => { s.SourceEntityName = sourceEntity; s.DestinationEntityName = destEntity; s.SourceSyncDataField = sourceSyncField; s.DestinationSyncDataField = destSyncField; SyncData(s); });

        public void SyncData(string schemaId, string sourceEntity, string destEntity, string sourceSyncField, string destSyncField, string syncType) =>
            WithSchema(schemaId, s => { s.SourceEntityName = sourceEntity; s.DestinationEntityName = destEntity; s.SourceSyncDataField = sourceSyncField; s.DestinationSyncDataField = destSyncField; s.SyncType = syncType; SyncData(s); });

        public void SyncData(string schemaId, string sourceEntity, string destEntity, string sourceSyncField, string destSyncField, string syncType, string syncDirection) =>
            WithSchema(schemaId, s => { s.SourceEntityName = sourceEntity; s.DestinationEntityName = destEntity; s.SourceSyncDataField = sourceSyncField; s.DestinationSyncDataField = destSyncField; s.SyncType = syncType; s.SyncDirection = syncDirection; SyncData(s); });

        private void WithSchema(string schemaId, Action<DataSyncSchema> action)
        {
            var s = FindSchema(schemaId);
            if (s != null) action(s);
        }
    }
}
