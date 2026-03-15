# Phase 1 — Plugin Architecture & Core Interfaces

**Version:** 1.0  
**Date:** 2026-03-13  
**Status:** Design  
**Depends on:** Nothing (foundation layer)

---

## 1. Objective

Replace the current tightly-coupled ETL classes with a fully plugin-driven architecture. Every component in the pipeline — sources, sinks, transformers, validators, loaders, notifiers — is a discoverable, hot-swappable plugin that the runtime finds via the existing `AssemblyHandler` / `[AddinAttribute]` mechanism already in BeepDM.

The result is a system where **adding a new connector or transformer requires zero changes to the ETL engine** — drop a DLL, restart, it appears.

---

## 2. Namespace Plan

```
TheTechIdea.Beep.Pipelines          ← top-level namespace for the new framework
├── Interfaces/                     ← all public contracts (no implementation)
│   ├── IPipelineSource.cs
│   ├── IPipelineSink.cs
│   ├── IPipelineTransformer.cs
│   ├── IPipelineValidator.cs
│   ├── IPipelineFilter.cs
│   ├── IPipelineAggregator.cs
│   ├── IPipelineJoin.cs
│   ├── IPipelineLookup.cs
│   ├── IPipelineNotifier.cs
│   ├── IPipelinePlugin.cs          ← base marker
│   └── IPipelineScheduler.cs
├── Models/                         ← data model classes
│   ├── PipelineDefinition.cs       ← replaces ETLScriptHDR (superset)
│   ├── PipelineStepDef.cs          ← replaces ETLScriptDet (superset)
│   ├── PipelineRecord.cs           ← typed row flowing through pipeline
│   ├── PipelineSchema.cs           ← field schema for PipelineRecord
│   ├── PipelineRunContext.cs        ← runtime state
│   ├── PipelineRunResult.cs        ← run summary
│   ├── PipelineRunLog.cs           ← row-level audit entry
│   ├── PipelineCheckpoint.cs       ← resumable state snapshot
│   ├── DataLineageRecord.cs        ← column-level lineage
│   └── PipelineParameter.cs        ← named runtime parameters
├── Registry/
│   └── PipelinePluginRegistry.cs   ← discovers & caches all plugins
└── Attributes/
    └── PipelinePluginAttribute.cs  ← decoration for plugin classes
```

Project location: `DataManagementModelsStandard` for interfaces/models, `DataManagementEngineStandard` for engine/registry.

---

## 3. Clean Code & Architecture Standards

> These standards apply to **all phases** of the pipeline framework. Every class, file, and folder must comply.

### 3.1 Folder = Namespace = Domain

Each sub-namespace maps directly to a folder. Never put a class in a folder that doesn't match its namespace.

```
DataManagementEngineStandard/Editor/ETL/
│
├── Interfaces/                     ← TheTechIdea.Beep.Pipelines.Interfaces
├── Models/                         ← TheTechIdea.Beep.Pipelines.Models
├── Registry/                       ← TheTechIdea.Beep.Pipelines.Registry
├── Attributes/                     ← TheTechIdea.Beep.Pipelines.Attributes
├── Engine/                         ← TheTechIdea.Beep.Pipelines.Engine
│   ├── Core/                       ← .Engine.Core          (PipelineEngine, PipelineManager)
│   ├── Checkpoint/                 ← .Engine.Checkpoint    (CheckpointManager)
│   └── Retry/                      ← .Engine.Retry         (RetryPolicy)
├── Workflow/                       ← TheTechIdea.Beep.Pipelines.Workflow
│   ├── Engine/                     ← .Workflow.Engine      (WorkFlowEngine)
│   ├── Models/                     ← .Workflow.Models      (WorkFlowDefinition, etc.)
│   ├── Storage/                    ← .Workflow.Storage     (WorkFlowStorage)
│   └── Templates/                  ← .Workflow.Templates   (built-in workflow templates)
├── Transforms/                     ← TheTechIdea.Beep.Pipelines.Transforms
│   ├── Transformers/               ← .Transforms.Transformers
│   └── Validators/                 ← .Transforms.Validators
├── Scheduling/                     ← TheTechIdea.Beep.Pipelines.Scheduling
│   ├── Schedulers/                 ← .Scheduling.Schedulers
│   └── Queue/                      ← .Scheduling.Queue
└── Observability/                  ← TheTechIdea.Beep.Pipelines.Observability
    ├── Logs/                       ← .Observability.Logs
    ├── Metrics/                    ← .Observability.Metrics
    ├── Alerting/                   ← .Observability.Alerting
    ├── Lineage/                    ← .Observability.Lineage
    ├── Audit/                      ← .Observability.Audit
    └── Notifiers/                  ← .Observability.Notifiers
```

