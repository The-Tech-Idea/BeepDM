# Phase 05 — Schema Discovery & EF Core Interop (`ISchemaService`)

> **Scope:** implement `ISchemaService` — the Studio's schema-discovery sub-service.
> Given a project root (and optionally a target framework, namespace, or EF Core
> `DbContext` type), it scans the project's assemblies, finds every entity
> candidate (POCO, `Entity`, `IEntity`, EF Core decorated), and returns an
> `IReadOnlyList<EntityDescriptor>` that the host UI can render and the migration
> engine can use to build a plan.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

The engine's existing `Editor/EntityDiscovery/EntityDiscoveryService.cs:11` already
does CLR-side discovery. The Studio wraps that and adds:

1. **File-system assembly scan** — for hosts that load plug-in assemblies from
   `bin/Debug/net10.0/` and want to include them in the discovery scope.
2. **EF Core interop** (optional adapter) — for projects that already use
   EF Core and want to import their `DbContext`'s entity types **without**
   taking a hard EF Core dependency in the engine.
3. **Schema design** — a view-model builder that turns discovered entities
   into a `MigrationPlanVm` the host can render in the **Migrations** tab
   (Phase 6 / Blazor Phase 20).

The optional EF Core interop is the bridge that lets a Studio host consume
EF Core projects. **The engine itself stays EF-Core-free** — the EF Core
reference lives in a separate adapter assembly loaded by the host.

## Public surface (this phase fills in)

```csharp
// Contracts/ISchemaService.cs
public interface ISchemaService
{
    Task<StudioResult<IReadOnlyList<EntityDescriptor>>> DiscoverAsync(EntityDiscoveryRequest request, IStudioProgress? progress = null, CancellationToken ct = default);
    Task<StudioResult<EntityDescriptor>> DescribeAsync(string assemblyPath, string entityFullName, CancellationToken ct = default);
    Task<StudioResult<MigrationPlanVm>> DesignAsync(SchemaDesignRequest request, CancellationToken ct = default);
}

// Models
public sealed record EntityDiscoveryRequest(
    string AssemblyPath,
    string? NamespaceName = null,
    bool IncludeSubNamespaces = true,
    EntityCategoryFilter CategoryFilter = EntityCategoryFilter.All,
    IReadOnlyList<string>? ExcludeAssembliesStartingWith = null);

public enum EntityCategoryFilter { All, Entity, IEntity, EfCore, Poco, ExcludeEfCore }

public sealed record EntityDescriptor(
    string Name,
    string FullName,
    string Namespace,
    string AssemblyName,
    EntityCategory Category,
    IReadOnlyList<EntityPropertyDescriptor> Properties);

public enum EntityCategory { Entity, IEntity, EfCore, Poco }

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

public sealed record SchemaDesignRequest(
    string SourceName,                                  // the target connection
    IReadOnlyList<EntityDescriptor> Entities,
    bool DetectRelationships = true,
    bool ApplyForeignKeys = false,
    bool ApplyIndexes = false);
```

## Folder layout (this phase creates)

```
Services/Studio/
├── Contracts/ISchemaService.cs                       ← DONE in Phase 1
├── Models/
│   ├── EntityDescriptor.cs
│   ├── EntityPropertyDescriptor.cs
│   ├── EntityDiscoveryRequest.cs
│   ├── EntityCategoryFilter.cs
│   ├── EntityCategory.cs
│   ├── SchemaDesignRequest.cs
│   └── MigrationPlanVm.cs                            ← also used by Phase 6
├── Schema/
│   ├── SchemaService.cs                              ← implements ISchemaService
│   ├── AssemblyScanner.cs                            ← file-system assembly scan
│   ├── EntityDiscoveryAdapter.cs                     ← wraps EntityDiscoveryService
│   ├── EntityClassifier.cs                           ← sorts entities into the 4 categories
│   ├── SchemaDesigner.cs                             ← builds a MigrationPlanVm
│   └── PocoMetadataReader.cs                         ← reads [Key], [MaxLength], etc.
└── Adapters/EfCore/
    ├── EfCoreEntityAdapter.cs                        ← SEPARATE assembly, loaded by hosts only
    ├── EfCoreDbContextIntrospector.cs                ← DbContext → EntityDescriptor[]
    └── EfCoreEntityAdapterOptions.cs
```

