using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.ConfigUtil;

/// <summary>
/// Records an application version deployment.
/// </summary>
public sealed class AppVersion
{
    /// <summary>Semantic version.</summary>
    public string Version { get; set; } = "0.0.0";

    /// <summary>When this version was built.</summary>
    public DateTime BuildDate { get; set; }

    /// <summary>Git commit SHA at build time.</summary>
    public string? GitSha { get; set; }

    /// <summary>CI/CD build ID.</summary>
    public string? BuildId { get; set; }

    /// <summary>Environments where this version is deployed.</summary>
    public List<string> DeployedEnvironments { get; set; } = new();

    /// <summary>Release notes or changelog.</summary>
    public string? ReleaseNotes { get; set; }

    /// <summary>Minimum database version required.</summary>
    public string? MinDatabaseVersion { get; set; }
}
