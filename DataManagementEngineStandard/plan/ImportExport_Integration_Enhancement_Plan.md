# DataImportManager — ImportExport Integration & Enhancement Plan

## 1. Executive Summary

`DataImportManager` (BeepDM core) and `ImportExportOrchestrator` / `ImportExportContextStore`
(Beep.Winform UI layer) currently operate as two independent worlds connected only by a
hand-built string-key parameter dictionary. The gaps are:

| Gap | Impact |
|---|---|
| `DataImportConfiguration` has no `ExecutionOptions` concept | Orchestrator rebuilds options inline; no round-trip |
| No shared `IImportContext` | UI layer clones/parses context manually via string keys |
| `RunMigrationPreflight` + sync-draft live in the UI Orchestrator | Core reusable logic is UI-locked |
| `IDataImportManager` interface has no pause/resume/cancel/status API | Orchestrator accesses them only via concrete type |
| Mapping has no `AutoMap` / convention-based builder callable from the wizard | Wizard calls `MappingManager.CreateEntityMap` cold; no dry-run |
| No batch-error recovery strategy or retry policy surface | Failures stop silently |
| `ImportStatus` type is not defined in shared interfaces | `GetStatus()` compiles by luck against the concrete class |

This plan fixes all gaps in **5 phases** across two projects:
- `BeepDM/DataManagementEngineStandard` (core — always changed first)
- `Beep.Winform/TheTechIdea.Beep.Winform.Default.Views` (UI layer)

---

## 1.1 BeepDM Implementation Conventions

These three cross-cutting skills apply throughout the commercial phases and **must** be
followed by any implementation.

### EnvironmentService — folder creation & registration
*(file: `DataManagementEngineStandard/Services/EnvironmentService.cs`)*

All file-backed stores introduced in commercial phases (WatermarkStore, ErrorStore,
RunHistoryStore, SchemaSnapshotStore) **must resolve their root path via
`EnvironmentService`** — never hard-code a path.

```csharp
// Recommended default base path for all import infrastructure files:
var importFolder = EnvironmentService.CreateAppfolder("Importing");
// Results in: <BeepRoot>/Importing/

// Then sub-folders per store:
var watermarkPath  = Path.Combine(importFolder, "Watermarks");
var errorPath      = Path.Combine(importFolder, "Errors");
var historyPath    = Path.Combine(importFolder, "History");
var snapshotPath   = Path.Combine(importFolder, "SchemaSnapshots");
```

Each store's default constructor should call `EnvironmentService.CreateAppfolder` and
create its sub-folder with `Directory.CreateDirectory`.

### ConnectionProperties — configuring a backing DataSource store
*(files: `DataManagementModelsStandard/ConfigUtil/ConnectionProperties.cs`, `IConnectionProperties.cs`)*

**Key discriminator fields on `ConnectionProperties`:**

| Property | Type | Role |
|---|---|---|
| `DatabaseType` | `DataSourceType` enum | Identifies the engine (e.g. `SqlLite`, `LiteDB`, `SqlServer`) |
| `Category` | `DatasourceCategory` enum | Broad class: `RDBMS`, `FILE`, `NOSQL`, `WEBAPI` … |
| `IsLocal` / `IsFile` / `IsDatabase` | `bool` flags | Used by `ConfigEditor` queries and `ConnectionDriverLinkingHelper` to filter candidates |

**Step 1 — Check for an already-registered local database first**

Before creating a new store connection, inspect `ConfigEditor.DataConnections` to reuse an
existing local DB (avoids duplicates on restart):

```csharp
var existing = editor.ConfigEditor.DataConnections
    .FirstOrDefault(c =>
        c.IsLocal &&
        c.IsFile &&
        c.IsDatabase &&
        c.Category == DatasourceCategory.RDBMS &&
        c.ConnectionName == $"ImportStore_{contextKey}");

if (existing != null)
    return editor.GetDataSource(existing.ConnectionName);
```

**Step 2 — Discover the installed local driver with `ConnectionDriverLinkingHelper`**

Do not hard-code `DataSourceType.SqlLite`. Instead, probe the installed drivers:

```csharp
// Use ConnectionDriverLinkingHelper to find the best local driver
// (resolves to whatever local RDBMS driver is installed — SQLite, LiteDB, etc.)
var driverConfig = ConnectionDriverLinkingHelper.LinkConnection2Drivers(
    new ConnectionProperties
    {
        IsLocal    = true,
        IsFile     = true,
        IsDatabase = true,
        Category   = DatasourceCategory.RDBMS
    },
    editor.ConfigEditor);

// If no specific driver found, fall back to the first locally-installed one
driverConfig ??= editor.ConfigEditor.DataDriversClasses
    .FirstOrDefault(c => c.IsLocal);

// Resolve DatabaseType from the discovered driver
var dbType = driverConfig?.DatasourceType ?? DataSourceType.SqlLite;
```

**Step 2b — ⚠️ Fallback when NO local database driver is installed**

`LinkConnection2Drivers` returns `null` **and** `DataDriversClasses.FirstOrDefault(c => c.IsLocal)` also returns `null` when the runtime has no local database plugin at all (e.g. a minimal deployment or a web host with no SQLite NuGet package loaded).

All four store implementations (error store, run history, watermark, schema snapshot) must
follow this selection chain:

```csharp
// Centralise in a helper used by all store constructors:
public static IImportErrorStore CreateErrorStore(IDMEEditor editor, string contextKey)
{
    bool hasLocalDriver = editor.ConfigEditor.DataDriversClasses
        .Any(c => c.IsLocal);

    if (!hasLocalDriver)
    {
        // Zero-dependency fallback — JSONL file, no database required
        var folder = EnvironmentService.CreateAppfolder("Importing");
        return new JsonFileImportErrorStore(Path.Combine(folder, "Errors"));
    }

    // DataSource-backed store — full query / replay support
    return new DataSourceImportErrorStore(editor, contextKey);
}
```

Apply the same pattern for `IImportRunHistoryStore`, `IWatermarkStore`, and
`ISchemaSnapshotStore`:

| Store | Has local driver | No local driver |
|---|---|---|
| `IImportErrorStore` | `DataSourceImportErrorStore` | `JsonFileImportErrorStore` |
| `IImportRunHistoryStore` | `DataSourceImportRunHistoryStore` | `JsonFileImportRunHistoryStore` |
| `IWatermarkStore` | `DataSourceWatermarkStore` (future) | `FileWatermarkStore` |
| `ISchemaSnapshotStore` | `DataSourceSchemaSnapshotStore` (future) | `JsonFileSchemaSnapshotStore` |

