using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Catalog
{
    /// <summary>
    /// Immutable identity record for a single distributed shard. Each
    /// shard wraps an existing <see cref="IProxyCluster"/> (the HA pool
    /// that actually owns the rows) plus weight / tag metadata used by
    /// the router and rebalancer.
    /// </summary>
    /// <remarks>
    /// The cluster reference itself is not owned by the shard — the
    /// caller that constructed the cluster is responsible for its
    /// lifetime. <see cref="ShardCatalog"/> only stores references and
    /// never disposes them on its own.
    /// </remarks>
    public sealed class Shard : IEquatable<Shard>
    {
        /// <summary>Initialises a new shard descriptor.</summary>
        /// <param name="shardId">Stable identifier (case-insensitive). Must not be null/whitespace.</param>
        /// <param name="cluster">HA pool that owns this shard's data. Must not be <c>null</c>.</param>
        /// <param name="weight">Routing weight; higher values receive proportionally more traffic for sharded entities. Must be &gt;= 1.</param>
        /// <param name="tags">Optional free-form tags persisted with the shard (e.g. region, tier).</param>
        public Shard(
            string                              shardId,
            IProxyCluster                       cluster,
            int                                 weight = 1,
            IReadOnlyDictionary<string, string> tags   = null)
        {
            if (string.IsNullOrWhiteSpace(shardId))
                throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(shardId));
            if (cluster == null)
                throw new ArgumentNullException(nameof(cluster));
            if (weight < 1)
                throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be >= 1.");

            ShardId = shardId;
            Cluster = cluster;
            Weight  = weight;
            Tags    = tags ?? EmptyTags;
        }

        /// <summary>Stable identifier for this shard (case-insensitive in the catalog).</summary>
        public string ShardId { get; }

        /// <summary>HA pool that owns this shard's rows.</summary>
        public IProxyCluster Cluster { get; }

        /// <summary>Routing weight (>= 1). Defaults to 1.</summary>
        public int Weight { get; }

        /// <summary>Free-form tags persisted with the shard.</summary>
        public IReadOnlyDictionary<string, string> Tags { get; }

        /// <summary>
        /// Equality is intentionally identity-based on
        /// <see cref="ShardId"/>: two <see cref="Shard"/> instances with
        /// the same id but different clusters are still considered the
        /// same shard for catalog/diff purposes (Phase 11 reshard logic
        /// needs this to compare current vs. desired topologies).
        /// </summary>
        public bool Equals(Shard other)
            => other != null
               && string.Equals(ShardId, other.ShardId, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as Shard);

        /// <inheritdoc/>
        public override int GetHashCode()
            => StringComparer.OrdinalIgnoreCase.GetHashCode(ShardId);

        /// <inheritdoc/>
        public override string ToString()
            => $"Shard({ShardId}, weight={Weight}, tags={Tags.Count})";

        private static readonly IReadOnlyDictionary<string, string> EmptyTags
            = new Dictionary<string, string>(0, StringComparer.OrdinalIgnoreCase);
    }
}
