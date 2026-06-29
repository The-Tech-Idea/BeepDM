using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Apps.Workflows;

/// <summary>
/// End-to-end scenario workflows for the two primary Studio personas.
/// Each chains the micro-workflows (QuickStart | Migrations | Deploy | Governance | Cicd)
/// into one composite journey so the host UI can offer "Solo Dev" and
/// "Enterprise" as single-decision paths.
/// </summary>
public interface IScenarioWorkflow
{
    /// <summary>
    /// Solo-developer path: create an app with one dev environment, provision several
    /// local datasources, bind them to the baseline, migrate each, and optionally seed.
    /// Single-developer, no approval gates, local-first by default.
    /// </summary>
    Task<StudioResult<SoloDevResult>> RunSoloDevAsync(SoloDevRequest request, CancellationToken ct = default);

    /// <summary>
    /// Enterprise path: register from a solution, configure multiple environments
    /// (dev / test / staging / production), provision datasources per env, set up
    /// RBAC + approval gates, deploy code to the baseline, generate a CI/CD pipeline,
    /// and record the startup audit.
    /// </summary>
    Task<StudioResult<EnterpriseResult>> RunEnterpriseAsync(EnterpriseRequest request, CancellationToken ct = default);
}

// ── Solo Dev ───────────────────────────────────────────────────────────────

public sealed class SoloDevRequest
{
    public string AppName { get; set; } = string.Empty;
    public string? TemplateId { get; set; } = "blank";
    /// <summary>Multiple datasources to provision for the dev environment. At least one.</summary>
    public List<SoloDatasourceSpec> Datasources { get; set; } = new() { new SoloDatasourceSpec() };
    public bool Seed { get; set; }
    public string? SeedSource { get; set; }
}

public sealed class SoloDatasourceSpec
{
    public string Name { get; set; } = "main";
    public string Type { get; set; } = "SqlLite";       // DataSourceType name
    public string? ConnectionString { get; set; }        // null → auto-generated
    public bool IsPrimary { get; set; } = true;
}

public sealed class SoloDevResult
{
    public required string AppId { get; set; }
    public required string BaselineEnvId { get; set; }
    public List<string> DatasourceNames { get; set; } = new();
    public bool SchemaApplied { get; set; }
    public bool Seeded { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ── Enterprise ─────────────────────────────────────────────────────────────

public sealed class EnterpriseRequest
{
    public string AppName { get; set; } = string.Empty;
    /// <summary>Path to the .sln. Populates the app's projects + roles.</summary>
    public string? SolutionPath { get; set; }
    /// <summary>Environment configurations to create. Defaults to dev/test/staging/prod.</summary>
    public List<EnterpriseEnvSpec> Environments { get; set; } = new()
    {
        new() { EnvironmentId = "dev", Tier = "dev", IsBaseline = true },
        new() { EnvironmentId = "test", Tier = "test" },
        new() { EnvironmentId = "staging", Tier = "staging", RequiresApproval = true },
        new() { EnvironmentId = "prod", Tier = "production", IsProduction = true, RequiresApproval = true },
    };
    /// <summary>Initial admin user (for RBAC). When non-null, the user is granted Admin on the app.</summary>
    public string? InitialAdminUser { get; set; }
    /// <summary>Generate a pipeline? When null, generated only when a solution path is supplied.</summary>
    public CicdProvider? CicdProvider { get; set; }
    /// <summary>Enable approval gates on production/staging environments. Default true.</summary>
    public bool EnableApprovals { get; set; } = true;
}

public sealed class EnterpriseEnvSpec
{
    public string EnvironmentId { get; set; } = string.Empty;
    public string Tier { get; set; } = "dev";
    public bool IsBaseline { get; set; }
    public bool IsProduction { get; set; }
    public bool RequiresApproval { get; set; }
    /// <summary>Datasource specs for THIS environment.</summary>
    public List<SoloDatasourceSpec> Datasources { get; set; } = new() { new SoloDatasourceSpec() };
}

public sealed class EnterpriseResult
{
    public required string AppId { get; set; }
    public required string AppName { get; set; }
    public int ProjectCount { get; set; }
    public List<string> EnvironmentIds { get; set; } = new();
    public bool SchemaApplied { get; set; }
    public bool CodeDeployed { get; set; }
    public bool PipelineGenerated { get; set; }
    public string? PipelineYaml { get; set; }
    public string Message { get; set; } = string.Empty;
}
