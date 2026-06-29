using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Apps.Workflows;

/// <summary>
/// CI/CD and deploy-to-environment workflow: generate a migration-aware pipeline,
/// run migrate-on-deploy against an env, and stand up a disposable preview database
/// for pull-request validation. Output is text (YAML/markdown) so the host can show
/// or write it without a CI provider dependency.
/// </summary>
public interface IAppCicdWorkflow
{
    /// <summary>Generate a pipeline descriptor (steps + rendered YAML) for the given provider.</summary>
    Task<StudioResult<PipelineDescriptor>> GeneratePipelineAsync(string appId, CicdProvider provider, CancellationToken ct = default);

    /// <summary>Run "migrate on deploy" for an env: apply pending migrations as a deploy step.</summary>
    Task<StudioResult<EnvMigrationReport>> MigrateOnDeployAsync(string appId, string envId, CancellationToken ct = default);

    /// <summary>Create a preview database from an app's baseline schema, for PR validation. Returns the preview datasource name.</summary>
    Task<StudioResult<PrPreviewResult>> CreatePreviewDatabaseAsync(string appId, string prId, CancellationToken ct = default);

    /// <summary>Tear down a previously-created preview database.</summary>
    Task<StudioResult<bool>> DropPreviewDatabaseAsync(string appId, string prId, CancellationToken ct = default);

    /// <summary>List preview databases currently alive for an app.</summary>
    Task<StudioResult<IReadOnlyList<PrPreviewResult>>> ListPreviewsAsync(string appId, CancellationToken ct = default);
}

public enum CicdProvider
{
    GitHubActions = 0,
    AzureDevOps = 1,
    Generic = 2
}

public sealed class PipelineDescriptor
{
    public required string AppId { get; set; }
    public CicdProvider Provider { get; set; }
    /// <summary>Rendered pipeline YAML the caller can drop into a repo.</summary>
    public string Yaml { get; set; } = string.Empty;
    /// <summary>Human-readable summary of the stages.</summary>
    public List<string> Stages { get; set; } = new();
}

public sealed class PrPreviewResult
{
    public required string AppId { get; set; }
    public required string PrId { get; set; }
    public required string PreviewDatasourceName { get; set; }
    public string? ConnectionString { get; set; }
    public bool SchemaApplied { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