### 3.2 One Class Per File

- **Every class, interface, record, and enum lives in its own `.cs` file.**
- File name = type name (e.g., `PipelineEngine.cs` for `class PipelineEngine`).
- Enums in their own file under the namespace folder where they are primarily used.

### 3.3 Partial Classes for Large Types

Any class exceeding ~250 lines, or with more than one clear responsibility domain, **must** be split into partial classes:

| Pattern | Partial File Suffix | Contains |
|---------|--------------------|-|
| `PipelineEngine.cs` | _(primary)_ | Constructor, public API, properties |
| `PipelineEngine.Execution.cs` | `.Execution` | `ExecutePipelineAsync`, batch processing |
| `PipelineEngine.Validation.cs` | `.Validation` | `ApplyValidatorsAsync`, reject routing |
| `PipelineEngine.Telemetry.cs` | `.Telemetry` | Metrics emission, lineage writing |
| `WorkFlowEngine.cs` | _(primary)_ | Constructor, public API |
| `WorkFlowEngine.Execution.cs` | `.Execution` | `RunAsync`, step dispatch loop |
| `WorkFlowEngine.Approval.cs` | `.Approval` | `ApproveAsync`, `RejectAsync`, gating |
| `PipelineManager.cs` | _(primary)_ | Constructor, CRUD operations |
| `PipelineManager.Legacy.cs` | `.Legacy` | `ImportFromLegacyScriptAsync` compat |
| `AlertingEngine.cs` | _(primary)_ | Constructor, public API |
| `AlertingEngine.Rules.cs` | `.Rules` | Rule evaluation logic |
| `AlertingEngine.Liveness.cs` | `.Liveness` | No-run-within detection |

> **Rule**: if you need to scroll more than one screen to find a method, that file needs splitting.

### 3.4 SOLID Applied to Plugins

| Principle | How It Applies |
|-----------|---------------|
| **S** — Single Responsibility | Each plugin class does ONE thing (read OR transform OR validate). Never combine. |
| **O** — Open/Closed | Add behaviour via new plugin class; never modify `PipelineEngine` switch logic for new steps. |
| **L** — Liskov Substitution | All `IPipelineSource` implementations are interchangeable — engine never type-casts plugins. |
| **I** — Interface Segregation | `IPipelineSource` and `IPipelineSink` are separate; a plugin implements only what it needs. |
| **D** — Dependency Inversion | `PipelineEngine` depends on `IPipelineSource`, never on `CsvFileSource` directly. |

### 3.5 Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Interfaces | `I` prefix + noun | `IPipelineSource`, `IPipelineSink` |
| Plugin classes | noun + role suffix | `CsvFileSource`, `FieldMapTransformer`, `NotNullValidator` |
| Engine/Manager classes | noun + `Engine`/`Manager` | `PipelineEngine`, `PipelineManager` |
| Models | pure noun | `PipelineDefinition`, `PipelineRecord` |
| Enums | singular noun | `RunStatus`, `StepKind`, `AlertTrigger` |
| Async methods | suffix `Async` | `RunAsync`, `ReadAsync`, `SaveAsync` |
| Private fields | `_camelCase` | `_pluginRegistry`, `_logger` |
| Constants | `PascalCase` in `static class` | `PipelineConstants.DefaultBatchSize` |

### 3.6 No Magic Strings or Numbers

All configuration keys, plugin IDs, and default values must be in a constants class:

```csharp
// Interfaces/PipelineConstants.cs
namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    public static class PipelineConstants
    {
        // Default execution values
        public const int  DefaultBatchSize       = 500;
        public const int  MaxParallelBatches     = 4;
        public const int  DefaultMaxRetries      = 3;
        public const int  DefaultStopErrorCount  = 10;
        public const int  MaxRowLogPerStep       = 100;

        // Built-in plugin IDs
        public static class PluginIds
        {
            public const string DbSource          = "beep.source.db";
            public const string DbSink            = "beep.sink.db";
            public const string FieldMapTransform = "beep.transform.fieldmap";
            public const string ExpressionTransform = "beep.transform.expression";
            public const string NotNullValidator  = "beep.validate.notnull";
            public const string CronScheduler     = "beep.schedule.cron";
            public const string FileWatchScheduler = "beep.schedule.filewatch";
            public const string EmailNotifier     = "beep.notify.email";
            public const string WebhookNotifier   = "beep.notify.webhook";
        }

        // Config dictionary keys
        public static class ConfigKeys
        {
            public const string ConnectionName  = "ConnectionName";
            public const string EntityName      = "EntityName";
            public const string BatchSize       = "BatchSize";
            public const string Expression      = "Expression";
            public const string CronExpression  = "CronExpression";
        }
    }
}
```

### 3.7 Async/Await Patterns

- All I/O operations **must** be async all the way up. No `.Result` or `.Wait()`.
- All public async methods accept `CancellationToken token` as last parameter.
- Never use `async void` except for event handlers (and mark those with `// event handler` comment).
- `ConfigureAwait(false)` in library code (non-UI thread context).

```csharp
// ✅ Correct
public async Task<PipelineRunResult> RunAsync(
    PipelineDefinition definition,
    CancellationToken token = default)
{
    await _engine.ExecuteAsync(definition, token).ConfigureAwait(false);
}

// ❌ Wrong
public PipelineRunResult Run(PipelineDefinition definition)
{
    return _engine.ExecuteAsync(definition).Result;  // deadlock risk
}
```

### 3.8 Immutability for Models

Data model classes (records flowing through the pipeline) should be **immutable**:

```csharp
// Models use init-only setters
public class PipelineField
{
    public string Name     { get; init; } = string.Empty;
    public Type   DataType { get; init; } = typeof(object);
    public bool   IsNullable { get; init; } = true;
}

// Records cloned, never mutated in-place
public PipelineRecord Clone()
    => new() { Schema = Schema, Values = (object?[])Values.Clone(), Meta = new(Meta) };
```

### 3.9 Error Reporting Pattern

Never throw from pipeline components — return or populate `IErrorsInfo`:

```csharp
// ✅ BeepDM convention
public IErrorsInfo Execute(IDMEEditor editor)
{
    editor.ErrorObject.Flag = Errors.Ok;
    try
    {
        DoWork();
    }
    catch (Exception ex)
    {
        editor.AddLogMessage(nameof(Execute), ex.Message, DateTime.Now,
            -1, null, Errors.Failed);
    }
    return editor.ErrorObject;
}
```

For pipeline run errors that terminate the run, populate `PipelineRunResult.ErrorMessage` and set `Status = RunStatus.Failed`.

---

## 4. Core Plugin Interfaces

### 4.1 Base Marker Interface

```csharp
namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Marker interface. All pipeline plugins implement this.
    /// </summary>
    public interface IPipelinePlugin
    {
        /// <summary>Unique plugin identity — matches PipelinePluginAttribute.PluginId.</summary>
        string PluginId { get; }

        /// <summary>Human-readable display name shown in Designer UI.</summary>
        string DisplayName { get; }

        /// <summary>Short description shown in tooltip / help.</summary>
        string Description { get; }

        /// <summary>
        /// Returns the parameter schema this plugin accepts.
        /// Used by Designer UI to auto-generate property panels.
        /// </summary>
        IReadOnlyList<PipelineParameterDef> GetParameterDefinitions();

        /// <summary>Apply a parameter bag at runtime before the pipeline starts.</summary>
        void Configure(IReadOnlyDictionary<string, object> parameters);
    }
}
```

### 4.2 Source Plugin

```csharp
namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Extracts records from a data source as an async stream.
    /// The runtime never loads all records into memory simultaneously.
    /// </summary>
    public interface IPipelineSource : IPipelinePlugin
    {
        /// <summary>Schema of the records this source produces.</summary>
        Task<PipelineSchema> GetSchemaAsync(PipelineRunContext ctx, CancellationToken token);

        /// <summary>
        /// Produces records as an async stream.
        /// Callers should consume via await foreach.
        /// </summary>
        IAsyncEnumerable<PipelineRecord> ReadAsync(PipelineRunContext ctx, CancellationToken token);

        /// <summary>Estimated total rows, or -1 if unknown. Used for progress %.</summary>
        Task<long> GetEstimatedRowCountAsync(PipelineRunContext ctx, CancellationToken token);
    }
}
```

