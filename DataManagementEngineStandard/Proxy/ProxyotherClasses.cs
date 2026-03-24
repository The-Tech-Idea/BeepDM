using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Proxy
{
    // ─────────────────────────────────────────────────────────────────
    //  Phase 11 — Cluster-tier routing strategy
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Selects the algorithm used by <see cref="ProxyCluster"/> to choose a node
    /// from the pool of live candidates for each incoming operation.
    /// </summary>
    public enum ProxyNodeRoutingStrategy
    {
        /// <summary>Pick the node with the fewest currently in-flight operations.</summary>
        LeastConnections,
        /// <summary>Distribute across nodes proportionally to their <see cref="IProxyNode.Weight"/>.</summary>
        WeightedRoundRobin,
        /// <summary>Always pick the node with the lowest rolling average response time.</summary>
        LowestLatency,
        /// <summary>Route everything to a single Primary; Replicas only receive overflow/reads.</summary>
        PrimaryWithStandby,
        /// <summary>Consistent hash ring keyed on <see cref="ProxyExecutionContext.CorrelationId"/>.</summary>
        ConsistentHash
    }

    // ─────────────────────────────────────────────────────────────────
    //  Connection pool
    // ─────────────────────────────────────────────────────────────────

    public class PooledConnection
    {
        public IDataSource DataSource { get; set; }
        public DateTime LastUsed { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Metrics
    // ─────────────────────────────────────────────────────────────────

    public class DataSourceMetrics
    {
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;

        public long TotalRequests     => Interlocked.Read(ref _totalRequests);
        public long SuccessfulRequests => Interlocked.Read(ref _successfulRequests);
        public long FailedRequests    => Interlocked.Read(ref _failedRequests);

        public double AverageResponseTime { get; set; }
        public DateTime LastRequested { get; set; }
        public DateTime LastSuccessful { get; set; }
        public long CircuitBreaks { get; set; }
        public DateTime LastChecked { get; set; }

        public void IncrementTotalRequests()      => Interlocked.Increment(ref _totalRequests);
        public void IncrementFailedRequests()     => Interlocked.Increment(ref _failedRequests);
        public void IncrementSuccessfulRequests() => Interlocked.Increment(ref _successfulRequests);
    }

    // ─────────────────────────────────────────────────────────────────
    //  Error taxonomy  (Phase 2)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Classifies the nature of a data-source exception for retry/circuit decisions.</summary>
    public enum ProxyErrorCategory
    {
        Transient,       // Safe to retry; likely transient network/IO condition
        Timeout,         // Request timed out; may be retried with back-off
        AuthFailure,     // Credentials / permission denied; do not retry
        Saturation,      // Resource exhausted (OOM, connection pool full); do not retry immediately
        Persistent,      // Permanent error (schema missing, bad SQL, etc.)
        Unknown
    }

    /// <summary>Severity used to weight circuit-breaker failure accumulation.</summary>
    public enum ProxyErrorSeverity { Low, Medium, High, Critical }

    /// <summary>Classifies an exception into a <see cref="ProxyErrorCategory"/> and <see cref="ProxyErrorSeverity"/>.</summary>
    public static class ProxyErrorClassifier
    {
        public static (ProxyErrorCategory Category, ProxyErrorSeverity Severity) Classify(Exception ex)
        {
            if (ex is OperationCanceledException)
                return (ProxyErrorCategory.Transient, ProxyErrorSeverity.Low);
            if (ex is TimeoutException)
                return (ProxyErrorCategory.Timeout, ProxyErrorSeverity.Medium);
            if (ex is System.IO.IOException)
                return (ProxyErrorCategory.Transient, ProxyErrorSeverity.Low);
            if (ex is UnauthorizedAccessException)
                return (ProxyErrorCategory.AuthFailure, ProxyErrorSeverity.High);
            if (ex is OutOfMemoryException)
                return (ProxyErrorCategory.Saturation, ProxyErrorSeverity.Critical);

            var msg = ex.Message ?? string.Empty;
            if (msg.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                return (ProxyErrorCategory.Timeout, ProxyErrorSeverity.Medium);
            if (msg.IndexOf("deadlock", StringComparison.OrdinalIgnoreCase) >= 0 ||
                msg.IndexOf("transient", StringComparison.OrdinalIgnoreCase) >= 0 ||
                msg.IndexOf("connection reset", StringComparison.OrdinalIgnoreCase) >= 0)
                return (ProxyErrorCategory.Transient, ProxyErrorSeverity.Medium);
            if (msg.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0)
                return (ProxyErrorCategory.Transient, ProxyErrorSeverity.Low);

            return (ProxyErrorCategory.Persistent, ProxyErrorSeverity.High);
        }

        public static bool IsRetryEligible(ProxyErrorCategory category) => category switch
        {
            ProxyErrorCategory.Transient => true,
            ProxyErrorCategory.Timeout   => true,
            _                            => false
        };
    }

    // ─────────────────────────────────────────────────────────────────
    //  Operation safety  (Phase 4)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Declares the retry safety model for a proxy operation.</summary>
    public enum ProxyOperationSafety
    {
        ReadSafe,          // Safe to retry any number of times (reads, queries)
        IdempotentWrite,   // Write may be retried with the same idempotency key
        NonIdempotentWrite // Single-execute only; no automatic retry
    }

    // ─────────────────────────────────────────────────────────────────
    //  Routing strategy  (Phase 3)
    // ─────────────────────────────────────────────────────────────────

    public enum ProxyRoutingStrategy
    {
        WeightedLatency,          // Prefer low-latency, high-weight sources
        LeastOutstandingRequests, // Route to source with fewest in-flight requests
        RoundRobin,               // Simple sequential selection
        HealthWeighted            // Weight inversely proportional to failure rate
    }

    // ─────────────────────────────────────────────────────────────────
    //  Datasource role  (GAP-004)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Declares the read/write role of a registered datasource.</summary>
    public enum ProxyDataSourceRole
    {
        Primary,  // Receives all writes; eligible for reads
        Replica,  // Read-only; never receives writes
        Standby   // No normal traffic; failover target only
    }

    // ─────────────────────────────────────────────────────────────────
    //  Resilience profiles  (Phase 2)
    // ─────────────────────────────────────────────────────────────────

    public enum ProxyResilienceProfileType { Conservative, Balanced, AggressiveFailover, Custom }

    public class ProxyResilienceProfile
    {
        public ProxyResilienceProfileType ProfileType  { get; init; } = ProxyResilienceProfileType.Balanced;
        public int       MaxRetries                    { get; init; } = 3;
        public int       RetryBaseDelayMs              { get; init; } = 500;
        public int       RetryMaxDelayMs               { get; init; } = 30_000;
        public bool      UseExponentialBackoff         { get; init; } = true;
        public bool      UseJitter                     { get; init; } = true;
        public int       FailureThreshold              { get; init; } = 5;
        public TimeSpan  CircuitResetTimeout           { get; init; } = TimeSpan.FromMinutes(5);
        public int       ConsecutiveSuccessesToClose   { get; init; } = 2;

        // ── Preset factories ──────────────────────────────────────────

        public static ProxyResilienceProfile Conservative => new()
        {
            ProfileType             = ProxyResilienceProfileType.Conservative,
            MaxRetries              = 5,
            RetryBaseDelayMs        = 1_000,
            RetryMaxDelayMs         = 60_000,
            FailureThreshold        = 3,
            CircuitResetTimeout     = TimeSpan.FromMinutes(10),
            ConsecutiveSuccessesToClose = 3
        };

        public static ProxyResilienceProfile Balanced => new()
        {
            ProfileType             = ProxyResilienceProfileType.Balanced,
            MaxRetries              = 3,
            RetryBaseDelayMs        = 500,
            RetryMaxDelayMs         = 30_000,
            FailureThreshold        = 5,
            CircuitResetTimeout     = TimeSpan.FromMinutes(5),
            ConsecutiveSuccessesToClose = 2
        };

        public static ProxyResilienceProfile AggressiveFailover => new()
        {
            ProfileType             = ProxyResilienceProfileType.AggressiveFailover,
            MaxRetries              = 1,
            RetryBaseDelayMs        = 100,
            RetryMaxDelayMs         = 5_000,
            FailureThreshold        = 2,
            CircuitResetTimeout     = TimeSpan.FromMinutes(2),
            ConsecutiveSuccessesToClose = 1
        };
    }

    // ─────────────────────────────────────────────────────────────────
    //  Cache profile  (Phase 5)
    // ─────────────────────────────────────────────────────────────────

    public enum ProxyCacheTier      { None, RequestScope, ShortLived, EntityProfile }
    public enum ProxyCacheConsistency { Eventual, StaleWhileRevalidate, WriteThrough }

    public class ProxyCacheProfile
    {
        public bool                  Enabled           { get; init; } = true;
        public ProxyCacheTier        Tier              { get; init; } = ProxyCacheTier.ShortLived;
        public TimeSpan              DefaultExpiration { get; init; } = TimeSpan.FromMinutes(5);
        public TimeSpan              StaleWindow       { get; init; } = TimeSpan.FromSeconds(30);
        public ProxyCacheConsistency Consistency       { get; init; } = ProxyCacheConsistency.WriteThrough;
        public int                   MaxItems          { get; init; } = 1_000;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Proxy policy  (Phase 1) — single source of truth
    // ─────────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────
    //  Write fan-out mode  (P2-13)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Controls how writes are distributed across Primary-role datasources.</summary>
    public enum ProxyWriteMode
    {
        /// <summary>Default: route write to the first available Primary only.</summary>
        SinglePrimary,
        /// <summary>
        /// Fan-out: attempt write on ALL Primary-role datasources concurrently.
        /// Fails if any Primary rejects.
        /// </summary>
        FanOut,
        /// <summary>
        /// Quorum write: succeed if at least <see cref="ProxyPolicy.WriteFanOutQuorum"/> Primaries
        /// acknowledge. Remaining writes are best-effort.
        /// </summary>
        QuorumWrite
    }

    public class ProxyPolicy
    {
        public string                  Name                    { get; init; } = "default";
        public string                  Version                 { get; init; } = "1.0";
        public string                  Environment             { get; init; } = "production";
        public ProxyResilienceProfile  Resilience              { get; init; } = ProxyResilienceProfile.Balanced;
        public ProxyCacheProfile       Cache                   { get; init; } = new();
        public ProxyRoutingStrategy    RoutingStrategy         { get; init; } = ProxyRoutingStrategy.WeightedLatency;
        public int                     HealthCheckIntervalMs   { get; init; } = 30_000;
        public int                     HealthCheckTimeoutSecs  { get; init; } = 5;
        public int                     HealthyThresholdCount   { get; init; } = 2;   // consecutive successes before marking healthy
        public int                     UnhealthyThresholdCount { get; init; } = 2;   // consecutive failures before marking unhealthy

        // ── P1-9: PII log redaction ────────────────────────────────────
        /// <summary>
        /// When true (default), all proxy log messages pass through
        /// <see cref="ProxyLogRedactor"/> before reaching the DME logger.
        /// Set false in development to see raw values.
        /// </summary>
        public bool EnableLogRedaction { get; init; } = true;

        // ── P2-13: Write fan-out ───────────────────────────────────────
        /// <summary>Controls how writes are routed to Primary-role datasources.</summary>
        public ProxyWriteMode WriteMode          { get; init; } = ProxyWriteMode.SinglePrimary;
        /// <summary>
        /// Minimum number of Primaries that must acknowledge a write for
        /// <see cref="ProxyWriteMode.QuorumWrite"/> to report success.
        /// Ignored for other modes. Defaults to 1.
        /// </summary>
        public int WriteFanOutQuorum             { get; init; } = 1;

        public static ProxyPolicy Default => new ProxyPolicy();

        // ── Cluster tier (Phase 11.1) ────────────────────────────────────────

        /// <summary>Node-selection algorithm used by <see cref="ProxyCluster"/>.</summary>
        public ProxyNodeRoutingStrategy NodeRoutingStrategy  { get; init; } = ProxyNodeRoutingStrategy.LeastConnections;

        /// <summary>Consecutive probe failures before a node is marked down.</summary>
        public int  NodeUnhealthyThreshold  { get; init; } = 2;

        /// <summary>Consecutive probe successes before a down node is restored.</summary>
        public int  NodeHealthyThreshold    { get; init; } = 1;

        /// <summary>How often (ms) the cluster probes each node for liveness.</summary>
        public int  NodeProbeIntervalMs     { get; init; } = 5_000;

        /// <summary>Timeout (ms) for a single liveness probe call.</summary>
        public int  NodeProbeTimeoutMs      { get; init; } = 2_000;

        /// <summary>When <c>true</c>, sessions are pinned to a node for the affinity TTL duration.</summary>
        public bool EnableNodeAffinity      { get; init; } = false;

        /// <summary>How long (seconds) a session-affinity binding remains alive without activity.</summary>
        public int  NodeAffinityTtlSeconds  { get; init; } = 300;

        /// <summary>
        /// When positive, new nodes ramp up their effective weight over this period (milliseconds).
        /// Set 0 to disable slow-start.
        /// </summary>
        public int  SlowStartDurationMs     { get; init; } = 0;

        /// <summary>
        /// Maximum replication lag (ms) a replica may have before it is excluded from read routing.
        /// Set 0 to disable the lag guard.
        /// </summary>
        public long MaxReplicaLagMs         { get; init; } = 0;

        // ── Phase 11.6: Entity affinity ──────────────────────────────────────

        /// <summary>
        /// Static entity→node mappings.  Null = entity affinity routing disabled.
        /// </summary>
        public EntityAffinityMap? EntityAffinity  { get; init; } = null;

        /// <summary>What to do when the affinity-pinned node is unavailable.</summary>
        public EntityAffinityFallback AffinityFallback { get; init; }
            = EntityAffinityFallback.RouteToAny;

        // ── Phase 11.7: Routing rules + traffic splits ───────────────────────

        /// <summary>
        /// Ordered routing rules (descending Priority).  First match wins.
        /// </summary>
        public IReadOnlyList<ProxyRoutingRule> RoutingRules { get; init; }
            = Array.Empty<ProxyRoutingRule>();

        /// <summary>Traffic-splitting rules for canary deployments.</summary>
        public IReadOnlyList<TrafficSplitRule> TrafficSplits { get; init; }
            = Array.Empty<TrafficSplitRule>();

        // ── Phase 11.8: Hedging + outlier detection ──────────────────────────

        /// <summary>Outlier detection configuration.  Null = disabled.</summary>
        public OutlierDetectionPolicy? OutlierDetection { get; init; } = null;

        /// <summary>Enable hedged requests when a node is slow to respond.</summary>
        public bool EnableHedging          { get; init; } = false;

        /// <summary>Delay (ms) before issuing the hedge request.</summary>
        public int  HedgingThresholdMs     { get; init; } = 100;

        /// <summary>Maximum in-flight copies including the original (minimum 2).</summary>
        public int  MaxHedgeRequests       { get; init; } = 2;

        // ── Phase 11.9: Scatter-gather read mode ─────────────────────────────

        /// <summary>How read operations are distributed across live nodes.</summary>
        public ProxyReadMode ReadMode      { get; init; } = ProxyReadMode.SingleNode;

        // ── Phase 11.10: Fault injection + rate limiting ─────────────────────

        /// <summary>Fault injection config.  Null = disabled (default).</summary>
        public FaultInjectionPolicy? FaultInjection { get; init; } = null;

        /// <summary>Per-node rate limits.  Empty = no rate limiting (default).</summary>
        public IReadOnlyList<NodeRateLimit> NodeRateLimits { get; init; }
            = Array.Empty<NodeRateLimit>();

        // ── Phase 11.11: Connection multiplexing ─────────────────────────────

        /// <summary>
        /// Total backend connections allowed across all nodes simultaneously.
        /// 0 = unlimited (default).
        /// </summary>
        public int ClusterMaxBackendConnections { get; init; } = 0;

        /// <summary>
        /// Maximum queued requests when all backend connections are busy.
        /// Only used when <see cref="ClusterMaxBackendConnections"/> &gt; 0.
        /// </summary>
        public int ClusterMaxQueueDepth         { get; init; } = 100;

        /// <summary>Time (ms) a queued request waits before timing out.</summary>
        public int ClusterQueueTimeoutMs        { get; init; } = 5_000;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Phase 11.6 — Entity affinity
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// What the cluster does when the affinity-pinned node is unavailable.
    /// </summary>
    public enum EntityAffinityFallback
    {
        /// <summary>Use the normal router strategy to pick any live node.</summary>
        RouteToAny,
        /// <summary>Throw an exception — the entity cannot be served without its owner.</summary>
        ThrowException,
        /// <summary>Wait until the pinned node recovers (blocks up to NodeProbeIntervalMs).</summary>
        WaitForOwner
    }

    // ─────────────────────────────────────────────────────────────────
    //  Phase 11.7 — Query routing rules + traffic splitting
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Strategy applied by a routing rule when no explicit TargetNodeId is given.</summary>
    public enum ProxyRoutingOverrideStrategy
    {
        /// <summary>Route to any replica node (role = Replica). Falls back to primary if none.</summary>
        RouteToReplica,
        /// <summary>Route to the primary node (role = Primary).</summary>
        RouteToPrimary,
        /// <summary>Route to any live node using the cluster's default router.</summary>
        RouteToAny
    }

    /// <summary>
    /// Declarative routing rule — evaluated before the normal router strategy.
    /// Rules are sorted by descending <see cref="Priority"/> (higher = evaluated first).
    /// </summary>
    public sealed class ProxyRoutingRule
    {
        /// <summary>
        /// Regex applied to the operation name (e.g. "GetEntity", "InsertDataRow").
        /// Null = any operation.
        /// </summary>
        public string? OperationPattern { get; init; }

        /// <summary>
        /// Regex applied to the entity/table name hint.
        /// Null = any entity.
        /// </summary>
        public string? EntityPattern { get; init; }

        /// <summary>Target node ID.  Null = use the override strategy.</summary>
        public string? TargetNodeId { get; init; }

        /// <summary>Strategy when TargetNodeId is null or target is unavailable.</summary>
        public ProxyRoutingOverrideStrategy OverrideStrategy { get; init; }
            = ProxyRoutingOverrideStrategy.RouteToReplica;

        /// <summary>Higher values are evaluated first.</summary>
        public int Priority { get; init; } = 0;
    }

    /// <summary>Scope of operations to which a traffic-split rule applies.</summary>
    public enum ProxySplitScope
    {
        All,
        ReadsOnly,
        WritesOnly
    }

    /// <summary>
    /// Directs a percentage of traffic to a specific node for canary testing.
    /// </summary>
    public sealed class TrafficSplitRule
    {
        /// <summary>Node that receives the canary slice.</summary>
        public string TargetNodeId { get; init; } = default!;

        /// <summary>0–100. 5 = 5 % of matching requests go to TargetNodeId.</summary>
        public int WeightPercent { get; init; } = 5;

        /// <summary>Scope of operations this split applies to.</summary>
        public ProxySplitScope OperationScope { get; init; } = ProxySplitScope.All;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Phase 11.8 — Outlier detection policy
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Envoy-style outlier detection: eject misbehaving nodes from the routing pool
    /// for an exponentially increasing back-off period.
    /// </summary>
    public sealed class OutlierDetectionPolicy
    {
        /// <summary>Consecutive errors needed to eject a node immediately.</summary>
        public int ConsecutiveErrorThreshold { get; init; } = 5;

        /// <summary>Error proportion (0.0–1.0) over the analysis interval.</summary>
        public double ErrorRateThreshold { get; init; } = 0.5;

        /// <summary>Interval (ms) over which error rate is measured.</summary>
        public int IntervalMs { get; init; } = 10_000;

        /// <summary>Base ejection time (ms) on first ejection.</summary>
        public int BaseEjectionTimeMs { get; init; } = 30_000;

        /// <summary>Maximum ejection time (ms) — exponential cap.</summary>
        public int MaxEjectionTimeMs { get; init; } = 300_000;

        /// <summary>Maximum % of nodes that may be ejected simultaneously.</summary>
        public int MaxEjectionPercent { get; init; } = 50;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Phase 11.9 — Scatter-gather read mode
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// How the cluster handles read operations when multiple live nodes are available.
    /// </summary>
    public enum ProxyReadMode
    {
        /// <summary>Route to the single best node (default).</summary>
        SingleNode,
        /// <summary>Broadcast to all live nodes in parallel and merge results.</summary>
        ScatterGather
    }

    // ─────────────────────────────────────────────────────────────────
    //  Phase 11.10 — Fault injection + rate limiting
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Enables deliberate error and latency injection for chaos engineering.
    /// Should only be enabled in test / staging environments.
    /// </summary>
    public sealed class FaultInjectionPolicy
    {
        /// <summary>Probability (0.0–1.0) that a request returns a fake error.</summary>
        public double ErrorRate { get; init; } = 0.0;

        /// <summary>Probability (0.0–1.0) that a request experiences an artificial delay.</summary>
        public double DelayRate { get; init; } = 0.0;

        /// <summary>Artificial delay (ms) when DelayRate triggers.</summary>
        public int DelayMs { get; init; } = 500;

        /// <summary>Limit injection to this node.  Null = all nodes.</summary>
        public string? TargetNodeId { get; init; }

        /// <summary>Limit injection to this entity.  Null = all entities.</summary>
        public string? TargetEntity { get; init; }
    }

    /// <summary>Caps requests-per-second routed to a specific node.</summary>
    public sealed class NodeRateLimit
    {
        /// <summary>The node this limit applies to.</summary>
        public string NodeId { get; init; } = default!;

        /// <summary>Maximum requests per second (token-bucket refill rate).</summary>
        public int MaxRps { get; init; } = 1000;

        /// <summary>What happens when the node's rate limit is exceeded.</summary>
        public RateLimitAction Action { get; init; } = RateLimitAction.RouteElsewhere;
    }

    /// <summary>Action taken when a node's rate limit is exceeded.</summary>
    public enum RateLimitAction
    {
        /// <summary>Pick the next available node instead (transparent to caller).</summary>
        RouteElsewhere,
        /// <summary>Queue the request until a slot is available.</summary>
        Queue,
        /// <summary>Immediately reject with a rate-limit exception.</summary>
        Reject
    }

    /// <summary>
    /// Thrown by fault injection.  Never thrown in production (FaultInjectionPolicy = null).
    /// </summary>
    [Serializable]
    public sealed class ProxyFaultInjectionException : Exception
    {
        public ProxyFaultInjectionException(string message) : base(message) { }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Options  (preserved for backward-compat; populated from policy)
    // ─────────────────────────────────────────────────────────────────

    public class ProxyDataSourceOptions
    {
        public int      MaxRetries                      { get; set; } = 3;
        public int      RetryDelayMilliseconds          { get; set; } = 500;
        public int      HealthCheckIntervalMilliseconds { get; set; } = 30_000;
        public int      FailureThreshold                { get; set; } = 5;
        public TimeSpan CircuitResetTimeout             { get; set; } = TimeSpan.FromMinutes(5);
        public bool     EnableCaching                   { get; set; } = true;
        public TimeSpan DefaultCacheExpiration          { get; set; } = TimeSpan.FromMinutes(5);
        public bool     EnableLoadBalancing             { get; set; } = true;

        public static ProxyDataSourceOptions FromPolicy(ProxyPolicy policy) => new()
        {
            MaxRetries                      = policy.Resilience.MaxRetries,
            RetryDelayMilliseconds          = policy.Resilience.RetryBaseDelayMs,
            HealthCheckIntervalMilliseconds = policy.HealthCheckIntervalMs,
            FailureThreshold                = policy.Resilience.FailureThreshold,
            CircuitResetTimeout             = policy.Resilience.CircuitResetTimeout,
            EnableCaching                   = policy.Cache.Enabled,
            DefaultCacheExpiration          = policy.Cache.DefaultExpiration,
        };
    }

    // ─────────────────────────────────────────────────────────────────
    //  Execution context  (Phase 4)
    // ─────────────────────────────────────────────────────────────────

    public class ProxyExecutionContext
    {
        public string               CorrelationId    { get; }
        public ProxyOperationSafety OperationSafety  { get; init; } = ProxyOperationSafety.ReadSafe;
        public string               IdempotencyKey   { get; init; }
        /// <summary>
        /// Optional key used to pin this request to a specific node via session affinity.
        /// Typically a user/session ID.  When null, affinity is skipped.
        /// </summary>
        public string               SessionKey       { get; init; }
        public List<ProxyAttemptRecord> Attempts     { get; } = new List<ProxyAttemptRecord>();
        public DateTime             StartedAt        { get; } = DateTime.UtcNow;

        public ProxyExecutionContext(string correlationId = null, ProxyOperationSafety safety = ProxyOperationSafety.ReadSafe)
        {
            CorrelationId   = correlationId ?? Guid.NewGuid().ToString("N").Substring(0, 8);
            OperationSafety = safety;
        }
    }

    public class ProxyAttemptRecord
    {
        public string             DataSourceName { get; init; }
        public int                AttemptNumber  { get; init; }
        public bool               Success        { get; init; }
        public TimeSpan           Duration       { get; init; }
        public string             ErrorMessage   { get; init; }
        public ProxyErrorCategory? ErrorCategory { get; init; }
        public DateTime           AttemptedAt    { get; init; } = DateTime.UtcNow;
    }

    // ─────────────────────────────────────────────────────────────────
    //  SLO snapshot  (Phase 6)
    // ─────────────────────────────────────────────────────────────────

    public class ProxySloSnapshot
    {
        public string   DataSourceName   { get; init; }
        public double   P50LatencyMs     { get; init; }
        public double   P95LatencyMs     { get; init; }
        public double   P99LatencyMs     { get; init; }
        public double   ErrorRatePercent { get; init; }
        public long     TotalRequests    { get; init; }
        /// <summary>Cache hit ratio [0.0 – 1.0] derived from the per-proxy CacheScope statistics.</summary>
        public double   CacheHitRatio    { get; init; }
        public DateTime SnapshotTime     { get; init; } = DateTime.UtcNow;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────

    public class FailoverEventArgs : EventArgs
    {
        public string FromDataSource { get; set; }
        public string ToDataSource   { get; set; }
        public string Reason         { get; set; }
    }

    public class RecoveryEventArgs : EventArgs
    {
        public string   DataSourceName { get; set; }
        public DateTime RecoveredAt    { get; set; } = DateTime.UtcNow;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Watchdog events  (Watchdog layer)
    // ─────────────────────────────────────────────────────────────────

    public class RoleChangeEventArgs : EventArgs
    {
        public string             DataSourceName { get; set; }
        public ProxyDataSourceRole OldRole        { get; set; }
        public ProxyDataSourceRole NewRole        { get; set; }
        public string             Reason         { get; set; }
    }

    /// <summary>Point-in-time health + role snapshot for a single node.</summary>
    public class WatchdogNodeStatus
    {
        public string              DataSourceName    { get; init; }
        public ProxyDataSourceRole Role              { get; init; }
        public bool                IsHealthy         { get; init; }
        public bool                IsCircuitOpen     { get; init; }
        public int                 WatchdogFailures  { get; init; }
        public int                 WatchdogSuccesses { get; init; }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Cache entry (internal)
    // ─────────────────────────────────────────────────────────────────

    public class CacheEntry
    {
        public object   Data           { get; set; }
        public DateTime Expiration     { get; set; }
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    }
}

