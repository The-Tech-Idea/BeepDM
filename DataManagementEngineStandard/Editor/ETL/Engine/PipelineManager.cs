using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Observability;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Persists, loads, and runs <see cref="PipelineDefinition"/> instances.
    /// Storage: {BeepDataPath}/Pipelines/{id}.pipeline.json
    /// Run history: {BeepDataPath}/Pipelines/History/{pipelineId}/{runId}.run.json
    /// </summary>
    public class PipelineManager
    {
        private readonly IDMEEditor     _editor;
        private readonly PipelineEngine _engine;
        private readonly string         _folder;
        private readonly string         _historyFolder;

        // ── Optional observability (set after construction) ─────────────
        public ObservabilityStore? ObservabilityStore
        {
            get => _engine.ObservabilityStore;
            set => _engine.ObservabilityStore = value;
        }
        public MetricsEngine? Metrics
        {
            get => _engine.Metrics;
            set => _engine.Metrics = value;
        }
        public AlertingEngine? Alerting
        {
            get => _engine.Alerting;
            set => _engine.Alerting = value;
        }
        public SecurityPolicyEngine? SecurityPolicy
        {
            get => _engine.SecurityPolicy;
            set => _engine.SecurityPolicy = value;
        }

        /// <summary>
        /// Caller identity for audit attribution. Set by the host application
        /// before calling Save/Delete/Run methods.
        /// </summary>
        public SecurityContext? SecurityContext { get; set; }

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented        = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public PipelineManager(IDMEEditor editor)
        {
            _editor        = editor ?? throw new ArgumentNullException(nameof(editor));
            _engine        = new PipelineEngine(editor);
            _folder        = EnvironmentService.CreateAppfolder("Pipelines");
            _historyFolder = EnvironmentService.CreateAppfolder("Pipelines", "History");
        }

        // ── CRUD ────────────────────────────────────────────────────────

        public async Task<IErrorsInfo> SaveAsync(PipelineDefinition pipeline)
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string path = PipelinePath(pipeline.Id);
                string json = JsonSerializer.Serialize(pipeline, _json);
                string tmp  = path + ".tmp";
                await File.WriteAllTextAsync(tmp, json);
                File.Move(tmp, path, overwrite: true);

                if (ObservabilityStore != null)
                    await ObservabilityStore.AppendAuditAsync(new AuditEntry
                    {
                        Action      = "PipelineConfigured",
                        EntityType  = "Pipeline",
                        EntityId    = pipeline.Id,
                        EntityName  = pipeline.Name,
                        PerformedBy = SecurityContext?.UserName ?? SecurityContext?.UserId,
                        IpAddress   = SecurityContext?.IpAddress
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(PipelineManager),
                    $"SaveAsync failed for '{pipeline.Name}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                _editor.ErrorObject.Flag    = Errors.Failed;
                _editor.ErrorObject.Message = ex.Message;
            }
            return _editor.ErrorObject;
        }

        public Task<IErrorsInfo> DeleteAsync(string pipelineId)
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string path = PipelinePath(pipelineId);
                if (File.Exists(path)) File.Delete(path);

                if (ObservabilityStore != null)
                    _ = ObservabilityStore.AppendAuditAsync(new AuditEntry
                    {
                        Action      = "PipelineDeleted",
                        EntityType  = "Pipeline",
                        EntityId    = pipelineId,
                        PerformedBy = SecurityContext?.UserName ?? SecurityContext?.UserId,
                        IpAddress   = SecurityContext?.IpAddress
                    });
            }
            catch (Exception ex)
            {
                _editor.ErrorObject.Flag    = Errors.Failed;
                _editor.ErrorObject.Message = ex.Message;
            }
            return Task.FromResult(_editor.ErrorObject);
        }

        public async Task<PipelineDefinition?> LoadAsync(string pipelineId)
        {
            string path = PipelinePath(pipelineId);
            if (!File.Exists(path)) return null;
            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<PipelineDefinition>(json, _json);
        }

        public async Task<IReadOnlyList<PipelineDefinition>> LoadAllAsync()
        {
            var result = new List<PipelineDefinition>();
            foreach (string file in Directory.EnumerateFiles(_folder, "*.pipeline.json"))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    var def     = JsonSerializer.Deserialize<PipelineDefinition>(json, _json);
                    if (def != null) result.Add(def);
                }
                catch { /* skip corrupt files */ }
            }
            return result;
        }

        public async Task<IReadOnlyList<PipelineDefinition>> FindByTagAsync(string tag)
        {
            var all = await LoadAllAsync();
            return all.Where(d =>
                d.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Any(t => t.Trim().Equals(tag, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // ── Execution ───────────────────────────────────────────────────

        public async Task<PipelineRunResult> RunAsync(
            string pipelineId,
            IProgress<PassedArgs>? progress = null,
            CancellationToken token = default)
        {
            var def = await LoadAsync(pipelineId)
                ?? throw new KeyNotFoundException($"Pipeline '{pipelineId}' not found.");
            return await RunDefinitionAsync(def, progress, token);
        }

        public async Task<PipelineRunResult> RunDefinitionAsync(
            PipelineDefinition def,
            IProgress<PassedArgs>? progress = null,
            CancellationToken token = default)
        {
            var result = await _engine.RunAsync(def, progress, token, securityContext: SecurityContext);

            // Update denormalised run summary on the definition
            def.LastRunAt     = result.FinishedAtUtc ?? DateTime.UtcNow;
            def.LastRunStatus = result.Status.ToString();
            def.LastRunId     = result.RunId;
            await SaveAsync(def);

            // Persist run result to history
            await PersistRunResultAsync(def.Id, result);

            // If canary or shadow mode is configured, execute the candidate in parallel
            if (!string.IsNullOrWhiteSpace(def.CandidatePipelineId)
                && (def.IsCanaryEnabled || def.IsShadowRunEnabled))
            {
                _ = RunCandidateSidecarAsync(def, result, progress, token);
            }

            return result;
        }

        /// <summary>
        /// Fires-and-forgets the candidate pipeline sidecar run (canary/shadow).
        /// Failures are swallowed so they never affect the baseline result.
        /// </summary>
        private async Task RunCandidateSidecarAsync(
            PipelineDefinition baselineDef,
            PipelineRunResult  baselineResult,
            IProgress<PassedArgs>? progress,
            CancellationToken token)
        {
            try
            {
                var candidateDef = await LoadAsync(baselineDef.CandidatePipelineId!)
                    .ConfigureAwait(false);
                if (candidateDef == null) return;

                var candidateResult = await _engine
                    .RunAsync(candidateDef, progress, token, securityContext: SecurityContext)
                    .ConfigureAwait(false);

                await PersistRunResultAsync(candidateDef.Id, candidateResult)
                    .ConfigureAwait(false);

                // Log comparison to observability if available
                if (ObservabilityStore != null)
                {
                    var action = baselineDef.IsShadowRunEnabled ? "ShadowRunCompleted" : "CanaryRunCompleted";
                    bool passed = candidateResult.Status == RunStatus.Succeeded
                               && candidateResult.RecordsRejected <= baselineResult.RecordsRejected * 1.05;

                    await ObservabilityStore.AppendAuditAsync(new AuditEntry
                    {
                        Action      = action,
                        EntityType  = "Pipeline",
                        EntityId    = baselineDef.Id,
                        EntityName  = $"{baselineDef.Name} vs {candidateDef.Name}",
                        NewValue    = $"Passed={passed} baseline={baselineResult.RunId} candidate={candidateResult.RunId}"
                    }).ConfigureAwait(false);
                }
            }
            catch
            {
                // Candidate sidecar failures must never propagate to the baseline caller
            }
        }

        public async Task<PipelineRunResult> ResumeAsync(
            string checkpointId,
            IProgress<PassedArgs>? progress = null,
            CancellationToken token = default)
        {
            var manager = new PipelineCheckpointManager(_editor);
            var cp      = await manager.LoadAsync(checkpointId)
                ?? throw new InvalidOperationException($"Checkpoint '{checkpointId}' not found.");

            var def = await LoadAsync(cp.PipelineId)
                ?? throw new InvalidOperationException($"Pipeline '{cp.PipelineId}' not found.");

            var result = await _engine.ResumeInternalAsync(def, cp, progress, token);

            def.LastRunAt     = result.FinishedAtUtc ?? DateTime.UtcNow;
            def.LastRunStatus = result.Status.ToString();
            def.LastRunId     = result.RunId;
            await SaveAsync(def);
            await PersistRunResultAsync(def.Id, result);

            return result;
        }

        // ── History ─────────────────────────────────────────────────────

        public async Task<IReadOnlyList<PipelineRunResult>> GetRunHistoryAsync(
            string pipelineId,
            int limit = 50)
        {
            string dir = Path.Combine(_historyFolder, pipelineId);
            if (!Directory.Exists(dir)) return Array.Empty<PipelineRunResult>();

            var results = new List<PipelineRunResult>();
            foreach (string file in Directory.EnumerateFiles(dir, "*.run.json")
                         .OrderByDescending(f => f)
                         .Take(limit))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    var r       = JsonSerializer.Deserialize<PipelineRunResult>(json, _json);
                    if (r != null) results.Add(r);
                }
                catch { /* skip corrupt */ }
            }
            return results;
        }

        // ── Private ─────────────────────────────────────────────────────

        private string PipelinePath(string id) =>
            Path.Combine(_folder, $"{id}.pipeline.json");

        private async Task PersistRunResultAsync(string pipelineId, PipelineRunResult result)
        {
            string dir = Path.Combine(_historyFolder, pipelineId);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"{result.RunId}.run.json");
            string json = JsonSerializer.Serialize(result, _json);
            await File.WriteAllTextAsync(path, json);
        }
    }
}
