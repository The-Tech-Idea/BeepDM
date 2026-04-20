using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Stateless helper that maps <c>(enlisted shards, per-shard
    /// capabilities, policy)</c> → <see cref="TransactionStrategy"/>.
    /// The Phase 09 coordinator delegates every strategy decision to
    /// this resolver so the selection rules stay in one place and are
    /// unit-testable without touching the coordinator.
    /// </summary>
    /// <remarks>
    /// Selection table:
    /// <list type="bullet">
    ///   <item>Zero shards → throws (nothing to enlist).</item>
    ///   <item>One shard → <see cref="TransactionStrategy.SingleShardFastPath"/>.</item>
    ///   <item>
    ///     Multiple shards, every involved cluster reports
    ///     <see cref="IProxyCluster.SupportsTwoPhaseCommit"/> = <c>true</c>,
    ///     and <paramref name="preferSagaOverTwoPhaseCommit"/> is
    ///     <c>false</c> → <see cref="TransactionStrategy.TwoPhaseCommit"/>.
    ///   </item>
    ///   <item>
    ///     Multiple shards, any involved cluster is saga-only OR the
    ///     caller prefers saga → <see cref="TransactionStrategy.Saga"/>.
    ///   </item>
    /// </list>
    /// <para>
    /// The resolver never reaches into the cluster pool for I/O; it
    /// only reads the opt-in capability flag. If a caller's shard id
    /// is missing from the supplied shard map the resolver falls back
    /// to saga to preserve correctness.
    /// </para>
    /// </remarks>
    public static class TransactionDecisionResolver
    {
        /// <summary>
        /// Resolves the strategy for the given enlisted shards.
        /// </summary>
        /// <param name="shardIds">Unique shard ids enlisted in the scope.</param>
        /// <param name="shards">Live shard map used to read capabilities.</param>
        /// <param name="preferSagaOverTwoPhaseCommit">
        /// When <c>true</c>, always prefer saga for multi-shard work
        /// even when every shard supports 2PC. Useful when the caller
        /// knows the workload is tolerant of eventual consistency and
        /// wants to avoid the prepare round's blocking window.
        /// </param>
        public static TransactionStrategy Resolve(
            IReadOnlyList<string>                       shardIds,
            IReadOnlyDictionary<string, IProxyCluster>  shards,
            bool                                        preferSagaOverTwoPhaseCommit)
        {
            if (shardIds == null) throw new ArgumentNullException(nameof(shardIds));
            if (shards   == null) throw new ArgumentNullException(nameof(shards));
            if (shardIds.Count == 0)
                throw new ArgumentException(
                    "A distributed transaction must enlist at least one shard.",
                    nameof(shardIds));

            var distinct = shardIds.Distinct(StringComparer.Ordinal).ToArray();

            if (distinct.Length == 1) return TransactionStrategy.SingleShardFastPath;
            if (preferSagaOverTwoPhaseCommit) return TransactionStrategy.Saga;

            return Every2PcCapable(distinct, shards)
                ? TransactionStrategy.TwoPhaseCommit
                : TransactionStrategy.Saga;
        }

        /// <summary>
        /// Returns <c>true</c> when every enlisted shard is present
        /// in <paramref name="shards"/> and has
        /// <see cref="IProxyCluster.SupportsTwoPhaseCommit"/> = <c>true</c>.
        /// Missing shards are treated as not-capable so the caller
        /// cannot silently end up in 2PC mode against a cluster the
        /// coordinator can't see.
        /// </summary>
        private static bool Every2PcCapable(
            IReadOnlyList<string>                       shardIds,
            IReadOnlyDictionary<string, IProxyCluster>  shards)
        {
            for (int i = 0; i < shardIds.Count; i++)
            {
                if (!shards.TryGetValue(shardIds[i], out var cluster)) return false;
                if (cluster == null || !cluster.SupportsTwoPhaseCommit) return false;
            }
            return true;
        }
    }
}
