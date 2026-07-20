// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Services.DatasourceManagement;
using TheTechIdea.Beep.Studio.Schema;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Studio.Source;

/// <summary>
/// Default implementation of <see cref="ISourceService"/>. Wraps the
/// engine's existing <see cref="DatasourceManagementService"/> so all
/// connection management flows through the same code path as
/// <c>DataConnection.razor</c>, <c>SetupOrchestrationService</c>, and the
/// CI/CD service. The Studio is a thin shell on top.
/// </summary>
/// <remarks>
/// Construction is via <c>new SourceService(dmeEditor)</c>; the DI extension
/// wires this in <c>BeepServiceExtensions.AddBeepStudio</c> by resolving
/// the host's <c>IDMEEditor</c> (which lives in the singleton
/// <c>IBeepService</c>).
/// </remarks>
public sealed class SourceService : ISourceService
{
    private readonly IDMEEditor _editor;
    private readonly DatasourceManagementService _inner;

    /// <summary>Construct the source service. <paramref name="editor"/> is the host's <see cref="IDMEEditor"/>.</summary>
    public SourceService(IDMEEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _inner = new DatasourceManagementService(editor);
    }

    /// <inheritdoc />
    public Task<StudioResult<IReadOnlyList<SourceInfo>>> ListAsync(SourceListFilter? filter = null, CancellationToken ct = default)
    {
        return Task.FromResult(ListInternal(filter));
    }

    /// <inheritdoc />
    public Task<StudioResult<SourceInfo>> GetAsync(string sourceName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return Task.FromResult(StudioResult<SourceInfo>.Fail(StudioErrorCode.InvalidArgument, "sourceName is required."));

        var conn = _inner.GetDatasource(sourceName);
        if (conn == null)
            return Task.FromResult(StudioResult<SourceInfo>.Fail(StudioErrorCode.NotFound, $"Source '{sourceName}' was not found."));

        return Task.FromResult(StudioResult<SourceInfo>.Ok(MapToInfo(conn)));
    }

    /// <inheritdoc />
    public Task<StudioResult<SourceInfo>> SaveAsync(SourceConfigurationRequest request, CancellationToken ct = default)
    {
        if (request == null)
            return Task.FromResult(StudioResult<SourceInfo>.Fail(StudioErrorCode.InvalidArgument, "request is required."));
        if (string.IsNullOrWhiteSpace(request.Name))
            return Task.FromResult(StudioResult<SourceInfo>.Fail(StudioErrorCode.InvalidArgument, "request.Name is required."));

        var existing = _inner.GetDatasource(request.Name);
        var conn = existing ?? new ConnectionProperties { ConnectionName = request.Name };
        ApplyToConnection(conn, request);

        IErrorsInfo result = existing == null
            ? _inner.AddDatasource(conn)
            : _inner.UpdateDatasource(conn);

        if (result?.Flag == Errors.Failed)
            return Task.FromResult(StudioResult<SourceInfo>.Fail(
                StudioErrorCode.PermissionDenied,
                result.Message ?? "Save failed.",
                result.Ex));

        return Task.FromResult(StudioResult<SourceInfo>.Ok(MapToInfo(conn)));
    }

    /// <inheritdoc />
    public Task<StudioResult<bool>> DeleteAsync(string sourceName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return Task.FromResult(StudioResult<bool>.Fail(StudioErrorCode.InvalidArgument, "sourceName is required."));

        var result = _inner.RemoveDatasource(sourceName);
        if (result?.Flag == Errors.Failed)
            return Task.FromResult(StudioResult<bool>.Fail(
                StudioErrorCode.PermissionDenied,
                result.Message ?? "Delete failed.",
                result.Ex));

        return Task.FromResult(StudioResult<bool>.Ok(true));
    }

