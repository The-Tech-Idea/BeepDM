using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Tracks which schedules have completed and which are now unblocked
    /// based on their declared dependencies.
    /// Completion records are scoped to today's UTC date window (auto-reset at midnight).
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

        /// <summary>Reset the day window (clear completions and fired set). Call at midnight.</summary>
        public void ResetDayWindow()
        {
            lock (_lock)
            {
                _completions.Clear();
                _fired.Clear();
            }
        }
    }
}
