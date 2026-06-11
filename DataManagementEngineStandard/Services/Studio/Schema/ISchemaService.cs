// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Schema;

/// <summary>
/// Schema discovery + design. Implemented in Phase 4. Wraps the engine's
/// existing <c>EntityDiscoveryService</c> with a view-model surface and an
/// optional EF Core adapter (in a sibling assembly so the engine stays
/// EF-Core-free).
/// </summary>
public interface ISchemaService
{
    /// <summary>Scan an assembly for entity types and return their descriptors.</summary>
    Task<StudioResult<IReadOnlyList<EntityDescriptor>>> DiscoverAsync(EntityDiscoveryRequest request, IStudioProgress? progress = null, CancellationToken ct = default);

    /// <summary>Describe a single entity type in detail (columns, PK, FK).</summary>
    Task<StudioResult<EntityDescriptor>> DescribeAsync(string assemblyPath, string entityFullName, CancellationToken ct = default);
}

/// <summary>A request to scan an assembly for entity types.</summary>
public sealed record EntityDiscoveryRequest(
    string AssemblyPath,
    string? NamespaceName = null,
    bool IncludeSubNamespaces = true,
    EntityCategoryFilter CategoryFilter = EntityCategoryFilter.All,
    IReadOnlyList<string>? ExcludeAssembliesStartingWith = null);

/// <summary>Filter for the category of entities to return.</summary>
public enum EntityCategoryFilter
{
    /// <summary>Include every category.</summary>
    All = 0,

    /// <summary>Only entities that inherit from the engine's <c>Entity</c> base class.</summary>
    Entity = 1,

    /// <summary>Only entities that implement the engine's <c>IEntity</c> contract.</summary>
    IEntity = 2,

    /// <summary>Only entities decorated with EF Core attributes (via the optional adapter).</summary>
    EfCore = 3,

    /// <summary>Only plain POCOs with no engine or EF Core decoration.</summary>
    Poco = 4,

    /// <summary>Exclude any EF Core decorated types.</summary>
    ExcludeEfCore = 5
}

/// <summary>A discovered entity type. Bindable in any UI.</summary>
public sealed record EntityDescriptor(
    string Name,
    string FullName,
    string Namespace,
    string AssemblyName,
    string Category,
    IReadOnlyList<EntityPropertyDescriptor> Properties);

/// <summary>The engine's classification of a discovered entity.</summary>
public static class EntityCategories
{
    /// <summary>Inherits from the engine's <c>Entity</c> base class.</summary>
    public const string Entity = "Entity";

    /// <summary>Implements the engine's <c>IEntity</c> contract.</summary>
    public const string IEntity = "IEntity";

    /// <summary>Decorated with EF Core attributes (via the optional adapter).</summary>
    public const string EfCore = "EfCore";

    /// <summary>Plain POCO with no engine or EF Core decoration.</summary>
    public const string Poco = "Poco";
}

/// <summary>A single property of a discovered entity.</summary>
public sealed record EntityPropertyDescriptor(
    string Name,
    string TypeFullName,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsForeignKey,
    string? ReferencedEntity,
    int? MaxLength,
    bool IsAutoIncrement,
    bool IsUnique,
    string? DefaultValue);
