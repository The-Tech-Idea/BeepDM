# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

BeepDM is a .NET data-management engine: a plugin-based runtime that connects to many datasource types (RDBMS, NoSQL, REST, files, streaming, vector stores) behind one set of interfaces, plus the layers built on top — configuration, ETL, sync, migration, forms, and unit-of-work CRUD.

It ships as two NuGet packages, both multi-targeting `net8.0;net9.0;net10.0`:

| Project | Package | Role |
|---|---|---|
| `DataManagementModelsStandard/` | `TheTechIdea.Beep.DataManagementModels` | Contracts, models, enums. No dependency on the engine. |
| `DataManagementEngineStandard/` | `TheTechIdea.Beep.DataManagementEngine` | All implementation. References Models. |

Everything else in the repo is tests, docs, or scratch. Root namespace for both projects is `TheTechIdea.Beep.*`.

## Build and test

```bash
dotnet build BeepDM.sln

# Building one project against a single TFM is much faster than all three:
dotnet build DataManagementEngineStandard/DataManagementEngine.csproj -f net9.0

# All tests, or one project, or one test:
dotnet test BeepDM.sln
dotnet test tests/SetupWizardTests/SetupWizardTests.csproj                             # net9.0
dotnet test DataManagementEngineStandard/Editor/Forms.Tests/FormsManager.Tests.csproj  # net8.0
dotnet test tests/SetupWizardTests/SetupWizardTests.csproj --filter "FullyQualifiedName~SchemaHashTests"
```

Baselines: Forms.Tests 54/54 pass; SetupWizardTests 54/55 — `SetupWizardBuilderTests.Build_Throws_WhenStepsOutOfOrder` is a **pre-existing failure**, not something you broke.

`tests/IntegrationTests/` contains only build artifacts — no source. xUnit + Moq throughout.

Historical note: `BeepDM.sln` used to reference nine test projects that didn't exist on disk, so any solution-level build or test failed with MSB3202 before compiling. Those phantom entries were removed and the two real test projects added. If you see that error again, someone re-added a project that isn't on disk.

Two build side effects worth knowing: the engine emits ~9,100 warnings (mostly CA1416 platform warnings from `Installer/`) — that's normal, and `0 Error(s)` is the signal to look for. Both projects set `GeneratePackageOnBuild=True` and have post-build targets that copy **outside the repo**, to `../outputDLL/` and `../../../LocalNugetFiles/`.

## Doc claims that were false (and may resurface)

The `.github/` agent-instruction files predate significant refactors. `.github/claude.md` has been deleted (superseded by this file) and `.github/copilot-instructions.md` corrected, but `.clinerules`, `.cursorrules`, and `.windsurfrules` still carry the same stale claims. `README.md` is far more accurate than any of them. Verified corrections worth knowing, since these errors are copied around:

- There is **no `Assembly_helpersStandard` project**, and no `Beep.Shell`/`BeepShell` or `DataSourcesPluginsCore` in this repo. `AssemblyHandler.Core.cs` is real but lives at `DataManagementEngineStandard/AssemblyHandler/AssemblySystem/`.
- `AddBeepServices` / `IBeepService` are **in this repo** (`DataManagementEngineStandard/Services/`), not an external `Beep.Container` package. `Beep.Container` survives only as a *namespace*.
- **`DMEEditor.CreateUnitOfWork<T>()` does not exist.** Any example using it is wrong.
- `UnitofWork<T>` has no `AddNew()` or `Modify()`. The real methods are `New()`, `Add()`, `Update()`, `Delete()`, `Commit()`.
- The IDataSourceHelper files are not flat under `Helpers/UniversalDataSourceHelpers/`; they're nested in per-type subfolders.

When those files conflict with the code, the code wins. Prefer verifying over trusting any doc here.

## Architecture

`BeepService` bootstraps and wires the object graph; `DMEEditor` is the hub every consumer operation flows through.

```
BeepService (Services/)                    ← DI entry, LoadConfigurations(), LoadAssemblies()
  ├── ConfigEditor (ConfigUtil/)           ← persisted JSON state
  ├── AssemblyHandler (AssemblyHandler/)   ← plugin/driver/NuGet discovery
  ├── Util                                 ← conversion, driver linking
  └── DMEEditor (Editor/DM/)               ← datasources, logging, events, ETL
        └── UnitofWork<T> (Editor/UOW/)    ← consumer-facing CRUD
```

**DMEEditor** (`Editor/DM/`, namespace `TheTechIdea.Beep`, not `.Editor`) is `partial` across four files, split by concern rather than by feature: `DMEEditor.cs` (datasource lifecycle, `GetEntityStructure`, `AddLogMessage`, `RaiseEvent`), `DMEEditor.Services.cs` (lazy service properties only, no logic), `DMEEditor.UniversalDataSourceHelpers.cs`, `DMEEditor.MigrationProviders.cs`. Add feature-local changes to the right partial; the Editor README asks that new work go into focused managers/helpers rather than growing `DMEEditor`.