The JSON file stores are therefore not optional niceties — they are **the required fallback
for all store types**. Every store interface must have a JSON file implementation before
the DataSource-backed implementation can be considered optional.

**Step 3 — Build the `ConnectionProperties` using the resolved type** *(skip if no local driver)*

```csharp
var storeConn = new ConnectionProperties
{
    ConnectionName = $"ImportStore_{contextKey}",
    DatabaseType   = dbType,                          // resolved above — not hard-coded
    Category       = DatasourceCategory.RDBMS,
    IsLocal        = true,
    IsFile         = true,
    IsDatabase     = true,
    FilePath       = errorPath,                       // from EnvironmentService
    FileName       = "import_errors.db",
    DriverName     = driverConfig?.PackageName ?? string.Empty,
    ConnectionString = $"Data Source={Path.Combine(errorPath, "import_errors.db")};"
};

editor.ConfigEditor.AddDataConnection(storeConn);
editor.ConfigEditor.SaveDataconnectionsValues();
```

`ConnectionName` must be unique. Use the `contextKey` to disambiguate multiple stores.

### ILocalDB — safe file creation for local stores
*(file: `DataManagementModelsStandard/DataBase/ILocalDB.cs`)*

After the `ConnectionProperties` is registered (Step 3 above), obtain the datasource and
initialise the file before the first write. Cast sequence must follow the hierarchy:
`SQLiteDataSource : InMemoryRDBSource : RDBSource` and `LiteDBDataSource` — both implement
`ILocalDB`, so the cast is safe for any locally-installed file-based driver:

```csharp
var ds = editor.GetDataSource(storeConn.ConnectionName);

if (ds is ILocalDB localDb)
{
    localDb.CreateDB();           // creates the .db file on disk if missing
}
ds.Openconnection();
```

Always call `ds.Closeconnection()` before any `ILocalDB.CopyDB()` or `DeleteDB()` call.
The `DatabaseType` and `Category` on `storeConn` ensure `GetDataSource` loads the correct
driver plugin — which is why Step 2 (driver discovery via `ConnectionDriverLinkingHelper`)
must complete successfully before `AddDataConnection` is called.

### Concrete DataSource Hierarchy — know before writing stores & profiling

Understanding which class to cast to prevents wrong API calls at runtime:

```
IRDBSource
  └── RDBSource                         ← base of ALL RDBMS drivers (SQL Server, PostgreSQL …)
        └── InMemoryRDBSource            ← adds IInMemoryDB; shareable schema across connections
              └── SQLiteDataSource       ← also: ILocalDB, IDataSource   ✅ preferred local store
IDataSource, ILocalDB
  └── LiteDBDataSource                  ← document store; NOT RDBMS, separate API surface
```

**Practical rules derived from this hierarchy:**

| Scenario | Guidance |
|---|---|
| **`DataSourceImportErrorStore` default** | Default to `SQLiteDataSource` as the backing engine — cast to `ILocalDB` to call `CreateDB()`, then use standard `IDataSource` CRUD |
| **`DataSourceImportRunHistoryStore` default** | Same — `SQLiteDataSource` via `ILocalDB.CreateDB()`, standard CRUD |
| **Cast check before RDBMS-specific ops** | `if (ds is RDBSource rdb)` — use this to access `RDBSource`-specific helpers (transactions, bulk ops, DDL) |
| **`IInMemoryDB` operations** | `if (ds is InMemoryRDBSource mem)` — for in-memory schema sharing (profiling scratch space) |
| **LiteDB-backed stores** | `LiteDBDataSource` implements `ILocalDB` but **not** `RDBSource`; never cast to `IRDBSource` — use `ILocalDB` + document-oriented `IDataSource` methods only |
| **`DataSourceHelperFactory`** | Returns `RdbmsHelper` for anything that `is RDBSource`; returns `DefaultDataSourceHelper` for `LiteDBDataSource` |
| **Schema drift (Phase 8)** | `SchemaComparator` should only compute type-widening/narrowing for `RDBSource`-derived sources — document stores have fluid schemas, so apply `DriftPolicy.Ignore` by default |
| **Profiling (Phase 10)** | `DataProfiler` routes RDBMS sampling through `RdbmsHelper.SampleFieldStatisticsAsync`; for `LiteDBDataSource` falls back to in-memory scan |

**File locations:**
- `BeepDataSources/DataSourcesPlugins/RDBMSDataSource/PartialClasses/RDBSource/RDBSource.cs` — RDBMS base
- `BeepDataSources/DataSourcesPlugins/RDBMSDataSource/InMemoryRDBSource.cs` — in-memory RDBMS
- `BeepDataSources/DataSourcesPluginsCore/SqliteDatasourceCore/SQLiteDataSource.cs` — SQLite (preferred local store)
- `BeepDataSources/DataSourcesPluginsCore/LiteDBDataSourceCore/LiteDBDataSource.cs` — LiteDB document store
- `DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionDriverLinkingHelper.cs` — driver discovery (`LinkConnection2Drivers`, `FindDriverByIsLocal`)

---

### ConfigEditor — connection registration and migration history
*(file: `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs`)*

Any store that creates a backing datasource must register its connection through
`editor.ConfigEditor`, not directly with the datasource factory, so the connection is
persisted and survives restarts. **Always complete the `ConnectionDriverLinkingHelper`
driver-discovery step (see `ConnectionProperties` convention above) before calling
`AddDataConnection`** — without a resolved `DriverName` and `DatabaseType`, `GetDataSource`
will fail to load the correct plugin at runtime:

```csharp
editor.ConfigEditor.AddDataConnection(storeConn);
editor.ConfigEditor.SaveDataconnectionsValues();   // persist to disk immediately
```

`ConfigEditor.MigrationHistoryManager` is the **canonical home for migration history**.
Phase 8 (Schema Drift) must query and write to `MigrationHistoryManager` rather than
inventing a separate store. Use `config.SaveMappingValues(entityName, dsName, mapping)`
when persisting per-entity field mappings from Phase 5 (Mapping Enhancements).

### ErrorHandlingHelper — structured async error handling
*(file: `DataManagementEngineStandard/Helpers/ErrorHandlingHelper.cs`)*

Every new `async Task<IErrorsInfo>` method introduced across all commercial phases must
wrap its inner logic in `ErrorHandlingHelper.ExecuteWithErrorHandlingAsync` (or the sync
variant) rather than bare `try/catch`:

```csharp
// Pattern for all new DataImportManager partial methods:
return await ErrorHandlingHelper.ExecuteWithErrorHandlingAsync(async () =>
{
    // ... implementation ...
    return erInfo;
}, context: $"{nameof(RunQualityChecksAsync)}/{config.SourceEntityName}", editor: _editor);
```

Use `ErrorHandlingHelper.CreateErrorInfo(ex, context)` when constructing `IErrorsInfo`
objects to return from helper classes that do not hold an `IDMEEditor` reference.

### DataTypesHelpers — type mapping and field-level type resolution
*(folder: `DataManagementEngineStandard/Helpers/DataTypesHelpers/`)*

- **Phase 7 (Quality Rules):** `RangeRule` and type-aware `RegexRule` must use
  `DataTypeFieldMappingHelper` to resolve the .NET type of a field before comparing
  values — never `object.ToString()` comparisons.
- **Phase 8 (Schema Drift):** `SchemaComparator` must use `DataTypeMappingRepository` to
  classify whether a type change is a widening (safe) or narrowing (breaking) drift before
  deciding `DriftPolicy` action.

```csharp
// In SchemaComparator — classify a type change:
bool isWidening = DataTypeMappingRepository
    .IsCompatibleAssignment(fromType: baseline.DataType, toType: current.DataType);
```

### UniversalDataSourceHelpers — datasource-type-aware operations
*(folder: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/`)*

- `DataSourceHelperFactory.GetHelper(dataSource)` returns the right helper
  (`RdbmsHelper`, `MongoDBHelper`, `RestApiHelper`, etc.) for a given `IDataSource`.
- **Phase 9 (Error Store):** `DataSourceImportErrorStore.SaveAsync` must route through
  the helper rather than calling `IDataSource.InsertRecord` directly, so RDBMS error
  stores use parameterized INSERT while document-store error stores use the doc-API path.
- **Phase 10 (Profiling):** `DataProfiler.ProfileAsync` should delegate `MIN/MAX/COUNT
  DISTINCT` sampling to `GeneralDataSourceHelper.SampleFieldStatisticsAsync` when
  available, falling back to an in-memory scan for non-RDBMS sources.

```csharp
var helper = DataSourceHelperFactory.GetHelper(source);
var stats   = await helper.SampleFieldStatisticsAsync(entityName, fieldName, sampleSize, token);
```

---

## 2. Phase 1 — Shared Context Model in Core

**Goal:** Replace the string-keyed parameter dictionary with a typed, serializable model
that both the core and UI share.

### 2.1 Add `ImportContext` to `Interfaces/IDataImportInterfaces.cs`

New class alongside existing `DataImportConfiguration`:

```
ImportContext
  ├── ImportSelectionContext Selection         (source/dest names + CreateIfNotExists)
  ├── EntityDataMap?          Mapping
  ├── ImportExecutionOptions  Options
  └── static ImportContext From(DataImportConfiguration)   factory
```

`ImportSelectionContext` and `ImportExecutionOptions` are added to the core interfaces file
so the UI classes become thin wrappers or aliases — the core owns the contract.

```csharp
public class ImportSelectionContext
{
    public string SourceDataSourceName       { get; set; } = string.Empty;
    public string SourceEntityName           { get; set; } = string.Empty;
    public string DestinationDataSourceName  { get; set; } = string.Empty;
    public string DestinationEntityName      { get; set; } = string.Empty;
    public bool   CreateDestinationIfNotExists { get; set; } = true;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(SourceDataSourceName)      &&
        !string.IsNullOrWhiteSpace(SourceEntityName)          &&
        !string.IsNullOrWhiteSpace(DestinationDataSourceName) &&
        !string.IsNullOrWhiteSpace(DestinationEntityName);
}

public class ImportExecutionOptions
{
    public bool RunMigrationPreflight  { get; set; }
    public bool AddMissingColumns      { get; set; } = true;
    public bool CreateSyncProfileDraft { get; set; }
    public bool RunImportOnFinish      { get; set; } = true;
    public int  BatchSize              { get; set; } = 100;
}

public class ImportContext
{
    public ImportSelectionContext Selection { get; set; } = new();
    public EntityDataMap?         Mapping  { get; set; }
    public ImportExecutionOptions Options  { get; set; } = new();

    public static ImportContext From(DataImportConfiguration config) => new()
    {
        Selection = new ImportSelectionContext
        {
            SourceDataSourceName      = config.SourceDataSourceName,
            SourceEntityName          = config.SourceEntityName,
            DestinationDataSourceName = config.DestDataSourceName,
            DestinationEntityName     = config.DestEntityName,
            CreateDestinationIfNotExists = config.CreateDestinationIfNotExists
        },
        Mapping = config.Mapping,
        Options = new ImportExecutionOptions { BatchSize = config.BatchSize }
    };
}
```

### 2.2 Extend `DataImportConfiguration` with execution options

Add to `DataImportConfiguration` in `IDataImportInterfaces.cs`:

```csharp
public bool RunMigrationPreflight  { get; set; }
public bool AddMissingColumns      { get; set; } = true;
public bool CreateSyncProfileDraft { get; set; }
public BatchErrorStrategy OnBatchError { get; set; } = BatchErrorStrategy.Skip;
public int  MaxRetries             { get; set; } = 3;
```

Add `BatchErrorStrategy` enum (same file):

```csharp
public enum BatchErrorStrategy { Abort, Skip, Retry }
```

### 2.3 Add `ImportStatus` and `ImportState` to `IDataImportInterfaces.cs`

`ImportStatus` is referenced in the Orchestrator but not defined in the shared interface
file. Add:

```csharp
public enum ImportState { Idle, Running, Paused, Completed, Faulted, Cancelled }

public class ImportStatus
{
    public ImportState State            { get; set; } = ImportState.Idle;
    public int  RecordsProcessed        { get; set; }
    public int  TotalRecords            { get; set; }
    public double PercentComplete       { get; set; }
    public string LastMessage           { get; set; } = string.Empty;
    public DateTime? StartedAt          { get; set; }
    public DateTime? FinishedAt         { get; set; }
    public ImportPerformanceMetrics? Metrics { get; set; }
}
```

**Files changed:**
- `Editor/Importing/Interfaces/IDataImportInterfaces.cs`

---

## 3. Phase 2 — Enhance `IDataImportManager` Interface + State Machine

**Goal:** Promote pause/resume/cancel/status from concrete-class-only to the interface
contract so the Orchestrator can depend on the interface, not the concrete type.

### 3.1 Add lifecycle methods to `IDataImportManager`

```csharp
public interface IDataImportManager : IDisposable
{
    // --- existing ---
    IDataImportValidationHelper   ValidationHelper   { get; }
    IDataImportTransformationHelper TransformationHelper { get; }
    IDataImportBatchHelper        BatchHelper        { get; }
    IDataImportProgressHelper     ProgressHelper     { get; }

    // --- new lifecycle ---
    void PauseImport();
    void ResumeImport();
    void CancelImport();
    ImportStatus GetImportStatus();           // returns a snapshot copy

    // --- new context-based entry point ---
    Task<IErrorsInfo> RunImportAsync(
        ImportContext context,
        IProgress<IPassedArgs> progress,
        CancellationToken token);

    // --- new configuration factory ---
    DataImportConfiguration BuildConfigurationFromContext(ImportContext context);

    // --- new migration/sync (implemented in Phase 3) ---
    Task<IErrorsInfo>    RunMigrationPreflightAsync(DataImportConfiguration config, Action<string>? log = null);
    Task<DataSyncSchema> BuildSyncDraftAsync(DataImportConfiguration config);
}
```

### 3.2 Implement in `DataImportManager.cs`

- Add `private ImportStatus _status = new();` with `lock(_lockObject)` guards.
- `GetImportStatus()` returns `new ImportStatus { ... }` — a **snapshot**, not the live object.
- **State transitions:**

```
Idle → Running     (RunImportAsync called)
Running → Paused   (PauseImport called)
Paused → Running   (ResumeImport called)
Running → Completed | Faulted | Cancelled  (import ends)
```

- `PauseImport()`  → `_pauseEvent.Reset()` + set `_status.State = ImportState.Paused`.
- `ResumeImport()` → `_pauseEvent.Set()`  + set `_status.State = ImportState.Running`.
- `CancelImport()` → `_internalCancellationTokenSource.Cancel()` + set `_status.State = ImportState.Cancelled`.
- `RunImportAsync(ImportContext, ...)` → calls `BuildConfigurationFromContext` then delegates
  to existing `RunImportAsync(DataImportConfiguration, ...)`.
- `BuildConfigurationFromContext(ImportContext)` → factory that maps `Selection` + `Mapping` +
  `Options` → `DataImportConfiguration`; calls `DefaultsManager.GetDefaults` for the
  destination data source.

### 3.3 Add retry loop in `DataImportBatchHelper.cs`

Add `BatchErrorStrategy` awareness inside `ProcessBatchAsync`:

```csharp
// pseudo
int attempt = 0;
while (attempt <= config.MaxRetries)
{
    result = await TryProcessBatchAsync(batch, config, progress, token);
    if (result.Flag == Errors.Ok || config.OnBatchError != BatchErrorStrategy.Retry)
        break;
    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));  // exponential back-off
    attempt++;
}
if (result.Flag != Errors.Ok && config.OnBatchError == BatchErrorStrategy.Abort)
    throw new ImportBatchException(result.Message);
