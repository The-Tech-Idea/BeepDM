using System;

namespace TheTechIdea.Beep.Editor.Importing.ErrorStore
{
    /// <summary>
    /// Full audit record for a single failed / quarantined import record.
    /// Persisted by <see cref="IImportErrorStore"/> implementations.
    /// </summary>
    public sealed class ImportErrorRecord
    {
        /// <summary>Pipeline key: "{sourceDS}/{sourceEntity}/{destDS}/{destEntity}".</summary>
        public string ContextKey    { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the error occurred.</summary>
        public DateTime OccurredAt  { get; set; } = DateTime.UtcNow;

        /// <summary>Zero-based batch counter within the overall import run.</summary>
        public int BatchNumber      { get; set; }

        /// <summary>Zero-based index of the record within its batch.</summary>
        public int RecordIndex      { get; set; }

        /// <summary>The name of the quality rule that triggered the failure (if applicable).</summary>
        public string? RuleName     { get; set; }

        /// <summary>Human-readable description of why the record failed.</summary>
        public string Reason        { get; set; } = string.Empty;

        /// <summary>
        /// The faulted record as it arrived from the source.
        /// Serialised as JSON when persisted.
        /// </summary>
        public object? RawRecord    { get; set; }

        /// <summary>
        /// <c>true</c> when this record has been successfully replayed after fixing the root cause.
        /// </summary>
        public bool Replayed        { get; set; }

        /// <summary>UTC timestamp of a successful replay (set by the Replay partial).</summary>
        public DateTime? ReplayedAt { get; set; }

        /// <summary>Optional notes added during manual triage of the error.</summary>
        public string? TriageNote   { get; set; }
    }
}