**ConfigEditor** (`ConfigUtil/ConfigEditor.cs`) is *not* partial — it's a façade delegating to six managers in `ConfigUtil/Managers/`: `ConfigPathManager` (paths), `DataConnectionManager` (`DataConnections.json`), `QueryManager` (`QueryList.json`), `EntityMappingManager` (`DDLCreateTables.json`), `ComponentConfigManager` (`ConnectionConfig.json`, workflows, projects, reports), `MigrationHistoryManager` (per-datasource migration files). Config lands in an OS-specific folder (`%ProgramData%\TheTechIdea\Beep` on Windows, `~/.config/...` on Linux), not the repo. Write config through the façade — never hand-roll JSON.

**How a connection becomes a live datasource** — this three-collection dance is the thing to understand before touching driver/plugin code:

1. `LoadConfigurations()` seeds `ConfigEditor.DataDriversClasses` (`ConnectionDriversConfig` — metadata: ADO driver names, connection-string templates) for ~60 database types.
2. `LoadAssemblies()` has `AssemblyHandler` scan DLLs into `ConfigEditor.DataSourcesClasses` (`AssemblyClassDefinition` — the actual `Type`s implementing `IDataSource`).
3. `GetDataSource(name)` finds the connection, links it to a driver config by `DataSourceType`, then bridges to the implementation by matching `driversConfig.classHandler` against `AssemblyClassDefinition.className`, and activates it.

That `classHandler` string is the only link between the metadata catalog and the code registry — a mismatched name is why a driver silently fails to resolve.

**Plugin discovery is interface-driven first, attribute-driven second.** `ScanAssembly` → `ProcessTypeInfo` routes types by which interface they implement (`IDataSource`, `IDM_Addin`, `ILoaderExtention`, `IWorkFlowAction`, `IRuleParser`, `IFileFormatReader`, `IDefaultValueResolver`, `ISchemaMigrationProvider`, ~15 total). `[AddinAttribute]` (defined in `DataManagementModelsStandard/Vis/AddinAttribute.cs`) then *enriches* the discovered class with `DatasourceType`/`Category`/order. It has **no constructor parameters** — always object-initializer syntax:

```csharp
[AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.CSV, FileType = "csv")]
```

Two `AssemblyHandler` implementations exist: the default (`Assembly.LoadFrom`) and `SharedContextAssemblyHandler` (`PluginSystem/`, `AssemblyLoadContext` isolation, lifecycle + health monitoring) for plugin-heavy hosts.

**IDataSourceHelper** generates per-dialect SQL/queries. Implementations sit in per-type subfolders under `Helpers/UniversalDataSourceHelpers/` (`RdbmsHelpers/RdbmsHelper.cs`, `MongoDBHelpers/`, `RedisHelpers/`, `CassandraHelpers/`, `RestApiHelpers/`, `GraphHelpers/`, `SearchHelpers/`, `TimeSeriesHelpers/`, `VectorHelpers/`, `StreamingHelpers/`, `FileHelpers/`, plus `Core/`). `DataSourceHelperFactory` (`Core/`) maps ~350 `DataSourceType` values to constructors, falling back to `DefaultDataSourceHelper`. Resolve via `dmEditor.GetDataSourceHelper(DataSourceType.MongoDB)`. Never hard-code a SQL dialect — go through the helper.

Note: **two unrelated interfaces are both named `IDataSourceHelper`.** The one above is `DataManagementModelsStandard/Editor/IDataSourceHelper.cs`; a different one lives in `Editor/BeepSync/Interfaces/ISyncHelpers.cs`.

## Conventions

**Errors.** Runtime data operations return `IErrorsInfo` (`DataManagementModelsStandard/ConfigUtil/`) rather than throwing — set `Flag` (an `Errors` enum: `Ok`, `Failed`, `Warning`, `Critical`, `Exception`, `Fatal`, …) and `Message`. Guard clauses and DI/config misuse *do* throw (`ArgumentNullException` etc.). So: expected data failures → `IErrorsInfo`; programmer error → throw.

**Logging** goes through `DMEEditor.AddLogMessage(...)` / `Logger.WriteLog(...)`, never `Console.WriteLine`.

**Never swallow exceptions silently.** A bare `catch { }` (or `catch (Exception) { }` with an empty/comment-only body) hides real faults — assembly load failures, `TypeLoadException`, transient I/O, version conflicts — and is not allowed. Every catch must **report** the exception through the error-management channel that is in scope, then it may still preserve its control flow (continue a retry/enumeration, fall through to the next candidate). Reporting channel, in priority order: (1) an `IDMEEditor` in scope → `editor?.AddLogMessage("<Class>", $"<context>: {ex.Message}", DateTime.Now, 0, null, Errors.Warning)` (use `Errors.Failed`/`Critical` for genuine failures, `Warning` for best-effort/skip paths); (2) else a class-local logger (`Logger`, `_logger`, `IDMLogger`); (3) else, only for low-level classes with neither (loggers, `Util`, pure helpers), capture `catch (Exception ex)` and `System.Diagnostics.Debug.WriteLine(...)` with a comment stating why continuing is safe — never a blind swallow. Do **not** invent an `IDMEEditor`/logger dependency (new field or ctor param) just to log; use the Debug+comment fallback instead. Guard the log call with `?.` so it can't itself throw. (For data-operation *failures* the method should still return `IErrorsInfo` per the Errors convention above — reporting in the catch is in addition to, not instead of, the return contract.)

