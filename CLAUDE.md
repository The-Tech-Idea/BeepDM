<!-- smarterasp-pem-rule:2026-10-02 -->
> **Fahad's SmarterASP hosting rule (2026-10-02):** Use IdentityServer's PEM RSA key method,
> parsed in memory. No PFX/PKCS#12 loading, Windows certificate store or user-profile key container;
> do not suggest import-flag retries or changing `Load User Profile`. Follow the hosting rule in
> `The-Tech-Idea/CLAUDE.md` and IdentityServer's `CredentialFiles.RsaKey`/`PemKeyRingCertificate` owners.
> Family Needs uses `DataProtection:Key`, never `CertificatePath`/`CertificatePassword`.
> Preserve each installation's existing keys and database key ring; no new key on startup, disabled
> encryption, key-ring deletion or copying IdentityServer's private key into another application.
> This supersedes older PFX deployment examples. Authentication migration still requires each app
> to protect its own cookies and stored secrets. Check deployed builds and log timestamps.
<!-- /smarterasp-pem-rule -->

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

Verified 2026-10-03 with `dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false`:
5,178 passing executions, zero failures/skips. Forms has 221 tests, Setup 232,
Studio 66, Migration 160 and FrameworkReliabilityTests 1,393 per TFM on net8/9/10.
The reliability suite includes child-process persistence and typed restart tests
on Windows; this does not establish Unix, live-provider or package-consumer gates.
The setup full-install fixture was corrected to use feed-resolved URLs. Do not
treat old documented test failures as permission to ignore a new failure.
See `.plans/framework/IMPLEMENTATION-LOG.md` for remaining implementation gates.
Optional migration ownership storage adds 54 cases/TFM: live-owner handles,
abandoned claims and explicit reconciliation. MigrationManager does not yet acquire
the capability automatically; read Editor/Migration/EXECUTION-OWNERSHIP.md. Initial
timing/fixture qualification failures remain in the log; broad CI stability is open.
Governed migration plans use version 2 schema/target/policy identity. See
`Editor/Migration/PLAN-INTENT.md` for policy revisions, hash-bound approval options,
legacy rejection and conservative partial-DDL resume limits.
Governed execution requires optional IMigrationHistoryPersistence; start/Running
checkpoint failures admit no DDL and post-DDL save failures require reconciliation.
ConfigEditor implements it with strict coordinated history snapshots. Custom loaders
need IJsonSnapshotCodec; legacy history void save/append now throw on failure.
See Services/Persistence/README.md for promotion and scheduling/storage limits.
Connection saves expose optional IConnectionConfigurationPersistence; legacy void
saves now throw on failure. Built-in fallback/catalog snapshots protect defined
credential containers with captured injectable policy. Catalog packages use 2.0;
old-key/corrupt evidence blocks mutation. Post-save observers cannot reclassify
durable success. Read Security/README.md for key ownership, DPAPI scope, exports,
mixed-reader/fallback downgrade limitations and unfinished all-route/Unix gates.
BeepSync built-in storage uses typed versioned envelopes, coordinated per-schema
updates and immutable hashed artifacts. Checkpointed runs require acknowledged
Running and Completed; terminal failure retains counts and requires reconciliation.
Post-commit diagnostics do not reclassify Success. Schema/cursor saves remain
explicit, not transactional with Completed. Read Editor/BeepSync/STORAGE-AND-OUTCOMES.md
for legacy rejection, roots, diagnostic failures, promotion/DQ and replay limits.
Import built-in required transformation failures now deny the row write with typed
safe outcomes. RecordsTransformationFailed is part of RecordsFailed, not uncertain
provider work; these failures stop whole-sync replay and preserve previous cursors.
Legacy built-in stage methods surface safe exceptions instead of input fallback.
Read Editor/Importing/TRANSFORMATION-OUTCOMES.md for explicit defaults, conversion
opt-ins and custom helpers. Read Editor/Importing/DEFAULTS-ADMISSION.md for required
editor-owned resolvers, one normalization/cache bypass, safe reported fallback
denial, owned catalogs/literals/rosters and remaining policy/plugin gates.
Required shipped outer arity/token routing now rejects ignored arguments and
time/hash sequence demos; nested date calls and GUID aliases are covered.
Exact shipped required expression/formula ASTs qualify bounded syntax, Boolean-only
conditions, precedence, typed exact comparisons, invariant numbers, Decimal rounding
and lazy branches. Nested overrides retain pinned selection; unused branches are
syntax-checked, not semantically prequalified. Broader query/identity/NFEL/numeric/
provider semantics and complete policy/plugin intent remain open.
Required shipped query plans validate all filters before callbacks, bind actual
closed invariant context and retain typed aggregates instead of skipping malformed
values. Null collections deny; real empty COUNT/EXISTS return 0/false. Streaming
limits/root-status/cancellation/disposal preserve Partial acknowledgements. Explicit
query context and provider allocation, hidden errors, isolation/translation remain
unqualified; direct/custom query semantics stay separate.
Required shipped identity/scope defaults require explicit string email/application
role keys, never imported-row identity or fabricated/group fallback. OS SID/name/
verified membership and named folders use actual APIs; native Downloads balances
COM/task-memory ownership. ENV reads only its declared scope; SYSTEMPATH means
Machine PATH. Host strings are not authentication/authorization proof or immutable
run context. Read defaults admission for platform/resource/custom limitations.
Required shipped dates use Gregorian ISO literals, invariant bounded formatting
and exact tick/calendar offsets. Nested bases resolve through the pinned roster
with actual context and must be DateTime/DateTimeOffset, not reparsed strings.
Kinds/offsets remain intact; dynamic clock leaves are not immutable run time or
named-zone DST qualification. Legacy direct and subclass semantics stay separate.
Required configuration uses explicit named flat string maps or declared Process
prefixes, never inferred editor/row/bare-variable sources. Validate the complete
bounded map before key selection; explicit-source failure and connection alias
conflicts deny. Capture is per resolution, not an automatic host/credential bridge
or immutable run configuration. See defaults admission for source names/limits.
NFEL-1 now uses bounded complete grammar and an owned actual RuleEngine execution
tree. Subtraction/decoded strings/unary/power/ternary/lazy Boolean branches, supplied
token/source admission, lifecycle/policy/timeout and bounded defensive history add
99 cases per TFM. Select an actual NfelParser; wrappers/RuleType strings do not switch
profiles. Read Engine Rules/NFEL.md for binary64/exact-parameter comparison behavior,
transport/old-token admission and callback/adapter/sandbox limits. Other parser/
helper/profile/provider and complete P1-P5 gates remain open.
Required-only dot parsing preserves quoted/empty literals and denies missing
segments; grouping quotes retain declared roles and public legacy parsing is unchanged.
Read
Editor/Importing/QUALITY-ADMISSION.md for actual post-transform/pre-write ordinary
and sync record admission, captured required/advisory policy, acknowledged
quarantine counts and remaining durable reject-recovery limits. Read
Editor/BeepSync/THRESHOLDS-AND-FAILURE-EVIDENCE.md for captured required/advisory
attempt thresholds, explicit record-only opt-out, real/empty denominators and
acknowledged Failed checkpoints with original counts. These slices do not establish
complete intent, cursor agreement or provider-run ownership. Read
Editor/Importing/REJECT-RECOVERY.md for actual operator-prepared/CAS-claimed file
reject replay. Only provider acknowledgements mark recovery; uncertain writes
remain blocked. Row recovery never completes a failed sync run or advances cursors.
Existing-target
mapped sync now binds captured provider metadata into both destination field lists,
validates renames/pairs/required targets and both bidirectional configs before
writes. RequireBoundMappingMetadata rejects missing targets rather than inferring
mapped DDL, even with creation enabled. Mapped creation/providers remain unqualified.

