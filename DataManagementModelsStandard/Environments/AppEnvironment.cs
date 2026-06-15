using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
namespace TheTechIdea.Beep.Environments;

/// <summary>
/// An environment definition (e.g. Dev, Test, Production).
/// </summary>
public sealed class AppEnvironment
{
    /// <summary>Unique ID (e.g. "dev", "staging", "live").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Standard tier classification.</summary>
    public EnvironmentTier Tier { get; set; }

    /// <summary>Sort order for display.</summary>
    public int Order { get; set; }

    /// <summary>Color hint for UI (hex or MudBlazor color name).</summary>
    public string Color { get; set; } = "#607D8B";

    /// <summary>Whether this is a production environment.</summary>
    public bool IsProduction { get; set; }

    /// <summary>Whether changes to this environment require approval.</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Tags for filtering.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>When this environment was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When this environment was last modified.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
