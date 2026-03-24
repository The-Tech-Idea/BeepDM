using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// ProxyCluster — scatter-gather reads + replica lag awareness (Phase 11.9).
    /// </summary>
    public partial class ProxyCluster
    {
        /// <summary>
        /// Optional delegate for measuring replication lag on a node.
        /// Receives the node and returns lag in milliseconds (-1 if unknown).
        /// Set this on the cluster instance to enable active lag probing.
        /// </summary>
        public Func<IProxyNode, Task<long>>? ReplicaLagProbe { get; set; }

        // ─────────────────────────────────────────────────────────────────
        //  Lag-aware read-candidate selection
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns live nodes suitable for read operations respecting the cluster's
        /// <see cref="ProxyPolicy.MaxReplicaLagMs"/> guard.
        /// Falls back to all live nodes when the lag guard excludes all replicas.
        /// </summary>
        private List<ProxyNode> GetReadCandidates()
        {
            var live = GetLiveNodes(writesOnly: false);
            long maxLag = _clusterPolicy.MaxReplicaLagMs;

            if (maxLag <= 0) return live;   // lag guard disabled

            var acceptable = live
                .Where(n =>
                    // Primary nodes always pass (they have no lag)
                    n.NodeRole != ProxyDataSourceRole.Replica
                    // Replicas with unknown lag (-1) pass the filter (benefit of doubt)
                    || n.ReplicaLagMs < 0
                    // Replicas within the lag threshold pass
                    || n.ReplicaLagMs <= maxLag)
                .ToList();

            // If all replicas were excluded, fall back to any live node
            return acceptable.Count > 0 ? acceptable : live;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Scatter-gather fan-out
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Executes <paramref name="operation"/> on every node in <paramref name="candidates"/>
        /// in parallel and merges the successful results via <paramref name="merge"/>.
        /// Throws <see cref="AggregateException"/> when all nodes fail.
        /// </summary>
        internal async Task<T> ExecuteScatterGatherAsync<T>(
            IReadOnlyList<ProxyNode>          candidates,
            Func<ProxyNode, Task<T>>          operation,
            Func<IReadOnlyList<T>, T>         merge,
            CancellationToken                 ct = default)
        {
            if (candidates.Count == 0)
                throw new InvalidOperationException(
                    $"ProxyCluster '{DatasourceName}': no live nodes for scatter-gather.");

            var tasks = candidates
                .Select(node => ExecuteSingleScatterAsync(node, operation, ct))
                .ToList();

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            var successful = results
                .Where(r => r.Success)
                .Select(r => r.Value!)
                .ToList();

            if (successful.Count == 0)
            {
                var firstFail = results.First(r => !r.Success);
                throw new AggregateException(
                    "All scatter-gather nodes failed.",
                    firstFail.Exception ?? new Exception("Unknown scatter-gather failure."));
            }

            return merge(successful);
        }

        private sealed record ScatterResult<T>(bool Success, T? Value, Exception? Exception);

        private async Task<ScatterResult<T>> ExecuteSingleScatterAsync<T>(
            ProxyNode              node,
            Func<ProxyNode, Task<T>> operation,
            CancellationToken      ct)
        {
            node.IncrementInFlight();
            try
            {
                var result = await operation(node).ConfigureAwait(false);
                RecordNodeOutcome(node, true);
                return new ScatterResult<T>(true, result, null);
            }
            catch (Exception ex)
            {
                RecordNodeOutcome(node, false);
                return new ScatterResult<T>(false, default, ex);
            }
            finally
            {
                node.DecrementInFlight();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Lag probe — called from probe loop (NodeProbing.cs)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// If a <see cref="ReplicaLagProbe"/> is registered and the node is a Replica,
        /// probes the node for current replication lag and stores the result on the node.
        /// </summary>
        private async Task ProbeReplicaLagAsync(ProxyNode node, CancellationToken ct)
        {
            if (ReplicaLagProbe is null) return;
            if (node.NodeRole != ProxyDataSourceRole.Replica) return;

            try
            {
                long lag = await ReplicaLagProbe(node).ConfigureAwait(false);
                node.ReplicaLagMs = lag;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog(
                    $"[ProxyCluster] Lag probe failed for node '{node.NodeId}': {ex.Message}");
            }
        }
    }
}
