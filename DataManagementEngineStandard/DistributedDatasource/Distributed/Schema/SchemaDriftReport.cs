using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Aggregated drift report produced by
    /// <see cref="IDistributedSchemaService.DetectSchemaDriftAsync"/>.
    /// Groups <see cref="SchemaDriftEntry"/> records by entity and
    /// exposes a stable summary suitable for CI gating.
    /// </summary>
    /// <remarks>
    /// Reports are immutable. Construction does not defensively copy the
    /// caller's list; callers are expected to pass a materialised
    /// enumerable (the partial uses a local <see cref="List{T}"/>).
    /// </remarks>
    public sealed class SchemaDriftReport
    {
        /// <summary>Initialises a new report.</summary>
        /// <param name="referenceShardId">Shard used as the reference (canonical) structure source; may be <c>null</c> when the report is empty.</param>
        /// <param name="entriesByEntity">Per-entity drift entries (case-insensitive key). Never <c>null</c>.</param>
        /// <param name="samplingErrorsByShard">Shard id → exception raised while sampling. Never <c>null</c>.</param>
        public SchemaDriftReport(
            string                                                 referenceShardId,
            IReadOnlyDictionary<string, IReadOnlyList<SchemaDriftEntry>> entriesByEntity,
            IReadOnlyDictionary<string, Exception>                 samplingErrorsByShard)
        {
            ReferenceShardId      = referenceShardId;
            EntriesByEntity       = entriesByEntity
                ?? new Dictionary<string, IReadOnlyList<SchemaDriftEntry>>(
                        0, StringComparer.OrdinalIgnoreCase);
            SamplingErrorsByShard = samplingErrorsByShard
                ?? new Dictionary<string, Exception>(0, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Shard whose structure was taken as the reference; <c>null</c> for an empty / all-failed sweep.</summary>
        public string ReferenceShardId { get; }

        /// <summary>Drift entries keyed by entity name (case-insensitive).</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<SchemaDriftEntry>> EntriesByEntity { get; }

        /// <summary>Sampling exceptions keyed by shard id (case-insensitive).</summary>
        public IReadOnlyDictionary<string, Exception> SamplingErrorsByShard { get; }

        /// <summary>Returns <c>true</c> when no drift entries exist and no shards failed sampling.</summary>
        public bool IsClean
            => EntriesByEntity.Count == 0 && SamplingErrorsByShard.Count == 0;

        /// <summary>Total drift entries across all entities.</summary>
        public int TotalDriftCount
            => EntriesByEntity.Values.Sum(list => list.Count);

        /// <summary>Enumerates every drift entry in a stable order (entity name, then shard id).</summary>
        public IEnumerable<SchemaDriftEntry> Enumerate()
        {
            foreach (var kv in EntriesByEntity.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var entry in kv.Value
                                        .OrderBy(e => e.ShardId,  StringComparer.OrdinalIgnoreCase)
                                        .ThenBy(e => e.ColumnName ?? string.Empty,
                                                StringComparer.OrdinalIgnoreCase)
                                        .ThenBy(e => e.Kind))
                {
                    yield return entry;
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"SchemaDriftReport(ref='{ReferenceShardId ?? "(none)"}', entries={TotalDriftCount}, sampleErrors={SamplingErrorsByShard.Count})";
    }
}
