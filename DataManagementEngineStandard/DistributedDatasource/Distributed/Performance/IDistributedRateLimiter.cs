using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Distributed.Performance
{
    /// <summary>
    /// Per-shard rate limiter consulted by the read / write executors
    /// before dispatching a call. Implementations cap throughput and
    /// surface <see cref="BackpressureException"/> when the caller
    /// cannot be admitted within the supplied wait budget.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The limiter layers on top of <see cref="Proxy.ProxyCluster"/>'s
    /// own capacity controls. When both are configured the distributed
    /// limiter runs first so hot-path callers see a single, coherent
    /// <see cref="BackpressureException"/> instead of two stacked
    /// guards.
    /// </para>
    /// <para>
    /// Implementations must be safe to call from many threads
    /// simultaneously. State (e.g. token counters) is keyed by
    /// <paramref name="shardId"/> so a misbehaving shard cannot
    /// starve healthy neighbours.
    /// </para>
    /// </remarks>
    public interface IDistributedRateLimiter
    {
        /// <summary>
        /// Attempts to acquire one permit for <paramref name="shardId"/>
        /// without waiting. Returns <c>true</c> when the permit was
        /// granted; <c>false</c> when the limiter is at capacity.
        /// </summary>
        bool TryAcquire(string shardId);

        /// <summary>
        /// Asynchronously acquires one permit for
        /// <paramref name="shardId"/>, throwing
        /// <see cref="BackpressureException"/> when the wait budget
        /// expires before a permit becomes available.
        /// </summary>
        /// <param name="shardId">Shard identifier.</param>
        /// <param name="wait">Maximum time to wait for a permit. <see cref="TimeSpan.Zero"/> = try-only.</param>
        /// <param name="cancellationToken">Optional cancellation.</param>
        Task AcquireAsync(
            string             shardId,
            TimeSpan           wait,
            CancellationToken  cancellationToken = default);

        /// <summary>
        /// Replaces the per-shard configuration at runtime. Implementations
        /// should avoid re-allocating existing buckets so in-flight
        /// callers are unaffected.
        /// </summary>
        void Configure(double ratePerSecond, int burst);
    }
}
