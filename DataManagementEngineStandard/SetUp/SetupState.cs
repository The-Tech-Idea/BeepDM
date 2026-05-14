using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Serializable snapshot of wizard progress used for checkpointing and resume.
    /// Persist to JSON so runs can resume after a crash or user interruption.
    /// </summary>
    public class SetupState
    {
        /// <summary>Set of step IDs that have completed successfully.</summary>
        public HashSet<string> CompletedStepIds { get; set; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Set of step IDs that were skipped (CanSkip returned true).</summary>
        public HashSet<string> SkippedStepIds { get; set; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Step ID of the last failed step, or null if no failure yet.</summary>
        public string FailedStepId { get; set; }

        /// <summary>Content hash of the entity list used for schema creation (Phase 3).</summary>
        public string SchemaHash { get; set; }

        /// <summary>Seeder IDs that have already been applied (Phase 4).</summary>
        public HashSet<string> CompletedSeederIds { get; set; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>UTC timestamp when the first step in this run started.</summary>
        public DateTimeOffset? StartedAt { get; set; }

        /// <summary>UTC timestamp of the last completed step.</summary>
        public DateTimeOffset? LastUpdatedAt { get; set; }

        /// <summary>Arbitrary key-value bag for step-specific persisted state.</summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Returns true when <paramref name="stepId"/> has been completed or skipped,
        /// meaning the wizard should not re-execute it.
        /// </summary>
        public bool IsStepCompleted(string stepId) =>
            CompletedStepIds.Contains(stepId) || SkippedStepIds.Contains(stepId);
    }
}