### 4.3 Sink Plugin

```csharp
namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Writes records into a target data store.
    /// Receives records in batches for efficient I/O.
    /// </summary>
    public interface IPipelineSink : IPipelinePlugin
    {
        /// <summary>
        /// Called once before streaming begins. Use to open connections,
        /// create missing tables, begin transactions, etc.
        /// </summary>
        Task BeginBatchAsync(PipelineRunContext ctx, PipelineSchema schema, CancellationToken token);

        /// <summary>Write one batch of records.</summary>
        Task WriteBatchAsync(IReadOnlyList<PipelineRecord> batch, PipelineRunContext ctx, CancellationToken token);

        /// <summary>
        /// Called once after all batches. Commit transactions, close files, update metadata.
        /// </summary>
        Task CommitAsync(PipelineRunContext ctx, CancellationToken token);

        /// <summary>Called on cancellation or error. Roll back / clean up.</summary>
        Task RollbackAsync(PipelineRunContext ctx, CancellationToken token);
    }
}
```

### 4.4 Transformer Plugin

```csharp
namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Transforms an async stream of records into another stream.
    /// A single transformer can map columns, split rows, enrich data, etc.
    /// Transformers are composable — chain N transformers between source and sink.
    /// </summary>
    public interface IPipelineTransformer : IPipelinePlugin
    {
        /// <summary>
        /// Declares the output schema given an input schema.
        /// Called before execution to allow downstream plugins to plan.
        /// </summary>
        PipelineSchema GetOutputSchema(PipelineSchema inputSchema);

        /// <summary>
        /// Transforms the incoming record stream into an outgoing record stream.
        /// Can yield 1:1, 1:N, or N:1 records (split / aggregate / filter are all transformers).
        /// </summary>
        IAsyncEnumerable<PipelineRecord> TransformAsync(
            IAsyncEnumerable<PipelineRecord> input,
            PipelineRunContext ctx,
            CancellationToken token);
    }
}
```

### 4.5 Validator Plugin

```csharp
namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    public enum ValidationOutcome { Pass, Warn, Reject }

    public record ValidationResult(
        ValidationOutcome Outcome,
        string RuleName,
        string Message,
        PipelineRecord? Record = null);

    /// <summary>
    /// Validates records against rules. Failed records are routed to the error sink.
    /// Valid records continue to the main sink.
    /// </summary>
    public interface IPipelineValidator : IPipelinePlugin
    {
        /// <summary>
        /// Validate one record. Return Pass, Warn, or Reject with message.
        /// </summary>
        Task<ValidationResult> ValidateAsync(PipelineRecord record, PipelineRunContext ctx, CancellationToken token);
    }
}
```

### 4.6 Scheduler Plugin

```csharp
namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Triggers pipeline runs on a schedule or event.
    /// Built-in: CronScheduler, FileWatchScheduler, EventBusScheduler.
    /// Custom schedulers are plugins.
    /// </summary>
    public interface IPipelineScheduler : IPipelinePlugin
    {
        event EventHandler<PipelineTriggerArgs> Triggered;

        /// <summary>Start watching / waiting for the trigger condition.</summary>
        Task StartAsync(CancellationToken token);

        /// <summary>Stop the scheduler gracefully.</summary>
        Task StopAsync();
    }
}
```

---

## 5. Plugin Attribute

```csharp
namespace TheTechIdea.Beep.Pipelines.Attributes
{
    /// <summary>
    /// Decorate any IPipelinePlugin implementation with this attribute.
    /// AssemblyHandler discovers it at startup, same pattern as [AddinAttribute].
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PipelinePluginAttribute : Attribute
    {
        public string PluginId     { get; }     // e.g. "beep.source.sqlite"
        public string DisplayName  { get; }     // "SQLite Source"
        public PipelinePluginType PluginType { get; }  // Source|Sink|Transformer|Validator|Scheduler
        public string Category    { get; set; } // "Database", "File", "Cloud", etc.
        public string Version     { get; set; } = "1.0.0";
        public string Author      { get; set; } = "The-Tech-Idea";
        public string IconPath    { get; set; } // Optional icon for Designer UI

        public PipelinePluginAttribute(string pluginId, string displayName, PipelinePluginType pluginType)
        {
            PluginId    = pluginId;
            DisplayName = displayName;
            PluginType  = pluginType;
        }
    }

    public enum PipelinePluginType
    {
        Source, Sink, Transformer, Validator, Filter,
        Aggregator, Join, Lookup, Notifier, Scheduler
    }
}
```

