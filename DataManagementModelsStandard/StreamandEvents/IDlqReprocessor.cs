using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ═══════════════════════════════════════════════════════════════════════════
    // DLQ reprocessing request / result
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Controls where reprocessed DLQ messages are published.</summary>
    public enum DlqReprocessMode
    {
        /// <summary>Republish each message to its original topic.</summary>
        RetryOriginalTopic,

        /// <summary>Republish each message to an explicitly specified alternate topic.</summary>
        SendToAlternateTopic,

        /// <summary>
        /// Simulate reprocessing without publishing (returns a log of what would happen).
        /// </summary>
        DryRun
    }

    /// <summary>Describes a reprocess operation over a dead-letter queue.</summary>
    public sealed class DlqReprocessRequest
    {
        /// <summary>Fully-qualified name of the DLQ topic to drain.</summary>
        public string DlqTopicName { get; init; } = string.Empty;

        /// <summary>Override target topic (required for <see cref="DlqReprocessMode.SendToAlternateTopic"/>).</summary>
        public string? TargetTopicName { get; init; }

        /// <summary>Optional predicate — only messages for which the filter returns <c>true</c> are reprocessed.</summary>
        public Func<DlqEnvelope<object>, bool>? Filter { get; init; }

        /// <summary>Maximum number of messages to attempt per invocation (default 100).</summary>
        public int MaxMessages { get; init; } = 100;

        /// <summary>Reprocess mode.</summary>
        public DlqReprocessMode Mode { get; init; } = DlqReprocessMode.RetryOriginalTopic;

        /// <summary>
        /// When <c>true</c> the <c>RetryCount</c> header is reset to zero before republishing,
        /// giving the message a full set of attempts.
        /// </summary>
        public bool ResetRetryCount { get; init; }
    }

    /// <summary>Summary of a completed DLQ reprocess operation.</summary>
    public sealed class DlqReprocessResult
    {
        /// <summary>Number of messages successfully republished.</summary>
        public int Reprocessed { get; init; }

        /// <summary>Number of messages skipped by the filter or already processed.</summary>
        public int Skipped { get; init; }

        /// <summary>Number of messages that failed during republishing.</summary>
        public int Failed { get; init; }

        /// <summary>
        /// Human-readable log of what would have been done (populated only for
        /// <see cref="DlqReprocessMode.DryRun"/>).
        /// </summary>
        public IReadOnlyList<string>? DryRunMessages { get; init; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IDlqReprocessor
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Manages inspection, reprocessing and purging of dead-letter queue topics.
    /// </summary>
    public interface IDlqReprocessor
    {
        /// <summary>
        /// Reads up to <see cref="DlqReprocessRequest.MaxMessages"/> messages from the DLQ and
        /// republishes them according to the request's <see cref="DlqReprocessMode"/>.
        /// </summary>
        Task<DlqReprocessResult> ReprocessAsync(
            DlqReprocessRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Non-destructively inspects messages in the DLQ without changing consumer offsets.
        /// </summary>
        IAsyncEnumerable<DlqEnvelope<object>> PeekAsync(
            string dlqTopic,
            int maxMessages = 50,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently removes messages from the DLQ, optionally filtered by a predicate.
        /// </summary>
        /// <returns>Number of messages purged.</returns>
        Task<int> PurgeAsync(
            string dlqTopic,
            Func<DlqEnvelope<object>, bool>? filter = null,
            CancellationToken cancellationToken = default);

        /// <summary>Returns the approximate number of unprocessed messages in the DLQ.</summary>
        Task<long> GetDlqDepthAsync(
            string dlqTopic,
            CancellationToken cancellationToken = default);
    }
}
