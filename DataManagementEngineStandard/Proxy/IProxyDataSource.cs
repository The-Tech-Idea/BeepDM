using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// Proxy-specific datasource contract that extends <see cref="IDataSource"/>
    /// with policy-driven balancing, failover, health, cache, metrics, and SLO capabilities.
    /// </summary>
    public interface IProxyDataSource : IDataSource, IDisposable
    {
        // ── Events ────────────────────────────────────────────────────

        /// <summary>Raised when proxy failover switches the active datasource.</summary>
        event EventHandler<FailoverEventArgs> OnFailover;

        /// <summary>Raised when a datasource recovers from unhealthy to healthy state.</summary>
        event EventHandler<RecoveryEventArgs> OnRecovery;

        // ── Policy (Phase 1) ──────────────────────────────────────────

        /// <summary>Applies a new <see cref="ProxyPolicy"/> as the single source of truth at runtime.</summary>
        void ApplyPolicy(ProxyPolicy policy);

        // ── Backward-compat knobs (stay in sync with policy) ─────────

        int MaxRetries                       { get; set; }
        int RetryDelayMilliseconds           { get; set; }
        int HealthCheckIntervalMilliseconds  { get; set; }

        // ── Load-balanced execution ───────────────────────────────────

        /// <summary>Executes an operation using the active proxy routing and resilience policy.</summary>
        /// <param name="isWrite">
        /// When <c>true</c> the operation is routed exclusively to Primary-role candidates.
        /// Defaults to <c>false</c> (read routing — all healthy candidates).
        /// </param>
        Task<T> ExecuteWithLoadBalancing<T>(Func<IDataSource, Task<T>> operation, bool isWrite = false, CancellationToken cancellationToken = default);

        // ── Connection pool ───────────────────────────────────────────

        IDataSource GetPooledConnection(string dsName);
        void        ReturnConnection(string dsName, IDataSource connection);
        IDataSource GetConnection(string dsName);

        // ── Cache (Phase 5) ───────────────────────────────────────────

        object GetEntityWithCache(string entityName, List<AppFilter> filter, TimeSpan? expiration = null);
        void   InvalidateCache(string entityName = null);

        // ── Metrics & SLO (Phase 6) ───────────────────────────────────

        IDictionary<string, DataSourceMetrics> GetMetrics();
        ProxySloSnapshot                       GetSloSnapshot(string dsName);
        IReadOnlyList<ProxySloSnapshot>        GetAllSloSnapshots();

        // ── Datasource membership ─────────────────────────────────────

        void AddDataSource(string dsName, int weight = 1);
        void RemoveDataSource(string dsName);
        void SetRole(string dsName, ProxyDataSourceRole role);

        // ── Watchdog ──────────────────────────────────────────────────

        int WatchdogIntervalMs       { get; set; }
        int WatchdogProbeTimeoutMs   { get; set; }
        int WatchdogFailureThreshold  { get; set; }
        int WatchdogRecoveryThreshold { get; set; }

        /// <summary>Raised when the watchdog promotes a Replica to Primary.</summary>
        event EventHandler<RoleChangeEventArgs> OnRolePromoted;

        /// <summary>Raised when the watchdog demotes a recovered source back to its original role.</summary>
        event EventHandler<RoleChangeEventArgs> OnRoleDemoted;

        void StartWatchdog();
        void StopWatchdog();

        IReadOnlyList<WatchdogNodeStatus> GetWatchdogStatus();

        // ── Audit (P1-10) ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Replaceable at runtime to enable or swap the audit write destination.
        /// Defaults to <see cref="NullProxyAuditSink.Instance"/> (no-op).
        /// Assign a <see cref="FileProxyAuditSink"/> (or any custom <see cref="IProxyAuditSink"/>)
        /// before issuing operations to capture a full audit trail.
        /// </summary>
        IProxyAuditSink AuditSink { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  IProxyCluster  — cluster-tier extension of IProxyDataSource (Phase 11)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extends <see cref="IProxyDataSource"/> with cluster-tier node management,
    /// policy fan-out, and aggregate observability.
    /// A <see cref="ProxyCluster"/> implements this interface — callers that only need
    /// <see cref="IDataSource"/> never need to cast to it.
    /// </summary>
    public interface IProxyCluster : IProxyDataSource
    {
        // ── Node management ────────────────────────────────────────────────

        /// <summary>Adds a new node to the cluster's routing pool.</summary>
        void AddNode(IProxyNode node);

        /// <summary>Removes the node with the given ID from the cluster.</summary>
        void RemoveNode(string nodeId);

        /// <summary>Returns a snapshot of all current cluster nodes.</summary>
        IReadOnlyList<IProxyNode> GetNodes();

        // ── Drain / rolling restart ────────────────────────────────────────

        /// <summary>
        /// Puts a node into draining mode: new routing skips it; blocks (synchronously)
        /// until <see cref="IProxyNode.InFlightCount"/> reaches zero or
        /// <paramref name="timeoutMs"/> elapses.
        /// </summary>
        void DrainNode(string nodeId, int timeoutMs = 30_000);

        /// <summary>Async variant of <see cref="DrainNode"/>.</summary>
        System.Threading.Tasks.Task DrainNodeAsync(string nodeId, int timeoutMs = 30_000,
            System.Threading.CancellationToken cancellationToken = default);

        // ── Policy propagation ─────────────────────────────────────────────

        /// <summary>
        /// Applies a new <see cref="ProxyPolicy"/> to the cluster AND fans it out
        /// to every registered node's underlying <see cref="IProxyDataSource"/>.
        /// </summary>
        void ApplyClusterPolicy(ProxyPolicy policy);

        // ── Aggregate observability ────────────────────────────────────────

        /// <summary>Returns merged metrics keyed by node ID.</summary>
        IDictionary<string, DataSourceMetrics> GetClusterMetrics();

        /// <summary>Returns an SLO snapshot for each cluster node.</summary>
        IReadOnlyList<ProxySloSnapshot> GetClusterSloSnapshots();

        // ── Cluster events ─────────────────────────────────────────────────

        event EventHandler<NodeStatusEventArgs>           OnNodeDown;
        event EventHandler<NodeStatusEventArgs>           OnNodeRestored;
        event EventHandler<RoleChangeEventArgs>           OnNodePromoted;
        event EventHandler<RoleChangeEventArgs>           OnNodeDemoted;
        event EventHandler<ClusterPolicyChangedEventArgs> OnClusterPolicyChanged;
        event EventHandler<AffinityRebalancedEventArgs>   OnAffinityRebalanced;
    }
}
