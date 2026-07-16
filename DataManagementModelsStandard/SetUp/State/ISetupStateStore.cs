using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.SetUp.State
{
    /// <summary>
    /// Where a wizard's <see cref="SetupState"/> is loaded from and saved to.
    /// </summary>
    /// <remarks>
    /// Substitutable so the <b>same product</b> runs solo (local JSON, no lease contention) and
    /// enterprise (shared/remote store with optimistic concurrency and leases). The old
    /// <c>SetupCheckpointStore</c> was <c>internal static</c> — not replaceable, and its lock was
    /// per-process, so two machines sharing a path would interleave.
    /// </remarks>
    public interface ISetupStateStore
    {
        /// <summary>
        /// Loads the state for <paramref name="key"/>, or null if none is stored.
        /// </summary>
        Task<SetupState> LoadAsync(SetupStateKey key, CancellationToken token = default);

        /// <summary>
        /// Persists the state for <paramref name="key"/>.
        /// </summary>
        /// <remarks>
        /// Implementations that support optimistic concurrency throw
        /// <see cref="SetupStateConflictException"/> when the stored version changed since it was
        /// last loaded under the held <paramref name="lease"/>.
        /// </remarks>
        Task SaveAsync(SetupStateKey key, SetupState state, ISetupStateLease lease = null,
            CancellationToken token = default);

        /// <summary>
        /// Attempts to acquire an exclusive lease on <paramref name="key"/>.
        /// Returns null when another runner already holds an unexpired lease.
        /// </summary>
        Task<ISetupStateLease> TryAcquireLeaseAsync(SetupStateKey key, TimeSpan ttl,
            CancellationToken token = default);
    }

    /// <summary>
    /// An exclusive claim on a <see cref="SetupStateKey"/>. Dispose releases it.
    /// </summary>
    public interface ISetupStateLease : IAsyncDisposable
    {
        SetupStateKey Key { get; }

        /// <summary>The run that holds this lease; written into <see cref="SetupState.RunId"/>.</summary>
        string RunId { get; }

        DateTimeOffset ExpiresAt { get; }

        /// <summary>Extends the lease. Returns false if it was lost (expired and reclaimed).</summary>
        Task<bool> RenewAsync(CancellationToken token = default);
    }

    /// <summary>
    /// Thrown by <see cref="ISetupStateStore.SaveAsync"/> when a concurrent writer changed the
    /// stored state — the caller's view is stale and the save was refused.
    /// </summary>
    public sealed class SetupStateConflictException : Exception
    {
        public SetupStateConflictException(string message) : base(message) { }
        public SetupStateConflictException(string message, Exception inner) : base(message, inner) { }
    }
}
