namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Phase 11 dual-write window lifecycle states. Drives
    /// <see cref="DualWriteWindow"/> and the router's write-path
    /// consultation of the <see cref="IDualWriteCoordinator"/> so
    /// reads stay consistent while an entity is being moved from one
    /// placement to another.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lifecycle: <see cref="Off"/> → <see cref="Shadow"/> →
    /// <see cref="DualWrite"/> → <see cref="Cutover"/> →
    /// <see cref="Off"/>. Every transition is one-way and logged so
    /// operators can audit a reshard end-to-end.
    /// </para>
    /// <para>
    /// Semantics per state:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="Off"/>: no migration in flight; router uses the active plan only.</item>
    ///   <item><see cref="Shadow"/>: target placement has been announced; reads/writes still flow only to the source.</item>
    ///   <item><see cref="DualWrite"/>: writes fan out to both source and target placements while the copy runs.</item>
    ///   <item><see cref="Cutover"/>: reads are being flipped to the target; writes still dual-hit during this short window.</item>
    /// </list>
    /// </remarks>
    public enum DualWriteState
    {
        /// <summary>No migration in flight.</summary>
        Off       = 0,

        /// <summary>Target placement announced; no write fan-out yet.</summary>
        Shadow    = 1,

        /// <summary>Writes fan out to both source and target placements.</summary>
        DualWrite = 2,

        /// <summary>Reads are flipping to the target placement; writes still dual-hit.</summary>
        Cutover   = 3
    }
}
