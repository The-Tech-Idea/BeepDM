using System;
using System.Collections.Generic;
using System.Diagnostics;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Distributed.Routing;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Phase 07 implementation of
    /// <see cref="IDistributedWriteExecutor"/>. Partial-class root
    /// holds the ctor, shared fields, quorum derivation, and helpers
    /// shared by the behaviour partials.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Files in this partial family:
    /// <list type="bullet">
    ///   <item><c>DistributedWriteExecutor.cs</c> — ctor, quorum resolution, helpers.</item>
    ///   <item><c>DistributedWriteExecutor.SingleShard.cs</c> — single-shard path.</item>
    ///   <item><c>DistributedWriteExecutor.FanOut.cs</c> — replicated / broadcast fan-out.</item>
    ///   <item><c>DistributedWriteExecutor.ScatterDelete.cs</c> — opt-in scatter writes.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Stateless once constructed; the parent
    /// <see cref="DistributedDataSource"/> rebuilds the executor only
    /// when the options snapshot changes.
    /// </para>
    /// </remarks>
    public sealed partial class DistributedWriteExecutor : IDistributedWriteExecutor
    {
        private readonly IShardInvoker                _shards;
        private readonly DistributedDataSourceOptions _options;

        /// <summary>Creates a new write executor.</summary>
        /// <param name="shards">Shard-id -> cluster lookup.</param>
        /// <param name="options">Distributed options; defaults used when omitted.</param>
        public DistributedWriteExecutor(
            IShardInvoker                shards,
            DistributedDataSourceOptions options = null)
        {
            _shards  = shards ?? throw new ArgumentNullException(nameof(shards));
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
                    $"Single-shard write executor called with {decision.ShardIds.Count} target shards for '{decision.EntityName}'.");
        }

        private static void RequireNonEmpty(RoutingDecision decision)
        {
            RequireDecision(decision);
            if (decision.ShardIds.Count == 0)
                throw new InvalidOperationException(
                    $"No live shards available for write on '{decision.EntityName}'. " +
                    $"MatchKind={decision.MatchKind}, Mode={decision.Mode}.");
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

        // ── Quorum resolution ─────────────────────────────────────────────

        /// <summary>
        /// Derives the effective quorum policy + required ack count
        /// from <paramref name="decision"/> and
        /// <paramref name="options"/>. Follows these rules:
        /// <list type="number">
        ///   <item>Caller override on <paramref name="options"/> wins.</item>
        ///   <item>Otherwise use <see cref="RoutingDecision.WriteQuorum"/>
        ///   from the placement: <c>0</c> means "all", anything else
        ///   implies <see cref="QuorumPolicy.AtLeastN"/>.</item>
        ///   <item>Default fallback is <see cref="QuorumPolicy.All"/>.</item>
        /// </list>
        /// </summary>
        internal static (QuorumPolicy policy, int requiredAck) ResolveQuorum(
            RoutingDecision         decision,
            DistributedWriteOptions options)
        {
            int shardCount = decision.ShardIds.Count;
            if (shardCount == 0) return (QuorumPolicy.All, 0);

            if (options?.QuorumOverride != null)
            {
                var policy = options.QuorumOverride.Value;
                int required = policy switch
                {
                    QuorumPolicy.All      => shardCount,
                    QuorumPolicy.Majority => (shardCount / 2) + 1,
                    QuorumPolicy.AtLeastN => Clamp(
                                                 options.AtLeastN ?? decision.WriteQuorum,
                                                 min: 1,
                                                 max: shardCount),
                    _                     => shardCount,
                };
                return (policy, required);
            }

            // Plan-derived: WriteQuorum == 0 means "all shards".
            if (decision.WriteQuorum <= 0 || decision.WriteQuorum >= shardCount)
            {
                return (QuorumPolicy.All, shardCount);
            }

            return (QuorumPolicy.AtLeastN,
                    Clamp(decision.WriteQuorum, min: 1, max: shardCount));
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // ── Leg execution ─────────────────────────────────────────────────

        /// <summary>
        /// Executes one per-shard write leg, capturing timing and any
        /// exception into a <see cref="WriteFanOutResult"/>. Per-shard
        /// HA / retry still flows through the cluster implementation.
        /// Phase 10 additionally reports success through
        /// <see cref="IShardInvoker.NotifyShardSuccess"/> so the
        /// distribution-tier circuit breaker can reset on recovery.
        /// </summary>
        private WriteFanOutResult ExecuteLeg(
            string                                 shardId,
            string                                 entityName,
            string                                 operation,
            Func<Proxy.IProxyCluster, IErrorsInfo> writeOperation)
        {
            var sw = Stopwatch.StartNew();
            IDisposable shardPermit = null;
            try
            {
                var cluster = _shards.GetShard(shardId);
                if (cluster == null)
                {
                    throw new InvalidOperationException(
                        $"Shard '{shardId}' is not registered.");
                }

                // Phase 14 — per-shard capacity gate + rate limit.
                shardPermit = _shards.AcquireShardCallPermit(shardId, System.Threading.CancellationToken.None);

                var result = writeOperation(cluster);
                sw.Stop();

                if (result == null || result.Flag == Errors.Ok)
                {
                    _shards.NotifyShardSuccess(shardId, sw.Elapsed.TotalMilliseconds);
                    return WriteFanOutResult.Success(shardId, sw.Elapsed);
                }

                var ex = result.Ex ??
                         new InvalidOperationException(
                             $"Shard '{shardId}' reported failure: " +
                             (result.Message ?? "(no message)"));
                _shards.NotifyShardFailure(entityName, shardId, operation, ex);
                return WriteFanOutResult.Failure(shardId, ex, sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _shards.NotifyShardFailure(entityName, shardId, operation, ex);
                return WriteFanOutResult.Failure(shardId, ex, sw.Elapsed);
            }
            finally
            {
                try { shardPermit?.Dispose(); } catch { /* best-effort */ }
            }
        }

        private void NotifyFanOut(
            RoutingDecision              decision,
            IReadOnlyList<string>        targetShardIds,
            DistributedExecutionContext  ctx,
            string                       reason)
        {
            var op  = ctx?.OperationName ?? string.Empty;
            var key = FormatPartitionKey(decision);
            for (int i = 0; i < targetShardIds.Count; i++)
            {
                _shards.NotifyShardSelected(
                    entityName:   decision.EntityName,
                    shardId:      targetShardIds[i],
                    operation:    op,
                    partitionKey: key,
                    reason:       reason);
            }
        }

        /// <summary>
        /// Overload used by broadcast / replicated fan-out paths that
        /// already resolved the healthy subset; preserved for the
        /// ScatterDelete / Phase 07 call sites that pass the full
        /// <see cref="RoutingDecision.ShardIds"/> list.
        /// </summary>
        private void NotifyFanOut(
            RoutingDecision              decision,
            DistributedExecutionContext  ctx,
            string                       reason)
            => NotifyFanOut(decision, decision.ShardIds, ctx, reason);

        private static string FormatPartitionKey(RoutingDecision decision)
        {
            if (decision.KeyValues.Count == 0) return null;
            var parts = new List<string>(decision.KeyValues.Count);
            foreach (var kv in decision.KeyValues)
            {
                parts.Add(kv.Key + "=" + kv.Value);
            }
            return string.Join("|", parts);
        }
    }
}
