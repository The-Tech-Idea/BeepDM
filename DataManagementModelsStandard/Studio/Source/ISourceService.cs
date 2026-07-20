// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio.Schema;

namespace TheTechIdea.Beep.Studio.Source;

/// <summary>
/// CRUD + health check for data sources (connections). Implemented in Phase 3.
/// The source registry is the central place where every connection used by
/// every project is recorded; downstream apps consume it via the push flow
/// (Phase 24 of the original plan).
/// </summary>
public interface ISourceService
{
    /// <summary>List sources, with optional filtering.</summary>
    Task<StudioResult<IReadOnlyList<SourceInfo>>> ListAsync(SourceListFilter? filter = null, CancellationToken ct = default);

    /// <summary>Get a single source by name.</summary>
    Task<StudioResult<SourceInfo>> GetAsync(string sourceName, CancellationToken ct = default);

    /// <summary>Create or update a source. Secrets are extracted and stored in the keychain before persistence.</summary>
    Task<StudioResult<SourceInfo>> SaveAsync(SourceConfigurationRequest request, CancellationToken ct = default);

    /// <summary>Delete a source by name. Refuses if any project binding still references it.</summary>
    Task<StudioResult<bool>> DeleteAsync(string sourceName, CancellationToken ct = default);

    /// <summary>Run a connection health check against the live data source.</summary>
    Task<StudioResult<SourceTestResult>> TestAsync(string sourceName, CancellationToken ct = default);

    /// <summary>Browse the schema: list entities, or describe a single entity, or fetch sample rows.</summary>
    Task<StudioResult<IReadOnlyList<EntityDescriptor>>> BrowseAsync(string sourceName, string? entityName = null, int sampleRows = 0, CancellationToken ct = default);

    /// <summary>
    /// Stage 6.4: look up the raw <c>ConnectionProperties</c> for a source — used by the AppStudio
    /// connection-edit flow so the view can host <c>BeepWpfConnectionDialog</c> without reaching
    /// into <c>IBeepService.Config_editor.DataConnections</c>. Returns null when the source is unknown.
    /// </summary>
    Task<StudioResult<TheTechIdea.Beep.ConfigUtil.ConnectionProperties?>> LookupConnectionAsync(string sourceName, CancellationToken ct = default);

    /// <summary>
    /// Stage 6.6: detect whether a source's database hosts ASP.NET Identity (or any identity-shaped
    /// schema). Wraps the engine's <c>IdentityManagementService.DetectAsync</c> so AppStudio doesn't
    /// construct that service directly with <c>IDMEEditor</c>.
    /// </summary>
    Task<StudioResult<TheTechIdea.Beep.Environments.Data.IdentityDetectionResult>> DetectIdentityAsync(string sourceName, CancellationToken ct = default);
}

/// <summary>Filter for <see cref="ISourceService.ListAsync"/>.</summary>
public sealed record SourceListFilter(
    string? EnvironmentId = null,
    string? ProjectId = null,
    string? OwnerId = null,
    string? SearchText = null,
    int Skip = 0,
    int Take = 100);

/// <summary>Metadata about a registered source. Wraps a <c>ConnectionProperties</c> with Studio-level bookkeeping.</summary>
public sealed record SourceInfo(
    string Name,
    string OwnerId,
    IReadOnlyList<string> Tags,
    string? Documentation,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastTestedAt,
    SourceTestResult? LastTestResult,
    string DataSourceType,
    string Category,
    string? Host,
    int? Port,
    string? Database);

/// <summary>Request to create or update a source.</summary>
public sealed record SourceConfigurationRequest(
    string Name,
    string DataSourceType,
    string Category,
    string? Host = null,
    int? Port = null,
    string? Database = null,
    string? UserId = null,
    string? Password = null,
    string? ConnectionString = null,
    string? OwnerId = null,
    IReadOnlyList<string>? Tags = null,
    string? Documentation = null,
    string? EnvironmentId = null,
    string? ProjectId = null);

/// <summary>Result of a connection health check.</summary>
public sealed record SourceTestResult(
    bool Success,
    int LatencyMs,
    string? ServerVersion,
    int? CatalogCount,
    int? EntityCount,
    string? ErrorMessage,
    IReadOnlyList<string>? Warnings);
