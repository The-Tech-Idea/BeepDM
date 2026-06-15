using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.ImportExport;

/// <summary>
/// Conflict resolution strategy when importing.
/// </summary>
public enum ConflictResolution
{
    Skip = 0,
    Update = 1,
    Error = 2
}

/// <summary>
/// Options for importing data into a datasource.
/// </summary>
public sealed class ImportOptions
{
    /// <summary>The datasource to import into.</summary>
    public string TargetDatasource { get; set; } = string.Empty;

    /// <summary>Path to the export package (ZIP file).</summary>
    public string PackagePath { get; set; } = string.Empty;

    /// <summary>How to handle conflicts.</summary>
    public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.Skip;

    /// <summary>Whether this is a dry-run (preview only).</summary>
    public bool DryRun { get; set; }

    /// <summary>Entities to import (empty = all in package).</summary>
    public List<string> EntityFilter { get; set; } = new();
}
