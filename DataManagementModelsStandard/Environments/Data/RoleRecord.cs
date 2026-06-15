using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.Environments.Data;

/// <summary>
/// A role record.
/// </summary>
public sealed class RoleRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NormalizedName { get; set; }
    public string? Description { get; set; }

    /// <summary>Number of users assigned to this role.</summary>
    public int UserCount { get; set; }
}
