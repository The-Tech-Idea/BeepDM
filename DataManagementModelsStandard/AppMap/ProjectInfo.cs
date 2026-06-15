using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.AppMap;

/// <summary>
/// Metadata about a discovered .NET project (parsed from .csproj).
/// </summary>
public sealed class ProjectInfo
{
    /// <summary>Project name (without .csproj extension).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the .csproj file.</summary>
    public string CsprojPath { get; set; } = string.Empty;

    /// <summary>Project directory (parent of .csproj).</summary>
    public string ProjectDirectory { get; set; } = string.Empty;

    /// <summary>Target framework moniker (e.g. net10.0).</summary>
    public string TargetFramework { get; set; } = string.Empty;

    /// <summary>SDK style (e.g. Microsoft.NET.Sdk.Web).</summary>
    public string? Sdk { get; set; }

    /// <summary>Output type: Exe, Library, WinExe.</summary>
    public string OutputType { get; set; } = "Library";

    /// <summary>Project type GUID from .sln (if known).</summary>
    public string? ProjectTypeGuid { get; set; }

    /// <summary>Package references with versions.</summary>
    public Dictionary<string, string> PackageReferences { get; set; } = new();

    /// <summary>Project references (names of other projects in the solution).</summary>
    public List<string> ProjectReferences { get; set; } = new();

    /// <summary>Root namespace from .csproj or convention.</summary>
    public string? RootNamespace { get; set; }

    /// <summary>Whether this project references test frameworks.</summary>
    public bool IsTestProject { get; set; }

    /// <summary>Data folders found by convention scan.</summary>
    public List<DataFolderInfo> DataFolders { get; set; } = new();
}

/// <summary>
/// A folder that likely contains data/entity classes.
/// </summary>
public sealed class DataFolderInfo
{
    public string Path { get; set; } = string.Empty;
    public bool HasDbContext { get; set; }
    public int FileCount { get; set; }
    public float Confidence { get; set; }
}
