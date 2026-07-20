// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Migration.Ledger;

namespace TheTechIdea.Beep.Studio.Repository;

/// <summary>
/// Stage 3: unified persistence abstraction for the Studio's long-lived metadata.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> Before Stage 3, every Studio concern persisted to its own JSON file
/// (<c>apps.json</c>, <c>governance-policies.json</c>, <c>env-profiles.json</c>, <c>masking/{appId}.json</c>,
/// <c>migration-ledger.json</c>), each with its own path resolution, write posture, and concurrency
/// story. Stage 3 collapses them into one storage layer so a host can swap the file backend for a
/// database backend (solo vs. enterprise) by changing exactly one line in <c>AddBeepStudio</c>.
/// </para>
/// <para>
/// <b>What this is NOT.</b> It is <b>not</b> a replacement for the domain-shaped services
/// (<c>AppRegistry</c>, <c>GovernanceService</c>, <c>EnvironmentProfileService</c>). Those keep
/// their domain operations (e.g. <c>AddEnvironment</c>, <c>SetDatasource</c>) and widely-consumed
/// signatures. <c>IStudioRepository</c> sits <i>beneath</i> them — services delegate to it for
/// persistence, callers keep calling services. This is the same layering <c>ISetupStateStore</c>
/// uses against <c>SetupWizard</c>.
/// </para>
/// <para>
/// <b>Mirrors <c>ISetupStateStore</c>.</b> The shape — load/save/lease with
/// <see cref="StudioRepositoryConflictException"/> on stale writes — is deliberately the same as
/// the Setup Framework's state-store pattern (Phases 3-4 of the Setup Framework) so host authors
/// have one mental model.
/// </para>
/// </remarks>
public interface IStudioRepository
{
    /// <summary>Apps aggregate (wraps what <c>AppRegistry</c> persists to <c>apps.json</c>).</summary>
    IAppRepositoryStore Apps { get; }

    /// <summary>Environment-profile aggregate (wraps <c>EnvironmentProfileService</c>'s JSON store).</summary>
    IEnvironmentProfileRepositoryStore EnvironmentProfiles { get; }

    /// <summary>Governance aggregate (wraps <c>GovernanceService</c>'s policies + approvals JSON files).</summary>
    IGovernanceRepositoryStore Governance { get; }

    /// <summary>Masking-rules aggregate (wraps <c>AppDataWorkflow</c>'s per-app masking JSON).</summary>
    IMaskingRuleRepositoryStore MaskingRules { get; }

    /// <summary>
    /// Migration-ledger aggregate. Returns the existing <see cref="IMigrationLedger"/> (Stage 2) — the
    /// repository surface doesn't replace it, just exposes it as one of the persistence concerns.
    /// </summary>
    IMigrationLedger MigrationLedger { get; }

    /// <summary>
    /// Acquire an exclusive cross-process lease on a named resource (e.g. "apps", "governance")
    /// for a long-running operation. Returns <c>null</c> if the resource is already leased.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>ISetupStateStore.TryAcquireLeaseAsync</c>. File impl: sibling <c>.lock</c> file
    /// (same idiom as <c>LocalJsonSetupStateStore</c>). DB impl: a row in a <c>locks</c> table.
    /// </remarks>
    Task<IStudioRepositoryLease?> TryAcquireLeaseAsync(string resourceKey, TimeSpan ttl, CancellationToken ct = default);
}

/// <summary>
/// Cross-process lease returned from <see cref="IStudioRepository.TryAcquireLeaseAsync"/>.
/// Dispose to release; renew before the TTL expires to keep it. Mirrors <c>ISetupStateLease</c>.
/// </summary>
public interface IStudioRepositoryLease : IAsyncDisposable
{
    string ResourceKey { get; }
    string LeaseId { get; }
    DateTimeOffset ExpiresAt { get; }
    Task<bool> RenewAsync(CancellationToken ct = default);
}

/// <summary>
/// Apps aggregate persistence. The persistence-layer counterpart to <c>IAppRegistry</c> (which is
/// domain-shaped). Reads return <c>(AppDefinition, RowVersion)</c> tuples so callers can pass the
/// version back on save for optimistic concurrency.
/// </summary>
public interface IAppRepositoryStore
{
    Task<IReadOnlyList<AppDefinition>> LoadAllAsync(CancellationToken ct = default);
    Task<(AppDefinition? App, string? RowVersion)> LoadAsync(string appId, CancellationToken ct = default);
    Task<string> SaveAsync(AppDefinition app, string? expectedRowVersion = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(string appId, string? expectedRowVersion = null, CancellationToken ct = default);
}

/// <summary>Environment-profile aggregate persistence.</summary>
public interface IEnvironmentProfileRepositoryStore
{
    Task<IReadOnlyList<EnvironmentProfile>> LoadAllAsync(CancellationToken ct = default);
    Task<string> SaveAsync(EnvironmentProfile profile, string? expectedRowVersion = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(string envId, string? expectedRowVersion = null, CancellationToken ct = default);
}

/// <summary>Governance aggregate persistence (policies + approvals).</summary>
public interface IGovernanceRepositoryStore
{
    Task<IReadOnlyList<GovernancePolicy>> LoadPoliciesAsync(CancellationToken ct = default);
    Task<string> SavePolicyAsync(GovernancePolicy policy, string? expectedRowVersion = null, CancellationToken ct = default);
    Task<bool> DeletePolicyAsync(string tier, string? expectedRowVersion = null, CancellationToken ct = default);

    Task<IReadOnlyList<ApprovalRequest>> LoadApprovalsAsync(CancellationToken ct = default);
    Task<string> SaveApprovalAsync(ApprovalRequest approval, string? expectedRowVersion = null, CancellationToken ct = default);
}

/// <summary>Masking-rules aggregate persistence (per-app).</summary>
public interface IMaskingRuleRepositoryStore
{
    Task<IReadOnlyList<Studio.Apps.Workflows.MaskingRule>> LoadAsync(string appId, CancellationToken ct = default);
    Task SaveAsync(string appId, IReadOnlyCollection<Studio.Apps.Workflows.MaskingRule> rules, CancellationToken ct = default);
}

/// <summary>
/// Raised by any <c>SaveAsync</c>/<c>DeleteAsync</c> when the supplied <c>expectedRowVersion</c>
/// does not match the current version. Mirrors <c>SetupStateConflictException</c>. Hosts surface
/// this as "this was edited by someone else; refresh and try again".
/// </summary>
public sealed class StudioRepositoryConflictException : Exception
{
    public StudioRepositoryConflictException(string message) : base(message) { }
    public StudioRepositoryConflictException(string message, Exception inner) : base(message, inner) { }

    /// <summary>The store key (e.g. app id, env id) whose write was refused.</summary>
    public string? ResourceKey { get; set; }

    /// <summary>The version the caller expected (stale).</summary>
    public string? ExpectedVersion { get; set; }

    /// <summary>The version actually in the store at the time of the refused write.</summary>
    public string? CurrentVersion { get; set; }
}
