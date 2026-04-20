using System;
using TheTechIdea.Beep.Distributed.Routing;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Phase 06 implementation of <see cref="IDistributedReadExecutor"/>.
    /// Partial-class root holds the ctor, shared fields, and helpers
    /// that would otherwise be duplicated across every behaviour
    /// partial.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Files in this partial family:
    /// <list type="bullet">
    ///   <item><c>DistributedReadExecutor.cs</c> — ctor, dispatch helpers, guards.</item>
    ///   <item><c>DistributedReadExecutor.SingleShard.cs</c> — single-shard path.</item>
    ///   <item><c>DistributedReadExecutor.Scatter.cs</c> — scatter fan-out + merger.</item>
    ///   <item><c>DistributedReadExecutor.Replicated.cs</c> — replicated read + failover.</item>
    /// </list>
    /// </para>
    /// <para>
    /// The executor is stateless once constructed (the options
    /// snapshot is captured at build time). The parent
    /// <see cref="DistributedDataSource"/> rebuilds the executor
    /// only when options themselves change, not on every plan swap.
    /// </para>
    /// </remarks>
    public sealed partial class DistributedReadExecutor : IDistributedReadExecutor
    {
        private readonly IShardInvoker                _shards;
        private readonly IResultMerger                _merger;
        private readonly DistributedDataSourceOptions _options;

        /// <summary>Creates a new executor bound to the supplied shard map and merger.</summary>
        /// <param name="shards">Shard-id -> <see cref="Proxy.IProxyCluster"/> lookup.</param>
        /// <param name="merger">Result merger used by scatter paths; defaults to <see cref="BasicResultMerger.Instance"/>.</param>
        /// <param name="options">Distributed options; defaults are used when omitted.</param>
        /// <exception cref="ArgumentNullException"><paramref name="shards"/> is <c>null</c>.</exception>
        public DistributedReadExecutor(
            IShardInvoker                shards,
            IResultMerger                merger  = null,
            DistributedDataSourceOptions options = null)
        {
            _shards  = shards ?? throw new ArgumentNullException(nameof(shards));
            _merger  = merger ?? BasicResultMerger.Instance;
            _options = options ?? new DistributedDataSourceOptions();
        }

        // ── Guards ────────────────────────────────────────────────────────

        private static void RequireDecision(RoutingDecision decision)
        {
            if (decision == null)
                throw new ArgumentNullException(nameof(decision));
        }

        private static void RequireSingleShard(RoutingDecision decision)
        {
            RequireDecision(decision);
            if (decision.ShardIds.Count != 1)
                throw new InvalidOperationException(
                    $"Single-shard executor called with {decision.ShardIds.Count} target shards for '{decision.EntityName}'.");
        }

        private static void RequireNonEmpty(RoutingDecision decision)
        {
            RequireDecision(decision);
            if (decision.ShardIds.Count == 0)
                throw new InvalidOperationException(
                    $"No live shards available for '{decision.EntityName}'. MatchKind={decision.MatchKind}, Mode={decision.Mode}.");
        }

        private Proxy.IProxyCluster ResolveShardOrThrow(string shardId, string operation, string entityName)
        {
            var cluster = _shards.GetShard(shardId);
            if (cluster == null)
            {
                throw new InvalidOperationException(
                    $"Shard '{shardId}' is not registered. operation='{operation}', entity='{entityName}'.");
            }
            return cluster;
        }

        private static string FormatPartitionKey(RoutingDecision decision)
        {
            if (decision.KeyValues.Count == 0) return null;
            var parts = new string[decision.KeyValues.Count];
            int i = 0;
            foreach (var kv in decision.KeyValues)
            {
                parts[i++] = kv.Key + "=" + kv.Value;
            }
            return string.Join("|", parts);
        }
    }
}