---

## 6. Plugin Registry

```csharp
namespace TheTechIdea.Beep.Pipelines.Registry
{
    /// <summary>
    /// Discovers, caches, and instantiates pipeline plugins.
    /// Uses AssemblyHandler for discovery (same mechanism as AddinAttribute connectors).
    /// </summary>
    public class PipelinePluginRegistry
    {
        private readonly IDMEEditor _editor;
        private readonly Dictionary<string, PipelinePluginDescriptor> _descriptors = new();

        public PipelinePluginRegistry(IDMEEditor editor)
        {
            _editor = editor;
        }

        /// <summary>Scan all loaded assemblies and register plugins.</summary>
        public void Discover()
        {
            // Enumerate all types in loaded assemblies that implement IPipelinePlugin
            // and carry [PipelinePluginAttribute]
            foreach (var asm in _editor.assemblyHandler.LoadedAssemblies)
            {
                foreach (var type in asm.GetTypes().Where(t =>
                    !t.IsAbstract &&
                    typeof(IPipelinePlugin).IsAssignableFrom(t) &&
                    t.GetCustomAttribute<PipelinePluginAttribute>() != null))
                {
                    var attr = type.GetCustomAttribute<PipelinePluginAttribute>();
                    _descriptors[attr.PluginId] = new PipelinePluginDescriptor(attr, type);
                }
            }
        }

        /// <summary>Create a fresh instance of a plugin by ID, injecting IDMEEditor.</summary>
        public T Create<T>(string pluginId) where T : IPipelinePlugin
        {
            if (!_descriptors.TryGetValue(pluginId, out var desc))
                throw new KeyNotFoundException($"Plugin '{pluginId}' not found in registry.");
            return (T)Activator.CreateInstance(desc.ImplementationType, _editor);
        }

        public IReadOnlyList<PipelinePluginDescriptor> GetAll() =>
            _descriptors.Values.ToList().AsReadOnly();

        public IReadOnlyList<PipelinePluginDescriptor> GetByType(PipelinePluginType type) =>
            _descriptors.Values.Where(d => d.Attribute.PluginType == type).ToList().AsReadOnly();
    }

    public record PipelinePluginDescriptor(PipelinePluginAttribute Attribute, Type ImplementationType);
}
```

---

## 7. Core Data Models

### 7.1 PipelineRecord — The Unit of Data Flow

```csharp
namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// A single row flowing through the pipeline.
    /// Carries schema reference + typed field values + metadata.
    /// </summary>
    public sealed class PipelineRecord
    {
        /// <summary>Ordered field values. Indexes align with PipelineSchema.Fields.</summary>
        public object?[] Values { get; }

        /// <summary>Schema this record conforms to.</summary>
        public PipelineSchema Schema { get; }

        /// <summary>
        /// Metadata bag: source row number, file line, partition key, etc.
        /// Keys defined as PipelineRecordMeta constants.
        /// </summary>
        public Dictionary<string, object> Meta { get; } = new();

        public PipelineRecord(PipelineSchema schema)
        {
            Schema = schema;
            Values = new object?[schema.Fields.Count];
        }

        public object? this[string fieldName]
        {
            get
            {
                var idx = Schema.GetFieldIndex(fieldName);
                return idx >= 0 ? Values[idx] : null;
            }
            set
            {
                var idx = Schema.GetFieldIndex(fieldName);
                if (idx >= 0) Values[idx] = value;
            }
        }

        public T? Get<T>(string fieldName)
        {
            var v = this[fieldName];
            return v is T typedVal ? typedVal : default;
        }

        /// <summary>Shallow clone for transformation steps that mutate values.</summary>
        public PipelineRecord Clone()
        {
            var clone = new PipelineRecord(Schema);
            Array.Copy(Values, clone.Values, Values.Length);
            foreach (var kv in Meta) clone.Meta[kv.Key] = kv.Value;
            return clone;
        }
    }

    public static class PipelineRecordMeta
    {
        public const string SourceRowNumber = "__src_row";
        public const string SourceFileName = "__src_file";
        public const string CorrelationId  = "__correlation_id";
        public const string PartitionKey   = "__partition_key";
        public const string Timestamp      = "__timestamp";
    }
}
```

