// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Manifest;

/// <summary>
/// Data lifecycle manifest — the link between data lifecycle and code lifecycle.
/// Implemented in Phase 9. The manifest is a JSON or YAML file in the project
/// repo that declares: "this code revision expects these data sources, with
/// these schemas, with this approval tier, with this retention policy."
/// The Studio reads it on startup, validates every apply against it, and is
/// the canonical artefact that ties data changes to code revisions.
/// </summary>
public interface IDataLifecycleManifestService
{
    /// <summary>
    /// Load the manifest from the configured path. If <paramref name="overridePath"/>
    /// is <c>null</c>, the Studio walks up from the current working directory
    /// looking for <c>beep/data-lifecycle-manifest.json</c>.
    /// </summary>
    Task<StudioResult<DataLifecycleManifest>> LoadAsync(string? overridePath = null, CancellationToken ct = default);

    /// <summary>Write the manifest to a path. Used by the host UI's manifest editor.</summary>
    Task<StudioResult<bool>> SaveAsync(DataLifecycleManifest manifest, string path, CancellationToken ct = default);

    /// <summary>
    /// Validate the manifest against the live Studio state (registered sources,
    /// registered drivers, governance policies). Returns a report with all
    /// issues (not just the first) — the host UI renders them as a list.
    /// </summary>
    Task<StudioResult<ManifestValidationReport>> ValidateAsync(DataLifecycleManifest manifest, CancellationToken ct = default);

    /// <summary>Resolve the manifest path by walking up from CWD to the repo root.</summary>
    string? ResolveManifestPath(string? startDirectory = null);

    /// <summary>The currently loaded manifest, or <c>null</c> if <see cref="LoadAsync"/> has not succeeded.</summary>
    DataLifecycleManifest? Current { get; }
}

/// <summary>Root manifest POCO. Bound from JSON or YAML.</summary>
public sealed record DataLifecycleManifest(
    /// <summary>Manifest format version. The Studio supports version 1; any other version returns <see cref="StudioErrorCode.ManifestVersionUnsupported"/>.</summary>
    int ManifestVersion,

    /// <summary>The data-platform team's contact (e.g. <c>data-platform@thetechidea.com</c>).</summary>
    string Owner,

    /// <summary>The project this manifest describes.</summary>
    ManifestProjectRef Project,

    /// <summary>The data-lifecycle spec: environments, expected sources, policies.</summary>
    DataLifecycleSpec DataLifecycle,

    /// <summary>When the manifest was generated. Optional; not used for validation.</summary>
    DateTimeOffset? GeneratedAt = null,

    /// <summary>URI of the JSON Schema for this manifest. Optional; used for self-validation.</summary>
    string? SchemaUri = null);

/// <summary>The project this manifest describes.</summary>
public sealed record ManifestProjectRef(
    /// <summary>Project name (e.g. <c>TheTechIdeaWeb.ApiService</c>).</summary>
    string Name,

    /// <summary>Project type (e.g. <c>DotnetWebApi</c>, <c>BlazorServer</c>, <c>WinForms</c>).</summary>
    string Type,

    /// <summary>Repository URL (e.g. <c>https://github.com/...</c>).</summary>
    string Repository,

    /// <summary>The code revision the manifest was authored against.</summary>
    CodeRevisionRef CodeRevision);

/// <summary>Reference to a specific code revision (git ref + sha).</summary>
public sealed record CodeRevisionRef(
    /// <summary>The ref (e.g. <c>refs/heads/main</c>, <c>refs/tags/v1.2.3</c>).</summary>
    string Ref,

    /// <summary>The full git SHA.</summary>
    string Sha);

