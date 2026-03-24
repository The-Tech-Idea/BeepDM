using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Tracks which schedules have completed and which are now unblocked
    /// based on their declared dependencies.
    /// Completion records are scoped to today's UTC date window (auto-reset at midnight).
    /// Supports deadlock detection, max-wait timeout, fail-fast propagation, and circuit breaker awareness.
    /// </summary>
    public sealed class DependencyGraph
    {
        // scheduleId → which scheduleIds it waits for
        private readonly Dictionary<string, HashSet<string>> _deps
            = new(StringComparer.Ordinal);

        // scheduleId → condition string: "ALL_SUCCESS" | "ANY_SUCCESS" | "ALL_COMPLETE"
        private readonly Dictionary<string, string> _conditions
            = new(StringComparer.Ordinal);

        // scheduleId → (success, completedAt) for today's window
        private readonly Dictionary<string, (bool Success, DateTime CompletedAt)> _completions
            = new(StringComparer.Ordinal);

        // Track which schedules were already dispatched in this window to avoid double-firing
        private readonly HashSet<string> _fired = new(StringComparer.Ordinal);

        // scheduleId → max-wait timeout in seconds (0 = no timeout)
        private readonly Dictionary<string, int> _maxWaitSeconds
            = new(StringComparer.Ordinal);

        // scheduleId → UTC time when the schedule started waiting for dependencies
        private readonly Dictionary<string, DateTime> _waitStartTimes
            = new(StringComparer.Ordinal);

        // scheduleId → set of failed upstream IDs (for fail-fast propagation)
        private readonly Dictionary<string, HashSet<string>> _failedUpstreams
            = new(StringComparer.Ordinal);

        // scheduleIds that are currently circuit-breaker tripped (should not be dispatched)
        private readonly HashSet<string> _circuitBrokenIds = new(StringComparer.Ordinal);

        private readonly object _lock = new();

        /// <summary>
        /// Register or update the dependency list for <paramref name="scheduleId"/>.
        /// </summary>
        public void RegisterDependency(string scheduleId, IEnumerable<string> dependsOnIds,
            string condition = "ALL_SUCCESS")
        {
            lock (_lock)
            {
                _deps[scheduleId]       = new HashSet<string>(dependsOnIds, StringComparer.Ordinal);
                _conditions[scheduleId] = condition;
                if (!_waitStartTimes.ContainsKey(scheduleId))
                    _waitStartTimes[scheduleId] = DateTime.UtcNow;
            }
        }

        /// <summary>Set the maximum wait time in seconds for a schedule's dependencies.</summary>
        public void SetMaxWait(string scheduleId, int maxWaitSeconds)
        {
            lock (_lock)
                _maxWaitSeconds[scheduleId] = maxWaitSeconds;
        }

        /// <summary>Mark a schedule as circuit-breaker tripped. It will be excluded from unblocked results.</summary>
        public void SetCircuitBroken(string scheduleId, bool broken)
        {
            lock (_lock)
            {
                if (broken) _circuitBrokenIds.Add(scheduleId);
                else _circuitBrokenIds.Remove(scheduleId);
            }
        }

        /// <summary>Record that <paramref name="scheduleId"/> completed.</summary>
        public void NotifyCompletion(string scheduleId, bool success, DateTime completedAt)
        {
            lock (_lock)
                _completions[scheduleId] = (success, completedAt);
        }

        /// <summary>
        /// Returns schedule IDs whose dependencies are now all satisfied (within today's UTC window).
        /// Each unblocked ID is returned only once per day.
        /// Excludes circuit-broken schedules and propagates fail-fast for ALL_SUCCESS failures.
        /// </summary>
        public IReadOnlyList<string> GetUnblockedSchedules()
        {
            var result   = new List<string>();
            var today    = DateTime.UtcNow.Date;

            lock (_lock)
            {
                foreach (var (schedId, deps) in _deps)
                {
                    if (deps.Count == 0) continue;
                    if (_fired.Contains(schedId)) continue;
                    if (_circuitBrokenIds.Contains(schedId)) continue;

                    string condition = _conditions.TryGetValue(schedId, out var c)
                        ? c : "ALL_SUCCESS";

                    bool unblocked = condition switch
                    {
                        "ANY_SUCCESS"  => deps.Any(d =>
                            _completions.TryGetValue(d, out var r) &&
                            r.Success && r.CompletedAt.Date == today),

                        "ALL_COMPLETE" => deps.All(d =>
                            _completions.TryGetValue(d, out var r) &&
                            r.CompletedAt.Date == today),

                        _ => deps.All(d =>          // ALL_SUCCESS (default)
                            _completions.TryGetValue(d, out var r) &&
                            r.Success && r.CompletedAt.Date == today)
                    };

                    if (unblocked)
                    {
                        result.Add(schedId);
                        _fired.Add(schedId);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns schedule IDs whose upstream dependencies have fatally failed and cannot be satisfied.
        /// For ALL_SUCCESS: any upstream failure means the dependent can never succeed today.
        /// These should be marked as failed-fast.
        /// </summary>
        public IReadOnlyList<string> GetFailFastSchedules()
        {
            var result = new List<string>();
            var today  = DateTime.UtcNow.Date;

            lock (_lock)
            {
                foreach (var (schedId, deps) in _deps)
                {
                    if (deps.Count == 0) continue;
                    if (_fired.Contains(schedId)) continue;
                    if (_failedUpstreams.ContainsKey(schedId)) continue;

                    string condition = _conditions.TryGetValue(schedId, out var c)
                        ? c : "ALL_SUCCESS";

                    // For ALL_SUCCESS: if any dependency failed today, this schedule can never unblock
                    if (condition == "ALL_SUCCESS")
                    {
                        var failedDeps = deps
                            .Where(d => _completions.TryGetValue(d, out var r) &&
                                        !r.Success && r.CompletedAt.Date == today)
                            .ToHashSet(StringComparer.Ordinal);

                        if (failedDeps.Count > 0)
                        {
                            _failedUpstreams[schedId] = failedDeps;
                            result.Add(schedId);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns schedule IDs that have exceeded their max-wait time while still waiting for dependencies.
        /// </summary>
        public IReadOnlyList<string> GetTimedOutSchedules()
        {
            var result = new List<string>();
            var now    = DateTime.UtcNow;

            lock (_lock)
            {
                foreach (var (schedId, deps) in _deps)
                {
                    if (deps.Count == 0) continue;
                    if (_fired.Contains(schedId)) continue;
                    if (_failedUpstreams.ContainsKey(schedId)) continue;

                    if (_maxWaitSeconds.TryGetValue(schedId, out var maxWait) && maxWait > 0 &&
                        _waitStartTimes.TryGetValue(schedId, out var startTime))
                    {
                        if ((now - startTime).TotalSeconds > maxWait)
                            result.Add(schedId);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Detects cycles in the dependency graph (deadlock detection).
        /// Returns the IDs involved in the first cycle found, or empty if none.
        /// </summary>
        public IReadOnlyList<string> DetectCycles()
        {
            lock (_lock)
            {
                // Topological sort via DFS with coloring:
                // white=unvisited, gray=in-progress, black=done
                var white = new HashSet<string>(_deps.Keys, StringComparer.Ordinal);
                var gray  = new HashSet<string>(StringComparer.Ordinal);
                var stack = new List<string>();

                foreach (var node in _deps.Keys)
                {
                    if (!white.Contains(node)) continue;

                    var cycleNodes = DfsFindCycle(node, white, gray, stack);
                    if (cycleNodes != null) return cycleNodes;
                }
            }
            return Array.Empty<string>();
        }

        private List<string>? DfsFindCycle(string node, HashSet<string> white,
            HashSet<string> gray, List<string> stack)
        {
            white.Remove(node);
            gray.Add(node);
            stack.Add(node);

            if (_deps.TryGetValue(node, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (gray.Contains(neighbor))
                    {
                        // Found cycle — extract the cycle portion
                        int idx = stack.IndexOf(neighbor);
                        return stack.GetRange(idx, stack.Count - idx);
                    }

                    if (white.Contains(neighbor))
                    {
                        var result = DfsFindCycle(neighbor, white, gray, stack);
                        if (result != null) return result;
                    }
                }
            }

            gray.Remove(node);
            stack.RemoveAt(stack.Count - 1);
            return null;
        }

        /// <summary>
        /// Returns all downstream schedule IDs that transitively depend on <paramref name="scheduleId"/>.
        /// Useful for impact analysis when a schedule fails.
        /// </summary>
        public IReadOnlyList<string> GetDownstreamDependents(string scheduleId)
        {
            var result  = new List<string>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue   = new Queue<string>();
            queue.Enqueue(scheduleId);

            lock (_lock)
            {
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var (id, deps) in _deps)
                    {
                        if (deps.Contains(current) && visited.Add(id))
                        {
                            result.Add(id);
                            queue.Enqueue(id);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>Reset the day window (clear completions and fired set). Call at midnight.</summary>
        public void ResetDayWindow()
        {
            lock (_lock)
            {
                _completions.Clear();
                _fired.Clear();
                _failedUpstreams.Clear();
                _waitStartTimes.Clear();
                // Re-initialize wait start times
                foreach (var schedId in _deps.Keys)
                    _waitStartTimes[schedId] = DateTime.UtcNow;
            }
        }
    }
}
