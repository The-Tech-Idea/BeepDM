// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Deployment;

/// <summary>
/// Deployment metadata + HMAC approval tokens. Implemented in Phase 10.
/// The service captures the code revision the Studio is running against,
/// enriches every audit event with it, and issues HMAC-signed approval
/// tokens that bind an approval to a specific code revision.
/// </summary>
/// <remarks>
/// We do <b>not</b> orchestrate deployments here. The service only
/// <i>resolves</i> the metadata that a CI/CD system has set; it does not
/// build, deploy, push, or publish anything.
/// </remarks>
public interface IDeploymentMetadataService
{
    /// <summary>Resolve the deployment metadata for the current process.</summary>
    Task<StudioResult<DeploymentMetadata>> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>Override the deployment metadata (used by tests and by the host during a dry-run).</summary>
    void Override(DeploymentMetadata? metadata);

    /// <summary>Issue an HMAC-signed approval token for a request + deployment pair.</summary>
    Task<StudioResult<ApprovalToken>> IssueApprovalTokenAsync(ApprovalTokenRequest request, CancellationToken ct = default);

    /// <summary>Verify an approval token against the current deployment metadata.</summary>
    Task<StudioResult<ApprovalTokenClaims>> VerifyApprovalTokenAsync(string token, CancellationToken ct = default);
}

/// <summary>The metadata that describes a single code revision a Studio instance is running against.</summary>
public sealed record DeploymentMetadata(
    /// <summary>The git ref (e.g. <c>refs/heads/main</c>, <c>refs/tags/v1.2.3</c>).</summary>
    string CodeRevisionRef,

    /// <summary>The full git SHA.</summary>
    string CodeRevisionSha,

    /// <summary>Optional CI build id (e.g. <c>42</c>).</summary>
    string? BuildId,

    /// <summary>Optional CI build URL.</summary>
    string? BuildUrl,

    /// <summary>Optional assembly version (e.g. <c>1.2.3</c>).</summary>
    string? Version,

    /// <summary>When the build was produced.</summary>
    DateTimeOffset BuiltAt,

    /// <summary>Free-form CI labels (commit author, branch, PR id, etc.).</summary>
    IReadOnlyDictionary<string, string>? Labels);

/// <summary>A request to issue an approval token.</summary>
public sealed record ApprovalTokenRequest(
    string ApprovalId,
    string PlanHash,
    RolloutTier Tier,
    DateTimeOffset IssuedAt,
    TimeSpan Lifetime);

/// <summary>The claims inside a verified approval token.</summary>
public sealed record ApprovalTokenClaims(
    string ApprovalId,
    string PlanHash,
    RolloutTier Tier,
    string CodeRevisionRef,
    string CodeRevisionSha,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);

/// <summary>The full approval token + claims + lifetime.</summary>
public sealed record ApprovalToken(
    string Token,
    ApprovalTokenClaims Claims,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
