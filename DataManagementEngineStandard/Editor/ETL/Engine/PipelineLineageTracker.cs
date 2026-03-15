using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Accumulates column-level <see cref="DataLineageRecord"/> entries during a run
    /// and persists them to {BeepDataPath}/Pipelines/Lineage/{runId}.lineage.json at completion.
    /// </summary>
    public class PipelineLineageTracker
    {
        private readonly IDMEEditor _editor;
        private readonly string     _folder;

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented    = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public PipelineLineageTracker(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _folder = EnvironmentService.CreateAppfolder("Pipelines", "Lineage");
        }

        /// <summary>
        /// Record that <paramref name="sourceField"/> from <paramref name="sourceDs"/>/<paramref name="sourceEntity"/>
        /// was written to <paramref name="destField"/> in <paramref name="destDs"/>/<paramref name="destEntity"/>.
        /// </summary>
        public void Track(
            PipelineRunContext ctx,
            string stepId,
            string stepName,
            string sourceDs,
            string sourceEntity,
            string sourceField,
            string destDs,
            string destEntity,
            string destField,
            string transformExpression = "")
        {
            ctx.LineageEntries.Add(new DataLineageRecord
            {
                RunId              = ctx.RunId,
                StepId             = stepId,
                StepName           = stepName,
                SourceDataSource   = sourceDs,
                SourceEntity       = sourceEntity,
                SourceField        = sourceField,
                DestDataSource     = destDs,
                DestEntity         = destEntity,
                DestField          = destField,
                TransformExpression = transformExpression,
                Timestamp          = DateTime.UtcNow
            });
        }

        /// <summary>Persist all lineage entries accumulated on <paramref name="ctx"/> to disk.</summary>
        public async Task FlushAsync(PipelineRunContext ctx)
        {
            if (ctx.LineageEntries.Count == 0) return;

            string path = Path.Combine(_folder, $"{ctx.RunId}.lineage.json");
            string json = JsonSerializer.Serialize(ctx.LineageEntries, _json);
            await File.WriteAllTextAsync(path, json);
        }

        /// <summary>Load lineage entries for a past run.</summary>
        public async Task<IReadOnlyList<DataLineageRecord>> LoadAsync(string runId)
        {
            string path = Path.Combine(_folder, $"{runId}.lineage.json");
            if (!File.Exists(path)) return Array.Empty<DataLineageRecord>();

            try
            {
                string json = await File.ReadAllTextAsync(path);
                return (IReadOnlyList<DataLineageRecord>?)
                           JsonSerializer.Deserialize<List<DataLineageRecord>>(json, _json)
                       ?? Array.Empty<DataLineageRecord>();
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(PipelineLineageTracker),
                    $"Failed to load lineage for run {runId}: {ex.Message}",
                    DateTime.Now, -1, null, ConfigUtil.Errors.Failed);
                return Array.Empty<DataLineageRecord>();
            }
        }
    }
}
