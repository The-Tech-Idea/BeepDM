// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.EntityDiscovery;
using TheTechIdea.Beep.Studio.Schema;

namespace TheTechIdea.Beep.Studio.Schema;

/// <summary>
/// Default implementation of <see cref="ISchemaService"/>. Wraps the engine's
/// <see cref="EntityDiscoveryService"/> with a <see cref="StudioResult{T}"/>-
/// shaped API and a file-system assembly scanner so hosts that don't load
/// plug-in assemblies via <see cref="Beep.Services.IBeepService"/> can still
/// discover their entities.
/// </summary>
public sealed class SchemaService : ISchemaService
{
    private readonly IDMEEditor _editor;
    private readonly EntityDiscoveryService _inner;

    public SchemaService(IDMEEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _inner = new EntityDiscoveryService(editor);
    }

    /// <inheritdoc />
    public Task<StudioResult<IReadOnlyList<EntityDescriptor>>> DiscoverAsync(
        EntityDiscoveryRequest request,
        IStudioProgress? progress = null,
        CancellationToken ct = default)
    {
        if (request == null)
            return Task.FromResult(StudioResult<IReadOnlyList<EntityDescriptor>>.Fail(StudioErrorCode.InvalidArgument, "request is required."));
        if (string.IsNullOrWhiteSpace(request.AssemblyPath))
            return Task.FromResult(StudioResult<IReadOnlyList<EntityDescriptor>>.Fail(StudioErrorCode.InvalidArgument, "request.AssemblyPath is required."));

        progress?.Report(new StudioProgressUpdate(
            OperationId: Guid.NewGuid().ToString("N"),
            OperationName: "Discovering entities",
            Stage: StudioProgressStage.Begin,
            CurrentStep: $"Loading {Path.GetFileName(request.AssemblyPath)}",
            Percent: 0,
            Severity: StudioProgressSeverity.Info,
            Timestamp: DateTimeOffset.UtcNow,
            Payload: null));

        try
        {
            if (!File.Exists(request.AssemblyPath))
                return Task.FromResult(StudioResult<IReadOnlyList<EntityDescriptor>>.Fail(
                    StudioErrorCode.NotFound,
                    $"Assembly file not found: {request.AssemblyPath}"));

            var assembly = Assembly.LoadFrom(request.AssemblyPath);
            var all = _inner.ScanAssemblyForEntities(assembly, request.IncludeSubNamespaces);
            IEnumerable<TheTechIdea.Beep.Editor.EntityDiscovery.DiscoveredEntity> q = all;

            if (!string.IsNullOrWhiteSpace(request.NamespaceName))
            {
                var ns = request.NamespaceName.Trim();
                q = request.IncludeSubNamespaces
                    ? q.Where(e => e.Namespace == ns || e.Namespace.StartsWith(ns + ".", StringComparison.Ordinal))
                    : q.Where(e => e.Namespace == ns);
            }

            if (request.ExcludeAssembliesStartingWith is { Count: > 0 })
            {
                q = q.Where(e => !request.ExcludeAssembliesStartingWith.Any(p =>
                    e.AssemblyName.StartsWith(p, StringComparison.OrdinalIgnoreCase)));
            }

            q = request.CategoryFilter switch
            {
                // PR 17 fix: the engine's EntityCategory enum is { Entity, Poco, EfCore, Unknown }.
                // There is no IEntity value, so the Entity filter and the IEntity filter both
                // map to the Entity category. If/when the engine grows a real IEntity category,
                // update both branches in the same commit.
                EntityCategoryFilter.Entity => q.Where(e => e.Category == TheTechIdea.Beep.Editor.EntityDiscovery.EntityCategory.Entity),
                EntityCategoryFilter.IEntity => q.Where(e => e.Category == TheTechIdea.Beep.Editor.EntityDiscovery.EntityCategory.Entity),
                EntityCategoryFilter.EfCore => q.Where(e => e.Category == TheTechIdea.Beep.Editor.EntityDiscovery.EntityCategory.EfCore),
                EntityCategoryFilter.Poco => q.Where(e => e.Category == TheTechIdea.Beep.Editor.EntityDiscovery.EntityCategory.Poco),
                EntityCategoryFilter.ExcludeEfCore => q.Where(e => e.Category != TheTechIdea.Beep.Editor.EntityDiscovery.EntityCategory.EfCore),
                _ => q
            };

            var list = q.Select(MapToDescriptor).ToList();

            progress?.Report(new StudioProgressUpdate(
                OperationId: Guid.NewGuid().ToString("N"),
                OperationName: "Discovering entities",
                Stage: StudioProgressStage.Complete,
                CurrentStep: $"Found {list.Count} entities",
                Percent: 100,
                Severity: StudioProgressSeverity.Info,
                Timestamp: DateTimeOffset.UtcNow,
                Payload: new Dictionary<string, object?> { ["count"] = list.Count }));

            return Task.FromResult(StudioResult<IReadOnlyList<EntityDescriptor>>.Ok(list));
        }
        catch (Exception ex)
        {
            progress?.Report(new StudioProgressUpdate(
                OperationId: Guid.NewGuid().ToString("N"),
                OperationName: "Discovering entities",
                Stage: StudioProgressStage.Failed,
                CurrentStep: ex.Message,
                Percent: 0,
                Severity: StudioProgressSeverity.Error,
                Timestamp: DateTimeOffset.UtcNow,
                Payload: null));

            return Task.FromResult(StudioResult<IReadOnlyList<EntityDescriptor>>.Fail(
                StudioErrorCode.InternalError,
                $"Failed to load assembly: {ex.Message}",
                ex));
        }
    }

