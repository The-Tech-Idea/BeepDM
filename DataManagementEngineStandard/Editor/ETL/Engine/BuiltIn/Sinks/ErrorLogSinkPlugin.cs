using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Sinks
{
    /// <summary>
    /// Built-in sink that writes rejected / invalid records to a newline-delimited JSON file
    /// at {BeepDataPath}/Pipelines/Errors/{runId}.errors.jsonl.
    /// Intended to be attached to the error outlet of the validation step.
    /// Parameters: none required.  Optional — MaxErrors (int, default 10 000).
    /// </summary>
    [PipelinePlugin(
        "beep.sink.errorlog",
        "Error Log Sink",
        PipelinePluginType.Sink,
        Category = "System",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class ErrorLogSinkPlugin : IPipelineSink
    {
        // ── IPipelinePlugin ───────────────────────────────────────────────

        public string PluginId    => "beep.sink.errorlog";
        public string DisplayName => "Error Log Sink";
        public string Description => "Writes rejected records to a JSONL error log file.";

        private int _maxErrors = 10_000;
        private int _errorCount;
        private StreamWriter? _writer;
        private string _filePath = string.Empty;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef
            {
                Name        = "MaxErrors",
                Type        = ParamType.Integer,
                IsRequired  = false,
                Description = "Maximum number of error records to persist (default 10 000)"
            }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("MaxErrors", out var mx) && int.TryParse(mx?.ToString(), out var mxi))
                _maxErrors = mxi;
        }

        // ── IPipelineSink ─────────────────────────────────────────────────

        public Task BeginBatchAsync(PipelineRunContext ctx, PipelineSchema schema, CancellationToken token)
        {
            string folder = EnvironmentService.CreateAppfolder("Pipelines", "Errors");
            _filePath = Path.Combine(folder, $"{ctx.RunId}.errors.jsonl");
            _writer   = new StreamWriter(_filePath, append: false, encoding: Encoding.UTF8);
            _errorCount = 0;
            return Task.CompletedTask;
        }

        public Task WriteBatchAsync(
            IReadOnlyList<PipelineRecord> batch,
            PipelineRunContext ctx,
            CancellationToken token)
        {
            if (_writer == null) return Task.CompletedTask;

            foreach (var record in batch)
            {
                if (_errorCount >= _maxErrors) break;
                token.ThrowIfCancellationRequested();

                var dict = BuildDict(record);
                string line = JsonSerializer.Serialize(dict, _json);
                _writer.WriteLine(line);
                _errorCount++;
                ctx.TotalRecordsRejected++;
            }

            return Task.CompletedTask;
        }

        public async Task CommitAsync(PipelineRunContext ctx, CancellationToken token)
        {
            if (_writer != null)
            {
                await _writer.FlushAsync();
                _writer.Dispose();
                _writer = null;
            }
        }

        public Task RollbackAsync(PipelineRunContext ctx, CancellationToken token)
        {
            _writer?.Dispose();
            _writer = null;
            // Delete partial error file on rollback
            if (File.Exists(_filePath))
                try { File.Delete(_filePath); } catch { /* best-effort */ }
            return Task.CompletedTask;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static Dictionary<string, object?> BuildDict(PipelineRecord record)
        {
            var dict = new Dictionary<string, object?>(record.Schema.Fields.Count + 1);
            for (int i = 0; i < record.Schema.Fields.Count; i++)
                dict[record.Schema.Fields[i].Name] = record.Values[i];

            // Attach validation message if present in Meta
            if (record.Meta.TryGetValue(PipelineRecordMeta.ValidationMessage, out var msg))
                dict["_validationMessage"] = msg;

            return dict;
        }
    }
}
