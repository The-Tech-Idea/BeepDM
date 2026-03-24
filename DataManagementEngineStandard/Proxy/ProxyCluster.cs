using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// A cluster of <see cref="IProxyDataSource"/> nodes that exposes a single
    /// <see cref="IDataSource"/> surface to callers.  All routing, failover,
    /// affinity and policy fan-out are handled internally.
    ///
    /// Usage:
    /// <code>
    /// var cluster = new ProxyCluster(policy);
    /// cluster.AddNode(new ProxyNode("primary",  primaryProxy,  weight: 2));
    /// cluster.AddNode(new ProxyNode("replica1", replicaProxy1, weight: 1,
    ///     role: ProxyDataSourceRole.Replica));
    ///
    /// // Register and use like any IDataSource
    /// IDataSource ds = cluster;
    /// var rows = await ds.GetEntityAsync("Orders", filters);
    /// </code>
    /// </summary>
    public partial class ProxyCluster : IProxyCluster
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Core state
        // ─────────────────────────────────────────────────────────────────────

        private readonly ConcurrentDictionary<string, ProxyNode> _nodes = new();
        private ProxyPolicy        _clusterPolicy;
        private ICircuitStateStore _circuitStateStore;
        private IProxyAuditSink    _auditSink;
        private volatile bool      _disposed;

        // ── Config persistence (IDMEEditor is optional — cluster works without it) ──
        private string?    _clusterName;

        // Affinity: sessionKey → (nodeId, lastUsed)
        private readonly ConcurrentDictionary<string, (string NodeId, DateTime LastUsed)>
            _affinityMap = new();

        // 11.11: cluster-wide connection-count cap
        private SemaphoreSlim? _clusterConnectionSemaphore;

        // ─────────────────────────────────────────────────────────────────────
        //  IDataSource identity properties
        // ─────────────────────────────────────────────────────────────────────

        public string             GuidID           { get; set; } = Guid.NewGuid().ToString();
        public string             DatasourceName   { get; set; } = "ProxyCluster";
        public DataSourceType     DatasourceType   { get; set; } = DataSourceType.Other;
        public DatasourceCategory Category         { get; set; } = DatasourceCategory.RDBMS;
        public IErrorsInfo        ErrorObject      { get; set; }
        public string             Id               { get; set; }
        public IDMLogger          Logger           { get; set; }
        public ConnectionState    ConnectionStatus { get; set; }
        public IDataConnection    Dataconnection   { get; set; }
        public string             ColumnDelimiter   { get; set; }
        public string             ParameterDelimiter{ get; set; }
        public List<string>       EntitiesNames    { get; set; } = new();
        public List<EntityStructure> Entities      { get; set; } = new();
        public IDMEEditor         DMEEditor        { get; set; }

        // ── IProxyDataSource backward-compat knobs ────────────────────────
        public int MaxRetries                      { get; set; } = 3;
        public int RetryDelayMilliseconds          { get; set; } = 500;
        public int HealthCheckIntervalMilliseconds { get; set; } = 5_000;

        // ── Audit ─────────────────────────────────────────────────────────
        private IProxyAuditSink _auditSinkField;
        public IProxyAuditSink AuditSink
        {
            get => _auditSink;
            set => _auditSink = value ?? NullProxyAuditSink.Instance;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Events
        // ─────────────────────────────────────────────────────────────────────

        // IProxyDataSource events
        public event EventHandler<PassedArgs>         PassEvent;
        public event EventHandler<FailoverEventArgs>   OnFailover;
        public event EventHandler<RecoveryEventArgs>   OnRecovery;
        public event EventHandler<RoleChangeEventArgs> OnRolePromoted;
        public event EventHandler<RoleChangeEventArgs> OnRoleDemoted;

        // IProxyCluster events
        public event EventHandler<NodeStatusEventArgs>           OnNodeDown;
        public event EventHandler<NodeStatusEventArgs>           OnNodeRestored;
        public event EventHandler<RoleChangeEventArgs>           OnNodePromoted;
        public event EventHandler<RoleChangeEventArgs>           OnNodeDemoted;
        public event EventHandler<ClusterPolicyChangedEventArgs> OnClusterPolicyChanged;
        public event EventHandler<AffinityRebalancedEventArgs>   OnAffinityRebalanced;

        // ─────────────────────────────────────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new empty cluster.  Add nodes via <see cref="AddNode"/> before use.
        /// </summary>
        /// <param name="clusterPolicy">Policy governing probe intervals, routing strategy, etc.
        ///     Defaults to <see cref="ProxyPolicy.Default"/> if omitted.</param>
        /// <param name="circuitStateStore">
        ///     Optional shared circuit-state backend.  Uses in-process store if omitted.
        /// </param>
        /// <param name="auditSink">Optional audit trail destination.</param>
        public ProxyCluster(
            ProxyPolicy        clusterPolicy     = null,
            ICircuitStateStore circuitStateStore = null,
            IProxyAuditSink    auditSink         = null)
        {
            _clusterPolicy     = clusterPolicy ?? ProxyPolicy.Default;
            _circuitStateStore = circuitStateStore ?? new InProcessCircuitStateStore();
            _auditSink         = auditSink ?? NullProxyAuditSink.Instance;

            // Sync backward-compat knobs from policy
            MaxRetries                      = _clusterPolicy.Resilience.MaxRetries;
            RetryDelayMilliseconds          = _clusterPolicy.Resilience.RetryBaseDelayMs;
            HealthCheckIntervalMilliseconds = _clusterPolicy.NodeProbeIntervalMs;
            WatchdogIntervalMs              = _clusterPolicy.NodeProbeIntervalMs;
            WatchdogProbeTimeoutMs          = _clusterPolicy.NodeProbeTimeoutMs;
            WatchdogFailureThreshold        = _clusterPolicy.NodeUnhealthyThreshold;
            WatchdogRecoveryThreshold       = _clusterPolicy.NodeHealthyThreshold;

            // Build the initial router
            _nodeRouter = BuildRouter(_clusterPolicy.NodeRoutingStrategy);

            // Start background probe timer and affinity TTL sweep
            StartNodeProbeTimer();
            StartAffinityTtlSweep();
        }

        /// <summary>
        /// Creates a cluster that persists its node configuration to
        /// <c>ConfigEditor.DataConnections</c> via the supplied <paramref name="editor"/>.
        ///
        /// <para>
        /// Nodes added with <see cref="AddNode"/> will automatically be saved to config.
        /// Call <see cref="LoadLocalNodesFromConfig"/> after construction to restore a
        /// previously saved cluster topology (local nodes only — remote nodes require
        /// their transport to be rebuilt; see <see cref="LoadNodesFromConfig"/>).
        /// </para>
        /// </summary>
        /// <param name="editor">
        /// The DMEEditor providing access to <c>ConfigEditor</c> for persistence.
        /// </param>
        /// <param name="clusterName">
        /// Logical name for this cluster.  Used as a tag in
        /// <c>ConnectionProperties.ParameterList["ClusterName"]</c> so node records can
        /// be distinguished from regular datasource connections.
        /// </param>
        /// <param name="clusterPolicy">Policy governing routing, retries, probing, etc.</param>
        /// <param name="circuitStateStore">Optional shared circuit-state backend.</param>
        /// <param name="auditSink">Optional audit trail destination.</param>
        public ProxyCluster(
            IDMEEditor         editor,
            string             clusterName,
            ProxyPolicy        clusterPolicy     = null,
            ICircuitStateStore circuitStateStore = null,
            IProxyAuditSink    auditSink         = null)
            : this(clusterPolicy, circuitStateStore, auditSink)
        {
            DMEEditor    = editor ?? throw new ArgumentNullException(nameof(editor));
            Logger       = editor.Logger;
            ErrorObject  = editor.ErrorObject;
            _clusterName = clusterName ?? throw new ArgumentNullException(nameof(clusterName));
            DatasourceName = clusterName;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Config persistence  (requires IDMEEditor constructor)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes every current node's settings to <c>ConfigEditor.DataConnections</c>
        /// as a <see cref="ConnectionProperties"/> record (DriverName = "BeepProxyNode").
        ///
        /// <para>
        /// For remote nodes the <c>Url</c>, <c>ApiKey</c>, and <c>Timeout</c> fields on  the
        /// node's <see cref="ProxyNode.NodeConfig"/> must already be populated before calling
        /// this method (see <see cref="ProxyNode.ToConnectionProperties"/>).
        /// </para>
        /// </summary>
        public void SaveNodesToConfig()
        {
            if (DMEEditor?.ConfigEditor == null) return;
            foreach (var node in _nodes.Values)
            {
                var cfg = node.NodeConfig ?? node.ToConnectionProperties(_clusterName);
                cfg.ParameterList["ClusterName"] = _clusterName ?? string.Empty;
                cfg.ParameterList["NodeRole"]    = node.NodeRole.ToString();
                cfg.ParameterList["Weight"]      = node.Weight.ToString();

                var existing = DMEEditor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => string.Equals(c.ConnectionName, cfg.ConnectionName,
                                                        StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                    DMEEditor.ConfigEditor.UpdateDataConnection(cfg, existing.GuidID);
                else
                    DMEEditor.ConfigEditor.AddDataConnection(cfg);
            }
            DMEEditor.ConfigEditor.SaveDataconnectionsValues();
            Logger?.WriteLog($"[ProxyCluster] Saved {_nodes.Count} node(s) to config (cluster={_clusterName}).");
        }

        /// <summary>
        /// Rebuilds <b>local</b> nodes from <c>ConfigEditor.DataConnections</c>.
        /// For each stored record with <c>ParameterList["ClusterName"] == clusterName</c>
        /// and <c>IsRemote == false</c>, the method resolves the backing
        /// <see cref="IProxyDataSource"/> from <paramref name="backingProxyFactory"/>
        /// and calls <see cref="AddNode"/>.
        ///
        /// <para>
        /// Remote nodes cannot be rehydrated automatically because their transport
        /// (HTTP client, credentials) cannot be reconstructed from config alone.
        /// Use <see cref="LoadNodesFromConfig"/> to supply a factory for remote nodes.
        /// </para>
        /// </summary>
        /// <param name="backingProxyFactory">
        /// Receives a <see cref="ConnectionProperties"/> and must return the
        /// <see cref="IProxyDataSource"/> that backs that node
        /// (typically a <see cref="ProxyDataSource"/> wrapping the named connection).
        /// Return <c>null</c> to skip that node.
        /// </param>
        public void LoadLocalNodesFromConfig(
            Func<ConnectionProperties, IProxyDataSource?> backingProxyFactory)
        {
            if (DMEEditor?.ConfigEditor == null) return;

            var records = DMEEditor.ConfigEditor.DataConnections
                .Where(c => c.DriverName == "BeepProxyNode"
                         && c.ParameterList.TryGetValue("ClusterName", out var cn)
                         && cn == _clusterName
                         && !c.IsRemote)
                .ToList();

            foreach (var cfg in records)
            {
                if (_nodes.ContainsKey(cfg.ConnectionName)) continue;
                var proxy = backingProxyFactory(cfg);
                if (proxy == null) continue;
                AddNode(new ProxyNode(cfg, proxy));
            }

            Logger?.WriteLog($"[ProxyCluster] Loaded {records.Count} local node(s) from config (cluster={_clusterName}).");
        }

        /// <summary>
        /// Rebuilds all nodes (local and remote) from <c>ConfigEditor.DataConnections</c>.
        /// </summary>
        /// <param name="nodeFactory">
        /// Receives each stored <see cref="ConnectionProperties"/> and must return the
        /// matching <see cref="IProxyDataSource"/> (or <c>null</c> to skip).
        /// For remote nodes, construct an <see cref="Remote.HttpProxyTransport"/> +
        /// <see cref="Remote.RemoteProxyDataSource"/> from <c>cfg.Url</c> and <c>cfg.ApiKey</c>.
        /// </param>
        public void LoadNodesFromConfig(
            Func<ConnectionProperties, IProxyDataSource?> nodeFactory)
        {
            if (DMEEditor?.ConfigEditor == null) return;

            var records = DMEEditor.ConfigEditor.DataConnections
                .Where(c => c.DriverName == "BeepProxyNode"
                         && c.ParameterList.TryGetValue("ClusterName", out var cn)
                         && cn == _clusterName)
                .ToList();

            foreach (var cfg in records)
            {
                if (_nodes.ContainsKey(cfg.ConnectionName)) continue;
                var proxy = nodeFactory(cfg);
                if (proxy == null) continue;
                AddNode(new ProxyNode(cfg, proxy));
            }

            Logger?.WriteLog($"[ProxyCluster] Loaded {records.Count} node(s) from config (cluster={_clusterName}).");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Policy (IProxyDataSource + IProxyCluster)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a new policy to the cluster itself AND fans it out to every node.
        /// Replaces the routing strategy, probe intervals, and all other settings.
        /// </summary>
        public void ApplyClusterPolicy(ProxyPolicy policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            _clusterPolicy = policy;
            _nodeRouter    = BuildRouter(policy.NodeRoutingStrategy);

            // Sync backward-compat knobs
            MaxRetries                      = policy.Resilience.MaxRetries;
            RetryDelayMilliseconds          = policy.Resilience.RetryBaseDelayMs;
            HealthCheckIntervalMilliseconds = policy.NodeProbeIntervalMs;
            WatchdogIntervalMs              = policy.NodeProbeIntervalMs;
            WatchdogProbeTimeoutMs          = policy.NodeProbeTimeoutMs;
            WatchdogFailureThreshold        = policy.NodeUnhealthyThreshold;
            WatchdogRecoveryThreshold       = policy.NodeHealthyThreshold;

            // Fan out to every node
            foreach (var node in _nodes.Values)
                node.Proxy.ApplyPolicy(policy);

            // 11.7 routing-rule cache
            RebuildRoutingRuleCache();
            // 11.10 rate-limiter rebuild
            RebuildRateLimiters();
            // 11.11 connection-count semaphore
            _clusterConnectionSemaphore?.Dispose();
            _clusterConnectionSemaphore = policy.ClusterMaxBackendConnections > 0
                ? new SemaphoreSlim(policy.ClusterMaxBackendConnections, policy.ClusterMaxBackendConnections)
                : null;

            // Restart probe timer with updated interval
            StopNodeProbeTimer();
            StartNodeProbeTimer();

            OnClusterPolicyChanged?.Invoke(this, new ClusterPolicyChangedEventArgs(policy));
            Logger?.WriteLog($"[ProxyCluster] Policy applied: strategy={policy.NodeRoutingStrategy}, probeInterval={policy.NodeProbeIntervalMs}ms");
        }

        /// <summary>Alias for <see cref="ApplyClusterPolicy"/> — satisfies <see cref="IProxyDataSource.ApplyPolicy"/>.</summary>
        public void ApplyPolicy(ProxyPolicy policy) => ApplyClusterPolicy(policy);

        // ─────────────────────────────────────────────────────────────────────
        //  Metrics + SLO  (IProxyDataSource + IProxyCluster)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns metrics from the first registered node (IProxyDataSource compat).</summary>
        public IDictionary<string, DataSourceMetrics> GetMetrics()
        {
            var first = _nodes.Values.FirstOrDefault();
            return first?.Proxy.GetMetrics() ?? new Dictionary<string, DataSourceMetrics>();
        }

        /// <summary>Returns merged metrics keyed by node ID.</summary>
        public IDictionary<string, DataSourceMetrics> GetClusterMetrics()
        {
            var result = new Dictionary<string, DataSourceMetrics>();
            foreach (var node in _nodes.Values)
            {
                var nodeMetrics = node.Proxy.GetMetrics();
                if (nodeMetrics != null)
                {
                    // Take the first entry from the node's own metrics dict and key it by nodeId
                    var m = nodeMetrics.Values.FirstOrDefault();
                    if (m != null)
                        result[node.NodeId] = m;
                }
            }
            return result;
        }

        public ProxySloSnapshot GetSloSnapshot(string dsName)
        {
            if (_nodes.TryGetValue(dsName, out var node))
                return node.Proxy.GetSloSnapshot(node.Proxy.DatasourceName ?? dsName);
            return new ProxySloSnapshot { DataSourceName = dsName };
        }

        public IReadOnlyList<ProxySloSnapshot> GetAllSloSnapshots() => GetClusterSloSnapshots();

        public IReadOnlyList<ProxySloSnapshot> GetClusterSloSnapshots() =>
            _nodes.Values
                .Select(n => n.Proxy.GetSloSnapshot(n.Proxy.DatasourceName ?? n.NodeId))
                .ToList();

        // ─────────────────────────────────────────────────────────────────────
        //  IDataSource — connection management
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Opens all node connections concurrently.</summary>
        public ConnectionState Openconnection()
        {
            foreach (var node in _nodes.Values)
            {
                try
                {
                    var state = node.Proxy.Openconnection();
                    node.IsAlive = state == ConnectionState.Open
                                || state == ConnectionState.Connecting;
                }
                catch
                {
                    node.IsAlive = false;
                }
            }

            ConnectionStatus = _nodes.Values.Any(n => n.IsAlive)
                ? ConnectionState.Open
                : ConnectionState.Closed;

            return ConnectionStatus;
        }

        /// <summary>Closes all node connections.</summary>
        public ConnectionState Closeconnection()
        {
            foreach (var node in _nodes.Values)
            {
                try { node.Proxy.Closeconnection(); } catch { /* best-effort */ }
            }
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionState.Closed;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  IDataSource — schema / structure (read from first live node)
        // ─────────────────────────────────────────────────────────────────────

        public IEnumerable<string> GetEntitesList()
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetEntitesList(); }
            finally { node.DecrementInFlight(); }
        }

        public bool CheckEntityExist(string entityName)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.CheckEntityExist(entityName); }
            finally { node.DecrementInFlight(); }
        }

        public int GetEntityIdx(string entityName)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetEntityIdx(entityName); }
            finally { node.DecrementInFlight(); }
        }

        public Type GetEntityType(string entityName)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetEntityType(entityName); }
            finally { node.DecrementInFlight(); }
        }

        public EntityStructure GetEntityStructure(string entityName, bool refresh)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetEntityStructure(entityName, refresh); }
            finally { node.DecrementInFlight(); }
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetEntityStructure(fnd, refresh); }
            finally { node.DecrementInFlight(); }
        }

        public IEnumerable<ChildRelation> GetChildTablesList(
            string tablename, string schemaName, string filterparamters)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetChildTablesList(tablename, schemaName, filterparamters); }
            finally { node.DecrementInFlight(); }
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(
            string entityname, string schemaName)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetEntityforeignkeys(entityname, schemaName); }
            finally { node.DecrementInFlight(); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  IDataSource — reads
        // ─────────────────────────────────────────────────────────────────────

        public IEnumerable<object> GetEntity(string entityName, List<AppFilter> filter)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetEntity(entityName, filter); }
            finally { node.DecrementInFlight(); }
        }

        public PagedResult GetEntity(string entityName, List<AppFilter> filter,
            int pageNumber, int pageSize)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetEntity(entityName, filter, pageNumber, pageSize); }
            finally { node.DecrementInFlight(); }
        }

        public async Task<IEnumerable<object>> GetEntityAsync(string entityName, List<AppFilter> filter)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return await node.Proxy.GetEntityAsync(entityName, filter).ConfigureAwait(false); }
            finally { node.DecrementInFlight(); }
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.RunQuery(qrystr); }
            finally { node.DecrementInFlight(); }
        }

        public double GetScalar(string query)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetScalar(query); }
            finally { node.DecrementInFlight(); }
        }

        public async Task<double> GetScalarAsync(string query)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return await node.Proxy.GetScalarAsync(query).ConfigureAwait(false); }
            finally { node.DecrementInFlight(); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  IDataSource — writes (route to Primary node)
        // ─────────────────────────────────────────────────────────────────────

        public IErrorsInfo ExecuteSql(string sql)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.ExecuteSql(sql); }
            finally { node.DecrementInFlight(); }
        }

        public IErrorsInfo InsertEntity(string entityName, object insertedData)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.InsertEntity(entityName, insertedData); }
            finally { node.DecrementInFlight(); }
        }

        public IErrorsInfo UpdateEntity(string entityName, object uploadDataRow)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.UpdateEntity(entityName, uploadDataRow); }
            finally { node.DecrementInFlight(); }
        }

        public IErrorsInfo UpdateEntities(string entityName, object uploadData,
            IProgress<PassedArgs> progress)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.UpdateEntities(entityName, uploadData, progress); }
            finally { node.DecrementInFlight(); }
        }

        public IErrorsInfo DeleteEntity(string entityName, object uploadDataRow)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.DeleteEntity(entityName, uploadDataRow); }
            finally { node.DecrementInFlight(); }
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.CreateEntityAs(entity); }
            finally { node.DecrementInFlight(); }
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.CreateEntities(entities); }
            finally { node.DecrementInFlight(); }
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.RunScript(dDLScripts); }
            finally { node.DecrementInFlight(); }
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            var node = SelectNode();
            node.IncrementInFlight();
            try   { return node.Proxy.GetCreateEntityScript(entities); }
            finally { node.DecrementInFlight(); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  IDataSource — transactions (route to Primary)
        // ─────────────────────────────────────────────────────────────────────

        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.BeginTransaction(args); }
            finally { node.DecrementInFlight(); }
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.EndTransaction(args); }
            finally { node.DecrementInFlight(); }
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            var node = SelectNode(isWrite: true);
            node.IncrementInFlight();
            try   { return node.Proxy.Commit(args); }
            finally { node.DecrementInFlight(); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  IDataSource — load-balanced execution (IProxyDataSource compat)
        // ─────────────────────────────────────────────────────────────────────

        public async Task<T> ExecuteWithLoadBalancing<T>(
            Func<IDataSource, Task<T>> operation,
            bool isWrite = false,
            CancellationToken cancellationToken = default)
        {
            // 11.11: cluster-wide connection cap
            if (_clusterConnectionSemaphore is not null)
            {
                bool acquired = await _clusterConnectionSemaphore
                    .WaitAsync(_clusterPolicy.ClusterQueueTimeoutMs, cancellationToken)
                    .ConfigureAwait(false);
                if (!acquired)
                    throw new TimeoutException(
                        $"[ProxyCluster] Connection queue timed out after {_clusterPolicy.ClusterQueueTimeoutMs} ms.");
            }

            var node = SelectNode(isWrite);
            node.IncrementInFlight();
            try
            {
                return await operation(node.Proxy).ConfigureAwait(false);
            }
            finally
            {
                node.DecrementInFlight();
                _clusterConnectionSemaphore?.Release();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Dispose
        // ─────────────────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            StopNodeProbeTimer();
            StopAffinityTtlSweep();

            foreach (var node in _nodes.Values)
            {
                try { node.Proxy.Dispose(); } catch { /* best-effort */ }
            }

            _nodes.Clear();
            _clusterConnectionSemaphore?.Dispose();
            _clusterConnectionSemaphore = null;
        }
    }
}
