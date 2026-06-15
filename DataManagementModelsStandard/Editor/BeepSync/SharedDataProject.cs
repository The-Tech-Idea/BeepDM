using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.AppMap;

namespace TheTechIdea.Beep.Editor.BeepSync;

/// <summary>
/// A Data project that is shared by multiple consumer projects.
/// </summary>
public sealed class SharedDataProject
{
    /// <summary>The Data project itself.</summary>
    public ProjectInfo DataProject { get; set; } = null!;

    /// <summary>Entities discovered in the Data project.</summary>
    public List<string> EntityNames { get; set; } = new();

    /// <summary>Projects that reference (consume) this Data project.</summary>
    public List<ProjectInfo> ConsumerProjects { get; set; } = new();

    /// <summary>Number of entities.</summary>
    public int EntityCount => EntityNames.Count;

    /// <summary>Number of consumers.</summary>
    public int ConsumerCount => ConsumerProjects.Count;
}