    /// <inheritdoc />
    public async Task<StudioResult<EntityDescriptor>> DescribeAsync(string assemblyPath, string entityFullName, CancellationToken ct = default)
    {
        var list = await DiscoverAsync(new EntityDiscoveryRequest(assemblyPath));
        if (!list.IsSuccess) return StudioResult<EntityDescriptor>.Fail(list.Error.Code, list.Error.Message, list.Error.Exception);
        var match = list.Value?.FirstOrDefault(e => string.Equals(e.FullName, entityFullName, StringComparison.Ordinal));
        if (match == null)
            return StudioResult<EntityDescriptor>.Fail(StudioErrorCode.NotFound, $"Entity '{entityFullName}' was not found in {assemblyPath}.");
        return StudioResult<EntityDescriptor>.Ok(match);
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static EntityDescriptor MapToDescriptor(TheTechIdea.Beep.Editor.EntityDiscovery.DiscoveredEntity e) => new(
        Name: e.Name,
        FullName: e.FullName,
        Namespace: e.Namespace,
        AssemblyName: e.AssemblyName,
        Category: e.Category.ToString(),
        Properties: ResolveProperties(e));

    private static IReadOnlyList<EntityPropertyDescriptor> ResolveProperties(TheTechIdea.Beep.Editor.EntityDiscovery.DiscoveredEntity e)
    {
        try
        {
            var t = Type.GetType(e.FullName, throwOnError: false);
            if (t == null) return Array.Empty<EntityPropertyDescriptor>();

            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return props.Select(p => new EntityPropertyDescriptor(
                Name: p.Name,
                TypeFullName: p.PropertyType.FullName ?? p.PropertyType.Name,
                IsNullable: IsNullable(p.PropertyType),
                IsPrimaryKey: IsPrimaryKey(p),
                IsForeignKey: false,                            // not available at the property-discovery layer
                ReferencedEntity: null,
                MaxLength: GetMaxLength(p),
                IsAutoIncrement: false,                         // requires runtime check
                IsUnique: false,
                DefaultValue: null)).ToList();
        }
        catch
        {
            return Array.Empty<EntityPropertyDescriptor>();
        }
    }

    private static bool IsNullable(Type t)
    {
        if (!t.IsValueType) return true;                       // reference types are nullable
        var nt = Nullable.GetUnderlyingType(t);
        return nt != null;
    }

    private static bool IsPrimaryKey(PropertyInfo p)
    {
        var attrs = p.GetCustomAttributes(inherit: true);
        foreach (var a in attrs)
        {
            var name = a.GetType().Name;
            if (name == "KeyAttribute" || name == "Key" || name == "PrimaryKeyAttribute" || name == "PrimaryKey")
                return true;
        }
        return false;
    }

    private static int? GetMaxLength(PropertyInfo p)
    {
        var attrs = p.GetCustomAttributes(inherit: true);
        foreach (var a in attrs)
        {
            var pi = a.GetType().GetProperty("MaxLength") ?? a.GetType().GetProperty("Length");
            if (pi?.GetValue(a) is int len) return len;
        }
        return null;
    }
}
