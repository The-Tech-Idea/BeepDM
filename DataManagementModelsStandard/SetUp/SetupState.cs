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
        /// <summary>The state-document version this build writes and understands.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>
        /// Schema version of this persisted document.
        /// </summary>
        /// <remarks>
        /// Without this, a shape change made <c>LoadPersistedState</c> fail deserialization, swallow
        /// the error, and start a "fresh" run — which on a live database is not a reset, it is a
        /// re-migration. <c>ISetupStateUpgrader</c> migrates older documents; unknown/newer versions
        /// fail loudly.
        /// </remarks>
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>Unique identifier for this wizard run, regenerated on each fresh start. Used to detect stale/concurrent checkpoints.</summary>
        public string RunId { get; set; }

        /// <summary>
        /// Monotonic write counter, incremented on every save. A remote store uses it for
        /// optimistic concurrency (the enterprise equivalent of the local lease); a local store
        /// leaves it advancing but relies on the lease + atomic file replace.
        /// </summary>
        public long Revision { get; set; }

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

        /// <summary>Id of the principal that ran this setup (Phase 5); null for anonymous.</summary>
        public string ActorId { get; set; }

        /// <summary>Whether the actor was authenticated. Never inferred — an anonymous run records false.</summary>
        public bool ActorAuthenticated { get; set; }

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
