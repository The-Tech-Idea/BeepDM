using System;
using System.Collections.Generic;
using System.IO;
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
    /// Built-in source plugin that reads records from a CSV file via the BeepDM
    /// CSV data source registered under the given connection name.
    /// Parameters:
    ///   FilePath        (string, required) — absolute path to the CSV file.
    ///   ConnectionName  (string, optional) — if a CSV data source is already registered, use it.
    ///   HasHeader       (bool,   optional, default true).
    ///   Delimiter       (string, optional, default ",").
    /// </summary>
    [PipelinePlugin(
        "beep.source.csv",
        "CSV File Source",
        PipelinePluginType.Source,
        Category = "File",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class CsvSourcePlugin : IPipelineSource
    {
        // ── IPipelinePlugin ───────────────────────────────────────────────

        public string PluginId   => "beep.source.csv";
        public string DisplayName => "CSV File Source";
        public string Description => "Reads records from a CSV file using the BeepDM CSV data source.";

        private string _filePath       = string.Empty;
        private string _connectionName = string.Empty;
        private bool   _hasHeader      = true;
        private string _delimiter      = ",";

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "FilePath",       Type = ParamType.String,  IsRequired = false, Description = "Absolute path to the CSV file" },
            new PipelineParameterDef { Name = "ConnectionName", Type = ParamType.String,  IsRequired = false, Description = "Registered CSV data source name" },
            new PipelineParameterDef { Name = "HasHeader",      Type = ParamType.Boolean, IsRequired = false, Description = "Whether the first row is a header (default true)" },
            new PipelineParameterDef { Name = "Delimiter",      Type = ParamType.String,  IsRequired = false, Description = "Field delimiter (default ',')" }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("FilePath",       out var fp)) _filePath       = fp?.ToString() ?? string.Empty;
            if (parameters.TryGetValue("ConnectionName", out var cn)) _connectionName = cn?.ToString() ?? string.Empty;
            if (parameters.TryGetValue("HasHeader",      out var hh)) _hasHeader      = hh is bool b ? b : !string.Equals(hh?.ToString(), "false", StringComparison.OrdinalIgnoreCase);
            if (parameters.TryGetValue("Delimiter",      out var dl)) _delimiter      = dl?.ToString() ?? ",";
        }

        // ── IPipelineSource ───────────────────────────────────────────────

        public Task<PipelineSchema> GetSchemaAsync(PipelineRunContext ctx, CancellationToken token)
        {
            var ds = ResolveDataSource(ctx)
                     ?? throw new InvalidOperationException("CSV data source could not be resolved.");

            var entityName = Path.GetFileNameWithoutExtension(_filePath);
            var entity     = ds.GetEntityStructure(entityName, false);

            if (entity == null)
                throw new InvalidOperationException($"Could not retrieve schema for '{entityName}' from CSV source.");

            return Task.FromResult(PipelineSchema.FromEntityStructure(entity));
        }

        public async IAsyncEnumerable<PipelineRecord> ReadAsync(
            PipelineRunContext ctx,
            [EnumeratorCancellation] CancellationToken token)
        {
            var ds = ResolveDataSource(ctx)
                     ?? throw new InvalidOperationException("CSV data source could not be resolved.");

            if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                ds.Openconnection();

            var entityName = Path.GetFileNameWithoutExtension(_filePath);
            var schema     = await GetSchemaAsync(ctx, token);
            var rows       = ds.GetEntity(entityName, new List<AppFilter>());

            foreach (var row in rows)
            {
                token.ThrowIfCancellationRequested();
                var record = new PipelineRecord(schema);
                MapRowToRecord(row, schema, record);
                yield return record;
            }
        }

        public Task<long> GetEstimatedRowCountAsync(PipelineRunContext ctx, CancellationToken token)
        {
            if (!File.Exists(_filePath)) return Task.FromResult(-1L);
            try
            {
                long count = 0;
                using var reader = File.OpenText(_filePath);
                while (reader.ReadLine() != null) count++;
                return Task.FromResult(_hasHeader ? Math.Max(0, count - 1) : count);
            }
            catch
            {
                return Task.FromResult(-1L);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private IDataSource? ResolveDataSource(PipelineRunContext ctx)
        {
            // Prefer an explicitly registered connection name
            if (!string.IsNullOrWhiteSpace(_connectionName))
                return ctx.DMEEditor.GetDataSource(_connectionName);

            // Look for a CSV source that has been registered against this file path
            if (!string.IsNullOrWhiteSpace(_filePath))
            {
                var name = Path.GetFileNameWithoutExtension(_filePath);
                return ctx.DMEEditor.GetDataSource(name);
            }

            return null;
        }

        private static void MapRowToRecord(object row, PipelineSchema schema, PipelineRecord record)
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
                var prop = System.Array.Find(props, p => string.Equals(p.Name, f.Name, StringComparison.OrdinalIgnoreCase));
                record[f.Name] = prop?.GetValue(row);
            }
        }
    }
}