```

**Files changed:**
- `Editor/Importing/Interfaces/IDataImportInterfaces.cs`
- `Editor/Importing/DataImportManager.cs`
- `Editor/Importing/Helpers/DataImportBatchHelper.cs`

---

## 4. Phase 3 — Move Preflight & Sync-Draft to Core

**Goal:** `RunMigrationPreflight` and `BuildAndSaveSyncDraft` are pure data-layer operations
that have leaked into the Winform UI project. Move them to core so any client (shell, web,
Winform) can call them.

### 4.1 Create `DataImportManager.Migration.cs` (new partial)

```
DataImportManager.Migration.cs
  ├── RunMigrationPreflightAsync(config, log)  → Task<IErrorsInfo>
  │       Uses MigrationManager.AnalyzeMigration / ApplyMigration
  │       Respects config.AddMissingColumns flag
  └── BuildSyncDraftAsync(config)              → Task<DataSyncSchema>
          Calls existing BeepSync / DataSyncSchemaBuilder helpers
```

Both methods are wired into the `RunImportAsync(DataImportConfiguration, ...)` pipeline:

```csharp
// Inside RunImportAsync, before fetching source data:
if (config.RunMigrationPreflight)
{
    var preflightResult = await RunMigrationPreflightAsync(config, log);
    if (preflightResult.Flag != Errors.Ok) return preflightResult;
}

// After successful import:
DataSyncSchema? syncDraft = null;
if (config.CreateSyncProfileDraft)
    syncDraft = await BuildSyncDraftAsync(config);
```

**Files changed / created:**
- `Editor/Importing/DataImportManager.Migration.cs` ← **new**
- `Editor/Importing/Interfaces/IDataImportInterfaces.cs`

---

## 5. Phase 4 — Slim Down the UI Layer

**Goal:** Remove all duplicated logic; thin down `ImportExportOrchestrator` to a pure UI
coordinator.

### 5.1 `ImportExportOrchestrator` changes

```csharp
// BEFORE
private DataImportManager? _importManager;

// AFTER — depend on interface, not concrete
private IDataImportManager? _importManager;
```

Remove `RunMigrationPreflight` and `BuildAndSaveSyncDraft` methods entirely. Replace inline
calls with:

```csharp
var preflightResult = await _importManager.RunMigrationPreflightAsync(config, log);
var syncDraft       = await _importManager.BuildSyncDraftAsync(config);
```

Refactor `ExecuteAsync` to build an `ImportContext` and delegate:

```csharp
public async Task<ImportExecutionResult> ExecuteAsync(
    ImportExecutionRequest request,
    IProgress<IPassedArgs>? progress,
    Action<string>? log)
{
    var context = new ImportContext
    {
        Selection = Map(request.Selection),   // UI→Core mapping helper
        Mapping   = request.Mapping,
        Options   = Map(request.Options)
    };

    var importResult = await _importManager.RunImportAsync(
        context, progress, _runCancellation?.Token ?? CancellationToken.None);

    return new ImportExecutionResult
    {
        ImportResult = importResult,
        SyncDraft    = request.Options.CreateSyncProfileDraft
                       ? await _importManager.BuildSyncDraftAsync(
                             _importManager.BuildConfigurationFromContext(context))
                       : null
    };
}
```

### 5.2 `ImportExportContextStore` — bridge to `ImportContext`

Add convenience methods so wizard steps no longer need `BuildParameters` / `ParseSelection`:

```csharp
public static ImportContext ToImportContext(
    ImportSelectionContext selection,
    EntityDataMap? mapping,
    ImportExecutionOptions options);

