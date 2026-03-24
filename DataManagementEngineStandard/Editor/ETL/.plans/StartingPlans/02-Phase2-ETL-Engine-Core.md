# Phase 2 — Core ETL Pipeline Engine

**Version:** 1.0  
**Date:** 2026-03-13  
**Status:** Design  
**Depends on:** Phase 1 (Plugin Architecture)

---

## 1. Objective

Build the runtime engine that executes `PipelineDefinition`s. This engine replaces and supersedes `ETLEditor`, `ETLDataCopier`, `ETLEntityCopyHelper`, `ETLEntityProcessor`, and `ETLScriptBuilder` with a unified, streaming, observable, fault-tolerant pipeline runner.

The existing classes are **kept as backward-compatibility shims** but all new work uses the engine defined here.

---

## 2. Design Philosophy

```
Source ──► [Buffer Channel] ──► Transformer₁ ──► Transformer₂ ──► Validator ──► [Buffer Channel] ──► Sink
                                                                        │
                                                                    Rejected ──► Error Sink
```

- Data flows as `IAsyncEnumerable<PipelineRecord>` between steps
- `System.Threading.Channels.Channel<T>` acts as the bounded buffer between producer and consumer, controlling backpressure
- Each step runs in its own `Task`; the engine fans them together with `Task.WhenAll`
- Cancellation propagates through `CancellationToken` to every step and plugin
- Checkpoints snapshot position in the stream so aborted runs can resume

---

## 3. Component Map

```
TheTechIdea.Beep.Pipelines.Engine/
├── PipelineEngine.cs            ← Top-level runner (replaces ETLEditor)
├── PipelineStepRunner.cs        ← Executes a single PipelineStepDef
├── PipelineChannelBridge.cs     ← Channel<PipelineRecord> producer/consumer helpers
├── PipelineRetryPolicy.cs       ← Exponential backoff with jitter
├── PipelineCheckpointManager.cs ← Write / resume checkpoint state
├── PipelineLineageTracker.cs    ← Column-level lineage accumulation
└── Built-in Plugins/
    ├── Sources/
    │   ├── DataSourcePlugin.cs  ← IDataSource → IPipelineSource adapter
    │   └── CsvSourcePlugin.cs
    ├── Sinks/
    │   ├── DataSinkPlugin.cs    ← IDataSource → IPipelineSink adapter
    │   └── ErrorLogSinkPlugin.cs
    └── Transformers/
        ├── FieldMapTransformer.cs
        ├── ExpressionTransformer.cs
        └── TypeCastTransformer.cs
```

---

## 4. PipelineEngine — Full Design

