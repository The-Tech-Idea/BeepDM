using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── State enum ────────────────────────────────────────────────────────────

    /// <summary>Current leadership role of this coordinator instance for a named resource.</summary>
    public enum LeadershipState
    {
        /// <summary>Leadership status has not yet been determined.</summary>
        Unknown,

        /// <summary>This instance is the active leader for the resource.</summary>
        Leading,

        /// <summary>Another instance holds leadership; this instance is a standby follower.</summary>
        Following
    }

    // ── Event args ────────────────────────────────────────────────────────────

    /// <summary>Published whenever leadership changes for a watched resource.</summary>
    public sealed class LeadershipChangedEventArgs
    {
        /// <summary>Leadership state before the change.</summary>
        public LeadershipState PreviousState { get; init; }

        /// <summary>Leadership state after the change.</summary>
        public LeadershipState NewState { get; init; }

        /// <summary>Instance ID of the new leader (<c>null</c> when leadership is vacated).</summary>
        public string LeaderId { get; init; }

        /// <summary>Name of the resource whose leadership changed.</summary>
        public string ResourceName { get; init; }

        /// <summary>UTC time the change was observed.</summary>
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Lease-based leader election for a named resource.
    /// Typically backed by an <see cref="IDistributedLock"/> at the infrastructure level.
    /// </summary>
    public interface ILeaderElection
    {
        /// <summary>
        /// Attempts to become the leader for <paramref name="resourceName"/>.
        /// Returns <c>true</c> when leadership is acquired; <c>false</c> if another instance leads.
        /// </summary>
        Task<bool> TryBecomeLeaderAsync(
            string resourceName,
            string instanceId,
            TimeSpan ttl,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Voluntarily surrenders leadership for <paramref name="resourceName"/>.
        /// No-op when this instance is not the current leader.
        /// </summary>
        Task ResignAsync(
            string resourceName,
            string instanceId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the instance ID of the current leader, or <c>null</c> when no leader exists.
        /// </summary>
        Task<string> GetCurrentLeaderAsync(
            string resourceName,
            CancellationToken cancellationToken = default);

        /// <summary>Returns <c>true</c> when <paramref name="instanceId"/> currently holds leadership.</summary>
        Task<bool> IsLeaderAsync(
            string resourceName,
            string instanceId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams <see cref="LeadershipChangedEventArgs"/> whenever leadership changes for
        /// <paramref name="resourceName"/>. Multiple concurrent callers each receive all events.
        /// Completes when <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        IAsyncEnumerable<LeadershipChangedEventArgs> WatchLeadershipAsync(
            string resourceName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Extends the leadership lease TTL. Returns <c>false</c> if this instance is no longer the leader.
        /// </summary>
        Task<bool> RenewLeadershipAsync(
            string resourceName,
            string instanceId,
            TimeSpan ttl,
            CancellationToken cancellationToken = default);
    }
}
