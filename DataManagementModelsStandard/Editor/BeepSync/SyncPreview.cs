using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
namespace TheTechIdea.Beep.Editor.BeepSync;

/// <summary>
/// Preview of what will happen during a multi-project sync.
/// </summary>
public sealed class SyncPreview
{
    /// <summary>The shared Data project being synced.</summary>
    public string DataProjectName { get; set; } = string.Empty;

    /// <summary>Per-entity sync preview.</summary>
    public List<SyncEntityPreview> Entities { get; set; } = new();

    /// <summary>Total entities to sync.</summary>
    public int TotalEntities => Entities.Count;

    /// <summary>Entities that will create new tables.</summary>
    public int CreateCount => Entities.Count(e => e.Operation == SyncOperation.Create);

    /// <summary>Entities that will alter existing tables.</summary>
    public int AlterCount => Entities.Count(e => e.Operation == SyncOperation.Alter);

    /// <summary>Entities that will be dropped.</summary>
    public int DropCount => Entities.Count(e => e.Operation == SyncOperation.Drop);
}

/// <summary>
/// Preview for a single entity during sync.
/// </summary>
public sealed class SyncEntityPreview
{
    public string EntityName { get; set; } = string.Empty;
    public SyncOperation Operation { get; set; }
    public List<string> ConsumerProjects { get; set; } = new();
    public string? Warning { get; set; }
}
