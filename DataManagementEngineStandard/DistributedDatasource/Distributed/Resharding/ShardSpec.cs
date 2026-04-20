using System;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Lightweight specification for a shard that is about to be
    /// registered with a <see cref="DistributedDataSource"/> via
    /// <see cref="IReshardingService.AddShardAsync"/>.
    /// </summary>
    public sealed class ShardSpec
    {
        /// <summary>Initialises a new spec.</summary>
        /// <param name="shardId">Stable shard id. Required; must not collide with an existing shard.</param>
        /// <param name="cluster">The proxy cluster that will host the shard. Required.</param>
        /// <param name="description">Free-form description surfaced in audit events.</param>
        public ShardSpec(string shardId, IProxyCluster cluster, string description = null)
        {
            if (string.IsNullOrWhiteSpace(shardId)) throw new ArgumentException("Shard id required.", nameof(shardId));
            ShardId     = shardId;
            Cluster     = cluster ?? throw new ArgumentNullException(nameof(cluster));
            Description = description ?? string.Empty;
        }

        /// <summary>Stable shard id. Must be unique in the catalog.</summary>
        public string ShardId { get; }

        /// <summary>Proxy cluster backing the shard.</summary>
        public IProxyCluster Cluster { get; }

        /// <summary>Free-form description surfaced in audit events.</summary>
        public string Description { get; }

        /// <inheritdoc/>
        public override string ToString() => $"ShardSpec({ShardId}, desc={Description})";
    }
}
