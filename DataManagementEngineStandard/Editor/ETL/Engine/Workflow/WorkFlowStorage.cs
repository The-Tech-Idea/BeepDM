using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Workflow;

namespace TheTechIdea.Beep.Workflows.Engine
{
    /// <summary>
    /// Persists <see cref="WorkFlowDefinition"/> files and run results.
    /// Storage:
    ///   Definitions  — {BeepDataPath}/Workflows/{id}.workflow.json
    ///   Run results  — {BeepDataPath}/WorkflowRuns/{workflowId}/{runId}.run.json
    ///   Approvals    — {BeepDataPath}/WorkflowRuns/{workflowId}/{runId}.approval.{stepId}.json
    /// </summary>
    public class WorkFlowStorage
    {
        private readonly IDMEEditor _editor;
        private readonly string     _defFolder;
        private readonly string     _runsFolder;

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented        = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public WorkFlowStorage(IDMEEditor editor)
        {
            _editor     = editor ?? throw new ArgumentNullException(nameof(editor));
            _defFolder  = EnvironmentService.CreateAppfolder("Workflows");
            _runsFolder = EnvironmentService.CreateAppfolder("WorkflowRuns");
        }

        // ── Definition CRUD ───────────────────────────────────────────────────

        public async Task<IErrorsInfo> SaveDefinitionAsync(WorkFlowDefinition def)
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                def.ModifiedAtUtc = DateTime.UtcNow;
                string path = DefinitionPath(def.Id);
                string json = JsonSerializer.Serialize(def, _json);
                string tmp  = path + ".tmp";
                await File.WriteAllTextAsync(tmp, json);
                File.Move(tmp, path, overwrite: true);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(WorkFlowStorage),
                    $"SaveDefinitionAsync failed for '{def.Name}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                _editor.ErrorObject.Flag    = Errors.Failed;
                _editor.ErrorObject.Message = ex.Message;
            }
            return _editor.ErrorObject;
        }

        public async Task<WorkFlowDefinition?> LoadDefinitionAsync(string id)
        {
            string path = DefinitionPath(id);
            if (!File.Exists(path)) return null;
            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<WorkFlowDefinition>(json, _json);
        }

        public async Task<IReadOnlyList<WorkFlowDefinition>> LoadAllDefinitionsAsync()
        {
            var list = new List<WorkFlowDefinition>();
            foreach (string file in Directory.EnumerateFiles(_defFolder, "*.workflow.json"))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    var def     = JsonSerializer.Deserialize<WorkFlowDefinition>(json, _json);
                    if (def != null) list.Add(def);
                }
                catch { /* skip corrupt files */ }
            }
            return list;
        }

        public Task<IErrorsInfo> DeleteDefinitionAsync(string id)
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string path = DefinitionPath(id);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                _editor.ErrorObject.Flag    = Errors.Failed;
                _editor.ErrorObject.Message = ex.Message;
            }
            return Task.FromResult(_editor.ErrorObject);
        }

        // ── Run result persistence ────────────────────────────────────────────

        public async Task SaveRunResultAsync(WorkFlowRunResult result)
        {
            string dir  = RunFolder(result.WorkFlowId);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"{result.RunId}.run.json");
            string json = JsonSerializer.Serialize(result, _json);
            string tmp  = path + ".tmp";
            await File.WriteAllTextAsync(tmp, json);
            File.Move(tmp, path, overwrite: true);
        }

        public async Task<WorkFlowRunResult?> LoadRunResultAsync(string runId)
        {
            // Scan all workflow run folders for the matching runId
            foreach (string dir in Directory.EnumerateDirectories(_runsFolder))
            {
                string path = Path.Combine(dir, $"{runId}.run.json");
                if (!File.Exists(path)) continue;
                string json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<WorkFlowRunResult>(json, _json);
            }
            return null;
        }

        public async Task<IReadOnlyList<WorkFlowRunResult>> GetRunHistoryAsync(
            string workflowId, int limit = 50)
        {
            string dir = RunFolder(workflowId);
            if (!Directory.Exists(dir)) return Array.Empty<WorkFlowRunResult>();

            var runs = new List<WorkFlowRunResult>();
            foreach (string file in Directory.EnumerateFiles(dir, "*.run.json")
                                             .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                                             .Take(limit))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    var run     = JsonSerializer.Deserialize<WorkFlowRunResult>(json, _json);
                    if (run != null) runs.Add(run);
                }
                catch { /* skip corrupt files */ }
            }
            return runs;
        }

        // ── Approval state ────────────────────────────────────────────────────

        public async Task SaveApprovalStateAsync(string runId, string stepId, ApprovalState state)
        {
            string dir = RunFolderByRunId(runId);
            if (dir == null) return;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"{runId}.approval.{stepId}.json");
            string json = JsonSerializer.Serialize(state, _json);
            await File.WriteAllTextAsync(path, json);
        }

        public async Task<ApprovalState?> LoadApprovalStateAsync(string runId, string stepId)
        {
            string dir = RunFolderByRunId(runId);
            if (dir == null) return null;
            string path = Path.Combine(dir, $"{runId}.approval.{stepId}.json");
            if (!File.Exists(path)) return null;
            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<ApprovalState>(json, _json);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string DefinitionPath(string id)
            => Path.Combine(_defFolder, $"{id}.workflow.json");

        private string RunFolder(string workflowId)
            => Path.Combine(_runsFolder, workflowId);

        /// <summary>
        /// Finds the run folder that contains a run file with <paramref name="runId"/>.
        /// Returns the first match or null.
        /// </summary>
        private string? RunFolderByRunId(string runId)
        {
            foreach (string dir in Directory.EnumerateDirectories(_runsFolder))
            {
                if (File.Exists(Path.Combine(dir, $"{runId}.run.json")))
                    return dir;
            }
            return null;
        }
    }
}
