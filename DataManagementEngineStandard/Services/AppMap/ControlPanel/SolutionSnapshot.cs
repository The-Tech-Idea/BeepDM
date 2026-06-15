using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.AppMap;

namespace TheTechIdea.Beep.Services.AppMap.ControlPanel;

/// <summary>
/// Aggregated live snapshot of an entire solution.
/// </summary>
public sealed class SolutionSnapshot
{
    /// <summary>The AppMap this snapshot is based on.</summary>
    public string AppMapId { get; set; } = string.Empty;

    /// <summary>Solution name.</summary>
    public string SolutionName { get; set; } = string.Empty;

    /// <summary>Current active environment.</summary>
    public string ActiveEnvironmentId { get; set; } = string.Empty;

    /// <summary>Runtime status of all projects.</summary>
    public List<ProjectRuntimeStatus> Projects { get; set; } = new();

    /// <summary>Database versions per datasource.</summary>
    public Dictionary<string, string> DatabaseVersions { get; set; } = new();

    /// <summary>Dependency map (project → depends on).</summary>
    public Dictionary<string, List<string>> DependencyMap { get; set; } = new();

    /// <summary>When this snapshot was taken.</summary>
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;

    /// <summary>Overall health.</summary>
    public HealthStatus OverallHealth
    {
        get
        {
            if (Projects.Count == 0) return HealthStatus.Unknown;
            if (Projects.All(p => p.IsRunning)) return HealthStatus.Healthy;
            if (Projects.Any(p => p.IsRunning)) return HealthStatus.Degraded;
            return HealthStatus.Unhealthy;
        }
    }
}