public static (ImportSelectionContext selection,
               EntityDataMap? mapping,
               ImportExecutionOptions options) FromImportContext(ImportContext ctx);
```

The existing string-key `BuildParameters` / `ParseSelection` / `ParseMapping` are kept for
backward compatibility with wizard step parameter-passing but are no longer used by the
Orchestrator.

**Files changed:**
- `ImportExport/ImportExportOrchestrator.cs`
- `ImportExport/ImportExportContextStore.cs`

---

## 6. Phase 5 — Mapping Enhancements (`MappingManager`)

**Goal:** Give the import wizard a richer mapping API without forcing it to do entity
introspection itself.

### 6.1 `AutoMapByConvention`

```csharp
public static EntityDataMap AutoMapByConvention(
    IDMEEditor editor,
    string sourceDS, string sourceEntity,
    string destDS,   string destEntity,
    NameMatchMode mode = NameMatchMode.CaseInsensitive)
```

| Mode | Behaviour |
|---|---|
| `CaseInsensitive` | Match field names ignoring case |
| `Fuzzy` | Normalize underscores and camelCase before matching |
| `TypeCompatible` | Only map where source type is assignable to dest type |

Returns a ready-to-use `EntityDataMap`. Unmapped fields are collected in new
`EntityDataMap.UnmappedSourceFields` and `EntityDataMap.UnmappedDestinationFields` lists.

### 6.2 `ValidateMappingAsync`

```csharp
public static Task<MappingValidationResult> ValidateMappingAsync(
    IDMEEditor editor,
    EntityDataMap mapping)
```

```csharp
public class MappingValidationResult
{
    public bool         IsValid         { get; set; }
    public List<string> Errors          { get; set; } = new();
    public List<string> Warnings        { get; set; } = new(); // type-widening, nullable←non-nullable
    public int          MappedFieldCount { get; set; }
}
```

### 6.3 `DiffMapping`

```csharp
public static MappingDiff DiffMapping(EntityDataMap original, EntityDataMap updated)
```

```csharp
public class MappingDiff
{
    public List<Mapping_rep_fields> Added   { get; set; } = new();
    public List<Mapping_rep_fields> Removed { get; set; } = new();
    public List<MappingFieldChange> Changed { get; set; } = new();
}

public class MappingFieldChange
{
    public Mapping_rep_fields Before { get; set; }
    public Mapping_rep_fields After  { get; set; }
}
```

Used by the wizard summary step to show a "what changed" review before committing.

### 6.4 New model file

Create `Editor/Mapping/Models/MappingModels.cs` with:
- `MappingValidationResult`
- `MappingDiff`
- `MappingFieldChange`
- `NameMatchMode` enum

**Files changed / created:**
- `Editor/Mapping/MappingManager.cs`
- `Editor/Mapping/Helpers/MappingDefaultsHelper.cs`
- `Editor/Mapping/Models/MappingModels.cs` ← **new**

---

> **Note:** The full Enhancement Checklist (Phases 1–11, 30 items) is in **Section 15**,
> and the complete File Map is in **Section 16** below.

---

## 9. Phase 6 — Incremental Sync & Change Tracking
*(Inspired by **Fivetran**, **Airbyte**, **Stitch**)*

**Goal:** Stop re-importing the entire source on every run. Track what changed and only
move the delta.

### 9.1 Add `SyncMode` to `ImportExecutionOptions`

```csharp
public enum SyncMode { FullRefresh, Incremental, Upsert }

// In ImportExecutionOptions:
public SyncMode  SyncMode             { get; set; } = SyncMode.FullRefresh;
public string    WatermarkColumn      { get; set; } = string.Empty;  // e.g. "UpdatedAt" or "RowId"
public object?   LastWatermarkValue   { get; set; }                  // persisted between runs
public UpsertMode UpsertKeyColumns    { get; set; }                  // column(s) for MERGE key
```

### 9.2 `WatermarkStore` — persist high-watermark between runs

```
Editor/Importing/Sync/WatermarkStore.cs   ← new
  ├── SaveWatermark(string contextKey, object value)
  ├── LoadWatermark(string contextKey) → object?
  └── Built-in impls: FileWatermarkStore (JSON), InMemoryWatermarkStore
```

`contextKey = "{sourceDS}/{sourceEntity}/{destDS}/{destEntity}"` — unique per pipeline.

### 9.3 Incremental filter injection

Inside `InitializeDataSources`, when `SyncMode == Incremental`:
- Load last watermark from `WatermarkStore`.
- Inject an `AppFilter` (`WatermarkColumn > lastValue`) into `config.SourceFilters`.
- After a successful import run, call `WatermarkStore.SaveWatermark(key, maxObservedValue)`.

### 9.4 Upsert mode

When `SyncMode == Upsert`:
- Wrap the destination write via `IDataSource.InsertOrUpdateEntity` (add to the interface
  contract if not present).
- Conflict key = `UpsertKeyColumns`; all other fields are updated on match.

> **Convention (EnvironmentService):** `FileWatermarkStore` must obtain its root path via
> `EnvironmentService.CreateAppfolder("Importing")` and store files under a `Watermarks/`
> sub-folder. Do not accept a hard-coded path from the caller.
>
> **Convention (ConnectionProperties):** The `contextKey` used as the watermark store key
> must match `"{ConnectionProperties.ConnectionName}/{entityName}"` so it aligns with
> connection registry entries.

**New files:**
- `Editor/Importing/Sync/WatermarkStore.cs`
- `Editor/Importing/Sync/IWatermarkStore.cs`

---

## 10. Phase 7 — Data Quality Contracts
*(Inspired by **Great Expectations**, **dbt tests**, **Talend Data Quality**)*

**Goal:** Validate data against declared rules at import time — block, warn, or quarantine
bad records instead of silently passing them downstream.

### 10.1 `IDataQualityRule` interface

```csharp
public interface IDataQualityRule
{
    string RuleName  { get; }
    string FieldName { get; }
    DataQualityAction OnFailure { get; }   // Block | Quarantine | Warn
    bool Evaluate(object? fieldValue, object record) → bool;
    string FailureMessage(object? fieldValue) → string;
}