    /// <inheritdoc />
    public async Task<StudioResult<SourceTestResult>> TestAsync(string sourceName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return StudioResult<SourceTestResult>.Fail(StudioErrorCode.InvalidArgument, "sourceName is required.");

        var started = DateTimeOffset.UtcNow;
        IDataSource? ds = null;
        try
        {
            ds = _editor.GetDataSource(sourceName);
            if (ds == null)
            {
                return StudioResult<SourceTestResult>.Fail(
                    StudioErrorCode.ConnectionFailed,
                    $"Data source '{sourceName}' could not be instantiated. Check the driver is installed.");
            }

            var state = _editor.OpenDataSource(sourceName);
            if (state != ConnectionState.Open)
            {
                return StudioResult<SourceTestResult>.Ok(new SourceTestResult(
                    Success: false,
                    LatencyMs: (int)(DateTimeOffset.UtcNow - started).TotalMilliseconds,
                    ServerVersion: null,
                    CatalogCount: null,
                    EntityCount: null,
                    ErrorMessage: $"Connection state is {state}; expected Open.",
                    Warnings: null));
            }

            // Try to read server version + entity count for the result.
            string? serverVersion = null;
            int? entityCount = null;
            try
            {
                var entities = _editor.ConfigEditor?.LoadDataSourceEntitiesValues(sourceName);
                if (entities != null) entityCount = entities.Entities?.Count;
                serverVersion = TryGetServerVersion(ds);
            }
            catch
            {
                // Soft-fail on introspection; the connection is still open.
            }

            return StudioResult<SourceTestResult>.Ok(new SourceTestResult(
                Success: true,
                LatencyMs: (int)(DateTimeOffset.UtcNow - started).TotalMilliseconds,
                ServerVersion: serverVersion,
                CatalogCount: null,
                EntityCount: entityCount,
                ErrorMessage: null,
                Warnings: null));
        }
        catch (Exception ex)
        {
            return StudioResult<SourceTestResult>.Ok(new SourceTestResult(
                Success: false,
                LatencyMs: (int)(DateTimeOffset.UtcNow - started).TotalMilliseconds,
                ServerVersion: null,
                CatalogCount: null,
                EntityCount: null,
                ErrorMessage: ex.Message,
                Warnings: null));
        }
        finally
        {
            try { _editor.CloseDataSource(sourceName); } catch { /* best-effort */ }
        }
    }

    /// <inheritdoc />
    public async Task<StudioResult<IReadOnlyList<EntityDescriptor>>> BrowseAsync(string sourceName, string? entityName = null, int sampleRows = 0, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return StudioResult<IReadOnlyList<EntityDescriptor>>.Fail(StudioErrorCode.InvalidArgument, "sourceName is required.");

        var entities = _editor.ConfigEditor?.LoadDataSourceEntitiesValues(sourceName);
        if (entities == null)
            return StudioResult<IReadOnlyList<EntityDescriptor>>.Ok(Array.Empty<EntityDescriptor>());

        var all = entities.Entities ?? new List<EntityStructure>();
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return StudioResult<IReadOnlyList<EntityDescriptor>>.Ok(
                all.Select(MapToEntityDescriptor).ToList());
        }

        // Specific entity — return a single-element list, optionally with sample rows
        // (sample rows are returned via the SchemaService.DescribeAsync in a future
        // PR; this method only lists entities for now).
        var match = all.FirstOrDefault(e =>
            string.Equals(e.EntityName, entityName, StringComparison.OrdinalIgnoreCase));
        if (match == null)
            return StudioResult<IReadOnlyList<EntityDescriptor>>.Fail(
                StudioErrorCode.NotFound,
                $"Entity '{entityName}' was not found in source '{sourceName}'.");

