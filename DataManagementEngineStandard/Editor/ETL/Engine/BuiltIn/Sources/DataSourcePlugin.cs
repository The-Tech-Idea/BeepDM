using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Sources
{
    /// <summary>
    /// Built-in source plugin that reads from any <c>IDataSource</c> registered with BeepDM.
    /// Parameters:
    ///   DataSourceName  (string, required) — registered connection name.
    ///   EntityName      (string, required) — table / entity name.
    ///   Filter          (string, optional) — semicolon-delimited "Field:Op:Value" triples.
    ///   BatchSize       (int,    optional, default 1000).
    /// </summary>
    [PipelinePlugin(
        "beep.source.datasource",
        "BeepDM Data Source",
        PipelinePluginType.Source,
        Category = "Database",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class DataSourcePlugin : IPipelineSource
    {
        // ── IPipelinePlugin ───────────────────────────────────────────────

        public string PluginId      => "beep.source.datasource";
        public string DisplayName   => "BeepDM Data Source";
        public string Description   => "Reads records from any IDataSource registered in BeepDM.";

        private string _dataSourceName = string.Empty;
        private string _entityName     = string.Empty;
        private List<AppFilter> _filters  = new();
        private int    _batchSize       = 1000;

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() =>
            new[]
            {
                new PipelineParameterDef { Name = "DataSourceName", Type = ParamType.String,  IsRequired = true,  Description = "Registered connection name" },
                new PipelineParameterDef { Name = "EntityName",     Type = ParamType.String,  IsRequired = true,  Description = "Table or entity to read" },
                new PipelineParameterDef { Name = "Filter",         Type = ParamType.String,  IsRequired = false, Description = "Optional filter as 'Field:Op:Value;...'" },
                new PipelineParameterDef { Name = "BatchSize",      Type = ParamType.Integer, IsRequired = false, Description = "Streaming batch size (default 1000)" }
            };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("DataSourceName", out var ds)) _dataSourceName = ds?.ToString() ?? string.Empty;
            if (parameters.TryGetValue("EntityName",     out var en)) _entityName     = en?.ToString() ?? string.Empty;
            if (parameters.TryGetValue("BatchSize",      out var bs) && int.TryParse(bs?.ToString(), out var bsi)) _batchSize = bsi;
            if (parameters.TryGetValue("Filter",         out var flt) && flt is string fltStr && !string.IsNullOrWhiteSpace(fltStr))
                _filters = ParseFilters(fltStr);
        }

        // ── IPipelineSource ───────────────────────────────────────────────

        public Task<PipelineSchema> GetSchemaAsync(PipelineRunContext ctx, CancellationToken token)
        {
            var ds = ctx.DMEEditor.GetDataSource(_dataSourceName)
                     ?? throw new InvalidOperationException($"Data source '{_dataSourceName}' not found.");
            var entity = ds.GetEntityStructure(_entityName, false)
                         ?? throw new InvalidOperationException($"Entity '{_entityName}' not found in '{_dataSourceName}'.");
            return Task.FromResult(PipelineSchema.FromEntityStructure(entity));
        }

        public async IAsyncEnumerable<PipelineRecord> ReadAsync(
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token)
        {
            var ds = ctx.DMEEditor.GetDataSource(_dataSourceName)
                     ?? throw new InvalidOperationException($"Data source '{_dataSourceName}' not found.");

            if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                ds.Openconnection();

            var schema = await GetSchemaAsync(ctx, token);
            var rows   = ds.GetEntity(_entityName, _filters);

            foreach (var row in rows)
            {
                token.ThrowIfCancellationRequested();
                var record = new PipelineRecord(schema);
                MapObjectToRecord(row, schema, record);
                yield return record;
            }
        }

        public Task<long> GetEstimatedRowCountAsync(PipelineRunContext ctx, CancellationToken token)
            => Task.FromResult(-1L); // IDataSource doesn't expose a cheap COUNT

        // ── Helpers ───────────────────────────────────────────────────────

        private static void MapObjectToRecord(object row, PipelineSchema schema, PipelineRecord record)
        {
            if (row is IDictionary<string, object> dict)
            {
                foreach (var f in schema.Fields)
                    record[f.Name] = dict.TryGetValue(f.Name, out var v) ? v : null;
                return;
            }

            var props = row.GetType().GetProperties();
            foreach (var f in schema.Fields)
            {
                var prop = props.FirstOrDefault(p => string.Equals(p.Name, f.Name, StringComparison.OrdinalIgnoreCase));
                record[f.Name] = prop?.GetValue(row);
            }
        }

        private static List<AppFilter> ParseFilters(string filterStr)
        {
            var list = new List<AppFilter>();
            foreach (var part in filterStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var tokens = part.Split(':', 3);
                if (tokens.Length != 3) continue;
                list.Add(new AppFilter { FieldName = tokens[0].Trim(), Operator = tokens[1].Trim(), FilterValue = tokens[2].Trim() });
            }
            return list;
        }
    }
}
