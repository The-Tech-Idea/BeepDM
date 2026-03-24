using System;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Immutable audit record capturing the evidence for a single resolved (or quarantined)
    /// bidirectional sync conflict.  Collected in <c>BeepSyncManager.LastRunConflicts</c> when
    /// <see cref="ConflictPolicy.CaptureEvidence"/> is <c>true</c>.
    /// </summary>
    public class ConflictEvidence
    {
        /// <summary>Entity (table) name where the conflict occurred.</summary>
        public string EntityName { get; set; }

        /// <summary>Primary-key string representation of the conflicting record.</summary>
        public string RecordKey { get; set; }

        /// <summary>Source-side values at the time of conflict detection.</summary>
        public object SourceValues { get; set; }

        /// <summary>Destination-side values at the time of conflict detection.</summary>
        public object DestinationValues { get; set; }

        /// <summary>
        /// Winner chosen by the resolution rule.
        /// Values: <c>"source"</c>, <c>"destination"</c>, <c>"quarantine"</c>.
        /// </summary>
        public string Winner { get; set; }

        /// <summary>
        /// Reason code returned by the rule.
        /// Examples: <c>"LATEST-WINS"</c>, <c>"CUSTOM"</c>, <c>"UNRESOLVABLE"</c>, <c>"FALLBACK"</c>.
        /// </summary>
        public string ReasonCode { get; set; }

        /// <summary>Rule Engine key that produced this decision.</summary>
        public string RuleKey { get; set; }

        /// <summary>Wall-clock time the rule engine spent evaluating this conflict.</summary>
        public TimeSpan RuleElapsed { get; set; }

        /// <summary>UTC timestamp when this conflict was detected.</summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Schema Id this conflict belongs to.</summary>
        public string SchemaId { get; set; }
    }
}
