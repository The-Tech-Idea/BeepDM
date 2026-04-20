using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Catalog
{
    /// <summary>
    /// Thread-safe in-memory implementation of <see cref="IShardCatalog"/>.
    /// Backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
    /// shard id (case-insensitive). Persistence is handled by the
    /// <c>ShardCatalog.Persistence.cs</c> partial.
    /// </summary>
    /// <remarks>
    /// The catalog does not own the <see cref="IProxyCluster"/> instances
    /// it stores — disposing the catalog does NOT dispose the clusters.
    /// Lifetime is the responsibility of the caller that built the
    /// clusters (typically the same code that constructs
    /// <see cref="DistributedDataSource"/>).
    /// </remarks>
    public partial class ShardCatalog : IShardCatalog
    {
        private readonly ConcurrentDictionary<string, Shard> _shards
            = new ConcurrentDictionary<string, Shard>(StringComparer.OrdinalIgnoreCase);

        private readonly IDMEEditor _dmeEditor; // optional; only required for persistence

        /// <summary>
        /// Creates an empty catalog with the given logical name.
        /// </summary>
        /// <param name="distributionName">Stable logical name persisted with every shard record.</param>
        /// <param name="dmeEditor">Optional editor used by the persistence partial; in-memory use can pass <c>null</c>.</param>
        public ShardCatalog(string distributionName, IDMEEditor dmeEditor = null)
        {
            if (string.IsNullOrWhiteSpace(distributionName))
                throw new ArgumentException("Distribution name cannot be null or whitespace.", nameof(distributionName));
            DistributionName = distributionName;
            _dmeEditor       = dmeEditor;
        }

        /// <inheritdoc/>
        public string DistributionName { get; }

        /// <inheritdoc/>
        public int Count => _shards.Count;

        /// <inheritdoc/>
        public bool Contains(string shardId)
            => !string.IsNullOrWhiteSpace(shardId) && _shards.ContainsKey(shardId);

        /// <inheritdoc/>
        public bool TryGet(string shardId, out Shard shard)
        {
            if (string.IsNullOrWhiteSpace(shardId))
            {
                shard = null;
                return false;
            }
            return _shards.TryGetValue(shardId, out shard);
        }

        /// <inheritdoc/>
        public Shard Get(string shardId)
        {
            if (!TryGet(shardId, out var shard))
                throw new KeyNotFoundException($"Shard '{shardId}' is not registered in catalog '{DistributionName}'.");
            return shard;
        }

        /// <inheritdoc/>
        public bool Add(Shard shard)
        {
            if (shard == null) throw new ArgumentNullException(nameof(shard));

            var added = true;
            _shards.AddOrUpdate(
                shard.ShardId,
                _ => shard,
                (_, existing) =>
                {
                    added = false;
                    return shard;
                });
            return added;
        }

        /// <inheritdoc/>
        public bool Remove(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return false;
            return _shards.TryRemove(shardId, out _);
        }

        /// <inheritdoc/>
        public IReadOnlyList<Shard> Snapshot()
            => _shards.Values
                      .OrderBy(s => s.ShardId, StringComparer.OrdinalIgnoreCase)
                      .ToList();

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, IProxyCluster> AsClusterMap()
        {
            var dict = new Dictionary<string, IProxyCluster>(_shards.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _shards)
                dict[kv.Key] = kv.Value.Cluster;
            return dict;
        }
    }
}