        return StudioResult<IReadOnlyList<EntityDescriptor>>.Ok(new[] { MapToEntityDescriptor(match) });
    }

    /// <inheritdoc />
    /// <remarks>
    /// Stage 6.4: pure data lookup of <c>ConnectionProperties</c>. The AppStudio connection-edit
    /// view uses this to feed <c>BeepWpfConnectionDialog</c> without bypassing
    /// <c>IStudioService</c> for the raw <c>IBeepService.Config_editor.DataConnections</c>.
    /// </remarks>
    public Task<StudioResult<ConnectionProperties?>> LookupConnectionAsync(string sourceName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return Task.FromResult(StudioResult<ConnectionProperties?>.Fail(StudioErrorCode.InvalidArgument, "sourceName is required."));

        try
        {
            var conn = _editor.ConfigEditor?.DataConnections?
                .FirstOrDefault(c => string.Equals(c.ConnectionName, sourceName, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(StudioResult<ConnectionProperties?>.Ok(conn));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StudioResult<ConnectionProperties?>.Fail(StudioErrorCode.InternalError, ex.Message, ex));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Stage 6.6: thin wrapper over the engine's <c>IdentityManagementService.DetectAsync</c>.
    /// The AppStudio <c>IdentityViewModel</c> uses this instead of constructing
    /// <c>new IdentityManagementService(IDMEEditor)</c> directly.
    /// </remarks>
    public async Task<StudioResult<TheTechIdea.Beep.Environments.Data.IdentityDetectionResult>> DetectIdentityAsync(string sourceName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return StudioResult<TheTechIdea.Beep.Environments.Data.IdentityDetectionResult>.Fail(StudioErrorCode.InvalidArgument, "sourceName is required.");

        try
        {
            // Constructed per-call (matches the engine's existing lazy-init pattern at
            // DMEEditor.Services.cs:169). The engine IdentityManagementService is the canonical
            // detector; we surface it through the Studio facade.
            var identity = new Services.AppMap.IdentityManagementService(_editor);
            var result = await identity.DetectAsync(sourceName).ConfigureAwait(false);
            return StudioResult<TheTechIdea.Beep.Environments.Data.IdentityDetectionResult>.Ok(result);
        }
        catch (Exception ex)
        {
            return StudioResult<TheTechIdea.Beep.Environments.Data.IdentityDetectionResult>.Fail(StudioErrorCode.InternalError, ex.Message, ex);
        }
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private StudioResult<IReadOnlyList<SourceInfo>> ListInternal(SourceListFilter? filter)
    {
        var all = _inner.GetAllDatasources();
        IEnumerable<ConnectionProperties> q = all;

        if (!string.IsNullOrWhiteSpace(filter?.SearchText))
        {
            var s = filter.SearchText.Trim();
            q = q.Where(c =>
                (c.ConnectionName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Host?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Database?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var list = q
            .OrderBy(c => c.ConnectionName, StringComparer.OrdinalIgnoreCase)
            .Skip(Math.Max(0, filter?.Skip ?? 0))
            .Take(Math.Clamp(filter?.Take ?? 100, 1, 1000))
            .Select(MapToInfo)
            .ToList();

        return StudioResult<IReadOnlyList<SourceInfo>>.Ok(list);
    }

    private static SourceInfo MapToInfo(ConnectionProperties c) => new(
        Name: c.ConnectionName ?? string.Empty,
        OwnerId: c.UserID ?? string.Empty,
        Tags: Array.Empty<string>(),
        Documentation: null,
        CreatedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow,
        LastTestedAt: null,
        LastTestResult: null,
        DataSourceType: c.DatabaseType.ToString(),
        Category: c.Category.ToString(),
        Host: c.Host,
        Port: c.Port,
        Database: c.Database);

    private static void ApplyToConnection(ConnectionProperties c, SourceConfigurationRequest r)
    {
        // Parse the DataSourceType / Category from the request strings.
        if (Enum.TryParse<DataSourceType>(r.DataSourceType, true, out var dst))
            c.DatabaseType = dst;
        if (Enum.TryParse<DatasourceCategory>(r.Category, true, out var cat))
            c.Category = cat;

        c.Host = r.Host ?? c.Host;
        c.Port = r.Port ?? c.Port;
        c.Database = r.Database ?? c.Database;
        c.UserID = r.UserId ?? c.UserID;
        c.Password = r.Password ?? c.Password;

        if (!string.IsNullOrWhiteSpace(r.ConnectionString))
            c.ConnectionString = r.ConnectionString;
    }

    private static EntityDescriptor MapToEntityDescriptor(EntityStructure s) => new(
        Name: s.EntityName ?? string.Empty,
        FullName: s.EntityName ?? string.Empty,
        Namespace: s.SchemaOrOwnerOrDatabase ?? string.Empty,
        AssemblyName: string.Empty,
        Category: string.IsNullOrEmpty(s.DatasourceEntityName) ? EntityCategories.Poco : EntityCategories.Entity,
        Properties: (s.Fields ?? new List<EntityField>())
            .Select(f => new EntityPropertyDescriptor(
                Name: f.FieldName ?? string.Empty,
                TypeFullName: !string.IsNullOrWhiteSpace(f.Fieldtype) ? f.Fieldtype : (f.ColumnTypeName ?? "string"),
                IsNullable: f.AllowDBNull,
                IsPrimaryKey: f.IsKey,
                IsForeignKey: false,                              // EntityField doesn't expose FK at the type level
                ReferencedEntity: null,
                MaxLength: f.Size > 0 ? f.Size : null,
                IsAutoIncrement: f.IsAutoIncrement,
                IsUnique: f.IsUnique,
                DefaultValue: f.DefaultValue))
            .ToList());

    private static string? TryGetServerVersion(IDataSource ds)
    {
        try
        {
            // IDataSource does not expose a Version property directly; the version
            // surfaces via the opened connection's ServerVersion. Best-effort: null.
            return null;
        }
        catch
        {
            return null;
        }
    }
}
