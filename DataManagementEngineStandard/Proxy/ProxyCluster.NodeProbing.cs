using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// ProxyCluster — node probing partition.
    /// Runs a lightweight background timer that probes every node with
    /// <see cref="IDataSource.Openconnection()"/> and tracks consecutive
    /// failures / recoveries to implement IProxyDataSource watchdog semantics.
    /// Also owns the affinity TTL sweep timer.
    /// </summary>
    public partial class ProxyCluster
    {
        // ── Probe timer ───────────────────────────────────────────────────
        private CancellationTokenSource _probeCts;
        private Task                    _probeTask;

        // ── Watchdog compatibility (IProxyDataSource) ─────────────────────
        public int WatchdogIntervalMs       { get; set; } = 5_000;
        public int WatchdogProbeTimeoutMs   { get; set; } = 2_000;
        public int WatchdogFailureThreshold { get; set; } = 2;
        public int WatchdogRecoveryThreshold{ get; set; } = 1;

        // ── Probe timer lifecycle ─────────────────────────────────────────

        private void StartNodeProbeTimer()
        {
            if (_probeTask != null && !_probeTask.IsCompleted) return;
            _probeCts  = new CancellationTokenSource();
            _probeTask = Task.Run(() => ProbeLoop(_probeCts.Token));
        }

        private void StopNodeProbeTimer()
        {
            _probeCts?.Cancel();
            try { _probeTask?.Wait(TimeSpan.FromSeconds(5)); } catch { /* best-effort */ }
            _probeTask = null;
            _probeCts?.Dispose();
            _probeCts  = null;
        }

        // ── Main probe loop ───────────────────────────────────────────────

        private async Task ProbeLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_clusterPolicy.NodeProbeIntervalMs, ct)
                              .ConfigureAwait(false);

                    foreach (var node in _nodes.Values.ToList())
                    {
                        if (ct.IsCancellationRequested) break;
                        await ProbeNodeAsync(node, ct).ConfigureAwait(false);
                    }

                    // 11.8: evaluate outlier thresholds after every probe round
                    RunOutlierDetection();
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"[ProxyCluster] ProbeLoop error: {ex.Message}");
                }
            }
        }

        private async Task ProbeNodeAsync(ProxyNode node, CancellationToken ct)
        {
            bool success;
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(_clusterPolicy.NodeProbeTimeoutMs);

                // A successful Openconnection() is sufficient as a liveness probe
                var state = await Task.Run(
                    () => node.Proxy.Openconnection(), timeout.Token)
                    .ConfigureAwait(false);

                success = state == ConnectionState.Open
                       || state == ConnectionState.Connecting;
            }
            catch
            {
                success = false;
            }

            node.LastProbeUtc = DateTime.UtcNow;

            if (success)
            {
                node.ConsecutiveFailures = 0;
                node.ConsecutiveSuccesses++;

                // 11.9: measure replica lag while the node is live
                await ProbeReplicaLagAsync(node, ct).ConfigureAwait(false);

                if (!node.IsAlive
                    && node.ConsecutiveSuccesses >= _clusterPolicy.NodeHealthyThreshold)
                {
                    MarkNodeAlive(node);
                }
            }
            else
            {
                node.ConsecutiveSuccesses = 0;
                node.ConsecutiveFailures++;

                if (node.IsAlive
                    && node.ConsecutiveFailures >= _clusterPolicy.NodeUnhealthyThreshold)
                {
                    MarkNodeDown(node, $"Probe failed {node.ConsecutiveFailures}× in a row");
                }
            }
        }

        // ── State transitions ─────────────────────────────────────────────

        private void MarkNodeDown(ProxyNode node, string reason)
        {
            node.IsAlive            = false;
            node.ConsecutiveSuccesses = 0;

            Logger?.WriteLog($"[ProxyCluster] Node DOWN: {node.NodeId}. Reason: {reason}");

            OnNodeDown?.Invoke(this, new NodeStatusEventArgs(node.NodeId, false, reason));

            // If this was a Primary, try to promote a Replica
            if (node.NodeRole == ProxyDataSourceRole.Primary)
                TryPromoteReplica(node);
        }

        private void MarkNodeAlive(ProxyNode node)
        {
            node.IsAlive           = true;
            node.ConsecutiveFailures = 0;

            Logger?.WriteLog($"[ProxyCluster] Node RESTORED: {node.NodeId}");

            OnNodeRestored?.Invoke(this, new NodeStatusEventArgs(node.NodeId, true));

            // If a replica was temporarily promoted while this primary was down, demote it
            TryDemoteProvisionalPrimary(node);
        }

        // ── Simple primary promotion / demotion ───────────────────────────

        private void TryPromoteReplica(ProxyNode failedPrimary)
        {
            var replica = _nodes.Values
                .FirstOrDefault(n => n.IsAlive
                    && !n.IsDraining
                    && n.NodeRole == ProxyDataSourceRole.Replica);

            if (replica == null) return;

            var oldRole  = replica.NodeRole;
            replica.NodeRole = ProxyDataSourceRole.Primary;

            Logger?.WriteLog(
                $"[ProxyCluster] Promoted '{replica.NodeId}' from Replica→Primary ('{failedPrimary.NodeId}' is down).");

            OnNodePromoted?.Invoke(this, new RoleChangeEventArgs
            {
                DataSourceName = replica.NodeId,
                OldRole        = oldRole,
                NewRole        = ProxyDataSourceRole.Primary,
                Reason         = $"Primary '{failedPrimary.NodeId}' failed"
            });
        }

        private void TryDemoteProvisionalPrimary(ProxyNode recoveredPrimary)
        {
            // Demote any replica that was promoted while recoveredPrimary was down
            var provisionals = _nodes.Values
                .Where(n => n.NodeId != recoveredPrimary.NodeId
                         && n.NodeRole == ProxyDataSourceRole.Primary)
                .ToList();

            foreach (var p in provisionals)
            {
                p.NodeRole = ProxyDataSourceRole.Replica;
                Logger?.WriteLog(
                    $"[ProxyCluster] Demoted '{p.NodeId}' Primary→Replica ('{recoveredPrimary.NodeId}' recovered).");

                OnNodeDemoted?.Invoke(this, new RoleChangeEventArgs
                {
                    DataSourceName = p.NodeId,
                    OldRole        = ProxyDataSourceRole.Primary,
                    NewRole        = ProxyDataSourceRole.Replica,
                    Reason         = $"Primary '{recoveredPrimary.NodeId}' recovered"
                });
            }
        }

        // ── Affinity TTL sweep timer ──────────────────────────────────────

        private CancellationTokenSource _affinitySweepCts;
        private Task                    _affinitySweepTask;

        private void StartAffinityTtlSweep()
        {
            if (!_clusterPolicy.EnableNodeAffinity) return;

            _affinitySweepCts  = new CancellationTokenSource();
            _affinitySweepTask = Task.Run(() => AffinityTtlSweepLoop(_affinitySweepCts.Token));
        }

        private void StopAffinityTtlSweep()
        {
            _affinitySweepCts?.Cancel();
            try { _affinitySweepTask?.Wait(TimeSpan.FromSeconds(3)); } catch { /* best-effort */ }
            _affinitySweepTask = null;
            _affinitySweepCts?.Dispose();
            _affinitySweepCts  = null;
        }

        private async Task AffinityTtlSweepLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Sweep every half-TTL
                    int sweepInterval = Math.Max(1_000,
                        (_clusterPolicy.NodeAffinityTtlSeconds * 1000) / 2);

                    await Task.Delay(sweepInterval, ct).ConfigureAwait(false);

                    var ttl     = TimeSpan.FromSeconds(_clusterPolicy.NodeAffinityTtlSeconds);
                    var cutoff  = DateTime.UtcNow - ttl;
                    var expired = _affinityMap
                        .Where(kv => kv.Value.LastUsed < cutoff)
                        .Select(kv => kv.Key)
                        .ToList();

                    foreach (var key in expired)
                        _affinityMap.TryRemove(key, out _);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"[ProxyCluster] AffinityTtlSweep error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Called by <see cref="RemoveNode"/> to reassign any affinity entries
        /// that were pointing at the removed/dead node.
        /// </summary>
        private void ReassignAffinityOnNodeRemoved(string removedNodeId)
        {
            if (_affinityMap.IsEmpty) return;

            var reassignments = new Dictionary<string, string>();
            var keysToUpdate  = _affinityMap
                .Where(kv => kv.Value.NodeId == removedNodeId)
                .Select(kv => kv.Key)
                .ToList();

            var fallback = _nodes.Values
                .FirstOrDefault(n => n.IsAlive && !n.IsDraining);

            foreach (var key in keysToUpdate)
            {
                if (fallback != null)
                {
                    _affinityMap[key] = (fallback.NodeId, DateTime.UtcNow);
                    reassignments[key] = fallback.NodeId;
                }
                else
                {
                    _affinityMap.TryRemove(key, out _);
                }
            }

            if (reassignments.Count > 0)
            {
                OnAffinityRebalanced?.Invoke(this,
                    new AffinityRebalancedEventArgs(reassignments));
            }
        }

        // ── IProxyDataSource watchdog interface (compatibility shims) ─────

        public void StartWatchdog() => StartNodeProbeTimer();
        public void StopWatchdog()  => StopNodeProbeTimer();

        public IReadOnlyList<WatchdogNodeStatus> GetWatchdogStatus() =>
            _nodes.Values
                .Select(n => new WatchdogNodeStatus
                {
                    DataSourceName    = n.NodeId,
                    Role              = n.NodeRole,
                    IsHealthy         = n.IsAlive,
                    IsCircuitOpen     = false, // circuit state lives inside node.Proxy
                    WatchdogFailures  = n.ConsecutiveFailures,
                    WatchdogSuccesses = n.ConsecutiveSuccesses
                })
                .ToList();

        // ── IProxyDataSource event forwarding ─────────────────────────────
        // OnRolePromoted / OnRoleDemoted come from IProxyDataSource and must be
        // declared on the main class (ProxyCluster.cs); they are fired here.
    }
}
