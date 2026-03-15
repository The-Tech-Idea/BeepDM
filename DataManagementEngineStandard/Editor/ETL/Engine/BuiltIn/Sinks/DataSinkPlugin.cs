using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() =>
            new[]
            {
                new PipelineParameterDef { Name = "DataSourceName", Type = ParamType.String, IsRequired = true,  Description = "Registered connection name" },
                new PipelineParameterDef { Name = "EntityName",     Type = ParamType.String, IsRequired = true,  Description = "Target table or entity" },
                new PipelineParameterDef { Name = "WriteMode",      Type = ParamType.String, IsRequired = false, Description = "Insert | Update | Upsert (default: Insert)" }
            };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("DataSourceName", out var ds)) _dataSourceName = ds?.ToString() ?? string.Empty;
            if (parameters.TryGetValue("EntityName",     out var en)) _entityName     = en?.ToString() ?? string.Empty;
            if (parameters.TryGetValue("WriteMode",      out var wm)) _writeMode      = wm?.ToString() ?? "Insert";
        }

        // ── IPipelineSink ─────────────────────────────────────────────────

        public Task BeginBatchAsync(PipelineRunContext ctx, PipelineSchema schema, CancellationToken token)
        {
            var ds = ctx.DMEEditor.GetDataSource(_dataSourceName)
                     ?? throw new InvalidOperationException($"Data source '{_dataSourceName}' not found.");

            if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                ds.Openconnection();

            return Task.CompletedTask;
        }

        public Task WriteBatchAsync(
            IReadOnlyList<PipelineRecord> batch,
            PipelineRunContext ctx,
            CancellationToken token)
        {
            var ds = ctx.DMEEditor.GetDataSource(_dataSourceName)
                     ?? throw new InvalidOperationException($"Data source '{_dataSourceName}' not found.");

            foreach (var record in batch)
            {
                token.ThrowIfCancellationRequested();
                var obj = RecordToExpandoObject(record);

                switch (_writeMode.ToUpperInvariant())
                {
                    case "UPDATE":
                        ds.UpdateEntity(_entityName, obj);
                        break;
                    case "UPSERT":
                        // Try update; if it reports a failure, fall back to insert
                        var updateResult = ds.UpdateEntity(_entityName, obj);
                        if (updateResult?.Flag == ConfigUtil.Errors.Failed)
                            ds.InsertEntity(_entityName, obj);
                        break;
                    default: // INSERT
                        ds.InsertEntity(_entityName, obj);
                        break;
                }

                ctx.TotalRecordsWritten++;
            }

            return Task.CompletedTask;
        }

        public Task CommitAsync(PipelineRunContext ctx, CancellationToken token)
            => Task.CompletedTask; // transaction commit handled by IDataSource internally

        public Task RollbackAsync(PipelineRunContext ctx, CancellationToken token)
            => Task.CompletedTask;

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
