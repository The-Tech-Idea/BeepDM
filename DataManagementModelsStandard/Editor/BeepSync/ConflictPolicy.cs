namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Defines how bidirectional sync conflicts are detected and resolved for a
    /// <see cref="DataSyncSchema"/>. When absent, the schema's
    /// <see cref="DataSyncSchema.ConflictResolutionStrategy"/> string is used as a fallback.
    /// </summary>
    public class ConflictPolicy
    {
        /// <summary>
        /// Rule Engine key that produces the winner for a given record conflict.
        /// Built-in keys: <c>"sync.conflict.source-wins"</c>,
        /// <c>"sync.conflict.destination-wins"</c>,
        /// <c>"sync.conflict.latest-timestamp-wins"</c>,
        /// <c>"sync.conflict.fail-on-conflict"</c>.
        /// </summary>
        public string ResolutionRuleKey { get; set; } = "sync.conflict.source-wins";

        /// <summary>Data source name where unresolvable records are quarantined.</summary>
        public string QuarantineDsName { get; set; }

        /// <summary>Entity / table in <see cref="QuarantineDsName"/> that receives quarantined rows.</summary>
        public string QuarantineEntity { get; set; }

        /// <summary>
        /// When <c>true</c> (default), each resolved conflict produces a <see cref="ConflictEvidence"/>
        /// entry in <c>BeepSyncManager.LastRunConflicts</c>.
        /// </summary>
        public bool CaptureEvidence { get; set; } = true;

        /// <summary>
        /// Maximum number of conflicts allowed per sync run before the
        /// <see cref="OnMaxExceededAction"/> is applied.  <c>-1</c> = unlimited.
        /// </summary>
        public int MaxConflictsPerRun { get; set; } = -1;

        /// <summary>
        /// Action taken when <see cref="MaxConflictsPerRun"/> is exceeded.
        /// Values: <c>"Abort"</c>, <c>"Continue"</c>, <c>"QuarantineRest"</c>.
        /// </summary>
        public string OnMaxExceededAction { get; set; } = "Abort";
    }
}
