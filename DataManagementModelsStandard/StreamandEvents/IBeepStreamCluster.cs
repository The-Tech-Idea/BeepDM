using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ══════════════════════════════════════════════════════════════════════════
    // Enums
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Lifecycle state of a streaming node.</summary>
    public enum NodeStatus
    {
        Created,
        Starting,
        Running,
        Stopping,
        Stopped,
        Faulted,
        Suspect,
        Dead
    }

    /// <summary>Cluster-level status.</summary>
    public enum ClusterStatus
    {
        Offline,
        Initializing,
        Running,
        Degraded,
        Rebalancing,
        ShuttingDown,
        Stopped,
        Error
    }

    /// <summary>Role a node plays in the cluster.</summary>
    public enum NodeRole
    {
        /// <summary>Cluster brain: metadata, partition assignment, rebalance decisions.</summary>
        Controller,
        /// <summary>Data serving: stores and serves messages for assigned partitions.</summary>
        Broker,
        /// <summary>Read-only replica for geo-read or analytics workloads.</summary>
        Observer,
        /// <summary>Combined controller + broker (common in small clusters).</summary>
        ControllerBroker
    }

    /// <summary>How many replicas must acknowledge a write before it is considered committed.</summary>
    public enum AcksMode
    {
        /// <summary>Fire-and-forget. No durability guarantee.</summary>
        None,
        /// <summary>Leader acknowledges. Fast but risks data loss on leader failure.</summary>
        Leader,
        /// <summary>All in-sync replicas acknowledge. Strongest durability guarantee.</summary>
        All
    }

    /// <summary>Strategy for assigning partitions to consumers within a group.</summary>
    public enum AssignmentStrategy
    {
        /// <summary>Assign contiguous partition ranges per topic to consumers.</summary>
        Range,
        /// <summary>Round-robin all topic-partitions across consumers.</summary>
        RoundRobin,
        /// <summary>Minimize partition movement on rebalance.</summary>
        Sticky,
        /// <summary>Incremental cooperative rebalance — only moved partitions are revoked.</summary>
        CooperativeSticky
    }

    /// <summary>Why a rebalance was triggered.</summary>
    public enum RebalanceReason
    {
        NodeJoined,
        NodeLeft,
        NodeFailed,
        TopicCreated,
        TopicDeleted,
        Manual
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Configuration DTOs
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Configuration for a single <see cref="IBeepStreamNode"/>.</summary>
    public sealed class BeepStreamNodeConfig
    {
        /// <summary>Unique node identifier. Auto-generated if null.</summary>
        public string NodeId { get; init; }

        /// <summary>Role this node plays in the cluster.</summary>
        public NodeRole Role { get; init; } = NodeRole.Broker;

        /// <summary>Default partition count for newly-created topics.</summary>
        public int DefaultPartitionCount { get; init; } = 4;

        /// <summary>Bounded channel capacity per partition (backpressure threshold).</summary>
        public int ChannelCapacity { get; init; } = 10_000;

        /// <summary>How long the node waits for inflight messages during graceful shutdown.</summary>
        public TimeSpan ShutdownDrainTimeout { get; init; } = TimeSpan.FromSeconds(15);

        /// <summary>Interval between automatic retention sweeps. Null = disabled.</summary>
        public TimeSpan? RetentionSweepInterval { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>Default topic storage configuration applied when none is supplied at topic creation.</summary>
        public TopicStorageConfig DefaultTopicConfig { get; init; } = TopicStorageConfig.Default;
    }

    /// <summary>Configuration for a <see cref="IBeepStreamCluster"/>.</summary>
    public sealed class BeepStreamClusterConfig
    {
        /// <summary>Unique cluster identifier. Auto-generated if null.</summary>
        public string ClusterId { get; init; }

        /// <summary>Number of nodes that should store a copy of each message for durability.</summary>
        public int ReplicationFactor { get; init; } = 1;

        /// <summary>Initial node configurations. More nodes can be added at runtime.</summary>
        public IReadOnlyList<BeepStreamNodeConfig> InitialNodes { get; init; } = Array.Empty<BeepStreamNodeConfig>();

        /// <summary>How often the watchdog probes node health.</summary>
        public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>Time without a heartbeat before a node becomes suspect.</summary>
        public TimeSpan SuspectThreshold { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>Time without a heartbeat before a node is declared dead.</summary>
        public TimeSpan NodeDeadThreshold { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>Minimum number of nodes required for cluster operation.</summary>
        public int MinNodes { get; init; } = 1;

        /// <summary>Enable automatic failover when a node dies.</summary>
        public bool EnableAutoFailover { get; init; } = true;

        /// <summary>Replication settings for the cluster.</summary>
        public ReplicationConfig Replication { get; init; } = new ReplicationConfig();

        /// <summary>Default strategy for consumer partition assignment.</summary>
        public AssignmentStrategy DefaultAssignmentStrategy { get; init; } = AssignmentStrategy.RoundRobin;

        /// <summary>Consumer session timeout — consumer is considered dead if no heartbeat within this period.</summary>
        public TimeSpan SessionTimeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>Interval for checking consumer heartbeats within consumer groups.</summary>
        public TimeSpan ConsumerHeartbeatInterval { get; init; } = TimeSpan.FromSeconds(3);

        /// <summary>Maximum time to wait for all members to join during a rebalance.</summary>
        public TimeSpan RebalanceTimeout { get; init; } = TimeSpan.FromSeconds(60);

        /// <summary>Use cooperative (incremental) rebalance instead of eager (stop-the-world).</summary>
        public bool CooperativeRebalanceEnabled { get; init; } = false;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Health / Telemetry DTOs
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Point-in-time health snapshot for a single node.</summary>
    public sealed class NodeHealthSnapshot
    {
        public string NodeId { get; init; }
        public NodeStatus Status { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public TimeSpan Uptime { get; init; }
        public int TopicCount { get; init; }
        public long TotalMessagesIn { get; init; }
        public long TotalMessagesOut { get; init; }
        public long PendingMessages { get; init; }
        public long StoredBytes { get; init; }
        public int ActiveConsumerGroups { get; init; }

        /// <summary>Optional error message if the node is faulted.</summary>
        public string ErrorMessage { get; init; }
    }

    /// <summary>Cluster-level health aggregate.</summary>
    public sealed class ClusterHealthSnapshot
    {
        public string ClusterId { get; init; }
        public ClusterStatus Status { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public int TotalNodes { get; init; }
        public int HealthyNodes { get; init; }
        public int FaultedNodes { get; init; }
        public IReadOnlyList<NodeHealthSnapshot> NodeSnapshots { get; init; } = Array.Empty<NodeHealthSnapshot>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Event Args
    // ══════════════════════════════════════════════════════════════════════════

    public sealed class NodeStatusChangedEventArgs : EventArgs
    {
        public string NodeId { get; init; }
        public NodeStatus PreviousStatus { get; init; }
        public NodeStatus CurrentStatus { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public sealed class ClusterTopologyChangedEventArgs : EventArgs
    {
        public string ClusterId { get; init; }
        public string Reason { get; init; }
        public IReadOnlyList<string> AddedNodeIds { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> RemovedNodeIds { get; init; } = Array.Empty<string>();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public sealed class NodeHealthChangedEventArgs : EventArgs
    {
        public string NodeId { get; init; }
        public NodeHealthSnapshot Snapshot { get; init; }
        public bool WasPreviouslyHealthy { get; init; }
    }

    public sealed class WatchdogAlertEventArgs : EventArgs
    {
        public string AlertId { get; init; }
        public WatchdogAlertSeverity Severity { get; init; }
        public string Message { get; init; }
        public string NodeId { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public enum WatchdogAlertSeverity
    {
        Info,
        Warning,
        Critical
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Cluster DTOs
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Describes which node is the leader and which are replicas for one topic-partition.</summary>
    public sealed class ClusterPartitionAssignment
    {
        public string Topic { get; init; }
        public int Partition { get; init; }
        public string LeaderNodeId { get; init; }
        public IReadOnlyList<string> ReplicaNodeIds { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> IsrNodeIds { get; init; } = Array.Empty<string>();
    }

    /// <summary>Point-in-time snapshot of the cluster's metadata.</summary>
    public sealed class ClusterMetadata
    {
        public string ClusterId { get; init; }
        public string ControllerNodeId { get; init; }
        public IReadOnlyList<ClusterPartitionAssignment> Assignments { get; init; } = Array.Empty<ClusterPartitionAssignment>();
        public IReadOnlyDictionary<string, NodeRole> NodeRoles { get; init; }
        public int MetadataVersion { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>Replication settings for the cluster.</summary>
    public sealed class ReplicationConfig
    {
        public int ReplicationFactor { get; init; } = 1;
        public int MinInSyncReplicas { get; init; } = 1;
        public AcksMode DefaultAcks { get; init; } = AcksMode.Leader;
        public bool AllowUncleanLeaderElection { get; init; } = false;
        public TimeSpan ReplicaLagMaxTime { get; init; } = TimeSpan.FromSeconds(10);
        public int ReplicaFetchMaxBytes { get; init; } = 1_048_576;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Interfaces
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A single streaming node that wraps an <c>EventStreamingCoordinator</c>.
    /// The coordinator handles all pub/sub, interceptors, and pipeline logic.
    /// The node adds cluster-awareness: role, partition assignments, and health.
    /// </summary>
    public interface IBeepStreamNode : IAsyncDisposable
    {
        /// <summary>Unique identifier for this node within the cluster.</summary>
        string NodeId { get; }

        /// <summary>Current lifecycle state.</summary>
        NodeStatus Status { get; }

        /// <summary>Role this node plays in the cluster.</summary>
        NodeRole Role { get; }

        /// <summary>Configuration snapshot.</summary>
        BeepStreamNodeConfig Config { get; }

        /// <summary>Assigned partition ranges (topic → partition list).</summary>
        IReadOnlyDictionary<string, IReadOnlyList<int>> AssignedPartitions { get; }

        /// <summary>Starts the node and its underlying coordinator.</summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>Graceful shutdown: stops the coordinator and drains inflight messages.</summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>Captures a point-in-time health report.</summary>
        NodeHealthSnapshot GetHealth();

        /// <summary>Fired whenever the node transitions between lifecycle states.</summary>
        event EventHandler<NodeStatusChangedEventArgs> StatusChanged;
    }

    /// <summary>
    /// Manages a cluster of <see cref="IBeepStreamNode"/> instances. Handles partitioning
    /// across nodes, replication, failover, and consumer group rebalancing.
    /// Routes publish/consume operations to the correct partition leader nodes.
    /// </summary>
    public interface IBeepStreamCluster : IAsyncDisposable
    {
        /// <summary>Unique cluster identifier.</summary>
        string ClusterId { get; }

        /// <summary>Cluster-level lifecycle state.</summary>
        ClusterStatus Status { get; }

        /// <summary>Current node roster.</summary>
        IReadOnlyList<IBeepStreamNode> Nodes { get; }

        /// <summary>Configuration snapshot.</summary>
        BeepStreamClusterConfig Config { get; }

        /// <summary>Starts all registered nodes and enables the cluster.</summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>Graceful cluster-wide shutdown.</summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>Adds a new node to the running cluster. Triggers rebalancing.</summary>
        Task<IBeepStreamNode> AddNodeAsync(BeepStreamNodeConfig config, CancellationToken cancellationToken = default);

        /// <summary>Removes a node from the cluster. Triggers rebalancing.</summary>
        Task RemoveNodeAsync(string nodeId, CancellationToken cancellationToken = default);

        /// <summary>Forces a partition rebalance across current nodes.</summary>
        Task RebalanceAsync(CancellationToken cancellationToken = default);

        /// <summary>Captures cluster-wide health from all nodes.</summary>
        ClusterHealthSnapshot GetHealth();

        /// <summary>Publish through the cluster — routes to the correct partition leader node.</summary>
        Task<PublishResult> PublishAsync<TPayload>(EventEnvelope<TPayload> envelope, CancellationToken cancellationToken = default);

        /// <summary>Consume through the cluster — merges from partition leader nodes.</summary>
        IAsyncEnumerable<ReceivedEvent<TPayload>> ConsumeAsync<TPayload>(string topic, string consumerGroup, StreamRetryPolicy retryPolicy = null, CancellationToken cancellationToken = default);

        /// <summary>Cluster metadata snapshot.</summary>
        ClusterMetadata GetMetadata();

        /// <summary>Fired when nodes are added, removed, or fail.</summary>
        event EventHandler<ClusterTopologyChangedEventArgs> TopologyChanged;
    }

    /// <summary>
    /// Background health monitor for a <see cref="IBeepStreamCluster"/>.
    /// Periodically probes nodes, detects failures, and triggers auto-recovery or failover.
    /// </summary>
    public interface IStreamWatchdog : IAsyncDisposable
    {
        /// <summary>True when the watchdog is actively monitoring.</summary>
        bool IsRunning { get; }

        /// <summary>Starts monitoring the given cluster.</summary>
        Task StartAsync(IBeepStreamCluster cluster, CancellationToken cancellationToken = default);

        /// <summary>Stops monitoring.</summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns the latest health snapshot per node.</summary>
        IReadOnlyDictionary<string, NodeHealthSnapshot> GetAllHealth();

        /// <summary>Fired when a node's health changes (e.g. Running → Faulted).</summary>
        event EventHandler<NodeHealthChangedEventArgs> NodeHealthChanged;

        /// <summary>Fired for any alert (info, warning, critical).</summary>
        event EventHandler<WatchdogAlertEventArgs> AlertRaised;
    }
}
