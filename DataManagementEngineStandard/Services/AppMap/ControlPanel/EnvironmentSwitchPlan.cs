using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
namespace TheTechIdea.Beep.Services.AppMap.ControlPanel;

/// <summary>
/// Describes what changes when switching all projects from one environment to another.
/// </summary>
public sealed class EnvironmentSwitchPlan
{
    /// <summary>Source environment ID.</summary>
    public string FromEnvironment { get; set; } = string.Empty;

    /// <summary>Target environment ID.</summary>
    public string ToEnvironment { get; set; } = string.Empty;

    /// <summary>Per-project changes.</summary>
    public List<EnvironmentSwitchChange> Changes { get; set; } = new();

    /// <summary>Total number of servers being switched.</summary>
    public int ChangeCount => Changes.Sum(c => c.ServerChanges.Count);

    /// <summary>Whether any production environment is involved.</summary>
    public bool InvolvesProduction { get; set; }
}

/// <summary>
/// A single project's environment switch change.
/// </summary>
public sealed class EnvironmentSwitchChange
{
    public string ProjectName { get; set; } = string.Empty;
    public List<ServerUrlChange> ServerChanges { get; set; } = new();
}

/// <summary>
/// Old → new URL for a server.
/// </summary>
public sealed class ServerUrlChange
{
    public string ServerType { get; set; } = string.Empty;
    public string OldUrl { get; set; } = string.Empty;
    public string NewUrl { get; set; } = string.Empty;
}
