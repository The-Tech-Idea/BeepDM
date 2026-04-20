using System;
using TheTechIdea.Beep.Distributed.Audit;
using TheTechIdea.Beep.Distributed.Execution;
using TheTechIdea.Beep.Distributed.Observability;
using TheTechIdea.Beep.Distributed.Performance;
using TheTechIdea.Beep.Distributed.Placement;
using TheTechIdea.Beep.Distributed.Resilience;
using TheTechIdea.Beep.Distributed.Schema;
using TheTechIdea.Beep.Distributed.Security;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// Tuning knobs for a <see cref="DistributedDataSource"/> instance.
    /// Phase 01 ships only the timeout / parallelism / disposal
    /// defaults. Phase 06 adds <c>MaxScatterParallelism</c> and
    /// <c>ScatterFailurePolicy</c>; Phase 07 adds
    /// <c>MaxFanOutParallelism</c>; Phase 10 adds
    /// <c>MinimumHealthyShardRatio</c>; Phase 14 adds the
    /// performance/back-pressure block.
    /// </summary>
    /// <remarks>
    /// All fields are public settable POCO properties so the options
    /// instance can be loaded from <c>ConfigEditor</c> JSON without
    /// custom converters. New fields must be appended; consumers must
    /// tolerate unknown values to keep persisted configs forward
    /// compatible.
    /// </remarks>
    public sealed class DistributedDataSourceOptions
    {
        /// <summary>
        /// Default per-shard timeout applied to read and write
        /// operations when the caller does not specify one. A value of
        /// <c>0</c> disables the per-shard timeout.
        /// </summary>
        public int DefaultPerShardTimeoutMs { get; set; } = 30_000;

        /// <summary>
        /// Total deadline applied to a distributed read that scatters
        /// across multiple shards (Phase 06). A value of <c>0</c>
        /// disables the deadline.
        /// </summary>
        public int DefaultReadDeadlineMs { get; set; } = 60_000;

        /// <summary>
        /// Maximum degree of parallelism for scatter reads. Phase 06
        /// honours this cap when issuing per-shard reads.
        /// </summary>
        public int MaxScatterParallelism { get; set; } = Math.Max(2, Environment.ProcessorCount);

        /// <summary>
        /// Maximum degree of parallelism for replicated / broadcast
        /// write fan-out. Phase 07 honours this cap.
        /// </summary>
        public int MaxFanOutParallelism { get; set; } = Math.Max(2, Environment.ProcessorCount);

        /// <summary>
        /// Timeout applied to <see cref="System.IDisposable.Dispose"/>
        /// while waiting for in-flight operations on each shard to
        /// drain. Defaults to 5 seconds.
        /// </summary>
        public int DisposeDrainTimeoutMs { get; set; } = 5_000;

        /// <summary>
        /// When <c>true</c> (default) the distributed datasource opens
        /// every shard cluster eagerly during
        /// <see cref="DistributedDataSource.Openconnection"/>. When
        /// <c>false</c> shards are opened lazily on first use.
        /// </summary>
        public bool EagerOpenShards { get; set; } = true;

        // ── Phase 03: Entity placement / unmapped routing ─────────────────

        /// <summary>
        /// Behaviour when the active <see cref="DistributionPlan"/>
        /// contains no placement matching an entity name. Defaults to
        /// <see cref="UnmappedEntityPolicy.RejectUnmapped"/> so missing
        /// placements fail loudly during development.
        /// </summary>
        public UnmappedEntityPolicy UnmappedPolicy { get; set; }
            = UnmappedEntityPolicy.RejectUnmapped;

        /// <summary>
        /// Shard id used by
        /// <see cref="UnmappedEntityPolicy.DefaultShardId"/>. Required
        /// when <see cref="UnmappedPolicy"/> is set to that value;
        /// ignored otherwise. Validated when the resolver is built.
        /// </summary>
        public string DefaultShardIdForUnmapped { get; set; }

        // ── Phase 05: Shard routing ───────────────────────────────────────

        /// <summary>
        /// When <c>false</c> (default) a write against a
        /// <see cref="Plan.DistributionMode.Sharded"/> entity that
        /// does not supply a partition-key value is rejected via
        /// <see cref="Routing.ShardRoutingException"/>. Set to
        /// <c>true</c> to allow scatter writes (fan out the same
        /// payload to every live shard); useful for reference / lookup
        /// tables that happen to live in a sharded family.
        /// </summary>
        public bool AllowScatterWrite { get; set; } = false;

        // ── Phase 06: Read execution ─────────────────────────────────────

        /// <summary>
        /// Policy applied when one or more shards in a scatter read
        /// fail or time out. Defaults to
        /// <see cref="ScatterFailurePolicy.BestEffort"/> so a single
        /// slow shard does not take down the whole query; flip to
        /// <see cref="ScatterFailurePolicy.FailFast"/> or
        /// <see cref="ScatterFailurePolicy.RequireAll"/> for strict
        /// reads. Consumed by the Phase 06 read executor.
        /// </summary>
        public ScatterFailurePolicy ScatterFailurePolicy { get; set; }
            = ScatterFailurePolicy.BestEffort;

        /// <summary>
        /// Policy used by the Phase 06 read executor to pick a shard
        /// for a <see cref="Plan.DistributionMode.Replicated"/> read
        /// that resolves to more than one live shard. Defaults to
        /// <see cref="ReplicatedReadPolicy.First"/> (the first id in
        /// the placement's ordered list) which keeps reads
        /// deterministic; flip to
        /// <see cref="ReplicatedReadPolicy.Random"/> to spread load.
        /// </summary>
        public ReplicatedReadPolicy ReplicatedReadPolicy { get; set; }
            = ReplicatedReadPolicy.First;

        // ── Phase 09: Distributed transactions ───────────────────────────

        /// <summary>
        /// When <c>true</c>, the Phase 09
        /// <see cref="Transactions.TransactionDecisionResolver"/> always
        /// picks <see cref="Transactions.TransactionStrategy.Saga"/>
        /// for multi-shard work, even when every shard advertises
        /// <see cref="Proxy.IProxyCluster.SupportsTwoPhaseCommit"/>.
        /// Defaults to <c>false</c> (prefer 2PC when available).
        /// </summary>
        public bool PreferSagaOverTwoPhaseCommit { get; set; } = false;

        /// <summary>
        /// Total deadline applied to a distributed transaction scope
        /// from <c>BeginTransaction</c> through <c>Commit</c> /
        /// <c>Rollback</c>. V1 surfaces the value for observability
        /// (logged at scope open); Phase 13 enforces it via
        /// cancellation. A value of <c>0</c> disables the deadline.
        /// </summary>
        public int TransactionDeadlineMs { get; set; } = 60_000;

        /// <summary>
        /// When <c>true</c> (default) the coordinator keeps an
        /// in-memory transaction log for observability. Disable only
        /// for tests that need to compare memory allocation
        /// baselines — durability is a Phase 13 feature either way.
        /// </summary>
        public bool EnableInMemoryTransactionLog { get; set; } = true;

        // ── Phase 10: Resilience & shard-down policy ─────────────────────

        /// <summary>
        /// Per-mode <see cref="ShardDownPolicy"/> configuration plus
        /// the distributed circuit-breaker knobs. Never <c>null</c>;
        /// replaced atomically via <see cref="DistributedDataSource.ResilienceOptions"/>
        /// at runtime.
        /// </summary>
        public ShardDownPolicyOptions ShardDownPolicy { get; set; }
            = new ShardDownPolicyOptions();

        /// <summary>
        /// Minimum ratio of healthy shards required to execute a
        /// scatter read or broadcast write. When the observed ratio
        /// falls below this threshold the executor raises
        /// <see cref="DistributedDataSource.OnDegradedMode"/> and
        /// throws <see cref="DegradedShardSetException"/> instead of
        /// returning partial data. Defaults to <c>0.5</c>; a value of
        /// <c>0</c> disables the gate entirely.
        /// </summary>
        public double MinimumHealthyShardRatio { get; set; } = 0.5;

        /// <summary>
        /// Poll interval for the background
        /// <see cref="ShardHealthMonitor"/>. A value of <c>0</c>
        /// disables polling (the monitor still tracks hot-path
        /// successes and failures). Defaults to 5 seconds.
        /// </summary>
        public int HealthMonitorIntervalMs { get; set; } = 5_000;

        /// <summary>
        /// When <c>true</c> (default) the distributed datasource
        /// starts the <see cref="ShardHealthMonitor"/> in
        /// <see cref="DistributedDataSource.Openconnection"/> and
        /// disposes it in
        /// <see cref="DistributedDataSource.Dispose"/>. Disable for
        /// tests that drive health updates manually.
        /// </summary>
        public bool EnableHealthMonitor { get; set; } = true;

        // ── Phase 12: Schema management & DDL broadcast ─────────────────

        /// <summary>
        /// Policy applied by
        /// <see cref="Schema.IDistributedSchemaService.CreateEntityAsync"/>
        /// when a <see cref="Plan.DistributionMode.Sharded"/> entity
        /// carries a database-generated identity column. Defaults to
        /// <see cref="IdentityColumnPolicy.WarnOnly"/> so existing
        /// schemas keep working; switch to
        /// <see cref="IdentityColumnPolicy.RejectShardedIdentity"/>
        /// once a distributed sequence provider is configured.
        /// </summary>
        public IdentityColumnPolicy IdentityColumnPolicy { get; set; }
            = IdentityColumnPolicy.WarnOnly;

        /// <summary>
        /// Optional <see cref="IDistributedSequenceProvider"/> used by
        /// Phase 07 sharded inserts to generate collision-free ids
        /// when the entity's primary key would otherwise rely on
        /// per-shard identity. <c>null</c> leaves id generation to
        /// the shard provider (the default).
        /// </summary>
        public IDistributedSequenceProvider SequenceProvider { get; set; }

        // ── Phase 13: Observability, audit, security ──────────────────────

        /// <summary>
        /// When <c>true</c> (default) the Phase 13 metrics
        /// aggregator is installed and executors record per-request
        /// samples; disable for tests that need byte-for-byte
        /// allocation baselines.
        /// </summary>
        public bool EnableDistributedMetrics { get; set; } = true;

        /// <summary>
        /// Optional custom metrics aggregator. When <c>null</c> a
        /// default <see cref="DistributedMetricsAggregator"/> is
        /// constructed against the live shard map.
        /// </summary>
        public IDistributedMetricsAggregator MetricsAggregator { get; set; }

        /// <summary>
        /// p95 latency (ms) above which a shard is flagged hot.
        /// Defaults to 500 ms; set <c>0</c> to disable hot-shard
        /// detection (the detector still tracks samples).
        /// </summary>
        public double HotShardP95ThresholdMs { get; set; } = 500.0;

        /// <summary>
        /// Number of consecutive windows above the p95 threshold
        /// required before <c>OnHotShardDetected</c> fires.
        /// Defaults to 3.
        /// </summary>
        public int HotShardConsecutiveWindows { get; set; } = 3;

        /// <summary>
        /// Requests-per-second threshold that trips
        /// <c>OnHotEntityDetected</c>; defaults to 500.
        /// </summary>
        public double HotEntityThresholdRps { get; set; } = 500.0;

        /// <summary>
        /// Audit sink wired to every distribution-tier decision
        /// point. Defaults to <see cref="NullDistributedAuditSink.Instance"/>;
        /// swap in <see cref="FileDistributedAuditSink"/> (or a
        /// bridge to <see cref="Proxy.IProxyAuditSink"/>) for
        /// production.
        /// </summary>
        public IDistributedAuditSink AuditSink { get; set; }

        /// <summary>
        /// When <c>true</c> (default) audit events carry redacted
        /// partition keys and messages; disable only when writing
        /// to an already-trusted sink.
        /// </summary>
        public bool RedactAuditFields { get; set; } = true;

        /// <summary>
        /// Optional access policy gating every read/write/DDL
        /// call. Defaults to <see cref="AllowAllAccessPolicy.Instance"/>
        /// so existing deployments keep working.
        /// </summary>
        public IDistributedAccessPolicy AccessPolicy { get; set; }

        /// <summary>
        /// Optional directory for the durable
        /// <see cref="Transactions.FileTransactionLog"/>. When
        /// <c>null</c> the coordinator keeps the Phase 09 in-memory
        /// log (subject to <see cref="EnableInMemoryTransactionLog"/>).
        /// </summary>
        public string DurableTransactionLogDirectory { get; set; }

        // ── Phase 14: Performance & capacity engineering ──────────────────

        /// <summary>
        /// Phase 14 back-pressure and capacity controls. Never
        /// <c>null</c>; the default instance preserves existing
        /// Phase 06 / 07 behaviour with generous caps.
        /// </summary>
        public PerformanceOptions Performance { get; set; } = new PerformanceOptions();

        /// <summary>
        /// Optional external rate limiter. When <c>null</c> the
        /// <see cref="DistributedDataSource"/> builds a
        /// <see cref="TokenBucketRateLimiter"/> from
        /// <see cref="PerformanceOptions.ShardRateLimitPerSecond"/>.
        /// </summary>
        public IDistributedRateLimiter RateLimiter { get; set; }
    }
}