/// <summary>The data-lifecycle spec inside a manifest.</summary>
public sealed record DataLifecycleSpec(
    /// <summary>The data-platform team's contact.</summary>
    string Owner,

    /// <summary>Compliance tier (e.g. <c>Standard</c>, <c>Regulated</c>, <c>Experimental</c>).</summary>
    string Tier,

    /// <summary>The environments the project targets (Dev / Test / Staging / Live / Custom).</summary>
    IReadOnlyList<ManifestEnvironmentSpec> Environments,

    /// <summary>The data sources the project expects to use.</summary>
    IReadOnlyList<ManifestExpectedSource> ExpectedSources,

    /// <summary>Schema-level policies.</summary>
    ManifestSchemaPolicies SchemaPolicies,

    /// <summary>Sync-level policies.</summary>
    ManifestSyncPolicies SyncPolicies,

    /// <summary>Audit-level policies.</summary>
    ManifestAuditPolicies AuditPolicies,

    /// <summary>Approval-level policies.</summary>
    ManifestApprovalPolicies ApprovalPolicies);

/// <summary>An environment the project targets.</summary>
public sealed record ManifestEnvironmentSpec(
    /// <summary>Environment id (e.g. <c>dev</c>, <c>live</c>). Unique within the manifest.</summary>
    string Id,

    /// <summary>Human-readable name (e.g. <c>Development</c>).</summary>
    string Name,

    /// <summary>The rollout tier.</summary>
    RolloutTier Tier,

    /// <summary>Aliases of the data sources this environment uses. Each alias must match an <see cref="ManifestExpectedSource.Alias"/>.</summary>
    IReadOnlyList<string> DataSourceAliases,

    /// <summary>True when every mutation targeting this environment requires an approval.</summary>
    bool RequiresApproval = false,

    /// <summary>How many distinct approvers must sign off.</summary>
    int RequiredApproverCount = 1,

    /// <summary>Minimum time between two applies to this environment.</summary>
    TimeSpan? CooldownBetweenRuns = null);

/// <summary>A data source the project expects to use.</summary>
public sealed record ManifestExpectedSource(
    /// <summary>Stable alias referenced by <see cref="ManifestEnvironmentSpec.DataSourceAliases"/>.</summary>
    string Alias,

    /// <summary>Driver package name (e.g. <c>Beep.DataSource.SqlServer</c>).</summary>
    string Driver,

    /// <summary>The data-source category (e.g. <c>RDBMS</c>, <c>NoSQL</c>, <c>File</c>).</summary>
    string Category,

    /// <summary>Free-form policies (e.g. <c>{ "pii": "Tag", "retention": { "days": 365 } }</c>).</summary>
    IReadOnlyDictionary<string, object?>? Policies = null);

/// <summary>Schema-level policies.</summary>
public sealed record ManifestSchemaPolicies(
    bool RequireMigrationPlanHash = true,
    bool ForbidDestructiveInLive = true,
    bool RequirePreflightOnLive = true,
    IReadOnlyList<string> BlockedOperations = null!);

/// <summary>Sync-level policies.</summary>
public sealed record ManifestSyncPolicies(
    bool WatermarkRequired = true,
    string? ConflictResolutionRule = "sync.conflict.source-wins",
    int MaxRowsPerRun = 1_000_000,
    IReadOnlyList<string> BlockedSchemas = null!);

/// <summary>Audit-level policies.</summary>
public sealed record ManifestAuditPolicies(
    IReadOnlyList<string> Redact = null!,
    int RetentionDays = 365,
    bool RequireHashChain = true);

/// <summary>Approval-level policies.</summary>
public sealed record ManifestApprovalPolicies(
    IReadOnlyList<string> DefaultApproverRoles = null!,
    bool RequirePlanHashMatch = true,
    TimeSpan? CooldownBetweenRuns = null);

/// <summary>The result of a manifest validation.</summary>
public sealed record ManifestValidationReport(
    bool IsValid,
    IReadOnlyList<ManifestValidationIssue> Issues,
    DateTimeOffset ValidatedAt,
    string? ManifestSha256);

/// <summary>A single manifest validation issue.</summary>
public sealed record ManifestValidationIssue(
    string Code,
    string Path,
    string Message,
    string Severity);
