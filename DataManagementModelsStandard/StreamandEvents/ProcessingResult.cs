using System;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>Outcome of processing a single received event.</summary>
    public sealed class EventProcessingResult
    {
        public EventProcessingStatus Status { get; init; }
        public string EventId { get; init; }
        public string Topic { get; init; }
        public string FailureReason { get; init; }
        public bool SentToDeadLetter { get; init; }
        public TimeSpan HandlerDuration { get; init; }
        public int AttemptNumber { get; init; }

        public static EventProcessingResult Success(string eventId, string topic, TimeSpan duration) =>
            new() { Status = EventProcessingStatus.Succeeded, EventId = eventId, Topic = topic, HandlerDuration = duration };

        public static EventProcessingResult Failed(string eventId, string topic, string reason, int attempt) =>
            new() { Status = EventProcessingStatus.Failed, EventId = eventId, Topic = topic, FailureReason = reason, AttemptNumber = attempt };

        public static EventProcessingResult DeadLettered(string eventId, string topic, string reason) =>
            new() { Status = EventProcessingStatus.DeadLettered, EventId = eventId, Topic = topic, FailureReason = reason, SentToDeadLetter = true };

        public static EventProcessingResult Skipped(string eventId, string topic) =>
            new() { Status = EventProcessingStatus.Skipped, EventId = eventId, Topic = topic };
    }

    public enum EventProcessingStatus
    {
        Succeeded,
        Failed,
        /// <summary>Retried at least once before succeeding.</summary>
        RetriedAndSucceeded,
        /// <summary>Routed to dead-letter after exhausting retries.</summary>
        DeadLettered,
        /// <summary>Duplicate detected and idempotency logic dropped it.</summary>
        Skipped
    }
}
