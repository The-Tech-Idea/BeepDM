using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Proxy
{
    // ─────────────────────────────────────────────────────────────────────────
    //  IProxyNode  — contract for a single cluster-tier node
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single addressable proxy node within a <see cref="ProxyCluster"/>.
    /// Wraps an <see cref="IProxyDataSource"/> with cluster-level metadata.
    /// </summary>
    public interface IProxyNode
    {
        /// <summary>Unique identifier for this node within the cluster.</summary>
        string NodeId { get; }

        /// <summary>
        /// Persisted configuration for this node backed by <see cref="ConnectionProperties"/>.
        /// Allows the node's address, credentials, role, and weight to be stored in
        /// <c>ConfigEditor.DataConnections</c> and survive application restarts.
        /// </summary>
        ConnectionProperties NodeConfig { get; }

        /// <summary>The underlying proxy datasource that handles actual I/O.</summary>
        IProxyDataSource Proxy { get; }

        /// <summary>
        /// Relative routing weight (1 = normal, 2 = twice as likely to be picked).
        /// Mutable so slow-start can ramp it up at runtime.
        /// </summary>
        int Weight { get; set; }

        /// <summary>Cluster-tier role: Primary receives writes; Replica receives reads; Standby is hot spare.</summary>
        ProxyDataSourceRole NodeRole { get; set; }

        /// <summary>Last liveness probe result. <c>false</c> means the node is excluded from routing.</summary>
        bool IsAlive { get; }

        /// <summary>UTC timestamp of the last probe attempt.</summary>
        DateTime LastProbeUtc { get; }

        /// <summary>Number of in-flight operations currently routed to this node.</summary>
        int InFlightCount { get; }

        /// <summary>Node-scoped aggregated metrics (populated from the first entry of node.Proxy.GetMetrics()).</summary>
        DataSourceMetrics Metrics { get; }

        /// <summary>UTC timestamp when this node was added to the cluster (used for slow-start weight ramp).</summary>
        DateTime AddedAtUtc { get; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ProxyNode  — concrete implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Concrete <see cref="IProxyNode"/> implementation.  Stores atomic in-flight
    /// counters and probe state used by <see cref="ProxyCluster"/> internals.
    /// </summary>
    public sealed class ProxyNode : IProxyNode
    {
        // ── Atomic in-flight counter ──────────────────────────────────────
        private int _inFlightCount;

        // ── IProxyNode ────────────────────────────────────────────────────

        /// <inheritdoc/>
        public string NodeId { get; }

        /// <inheritdoc/>
        public ConnectionProperties NodeConfig { get; private set; }

        /// <inheritdoc/>
        public IProxyDataSource Proxy { get; }

        /// <inheritdoc/>
        public int Weight { get; set; }

        /// <inheritdoc/>
        public ProxyDataSourceRole NodeRole { get; set; }

        /// <inheritdoc/>
        public bool IsAlive { get; internal set; } = true;

        /// <inheritdoc/>
        public DateTime LastProbeUtc { get; internal set; } = DateTime.UtcNow;

        /// <inheritdoc/>
        public int InFlightCount => Interlocked.CompareExchange(ref _inFlightCount, 0, 0);

        /// <inheritdoc/>
        public DataSourceMetrics Metrics
            => Proxy?.GetMetrics()?.Values.FirstOrDefault() ?? new DataSourceMetrics();

        /// <inheritdoc/>
        public DateTime AddedAtUtc { get; } = DateTime.UtcNow;

        // ── Internal cluster properties ───────────────────────────────────

        /// <summary>When <c>true</c> the node is draining — no new requests are routed to it.</summary>
        internal bool IsDraining { get; set; }

        /// <summary>Consecutive probe failures since last recovery (used by probing loop).</summary>
        internal int ConsecutiveFailures { get; set; }

        /// <summary>Consecutive probe successes since last failure (used by probing loop).</summary>
        internal int ConsecutiveSuccesses { get; set; }

        // ── Outlier-detection state (Phase 11.8) ──────────────────────────

        /// <summary>Rolling error counter for outlier detection (reset on ejection).</summary>
        internal int OutlierErrorCount { get; set; }

        /// <summary>Total requests in the current analysis window.</summary>
        internal int OutlierWindowRequests { get; set; }

        /// <summary>Error requests in the current analysis window.</summary>
        internal int OutlierWindowErrors { get; set; }

        /// <summary>UTC start of the current outlier analysis window.</summary>
        internal DateTime OutlierWindowStart { get; set; } = DateTime.UtcNow;

        /// <summary>Number of times this node has been ejected (for exponential back-off).</summary>
        internal int OutlierEjectionCount { get; set; }

        /// <summary>When set, the node is ejected until this UTC time has elapsed.</summary>
        internal DateTime? OutlierEjectedUntil { get; set; }

        // ── Replica-lag guard (Phase 11.9) ────────────────────────────────

        /// <summary>Most recently measured replication lag in milliseconds (0 = unknown/primary).</summary>
        internal long ReplicaLagMs { get; set; }

        // ── Constructor ───────────────────────────────────────────────────

        /// <summary>
        /// Creates a new cluster node.
        /// </summary>
        /// <param name="nodeId">Unique node identifier.</param>
        /// <param name="proxy">Underlying proxy datasource.</param>
        /// <param name="weight">Initial routing weight (default 1).</param>
        /// <param name="role">Node role (default <see cref="ProxyDataSourceRole.Primary"/>).</param>
        public ProxyNode(
            string nodeId,
            IProxyDataSource proxy,
            int weight = 1,
            ProxyDataSourceRole role = ProxyDataSourceRole.Primary)
        {
            NodeId   = nodeId  ?? throw new ArgumentNullException(nameof(nodeId));
            Proxy    = proxy   ?? throw new ArgumentNullException(nameof(proxy));
            Weight   = weight > 0 ? weight : 1;
            NodeRole = role;
            NodeConfig = BuildConfig(nodeId, weight, role, clusterName: null, remoteUrl: null, apiKey: null, timeoutSeconds: 30);
        }

        /// <summary>
        /// Creates a node that is already backed by a <see cref="ConnectionProperties"/> record
        /// (e.g. loaded from <c>ConfigEditor.DataConnections</c>).
        /// <paramref name="config"/> values take precedence over the other arguments.
        /// </summary>
        public ProxyNode(
            ConnectionProperties config,
            IProxyDataSource proxy)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            Proxy    = proxy ?? throw new ArgumentNullException(nameof(proxy));
            NodeId   = config.ConnectionName;
            Weight   = config.ParameterList.TryGetValue("Weight", out var w) && int.TryParse(w, out var wi) ? wi : 1;
            NodeRole = config.ParameterList.TryGetValue("NodeRole", out var r)
                           && Enum.TryParse<ProxyDataSourceRole>(r, out var ri)
                           ? ri : ProxyDataSourceRole.Primary;
            NodeConfig = config;
        }

        // ── Config persistence helpers ────────────────────────────────────

        /// <summary>
        /// Returns (and updates) the <see cref="ConnectionProperties"/> representing this node
        /// for storage in <c>ConfigEditor.DataConnections</c>.
        /// </summary>
        /// <param name="clusterName">Name of the owning cluster — written to <c>ParameterList["ClusterName"]</c>.</param>
        /// <param name="remoteUrl">
        /// Base URL for remote (HTTP) nodes, e.g. <c>http://worker-a:5100</c>.
        /// Leave <c>null</c> for local nodes.
        /// </param>
        /// <param name="apiKey">API key stored in <c>ConnectionProperties.ApiKey</c>. Never logged.</param>
        /// <param name="timeoutSeconds">Transport timeout in seconds (stored in <c>Timeout</c>).</param>
        public ConnectionProperties ToConnectionProperties(
            string? clusterName     = null,
            string? remoteUrl       = null,
            string? apiKey          = null,
            int     timeoutSeconds  = 30)
        {
            NodeConfig = BuildConfig(NodeId, Weight, NodeRole, clusterName, remoteUrl, apiKey, timeoutSeconds);
            return NodeConfig;
        }

        private static ConnectionProperties BuildConfig(
            string nodeId,
            int weight,
            ProxyDataSourceRole role,
            string? clusterName,
            string? remoteUrl,
            string? apiKey,
            int timeoutSeconds)
        {
            var cfg = new ConnectionProperties
            {
                ConnectionName = nodeId,
                DriverName     = "BeepProxyNode",
                Category       = DatasourceCategory.RDBMS,
                DatabaseType   = DataSourceType.Other,
                IsRemote       = !string.IsNullOrEmpty(remoteUrl),
                Url            = remoteUrl,
                ApiKey         = apiKey,
                Timeout        = timeoutSeconds,
                ParameterList  = new Dictionary<string, string>
                {
                    ["NodeRole"]    = role.ToString(),
                    ["Weight"]      = weight.ToString(),
                    ["ClusterName"] = clusterName ?? string.Empty
                }
            };
            return cfg;
        }

        // ── Internal helpers ──────────────────────────────────────────────

        internal void IncrementInFlight() => Interlocked.Increment(ref _inFlightCount);
        internal void DecrementInFlight() => Interlocked.Decrement(ref _inFlightCount);

        public override string ToString() =>
            $"ProxyNode[{NodeId}, role={NodeRole}, alive={IsAlive}, weight={Weight}, inFlight={InFlightCount}]";
    }
}