```csharp
namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Central runtime that executes a PipelineDefinition.
    /// This is the successor to ETLEditor.
    /// 
    /// Usage:
    ///     var engine = new PipelineEngine(editor);
    ///     var result = await engine.RunAsync(definition, progress, token);
    /// </summary>
    public class PipelineEngine
    {
        private readonly IDMEEditor          _editor;
        private readonly PipelinePluginRegistry _registry;
        private readonly PipelineCheckpointManager _checkpoints;

        public PipelineEngine(IDMEEditor editor)
        {
            _editor      = editor      ?? throw new ArgumentNullException(nameof(editor));
            _registry    = editor.Pipelines as PipelinePluginRegistry
                             ?? new PipelinePluginRegistry(editor);
            _checkpoints = new PipelineCheckpointManager(editor);
        }

        // ── Public API ──────────────────────────────────────────────────────

        /// <summary>Run a pipeline to completion.</summary>
        public Task<PipelineRunResult> RunAsync(
            PipelineDefinition definition,
            IProgress<PassedArgs>? progress    = null,
            CancellationToken      token       = default,
            IReadOnlyDictionary<string, object>? overrideParams = null)
        {
            var ctx = BuildContext(definition, progress, token, overrideParams);
            return ExecutePipelineAsync(definition, ctx);
        }

        /// <summary>Validate a definition without running it.</summary>
        public Task<PipelineRunResult> DryRunAsync(
            PipelineDefinition definition,
            CancellationToken token = default)
        {
            /* Connect source, read first 100 rows, validate schema compatibility
               against each step, return result with no data written */
        }

        /// <summary>Resume a previously checkpointed run.</summary>
        public Task<PipelineRunResult> ResumeAsync(
            string checkpointId,
            IProgress<PassedArgs>? progress = null,
            CancellationToken token = default)
        {
            /* Load checkpoint, rebuild context with resume position,
               skip completed steps, continue from last committed batch */
        }

        // ── Internal execution ───────────────────────────────────────────────

        private async Task<PipelineRunResult> ExecutePipelineAsync(
            PipelineDefinition def, PipelineRunContext ctx)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                ctx.ReportProgress($"Pipeline '{def.Name}' started", 0);

                // 1. Resolve source plugin
                var source = _registry.Create<IPipelineSource>(def.SourcePluginId);
                source.Configure(def.Parameters);

                // 2. Get source schema
                var schema = await source.GetSchemaAsync(ctx, ctx.Token);

                // 3. Build transformer chain
                var transformers = def.Steps
                    .Where(s => s.IsActive && s.Kind == StepKind.Transform)
                    .OrderBy(s => s.Sequence)
                    .Select(s =>
                    {
                        var t = _registry.Create<IPipelineTransformer>(s.PluginId);
                        t.Configure(s.Config);
                        return t;
                    }).ToList();

                // 4. Build validators
                var validators = def.Steps
                    .Where(s => s.IsActive && s.Kind == StepKind.Validate)
                    .OrderBy(s => s.Sequence)
                    .Select(s =>
                    {
                        var v = _registry.Create<IPipelineValidator>(s.PluginId);
                        v.Configure(s.Config);
                        return v;
                    }).ToList();

                // 5. Resolve sinks
                var sink      = _registry.Create<IPipelineSink>(def.SinkPluginId);
                var errorSink = string.IsNullOrEmpty(def.ErrorSinkPluginId)
                    ? null
                    : _registry.Create<IPipelineSink>(def.ErrorSinkPluginId);

                // 6. Build the streaming pipeline
                IAsyncEnumerable<PipelineRecord> stream = source.ReadAsync(ctx, ctx.Token);

                // Apply transformers
                foreach (var transformer in transformers)
                    stream = transformer.TransformAsync(stream, ctx, ctx.Token);

                // Apply validators + route rejects
                stream = ApplyValidatorsAsync(stream, validators, errorSink, ctx);

                // 7. Open sink
                await sink.BeginBatchAsync(ctx, schema, ctx.Token);

                // 8. Drain stream into sink in batches
                await DrainToBatchedSinkAsync(stream, sink, def.BatchSize, ctx);

                // 9. Commit
                await sink.CommitAsync(ctx, ctx.Token);

                // 10. Save checkpoint as COMPLETE
                if (def.EnableCheckpointing)
                    await _checkpoints.CompleteAsync(ctx.RunId);

                sw.Stop();
                ctx.ReportProgress($"Pipeline '{def.Name}' completed", 100);
                return BuildResult(ctx, sw.Elapsed, success: true);
            }
            catch (OperationCanceledException)
            {
                await SaveCancellationCheckpointAsync(def, ctx);
                return BuildResult(ctx, sw.Elapsed, success: false, cancelled: true);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("PipelineEngine", ex.Message, DateTime.Now, -1, null, Errors.Failed);
                return BuildResult(ctx, sw.Elapsed, success: false, error: ex);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private async IAsyncEnumerable<PipelineRecord> ApplyValidatorsAsync(
            IAsyncEnumerable<PipelineRecord> stream,
            IReadOnlyList<IPipelineValidator> validators,
            IPipelineSink? errorSink,
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await foreach (var rec in stream.WithCancellation(token))
            {
                bool rejected = false;
                foreach (var v in validators)
                {
                    var result = await v.ValidateAsync(rec, ctx, token);
                    if (result.Outcome == ValidationOutcome.Reject)
                    {
                        ctx.TotalRecordsRejected++;
                        // Route to error sink (non-blocking — buffer)
                        // error sink writing is fire-and-queued
                        rejected = true;
                        break;
                    }
                    if (result.Outcome == ValidationOutcome.Warn)
                        ctx.TotalRecordsWarned++;
                }
                if (!rejected) yield return rec;
            }
        }

        private async Task DrainToBatchedSinkAsync(
            IAsyncEnumerable<PipelineRecord> stream,
            IPipelineSink sink,
            int batchSize,
            PipelineRunContext ctx)
        {
            var batch = new List<PipelineRecord>(batchSize);
            await foreach (var rec in stream.WithCancellation(ctx.Token))
            {
                batch.Add(rec);
                ctx.TotalRecordsRead++;

                if (batch.Count >= batchSize)
                {
                    await WriteWithRetryAsync(sink, batch, ctx);
                    ctx.TotalRecordsWritten += batch.Count;
                    batch.Clear();

                    if (ctx.Token.IsCancellationRequested) break;

                    ctx.ReportProgress(
                        $"Written {ctx.TotalRecordsWritten:N0} rows",
                        -1);
                }
            }
            // Final partial batch
            if (batch.Count > 0)
            {
                await WriteWithRetryAsync(sink, batch, ctx);
                ctx.TotalRecordsWritten += batch.Count;
            }
        }

        private async Task WriteWithRetryAsync(
            IPipelineSink sink,
            List<PipelineRecord> batch,
            PipelineRunContext ctx)
        {
            var policy = new PipelineRetryPolicy(maxRetries: 3, baseDelayMs: 500);
            await policy.ExecuteAsync(async () =>
                await sink.WriteBatchAsync(batch, ctx, ctx.Token));
        }

        private PipelineRunContext BuildContext(
            PipelineDefinition def,
            IProgress<PassedArgs>? progress,
            CancellationToken token,
            IReadOnlyDictionary<string, object>? overrideParams)
        {
            var merged = new Dictionary<string, object>(def.Parameters);
            if (overrideParams != null)
                foreach (var kv in overrideParams)
                    merged[kv.Key] = kv.Value;

            return new PipelineRunContext
            {
                PipelineId   = def.Id,
                PipelineName = def.Name,
                DMEEditor    = _editor,
                Progress     = progress ?? new Progress<PassedArgs>(),
                Token        = token,
                Parameters   = merged
            };
        }

        private static PipelineRunResult BuildResult(
            PipelineRunContext ctx,
            TimeSpan elapsed,
            bool success,
            bool cancelled = false,
            Exception? error = null)
        {
            return new PipelineRunResult
            {
                RunId              = ctx.RunId,
                PipelineId         = ctx.PipelineId,
                PipelineName       = ctx.PipelineName,
                StartedAtUtc       = ctx.StartedAtUtc,
                FinishedAtUtc      = DateTime.UtcNow,
                Duration           = elapsed,
                Success            = success,
                Cancelled          = cancelled,
                ErrorMessage       = error?.Message,
                TotalRecordsRead   = ctx.TotalRecordsRead,
                TotalRecordsWritten = ctx.TotalRecordsWritten,
                TotalRecordsRejected = ctx.TotalRecordsRejected,
                StepsCompleted     = ctx.StepsCompleted,
                StepsFailed        = ctx.StepsFailed
            };
        }
    }
}
```