### 7.2 PipelineSchema

```csharp
namespace TheTechIdea.Beep.Pipelines.Models
{
    public sealed class PipelineSchema
    {
        public string Name { get; init; } = string.Empty;
        public IReadOnlyList<PipelineField> Fields { get; init; } = Array.Empty<PipelineField>();

        private readonly Dictionary<string, int> _index;

        public PipelineSchema(string name, IEnumerable<PipelineField> fields)
        {
            Name   = name;
            Fields = fields.ToList().AsReadOnly();
            _index = Fields.Select((f, i) => (f.Name, i))
                           .ToDictionary(x => x.Name, x => x.i, StringComparer.OrdinalIgnoreCase);
        }

        public int GetFieldIndex(string name) =>
            _index.TryGetValue(name, out var idx) ? idx : -1;

        /// <summary>Build a PipelineSchema from an EntityStructure (BeepDM model).</summary>
        public static PipelineSchema FromEntityStructure(EntityStructure entity)
        {
            var fields = entity.Fields.Select(f => new PipelineField(
                f.fieldname,
                MapBeepType(f.fieldtype),
                f.IsKey,
                f.AllowDBNull));
            return new PipelineSchema(entity.EntityName, fields);
        }

        private static Type MapBeepType(string beepType) => beepType?.ToLowerInvariant() switch
        {
            "int"     or "integer"  => typeof(int),
            "long"    or "bigint"   => typeof(long),
            "decimal" or "numeric"  => typeof(decimal),
            "double"  or "float"    => typeof(double),
            "bool"    or "boolean"  => typeof(bool),
            "datetime"              => typeof(DateTime),
            "guid"    or "uniqueidentifier" => typeof(Guid),
            "byte[]"  or "binary"   => typeof(byte[]),
            _                       => typeof(string)
        };
    }

    public record PipelineField(
        string Name,
        Type   DataType,
        bool   IsKey       = false,
        bool   IsNullable  = true,
        int    MaxLength   = -1,
        string? Description = null);
}
```

### 7.3 PipelineDefinition — Replaces ETLScriptHDR

```csharp
namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Full pipeline specification: source, transformers, sinks, validators,
    /// scheduler, parameters, and visual layout.
    /// Replaces ETLScriptHDR — can be constructed from one for backward compatibility.
    /// </summary>
    public class PipelineDefinition
    {
        public string Id            { get; set; } = Guid.NewGuid().ToString();
        public string Name          { get; set; } = string.Empty;
        public string Description   { get; set; } = string.Empty;
        public string Category      { get; set; } = string.Empty;
        public string Tags          { get; set; } = string.Empty;   // csv
        public int    Version       { get; set; } = 1;
        public bool   IsEnabled     { get; set; } = true;

        // ── Connectivity ───────────────────────────────────────────────────
        public string SourcePluginId    { get; set; } = string.Empty;
        public string SinkPluginId      { get; set; } = string.Empty;
        public string ErrorSinkPluginId { get; set; } = string.Empty; // rows that fail validation land here

        // ── Execution steps (ordered) ──────────────────────────────────────
        public List<PipelineStepDef> Steps { get; set; } = new();

        // ── Parameters ────────────────────────────────────────────────────
        public Dictionary<string, object> Parameters { get; set; } = new();

        // ── Scheduling ────────────────────────────────────────────────────
        public string? SchedulerPluginId { get; set; }
        public Dictionary<string, object> SchedulerParameters { get; set; } = new();

        // ── Execution Policy ──────────────────────────────────────────────
        public int  BatchSize           { get; set; } = 500;
        public int  MaxParallelBatches  { get; set; } = 4;
        public int  MaxRetries          { get; set; } = 3;
        public int  StopOnErrorCount    { get; set; } = 0;    // 0 = never stop
        public bool EnableCheckpointing { get; set; } = true;
        public bool EnableLineageTracking { get; set; } = true;

        // ── Visual Layout (Phase 7) ────────────────────────────────────────
        public string? VisualLayoutJson { get; set; }

        // ── Run History ───────────────────────────────────────────────────
        public DateTime? LastRunAt     { get; set; }
        public string?   LastRunStatus { get; set; }
        public string?   LastRunId     { get; set; }

        // ── Backward compat: migrate from ETLScriptHDR ────────────────────
        public static PipelineDefinition FromLegacyScript(ETLScriptHDR hdr)
        {
            var pd = new PipelineDefinition
            {
                Id          = hdr.GuidId ?? Guid.NewGuid().ToString(),
                Name        = hdr.ScriptName,
                LastRunAt   = hdr.LastRunDateTime,
                LastRunId   = hdr.LastRunCorrelationId,
                LastRunStatus = hdr.LastRunSummary
            };
            foreach (var det in hdr.ScriptDetails ?? new())
                pd.Steps.Add(PipelineStepDef.FromLegacyScriptDet(det));
            return pd;
        }
    }
}
```

