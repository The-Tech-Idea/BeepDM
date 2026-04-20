using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Logging;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Single chokepoint for emitting self-observability envelopes
    /// (drop spike, sink unhealthy, budget breach, chain divergence, …).
    /// Enforces a per-(category, key) dedup window so a failure that
    /// fires once per millisecond cannot itself flood the pipeline.
    /// </summary>
    /// <remarks>
    /// The emitter writes envelopes through an injected delegate so it
    /// stays decoupled from <see cref="TelemetryPipeline"/> internals;
    /// the pipeline binds the delegate to its synthetic-enqueue path so
    /// self events bypass the deduper / rate-limiter / sampler stages
    /// (those have already had a chance to run on the originating
    /// envelopes — running them again would double-account).
    /// </remarks>
    public sealed class SelfEventEmitter
    {
        /// <summary>Default dedup window (1 minute) per (category, key).</summary>
        public static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(1);

        private readonly Action<TelemetryEnvelope> _emit;
        private readonly TimeSpan _window;
        private readonly Action<TelemetryEnvelope> _onEvent;
        private readonly ConcurrentDictionary<string, DateTime> _lastEmittedUtc = new(StringComparer.Ordinal);
        private readonly PipelineMetrics _metrics;

        /// <summary>
        /// Creates a new emitter. <paramref name="emit"/> is invoked for
        /// every accepted self event (after the dedup window has cleared);
        /// <paramref name="onEvent"/> is an optional operator hook that
        /// fires for every event the emitter accepts (regardless of where
        /// it routes it).
        /// </summary>
        public SelfEventEmitter(
            string pipelineName,
            Action<TelemetryEnvelope> emit,
            PipelineMetrics metrics = null,
            TimeSpan? window = null,
            Action<TelemetryEnvelope> onEvent = null)
        {
            PipelineName = string.IsNullOrWhiteSpace(pipelineName) ? "pipeline" : pipelineName;
            _emit = emit ?? (_ => { });
            _metrics = metrics;
            _window = window.HasValue && window.Value > TimeSpan.Zero ? window.Value : DefaultWindow;
            _onEvent = onEvent;
        }

        /// <summary>Pipeline label embedded in every emitted envelope.</summary>
        public string PipelineName { get; }

        /// <summary>Configured dedup window.</summary>
        public TimeSpan Window => _window;

        /// <summary>
        /// Emits a self event under <paramref name="category"/>. Repeat
        /// invocations for the same <paramref name="dedupKey"/> within
        /// <see cref="Window"/> are folded into a counter increment.
        /// </summary>
        public bool Emit(
            string category,
            string dedupKey,
            BeepLogLevel level,
            string message,
            IReadOnlyDictionary<string, object> properties = null,
            Exception exception = null)
        {
            if (string.IsNullOrEmpty(category))
            {
                category = SelfEventCategory.Root;
            }

            string composite = string.Concat(category, "|", dedupKey ?? string.Empty);
            DateTime now = DateTime.UtcNow;

            DateTime? skip = null;
            _lastEmittedUtc.AddOrUpdate(
                composite,
                now,
                (_, existing) =>
                {
                    if (now - existing < _window)
                    {
                        skip = existing;
                        return existing;
                    }
                    return now;
                });

            if (skip.HasValue)
            {
                _metrics?.IncrementSelfEventDeduped();
                return false;
            }

            TelemetryEnvelope envelope = BuildEnvelope(category, level, message, properties, exception);

            try
            {
                _emit(envelope);
                _metrics?.IncrementSelfEventEmitted();
                _onEvent?.Invoke(envelope);
                return true;
            }
            catch
            {
                // The emitter must never throw out of a diagnostic path.
                return false;
            }
        }

        private TelemetryEnvelope BuildEnvelope(
            string category,
            BeepLogLevel level,
            string message,
            IReadOnlyDictionary<string, object> properties,
            Exception exception)
        {
            Dictionary<string, object> bag;
            if (properties is null || properties.Count == 0)
            {
                bag = new Dictionary<string, object>(1, StringComparer.Ordinal)
                {
                    ["pipeline"] = PipelineName
                };
            }
            else
            {
                bag = new Dictionary<string, object>(properties.Count + 1, StringComparer.Ordinal);
                foreach (KeyValuePair<string, object> kvp in properties)
                {
                    bag[kvp.Key] = kvp.Value;
                }
                bag["pipeline"] = PipelineName;
            }

            return new TelemetryEnvelope
            {
                Kind = TelemetryKind.Self,
                Level = level,
                Category = category,
                Message = message,
                Exception = exception,
                Properties = bag
            };
        }
    }
}
