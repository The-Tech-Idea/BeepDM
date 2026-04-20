using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Thread-safe registry of active <see cref="DualWriteWindow"/>
    /// instances keyed by entity name. Consulted by the router on
    /// every write so an in-flight migration can transparently fan
    /// writes out to both source and target placements during
    /// <see cref="DualWriteState.DualWrite"/> and
    /// <see cref="DualWriteState.Cutover"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations MUST be safe for concurrent readers (router
    /// hot path) and a single writer (the reshard orchestrator).
    /// Windows are reference-stable: a window returned by
    /// <see cref="TryGetWindow"/> can be observed to change state
    /// while the caller holds the reference, but will not be
    /// swapped out from under the caller.
    /// </para>
    /// <para>
    /// Only one active window per entity is supported in v1 —
    /// attempts to register a second window throw. Callers that
    /// need to chain migrations must complete (or cancel) the first
    /// window before starting the next.
    /// </para>
    /// </remarks>
    public interface IDualWriteCoordinator
    {
        /// <summary>
        /// Registers <paramref name="window"/> for its entity.
        /// Throws when an active window already exists for the
        /// entity.
        /// </summary>
        void Register(DualWriteWindow window);

        /// <summary>
        /// Removes the window for <paramref name="entityName"/>. No-
        /// op when no window is registered.
        /// </summary>
        void Unregister(string entityName);

        /// <summary>
        /// Returns the currently-registered window for
        /// <paramref name="entityName"/>, or <c>null</c> when no
        /// migration is active.
        /// </summary>
        DualWriteWindow TryGetWindow(string entityName);

        /// <summary>
        /// Returns a snapshot of every active window. Used by
        /// diagnostics and the resharding service to enumerate in-
        /// flight migrations.
        /// </summary>
        IReadOnlyList<DualWriteWindow> Snapshot();

        /// <summary>
        /// Convenience helper that returns extra shard ids the
        /// router must add to a write decision for
        /// <paramref name="entityName"/>. Returns an empty list
        /// when no window exists or the window is not in a dual-
        /// write state.
        /// </summary>
        IReadOnlyList<string> GetAdditionalWriteShardIds(string entityName);
    }
}
