using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using AppMapModel = TheTechIdea.Beep.AppMap.AppMap;

namespace TheTechIdea.Beep.Services.AppMap.ControlPanel;

/// <summary>
/// Aggregates AppMap, Environment, and health-check data into a live
/// solution snapshot. Provides environment-wide switching across all
/// projects in one operation.
/// </summary>
public interface ISolutionControlService
{
    /// <summary>
    /// Build a live snapshot of the entire solution in a specific environment.
    /// </summary>
    Task<SolutionSnapshot> GetSnapshotAsync(AppMapModel appMap, string environmentId, CancellationToken token = default);

    /// <summary>
    /// Run health checks against all project servers and databases.
    /// Updates the snapshot with live health status.
    /// </summary>
    Task<SolutionSnapshot> HealthCheckAllAsync(AppMapModel appMap, string environmentId, CancellationToken token = default);

    /// <summary>
    /// Switch ALL projects from one environment to another.
    /// Returns a diff plan showing what changes.
    /// </summary>
    Task<EnvironmentSwitchPlan> SwitchEnvironmentAsync(AppMapModel appMap, string fromEnv, string toEnv, CancellationToken token = default);

    /// <summary>
    /// Build a project→dependency adjacency map.
    /// </summary>
    Task<Dictionary<string, List<string>>> GetDependencyMapAsync(AppMapModel appMap, CancellationToken token = default);
}
