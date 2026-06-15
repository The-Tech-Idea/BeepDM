using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.Environments;

/// <summary>
/// Links a project to an environment with its server configs.
/// </summary>
public sealed class ProjectEnvironmentProfile
{
    /// <summary>The project this profile belongs to.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>The environment this profile is for.</summary>
    public string EnvironmentId { get; set; } = string.Empty;

    /// <summary>Server configurations for this project in this environment.</summary>
    public List<EnvironmentServerConfig> Servers { get; set; } = new();

    /// <summary>Configuration overrides for this project in this environment.</summary>
    public List<EnvironmentConfigOverride> Overrides { get; set; } = new();

    /// <summary>Whether this is the primary profile for the project.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>When this profile was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When this profile was last modified.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
