// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
//
// This file ships in PR 1 (the Studio contracts). Every method returns
// StudioResult<T>.Fail(StudioErrorCode.HostNotSupported, "Phase N implements").
// The real implementations land in their respective phases (2-7, 9, 10) per
// the engine plan in Services/Studio/Plans/.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio.Contracts;
using TheTechIdea.Beep.Studio.Deployment;
using TheTechIdea.Beep.Studio.Driver;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Manifest;
using TheTechIdea.Beep.Studio.Migration;
using TheTechIdea.Beep.Studio.Schema;
using TheTechIdea.Beep.Studio.Source;
using TheTechIdea.Beep.Studio.Sync;

namespace TheTechIdea.Beep.Studio.Stubs;

// All stub methods follow the same pattern. A `[Stub("Phase N")]` attribute
// would be ideal; for now the message text identifies the phase.

internal static class StubMessages
{
    public const string NotImplemented = "Not yet implemented. See the engine plan at Services/Studio/Plans/ for the phase that implements this.";
}

// ── EnvironmentProfile ────────────────────────────────────────────────────────
// EnvironmentProfileServiceStub replaced by the real EnvironmentProfileService
// (Studio/EnvironmentProfileService.cs) in BeepServiceExtensions.AddBeepStudio.
// The stub is retained here for backward compatibility and test scenarios.

internal sealed class EnvironmentProfileServiceStub : IEnvironmentProfileService
{
    public Task<StudioResult<IReadOnlyList<EnvironmentProfile>>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<IReadOnlyList<EnvironmentProfile>>.Ok(Array.Empty<EnvironmentProfile>()));
    public Task<StudioResult<EnvironmentProfile>> GetAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvironmentProfile>.Fail(StudioErrorCode.HostNotSupported, StubMessages.NotImplemented));
    public Task<StudioResult<EnvironmentProfile>> SaveAsync(EnvironmentProfile p, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvironmentProfile>.Fail(StudioErrorCode.HostNotSupported, StubMessages.NotImplemented));
    public Task<StudioResult<bool>> DeleteAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.HostNotSupported, StubMessages.NotImplemented));
    public Task<StudioResult<EnvironmentProfile>> GetDefaultAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<EnvironmentProfile>.Fail(StudioErrorCode.HostNotSupported, StubMessages.NotImplemented));
}

// ── Driver ────────────────────────────────────────────────────────────────────
// DriverServiceStub removed in PR 8: the real DriverService (Driver/DriverService.cs)
// is now wired in BeepServiceExtensions.AddBeepStudio via a factory that resolves
// the host's IDMEEditor.

// ── Source ────────────────────────────────────────────────────────────────────
// SourceServiceStub removed in PR 2: the real SourceService (Source/SourceService.cs)
// is now wired in BeepServiceExtensions.AddBeepStudio via a factory that resolves
// the host's IDMEEditor. The stub would shadow the real impl and is no longer
// referenced anywhere.

// ── Schema ────────────────────────────────────────────────────────────────────
// SchemaServiceStub removed in PR 3: the real SchemaService (Schema/SchemaService.cs)
// is now wired in BeepServiceExtensions.AddBeepStudio via a factory that resolves
// the host's IDMEEditor.

// ── Migration ─────────────────────────────────────────────────────────────────
// MigrationStudioServiceStub removed in PR 4: the real MigrationStudioService
// (Migration/MigrationStudioService.cs) is now wired in BeepServiceExtensions.AddBeepStudio
// via a factory that resolves the host's IDMEEditor.

// ── Sync ──────────────────────────────────────────────────────────────────────
// SyncStudioServiceStub removed in PR 5: the real SyncStudioService (Sync/SyncStudioService.cs)
// is now wired in BeepServiceExtensions.AddBeepStudio via a factory that resolves
// the host's IDMEEditor.

// ── Governance ────────────────────────────────────────────────────────────────
// GovernanceServiceStub removed in PR 6: the real GovernanceService
// (Governance/GovernanceService.cs) is now wired in BeepServiceExtensions.AddBeepStudio
// with a fallback to NullBeepAudit when the engine's audit feature is disabled.

// ── Manifest ──────────────────────────────────────────────────────────────────
// DataLifecycleManifestServiceStub removed in PR 7: the real
// DataLifecycleManifestService (Manifest/DataLifecycleManifestService.cs)
// is now wired in BeepServiceExtensions.AddBeepStudio.

// ── Deployment ────────────────────────────────────────────────────────────────
// DeploymentMetadataServiceStub removed in PR 7: the real
// DeploymentMetadataService (Deployment/DeploymentMetadataService.cs) is now
// wired in BeepServiceExtensions.AddBeepStudio. It includes HMAC-SHA256
// approval-token issue + verify.

// ── Deployment ────────────────────────────────────────────────────────────────

internal sealed class DeploymentMetadataServiceStub : IDeploymentMetadataService
{
    public Task<StudioResult<DeploymentMetadata>> GetCurrentAsync(CancellationToken ct = default) =>
        Task.FromResult(StudioResult<DeploymentMetadata>.Fail(StudioErrorCode.HostNotSupported, StubMessages.NotImplemented));
    public void Override(DeploymentMetadata? m) { /* no-op */ }
    public Task<StudioResult<ApprovalToken>> IssueApprovalTokenAsync(ApprovalTokenRequest r, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ApprovalToken>.Fail(StudioErrorCode.HostNotSupported, StubMessages.NotImplemented));
    public Task<StudioResult<ApprovalTokenClaims>> VerifyApprovalTokenAsync(string t, CancellationToken ct = default) =>
        Task.FromResult(StudioResult<ApprovalTokenClaims>.Fail(StudioErrorCode.HostNotSupported, StubMessages.NotImplemented));
}
