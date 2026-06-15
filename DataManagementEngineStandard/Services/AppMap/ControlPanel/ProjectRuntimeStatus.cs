using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Environments;

namespace TheTechIdea.Beep.Services.AppMap.ControlPanel;

/// <summary>
/// Runtime status of a project in a specific environment.
/// </summary>
public sealed class ProjectRuntimeStatus
{
    /// <summary>Project name.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>Server type.</summary>
    public ServerType ServerType { get; set; }

    /// <summary>Base URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Health check result.</summary>
    public HealthCheckResult? Health { get; set; }

    /// <summary>Whether the server is reachable.</summary>
    public bool IsRunning => Health?.IsHealthy ?? false;

    /// <summary>When status was last checked.</summary>
    public DateTime LastCheckedAt { get; set; }
}
