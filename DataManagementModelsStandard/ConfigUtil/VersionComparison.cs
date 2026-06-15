using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
namespace TheTechIdea.Beep.ConfigUtil;

/// <summary>
/// Result of comparing two database versions.
/// </summary>
public sealed class VersionComparison
{
    public DatabaseVersion? Version1 { get; set; }
    public DatabaseVersion? Version2 { get; set; }
    public List<VersionChangeEntry> Changes { get; set; } = new();
    public int BreakingChangesCount => Changes.Count(c => c.IsBreaking);
    public int AddedCount => Changes.Count(c => c.ChangeType == VersionChangeType.Added);
    public int ModifiedCount => Changes.Count(c => c.ChangeType == VersionChangeType.Modified);
    public int RemovedCount => Changes.Count(c => c.ChangeType == VersionChangeType.Removed);
}