**Progress and events.** Long operations take `IProgress<PassedArgs>` (`PassedArgs` is in `DataManagementModelsStandard/Addin/`). `PassEvent` is an event on `IDataSource`/`IETL` and the concrete `DMEEditor` — but it is *not* on the `IDMEEditor` interface, which instead exposes `Passedarguments` and `RaiseEvent(sender, args)`.

**Service lifetime** is Singleton for desktop/CLI, Scoped for web/Blazor (`AddBeepForDesktop` / `AddBeepForWeb` / `AddBeepForBlazorServer` shortcuts exist). Preserve existing registrations and lifetimes; add rather than change.

**Public API stability matters** — `IDMEEditor` (~47 members), `IDataSource`, `IConfigEditor`, and `UnitofWork<T>` are consumed by external apps and sibling repos. These ship as NuGet packages, so a signature change is a breaking release, not a local refactor.

**Misspellings are baked into the contracts.** Don't "fix" them casually — they're load-bearing across packages: `IErrorsInfo.Fucntion`, `PassedArgs.Messege`, `IBeepService.ConfigureationType`. Same for the lowercase members `DMEEditor.progress`, `DMEEditor.assemblyHandler`, and `IDMEEditor.progress`.

`DataManagementEngineStandard/globalusing.cs` globally imports 12 `TheTechIdea.Beep.*` namespaces (`Core`, `DataBase`, `Editor`, `Addin`, `ConfigUtil`, `Utilities`, `Services`, `Vis`, `Report`, `Logger`, `NuGet`, `Tools`) — engine files often need no explicit using for these. The Models project has no equivalent and imports explicitly.

## UnitofWork

`UnitofWork<T>` is `partial` across five files in `Editor/UOW/` (`.Core`, `.CRUD`, `.Core.Extensions`, `.Core.Utilities`, `.OBLIntegration`), with seven constructors and helpers in `Helpers/`. Construct it directly or via the static `UnitOfWorkFactory` (which returns the non-generic `IUnitOfWorkWrapper`):

```csharp
var uow = new UnitofWork<Product>(dmEditor, "northwind.db", "Products");
uow.Get(new List<AppFilter> { new AppFilter { FieldName = "CategoryId", FilterValue = "1" } });

var product = uow.New();
product.ProductName = "Widget";
uow.Add(product);
uow.Commit();
```

Forms saves must route through `UnitofWork<T>` — `FormsManager` never writes to a datasource directly.

## Known broken things

Don't mistake these for bugs you introduced:

- `DataManagementModelsStandard/Environments/XrefUser.cs` uses `namespace Beep.Container.Model` — the one file outside the `TheTechIdea.Beep.*` root.
- `RegisterBeepinServiceCollection.cs` appears to contain a duplicated block (`Register`/`RegisterScoped`/`CreateMapping`/`GetBeepService` recur ~line 894).
- `.github/.clinerules`, `.github/.cursorrules`, and `.github/.windsurfrules` still carry the stale claims described above (they say ".NET 9.0", "5 core / 9 planned" helpers, etc.). `.github/copilot-instructions.md` has been corrected and points here.

Recently fixed — if you see these patterns in older branches or docs, they're wrong:

- `DMEEditor.RegisterDataSourceHelper(...)` used to construct a **fresh** `DataSourceHelperFactory` per call, so registrations were discarded and `GetDataSourceHelper` never returned the custom helper. The factory is now a lazily-created per-editor instance (`DataSourceHelperFactoryInstance`) backed by a `ConcurrentDictionary`, since registration and resolution can now race on a shared instance.
- Two code generators (`Tools/Helpers/UiComponentGeneratorHelper.cs`, `Tools/Helpers/ServerlessGeneratorHelper.cs`) emitted calls to the nonexistent `DMEEditor.CreateUnitOfWork<T>()` plus `AddNew`/`Modify`. They now emit `new UnitofWork<T>(editor, "<datasource>", "<entity>")` with `Add`/`Update`, and the required `using`/`@using` directives.

## Docs and skills

`README.md` is the most accurate architecture reference — it documents the five core services in depth. `Docs/*.md` covers subsystems (`CoreArchitecture`, `HowToCreateNewDataSource`, `UnitOfWork`, `ETL`, `SetupFramework`, `RulesEngine`, `Proxy`); `Help/*.html` is the rendered equivalent. Per-folder `README.md` files are common and generally current.

Deep-dive skills for subsystems (setup, migration, etl, forms, unitofwork, configeditor, assemblyhandler, rdbms helpers, proxy, …) are duplicated across `.cursor/<name>/SKILL.md` and `.harness/skills/beepdm-<name>/SKILL.md`. **If you change how one layer hands off to another, update both locations** plus the integration map in `.github/`, or the agents drift apart.

`.plans/` holds a phased-plan + master-tracker workflow (`MASTER-TODO-TRACKER.md` with `PHASE-NN-*.md` documents). Multi-step work in this repo is expected to follow that structure.