---

## 5. Built-in Source: DataSourcePlugin

Adapts any BeepDM `IDataSource` to work as a streaming `IPipelineSource`. This connects the entire existing connector ecosystem (SQLite, SQL Server, MySQL, Oracle, REST APIs, etc.) to the new pipeline engine with zero extra code.

```csharp
[PipelinePlugin("beep.source.datasource", "BeepDM Data Source", PipelinePluginType.Source,
    Category = "Database")]
public class DataSourcePlugin : IPipelineSource
{
    private readonly IDMEEditor _editor;
    private string _dataSourceName = string.Empty;
    private string _entityName     = string.Empty;
    private string? _filterExpr    = null;
    private int    _batchSize      = 1000;

    public string PluginId    => "beep.source.datasource";
    public string DisplayName => "BeepDM Data Source";
    public string Description => "Reads any data source registered with BeepDM";

    public DataSourcePlugin(IDMEEditor editor) => _editor = editor;

    public void Configure(IReadOnlyDictionary<string, object> p)
    {
        _dataSourceName = p.GetString("DataSourceName");
        _entityName     = p.GetString("EntityName");
        _filterExpr     = p.GetStringOrNull("Filter");
        _batchSize      = p.GetInt("BatchSize", 1000);
    }

    public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() =>
    [
        new("DataSourceName", typeof(string), required: true,  "Name of the BeepDM connection"),
        new("EntityName",     typeof(string), required: true,  "Entity / table name"),
        new("Filter",         typeof(string), required: false, "Optional filter expression"),
        new("BatchSize",      typeof(int),    required: false, "Read batch size (default 1000)", 1000)
    ];

    public async Task<PipelineSchema> GetSchemaAsync(PipelineRunContext ctx, CancellationToken token)
    {
        var ds = _editor.GetDataSource(_dataSourceName);
        var entity = ds.GetEntityStructure(_entityName, true);
        return PipelineSchema.FromEntityStructure(entity);
    }

    public async Task<long> GetEstimatedRowCountAsync(PipelineRunContext ctx, CancellationToken token)
    {
        // Best-effort: many sources expose a COUNT equivalent
        return -1;
    }

    public async IAsyncEnumerable<PipelineRecord> ReadAsync(
        PipelineRunContext ctx,
        [EnumeratorCancellation] CancellationToken token)
    {
        var ds = _editor.GetDataSource(_dataSourceName);
        if (ds.ConnectionStatus != ConnectionState.Open)
            ds.Openconnection();

        var schema = await GetSchemaAsync(ctx, token);
        var filters = BuildFilters(_filterExpr);

        // Read in batches, yield record-by-record
        int offset = 0;
        while (true)
        {
            token.ThrowIfCancellationRequested();

            var data = ds.GetEntity(_entityName, filters) as DataTable;
            if (data == null || data.Rows.Count == 0) break;

            foreach (DataRow row in data.Rows)
            {
                var rec = new PipelineRecord(schema);
                for (int i = 0; i < schema.Fields.Count; i++)
                    rec.Values[i] = row.IsNull(i) ? null : row[i];
                rec.Meta[PipelineRecordMeta.SourceRowNumber] = offset++;
                yield return rec;
            }

            if (data.Rows.Count < _batchSize) break;
        }
    }

    private List<AppFilter> BuildFilters(string? expr)
    {
        // Parse simple "Field op Value" expressions into AppFilter list
        // Full expression parser in Phase 4
        return new List<AppFilter>();
    }
}
```

