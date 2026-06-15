using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
namespace TheTechIdea.Beep.AppMap;

/// <summary>
/// A project with its detected/assigned role and confidence.
/// </summary>
public sealed class ProjectRoleAssignment
{
    public ProjectInfo Project { get; set; } = null!;
    public ProjectRole Role { get; set; } = ProjectRole.Unknown;

    /// <summary>Confidence of auto-detection (0.0 to 1.0).</summary>
    public float Confidence { get; set; }

    /// <summary>True if the user manually overrode the detected role.</summary>
    public bool IsManualOverride { get; set; }

    /// <summary>When the role was assigned.</summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Which heuristic matched (if any).</summary>
    public string? MatchedHeuristic { get; set; }
}
