using System;
using System.Collections.Generic;
using System.Threading;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Simple in-process publish/subscribe event bus used by <c>EventBusScheduler</c>.
    /// External code can publish named topics; schedulers subscribed to those topics fire.
    /// Thread-safe; subscriber exceptions are swallowed to protect the publisher.
    /// </summary>
    public static class PipelineEventBus
    {
        private static readonly Dictionary<string, List<Action<IReadOnlyDictionary<string, object>>>>
            _subs = new(StringComparer.OrdinalIgnoreCase);

        private static readonly ReaderWriterLockSlim _rwl = new();

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
        /// </summary>
        public static void Publish(string topic,
            IReadOnlyDictionary<string, object>? payload = null)
        {
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
    }
}
