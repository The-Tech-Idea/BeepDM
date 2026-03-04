````markdown
# Data Import Quick Reference

## Minimal Import

```csharp
using var mgr = new DataImportManager(editor);
var config = mgr.CreateImportConfiguration("SrcEntity","SrcDB","DstEntity","DstDB");
config.BatchSize = 200;
await mgr.RunImportAsync(config, progress, CancellationToken.None);
```

## Config Cheat-Sheet

```
SelectedFields            List<string>
SourceFilters             List<AppFilter>
CustomTransformation      Func<object,object>
ApplyDefaults             bool
BatchSize / MaxRetries
CreateDestinationIfNotExists / AddMissingColumns
DriftPolicy               SchemaDriftPolicy  (AutoAddColumns | Strict | Ignore)
SyncMode                  SyncMode           (FullRefresh | Incremental | Upsert)
WatermarkColumn           string
UpsertKeyColumns          List<string>
QualityRules              List<IDataQualityRule>
ErrorStore                IImportErrorStore
RunHistoryStore           IImportRunHistoryStore
Staging                   StagingOptions
```

## Quality Rules

```csharp
config.QualityRules.Add(new NotNullRule("Email",   DataQualityAction.Block));
config.QualityRules.Add(new UniqueRule("OrderId",  DataQualityAction.Quarantine));
config.QualityRules.Add(new RangeRule("Age",0,150, DataQualityAction.Warn));
config.QualityRules.Add(new RegexRule("Phone", @"^\d+$", DataQualityAction.Block));
```

## Incremental Sync

```csharp
config.SyncMode = SyncMode.Incremental;
config.WatermarkColumn = "UpdatedAt";
config.LastWatermarkValue = await store.LoadWatermarkAsync(key);
```

## Error Store & Replay

```csharp
config.ErrorStore = new JsonFileImportErrorStore();
// Replay quarantined records later:
await mgr.ReplayFailedRecordsAsync(contextKey, progress, token);
```

## Run History

```csharp
config.RunHistoryStore = new JsonFileImportRunHistoryStore();
var runs = await config.RunHistoryStore.LoadAsync(key, token);
```

## Staging

```csharp
config.Staging = new StagingOptions { Enabled = true, StagingEntitySuffix = "_raw" };
```

## Validation

```csharp
var r = mgr.ValidationHelper.ValidateImportConfiguration(config);
if (r.Flag != Errors.Ok) return;
```

## Profiling

```csharp
var profile = await DataProfiler.ProfileAsync(editor, "DB", "Entity", sampleSize: 500);
```

## Lifecycle

```csharp
mgr.PauseImport();   mgr.ResumeImport();   mgr.CancelImport();
var status = mgr.GetImportStatus();
```

## Key Locations

```
DataManagementEngineStandard/Editor/Importing/DataImportManager.*        — orchestrator (4 partials)
Interfaces/IDataImportInterfaces.cs             — all interfaces + DataImportConfiguration
Quality/BuiltInRules.cs                         — NotNull/Unique/Range/Regex
ErrorStore/ | History/ | Staging/ | Sync/       — phase 9-11 stores
Profiling/DataProfiler.cs
```
````
