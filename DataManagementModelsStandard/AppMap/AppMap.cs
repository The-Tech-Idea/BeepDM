using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Environments;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.AppMap;

/// <summary>
/// Top-level model representing an entire multi-project solution map.
/// Aggregates solution info, role assignments, environments, and versions.
/// </summary>
public sealed class AppMap
{
    /// <summary>Unique identifier for this AppMap.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];

    /// <summary>Solution discovery info.</summary>
    public SolutionInfo Solution { get; set; } = null!;

    /// <summary>Projects with their assigned roles.</summary>
    public List<ProjectRoleAssignment> Projects { get; set; } = new();

    /// <summary>All known environment tiers.</summary>
    public List<AppEnvironment> Environments { get; set; } = new();

    /// <summary>Known database versions.</summary>
    public List<DatabaseVersion> DatabaseVersions { get; set; } = new();

    /// <summary>Known application versions.</summary>
    public List<AppVersion> AppVersions { get; set; } = new();

    /// <summary>When this AppMap was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When this AppMap was last modified.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Schema version of the AppMap format.</summary>
    public int SchemaVersion { get; set; } = 1;

    // -- Helpers --

    public List<ProjectRoleAssignment> GetProjectsByRole(ProjectRole role) =>
        Projects.Where(p => p.Role == role).ToList();

    public ProjectRoleAssignment? GetProject(string name) =>
        Projects.FirstOrDefault(p => p.Project.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public List<ProjectRoleAssignment> GetDataProjects() =>
        GetProjectsByRole(ProjectRole.Data);

    public List<ProjectRoleAssignment> GetApiProjects() =>
        GetProjectsByRole(ProjectRole.Api);

    public List<ProjectRoleAssignment> GetWebProjects() =>
        GetProjectsByRole(ProjectRole.Web);

    public List<ProjectRoleAssignment> GetIdentityServerProjects() =>
        GetProjectsByRole(ProjectRole.IdentityServer);

    public int ProjectCount => Projects.Count;
    public int DataProjectCount => GetDataProjects().Count;
    public int ApiProjectCount => GetApiProjects().Count;
    public int WebProjectCount => GetWebProjects().Count;
}
