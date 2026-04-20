using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Fan-out sink that forwards every batch to a fixed list of inner
    /// sinks. Per-inner exceptions are caught and reflected through
    /// <see cref="IsHealthy"/> so a single failing destination cannot
    /// take the rest of the pipeline down with it.
    /// </summary>
    /// <remarks>
    /// Composite isolation is layered on top of the per-sink try/catch in
    /// <see cref="BatchWriter"/>. The <see cref="BatchWriter"/> still sees
    /// the composite as one sink, so a partial failure inside the
    /// composite does not increment its sink-error counter; instead the
    /// failure surfaces here through <see cref="LastError"/> and the
    /// per-sink unhealthy bit.
    /// </remarks>
    public sealed class CompositeSink : ITelemetrySink
    {
        private readonly ITelemetrySink[] _sinks;
        private readonly bool[] _healthy;
        private long _innerFailures;
        private string _lastError;

        /// <summary>Creates a composite that forwards to <paramref name="sinks"/>.</summary>
        public CompositeSink(string name, IEnumerable<ITelemetrySink> sinks)
        {
            if (sinks is null)
            {
                throw new ArgumentNullException(nameof(sinks));
            }

            Name = string.IsNullOrWhiteSpace(name) ? "composite" : name;

            List<ITelemetrySink> tmp = new List<ITelemetrySink>();
            foreach (ITelemetrySink sink in sinks)
            {
                if (sink is not null)
                {
                    tmp.Add(sink);
                }
            }
            _sinks = tmp.ToArray();
            _healthy = new bool[_sinks.Length];
            for (int i = 0; i < _healthy.Length; i++)
            {
                _healthy[i] = true;
            }
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool IsHealthy
        {
            get
            {
                for (int i = 0; i < _healthy.Length; i++)
                {
                    if (!_healthy[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>Total per-inner-sink exceptions caught since startup.</summary>
        public long InnerFailureCount => Interlocked.Read(ref _innerFailures);

        /// <summary>Most recent inner failure message, or <c>null</c>.</summary>
        public string LastError => Volatile.Read(ref _lastError);

        /// <summary>Inner sinks in the order they were registered.</summary>
        public IReadOnlyList<ITelemetrySink> Inner => _sinks;

        /// <inheritdoc />
        public async Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
        {
            if (batch is null || batch.Count == 0 || _sinks.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _sinks.Length; i++)
            {
                try
                {
                    await _sinks[i].WriteBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                    _healthy[i] = true;
                }
                catch (Exception ex)
                {
                    _healthy[i] = false;
                    Interlocked.Increment(ref _innerFailures);
                    Volatile.Write(ref _lastError, ex.Message);
                }
            }
        }

        /// <inheritdoc />
        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < _sinks.Length; i++)
            {
                try
                {
                    await _sinks[i].FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _healthy[i] = false;
                    Interlocked.Increment(ref _innerFailures);
                    Volatile.Write(ref _lastError, ex.Message);
                }
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            for (int i = 0; i < _sinks.Length; i++)
            {
                try
                {
                    await _sinks[i].DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Disposal is best-effort; never throw from DisposeAsync.
                }
            }
        }
    }
}
