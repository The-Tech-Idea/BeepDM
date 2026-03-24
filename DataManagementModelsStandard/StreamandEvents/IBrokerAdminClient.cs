using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Consumer group models ─────────────────────────────────────────────────

    /// <summary>Brief summary of a consumer group visible from the broker.</summary>
    public sealed class ConsumerGroupSummary
    {
        public string GroupId     { get; init; }
        /// <summary>Broker-reported state, e.g. "Stable", "PreparingRebalance", "Empty".</summary>
        public string State       { get; init; }
        public int    MemberCount { get; init; }
        public IReadOnlyList<string> AssignedTopics { get; init; } = Array.Empty<string>();
    }

    /// <summary>Detailed consumer group descriptor including member assignments.</summary>
    public sealed class ConsumerGroupInfo
    {
        public string GroupId                            { get; init; }
        public string State                              { get; init; }
        public IReadOnlyList<ConsumerGroupMember> Members { get; init; } = Array.Empty<ConsumerGroupMember>();
    }

    /// <summary>One member of a consumer group with its partition assignments.</summary>
    public sealed class ConsumerGroupMember
    {
        public string MemberId   { get; init; }
        public string ClientId   { get; init; }
        public string ClientHost { get; init; }
        public IReadOnlyList<(string Topic, int Partition)> PartitionAssignments { get; init; }
            = Array.Empty<(string, int)>();
    }

    // ── Partition / cluster models ────────────────────────────────────────────

    /// <summary>Metadata for a single topic including its partitions and configuration.</summary>
    public sealed class TopicPartitionInfo
    {
        public string TopicName                                      { get; init; }
        public bool   IsCompacted                                    { get; init; }
        public int    ReplicationFactor                              { get; init; }
        public IReadOnlyList<PartitionInfo> Partitions               { get; init; } = Array.Empty<PartitionInfo>();
        public IReadOnlyDictionary<string, string> Configs           { get; init; }
            = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>Metadata for one partition.</summary>
    public sealed class PartitionInfo
    {
        public int Partition { get; init; }
        public int Leader    { get; init; }
        public IReadOnlyList<int> Replicas { get; init; } = Array.Empty<int>();
        public IReadOnlyList<int> Isr      { get; init; } = Array.Empty<int>();
    }

    /// <summary>Offset and lag snapshot for a single (topic, partition, consumer-group) triple.</summary>
    public sealed class PartitionOffsetResult
    {
        public string Topic           { get; init; }
        public int    Partition       { get; init; }
        public long   CommittedOffset { get; init; }
        public long   HighWatermark   { get; init; }
        public long   Lag             => Math.Max(0L, HighWatermark - CommittedOffset);
        public int?   LeaderEpoch     { get; init; }
    }

    /// <summary>Broker cluster descriptor.</summary>
    public sealed class ClusterInfo
    {
        public string ClusterId  { get; init; }
        public int    BrokerCount { get; init; }
        public int    ControllerId { get; init; }
        public IReadOnlyList<BrokerNodeInfo> Brokers { get; init; } = Array.Empty<BrokerNodeInfo>();
    }

    /// <summary>Details for a single broker node.</summary>
    public sealed class BrokerNodeInfo
    {
        public int    BrokerId { get; init; }
        public string Host     { get; init; }
        public int    Port     { get; init; }
        public string Rack     { get; init; }
    }

    // ── Offset reset ──────────────────────────────────────────────────────────

    /// <summary>Strategy for resetting a consumer group's offset position.</summary>
    public enum OffsetResetTarget
    {
        /// <summary>Reset to the earliest available offset on the partition.</summary>
        Earliest,

        /// <summary>Reset to the latest (end-of-log) offset.</summary>
        Latest,

        /// <summary>Reset to an exact numeric offset.</summary>
        SpecificOffset,

        /// <summary>Reset to the first offset whose timestamp is ≥ the supplied value.</summary>
        Timestamp
    }

    /// <summary>Describes an offset reset operation for a consumer group.</summary>
    public sealed class ResetOffsetsRequest
    {
        public string ConsumerGroupId         { get; init; }
        public string Topic                   { get; init; }
        /// <summary>Partitions to reset. Empty = all partitions.</summary>
        public IReadOnlyList<int> Partitions  { get; init; } = Array.Empty<int>();
        public OffsetResetTarget  Target      { get; init; }
        public long?              SpecificOffset { get; init; }
        public DateTimeOffset?    Timestamp   { get; init; }
    }

    /// <summary>Describes a change to one or more topic-level configuration keys.</summary>
    public sealed class AlterTopicConfigRequest
    {
        public string TopicName { get; init; }
        public IReadOnlyDictionary<string, string> Configs { get; init; }
            = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>Request to list offsets for specific (topic, partition) pairs by timestamp.</summary>
    public sealed class ListOffsetsRequest
    {
        public string              Topic      { get; init; }
        public IReadOnlyList<int>  Partitions { get; init; } = Array.Empty<int>();
        public DateTimeOffset      Timestamp  { get; init; }
    }

    // ── ACL models ────────────────────────────────────────────────────────────

    /// <summary>Resource type for ACL rules.</summary>
    public enum AclResourceType { Topic, Group, Cluster, TransactionalId }

    /// <summary>Access control list binding for a single resource × principal × operation.</summary>
    public sealed class AclBinding
    {
        public AclResourceType ResourceType   { get; init; }
        public string          ResourceName   { get; init; }
        public string          Principal      { get; init; }
        public string          Operation      { get; init; }  // e.g. "Read", "Write", "All"
        public string          PermissionType { get; init; }  // "Allow" | "Deny"
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Broker-neutral admin interface for consumer group introspection, offset management,
    /// cluster metadata, topic config changes, and ACL administration.
    /// <para>
    /// Implementations live in broker-specific adapter assemblies (e.g. a Kafka adapter wrapping
    /// <c>Confluent.Kafka.Admin.IAdminClient</c>).  This interface carries no broker SDK references.
    /// </para>
    /// </summary>
    public interface IBrokerAdminClient
    {
        // ── Consumer group ────────────────────────────────────────────────────

        /// <summary>Lists all consumer groups visible to the broker.</summary>
        Task<IReadOnlyList<ConsumerGroupSummary>> ListConsumerGroupsAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns detailed metadata for a specific consumer group.</summary>
        Task<ConsumerGroupInfo> DescribeConsumerGroupAsync(string groupId, CancellationToken cancellationToken = default);

        /// <summary>Returns current committed offsets and high-water marks for a group on a topic.</summary>
        Task<IReadOnlyList<PartitionOffsetResult>> GetCommittedOffsetsAsync(
            string groupId, string topic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets consumer group offsets according to <paramref name="request"/>.
        /// The consumer group must be inactive (all members stopped) before calling.
        /// </summary>
        Task ResetOffsetsAsync(ResetOffsetsRequest request, CancellationToken cancellationToken = default);

        // ── Topic / cluster ───────────────────────────────────────────────────

        /// <summary>Returns partition and config metadata for a topic.</summary>
        Task<TopicPartitionInfo> DescribeTopicAsync(string topicName, CancellationToken cancellationToken = default);

        /// <summary>Returns cluster-level metadata (broker count, controller, etc.).</summary>
        Task<ClusterInfo> DescribeClusterAsync(CancellationToken cancellationToken = default);

        /// <summary>Updates configuration keys for a topic without recreating it.</summary>
        Task AlterTopicConfigAsync(AlterTopicConfigRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists offsets for the given (topic, partition) pairs at the requested timestamp.
        /// Useful for time-travel debugging.
        /// </summary>
        Task<IReadOnlyList<PartitionOffsetResult>> ListOffsetsForTimestampAsync(
            ListOffsetsRequest request, CancellationToken cancellationToken = default);

        // ── ACL ───────────────────────────────────────────────────────────────

        /// <summary>Creates an ACL rule on the broker.</summary>
        Task CreateAclAsync(AclBinding binding, CancellationToken cancellationToken = default);

        /// <summary>Deletes an ACL rule from the broker.</summary>
        Task DeleteAclAsync(AclBinding binding, CancellationToken cancellationToken = default);
    }
}
