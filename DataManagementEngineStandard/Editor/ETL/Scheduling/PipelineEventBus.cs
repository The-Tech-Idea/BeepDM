using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Simple in-process publish/subscribe event bus used by <c>EventBusScheduler</c>.
    /// External code can publish named topics; schedulers subscribed to those topics fire.
    /// Thread-safe; subscriber exceptions are swallowed to protect the publisher.
    /// Maintains a bounded event history for diagnostics and audit.
    /// </summary>
    public static class PipelineEventBus
    {
        private static readonly Dictionary<string, List<Action<IReadOnlyDictionary<string, object>>>>
            _subs = new(StringComparer.OrdinalIgnoreCase);

        private static readonly ReaderWriterLockSlim _rwl = new();

        // Bounded event history for diagnostics (ring buffer)
        private static readonly LinkedList<EventHistoryEntry> _history = new();
        private static readonly object _historyLock = new();
        private static int _maxHistorySize = 1000;

        /// <summary>Set the maximum number of events retained in the history buffer.</summary>
        public static void SetMaxHistorySize(int maxSize)
        {
            if (maxSize < 0) maxSize = 0;
            _maxHistorySize = maxSize;
            TrimHistory();
        }

        /// <summary>Subscribe <paramref name="handler"/> to <paramref name="topic"/>.</summary>
        public static void Subscribe(string topic,
            Action<IReadOnlyDictionary<string, object>> handler)
        {
            _rwl.EnterWriteLock();
            try
            {
                if (!_subs.TryGetValue(topic, out var list))
                    _subs[topic] = list = new List<Action<IReadOnlyDictionary<string, object>>>();
                list.Add(handler);
            }
            finally { _rwl.ExitWriteLock(); }
        }

        /// <summary>Unsubscribe <paramref name="handler"/> from <paramref name="topic"/>.</summary>
        public static void Unsubscribe(string topic,
            Action<IReadOnlyDictionary<string, object>> handler)
        {
            _rwl.EnterWriteLock();
            try
            {
                if (_subs.TryGetValue(topic, out var list))
                    list.Remove(handler);
            }
            finally { _rwl.ExitWriteLock(); }
        }

        /// <summary>
        /// Publish an event to all subscribers of <paramref name="topic"/>.
        /// <paramref name="payload"/> fields are forwarded as pipeline override parameters.
        /// Subscriber exceptions are caught and do not affect other subscribers.
        /// The event is recorded in the history buffer for diagnostics.
        /// </summary>
        public static void Publish(string topic,
            IReadOnlyDictionary<string, object>? payload = null)
        {
            // Record in history
            RecordHistory(topic, payload);

            List<Action<IReadOnlyDictionary<string, object>>>? copy;
            _rwl.EnterReadLock();
            try
            {
                _subs.TryGetValue(topic, out var list);
                copy = list != null
                    ? new List<Action<IReadOnlyDictionary<string, object>>>(list)
                    : null;
            }
            finally { _rwl.ExitReadLock(); }

            if (copy == null) return;

            IReadOnlyDictionary<string, object> arg =
                payload ?? new Dictionary<string, object>();

            foreach (var h in copy)
            {
                try { h(arg); }
                catch { /* isolate subscriber failures */ }
            }
        }

        /// <summary>
        /// Publish a typed pipeline lifecycle event (run started, completed, failed).
        /// Includes standard fields: RunId, PipelineId, ScheduleId, Status.
        /// </summary>
        public static void PublishPipelineEvent(string eventType, string runId,
            string pipelineId, string scheduleId, string? error = null)
        {
            var payload = new Dictionary<string, object>
            {
                ["EventType"]  = eventType,
                ["RunId"]      = runId,
                ["PipelineId"] = pipelineId,
                ["ScheduleId"] = scheduleId,
                ["Timestamp"]  = DateTime.UtcNow.ToString("O")
            };
            if (error != null) payload["Error"] = error;

            Publish($"pipeline.{eventType}", payload);
        }

        /// <summary>
        /// Get recent event history, newest first, optionally filtered by topic.
        /// </summary>
        public static IReadOnlyList<EventHistoryEntry> GetHistory(string? topicFilter = null, int maxCount = 100)
        {
            var result = new List<EventHistoryEntry>();
            lock (_historyLock)
            {
                var node = _history.Last;
                while (node != null && result.Count < maxCount)
                {
                    if (topicFilter == null ||
                        node.Value.Topic.Contains(topicFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(node.Value);
                    }
                    node = node.Previous;
                }
            }
            return result;
        }

        /// <summary>Get the count of active subscriptions across all topics.</summary>
        public static int GetSubscriptionCount()
        {
            _rwl.EnterReadLock();
            try
            {
                return _subs.Values.Sum(list => list.Count);
            }
            finally { _rwl.ExitReadLock(); }
        }

        /// <summary>Get all topic names that have at least one subscriber.</summary>
        public static IReadOnlyList<string> GetActiveTopics()
        {
            _rwl.EnterReadLock();
            try
            {
                return _subs.Where(kv => kv.Value.Count > 0)
                            .Select(kv => kv.Key)
                            .ToList();
            }
            finally { _rwl.ExitReadLock(); }
        }

        private static void RecordHistory(string topic, IReadOnlyDictionary<string, object>? payload)
        {
            if (_maxHistorySize <= 0) return;

            var entry = new EventHistoryEntry
            {
                Topic   = topic,
                Payload = payload != null ? new Dictionary<string, object>(payload) : null
            };

            lock (_historyLock)
            {
                _history.AddLast(entry);
                TrimHistoryLocked();
            }
        }

        private static void TrimHistory()
        {
            lock (_historyLock)
                TrimHistoryLocked();
        }

        private static void TrimHistoryLocked()
        {
            while (_history.Count > _maxHistorySize)
                _history.RemoveFirst();
        }
    }

    /// <summary>
    /// A recorded event from the PipelineEventBus for diagnostics and audit.
    /// </summary>
    public class EventHistoryEntry
    {
        public string Topic       { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Payload { get; set; }
    }
}
