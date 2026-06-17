using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Topological sort and cycle detection for trigger dependency graphs.
    /// Uses DFS-based algorithm on TriggerDefinition.DependsOn lists.
    /// </summary>
    public class TriggerDependencyManager : ITriggerDependencyManager
    {
        private int _maxDependencyDepth = 100;
        private TimeSpan _cycleDetectionTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Maximum depth of the dependency graph traversal. Prevents stack
        /// overflow on extremely deep or accidentally recursive dependency chains.
        /// Default: 100.
        /// </summary>
        public int MaxDependencyDepth
        {
            get => _maxDependencyDepth;
            set => _maxDependencyDepth = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Must be positive");
        }

        /// <summary>
        /// Maximum time allowed for cycle detection. If exceeded, the graph is
        /// assumed to be too complex to check and a warning is logged (the
        /// operation proceeds without cycle detection). Default: 5 seconds.
        /// </summary>
        public TimeSpan CycleDetectionTimeout
        {
            get => _cycleDetectionTimeout;
            set => _cycleDetectionTimeout = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value), "Must be positive");
        }
        /// <summary>Orders trigger definitions according to dependency requirements.</summary>
        /// <remarks>
        /// If a trigger's <see cref="TriggerDefinition.DependsOn"/> list references a
        /// <c>TriggerId</c> that is NOT present in <paramref name="triggers"/>, the
        /// dependency is logged as a warning and the trigger is still added to the
        /// result in its natural input order. The previous behavior silently dropped
        /// the missing dep and the dependent trigger ran without its prerequisite —
        /// a "fire and forget" that surfaced as a runtime no-op. The warning makes
        /// the misconfiguration visible to the operator.
        /// </remarks>
        public IReadOnlyList<TriggerDefinition> OrderByDependency(IReadOnlyList<TriggerDefinition> triggers)
        {
            if (triggers == null || triggers.Count == 0)
                return Array.Empty<TriggerDefinition>();

            var cycle = FindCycle(triggers);
            if (cycle.Count > 0)
                throw new InvalidOperationException(
                    $"Circular trigger dependency detected: {string.Join(" → ", cycle)}");

            var byId = triggers.ToDictionary(t => t.TriggerId, StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result  = new List<TriggerDefinition>();

            void Visit(TriggerDefinition t, int depth = 0)
            {
                if (depth > _maxDependencyDepth)
                {
                    Debug.WriteLine(
                        $"[TriggerDependencyManager] Max dependency depth ({_maxDependencyDepth}) " +
                        $"exceeded for '{t.TriggerName}'. Check for accidental recursion.");
                    return;
                }
                if (!visited.Add(t.TriggerId)) return;
                if (t.DependsOn != null)
                {
                    foreach (var depId in t.DependsOn)
                    {
                        if (string.IsNullOrEmpty(depId)) continue;
                        if (byId.TryGetValue(depId, out var dep))
                        {
                            Visit(dep, depth + 1);
                        }
                        else
                        {
                            // Missing dependency: log a warning so the operator can
                            // spot the misconfiguration. We do not throw because
                            // OrderByDependency is also called from non-critical
                            // paths (e.g. UI inspection), and a missing dep is
                            // usually a deployment issue rather than a hard error.
                            Debug.WriteLine(
                                $"[TriggerDependencyManager] Trigger '{t.TriggerName}' (id={t.TriggerId}) " +
                                $"depends on '{depId}', but no trigger with that id is in the input set. " +
                                "The dependent trigger will fire without its prerequisite.");
                        }
                    }
                }
                result.Add(t);
            }

            foreach (var t in triggers) Visit(t);
            return result;
        }

        /// <summary>Returns whether the trigger graph contains a circular dependency.</summary>
        public bool HasCircularDependency(IReadOnlyList<TriggerDefinition> triggers)
            => FindCycle(triggers).Count > 0;

        /// <summary>Returns the first detected trigger dependency cycle, if any.</summary>
        public IReadOnlyList<string> FindCycle(IReadOnlyList<TriggerDefinition> triggers)
        {
            if (triggers == null || triggers.Count == 0) return Array.Empty<string>();

            var byId   = triggers.ToDictionary(t => t.TriggerId, StringComparer.OrdinalIgnoreCase);
            var white  = new HashSet<string>(byId.Keys, StringComparer.OrdinalIgnoreCase);
            var grey   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack  = new List<string>();
            var deadline = DateTime.UtcNow + _cycleDetectionTimeout;

            bool Dfs(string id)
            {
                if (DateTime.UtcNow > deadline)
                {
                    Debug.WriteLine(
                        $"[TriggerDependencyManager] Cycle detection timed out after {_cycleDetectionTimeout.TotalSeconds:F0}s. " +
                        $"Graph has {triggers.Count} triggers. Skipping cycle check — proceeding without cycle detection.");
                    return false;
                }
                white.Remove(id);
                grey.Add(id);
                stack.Add(id);

                if (byId.TryGetValue(id, out var t))
                {
                    foreach (var depId in t.DependsOn ?? new List<string>())
                    {
                        if (!byId.ContainsKey(depId)) continue;
                        if (grey.Contains(depId))
                        {
                            // Close the cycle in the stack
                            var idx = stack.IndexOf(depId);
                            if (idx >= 0) stack.Add(depId);
                            return true;
                        }
                        if (white.Contains(depId) && Dfs(depId)) return true;
                    }
                }

                grey.Remove(id);
                stack.RemoveAt(stack.Count - 1);
                return false;
            }

            foreach (var id in new List<string>(white))
            {
                stack.Clear();
                if (Dfs(id))
                    // Return the trigger names for readability
                    return stack.Select(s => byId.TryGetValue(s, out var td)
                        ? (td.TriggerName ?? td.TriggerId)
                        : s).ToList();
            }

            return Array.Empty<string>();
        }
    }
}
