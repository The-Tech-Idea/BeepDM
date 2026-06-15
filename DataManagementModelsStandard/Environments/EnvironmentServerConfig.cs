using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
namespace TheTechIdea.Beep.Environments;

/// <summary>
/// Server configuration for a specific environment.
/// </summary>
public sealed class EnvironmentServerConfig
{
    /// <summary>Type of server.</summary>
    public ServerType ServerType { get; set; }

    /// <summary>Base URL (e.g. https://localhost:5001).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Health check endpoint (relative to URL).</summary>
    public string? HealthCheckEndpoint { get; set; }

    /// <summary>Whether this server is required for the environment to be healthy.</summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>Additional metadata.</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
