using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.ImportExport;

/// <summary>
/// Report after an import operation.
/// </summary>
public sealed class ImportReport
{
    public bool IsDryRun { get; set; }
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<EntityImportReport> PerEntity { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
}

/// <summary>
/// Per-entity import report.
/// </summary>
public sealed class EntityImportReport
{
    public string EntityName { get; set; } = string.Empty;
    public int RowsInPackage { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorDetails { get; set; } = new();
}
