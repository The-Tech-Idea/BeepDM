using System.Collections.Generic;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Catalog
{
    /// <summary>
    /// Thread-safe registry of <see cref="Shard"/> records. Implementations
    /// MUST allow concurrent <see cref="TryGet"/> / <see cref="Snapshot"/>
    /// reads with in-flight <see cref="Add"/> / <see cref="Remove"/> writes
    /// without throwing, and the snapshot returned MUST be a stable copy
    /// safe to enumerate after subsequent mutations.
    /// </summary>
    /// <remarks>
    /// The catalog is the single source of truth for "which shard IDs
    /// exist right now". The <see cref="DistributionPlan"/> references
    /// shards by ID; <see cref="DistributedDataSource.ApplyDistributionPlan"/>
    /// validates the plan against the live catalog and raises
    /// <see cref="Distributed.Events.PlacementViolationEventArgs"/> for
    /// any unknown shard id.
    /// </remarks>
    public interface IShardCatalog
    {
        /// <summary>Logical name persisted with each shard record (used as the partition key for ConfigEditor lookups).</summary>
        string DistributionName { get; }

        /// <summary>Number of shards currently registered.</summary>
        int Count { get; }

        /// <summary>Returns <c>true</c> when <paramref name="shardId"/> is registered.</summary>
        bool Contains(string shardId);

        /// <summary>Looks up a shard by id; returns <c>false</c> when missing.</summary>
        bool TryGet(string shardId, out Shard shard);

        /// <summary>Looks up a shard by id; throws when missing.</summary>
        Shard Get(string shardId);

        /// <summary>
        /// Adds or replaces a shard. Replaces atomically when an entry with
        /// the same id already exists. Returns <c>true</c> when a new entry
        /// was added, <c>false</c> when an existing entry was updated.
        /// </summary>
        bool Add(Shard shard);

        /// <summary>Removes the shard with the given id; returns <c>false</c> when missing.</summary>
        bool Remove(string shardId);

        /// <summary>Returns a snapshot of the catalog ordered by shard id.</summary>
        IReadOnlyList<Shard> Snapshot();

        /// <summary>
        /// Returns a shard-id → <see cref="IProxyCluster"/> projection
        /// suitable for the <see cref="DistributedDataSource"/>
        /// constructor. The projection is a snapshot and is not affected
        /// by subsequent catalog mutations.
        /// </summary>
        IReadOnlyDictionary<string, IProxyCluster> AsClusterMap();
    }
}
