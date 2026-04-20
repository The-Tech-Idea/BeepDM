using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Resilience
{
    /// <summary>
    /// Aggregates each shard's <see cref="Proxy.IProxyCluster"/> health
    /// into a distribution-tier view used by the Phase 10 read / write
    /// executors, the <see cref="DistributedCircuitBreaker"/>, and the
    /// <see cref="ShardDownPolicy"/> enforcement pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations MUST be thread-safe: every accessor may be
    /// called concurrently by the executors while the background
    /// polling loop (or direct <c>RecordSuccess</c> / <c>RecordFailure</c>
    /// calls from the hot path) updates the state.
    /// </para>
    /// <para>
    /// The monitor never short-circuits a shard on its own. It emits
    /// <see cref="Events.ShardDownEventArgs"/> and
    /// <see cref="Events.ShardRestoredEventArgs"/> when the health
    /// classification flips, and the executors decide whether / how to
    /// route around the shard by consulting
    /// <see cref="IsShardHealthy(string)"/> and the configured
    /// <see cref="ShardDownPolicy"/>.
    /// </para>
    /// </remarks>
    public interface IShardHealthMonitor : IDisposable
    {
        /// <summary>Starts the background polling loop (idempotent).</summary>
        void Start();

        /// <summary>Stops the background polling loop (idempotent).</summary>
        void Stop();

        /// <summary>
        /// Returns the current snapshot for <paramref name="shardId"/>,
        /// or <c>null</c> when the shard is not registered.
        /// </summary>
        ShardHealthSnapshot GetSnapshot(string shardId);

        /// <summary>
        /// Snapshot of every known shard keyed by shard id. The
        /// returned dictionary is a copy — safe to enumerate without a
        /// lock.
        /// </summary>
        IReadOnlyDictionary<string, ShardHealthSnapshot> GetAllSnapshots();

        /// <summary>
        /// Fast check used on the read / write hot path. Returns
        /// <c>true</c> when the last snapshot for
        /// <paramref name="shardId"/> is marked healthy; unknown shards
        /// are treated as healthy (fail-open) so a racy registration
        /// does not spuriously fail the call.
        /// </summary>
        bool IsShardHealthy(string shardId);

        /// <summary>
        /// Fraction of currently healthy shards across the known set,
        /// <c>0.0</c> when no shard is registered. Used by the scatter
        /// gate (<see cref="DistributedDataSourceOptions"/>
        /// <c>MinimumHealthyShardRatio</c>).
        /// </summary>
        double GetHealthyShardRatio();

        /// <summary>
        /// Records a successful observation (typically from the hot
        /// path or the polling loop). Resets the consecutive failure
        /// counter and may flip the shard back to healthy, raising
        /// <see cref="Events.ShardRestoredEventArgs"/>.
        /// </summary>
        void RecordSuccess(string shardId, double latencyMs = 0);

        /// <summary>
        /// Records a failed observation. Increments the consecutive
        /// failure counter and may flip the shard to unhealthy,
        /// raising <see cref="Events.ShardDownEventArgs"/>.
        /// </summary>
        void RecordFailure(string shardId, Exception error, string reason = null);

        /// <summary>
        /// Removes <paramref name="shardId"/> from the monitored set.
        /// Called when a shard is unregistered from the distributed
        /// datasource (Phase 11 reshard).
        /// </summary>
        void Forget(string shardId);

        /// <summary>
        /// Raised whenever a shard transitions from healthy to unhealthy.
        /// </summary>
        event EventHandler<Events.ShardDownEventArgs> OnShardDown;

        /// <summary>
        /// Raised whenever a shard transitions from unhealthy to healthy.
        /// </summary>
        event EventHandler<Events.ShardRestoredEventArgs> OnShardRestored;
    }
}