---

## 6. Built-in Sink: DataSinkPlugin

```csharp
[PipelinePlugin("beep.sink.datasource", "BeepDM Data Sink", PipelinePluginType.Sink,
    Category = "Database")]
public class DataSinkPlugin : IPipelineSink
{
    public string PluginId    => "beep.sink.datasource";
    public string DisplayName => "BeepDM Data Sink";
    public string Description => "Writes to any data source registered with BeepDM";

    // Configuration: DataSourceName, EntityName, WriteMode (Insert|Update|Upsert|Merge)
    // WriteMode = Upsert default (create table if missing, match on key fields)

    public async Task BeginBatchAsync(PipelineRunContext ctx, PipelineSchema schema, CancellationToken token)
    {
        // Open connection
        // If CreateMissingEntity=true: check if entity exists, create from schema if not
        // Begin transaction if supported
    }

    public async Task WriteBatchAsync(IReadOnlyList<PipelineRecord> batch, PipelineRunContext ctx, CancellationToken token)
    {
        // Convert PipelineRecord[] → dynamic objects or DataTable rows
        // Call ds.InsertInTable / UpdateTable depending on WriteMode
        // Per-record error captured → routed to error sink, not thrown
    }

    public async Task CommitAsync(PipelineRunContext ctx, CancellationToken token)
    {
        // Commit transaction, update entity stats
    }

    public async Task RollbackAsync(PipelineRunContext ctx, CancellationToken token)
    {
        // Rollback transaction, clean up any partial writes
    }
}
```

---

## 7. Built-in Transformer: FieldMapTransformer

```csharp
[PipelinePlugin("beep.transform.fieldmap", "Field Mapping", PipelinePluginType.Transformer,
    Category = "Transform")]
public class FieldMapTransformer : IPipelineTransformer
{
    // Configuration: Dictionary<string, string> Mappings
    // Key = destination field name, Value = source field name or literal

    public PipelineSchema GetOutputSchema(PipelineSchema inputSchema)
    {
        // Return new schema with renamed/selected fields per mapping config
    }

    public async IAsyncEnumerable<PipelineRecord> TransformAsync(
        IAsyncEnumerable<PipelineRecord> input,
        PipelineRunContext ctx,
        [EnumeratorCancellation] CancellationToken token)
    {
        var outSchema = GetOutputSchema(/* cached from Configure */);
        await foreach (var rec in input.WithCancellation(token))
        {
            var outRec = new PipelineRecord(outSchema);
            foreach (var (destField, srcExpr) in _mappings)
                outRec[destField] = ResolveValue(rec, srcExpr);
            yield return outRec;
        }
    }
}
```

---

## 8. PipelineRetryPolicy

