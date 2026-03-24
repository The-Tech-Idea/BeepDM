using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Result ────────────────────────────────────────────────────────────────

    /// <summary>Result of a single <see cref="IDistributedLock.TryAcquireAsync"/> call.</summary>
    public sealed class LockAcquireResult
    {
        /// <summary>True when the caller now holds the lock.</summary>
        public bool Acquired { get; init; }

        /// <summary>
        /// Opaque fencing token issued at acquire time.
        /// Must be supplied on <see cref="IDistributedLock.ReleaseAsync"/> and
        /// <see cref="IDistributedLock.RenewAsync"/> to prevent stale-owner operations.
        /// </summary>
        public string LockToken { get; init; }

        /// <summary>Instance ID of the lock owner at the time of the call.</summary>
        public string OwnerId { get; init; }

        /// <summary>UTC time the lock was acquired (null when <see cref="Acquired"/> is false).</summary>
        public DateTimeOffset? AcquiredAt { get; init; }

        /// <summary>UTC time after which the lock expires if not renewed (null when not acquired).</summary>
        public DateTimeOffset? ExpiresAt { get; init; }

        public static LockAcquireResult Success(string token, string ownerId, DateTimeOffset expiresAt) =>
            new() { Acquired = true, LockToken = token, OwnerId = ownerId, AcquiredAt = DateTimeOffset.UtcNow, ExpiresAt = expiresAt };

        public static LockAcquireResult NotAcquired(string currentOwner = null) =>
            new() { Acquired = false, OwnerId = currentOwner };
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Named, TTL-based distributed mutual exclusion.
    /// All implementations must be safe for concurrent calls from multiple threads and instances.
    /// </summary>
    public interface IDistributedLock
    {
        /// <summary>
        /// Attempts to atomically acquire a named lock.
        /// Returns <see cref="LockAcquireResult.Acquired"/> = false immediately if the lock is held.
        /// </summary>
        Task<LockAcquireResult> TryAcquireAsync(
            string lockName,
            TimeSpan ttl,
            string ownerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases a named lock.
        /// No-op if <paramref name="lockToken"/> does not match the current holder's token
        /// (prevents a late/crashed owner from releasing a lock already re-acquired by another instance).
        /// </summary>
        Task ReleaseAsync(
            string lockName,
            string lockToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Extends the TTL of a held lock.
        /// Returns <c>false</c> if the lock has expired or the <paramref name="lockToken"/> does not match.
        /// </summary>
        Task<bool> RenewAsync(
            string lockName,
            string lockToken,
            TimeSpan ttl,
            CancellationToken cancellationToken = default);

        /// <summary>Returns true if the named lock is currently held by <paramref name="ownerId"/>.</summary>
        Task<bool> IsHeldByAsync(
            string lockName,
            string ownerId,
            CancellationToken cancellationToken = default);

        /// <summary>Returns the <c>ownerId</c> of the current lock holder, or <c>null</c> if unlocked.</summary>
        Task<string> GetOwnerAsync(
            string lockName,
            CancellationToken cancellationToken = default);
    }

    // ── RAII handle ───────────────────────────────────────────────────────────

    /// <summary>
    /// Auto-releasing, auto-renewing lock scope.
    /// Dispose releases the underlying lock. Use inside <c>await using</c>.
    /// </summary>
    public sealed class DistributedLockHandle : IAsyncDisposable
    {
        private readonly IDistributedLock _lock;
        private readonly string _lockName;
        private readonly string _lockToken;
        private readonly CancellationTokenSource _renewalCts;
        private bool _released;

        private DistributedLockHandle(
            IDistributedLock @lock,
            string lockName,
            string lockToken,
            TimeSpan renewalInterval)
        {
            _lock      = @lock;
            _lockName  = lockName;
            _lockToken = lockToken;
            _renewalCts = new CancellationTokenSource();

            // Background auto-renewal
            _ = RenewLoopAsync(renewalInterval, _renewalCts.Token);
        }

        private async Task RenewLoopAsync(TimeSpan interval, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(interval, ct).ConfigureAwait(false);
                    if (!await _lock.RenewAsync(_lockName, _lockToken, interval * 3, ct).ConfigureAwait(false))
                        break; // lock expired or stolen — stop renewal silently
                }
            }
            catch (OperationCanceledException) { /* expected on dispose */ }
        }

        /// <summary>
        /// Attempts to acquire a named lock, returning a handle on success or <c>null</c> if not acquired.
        /// The handle automatically renews the lock at <paramref name="renewalInterval"/> intervals.
        /// </summary>
        public static async Task<DistributedLockHandle> AcquireAsync(
            IDistributedLock distributedLock,
            string lockName,
            TimeSpan ttl,
            string ownerId,
            TimeSpan? renewalInterval = null,
            CancellationToken cancellationToken = default)
        {
            var result = await distributedLock.TryAcquireAsync(lockName, ttl, ownerId, cancellationToken)
                .ConfigureAwait(false);

            if (!result.Acquired) return null;

            return new DistributedLockHandle(
                distributedLock,
                lockName,
                result.LockToken,
                renewalInterval ?? TimeSpan.FromMilliseconds(ttl.TotalMilliseconds / 3));
        }

        public async ValueTask DisposeAsync()
        {
            if (_released) return;
            _released = true;
            await _renewalCts.CancelAsync().ConfigureAwait(false);
            _renewalCts.Dispose();
            await _lock.ReleaseAsync(_lockName, _lockToken).ConfigureAwait(false);
        }
    }
}
