using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Per-entity dual-write window tracking a running Phase 11
    /// reshard operation. Stores the entity name, current
    /// <see cref="DualWriteState"/>, and both placements (source +
    /// target) so the router can fan writes out to both sets while a
    /// move/repartition is in progress.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Windows are created by <see cref="ReshardingService"/> and
    /// registered with an <see cref="IDualWriteCoordinator"/>. The
    /// router consults the coordinator on every write to augment the
    /// decision's shard list whenever a window is in
    /// <see cref="DualWriteState.DualWrite"/> or
    /// <see cref="DualWriteState.Cutover"/>.
    /// </para>
    /// <para>
    /// State transitions are enforced here: illegal transitions throw
    /// <see cref="InvalidOperationException"/> rather than silently
    /// corrupting the migration. <see cref="TransitionTo"/> and
    /// <see cref="TryTransitionTo"/> are thread-safe via a per-window
    /// lock.
    /// </para>
    /// </remarks>
    public sealed class DualWriteWindow
    {
        private readonly object _sync = new object();
        private DualWriteState  _state;
        private DateTime        _updatedUtc;

        /// <summary>Initialises a new window in <see cref="DualWriteState.Shadow"/>.</summary>
        /// <param name="reshardId">Stable identifier for the governing reshard operation. Required.</param>
        /// <param name="entityName">Logical entity being migrated. Required.</param>
        /// <param name="sourceShardIds">Placement the entity currently lives on. Must contain at least one id.</param>
        /// <param name="targetShardIds">Placement the entity is moving to. Must contain at least one id.</param>
        public DualWriteWindow(
            string                reshardId,
            string                entityName,
            IReadOnlyList<string> sourceShardIds,
            IReadOnlyList<string> targetShardIds)
        {
            if (string.IsNullOrWhiteSpace(reshardId))
                throw new ArgumentException("Reshard id cannot be null or whitespace.", nameof(reshardId));
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));
            if (sourceShardIds == null || sourceShardIds.Count == 0)
                throw new ArgumentException("At least one source shard is required.", nameof(sourceShardIds));
            if (targetShardIds == null || targetShardIds.Count == 0)
                throw new ArgumentException("At least one target shard is required.", nameof(targetShardIds));

            ReshardId      = reshardId;
            EntityName     = entityName;
            SourceShardIds = Normalise(sourceShardIds);
            TargetShardIds = Normalise(targetShardIds);
            _state         = DualWriteState.Shadow;
            StartedUtc     = DateTime.UtcNow;
            _updatedUtc    = StartedUtc;
        }

        /// <summary>Stable identifier for the governing reshard operation.</summary>
        public string ReshardId { get; }

        /// <summary>Logical entity being migrated.</summary>
        public string EntityName { get; }

        /// <summary>Placement the entity currently lives on.</summary>
        public IReadOnlyList<string> SourceShardIds { get; }

        /// <summary>Placement the entity is moving to.</summary>
        public IReadOnlyList<string> TargetShardIds { get; }

        /// <summary>UTC timestamp the window was opened.</summary>
        public DateTime StartedUtc { get; }

        /// <summary>UTC timestamp of the last <see cref="State"/> change.</summary>
        public DateTime UpdatedUtc
        {
            get { lock (_sync) { return _updatedUtc; } }
        }

        /// <summary>Current state of the window.</summary>
        public DualWriteState State
        {
            get { lock (_sync) { return _state; } }
        }

        /// <summary>
        /// Returns <c>true</c> when the window is in a state that
        /// requires writes to be fanned out to both source and target
        /// placements.
        /// </summary>
        public bool IsWriteDualHit
        {
            get
            {
                lock (_sync)
                {
                    return _state == DualWriteState.DualWrite
                        || _state == DualWriteState.Cutover;
                }
            }
        }

        /// <summary>
        /// Returns the union of source and target shard ids in stable,
        /// case-insensitive order. Used by the router when augmenting
        /// a write decision during
        /// <see cref="DualWriteState.DualWrite"/> /
        /// <see cref="DualWriteState.Cutover"/>.
        /// </summary>
        public IReadOnlyList<string> UnionShardIds()
        {
            var set  = new HashSet<string>(SourceShardIds, StringComparer.OrdinalIgnoreCase);
            var list = new List<string>(SourceShardIds);
            for (int i = 0; i < TargetShardIds.Count; i++)
            {
                if (set.Add(TargetShardIds[i])) list.Add(TargetShardIds[i]);
            }
            return list;
        }

        /// <summary>
        /// Transitions the window to <paramref name="next"/>. Throws
        /// when the transition is illegal.
        /// </summary>
        public void TransitionTo(DualWriteState next)
        {
            if (!TryTransitionTo(next, out var reason))
                throw new InvalidOperationException(
                    $"Illegal dual-write transition {State} -> {next} for entity '{EntityName}': {reason}");
        }

        /// <summary>
        /// Attempts to transition the window to <paramref name="next"/>.
        /// Returns <c>false</c> without mutating state when the
        /// transition is illegal.
        /// </summary>
        public bool TryTransitionTo(DualWriteState next, out string reason)
        {
            lock (_sync)
            {
                if (!IsTransitionAllowed(_state, next, out reason)) return false;
                _state      = next;
                _updatedUtc = DateTime.UtcNow;
                return true;
            }
        }

        /// <summary>
        /// Forces the window to <see cref="DualWriteState.Off"/>
        /// regardless of current state. Used when a reshard is
        /// cancelled mid-flight.
        /// </summary>
        public void ForceOff()
        {
            lock (_sync)
            {
                _state      = DualWriteState.Off;
                _updatedUtc = DateTime.UtcNow;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"DualWriteWindow(reshard={ReshardId}, entity={EntityName}, state={State}, " +
               $"src=[{string.Join(",", SourceShardIds)}], tgt=[{string.Join(",", TargetShardIds)}])";

        // ── Helpers ───────────────────────────────────────────────────────

        private static bool IsTransitionAllowed(DualWriteState from, DualWriteState to, out string reason)
        {
            reason = string.Empty;
            if (from == to)
            {
                reason = "transition is a no-op";
                return false;
            }
            switch (from)
            {
                case DualWriteState.Shadow:    return AllowFromShadow(to, out reason);
                case DualWriteState.DualWrite: return AllowFromDualWrite(to, out reason);
                case DualWriteState.Cutover:   return AllowFromCutover(to, out reason);
                case DualWriteState.Off:
                    reason = "window has already been closed";
                    return false;
                default:
                    reason = "unknown source state";
                    return false;
            }
        }

        private static bool AllowFromShadow(DualWriteState to, out string reason)
        {
            reason = string.Empty;
            if (to == DualWriteState.DualWrite) return true;
            if (to == DualWriteState.Off)       return true;
            reason = "Shadow only permits DualWrite or Off";
            return false;
        }

        private static bool AllowFromDualWrite(DualWriteState to, out string reason)
        {
            reason = string.Empty;
            if (to == DualWriteState.Cutover) return true;
            if (to == DualWriteState.Off)     return true;
            reason = "DualWrite only permits Cutover or Off";
            return false;
        }

        private static bool AllowFromCutover(DualWriteState to, out string reason)
        {
            reason = string.Empty;
            if (to == DualWriteState.Off) return true;
            reason = "Cutover only permits Off";
            return false;
        }

        private static IReadOnlyList<string> Normalise(IReadOnlyList<string> shardIds)
            => shardIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }
}
