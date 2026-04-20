using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Cross-platform bounded queue used as the producer / consumer hand-off
    /// inside <see cref="TelemetryPipeline"/>. Wraps
    /// <see cref="System.Threading.Channels.Channel{T}"/> with a selectable
    /// <see cref="BackpressureMode"/> and a drop counter so the pipeline can
    /// surface overflow through metrics (Phase 11).
    /// </summary>
    /// <remarks>
    /// One queue is created per <see cref="TelemetryPipeline"/>. Logs and
    /// audit envelopes share the same queue so they retain producer order.
    /// The underlying channel is configured with <c>SingleReader = true</c>
    /// to allow the runtime to pick the fast path; multiple producers are
    /// supported.
    /// </remarks>
    internal sealed class BoundedChannelQueue
    {
        private readonly Channel<TelemetryEnvelope> _channel;
        private readonly int _capacity;
        private readonly BackpressureMode _mode;
        private long _droppedCount;

        public BoundedChannelQueue(int capacity, BackpressureMode mode)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Queue capacity must be positive.");
            }

            _capacity = capacity;
            _mode = mode;

            BoundedChannelFullMode fullMode = mode switch
            {
                BackpressureMode.DropOldest => BoundedChannelFullMode.DropOldest,
                BackpressureMode.Block => BoundedChannelFullMode.Wait,
                BackpressureMode.FailFast => BoundedChannelFullMode.DropWrite,
                _ => BoundedChannelFullMode.Wait
            };

            BoundedChannelOptions options = new BoundedChannelOptions(capacity)
            {
                FullMode = fullMode,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };

            _channel = Channel.CreateBounded<TelemetryEnvelope>(options);
        }

        /// <summary>Reader exposed to the <see cref="BatchWriter"/>.</summary>
        public ChannelReader<TelemetryEnvelope> Reader => _channel.Reader;

        /// <summary>Configured capacity (used by metrics).</summary>
        public int Capacity => _capacity;

        /// <summary>Configured backpressure mode.</summary>
        public BackpressureMode Mode => _mode;

        /// <summary>Total envelopes dropped due to backpressure since startup.</summary>
        public long DroppedCount => Interlocked.Read(ref _droppedCount);

        /// <summary>
        /// Best-effort current depth. The underlying channel exposes
        /// <see cref="ChannelReader{T}.Count"/> on .NET 6+; we treat it as a
        /// hint only because consumers and producers may race the read.
        /// </summary>
        public int CurrentDepth => _channel.Reader.Count;

        /// <summary>
        /// Synchronous, non-blocking enqueue used by the logging hot path.
        /// Returns <c>false</c> when the envelope was dropped (mode-dependent)
        /// or could not be written.
        /// </summary>
        public bool TryEnqueue(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return false;
            }

            switch (_mode)
            {
                case BackpressureMode.DropOldest:
                    if (_channel.Reader.Count >= _capacity)
                    {
                        Interlocked.Increment(ref _droppedCount);
                    }
                    _channel.Writer.TryWrite(envelope);
                    return true;

                case BackpressureMode.FailFast:
                    if (_channel.Writer.TryWrite(envelope))
                    {
                        return true;
                    }
                    Interlocked.Increment(ref _droppedCount);
                    return false;

                case BackpressureMode.Block:
                default:
                    if (_channel.Writer.TryWrite(envelope))
                    {
                        return true;
                    }
                    // Caller asked for sync enqueue; we never block here.
                    // The async path (EnqueueAsync) is the supported one for Block mode.
                    Interlocked.Increment(ref _droppedCount);
                    return false;
            }
        }

        /// <summary>
        /// Async enqueue used by the audit producer. Honors
        /// <see cref="BackpressureMode.Block"/> by awaiting capacity and
        /// honors <see cref="BackpressureMode.FailFast"/> by throwing.
        /// </summary>
        public async ValueTask EnqueueAsync(TelemetryEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope is null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            switch (_mode)
            {
                case BackpressureMode.Block:
                    await _channel.Writer.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
                    return;

                case BackpressureMode.FailFast:
                    if (!_channel.Writer.TryWrite(envelope))
                    {
                        Interlocked.Increment(ref _droppedCount);
                        throw new InvalidOperationException(
                            "Telemetry queue is full and the configured backpressure mode is FailFast.");
                    }
                    return;

                case BackpressureMode.DropOldest:
                default:
                    if (_channel.Reader.Count >= _capacity)
                    {
                        Interlocked.Increment(ref _droppedCount);
                    }
                    _channel.Writer.TryWrite(envelope);
                    return;
            }
        }

        /// <summary>
        /// Marks the channel complete so the drain loop can exit after
        /// consuming the remaining items.
        /// </summary>
        public void Complete() => _channel.Writer.TryComplete();
    }
}
