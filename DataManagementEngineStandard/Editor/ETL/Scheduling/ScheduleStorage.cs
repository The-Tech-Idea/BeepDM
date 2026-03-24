using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Observability;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Persists <see cref="ScheduleDefinition"/> instances as JSON files.
    /// Storage: {BeepDataPath}/Schedules/{id}.schedule.json
    /// </summary>
    public class ScheduleStorage
    {
        private readonly IDMEEditor _editor;
        private readonly string     _folder;

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented        = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ScheduleStorage(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _folder = EnvironmentService.CreateAppfolder("Schedules");
        }

        /// <summary>Optional observability store for audit logging.</summary>
        public ObservabilityStore? ObservabilityStore { get; set; }

        public async Task<IErrorsInfo> SaveAsync(ScheduleDefinition def)
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                def.ModifiedAtUtc = DateTime.UtcNow;
                string path = SchedulePath(def.Id);
                string text = JsonSerializer.Serialize(def, _json);
                string tmp  = path + ".tmp";
                await File.WriteAllTextAsync(tmp, text).ConfigureAwait(false);
                File.Move(tmp, path, overwrite: true);

                if (ObservabilityStore != null)
                    await ObservabilityStore.AppendAuditAsync(new AuditEntry
                    {
                        Action     = "ScheduleConfigured",
                        EntityType = "Schedule",
                        EntityId   = def.Id,
                        EntityName = def.Name
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(ScheduleStorage),
                    $"SaveAsync failed for '{def.Name}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                _editor.ErrorObject.Flag    = Errors.Failed;
                _editor.ErrorObject.Message = ex.Message;
            }
            return _editor.ErrorObject;
        }

        public async Task<IReadOnlyList<ScheduleDefinition>> LoadAllAsync()
        {
            var result = new List<ScheduleDefinition>();
            try
            {
                foreach (var file in Directory.GetFiles(_folder, "*.schedule.json"))
                {
                    try
                    {
                        string text = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                        var def = JsonSerializer.Deserialize<ScheduleDefinition>(text, _json);
                        if (def != null) result.Add(def);
                    }
                    catch { /* skip corrupt files */ }
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(ScheduleStorage),
                    $"LoadAllAsync failed: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
        }

        public async Task<ScheduleDefinition?> LoadAsync(string id)
        {
            string path = SchedulePath(id);
            if (!File.Exists(path)) return null;
            string text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return JsonSerializer.Deserialize<ScheduleDefinition>(text, _json);
        }

        public Task<IErrorsInfo> DeleteAsync(string id)
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string path = SchedulePath(id);
                if (File.Exists(path)) File.Delete(path);

                if (ObservabilityStore != null)
                    _ = ObservabilityStore.AppendAuditAsync(new AuditEntry
                    {
                        Action     = "ScheduleDeleted",
                        EntityType = "Schedule",
                        EntityId   = id
                    });
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(ScheduleStorage),
                    $"DeleteAsync failed for '{id}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                _editor.ErrorObject.Flag    = Errors.Failed;
                _editor.ErrorObject.Message = ex.Message;
            }
            return Task.FromResult(_editor.ErrorObject);
        }

        private string SchedulePath(string id) =>
            Path.Combine(_folder, $"{id}.schedule.json");
    }
}
