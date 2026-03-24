using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// Thread-safe map from entity (table) name → owning node ID.
    /// Supports exact-match and prefix-match lookups (longest prefix wins).
    /// Inspired by Vitess VSchema shard/tablet routing and Kafka partition leader election.
    /// </summary>
    public sealed class EntityAffinityMap
    {
        // Exact match: "Orders" → "node-1"
        private readonly ConcurrentDictionary<string, string> _exact
            = new(StringComparer.OrdinalIgnoreCase);

        // Prefix match: "Orders_" → "node-2"
        private readonly ConcurrentDictionary<string, string> _prefix
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// When no exact/prefix match exists, fall back to this node.
        /// Null means "no static fallback — use the cluster router".
        /// </summary>
        public string? FallbackNodeId { get; set; }

        // ── Mutation ──────────────────────────────────────────────────

        /// <summary>Map an exact entity name to a node.</summary>
        public void MapEntity(string entityName, string nodeId)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(entityName);
            ArgumentNullException.ThrowIfNullOrEmpty(nodeId);
            _exact[entityName] = nodeId;
        }

        /// <summary>Map all entity names starting with <paramref name="prefix"/> to a node.</summary>
        public void MapPrefix(string prefix, string nodeId)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(prefix);
            ArgumentNullException.ThrowIfNullOrEmpty(nodeId);
            _prefix[prefix] = nodeId;
        }

        /// <summary>Remove an exact entity mapping.</summary>
        public void UnmapEntity(string entityName) => _exact.TryRemove(entityName, out _);

        /// <summary>Remove a prefix mapping.</summary>
        public void UnmapPrefix(string prefix) => _prefix.TryRemove(prefix, out _);

        // ── Lookup ────────────────────────────────────────────────────

        /// <summary>
        /// Resolve the preferred nodeId for <paramref name="entityName"/>.
        /// Returns null when no mapping is found and <see cref="FallbackNodeId"/> is null.
        /// </summary>
        public string? Resolve(string entityName)
        {
            if (string.IsNullOrEmpty(entityName)) return FallbackNodeId;

            // 1. Exact match
            if (_exact.TryGetValue(entityName, out var nodeId))
                return nodeId;

            // 2. Longest matching prefix wins
            string? bestPrefix = null;
            string? bestNodeId = null;
            foreach (var kv in _prefix)
            {
                if (entityName.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (bestPrefix is null || kv.Key.Length > bestPrefix.Length)
                    {
                        bestPrefix = kv.Key;
                        bestNodeId = kv.Value;
                    }
                }
            }
            if (bestNodeId is not null) return bestNodeId;

            // 3. FallbackNodeId (may be null)
            return FallbackNodeId;
        }

        // ── Rebalancing ───────────────────────────────────────────────

        /// <summary>
        /// Reassign all entity/prefix mappings that point to <paramref name="failedNodeId"/>
        /// to a new node chosen round-robin from <paramref name="liveNodeIds"/>.
        /// Returns the reassignment map (keys are "entity:&lt;name&gt;" or "prefix:&lt;name&gt;").
        /// </summary>
        public IReadOnlyDictionary<string, string> ReassignAfterFailure(
            string failedNodeId,
            IReadOnlyList<string> liveNodeIds)
        {
            if (liveNodeIds.Count == 0)
                return new Dictionary<string, string>();

            var reassignments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int idx = 0;
            string NextLive() => liveNodeIds[idx++ % liveNodeIds.Count];

            foreach (var key in _exact.Keys.ToList())
            {
                if (_exact.TryGetValue(key, out var nid) && nid == failedNodeId)
                {
                    var newNode = NextLive();
                    _exact[key] = newNode;
                    reassignments[$"entity:{key}"] = newNode;
                }
            }

            foreach (var key in _prefix.Keys.ToList())
            {
                if (_prefix.TryGetValue(key, out var nid) && nid == failedNodeId)
                {
                    var newNode = NextLive();
                    _prefix[key] = newNode;
                    reassignments[$"prefix:{key}"] = newNode;
                }
            }

            return reassignments;
        }

        /// <summary>
        /// Returns all current exact and prefix mappings for observability.
        /// </summary>
        public IReadOnlyDictionary<string, string> GetAllMappings()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _exact)   result[$"entity:{kv.Key}"] = kv.Value;
            foreach (var kv in _prefix)  result[$"prefix:{kv.Key}"] = kv.Value;
            return result;
        }
    }
}