public enum DataQualityAction { Block, Quarantine, Warn }
```

### 10.2 Built-in rules (new file `Editor/Importing/Quality/BuiltInRules.cs`)

| Rule class | dbt / GE equivalent |
|---|---|
| `NotNullRule` | `not_null` |
| `UniqueRule` | `unique` |
| `RangeRule(min, max)` | `accepted_range` |
| `RegexRule(pattern)` | `matches_like` |
| `AcceptedValuesRule(set)` | `accepted_values` |
| `ReferentialIntegrityRule(lookupDS, lookupEntity, keyField)` | relationships |

### 10.3 `DataQualityProfile` on `DataImportConfiguration`

```csharp
public List<IDataQualityRule> QualityRules { get; set; } = new();
```

Rules are evaluated per-record inside `DataImportBatchHelper.ProcessBatchAsync` before
writing to the destination. Failures route to the **Error Output** (Phase 8).

### 10.4 Quality summary in `ImportStatus`

```csharp
// Add to ImportStatus:
public int  RecordsBlocked     { get; set; }
public int  RecordsQuarantined { get; set; }
public int  RecordsWarned      { get; set; }
```

**New files:**
- `Editor/Importing/Quality/IDataQualityRule.cs`
- `Editor/Importing/Quality/BuiltInRules.cs`
- `Editor/Importing/Quality/DataQualityEvaluator.cs`

---

## 11. Phase 8 — Schema Drift Detection & Auto-Healing
*(Inspired by **Azure Data Factory schema drift**, **Confluent Schema Registry**, **Informatica**)*

**Goal:** Detect when the source schema changes between runs and handle it gracefully
instead of crashing.

### 11.1 `SchemaSnapshot` and `SchemaComparator`

```csharp
// New file: Editor/Importing/Schema/SchemaSnapshot.cs
public class SchemaSnapshot
{
    public string       DataSourceName { get; set; }
    public string       EntityName     { get; set; }
    public DateTime     CapturedAt     { get; set; }
    public List<EntityField> Fields    { get; set; } = new();
}

// New file: Editor/Importing/Schema/SchemaComparator.cs
public static class SchemaComparator
{
    public static SchemaDriftReport Compare(SchemaSnapshot baseline, EntityStructure current);
}

public class SchemaDriftReport
{
    public bool HasDrift            { get; set; }
    public List<string> AddedFields   { get; set; } = new();   // new cols in source
    public List<string> RemovedFields { get; set; } = new();   // gone from source
    public List<SchemaFieldChange> TypeChanges { get; set; } = new();
}
```

### 11.2 `SchemaDriftPolicy` on `DataImportConfiguration`

```csharp
public enum SchemaDriftPolicy { Ignore, AbortOnDrift, AutoAddColumns, AutoDropColumns }

// In DataImportConfiguration:
public SchemaDriftPolicy DriftPolicy { get; set; } = SchemaDriftPolicy.AutoAddColumns;
```

### 11.3 Integration in `RunImportAsync`

Before `InitializeDataSources`:
1. Load the baseline snapshot for `SourceEntityName` from a `SchemaSnapshotStore`.
2. Call `SchemaComparator.Compare(baseline, liveStructure)`.
3. Apply `DriftPolicy`: `AbortOnDrift` → return error; `AutoAddColumns` → emit ALTER TABLE
   script via `MigrationManager`; `Ignore` → continue.
4. Save a new snapshot after successful run.

**New files:**
- `Editor/Importing/Schema/SchemaSnapshot.cs`
- `Editor/Importing/Schema/SchemaComparator.cs`
- `Editor/Importing/Schema/SchemaSnapshotStore.cs`  (JSON or DB backed)

---

## 12. Phase 9 — Dead-Letter Queue / Error Output Routing
*(Inspired by **SSIS Error Output**, **Azure Service Bus DLQ**, **Kafka dead-letter topics**)*

**Goal:** Failed and quarantined records are never silently swallowed. They are written to a
configurable error store where they can be inspected, corrected, and replayed.

### 12.1 `IImportErrorStore` interface

```csharp
public interface IImportErrorStore
{
    Task SaveAsync(ImportErrorRecord record, CancellationToken token = default);
    Task<IEnumerable<ImportErrorRecord>> LoadAsync(string contextKey, CancellationToken token = default);
    Task ClearAsync(string contextKey, CancellationToken token = default);
}

public class ImportErrorRecord
{
    public Guid     Id           { get; set; } = Guid.NewGuid();
    public string   ContextKey   { get; set; } = string.Empty;
    public int      BatchNumber  { get; set; }
    public int      RecordIndex  { get; set; }
    public object?  RawRecord    { get; set; }
    public string   Reason       { get; set; } = string.Empty;
    public string   RuleName     { get; set; } = string.Empty;
    public DateTime OccurredAt   { get; set; } = DateTime.UtcNow;
    public bool     Replayed     { get; set; }
}
```

### 12.2 Built-in implementations

- `JsonFileImportErrorStore` — writes `JSONL` file per context key (default, zero-config).
- `DataSourceImportErrorStore` — writes to a `_import_errors` table in any `IDataSource`.

### 12.3 Replay API on `IDataImportManager`

```csharp
Task<IErrorsInfo> ReplayFailedRecordsAsync(
    string contextKey,
    IProgress<IPassedArgs> progress,
    CancellationToken token);
```

Loads records from the error store, re-runs quality rules and transformation pipeline, and
attempts to write them again. Successfully replayed records are flagged `Replayed = true`.

### 12.4 Wire into `DataImportConfiguration`

```csharp
public IImportErrorStore? ErrorStore { get; set; }  // null = errors are only logged
```

> **Convention (EnvironmentService):** `JsonFileImportErrorStore` must use
> `EnvironmentService.CreateAppfolder("Importing")` + `Errors/` sub-folder as its default
> storage root.
>
> **Convention (ILocalDB + ConnectionProperties):** `DataSourceImportErrorStore` must cast
> the backing datasource to `ILocalDB`, call `CreateDB()` before the first write, and
> configure `ConnectionProperties` with `IsLocal=true, IsFile=true, IsDatabase=true`.
> Always call `Closeconnection()` before any `CopyDB` or `DeleteDB` operation.

**New files:**
- `Editor/Importing/ErrorStore/IImportErrorStore.cs`
- `Editor/Importing/ErrorStore/ImportErrorRecord.cs`
- `Editor/Importing/ErrorStore/JsonFileImportErrorStore.cs`
- `Editor/Importing/ErrorStore/DataSourceImportErrorStore.cs`

---

## 13. Phase 10 — Data Profiling & Import Run History
*(Inspired by **Informatica Data Quality**, **AWS Glue Data Catalog**, **Azure Purview lineage**)*

**Goal:** Surface data statistics before mapping and persist a full audit trail of every
import run.

### 13.1 `DataProfiler` — pre-import source profiling

```csharp
// New file: Editor/Importing/Profiling/DataProfiler.cs
public static class DataProfiler
{
    public static Task<DataProfile> ProfileAsync(
        IDataSource source,
        string entityName,
        int sampleSize = 1000,
        CancellationToken token = default);
}

