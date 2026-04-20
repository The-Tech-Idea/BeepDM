using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Observability
{
    /// <summary>
    /// Rolling-window p95 latency detector used by
    /// <see cref="DistributedMetricsAggregator"/>. A shard whose
    /// p95 latency stays above <see cref="P95ThresholdMs"/> for
    /// <see cref="ConsecutiveWindows"/> successive windows is
    /// flagged as "hot" and reported via
    /// <see cref="OnHotShardDetected"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The detector keeps a bounded ring of recent latencies per
    /// shard (<see cref="WindowSize"/> samples). When the window
    /// is full it computes an approximate p95 by sorting a copy
    /// of the buffer; for realistic per-shard cardinalities
    /// (<c>≤ few dozen shards</c> and <c>≤ 2048</c> samples) the
    /// cost is a few hundred microseconds per sample and safely
    /// runs on the hot path.
    /// </para>
    /// <para>
    /// The detector never throws: a faulty event handler cannot
    /// break the recording path.
    /// </para>
    /// </remarks>
    public sealed class HotShardDetector
    {
        private readonly ConcurrentDictionary<string, RollingWindow> _windows
            = new ConcurrentDictionary<string, RollingWindow>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, int> _breachCount
            = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Samples kept per shard (default 256).</summary>
        public int WindowSize { get; }

        /// <summary>p95 threshold (ms) that triggers breach counting.</summary>
        public double P95ThresholdMs { get; }

        /// <summary>Consecutive-window breaches required before firing.</summary>
        public int ConsecutiveWindows { get; }

        /// <summary>Raised when a shard breaches the threshold.</summary>
        public event EventHandler<HotShardEventArgs> OnHotShardDetected;

        /// <summary>Creates a new detector with explicit tuning.</summary>
        public HotShardDetector(
            int    windowSize         = 256,
            double p95ThresholdMs     = 500.0,
            int    consecutiveWindows = 3)
        {
            if (windowSize         < 8)  windowSize         = 8;
            if (consecutiveWindows < 1)  consecutiveWindows = 1;
            if (p95ThresholdMs     < 1)  p95ThresholdMs     = 1;

            WindowSize         = windowSize;
            P95ThresholdMs     = p95ThresholdMs;
            ConsecutiveWindows = consecutiveWindows;
        }

        /// <summary>Records one latency sample for the given shard.</summary>
        public void RecordLatency(string shardId, double latencyMs)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return;

            var window = _windows.GetOrAdd(shardId, _ => new RollingWindow(WindowSize));
            bool windowFull;
            double p95;
            lock (window)
            {
                window.Add(latencyMs);
                if (!window.IsFull)
                {
                    return;
                }
                windowFull = true;
                p95 = window.ComputeP95();
            }

            if (!windowFull) return;

            if (p95 >= P95ThresholdMs)
            {
                int consecutive = _breachCount.AddOrUpdate(shardId, 1, (_, prev) => prev + 1);
                if (consecutive >= ConsecutiveWindows)
                {
                    RaiseHot(shardId, p95, consecutive);
                }
            }
            else
            {
                _breachCount[shardId] = 0;
            }
        }

        /// <summary>Returns current p95 for a shard (0 if not enough samples).</summary>
        public double GetP95(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return 0.0;
            if (!_windows.TryGetValue(shardId, out var w)) return 0.0;
            lock (w) return w.IsFull ? w.ComputeP95() : 0.0;
        }

        /// <summary>Returns the current average latency for a shard.</summary>
        public double GetAverage(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return 0.0;
            if (!_windows.TryGetValue(shardId, out var w)) return 0.0;
            lock (w) return w.ComputeAverage();
        }

        private void RaiseHot(string shardId, double p95, int consecutive)
        {
            var handler = OnHotShardDetected;
            if (handler == null) return;
            try
            {
                handler(this, new HotShardEventArgs(
                    shardId:            shardId,
                    p95LatencyMs:       p95,
                    thresholdMs:        P95ThresholdMs,
                    consecutiveWindows: consecutive,
                    observedAtUtc:      DateTime.UtcNow));
            }
            catch
            {
                // Guard the hot path: detectors never surface handler faults.
            }
        }

        private sealed class RollingWindow
        {
            private readonly double[] _buffer;
            private int _cursor;
            private int _count;

            public RollingWindow(int capacity)
            {
                _buffer = new double[capacity];
            }

            public bool IsFull => _count >= _buffer.Length;

            public void Add(double value)
            {
                _buffer[_cursor] = value;
                _cursor = (_cursor + 1) % _buffer.Length;
                if (_count < _buffer.Length) _count++;
            }

            public double ComputeP95()
            {
                int n = _count;
                if (n == 0) return 0.0;
                var copy = new double[n];
                Array.Copy(_buffer, copy, n);
                Array.Sort(copy);
                int idx = (int)Math.Ceiling(0.95 * n) - 1;
                if (idx < 0) idx = 0;
                if (idx >= n) idx = n - 1;
                return copy[idx];
            }

            public double ComputeAverage()
            {
                int n = _count;
                if (n == 0) return 0.0;
                double sum = 0.0;
                for (int i = 0; i < n; i++) sum += _buffer[i];
                return sum / n;
            }
        }
    }
}
