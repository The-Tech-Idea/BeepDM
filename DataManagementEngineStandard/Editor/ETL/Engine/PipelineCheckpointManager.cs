using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Persists pipeline run checkpoints to disk so that an interrupted run
    /// can be resumed from the last committed batch.
    ///
    /// Storage location: {BeepDataPath}/Pipelines/Checkpoints/{runId}.chk.json
    /// </summary>
    public class PipelineCheckpointManager
    {
        private readonly IDMEEditor _editor;
        private readonly string     _folder;

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented    = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public PipelineCheckpointManager(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _folder = EnvironmentService.CreateAppfolder("Pipelines", "Checkpoints");
        }

        // ── Write ────────────────────────────────────────────────────────

        /// <summary>
        /// Snapshot current run progress to a checkpoint file.
        /// Called by the engine after each committed batch.
        /// </summary>
        public async Task SaveAsync(
            PipelineRunContext ctx,
            string lastCommittedStepId,
            long   batchOffset)
        {
            var cp = new PipelineCheckpoint
            {
                RunId                 = ctx.RunId,
                PipelineId            = ctx.PipelineId,
                PipelineName          = ctx.PipelineName,
                LastCommittedStepId   = lastCommittedStepId,
                LastCommittedOffset   = batchOffset,
                TotalRecordsWritten   = ctx.TotalRecordsWritten,
                CreatedAt             = DateTime.UtcNow,
                Status                = "Running"
            };

            await WriteCheckpointAsync(cp);
        }

        /// <summary>Mark the checkpoint file as completed so it is excluded from pending list.</summary>
        public async Task CompleteAsync(string runId)
        {
            var cp = await LoadAsync(runId);
            if (cp == null) return;
            cp.Status = "Complete";
            await WriteCheckpointAsync(cp);
        }

        // ── Read ─────────────────────────────────────────────────────────

        /// <summary>Load a checkpoint by run ID, or null if none exists.</summary>
        public async Task<PipelineCheckpoint?> LoadAsync(string runId)
        {
            string path = Path.Combine(_folder, $"{runId}.chk.json");
            if (!File.Exists(path)) return null;

            try
            {
                string json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<PipelineCheckpoint>(json, _json);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(PipelineCheckpointManager),
                    $"Failed to load checkpoint {runId}: {ex.Message}",
                    DateTime.Now, -1, null, ConfigUtil.Errors.Failed);
                return null;
            }
        }

        /// <summary>Returns all checkpoints that are not yet complete (resumable runs).</summary>
        public async Task<IReadOnlyList<PipelineCheckpoint>> ListPendingAsync()
        {
            var result = new List<PipelineCheckpoint>();
            foreach (string file in Directory.EnumerateFiles(_folder, "*.chk.json"))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    var   cp   = JsonSerializer.Deserialize<PipelineCheckpoint>(json, _json);
                    if (cp != null && cp.Status != "Complete")
                        result.Add(cp);
                }
                catch { /* skip corrupt files */ }
            }
            return result;
        }

        /// <summary>Delete the checkpoint file for a run (cleanup after success).</summary>
        public Task DeleteAsync(string runId)
        {
            string path = Path.Combine(_folder, $"{runId}.chk.json");
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }

        // ── Private ───────────────────────────────────────────────────────

        private async Task WriteCheckpointAsync(PipelineCheckpoint cp)
        {
            string path = Path.Combine(_folder, $"{cp.RunId}.chk.json");
            string json = JsonSerializer.Serialize(cp, _json);
            // Write atomically via temp file
            string tmp = path + ".tmp";
            await File.WriteAllTextAsync(tmp, json);
            File.Move(tmp, path, overwrite: true);
        }
    }

    /// <summary>Data contract stored in each checkpoint file.</summary>
    public class PipelineCheckpoint
    {
        public string   RunId                 { get; set; } = string.Empty;
        public string   PipelineId            { get; set; } = string.Empty;
        public string   PipelineName          { get; set; } = string.Empty;
        public string   LastCommittedStepId   { get; set; } = string.Empty;
        public long     LastCommittedOffset   { get; set; }
        public long     TotalRecordsWritten   { get; set; }
        public DateTime CreatedAt             { get; set; } = DateTime.UtcNow;
        /// <summary>"Running" | "Complete"</summary>
        public string   Status                { get; set; } = "Running";
    }
}
