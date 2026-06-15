using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.Editor.BeepSync;

/// <summary>
/// Maps a project to a specific datasource in a given environment.
/// </summary>
public sealed class ProjectDataSourceBinding
{
    /// <summary>Project name.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>Environment ID.</summary>
    public string EnvironmentId { get; set; } = string.Empty;

    /// <summary>Datasource name (as registered in BeepDM).</summary>
    public string DatasourceName { get; set; } = string.Empty;

    /// <summary>Whether this is the primary datasource for the project.</summary>
    public bool IsPrimary { get; set; } = true;

    /// <summary>Whether this datasource is read-only for this project.</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>When this binding was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When this binding was last modified.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
