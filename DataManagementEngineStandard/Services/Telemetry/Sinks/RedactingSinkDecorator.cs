using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Telemetry.Redaction;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Wraps an inner <see cref="ITelemetrySink"/> with a per-sink redactor
    /// stack. Used when a single sink (e.g. an audit SQLite store) requires
    /// stricter redaction than the global pipeline chain.
    /// </summary>
    /// <remarks>
    /// Each batch is cloned before redaction so the in-place mutations made
    /// by the sink-local redactors do not leak back to other fan-out sinks
    /// in the same write. Cloning cost is paid once per envelope per
    /// decorated sink — comparable to a property-bag copy — so this should
    /// only be used where the stricter rules really are needed.
    /// </remarks>
    public sealed class RedactingSinkDecorator : ITelemetrySink
    {
        private readonly ITelemetrySink _inner;
        private readonly IReadOnlyList<IRedactor> _redactors;

        /// <summary>
        /// Decorates <paramref name="inner"/> with the supplied redactor
        /// chain. Null/empty chains throw because that would make the
        /// decorator pointless and silently dangerous.
        /// </summary>
        public RedactingSinkDecorator(ITelemetrySink inner, IEnumerable<IRedactor> redactors)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (redactors is null)
            {
                throw new ArgumentNullException(nameof(redactors));
            }
            var list = new List<IRedactor>();
            foreach (var redactor in redactors)
            {
                if (redactor != null)
                {
                    list.Add(redactor);
                }
            }
            if (list.Count == 0)
            {
                throw new ArgumentException("At least one redactor is required.", nameof(redactors));
            }
            _redactors = list;
        }

        /// <inheritdoc/>
        public string Name => string.Concat(_inner.Name, "+redact");

        /// <inheritdoc/>
        public bool IsHealthy => _inner.IsHealthy;

        /// <summary>The sink wrapped by this decorator.</summary>
        public ITelemetrySink Inner => _inner;

        /// <summary>The per-sink redactor chain, in execution order.</summary>
        public IReadOnlyList<IRedactor> Redactors => _redactors;

        /// <inheritdoc/>
        public Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
        {
            if (batch is null || batch.Count == 0)
            {
                return Task.CompletedTask;
            }

            var cloned = new TelemetryEnvelope[batch.Count];
            for (int i = 0; i < batch.Count; i++)
            {
                TelemetryEnvelope source = batch[i];
                TelemetryEnvelope copy = source?.Clone();
                if (copy != null)
                {
                    for (int r = 0; r < _redactors.Count; r++)
                    {
                        try
                        {
                            _redactors[r].Redact(copy);
                        }
                        catch
                        {
                            // Same isolation rule as the pipeline: a faulty
                            // redactor is dropped silently rather than failing
                            // the entire write.
                        }
                    }
                }
                cloned[i] = copy;
            }

            return _inner.WriteBatchAsync(cloned, cancellationToken);
        }

        /// <inheritdoc/>
        public Task FlushAsync(CancellationToken cancellationToken)
        {
            return _inner.FlushAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            return _inner.DisposeAsync();
        }
    }
}