`tests/IntegrationTests/` contains only build artifacts — no source. xUnit + Moq throughout.

EntityField.Clone now terminates without copying source observers. For independent
run metadata use Models EntityMetadataSnapshot.Capture; legacy EntityStructure.Clone
remains shallow. Read DataManagementModelsStandard/DataBase/METADATA-SNAPSHOTS.md
for supported graphs, aliases, failure limits and caller coordination. This API
alone does not automatically bind metadata; sync now explicitly binds existing-target
mapped configs. Policies/context are not complete immutable intent. DM/factory/compiler generation
now keys by generated source identity and exact requested types. Public bare-name
cache seeds no longer override metadata; static MyType/MyObject are not authority.
Read Engine ConfigUtil/GENERATED-TYPES.md for single-flight/bounds, legacy behavior
changes, loaded assembly lifetime and remaining provider/consumer gates.

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

**Service lifetime** is a host decision: use deferred `AddBeepRuntime` with Singleton for a single desktop/CLI runtime or Scoped for hosted requests. The `AddBeepForDesktop/Web/BlazorServer` shortcuts are not implemented APIs here. Legacy `AddBeepServices`/builder `Build()` return a separate caller-owned eager runtime. Transient aliases can resolve different graphs; resolve `IBeepService` once for coherent transient use. See `Services/RUNTIME-OWNERSHIP.md` and the `beepdm-runtime` skill for datasource ownership and shutdown limits.

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
- `.github/.clinerules`, `.github/.cursorrules`, and `.github/.windsurfrules` still carry the stale claims described above (they say ".NET 9.0", "5 core / 9 planned" helpers, etc.). `.github/copilot-instructions.md` has been corrected and points here.