```csharp
public class PipelineRetryPolicy
{
    private readonly int   _maxRetries;
    private readonly int   _baseDelayMs;
    private readonly double _backoffFactor;

    public PipelineRetryPolicy(int maxRetries = 3, int baseDelayMs = 500, double backoffFactor = 2.0)
    {
        _maxRetries    = maxRetries;
        _baseDelayMs   = baseDelayMs;
        _backoffFactor = backoffFactor;
    }

    public async Task ExecuteAsync(Func<Task> operation)
    {
        int attempt = 0;
        while (true)
        {
            try      { await operation(); return; }
            catch (Exception) when (attempt < _maxRetries)
            {
                int delay = (int)(_baseDelayMs * Math.Pow(_backoffFactor, attempt));
                // Add jitter: ±20%
                delay += Random.Shared.Next(-(delay / 5), delay / 5);
                await Task.Delay(delay);
                attempt++;
            }
        }
    }
}
```

---

## 9. PipelineCheckpointManager

Checkpoints allow a 3-hour pipeline run that fails at step 4 of 6 to resume from step 4 without re-reading and re-writing the first three steps' worth of data.

```csharp
public class PipelineCheckpointManager
{
    // Storage: JSON files in ExePath/Checkpoints/{runId}.chk.json
    // Fields: PipelineId, RunId, LastCommittedStepId, LastCommittedBatchOffset, 
    //         ContextSnapshot (partial RuntimeState), CreatedAt, Status

    public Task SaveAsync(PipelineRunContext ctx, string lastCommittedStepId, long batchOffset) { }
    public Task CompleteAsync(string runId) { }
    public Task<PipelineCheckpoint?> LoadAsync(string checkpointId) { }
    public Task DeleteAsync(string runId) { }
    public Task<IReadOnlyList<PipelineCheckpoint>> ListPendingAsync() { }
}
```

---

## 10. PipelineManager — Replaces ETLScriptManager

```csharp
namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Persists, loads, validates, and triggers PipelineDefinitions.
    /// Replaces ETLScriptManager.
    /// Backward compatible: wraps ETLScriptManager for legacy scripts.
    /// 
    /// Storage: ExePath/Pipelines/{id}.pipeline.json
    /// </summary>
    public class PipelineManager
    {
        // CRUD
        Task<IErrorsInfo> SaveAsync(PipelineDefinition pipeline);
        Task<IErrorsInfo> DeleteAsync(string pipelineId);
        Task<PipelineDefinition?> LoadAsync(string pipelineId);
        Task<IReadOnlyList<PipelineDefinition>> LoadAllAsync();
        Task<IReadOnlyList<PipelineDefinition>> FindByTagAsync(string tag);

        // Execution
        Task<PipelineRunResult> RunAsync(string pipelineId, IProgress<PassedArgs>? progress, CancellationToken token);
        Task<PipelineRunResult> RunDefinitionAsync(PipelineDefinition def, IProgress<PassedArgs>? progress, CancellationToken token);

        // History
        Task<IReadOnlyList<PipelineRunResult>> GetRunHistoryAsync(string pipelineId, int limit = 50);

        // Legacy adapter
        Task<PipelineDefinition> ImportFromLegacyScriptAsync(ETLScriptHDR hdr);
        Task<ETLScriptHDR> ExportToLegacyScriptAsync(PipelineDefinition def);
    }
}
```

---

## 11. Deliverables (Implementation Checklist)

- [ ] `PipelineEngine.cs` — core runner
- [ ] `PipelineStepRunner.cs` — per-step executor
- [ ] `PipelineRetryPolicy.cs`
- [ ] `PipelineCheckpointManager.cs`
- [ ] `PipelineLineageTracker.cs`
- [ ] `PipelineManager.cs` — persistence layer
- [ ] `DataSourcePlugin.cs` (Source)
- [ ] `DataSinkPlugin.cs` (Sink)
- [ ] `ErrorLogSinkPlugin.cs` (Sink)
- [ ] `CsvSourcePlugin.cs` (Source)
- [ ] `FieldMapTransformer.cs` (Transformer)
- [ ] `TypeCastTransformer.cs` (Transformer)
- [ ] Integration tests: read SQLite → transform → write SQLite
- [ ] Integration test: cancel mid-run, resume from checkpoint
- [ ] `ETLEditor` — update to delegate to `PipelineEngine` internally (shim)

---

## 12. Estimated Effort

| Task | Days |
|------|------|
| PipelineEngine + helpers | 4 |
| PipelineManager | 2 |
| DataSourcePlugin + DataSinkPlugin | 2 |
| CsvSource, ErrorLogSink | 1 |
| FieldMap + TypeCast transformers | 1.5 |
| PipelineRetryPolicy + Checkpoint | 1.5 |
| Integration tests | 2 |
| **Total** | **14 days** |
