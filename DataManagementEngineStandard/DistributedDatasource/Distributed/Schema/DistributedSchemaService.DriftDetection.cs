using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// <see cref="DistributedSchemaService"/> partial — drift detection.
    /// For every entity in the active plan, samples the per-shard
    /// <see cref="EntityStructure"/>, picks a reference shard, and
    /// flags any shard whose structure disagrees with the reference.
    /// </summary>
    /// <remarks>
    /// The reference is deterministic: the first successfully-sampled
    /// shard in the placement's ordered list. When every shard fails
    /// sampling the report surfaces those exceptions via
    /// <see cref="SchemaDriftReport.SamplingErrorsByShard"/> and
    /// contains no drift entries for the entity.
    /// </remarks>
    public sealed partial class DistributedSchemaService
    {
        /// <inheritdoc/>
        public Task<SchemaDriftReport> DetectSchemaDriftAsync(
            CancellationToken cancellationToken = default)
        {
            var plan = _getCurrentPlan();
            if (plan == null || plan.IsEmpty)
            {
                return Task.FromResult(new SchemaDriftReport(
                    referenceShardId:      null,
                    entriesByEntity:       new Dictionary<string, IReadOnlyList<SchemaDriftEntry>>(
                                               0, StringComparer.OrdinalIgnoreCase),
                    samplingErrorsByShard: new Dictionary<string, Exception>(
                                               0, StringComparer.OrdinalIgnoreCase)));
            }

            var entriesByEntity   = new Dictionary<string, IReadOnlyList<SchemaDriftEntry>>(
                                        StringComparer.OrdinalIgnoreCase);
            var samplingErrors    = new Dictionary<string, Exception>(
                                        StringComparer.OrdinalIgnoreCase);
            string firstReference = null;

            foreach (var kv in plan.EntityPlacements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entries = new List<SchemaDriftEntry>();
                var refShard = SampleEntityStructures(
                    kv.Value.EntityName,
                    kv.Value.ShardIds,
                    entries,
                    samplingErrors);

                if (firstReference == null && refShard != null)
                    firstReference = refShard;

                if (entries.Count > 0)
                    entriesByEntity[kv.Key] = entries;
            }

            return Task.FromResult(new SchemaDriftReport(
                referenceShardId:      firstReference,
                entriesByEntity:       entriesByEntity,
                samplingErrorsByShard: samplingErrors));
        }

        private string SampleEntityStructures(
            string                       entityName,
            IReadOnlyList<string>        shardIds,
            List<SchemaDriftEntry>       entries,
            Dictionary<string, Exception> samplingErrors)
        {
            EntityStructure reference   = null;
            string          referenceId = null;
            var             samples     = new List<(string shardId, EntityStructure structure)>();

            foreach (var shardId in shardIds)
            {
                var cluster = _resolveShard(shardId);
                if (cluster == null)
                {
                    entries.Add(new SchemaDriftEntry(
                        entityName, shardId, SchemaDriftKind.SamplingFailed,
                        samplingException: new InvalidOperationException(
                            $"Shard '{shardId}' is not registered.")));
                    continue;
                }

                EntityStructure observed;
                try
                {
                    observed = cluster.GetEntityStructure(entityName, refresh: true);
                }
                catch (Exception ex)
                {
                    entries.Add(new SchemaDriftEntry(
                        entityName, shardId, SchemaDriftKind.SamplingFailed,
                        samplingException: ex));
                    samplingErrors[shardId] = ex;
                    continue;
                }

                if (observed == null)
                {
                    entries.Add(new SchemaDriftEntry(
                        entityName, shardId, SchemaDriftKind.MissingEntity));
                    continue;
                }

                samples.Add((shardId, observed));
                if (reference == null)
                {
                    reference   = observed;
                    referenceId = shardId;
                }
            }

            if (reference == null) return null;

            foreach (var (shardId, observed) in samples)
            {
                if (string.Equals(shardId, referenceId, StringComparison.OrdinalIgnoreCase))
                    continue;
                CompareStructures(entityName, referenceId, shardId, reference, observed, entries);
            }

            return referenceId;
        }

        private static void CompareStructures(
            string                      entityName,
            string                      referenceShardId,
            string                      observedShardId,
            EntityStructure             reference,
            EntityStructure             observed,
            List<SchemaDriftEntry>      entries)
        {
            var refFields = BuildFieldMap(reference.Fields);
            var obsFields = BuildFieldMap(observed.Fields);

            foreach (var kv in refFields)
            {
                if (!obsFields.TryGetValue(kv.Key, out var obs))
                {
                    entries.Add(new SchemaDriftEntry(
                        entityName, observedShardId, SchemaDriftKind.MissingColumn,
                        columnName:    kv.Key,
                        expectedValue: DescribeField(kv.Value)));
                    continue;
                }
                CompareField(entityName, observedShardId, kv.Key, kv.Value, obs, entries);
            }

            foreach (var kv in obsFields)
            {
                if (refFields.ContainsKey(kv.Key)) continue;
                entries.Add(new SchemaDriftEntry(
                    entityName, observedShardId, SchemaDriftKind.ExtraColumn,
                    columnName:    kv.Key,
                    observedValue: DescribeField(kv.Value)));
            }
        }

        private static void CompareField(
            string                     entityName,
            string                     shardId,
            string                     fieldName,
            EntityField                reference,
            EntityField                observed,
            List<SchemaDriftEntry>     entries)
        {
            if (!string.Equals(reference.Fieldtype ?? string.Empty,
                               observed.Fieldtype  ?? string.Empty,
                               StringComparison.OrdinalIgnoreCase)
                || reference.Size != observed.Size
                || reference.NumericPrecision != observed.NumericPrecision)
            {
                entries.Add(new SchemaDriftEntry(
                    entityName, shardId, SchemaDriftKind.ColumnTypeMismatch,
                    columnName:    fieldName,
                    expectedValue: DescribeField(reference),
                    observedValue: DescribeField(observed)));
            }

            if (reference.AllowDBNull != observed.AllowDBNull)
            {
                entries.Add(new SchemaDriftEntry(
                    entityName, shardId, SchemaDriftKind.ColumnNullabilityMismatch,
                    columnName:    fieldName,
                    expectedValue: reference.AllowDBNull ? "NULL" : "NOT NULL",
                    observedValue: observed.AllowDBNull  ? "NULL" : "NOT NULL"));
            }

            if (reference.IsKey != observed.IsKey)
            {
                entries.Add(new SchemaDriftEntry(
                    entityName, shardId, SchemaDriftKind.PrimaryKeyMismatch,
                    columnName:    fieldName,
                    expectedValue: reference.IsKey ? "KEY" : "NOT_KEY",
                    observedValue: observed.IsKey  ? "KEY" : "NOT_KEY"));
            }

            if (reference.IsAutoIncrement != observed.IsAutoIncrement)
            {
                entries.Add(new SchemaDriftEntry(
                    entityName, shardId, SchemaDriftKind.IdentityMismatch,
                    columnName:    fieldName,
                    expectedValue: reference.IsAutoIncrement ? "IDENTITY" : "PLAIN",
                    observedValue: observed.IsAutoIncrement  ? "IDENTITY" : "PLAIN"));
            }
        }

        private static Dictionary<string, EntityField> BuildFieldMap(IEnumerable<EntityField> fields)
        {
            var result = new Dictionary<string, EntityField>(StringComparer.OrdinalIgnoreCase);
            if (fields == null) return result;
            foreach (var f in fields)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;
                result[f.FieldName] = f;
            }
            return result;
        }

        private static string DescribeField(EntityField field)
        {
            if (field == null) return "(null)";
            var type = field.Fieldtype ?? "?";
            var size = field.Size > 0 ? "(" + field.Size + ")" : string.Empty;
            var nn   = field.AllowDBNull ? "NULL" : "NOT NULL";
            var key  = field.IsKey ? " PK" : string.Empty;
            var ai   = field.IsAutoIncrement ? " IDENTITY" : string.Empty;
            return type + size + " " + nn + key + ai;
        }
    }
}
