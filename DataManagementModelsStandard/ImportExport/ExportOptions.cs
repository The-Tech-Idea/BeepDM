using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.ImportExport;

/// <summary>
/// Options for exporting data from a datasource.
/// </summary>
public sealed class ExportOptions
{
    /// <summary>The datasource to export from.</summary>
    public string DatasourceName { get; set; } = string.Empty;

    /// <summary>Entity names to export (empty = all).</summary>
    public List<string> EntityNames { get; set; } = new();

    /// <summary>Max rows per entity (0 = unlimited).</summary>
    public int MaxRowsPerEntity { get; set; } = 10_000;

    /// <summary>Whether to include schema definitions.</summary>
    public bool IncludeSchema { get; set; } = true;

    /// <summary>Compression level for ZIP (0-9).</summary>
    public int CompressionLevel { get; set; } = 6;

    /// <summary>Output file path (optional, auto-generated if empty).</summary>
    public string? OutputPath { get; set; }
}
