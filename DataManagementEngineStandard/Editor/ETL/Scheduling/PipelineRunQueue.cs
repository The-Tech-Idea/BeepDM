using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Thread-safe in-memory priority queue for <see cref="QueuedRun"/> items.
    /// Lower <see cref="QueuedRun.Priority"/> number = higher dispatch priority.
    /// Within the same priority, earlier <see cref="QueuedRun.TriggeredAtUtc"/> is dequeued first (FIFO).
    /// </summary>
    public sealed class PipelineRunQueue
    {
        private readonly PriorityQueue<QueuedRun, (int Priority, DateTime TriggeredAt)> _heap
            = new(Comparer<(int, DateTime)>.Create((a, b) =>
            {
                int c = a.Item1.CompareTo(b.Item1);
                return c != 0 ? c : a.Item2.CompareTo(b.Item2);
            }));

        private readonly SemaphoreSlim _signal = new(0);
        private readonly object        _lock   = new();

        /// <summary>Number of runs currently waiting.</summary>
        public int Count { get { lock (_lock) return _heap.Count; } }

        /// <summary>Enqueue a run for dispatch.</summary>
        public void Enqueue(QueuedRun run)
        {
            lock (_lock)
                _heap.Enqueue(run, (run.Priority, run.TriggeredAtUtc));
            _signal.Release();
        }

        /// <summary>Awaits the next available run in priority order.</summary>
        public async Task<QueuedRun> DequeueAsync(CancellationToken token)
        {
            while (true)
            {
                await _signal.WaitAsync(token).ConfigureAwait(false);
                lock (_lock)
                {
                    if (_heap.TryDequeue(out var run, out _))
                        return run;
                }
            }
        }
    }
}