public class DataProfile
{
    public string          EntityName  { get; set; }
    public int             TotalRows   { get; set; }
    public List<FieldProfile> Fields   { get; set; } = new();
}

public class FieldProfile
{
    public string   FieldName      { get; set; }
    public string   InferredType   { get; set; }
    public double   NullPercent    { get; set; }
    public int      DistinctCount  { get; set; }
    public object?  MinValue       { get; set; }
    public object?  MaxValue       { get; set; }
    public object?  MostFrequent   { get; set; }
    public double   AvgLength      { get; set; }   // for string fields
}
```

The profile result is surfaced in the wizard's source-selection step to help the user make
informed mapping decisions (e.g. "this field is 40% null — map to a nullable dest column").

### 13.2 `ImportRunRecord` — full audit log

```csharp
// New file: Editor/Importing/History/ImportRunRecord.cs
public class ImportRunRecord
{
    public Guid     RunId                 { get; set; } = Guid.NewGuid();
    public string   ContextKey            { get; set; }
    public DateTime StartedAt             { get; set; }
    public DateTime? FinishedAt           { get; set; }
    public ImportState FinalState         { get; set; }
    public int      RecordsProcessed      { get; set; }
    public int      RecordsBlocked        { get; set; }
    public int      RecordsQuarantined    { get; set; }
    public string   SourceDataSourceName  { get; set; }
    public string   SourceEntityName      { get; set; }
    public string   DestDataSourceName    { get; set; }
    public string   DestEntityName        { get; set; }
    public SyncMode SyncMode              { get; set; }
    public string?  FailureMessage        { get; set; }
    public List<ImportPerformanceMetrics> BatchMetrics { get; set; } = new();
}
```

### 13.3 `IImportRunHistoryStore` interface

```csharp
public interface IImportRunHistoryStore
{
    Task SaveRunAsync(ImportRunRecord record, CancellationToken token = default);
    Task<IEnumerable<ImportRunRecord>> GetRunsAsync(string contextKey, int maxRecords = 50);
    Task<ImportRunRecord?> GetLastSuccessfulRunAsync(string contextKey);
}
```

Built-in implementations: `JsonFileImportRunHistoryStore`, `DataSourceImportRunHistoryStore`.

The `WatermarkStore` (Phase 6) can query `GetLastSuccessfulRunAsync` to derive the
watermark if no explicit value was saved, using `FinishedAt` as the fallback.

### 13.4 Wire into `DataImportConfiguration`

```csharp
public IImportRunHistoryStore? RunHistoryStore { get; set; }  // null = history not persisted
```

`DataImportManager.RunImportAsync` creates an `ImportRunRecord` at start, updates it
throughout, and saves it on completion regardless of success or failure.

> **Convention (EnvironmentService):** `JsonFileImportRunHistoryStore` must use
> `EnvironmentService.CreateAppfolder("Importing")` + `History/` sub-folder.
>
> **Convention (ILocalDB + ConnectionProperties):** `DataSourceImportRunHistoryStore` must
> use `ILocalDB.CreateDB()` to ensure the SQLite file exists, and register the connection
> via a fully flagged `ConnectionProperties` (`IsLocal`, `IsFile`, `IsDatabase` = `true`)
> using a unique `ConnectionName` derived from the context key.

**New files:**
- `Editor/Importing/Profiling/DataProfiler.cs`
- `Editor/Importing/Profiling/FieldProfile.cs`
- `Editor/Importing/History/ImportRunRecord.cs`
- `Editor/Importing/History/IImportRunHistoryStore.cs`
- `Editor/Importing/History/JsonFileImportRunHistoryStore.cs`
- `Editor/Importing/History/DataSourceImportRunHistoryStore.cs`

---

## 14. Phase 11 — Raw-Stage + Typed Normalization (Two-Layer Write)
*(Inspired by **Airbyte normalization**, **dbt raw layer**, **Fivetran raw schema**)*

**Goal:** Write source records verbatim to a staging entity first, then normalize/transform
to the final destination. Enables full replayability without re-fetching from source.

### 14.1 `StagingOptions` on `DataImportConfiguration`

```csharp
public class StagingOptions
{
    public bool   Enabled            { get; set; }
    public string StagingEntitySuffix { get; set; } = "_raw";
    public bool   DropStagingAfterNormalize { get; set; } = false;
    public bool   SkipNormalization  { get; set; }   // ingest-only mode
}

