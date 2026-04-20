using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Throughput / drop / latency probe for the
    /// <see cref="TelemetryPipeline"/>. Fires <see cref="TargetEventsPerSec"/>
    /// envelopes for <see cref="Duration"/> and reports queue depth, drop
    /// counts, and end-to-end latency observed at a downstream
    /// <see cref="MemorySink"/>.
    /// </summary>
    /// <remarks>
    /// Designed for two scenarios:
    /// <list type="bullet">
    ///   <item>CI smoke: assert that documented per-platform floor numbers
    ///         are met on the build agent.</item>
    ///   <item>Local triage: drop into a console app to characterize a
    ///         given sink set on the operator's hardware.</item>
    /// </list>
    /// The harness intentionally avoids any third-party benchmarking
    /// library so it can run inside the engine assembly itself.
    /// </remarks>
    public sealed class PerfHarness
    {
        private readonly TelemetryPipeline _pipeline;
        private readonly MemorySink _observer;

        /// <summary>
        /// Creates a harness that submits envelopes through
        /// <paramref name="pipeline"/> and observes them via
        /// <paramref name="observer"/>. The observer must be one of the
        /// pipeline's sinks; the harness uses it to compute end-to-end
        /// latency.
        /// </summary>
        public PerfHarness(TelemetryPipeline pipeline, MemorySink observer)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }

        /// <summary>Target submission rate (envelopes per second).</summary>
        public int TargetEventsPerSec { get; set; } = 10_000;

        /// <summary>Total run duration.</summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>Number of producer tasks that share the load.</summary>
        public int ProducerCount { get; set; } = 1;

        /// <summary>Logical category attached to every emitted envelope.</summary>
        public string Category { get; set; } = "PerfHarness";

        /// <summary>
        /// Runs the harness and returns the recorded
        /// <see cref="PerfHarnessResult"/>. Cancellation aborts before
        /// the configured duration elapses.
        /// </summary>
        public async Task<PerfHarnessResult> RunAsync(CancellationToken cancellationToken = default)
        {
            if (TargetEventsPerSec <= 0)
            {
                throw new InvalidOperationException("TargetEventsPerSec must be positive.");
            }
            if (Duration <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("Duration must be positive.");
            }
            if (ProducerCount <= 0)
            {
                throw new InvalidOperationException("ProducerCount must be positive.");
            }

            int producers = ProducerCount;
            int perProducerRate = Math.Max(1, TargetEventsPerSec / producers);
            TimeSpan perEventDelay = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / perProducerRate);

            long submitted = 0;
            long droppedAtSubmit = 0;
            int peakQueueDepth = 0;

            DateTime startUtc = DateTime.UtcNow;
            Stopwatch sw = Stopwatch.StartNew();

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(Duration);

            Task[] producerTasks = new Task[producers];
            for (int p = 0; p < producers; p++)
            {
                int producerId = p;
                producerTasks[p] = Task.Run(async () =>
                {
                    long localCount = 0;
                    long lastDropSnapshot = _pipeline.DroppedCount;
                    while (!cts.IsCancellationRequested)
                    {
                        TelemetryEnvelope env = new TelemetryEnvelope
                        {
                            Kind = TelemetryKind.Log,
                            Level = BeepLogLevel.Information,
                            Category = Category,
                            Message = "perf",
                            TimestampUtc = DateTime.UtcNow,
                            Properties = new Dictionary<string, object>(2, StringComparer.Ordinal)
                            {
                                ["producer"] = producerId,
                                ["seq"] = localCount
                            }
                        };

                        _pipeline.SubmitLog(env);
                        Interlocked.Increment(ref submitted);
                        localCount++;

                        int depth = _pipeline.CurrentDepth;
                        int prevPeak;
                        do
                        {
                            prevPeak = Volatile.Read(ref peakQueueDepth);
                            if (depth <= prevPeak) { break; }
                        }
                        while (Interlocked.CompareExchange(ref peakQueueDepth, depth, prevPeak) != prevPeak);

                        long currentDrops = _pipeline.DroppedCount;
                        long delta = currentDrops - lastDropSnapshot;
                        if (delta > 0)
                        {
                            Interlocked.Add(ref droppedAtSubmit, delta);
                            lastDropSnapshot = currentDrops;
                        }

                        if (perEventDelay > TimeSpan.Zero)
                        {
                            try { await Task.Delay(perEventDelay, cts.Token).ConfigureAwait(false); }
                            catch (OperationCanceledException) { return; }
                        }
                    }
                }, cts.Token);
            }

            try { await Task.WhenAll(producerTasks).ConfigureAwait(false); }
            catch (OperationCanceledException) { /* expected on duration timeout */ }

            await _pipeline.FlushAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            sw.Stop();

            long observedCount = _observer.WrittenCount;
            DateTime stopUtc = DateTime.UtcNow;
            double elapsedSeconds = sw.Elapsed.TotalSeconds;
            double effectiveRate = elapsedSeconds > 0 ? observedCount / elapsedSeconds : 0;
            double submitRate = elapsedSeconds > 0 ? submitted / elapsedSeconds : 0;
            long lost = Math.Max(0, submitted - observedCount);
            double lossPct = submitted > 0 ? lost * 100.0 / submitted : 0;

            return new PerfHarnessResult
            {
                StartUtc = startUtc,
                StopUtc = stopUtc,
                Elapsed = sw.Elapsed,
                Submitted = Volatile.Read(ref submitted),
                Observed = observedCount,
                DroppedQueueFull = Volatile.Read(ref droppedAtSubmit),
                LostInPipeline = lost,
                LossPercent = lossPct,
                PeakQueueDepth = Volatile.Read(ref peakQueueDepth),
                EffectiveObservedRatePerSec = effectiveRate,
                EffectiveSubmitRatePerSec = submitRate,
                TargetRatePerSec = TargetEventsPerSec,
                ProducerCount = producers,
                LastFlushLatencyMs = _pipeline.Metrics?.LastFlushLatencyMs ?? 0
            };
        }
    }

    /// <summary>Result emitted by <see cref="PerfHarness.RunAsync"/>.</summary>
    public sealed class PerfHarnessResult
    {
        /// <summary>UTC timestamp when the run started.</summary>
        public DateTime StartUtc { get; set; }
        /// <summary>UTC timestamp when the run stopped.</summary>
        public DateTime StopUtc { get; set; }
        /// <summary>Wall-clock duration of the run.</summary>
        public TimeSpan Elapsed { get; set; }
        /// <summary>Envelopes submitted to the pipeline.</summary>
        public long Submitted { get; set; }
        /// <summary>Envelopes that reached the observer sink.</summary>
        public long Observed { get; set; }
        /// <summary>Envelopes dropped at the queue boundary (overflow).</summary>
        public long DroppedQueueFull { get; set; }
        /// <summary>Envelopes that did not reach the observer (drop + sink loss).</summary>
        public long LostInPipeline { get; set; }
        /// <summary>Loss percentage relative to submitted.</summary>
        public double LossPercent { get; set; }
        /// <summary>Maximum queue depth observed during the run.</summary>
        public int PeakQueueDepth { get; set; }
        /// <summary>Throughput observed at the sink.</summary>
        public double EffectiveObservedRatePerSec { get; set; }
        /// <summary>Producer-side throughput.</summary>
        public double EffectiveSubmitRatePerSec { get; set; }
        /// <summary>Configured target submission rate.</summary>
        public int TargetRatePerSec { get; set; }
        /// <summary>Number of producer tasks used.</summary>
        public int ProducerCount { get; set; }
        /// <summary>Last per-batch flush latency reported by the pipeline.</summary>
        public long LastFlushLatencyMs { get; set; }

        /// <summary>Renders the result as a single-line summary suitable for logs.</summary>
        public string ToSummary()
            => $"PerfHarness elapsed={Elapsed.TotalSeconds:F2}s "
             + $"submitted={Submitted} observed={Observed} dropped(queue)={DroppedQueueFull} "
             + $"lost={LostInPipeline} ({LossPercent:F2}%) peakQueue={PeakQueueDepth} "
             + $"submitRate={EffectiveSubmitRatePerSec:F0}/s observedRate={EffectiveObservedRatePerSec:F0}/s "
             + $"target={TargetRatePerSec}/s producers={ProducerCount} flushMs={LastFlushLatencyMs}";
    }
}