Recently fixed — if you see these patterns in older branches or docs, they're wrong:

- `DMEEditor.RegisterDataSourceHelper(...)` used to construct a **fresh** `DataSourceHelperFactory` per call, so registrations were discarded and `GetDataSourceHelper` never returned the custom helper. The factory is now a lazily-created per-editor instance (`DataSourceHelperFactoryInstance`) backed by a `ConcurrentDictionary`, since registration and resolution can now race on a shared instance.
- Two code generators (`Tools/Helpers/UiComponentGeneratorHelper.cs`, `Tools/Helpers/ServerlessGeneratorHelper.cs`) emitted calls to the nonexistent `DMEEditor.CreateUnitOfWork<T>()` plus `AddNew`/`Modify`. They now emit `new UnitofWork<T>(editor, "<datasource>", "<entity>")` with `Add`/`Update`, and the required `using`/`@using` directives.

## Docs and skills

`README.md` is the most accurate architecture reference — it documents the five core services in depth. `Docs/*.md` covers subsystems (`CoreArchitecture`, `HowToCreateNewDataSource`, `UnitOfWork`, `ETL`, `SetupFramework`, `RulesEngine`, `Proxy`); `Help/*.html` is the rendered equivalent. Per-folder `README.md` files are common and generally current.

### Help/*.html is mirrored to the website — always update both

`Help/` is published as the BeepDM product documentation on the TheTechIdea website. The website copy lives **outside this repo**, in a different repository:

```
C:\Users\f_ald\source\repos\fahadTheTechIdea\MyWebSite\TheTechIdeaWeb\TheTechIdeaWeb.Web\wwwroot\Products Documentation\beepdm\
```

**Whenever you add, edit, or delete a file under `Help/`, make the identical change in that folder in the same session.** A Help edit that isn't mirrored ships stale docs to the live site. This covers `.html`, `sphinx-style.css`, and `navigation.js` — everything except `README.md` and `NAVIGATION_README.md`, which are internal to the repo and deliberately not published.

**The theme is shared, so pages render identically.** `sphinx-style.css` and `navigation.js` are byte-identical in both locations, every page links them relatively (`href="sphinx-style.css"`), and the pages are complete standalone HTML documents served straight from `wwwroot` — they are *not* injected into a Razor layout. So a copied page picks up the same theme; there is no destination-specific CSS to reconcile.

**But the mirror is not a blind copy — diff before overwriting.** One file is intentionally different:

- `formsmanager.html` — the public copy has references to the internal `DataManagementEngineStandard/Editor/Forms/.plans` folder **stripped**. Internal repo paths, `.plans/` links, and anything that only makes sense to someone with a checkout must not be published. If you edit this file, re-apply that removal on the website side rather than copying it verbatim.

Always `diff --strip-trailing-cr` the file *before* overwriting it. If the website copy already differs, find out why before clobbering it — the difference may be deliberate sanitization (as above) or the website copy may be the *better* one.

Apart from that file, the folders are a file-for-file content-identical mirror; the only other difference is line endings — `Help/` uses **LF**, the website copy uses **CRLF**. Preserve that when copying:

```powershell
$src = "C:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\Help"
$dst = "C:\Users\f_ald\source\repos\fahadTheTechIdea\MyWebSite\TheTechIdeaWeb\TheTechIdeaWeb.Web\wwwroot\Products Documentation\beepdm"
foreach ($f in @('changed-file.html')) {
  $t = [IO.File]::ReadAllText((Join-Path $src $f)) -replace "`r`n","`n" -replace "`n","`r`n"
  [IO.File]::WriteAllText((Join-Path $dst $f), $t, (New-Object Text.UTF8Encoding $false))
}
```

Verify with `diff --strip-trailing-cr Help/<file> "<website>/<file>"` — it must report no differences. Note the website path contains a space (`Products Documentation`), so quote it.

Because the mirror is a **separate git repository**, it needs its own commit. Don't assume committing in BeepDM publishes anything; mention the website repo has uncommitted changes so the user can commit and deploy it.

Deep-dive skills for subsystems (setup, migration, etl, forms, unitofwork, configeditor, assemblyhandler, rdbms helpers, proxy, …) are duplicated across `.cursor/<name>/SKILL.md` and `.harness/skills/beepdm-<name>/SKILL.md`. **If you change how one layer hands off to another, update both locations** plus the integration map in `.github/`, or the agents drift apart.

`.plans/` holds a phased-plan + master-tracker workflow (`MASTER-TODO-TRACKER.md` with `PHASE-NN-*.md` documents). Multi-step work in this repo is expected to follow that structure.
