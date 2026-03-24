using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>A poisoned event that has been quarantined into the dead-letter channel.</summary>
    public sealed class DeadLetterRecord
    {
        public string DeadLetterId { get; init; } = Guid.NewGuid().ToString();
        public string OriginalEventId { get; init; }
        public string OriginalTopic { get; init; }
        public string DeadLetterTopic { get; init; }
        public string ConsumerGroup { get; init; }
        public string FailureReason { get; init; }
        public string FailureCategory { get; init; }
        public int AttemptCount { get; init; }
        public DateTime FirstFailedAt { get; init; }
        public DateTime DeadLetteredAt { get; init; } = DateTime.UtcNow;

        /// <summary>Serialized original payload preserved for replay/investigation.</summary>
        public byte[] OriginalPayloadBytes { get; init; }

        /// <summary>Original headers cloned for forensic use.</summary>
        public IReadOnlyDictionary<string, string> OriginalHeaders { get; init; }

        public string Status { get; set; } = "Quarantined"; // Quarantined | Replayed | Discarded
    }

    /// <summary>
    /// Dead-letter write contract.
    /// Engine implementations route exhausted events through this.
    /// </summary>
    public interface IDeadLetterWriter
    {
        Task WriteAsync(DeadLetterRecord record, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DeadLetterRecord>> QueryAsync(string topic, int maxRecords = 100, CancellationToken cancellationToken = default);
        Task ReplayAsync(string deadLetterId, CancellationToken cancellationToken = default);
        Task DiscardAsync(string deadLetterId, CancellationToken cancellationToken = default);
    }

    /// <summary>Classifies whether an error warrants retry, dead-letter, or abort.</summary>
    public sealed class PoisonClassification
    {
        public string FailureCategory { get; init; }
        public PoisonAction RecommendedAction { get; init; }
        public string Reason { get; init; }
    }

    public enum PoisonAction { Retry, DeadLetter, Discard, Abort }
}
