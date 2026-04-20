using System;
using System.Collections.Generic;
using System.Threading;

namespace TheTechIdea.Beep.Services.Telemetry.Dedup
{
    /// <summary>
    /// Bounded, time-windowed message deduper. Collapses identical
    /// <c>(level, category, normalized-template)</c> envelopes within a
    /// configurable window into a single forwarded envelope plus a
    /// post-window summary carrying the suppressed count.
    /// </summary>
    /// <remarks>
    /// The class is split across two partial files: <c>.Core</c> exposes
    /// the public surface and accepts envelopes; <c>.Window</c> owns
    /// eviction and summary emission.
    /// </remarks>
    public sealed partial class WindowedDeduper : IMessageDeduper
    {
        /// <summary>Default window when none is supplied.</summary>
        public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(10);

        /// <summary>Default maximum tracked keys when none is supplied.</summary>
        public const int DefaultMaxKeys = 1024;

        /// <summary>Property key carrying the number of suppressed envelopes in a summary.</summary>
        public const string DedupCountProperty = "dedup.count";

        /// <summary>Property key carrying the original normalized template.</summary>
        public const string DedupTemplateProperty = "dedup.template";

        private readonly object _gate = new object();
        private readonly Dictionary<string, WindowEntry> _windows;
        private readonly LinkedList<string> _lru;
        private readonly TimeSpan _window;
        private readonly int _maxKeys;
        private readonly Func<DateTime> _utcNow;

        private Action<TelemetryEnvelope> _emitSummary;
        private long _suppressedCount;

        /// <summary>Creates a deduper with the supplied window and key cap.</summary>
        public WindowedDeduper(TimeSpan? window = null, int? maxKeys = null, Func<DateTime> utcNow = null)
        {
            _window = (window ?? DefaultWindow);
            if (_window <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(window), "Window must be positive.");
            }
            _maxKeys = maxKeys ?? DefaultMaxKeys;
            if (_maxKeys <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxKeys), "MaxKeys must be positive.");
            }
            _windows = new Dictionary<string, WindowEntry>(_maxKeys, StringComparer.Ordinal);
            _lru = new LinkedList<string>();
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public string Name => "windowed";

        /// <inheritdoc/>
        public long SuppressedCount => Interlocked.Read(ref _suppressedCount);

        /// <summary>Configured window length.</summary>
        public TimeSpan Window => _window;

        /// <summary>Configured maximum tracked keys.</summary>
        public int MaxKeys => _maxKeys;

        /// <inheritdoc/>
        public void Bind(Action<TelemetryEnvelope> emitSummary)
        {
            _emitSummary = emitSummary;
        }

        /// <inheritdoc/>
        public bool TryAccept(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return false;
            }

            string template = MessageTemplateNormalizer.Normalize(envelope.Message);
            string key = BuildKey(envelope, template);
            DateTime now = _utcNow();

            List<TelemetryEnvelope> expired = null;
            bool forward;

            lock (_gate)
            {
                expired = ExpireOldEntries_NoLock(now);

                if (_windows.TryGetValue(key, out WindowEntry entry))
                {
                    entry.Count++;
                    Touch_NoLock(entry);
                    Interlocked.Increment(ref _suppressedCount);
                    forward = false;
                }
                else
                {
                    entry = new WindowEntry
                    {
                        Key = key,
                        Template = template,
                        Level = envelope.Level,
                        Category = envelope.Category,
                        FirstSeenUtc = now,
                        ExpiresAtUtc = now + _window,
                        Count = 1
                    };
                    EvictIfNeeded_NoLock();
                    _windows[key] = entry;
                    entry.Node = _lru.AddLast(key);
                    forward = true;
                }
            }

            FlushExpired(expired);
            return forward;
        }

        /// <inheritdoc/>
        public void DrainExpired()
        {
            DateTime now = _utcNow();
            List<TelemetryEnvelope> expired;
            lock (_gate)
            {
                expired = ExpireOldEntries_NoLock(now);
            }
            FlushExpired(expired);
        }

        private void FlushExpired(List<TelemetryEnvelope> expired)
        {
            if (expired is null || expired.Count == 0 || _emitSummary is null)
            {
                return;
            }
            for (int i = 0; i < expired.Count; i++)
            {
                try
                {
                    _emitSummary(expired[i]);
                }
                catch
                {
                    // Summary emission must never throw past the deduper.
                }
            }
        }

        private static string BuildKey(TelemetryEnvelope envelope, string template)
        {
            return string.Concat(
                ((int)envelope.Level).ToString(),
                "|",
                envelope.Category ?? string.Empty,
                "|",
                template ?? string.Empty);
        }
    }
}
