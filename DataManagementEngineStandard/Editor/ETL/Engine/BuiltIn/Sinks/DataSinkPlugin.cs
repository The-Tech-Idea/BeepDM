using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Sinks
{
    /// <summary>
    /// Built-in sink that writes records into any <c>IDataSource</c> registered with BeepDM.
    /// Parameters:
    ///   DataSourceName  (string, required) — registered connection name.
    ///   EntityName      (string, required) — target table / entity.
    ///   WriteMode       (string, optional) — Insert | Update | Upsert (default: Insert).
    /// </summary>
    [PipelinePlugin(
        "beep.sink.datasource",
        "BeepDM Data Sink",
        PipelinePluginType.Sink,
        Category = "Database",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class DataSinkPlugin : IPipelineSink
    {
        // ── IPipelinePlugin ───────────────────────────────────────────────

        public string PluginId    => "beep.sink.datasource";
        public string DisplayName => "BeepDM Data Sink";
        public string Description => "Writes records into any IDataSource registered in BeepDM.";

        private string _dataSourceName = string.Empty;
        private string _entityName     = string.Empty;
        private string _writeMode      = "Insert";
        private bool _useTransaction;

        private string SessionKey => $"{PluginId}:{_dataSourceName}:{_entityName}";
        private sealed class SinkSession
        {
            public IDataSource Source;
            public bool TransactionActive;
            public long PendingWrites;
        }

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() =>
            new[]
            {
                new PipelineParameterDef { Name = "DataSourceName", Type = ParamType.String, IsRequired = true,  Description = "Registered connection name" },
                new PipelineParameterDef { Name = "EntityName",     Type = ParamType.String, IsRequired = true,  Description = "Target table or entity" },
                new PipelineParameterDef { Name = "WriteMode",      Type = ParamType.String, IsRequired = false, Description = "Insert | Update | Upsert (native capability required)" },
                new PipelineParameterDef { Name = "UseTransaction", Type = ParamType.Boolean, IsRequired = false, Description = "Require IRDBSource transactions (default: false; partial writes possible)" }
            };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("DataSourceName", out var ds)) _dataSourceName = ds?.ToString() ?? string.Empty;
            if (parameters.TryGetValue("EntityName",     out var en)) _entityName     = en?.ToString() ?? string.Empty;
            if (parameters.TryGetValue("WriteMode",      out var wm)) _writeMode      = wm?.ToString() ?? "Insert";
            _useTransaction = parameters.TryGetValue("UseTransaction", out var tx) && bool.TryParse(tx?.ToString(), out var enabled) && enabled;
            if (!new[] { "INSERT", "UPDATE", "UPSERT" }.Contains(_writeMode.ToUpperInvariant()))
                throw new ArgumentException($"Unsupported write mode '{_writeMode}'.", nameof(parameters));
        }

        // ── IPipelineSink ─────────────────────────────────────────────────

        public Task BeginBatchAsync(PipelineRunContext ctx, PipelineSchema schema, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var ds = ctx.DMEEditor.GetDataSource(_dataSourceName)
                     ?? throw new InvalidOperationException($"Data source '{_dataSourceName}' not found.");

            if (string.Equals(_writeMode, "Upsert", StringComparison.OrdinalIgnoreCase) && ds is not IUpsertDataSource)
                throw new NotSupportedException("Upsert requires IUpsertDataSource; failed updates cannot safely be treated as missing rows.");
            if (_useTransaction && ds is not IRDBSource)
                throw new NotSupportedException("This datasource does not expose IRDBSource transactions.");
            if (ds.ConnectionStatus != System.Data.ConnectionState.Open && ds.Openconnection() != System.Data.ConnectionState.Open)
                throw new InvalidOperationException($"Could not open datasource '{_dataSourceName}'.");

            var session = new SinkSession { Source = ds };
            ctx.RuntimeState[SessionKey] = session;
            if (_useTransaction)
            {
                var begin = ((IRDBSource)ds).BeginTransaction(new PassedArgs());
                if (begin?.Flag != Errors.Ok)
                    throw new InvalidOperationException($"Could not begin datasource transaction: {begin?.Message}");
                session.TransactionActive = true;
            }

            return Task.CompletedTask;
        }

        public Task WriteBatchAsync(
            IReadOnlyList<PipelineRecord> batch,
            PipelineRunContext ctx,
            CancellationToken token)
        {
            var session = GetSession(ctx);
            var ds = session.Source;
            long acknowledged = 0;

            foreach (var record in batch)
            {
                token.ThrowIfCancellationRequested();
                var obj = RecordToExpandoObject(record);

                IErrorsInfo result;
                try
                {
                    result = _writeMode.ToUpperInvariant() switch
                    {
                        "UPDATE" => ds.UpdateEntity(_entityName, obj),
                        "UPSERT" => ((IUpsertDataSource)ds).UpsertEntity(_entityName, obj),
                        _ => ds.InsertEntity(_entityName, obj)
                    };
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    throw new PipelineWriteException($"Write to '{_entityName}' threw: {ex.Message}", acknowledged, false, ex);
                }

                if (result?.Flag != Errors.Ok)
                    throw new PipelineWriteException($"Write to '{_entityName}' was not acknowledged: {result?.Message}",
                        acknowledged, acknowledged == 0 && result?.Flag == Errors.Failed);
                acknowledged++;
                if (session.TransactionActive) session.PendingWrites++;
                else ctx.TotalRecordsWritten++;
            }

            return Task.CompletedTask;
        }

        public Task CommitAsync(PipelineRunContext ctx, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var session = GetSession(ctx);
            if (session.TransactionActive)
            {
                var result = ((IRDBSource)session.Source).Commit(new PassedArgs());
                if (result?.Flag != Errors.Ok)
                    throw new PipelineWriteException($"Transaction commit failed: {result?.Message}", session.PendingWrites, false);
                session.TransactionActive = false;
                ctx.TotalRecordsWritten += session.PendingWrites;
                session.PendingWrites = 0;
            }
            ctx.RuntimeState.Remove(SessionKey);
            return Task.CompletedTask;
        }

        public Task RollbackAsync(PipelineRunContext ctx, CancellationToken token)
        {
            if (ctx.RuntimeState.TryGetValue(SessionKey, out var state) && state is SinkSession session)
            {
                if (session.TransactionActive)
                {
                    var result = ((IRDBSource)session.Source).EndTransaction(new PassedArgs());
                    if (result?.Flag != Errors.Ok)
                        throw new PipelineWriteException($"Transaction rollback failed: {result?.Message}", session.PendingWrites, false);
                }
                ctx.RuntimeState.Remove(SessionKey);
            }
            return Task.CompletedTask;
        }

        private SinkSession GetSession(PipelineRunContext ctx)
            => ctx.RuntimeState.TryGetValue(SessionKey, out var state) && state is SinkSession session
                ? session : throw new InvalidOperationException("BeginBatchAsync must complete before writing or committing.");

        // ── Helpers ───────────────────────────────────────────────────────

        private static ExpandoObject RecordToExpandoObject(PipelineRecord record)
        {
            var dict = (IDictionary<string, object?>)new ExpandoObject();
            for (int i = 0; i < record.Schema.Fields.Count; i++)
                dict[record.Schema.Fields[i].Name] = record.Values[i];
            return (ExpandoObject)dict;
        }
    }
}