### 7.4 PipelineStepDef — Replaces ETLScriptDet

```csharp
namespace TheTechIdea.Beep.Pipelines.Models
{
    public enum StepKind
    {
        Extract,        // reads from a source
        Transform,      // maps / converts fields
        Filter,         // drops records not matching predicate
        Validate,       // DQ rules — rejects go to error sink
        Enrich,         // lookup / join against reference data
        Aggregate,      // group-by / sum / count
        Load,           // writes to sink
        Notify,         // sends email / webhook / event
        Script          // user C# snippet (sandboxed)
    }

    public class PipelineStepDef
    {
        public string   Id          { get; set; } = Guid.NewGuid().ToString();
        public string   Name        { get; set; } = string.Empty;
        public int      Sequence    { get; set; }
        public StepKind Kind        { get; set; }
        public string   PluginId    { get; set; } = string.Empty;
        public bool     IsActive    { get; set; } = true;
        public bool     IsParallel  { get; set; } = false;

        /// <summary>Plugin-specific configuration as key-value pairs.</summary>
        public Dictionary<string, object> Config { get; set; } = new();

        /// <summary>
        /// Optional field mapping: key = dest field name, value = source expression.
        /// Supports simple column names and basic expressions.
        /// </summary>
        public Dictionary<string, string> FieldMappings { get; set; } = new();

        /// <summary>Optional filter predicate (e.g. "Age > 18 AND Country = 'US'").</summary>
        public string? FilterExpression { get; set; }

        /// <summary>Retry policy overrides for this step (0 inherits from PipelineDefinition).</summary>
        public int MaxRetries { get; set; } = 0;

        /// <summary>Timeout in seconds (0 = inherit from run context).</summary>
        public int TimeoutSeconds { get; set; } = 0;

        // Backward compat
        public static PipelineStepDef FromLegacyScriptDet(ETLScriptDet det)
        {
            var kind = det.ScriptType switch
            {
                DDLScriptType.CreateEntity => StepKind.Extract,
                DDLScriptType.CopyData     => StepKind.Load,
                _                          => StepKind.Transform
            };
            return new PipelineStepDef
            {
                Name = det.SourceEntityName,
                Kind = kind,
                IsActive = det.Active,
                Config = new Dictionary<string, object>
                {
                    ["SourceDataSource"]      = det.SourceDataSourceName,
                    ["SourceEntity"]          = det.SourceEntityName,
                    ["DestinationDataSource"] = det.DestinationDataSourceName,
                    ["DestinationEntity"]     = det.DestinationEntityName,
                    ["CopyData"]              = det.CopyData
                }
            };
        }
    }
}
```

### 7.5 PipelineRunContext