## Assembly scanner

`AssemblyScanner` extends the engine's existing `EntityDiscoveryService` with
file-system awareness:

```csharp
public sealed class AssemblyScanner
{
    public Task<IReadOnlyList<DiscoveredAssembly>> ScanAsync(string root, IStudioProgress? progress, CancellationToken ct);
    public IEnumerable<Type> LoadTypesFromAssembly(string assemblyPath, IReadOnlyList<string>? excludeNamespaces);
}

public sealed record DiscoveredAssembly(
    string Path,
    string Name,
    string TargetFramework,
    long SizeBytes,
    IReadOnlyList<EntityDescriptor> Entities);
```

It walks `bin/Debug/<tfm>/` and `bin/Release/<tfm>/` and every `.dll` it finds
is passed through the existing `EntityDiscoveryService.DiscoverAllEntities(...)`.

## EF Core interop (separate, optional)

`Adapters/EfCore/EfCoreEntityAdapter.cs` lives in its own project
(`BeepDMS.Studio.EfCoreAdapter`) and is **not** referenced by the engine.
Hosts that want EF Core interop reference both the engine and this adapter.

The adapter uses `Microsoft.EntityFrameworkCore` (relational or design-time)
to introspect a `DbContext` and produce `EntityDescriptor` records:

```csharp
public interface IEfCoreEntityAdapter
{
    Task<IReadOnlyList<EntityDescriptor>> IntrospectAsync(Type dbContextType, CancellationToken ct);
    Task<IReadOnlyList<EntityDescriptor>> IntrospectAsync(string dbContextTypeName, CancellationToken ct);
}
```

The Blazor host (or the WebApi host) can load this adapter via
`IStudioHostAdapter.LoadEfCoreAdapter()` (Phase 9). Console and WPF hosts
that don't need EF Core simply don't load it.

The adapter is **read-only** — it does not generate migrations, it does not
mutate the `DbContext`, and it does not call `EnsureCreated`. Its sole job
is to **export** EF Core entity metadata into the Studio's view-model format
so the same migration engine can plan against it.

## Schema design

`SchemaService.DesignAsync` turns a `SchemaDesignRequest` into a
`MigrationPlanVm` by calling the engine's existing
`IMigrationManager.BuildMigrationPlanForModel(...)`. The result is wrapped
in a view-model with bindable properties for the Blazor/WPF/WinForms host.

`MigrationPlanVm` is the same type used by Phase 6 — the host's
**Migrations** tab uses the same view-model whether the entities came from
POCO discovery (this phase) or were explicitly passed in.

## Cross-cutting

- The engine itself does **not** reference `Microsoft.EntityFrameworkCore.*`.
- The EF Core adapter is a separate assembly. Its only API surface is
  `IEfCoreEntityAdapter` — a small, mockable interface.
- `EntityClassifier` (sorts entities into the 4 categories) uses the same
  rules as the engine's `EntityDiscoveryService` (CLR-side checks for
  `Entity` / `IEntity` / `IsEfDecoratedType` / `IsDiscoverablePoco`).
- `SchemaService.DesignAsync` is **idempotent** — calling it twice with the
  same request returns the same plan (cached by plan-hash).

---

## Todo Tracker

