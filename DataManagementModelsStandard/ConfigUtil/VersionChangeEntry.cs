using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
namespace TheTechIdea.Beep.ConfigUtil;

/// <summary>
/// Describes what changed between two versions.
/// </summary>
public sealed class VersionChangeEntry
{
    public string EntityName { get; set; } = string.Empty;
    public VersionChangeType ChangeType { get; set; }
    public List<string> PropertyChanges { get; set; } = new();
    public string? FromVersion { get; set; }
    public string? ToVersion { get; set; }
    public bool IsBreaking { get; set; }
}
