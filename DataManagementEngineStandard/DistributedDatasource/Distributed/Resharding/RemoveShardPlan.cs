using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Drain plan supplied to
    /// <see cref="IReshardingService.RemoveShardAsync"/>. Specifies
    /// which shard is being removed and which shards inherit the
    /// entities it owned.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two modes of operation:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="ExplicitRedirects"/> populated — every entity
    ///       declared on <see cref="ShardId"/> must have an explicit
    ///       target shard mapping. Entities without a mapping cause
    ///       the remove to abort before any data is touched.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="ExplicitRedirects"/> empty and
    ///       <see cref="FallbackTargetShardIds"/> populated — each
    ///       entity is redistributed across the fallback shards (used
    ///       for hash-partitioned removals).
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public sealed class RemoveShardPlan
    {
        /// <summary>Initialises a new remove plan.</summary>
        /// <param name="shardId">Shard being drained. Required.</param>
        /// <param name="explicitRedirects">Per-entity redirect map; may be <c>null</c> when <paramref name="fallbackTargetShardIds"/> is used.</param>
        /// <param name="fallbackTargetShardIds">Fallback shard list when <paramref name="explicitRedirects"/> is <c>null</c>/empty; may be empty when every entity has an explicit redirect.</param>
        /// <param name="requireDataDrained">When <c>true</c>, the operation verifies the source shard is drained before the cutover; when <c>false</c>, cutover runs regardless.</param>
        public RemoveShardPlan(
            string                              shardId,
            IReadOnlyDictionary<string, string> explicitRedirects      = null,
            IReadOnlyList<string>               fallbackTargetShardIds = null,
            bool                                requireDataDrained     = true)
        {
            if (string.IsNullOrWhiteSpace(shardId))
                throw new ArgumentException("Shard id required.", nameof(shardId));

            ShardId                = shardId;
            ExplicitRedirects      = explicitRedirects ?? EmptyRedirects;
            FallbackTargetShardIds = fallbackTargetShardIds == null
                                        ? Array.Empty<string>()
                                        : fallbackTargetShardIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
            RequireDataDrained     = requireDataDrained;
        }

        /// <summary>Shard being drained.</summary>
        public string ShardId { get; }

        /// <summary>Per-entity redirect map (entity → target shard).</summary>
        public IReadOnlyDictionary<string, string> ExplicitRedirects { get; }

        /// <summary>Fallback shard list used when <see cref="ExplicitRedirects"/> is empty.</summary>
        public IReadOnlyList<string> FallbackTargetShardIds { get; }

        /// <summary>When <c>true</c>, the service verifies the source is drained before the cutover.</summary>
        public bool RequireDataDrained { get; }

        private static readonly IReadOnlyDictionary<string, string> EmptyRedirects
            = new Dictionary<string, string>(0, StringComparer.OrdinalIgnoreCase);
    }
}