| # | Task | Status | Notes |
|---|------|--------|-------|
| P05-01 | `Models/EntityDescriptor.cs` + `EntityPropertyDescriptor.cs` | ⬜ | |
| P05-02 | `Models/EntityDiscoveryRequest.cs` + `EntityCategoryFilter.cs` + `EntityCategory.cs` | ⬜ | |
| P05-03 | `Models/SchemaDesignRequest.cs` + `Models/MigrationPlanVm.cs` (placeholder — Phase 6 fills in) | ⬜ | |
| P05-04 | `Schema/AssemblyScanner.cs` — file-system scan | ⬜ | |
| P05-05 | `Schema/EntityDiscoveryAdapter.cs` — wraps the engine's `EntityDiscoveryService` | ⬜ | |
| P05-06 | `Schema/EntityClassifier.cs` — categorizes each discovered type | ⬜ | |
| P05-07 | `Schema/PocoMetadataReader.cs` — reads `[Key]`, `[MaxLength]`, `[Required]`, etc. (from `System.ComponentModel.DataAnnotations` only — no EF Core dep) | ⬜ | |
| P05-08 | `Schema/SchemaDesigner.cs` — calls `IMigrationManager.BuildMigrationPlanForModel` | ⬜ | |
| P05-09 | `Schema/SchemaService.cs` — implements `ISchemaService` | ⬜ | |
| P05-10 | Wire `ISchemaService` into `AddBeepStudio()` | ⬜ | |
| P05-11 | Create the separate `BeepDMS.Studio.EfCoreAdapter` project (sibling of the engine) | ⬜ | |
| P05-12 | `Adapters/EfCore/IEfCoreEntityAdapter.cs` + `EfCoreEntityAdapter.cs` + `EfCoreDbContextIntrospector.cs` | ⬜ | |
| P05-13 | `Adapters/EfCore/EfCoreEntityAdapterOptions.cs` (e.g. which `DbContext` factories to load) | ⬜ | |
| P05-14 | Tests: `AssemblyScannerTests` (3+), `SchemaServiceTests` (3+), `PocoMetadataReaderTests` (2+), `EfCoreEntityAdapterTests` (3+, with an in-memory `DbContext`) | ⬜ | |
| P05-15 | Document: optional EF Core interop contract + when to use the adapter | ⬜ | |
| P05-16 | Update `00-overview-and-scope.md` + `MASTER-TODO-TRACKER.md` to mark Phase 05 done | ⬜ | |

---

## Validation (definition of done)

- [ ] `dotnet build DataManagementEngineStandard` succeeds with **0 errors** (no EF Core reference).
- [ ] `SchemaService.DiscoverAsync` on a sample project (a small console app with 3 POCO entities) returns 3 `EntityDescriptor` records.
- [ ] `EntityClassifier` categorizes the 3 sample entities correctly (1 `Entity`, 1 `IEntity`, 1 `Poco`).
- [ ] `SchemaService.DesignAsync` on the 3 entities produces a `MigrationPlanVm` with 3 create operations.
- [ ] The `BeepDMS.Studio.EfCoreAdapter` project builds standalone and produces a `IEfCoreEntityAdapter` that returns the right entity count for a test `DbContext` (2 entities, one with a `Guid` PK).
- [ ] The engine project file (`DataManagementEngineStandard.csproj`) has **no** `Microsoft.EntityFrameworkCore.*` package references.
- [ ] All 11+ new tests pass.

---

## Pitfalls

1. **Don't add `Microsoft.EntityFrameworkCore.*` to the engine** — the adapter goes in a sibling project. The engine must stay EF-Core-free.
2. **Don't re-implement the CLR-side entity detection** — call into the engine's `EntityDiscoveryService`. The Studio's `EntityDiscoveryAdapter` is a thin shim.
3. **Don't call `IDataSource.GetEntityStructure` during discovery** — that's a database round-trip and shouldn't be in the hot path of scanning local assemblies. Get the structure lazily, when the user actually opens an entity in the host UI.
4. **Don't cache the `MigrationPlanVm` across host restarts** — it must be rebuilt on every host start because the `MigrationPlanArtifact` includes a `planHash` that the engine recomputes from the live assembly metadata.
5. **Don't crash on a plug-in assembly that doesn't load** — the scanner must report the failure and continue.
6. **Don't trust `[Key]` / `[MaxLength]` annotations** from untrusted assemblies — only read them from assemblies in the project's own `bin/` folder.

---

## Related

- Phase 01 — contracts (this phase implements `ISchemaService`)
- Phase 06 — migration orchestration (consumes the plan from `DesignAsync`)
- Phase 09 — platform adapters (the EF Core adapter is loaded by the host adapter)
- `BeepDM/DataManagementEngineStandard/Editor/EntityDiscovery/EntityDiscoveryService.cs:11` — the existing scanner we wrap
- `BeepDM/DataManagementEngineStandard/Editor/Migration/IMigrationManager.cs` — `BuildMigrationPlanForModel` consumed by `SchemaDesigner`
