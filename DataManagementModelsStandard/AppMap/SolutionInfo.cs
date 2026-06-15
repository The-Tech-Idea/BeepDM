using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.AppMap;

/// <summary>
/// Information about a discovered .sln file.
/// </summary>
public sealed class SolutionInfo
{
    /// <summary>Solution name (without .sln extension).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the .sln file.</summary>
    public string SlnPath { get; set; } = string.Empty;

    /// <summary>Solution format version.</summary>
    public string FormatVersion { get; set; } = string.Empty;

    /// <summary>Projects found in the solution.</summary>
    public List<ProjectInfo> Projects { get; set; } = new();

    /// <summary>Solution configurations (Debug, Release, etc.).</summary>
    public List<string> Configurations { get; set; } = new();

    /// <summary>When the solution was discovered.</summary>
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}