```csharp
namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Immutable-ish bag of runtime state passed to every plugin during a run.
    /// Carries correlation ID, resolved services, parameters, and accumulated telemetry.
    /// </summary>
    public class PipelineRunContext
    {
        public string RunId             { get; } = Guid.NewGuid().ToString();
        public string PipelineId        { get; init; } = string.Empty;
        public string PipelineName      { get; init; } = string.Empty;
        public DateTime StartedAtUtc    { get; } = DateTime.UtcNow;
        public IDMEEditor DMEEditor     { get; init; } = null!;
        public IProgress<PassedArgs> Progress { get; init; } = new Progress<PassedArgs>();
        public CancellationToken Token  { get; init; }

        /// <summary>Resolved parameters: definition defaults overridden by trigger params.</summary>
        public IReadOnlyDictionary<string, object> Parameters { get; init; }
            = new Dictionary<string, object>();

        /// <summary>Shared state bag. Plugins may write to this to pass data to later steps.</summary>
        public Dictionary<string, object> RuntimeState { get; } = new();

        // ── Telemetry ──────────────────────────────────────────────────────
        public long TotalRecordsRead       { get; set; }
        public long TotalRecordsWritten    { get; set; }
        public long TotalRecordsRejected   { get; set; }
        public long TotalRecordsWarned     { get; set; }
        public long TotalBytesProcessed    { get; set; }
        public int  StepsCompleted         { get; set; }
        public int  StepsFailed            { get; set; }
        public string? CheckpointId        { get; set; }

        // ── Lineage ───────────────────────────────────────────────────────
        public List<DataLineageRecord> LineageEntries { get; } = new();

        /// <summary>Emit a progress message without blocking the stream.</summary>
        public void ReportProgress(string message, int pct = -1)
        {
            Progress?.Report(new PassedArgs { Messege = message, ParameterInt1 = pct });
        }
    }
}
```

---

## 8. Built-in Plugin Stubs (to be implemented in Phase 2)

| PluginId | Type | Description |
|----------|------|-------------|
| `beep.source.datasource` | Source | Reads any `IDataSource` registered in BeepDM |
| `beep.source.csv` | Source | Streams rows from CSV / TSV files |
| `beep.source.json` | Source | Streams rows from JSON arrays |
| `beep.source.excel` | Source | Reads Excel worksheets |
| `beep.sink.datasource` | Sink | Writes to any `IDataSource` registered in BeepDM |
| `beep.sink.csv` | Sink | Writes rows to CSV / TSV files |
| `beep.sink.json` | Sink | Writes rows to JSON arrays |
| `beep.sink.errorlog` | Sink | Writes rejected rows to structured error log |
| `beep.transform.fieldmap` | Transformer | Static field-to-field column mapping |
| `beep.transform.expression` | Transformer | Expression-based computed fields  |
| `beep.transform.typecast` | Transformer | Auto type conversion with configurable rules |
| `beep.transform.dedup` | Transformer | Remove duplicate rows by key |
| `beep.validate.notnull` | Validator | Reject rows with null required fields |
| `beep.validate.regex` | Validator | Reject rows where field fails regex |
| `beep.validate.range` | Validator | Reject rows outside numeric/date range |
| `beep.schedule.cron` | Scheduler | Standard cron-expression trigger |
| `beep.schedule.filewatch` | Scheduler | Trigger on file arrival in folder |
| `beep.schedule.manual` | Scheduler | Triggered only by explicit API call |

---

## 9. Deliverables (Implementation Checklist)

- [ ] Create `DataManagementModelsStandard/Pipelines/` project folder
- [ ] Implement all interface files under `Interfaces/`
- [ ] Implement model files under `Models/`
- [ ] Implement `PipelinePluginAttribute` under `Attributes/`
- [ ] Implement `PipelinePluginRegistry` with discovery via `AssemblyHandler`
- [ ] Add `PipelinePluginRegistry` to `IDMEEditor` as `Pipelines` property
- [ ] Unit tests for `PipelineRecord`, `PipelineSchema`, `PipelinePluginRegistry.Discover()`
- [ ] `PipelineDefinition.FromLegacyScript()` round-trip test
- [ ] Update `IDMEEditor` to expose `IPipelinePluginRegistry Pipelines { get; }`

---

## 10. Migration Notes

`ETLScriptHDR` and `ETLScriptDet` are **not deleted**. They remain as the persistence format for the existing `ETLScriptManager`. `PipelineDefinition.FromLegacyScript()` converts them at runtime. All existing `ETLScriptManager` API continues to work.

---

## 11. Estimated Effort

| Task | Days |
|------|------|
| Interfaces (9 files) | 1 |
| Models (9 files) | 2 |
| PipelinePluginAttribute | 0.5 |
| PipelinePluginRegistry | 1 |
| Unit tests | 1 |
| IDMEEditor extension point | 0.5 |
| **Total** | **6 days** |
