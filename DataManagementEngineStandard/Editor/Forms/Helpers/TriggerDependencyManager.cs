using System;
using System.Collections.Generic;
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

            void Visit(TriggerDefinition t)
            {
                if (!visited.Add(t.TriggerId)) return;
                foreach (var depId in t.DependsOn ?? new List<string>())
                    if (byId.TryGetValue(depId, out var dep))
                        Visit(dep);
                result.Add(t);
            }

            foreach (var t in triggers) Visit(t);
            return result;
        }

        public bool HasCircularDependency(IReadOnlyList<TriggerDefinition> triggers)
            => FindCycle(triggers).Count > 0;

        public IReadOnlyList<string> FindCycle(IReadOnlyList<TriggerDefinition> triggers)
        {
            if (triggers == null || triggers.Count == 0) return Array.Empty<string>();

            var byId   = triggers.ToDictionary(t => t.TriggerId, StringComparer.OrdinalIgnoreCase);
            var white  = new HashSet<string>(byId.Keys, StringComparer.OrdinalIgnoreCase);
            var grey   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack  = new List<string>();

            bool Dfs(string id)
            {
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
