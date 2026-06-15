using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.AppMap;

/// <summary>
/// Adjacency list representing project→project dependency relationships.
/// </summary>
public sealed class ProjectDependencyGraph
{
    /// <summary>Adjacency: project name → list of project names it depends on.</summary>
    public Dictionary<string, List<string>> Adjacency { get; set; } = new();

    /// <summary>Total number of nodes (projects).</summary>
    public int NodeCount => Adjacency.Count;

    /// <summary>Total number of edges (dependencies).</summary>
    public int EdgeCount => Adjacency.Values.Sum(v => v.Count);

    /// <summary>Projects with no incoming edges (root projects).</summary>
    public List<string> GetRootProjects()
    {
        var allReferenced = Adjacency.Values.SelectMany(v => v).ToHashSet();
        return Adjacency.Keys.Where(k => !allReferenced.Contains(k)).ToList();
    }

    /// <summary>Projects with no outgoing edges (leaf projects).</summary>
    public List<string> GetLeafProjects() =>
        Adjacency.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();

    /// <summary>Detect circular dependencies.</summary>
    public List<List<string>> FindCycles()
    {
        var cycles = new List<List<string>>();
        var visited = new HashSet<string>();
        var stack = new HashSet<string>();

        foreach (var node in Adjacency.Keys)
        {
            if (!visited.Contains(node))
                DfsCycle(node, visited, stack, new List<string>(), cycles);
        }
        return cycles;
    }

    private void DfsCycle(string node, HashSet<string> visited, HashSet<string> stack,
        List<string> path, List<List<string>> cycles)
    {
        visited.Add(node);
        stack.Add(node);
        path.Add(node);

        if (Adjacency.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                    DfsCycle(neighbor, visited, stack, path, cycles);
                else if (stack.Contains(neighbor))
                {
                    var cycleStart = path.IndexOf(neighbor);
                    cycles.Add(path.Skip(cycleStart).ToList());
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        stack.Remove(node);
    }
}
