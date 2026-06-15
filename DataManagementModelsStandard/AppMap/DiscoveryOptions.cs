using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.AppMap;

/// <summary>
/// Options for solution/project discovery scanning.
/// </summary>
public sealed class DiscoveryOptions
{
    /// <summary>Maximum directory depth to search for .sln files.</summary>
    public int MaxDepth { get; set; } = 4;

    /// <summary>Whether to resolve transitive project references.</summary>
    public bool FollowProjectReferences { get; set; } = true;

    /// <summary>Whether to include test projects in results.</summary>
    public bool IncludeTestProjects { get; set; } = true;

    /// <summary>Folder name patterns that indicate data/model directories.</summary>
    public List<string> DataFolderPatterns { get; set; } = new() { "Data", "Models", "Entities", "Domain" };

    /// <summary>Whether to extract NuGet package references from .csproj.</summary>
    public bool IncludeNuGetPackages { get; set; } = true;

    /// <summary>File patterns to search for DbContext classes.</summary>
    public List<string> DbContextPatterns { get; set; } = new() { "**/DbContext*.cs", "**/*DbContext.cs" };
}
