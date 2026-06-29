using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Apps.Workflows;

/// <summary>
/// Schema migration workflow, scoped to an App×Environment. Binds the engine's
/// <c>MigrationManager</c> / <c>MigrationTrackingService</c> to the App aggregate so
/// every operation names which app and which environment it targets.
/// </summary>
public interface IAppMigrationWorkflow
{
    /// <summary>Apply the app's entity schema to a specific environment's primary datasource.</summary>
    Task<StudioResult<EnvMigrationReport>> MigrateAsync(string appId, string envId, MigrationOptions? options = null, CancellationToken ct = default);

    /// <summary>Dry-run: what would change on the target env's datasource. No writes.</summary>
    Task<StudioResult<EnvMigrationReport>> DryRunAsync(string appId, string envId, CancellationToken ct = default);

    /// <summary>Undo the last applied migration on the env's datasource.
    /// <paramref name="datasourceName"/> targets a specific datasource; when null, the primary is used.</summary>
    Task<StudioResult<bool>> RollbackAsync(string appId, string envId, string? datasourceName = null, CancellationToken ct = default);

    /// <summary>Migration history for an env's datasource.
    /// <paramref name="datasourceName"/> targets a specific datasource; when null, the primary is used.</summary>
    Task<StudioResult<IReadOnlyList<EnvMigrationHistoryItem>>> GetHistoryAsync(string appId, string envId, string? datasourceName = null, CancellationToken ct = default);

    /// <summary>Compare the schema of two environments of the same app (e.g. dev vs prod).
    /// <paramref name="datasourceName"/> targets a specific datasource; when null, the primary is used.</summary>
    Task<StudioResult<SchemaCompareResult>> CompareEnvironmentsAsync(string appId, string sourceEnv, string targetEnv, string? datasourceName = null, CancellationToken ct = default);
}

/// <summary>Options for an app-scoped migration.</summary>
public sealed class MigrationOptions
{
    public bool DetectRelationships { get; set; } = true;
    public bool ApplyForeignKeys { get; set; }
    public bool ApplyIndexes { get; set; }
    /// <summary>Restrict to these entity type full names. Empty = all of the app's entities.</summary>
    public List<string> EntityTypeNames { get; set; } = new();
    /// <summary>Target a single datasource by name. When null, the workflow migrates EVERY
    /// datasource in the environment (an env can hold several — app DB, identity DB, …).</summary>
    public string? DatasourceName { get; set; }
}

public sealed class EnvMigrationReport
{
    public required string AppId { get; set; }
    public required string EnvId { get; set; }
    public required string DatasourceName { get; set; }
    public bool Succeeded { get; set; }
    public bool WasUpToDate { get; set; }
    public int OperationsApplied { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SchemaVersion { get; set; }
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EnvMigrationHistoryItem
{
    public string DatasourceName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset AppliedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class SchemaCompareResult
{
    public required string AppId { get; set; }
    public required string SourceEnv { get; set; }
    public required string TargetEnv { get; set; }
    public bool AreEqual { get; set; }
    public List<string> MissingInTarget { get; set; } = new();
    public List<string> MissingInSource { get; set; } = new();
    public List<string> Different { get; set; } = new();
}
