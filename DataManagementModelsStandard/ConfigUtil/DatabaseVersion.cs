using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.ConfigUtil;

/// <summary>
/// Records a database schema version snapshot.
/// </summary>
public sealed class DatabaseVersion
{
    /// <summary>Semantic version (e.g. "2.3.1").</summary>
    public string Version { get; set; } = "0.0.0";

    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
    public string? PreRelease { get; set; }

    /// <summary>The datasource this version applies to.</summary>
    public string DatasourceName { get; set; } = string.Empty;

    /// <summary>When this version was applied.</summary>
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Hash of the migration plan that produced this version.</summary>
    public string? MigrationPlanHash { get; set; }

    /// <summary>Hash of the full schema at this version.</summary>
    public string? SchemaHash { get; set; }

    /// <summary>Number of entities at this version.</summary>
    public int EntityCount { get; set; }

    /// <summary>Who or what applied this version.</summary>
    public string? AppliedBy { get; set; }

    /// <summary>Formatted version string.</summary>
    public string VersionString =>
        string.IsNullOrEmpty(PreRelease) ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{PreRelease}";
}
