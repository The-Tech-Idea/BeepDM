using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// ProxyCluster — node management partition.
    /// Implements: AddNode, RemoveNode, GetNodes, DrainNode/DrainNodeAsync,
    /// IProxyDataSource datasource-membership shims.
    /// </summary>
    public partial class ProxyCluster
    {
        // ─────────────────────────────────────────────────────────────────────
        //  IProxyCluster — node management
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a node to the cluster routing pool.
        /// Injects shared <see cref="ICircuitStateStore"/> and <see cref="IProxyAuditSink"/>,
        /// fans out the current cluster policy, and rebuilds any consistent-hash ring.
        /// </summary>
        public void AddNode(IProxyNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (!(node is ProxyNode concrete))
                throw new ArgumentException("Node must be a ProxyNode instance.", nameof(node));

            if (_nodes.ContainsKey(node.NodeId))
                throw new InvalidOperationException(
                    $"ProxyCluster: node '{node.NodeId}' already exists.");

            // Fan out shared state and policy to the new node's underlying proxy
            concrete.Proxy.AuditSink = _auditSink;
            concrete.Proxy.ApplyPolicy(_clusterPolicy);

            _nodes[node.NodeId] = concrete;

            // Rebuild consistent-hash ring if that strategy is active
            if (_nodeRouter is ConsistentHashRouter chr)
                chr.Rebuild(_nodes.Values);

            // Persist the node config if this cluster has a DMEEditor
            if (DMEEditor?.ConfigEditor != null)
            {
                var cfg = concrete.NodeConfig ?? concrete.ToConnectionProperties(_clusterName);
                cfg.ParameterList["ClusterName"] = _clusterName ?? string.Empty;
                PersistNodeConfig(cfg);
            }

            Logger?.WriteLog($"[ProxyCluster] Node added: {node.NodeId} (role={node.NodeRole}, weight={node.Weight})");
        }

        /// <summary>
        /// Removes a node from the cluster.
        /// If the node was handling in-flight requests the caller should
        /// call <see cref="DrainNode"/> first.
        /// </summary>
        public void RemoveNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) throw new ArgumentNullException(nameof(nodeId));

            if (!_nodes.TryRemove(nodeId, out var removed))
                return; // idempotent

            // Rebuild consistent-hash ring
            if (_nodeRouter is ConsistentHashRouter chr)
                chr.Rebuild(_nodes.Values);

            // Clean up affinity entries pointing to the removed node
            ReassignAffinityOnNodeRemoved(nodeId);

            // Remove from persisted config if available
            if (DMEEditor?.ConfigEditor != null)
            {
                var existing = DMEEditor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName == nodeId
                                      && c.DriverName == "BeepProxyNode");
                if (existing != null)
                {
                    DMEEditor.ConfigEditor.RemoveDataConnection(nodeId);
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                }
            }

            Logger?.WriteLog($"[ProxyCluster] Node removed: {nodeId}");
        }

        /// <summary>Returns a point-in-time snapshot of all cluster nodes.</summary>
        public IReadOnlyList<IProxyNode> GetNodes() =>
            _nodes.Values.Cast<IProxyNode>().ToList();

        // ─────────────────────────────────────────────────────────────────────
        //  Drain  — zero-downtime rolling replacement
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Marks the node as draining (new routing skips it) then blocks until
        /// <see cref="ProxyNode.InFlightCount"/> reaches zero or <paramref name="timeoutMs"/> elapses.
        /// </summary>
        public void DrainNode(string nodeId, int timeoutMs = 30_000)
        {
            DrainNodeAsync(nodeId, timeoutMs).GetAwaiter().GetResult();
        }

        /// <inheritdoc cref="DrainNode"/>
        public async Task DrainNodeAsync(string nodeId, int timeoutMs = 30_000,
            CancellationToken cancellationToken = default)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
                throw new KeyNotFoundException($"ProxyCluster: node '{nodeId}' not found.");

            node.IsDraining = true;
            Logger?.WriteLog($"[ProxyCluster] Draining node '{nodeId}' (timeout={timeoutMs}ms)...");

            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (node.InFlightCount > 0 && DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }

            if (node.InFlightCount > 0)
                Logger?.WriteLog($"[ProxyCluster] Drain timeout for '{nodeId}': {node.InFlightCount} in-flight remaining.");
            else
                Logger?.WriteLog($"[ProxyCluster] Node '{nodeId}' drained successfully.");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  IProxyDataSource — datasource membership shims
        //  (ProxyCluster does not use ds-name-based membership internally,
        //   but these satisfy the interface for compatibility.)
        // ─────────────────────────────────────────────────────────────────────

        public void AddDataSource(string dsName, int weight = 1)
        {
            // No-op at cluster level — callers should use AddNode instead.
            // Provided for IProxyDataSource interface compatibility.
            Logger?.WriteLog($"[ProxyCluster] AddDataSource('{dsName}') ignored — use AddNode.");
        }

        public void RemoveDataSource(string dsName)
        {
            // Try to match by nodeId
            if (_nodes.ContainsKey(dsName))
                RemoveNode(dsName);
        }

        public void SetRole(string dsName, ProxyDataSourceRole role)
        {
            if (_nodes.TryGetValue(dsName, out var node))
                node.NodeRole = role;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  IProxyDataSource — connection pool stubs (cluster uses node proxies)
        // ─────────────────────────────────────────────────────────────────────

        public IDataSource GetPooledConnection(string dsName) =>
            _nodes.TryGetValue(dsName, out var n) ? n.Proxy.GetPooledConnection(dsName) : null;

        public void ReturnConnection(string dsName, IDataSource connection)
        {
            if (_nodes.TryGetValue(dsName, out var n))
                n.Proxy.ReturnConnection(dsName, connection);
        }

        public IDataSource GetConnection(string dsName) =>
            _nodes.TryGetValue(dsName, out var n) ? n.Proxy.GetConnection(dsName) : null;

        // ─────────────────────────────────────────────────────────────────────
        //  IProxyDataSource — cache shims (caching belongs per-node)
        // ─────────────────────────────────────────────────────────────────────

        public object GetEntityWithCache(string entityName, List<AppFilter> filter,
            TimeSpan? expiration = null)
        {
            var node = SelectNode(isWrite: false);
            node.IncrementInFlight();
            try   { return node.Proxy.GetEntityWithCache(entityName, filter, expiration); }
            finally { node.DecrementInFlight(); }
        }

        public void InvalidateCache(string entityName = null)
        {
            foreach (var n in _nodes.Values)
                n.Proxy.InvalidateCache(entityName);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Private ConfigEditor helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Upserts a node's <see cref="ConnectionProperties"/> in ConfigEditor.
        /// Preserves the existing GuidID of a record with the same connection name so
        /// downstream references remain stable after an update.
        /// </summary>
        private void PersistNodeConfig(ConnectionProperties cfg)
        {
            if (DMEEditor?.ConfigEditor == null) return;

            var existing = DMEEditor.ConfigEditor.DataConnections
                .FirstOrDefault(c => string.Equals(c.ConnectionName, cfg.ConnectionName,
                                                    StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                DMEEditor.ConfigEditor.UpdateDataConnection(cfg, existing.GuidID);
            else
                DMEEditor.ConfigEditor.AddDataConnection(cfg);

            DMEEditor.ConfigEditor.SaveDataconnectionsValues();
        }
    }
}
