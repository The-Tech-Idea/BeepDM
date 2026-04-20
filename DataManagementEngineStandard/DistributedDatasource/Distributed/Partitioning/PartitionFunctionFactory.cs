using System;
using System.Collections.Generic;
using System.Text.Json;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Partitioning
{
    /// <summary>
    /// Materialises an <see cref="IPartitionFunction"/> from a
    /// <see cref="PartitionFunctionRef"/> + the placement's shard
    /// list. Encapsulates the JSON parsing rules used by the
    /// persisted form of each function (boundaries, value maps,
    /// composite child refs).
    /// </summary>
    /// <remarks>
    /// <para>
    /// JSON shapes accepted in <see cref="PartitionFunctionRef.Parameters"/>:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>Hash</c>: optional <c>"VirtualSlots"</c> integer.</item>
    ///   <item><c>Range</c>: required <c>"Boundaries"</c> JSON array of <c>{ "max": &lt;value-or-null&gt;, "shardId": "..." }</c>.</item>
    ///   <item><c>List</c>: required <c>"Values"</c> JSON object (<c>{ "EU": "shard-1", "US": "shard-2" }</c>); optional <c>"DefaultShardId"</c>.</item>
    ///   <item><c>Composite</c>: required <c>"Functions"</c> JSON array of nested <see cref="PartitionFunctionRef"/> shapes (<c>{ "kind": "...", "keyColumns": [...], "parameters": {...} }</c>).</item>
    /// </list>
    /// </remarks>
    public static class PartitionFunctionFactory
    {
        /// <summary>Parameter key for hash virtual-slot override.</summary>
        public const string VirtualSlotsParameterKey = HashPartitionFunction.VirtualSlotsParameterKey;
        /// <summary>Parameter key for range boundaries JSON.</summary>
        public const string BoundariesParameterKey   = "Boundaries";
        /// <summary>Parameter key for list value map JSON.</summary>
        public const string ValuesParameterKey       = "Values";
        /// <summary>Parameter key for list default shard fallback.</summary>
        public const string DefaultShardParameterKey = "DefaultShardId";
        /// <summary>Parameter key for composite inner functions JSON.</summary>
        public const string FunctionsParameterKey    = "Functions";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Builds an <see cref="IPartitionFunction"/> from
        /// <paramref name="reference"/>. Shards from
        /// <paramref name="placementShardIds"/> are required for
        /// <see cref="PartitionKind.Hash"/>; ignored otherwise (the
        /// other functions encode their shard targets in their
        /// parameters).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reference"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Reference kind is <see cref="PartitionKind.None"/> or required parameters are missing/malformed.</exception>
        public static IPartitionFunction Create(
            PartitionFunctionRef  reference,
            IReadOnlyList<string> placementShardIds = null)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));

            switch (reference.Kind)
            {
                case PartitionKind.Hash:
                    return BuildHash(reference, placementShardIds);
                case PartitionKind.Range:
                    return BuildRange(reference);
                case PartitionKind.List:
                    return BuildList(reference);
                case PartitionKind.Composite:
                    return BuildComposite(reference, placementShardIds);
                case PartitionKind.None:
                default:
                    throw new ArgumentException(
                        $"PartitionFunctionRef.Kind '{reference.Kind}' is not supported by PartitionFunctionFactory.",
                        nameof(reference));
            }
        }

        // ── Builders ──────────────────────────────────────────────────────

        private static IPartitionFunction BuildHash(
            PartitionFunctionRef  reference,
            IReadOnlyList<string> placementShardIds)
        {
            if (placementShardIds == null || placementShardIds.Count == 0)
                throw new ArgumentException(
                    "Hash partition functions require at least one placement shard id.",
                    nameof(placementShardIds));

            int virtualSlots = HashPartitionFunction.DefaultVirtualSlots;
            if (TryGetParam(reference, VirtualSlotsParameterKey, out var raw) &&
                int.TryParse(raw, out var parsed) && parsed > 0)
            {
                virtualSlots = parsed;
            }

            return new HashPartitionFunction(reference.KeyColumns, placementShardIds, virtualSlots);
        }

        private static IPartitionFunction BuildRange(PartitionFunctionRef reference)
        {
            if (!TryGetParam(reference, BoundariesParameterKey, out var json))
                throw new ArgumentException(
                    $"Range partition function requires a '{BoundariesParameterKey}' parameter.",
                    nameof(reference));

            var entries = JsonSerializer.Deserialize<List<RangeBoundaryDto>>(json, JsonOptions)
                          ?? new List<RangeBoundaryDto>();
            if (entries.Count == 0)
                throw new ArgumentException(
                    $"Range partition function '{BoundariesParameterKey}' parameter is empty.",
                    nameof(reference));

            var boundaries = new List<RangePartitionBoundary>(entries.Count);
            foreach (var dto in entries)
            {
                if (string.IsNullOrWhiteSpace(dto.ShardId))
                    throw new ArgumentException("Range boundary missing shardId.", nameof(reference));
                boundaries.Add(new RangePartitionBoundary(NormaliseJsonValue(dto.Max), dto.ShardId));
            }

            return new RangePartitionFunction(reference.KeyColumns, boundaries);
        }

        private static IPartitionFunction BuildList(PartitionFunctionRef reference)
        {
            if (!TryGetParam(reference, ValuesParameterKey, out var json))
                throw new ArgumentException(
                    $"List partition function requires a '{ValuesParameterKey}' parameter.",
                    nameof(reference));

            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                      ?? new Dictionary<string, string>();

            var valueMap = new Dictionary<object, string>(raw.Count);
            foreach (var kv in raw)
                valueMap[kv.Key] = kv.Value;

            string defaultShardId = null;
            if (TryGetParam(reference, DefaultShardParameterKey, out var dflt) &&
                !string.IsNullOrWhiteSpace(dflt))
            {
                defaultShardId = dflt;
            }

            return new ListPartitionFunction(reference.KeyColumns, valueMap, defaultShardId);
        }

        private static IPartitionFunction BuildComposite(
            PartitionFunctionRef  reference,
            IReadOnlyList<string> placementShardIds)
        {
            if (!TryGetParam(reference, FunctionsParameterKey, out var json))
                throw new ArgumentException(
                    $"Composite partition function requires a '{FunctionsParameterKey}' parameter.",
                    nameof(reference));

            var children = JsonSerializer.Deserialize<List<NestedFunctionDto>>(json, JsonOptions)
                           ?? new List<NestedFunctionDto>();
            if (children.Count < 2)
                throw new ArgumentException(
                    "Composite partition function requires at least two nested functions.",
                    nameof(reference));

            var inner = new List<IPartitionFunction>(children.Count);
            foreach (var dto in children)
            {
                if (!Enum.TryParse<PartitionKind>(dto.Kind, ignoreCase: true, out var kind))
                    throw new ArgumentException(
                        $"Composite child has unknown kind '{dto.Kind}'.", nameof(reference));
                if (dto.KeyColumns == null || dto.KeyColumns.Count == 0)
                    throw new ArgumentException(
                        "Composite child is missing keyColumns.", nameof(reference));

                var nestedRef = new PartitionFunctionRef(kind, dto.KeyColumns, dto.Parameters);
                inner.Add(Create(nestedRef, placementShardIds));
            }

            return new CompositePartitionFunction(inner);
        }

        // ── Parameter helpers ─────────────────────────────────────────────

        private static bool TryGetParam(
            PartitionFunctionRef reference,
            string               key,
            out string           value)
        {
            value = null;
            if (reference.Parameters == null || reference.Parameters.Count == 0)
                return false;

            foreach (var kv in reference.Parameters)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kv.Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Converts a <see cref="JsonElement"/> produced by
        /// deserialising an <c>object</c>-typed JSON property into the
        /// closest primitive .NET type so downstream
        /// <see cref="PartitionKeyCoercer"/> calls behave consistently.
        /// Non-<see cref="JsonElement"/> inputs are returned unchanged.
        /// </summary>
        private static object NormaliseJsonValue(object raw)
        {
            if (raw is not JsonElement el) return raw;

            switch (el.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                case JsonValueKind.String:
                    return el.GetString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Number:
                    if (el.TryGetInt64(out var l)) return l;
                    if (el.TryGetDecimal(out var d)) return d;
                    return el.GetDouble();
                default:
                    return el.GetRawText();
            }
        }

        // ── Internal DTOs (System.Text.Json deserialisation targets) ──────

        private sealed class RangeBoundaryDto
        {
            public object Max     { get; set; }
            public string ShardId { get; set; }
        }

        private sealed class NestedFunctionDto
        {
            public string                     Kind       { get; set; }
            public List<string>               KeyColumns { get; set; }
            public Dictionary<string, string> Parameters { get; set; }
        }
    }
}