// In DataImportConfiguration:
public StagingOptions? Staging { get; set; }
```

### 14.2 Two-phase write in `RunImportAsync`

When `Staging.Enabled`:
1. **Raw write** — insert every source record verbatim into `{destEntity}_raw`, adding
   `_imported_at` (UTC timestamp) and `_run_id` (from `ImportRunRecord.RunId`) columns.
2. **Normalize** (unless `SkipNormalization`) — apply transformation pipeline and mapping,
   then upsert from staging into the final entity using `Staging` as the source.
3. Optionally drop the staging entity post-normalization.

This pattern means a failed normalization can be re-run from staging without contacting
the original source system again.

**New file:**
- `Editor/Importing/Staging/StagingOptions.cs`

---

## 15. Updated Enhancement Checklist — All Phases

| # | Enhancement | Phase | Inspired by | Priority |
|---|---|---|---|---|
| 1 | Typed `ImportContext` replaces string-key dict | 1 | Internal best practice | Critical |
| 2 | `ImportStatus` + `ImportState` machine in interface | 1–2 | Internal best practice | Critical |
| 3 | `BatchErrorStrategy` + exponential back-off | 2 | SSIS / ADF | High |
| 4 | `PauseImport/ResumeImport/CancelImport` on interface | 2 | ADF pipeline control | High |
| 5 | `RunMigrationPreflightAsync` moves to core | 3 | Internal best practice | High |
| 6 | `BuildSyncDraftAsync` moves to core | 3 | Internal best practice | High |
| 7 | Orchestrator on `IDataImportManager` interface | 4 | Internal best practice | High |
| 8 | `ImportContext` bridge methods on ContextStore | 4 | Internal best practice | Medium |
| 9 | `AutoMapByConvention` + `NameMatchMode` | 5 | Talend / Informatica | High |
| 10 | `ValidateMappingAsync` + `MappingValidationResult` | 5 | Talend / Informatica | High |
| 11 | `DiffMapping` for wizard summary | 5 | Liquibase / Flyway | Medium |
| 12 | Incremental sync + watermark (`SyncMode.Incremental`) | 6 | Fivetran / Airbyte | High |
| 13 | Upsert / MERGE mode (`SyncMode.Upsert`) | 6 | Airbyte / Stitch | High |
| 14 | `WatermarkStore` (file + in-memory) | 6 | Fivetran | Medium |
| 15 | `IDataQualityRule` + built-in rules (not-null, unique, range, regex…) | 7 | Great Expectations / dbt | High |
| 16 | `DataQualityEvaluator` wired into batch pipeline | 7 | Great Expectations | High |
| 17 | Quality counters (`Blocked`, `Quarantined`, `Warned`) on `ImportStatus` | 7 | Informatica DQ | Medium |
| 18 | `SchemaDriftReport` + `SchemaDriftPolicy` | 8 | ADF schema drift / Confluent | High |
| 19 | `SchemaSnapshotStore` — baseline versioning | 8 | Liquibase / Schema Registry | Medium |
| 20 | Auto-generate ALTER TABLE scripts from drift report | 8 | Flyway / Liquibase | Medium |
| 21 | `IImportErrorStore` dead-letter queue | 9 | SSIS error output / Azure DLQ | High |
| 22 | Replay API (`ReplayFailedRecordsAsync`) | 9 | Kafka DLQ replay / SSIS | High |
| 23 | `JsonFileImportErrorStore` (**required fallback**) + `DataSourceImportErrorStore` (when local driver present) | 9 | Azure Service Bus DLQ | High |
| 24 | `DataProfiler` — source field stats before mapping | 10 | Informatica / AWS Glue | High |
| 25 | `ImportRunRecord` — full audit trail per run | 10 | Azure Purview / ADF monitor | High |
| 26 | `IImportRunHistoryStore` — `JsonFileImportRunHistoryStore` (**required fallback**) + `DataSourceImportRunHistoryStore` | 10 | ADF run history / dbt | High |
| 27 | Watermark derived from last successful run record | 10 | Fivetran / Airbyte | Medium |
| 27b | `LocalStoreFactory` — centralised store selection (detects local driver; falls back to JSON file stores) | 6–10 | Internal pattern | High |
| 28 | Raw-staging write + typed normalization | 11 | Airbyte / dbt raw layer | Medium |
| 29 | `_imported_at` + `_run_id` system columns on raw stage | 11 | Airbyte / Fivetran | Low |
| 30 | Replay normalization from staging without re-fetch | 11 | Airbyte / dbt | Medium |

---

## 16. Updated File Map — Commercial Phases

### New files — `BeepDM/DataManagementEngineStandard`

| File | Phase | Purpose |
|---|---|---|
| `Editor/Importing/Sync/IWatermarkStore.cs` | 6 | Interface for watermark persistence |
| `Editor/Importing/Sync/WatermarkStore.cs` | 6 | File + in-memory implementations |
| `Editor/Importing/Quality/IDataQualityRule.cs` | 7 | Rule interface + `DataQualityAction` enum |
| `Editor/Importing/Quality/BuiltInRules.cs` | 7 | `NotNullRule`, `UniqueRule`, `RangeRule`, `RegexRule`, `AcceptedValuesRule`, `ReferentialIntegrityRule` |
| `Editor/Importing/Quality/DataQualityEvaluator.cs` | 7 | Evaluates rules per record; routes to error store |
| `Editor/Importing/Schema/SchemaSnapshot.cs` | 8 | Point-in-time schema snapshot |
| `Editor/Importing/Schema/SchemaComparator.cs` | 8 | `Compare` → `SchemaDriftReport` |
| `Editor/Importing/Schema/SchemaSnapshotStore.cs` | 8 | JSON + DataSource backed |
| `Editor/Importing/ErrorStore/IImportErrorStore.cs` | 9 | Dead-letter interface |
| `Editor/Importing/ErrorStore/ImportErrorRecord.cs` | 9 | Failed record model |
| `Editor/Importing/ErrorStore/JsonFileImportErrorStore.cs` | 9 | JSONL file implementation (**required fallback**) |
| `Editor/Importing/ErrorStore/DataSourceImportErrorStore.cs` | 9 | Writes to `_import_errors` table (requires local driver) |
| `Editor/Importing/Profiling/DataProfiler.cs` | 10 | Async source profiling |
| `Editor/Importing/Profiling/FieldProfile.cs` | 10 | Per-field statistics |
| `Editor/Importing/History/ImportRunRecord.cs` | 10 | Full run audit record |
| `Editor/Importing/History/IImportRunHistoryStore.cs` | 10 | Run history interface |
| `Editor/Importing/History/JsonFileImportRunHistoryStore.cs` | 10 | JSONL file implementation (**required fallback**) |
| `Editor/Importing/History/DataSourceImportRunHistoryStore.cs` | 10 | DataSource-backed history (requires local driver) |
| `Editor/Importing/Factories/LocalStoreFactory.cs` | 6–10 | Detects local driver presence; returns JSON file or DataSource-backed store for all store types |
| `Editor/Importing/Staging/StagingOptions.cs` | 11 | Two-layer write configuration |
| `Editor/Mapping/Models/MappingModels.cs` | 5 | `MappingValidationResult`, `MappingDiff`, `MappingFieldChange`, `NameMatchMode` |

---

## 17. Verification Criteria

| Scenario | Expected Result |
|---|---|
| `MockDataImportManager : IDataImportManager` compiles and substitutes for concrete | Orchestrator has no hard dependency on `DataImportManager` |
| State machine: `RunImportAsync` → `PauseImport` → `GetImportStatus().State` | `Paused` |
| State machine resumed: `ResumeImport` → processing continues → `Completed` | `Completed` |
| State machine cancelled: `CancelImport` | `Cancelled`; result `Flag == Ok` with cancel message |
| `AutoMapByConvention` on 10-field source ↔ dest with mixed casing | ≥ 8 fields matched |
| `ValidateMappingAsync` with a required dest field unmapped | `IsValid = false`; error in `Errors` list |
| `BatchErrorStrategy.Retry` — batch fails 2× then succeeds | Processed; `RecordsProcessed == batchCount` |
| `BatchErrorStrategy.Abort` — batch fails | Import halts; `State == Faulted` |
| `ToImportContext` / `FromImportContext` round-trip | All properties identical; no data loss |
| `BuildConfigurationFromContext` with a valid `ImportContext` | Returns populated `DataImportConfiguration` with defaults loaded |
