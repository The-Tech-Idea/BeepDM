using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Thread-safe in-memory priority queue for <see cref="QueuedRun"/> items with workload class isolation.
    /// Lower <see cref="QueuedRun.Priority"/> number = higher dispatch priority.
    /// Within the same priority, earlier <see cref="QueuedRun.TriggeredAtUtc"/> is dequeued first (FIFO).
    /// Workload classes (critical, standard, backfill) are dispatched in priority order,
    /// with configurable concurrency quotas per class to prevent backfill starvation of live runs.
    /// </summary>
    public sealed class PipelineRunQueue
    {
        // Per-class queues for isolation
        private readonly Dictionary<string, PriorityQueue<QueuedRun, (int Priority, DateTime TriggeredAt)>> _classQueues = new(StringComparer.OrdinalIgnoreCase);

        // Class dispatch order: critical → standard → backfill
        private static readonly string[] ClassOrder = { "critical", "standard", "backfill" };

        // Per-class max concurrent runs (0 = unlimited)
        private readonly Dictionary<string, int> _classConcurrencyLimits = new(StringComparer.OrdinalIgnoreCase)
        {
            ["critical"] = 0,
            ["standard"] = 0,
            ["backfill"] = 2
        };

        // Per-class active run counts
        private readonly Dictionary<string, int> _classActiveCounts = new(StringComparer.OrdinalIgnoreCase);

        private readonly SemaphoreSlim _signal = new(0);
        private readonly object        _lock   = new();

        /// <summary>Number of runs currently waiting across all classes.</summary>
        public int Count
        {
            get
            {
                lock (_lock)
                    return _classQueues.Values.Sum(q => q.Count);
            }
        }

        /// <summary>Number of runs waiting in a specific workload class.</summary>
        public int CountByClass(string workloadClass)
        {
            lock (_lock)
                return _classQueues.TryGetValue(workloadClass, out var q) ? q.Count : 0;
        }

        /// <summary>Set the maximum concurrent dispatch limit for a workload class. 0 = unlimited.</summary>
        public void SetClassConcurrencyLimit(string workloadClass, int maxConcurrent)
        {
            lock (_lock)
                _classConcurrencyLimits[workloadClass] = maxConcurrent;
        }

        /// <summary>Enqueue a run for dispatch into its workload class queue.</summary>
        public void Enqueue(QueuedRun run)
        {
            lock (_lock)
            {
                var cls = string.IsNullOrEmpty(run.WorkloadClass) ? "standard" : run.WorkloadClass;
                if (!_classQueues.TryGetValue(cls, out var queue))
                {
                    queue = new PriorityQueue<QueuedRun, (int Priority, DateTime TriggeredAt)>(
                        Comparer<(int, DateTime)>.Create((a, b) =>
                        {
                            int c = a.Item1.CompareTo(b.Item1);
                            return c != 0 ? c : a.Item2.CompareTo(b.Item2);
                        }));
                    _classQueues[cls] = queue;
                }
                queue.Enqueue(run, (run.Priority, run.TriggeredAtUtc));
            }
            _signal.Release();
        }

        /// <summary>
        /// Awaits the next available run, dispatching in workload class priority order.
        /// Critical runs are dispatched first, then standard, then backfill.
        /// Respects per-class concurrency limits.
        /// </summary>
        public async Task<QueuedRun> DequeueAsync(CancellationToken token)
        {
            while (true)
            {
                await _signal.WaitAsync(token).ConfigureAwait(false);
                lock (_lock)
                {
                    // Try each class in priority order
                    foreach (var cls in ClassOrder)
                    {
                        if (!_classQueues.TryGetValue(cls, out var queue) || queue.Count == 0)
                            continue;

                        // Check concurrency limit for this class
                        if (_classConcurrencyLimits.TryGetValue(cls, out var limit) && limit > 0)
                        {
                            _classActiveCounts.TryGetValue(cls, out var active);
                            if (active >= limit) continue;
                        }

                        if (queue.TryDequeue(out var run, out _))
                        {
                            _classActiveCounts.TryGetValue(run.WorkloadClass ?? "standard", out var count);
                            _classActiveCounts[run.WorkloadClass ?? "standard"] = count + 1;
                            return run;
                        }
                    }

                    // Also try any custom classes not in ClassOrder
                    foreach (var (cls, queue) in _classQueues)
                    {
                        if (Array.IndexOf(ClassOrder, cls.ToLowerInvariant()) >= 0) continue;
                        if (queue.Count == 0) continue;

                        if (queue.TryDequeue(out var run, out _))
                        {
                            _classActiveCounts.TryGetValue(cls, out var count);
                            _classActiveCounts[cls] = count + 1;
                            return run;
                        }
                    }
                }
                // No dispatchable run found (all classes at concurrency limit) — wait for next signal
            }
        }

        /// <summary>
        /// Notify the queue that a run from a workload class has finished,
        /// freeing a concurrency slot for that class.
        /// </summary>
        public void NotifyRunCompleted(string workloadClass)
        {
            lock (_lock)
            {
                var cls = string.IsNullOrEmpty(workloadClass) ? "standard" : workloadClass;
                if (_classActiveCounts.TryGetValue(cls, out var count) && count > 0)
                    _classActiveCounts[cls] = count - 1;
            }
            // Signal in case a run was waiting for a class slot to free up
            _signal.Release();
        }
    }
}
