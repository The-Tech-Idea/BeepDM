using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Tracks and persists watermark values for CDC/incremental pipeline runs.
    /// Each schedule+pipeline pair maintains its own watermark state, enabling
    /// idempotent replay and deterministic window boundaries.
    /// Storage: {BeepDataPath}/Watermarks/{scheduleId}.watermark.json
    /// </summary>
    public sealed class WatermarkTracker
    {
        private readonly IDMEEditor _editor;
        private readonly string     _folder;

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented        = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public WatermarkTracker(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _folder = EnvironmentService.CreateAppfolder("Watermarks");
        }

        /// <summary>
        /// Load the current watermark state for a schedule.
        /// Returns null if no watermark has been saved yet (indicating a first run / full load).
        /// </summary>
        public async Task<WatermarkState?> LoadAsync(string scheduleId)
        {
            string path = WatermarkPath(scheduleId);
            if (!File.Exists(path)) return null;

            try
            {
                string text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                return JsonSerializer.Deserialize<WatermarkState>(text, _json);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(WatermarkTracker),
                    $"Failed to load watermark for schedule '{scheduleId}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Persist the watermark state after a successful incremental run.
        /// Writes atomically using a temp file rename.
        /// </summary>
        public async Task<IErrorsInfo> SaveAsync(string scheduleId, WatermarkState state)
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                state.UpdatedAtUtc = DateTime.UtcNow;
                string path = WatermarkPath(scheduleId);
                string text = JsonSerializer.Serialize(state, _json);
                string tmp  = path + ".tmp";
                await File.WriteAllTextAsync(tmp, text).ConfigureAwait(false);
                File.Move(tmp, path, overwrite: true);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(WatermarkTracker),
                    $"SaveAsync failed for schedule '{scheduleId}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                _editor.ErrorObject.Flag    = Errors.Failed;
                _editor.ErrorObject.Message = ex.Message;
            }
            return _editor.ErrorObject;
        }

        /// <summary>
        /// Compute the effective filter window for an incremental run.
        /// Applies the lookback overlap from <see cref="WatermarkConfig"/> to guard against late-arriving data.
        /// </summary>
        public WatermarkWindow ComputeWindow(WatermarkConfig config, WatermarkState? currentState)
        {
            string? fromValue = null;
            if (currentState?.LastWatermarkValue != null && config.LookbackSeconds > 0 &&
                config.WatermarkType == "datetime")
            {
                // Subtract lookback from the last watermark for overlap
                if (DateTime.TryParse(currentState.LastWatermarkValue, out var lastDt))
                {
                    var adjusted = lastDt.AddSeconds(-config.LookbackSeconds);
                    fromValue = adjusted.ToString("O");
                }
                else
                {
                    fromValue = currentState.LastWatermarkValue;
                }
            }
            else
            {
                fromValue = currentState?.LastWatermarkValue;
            }

            return new WatermarkWindow
            {
                WatermarkColumn = config.WatermarkColumn,
                WatermarkType   = config.WatermarkType,
                FromValue       = fromValue,
                ToValue         = null, // open-ended: process all records beyond FromValue
                IsFirstRun      = currentState == null || currentState.LastWatermarkValue == null
            };
        }

        /// <summary>Delete the watermark for a schedule (e.g. to force a full refresh).</summary>
        public Task<IErrorsInfo> ResetAsync(string scheduleId)
        {
            _editor.ErrorObject.Flag = Errors.Ok;
            try
            {
                string path = WatermarkPath(scheduleId);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(WatermarkTracker),
                    $"ResetAsync failed for schedule '{scheduleId}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                _editor.ErrorObject.Flag    = Errors.Failed;
                _editor.ErrorObject.Message = ex.Message;
            }
            return Task.FromResult(_editor.ErrorObject);
        }

        private string WatermarkPath(string scheduleId) =>
            Path.Combine(_folder, $"{scheduleId}.watermark.json");
    }

    /// <summary>
    /// Persisted state of the last successful watermark value for a schedule.
    /// </summary>
    public class WatermarkState
    {
        /// <summary>Schedule ID this watermark belongs to.</summary>
        public string ScheduleId { get; set; } = string.Empty;

        /// <summary>Pipeline ID this watermark belongs to.</summary>
        public string PipelineId { get; set; } = string.Empty;

        /// <summary>Serialized last-processed watermark value.</summary>
        public string? LastWatermarkValue { get; set; }

        /// <summary>Run ID of the run that last advanced the watermark.</summary>
        public string? LastRunId { get; set; }

        /// <summary>UTC time the watermark was last updated.</summary>
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Number of records processed in the last incremental run.</summary>
        public long LastRecordsProcessed { get; set; }
    }

    /// <summary>
    /// Computed filter window for an incremental run.
    /// Passed into the pipeline context as override parameters.
    /// </summary>
    public class WatermarkWindow
    {
        /// <summary>Column name to filter on.</summary>
        public string WatermarkColumn { get; set; } = string.Empty;

        /// <summary>Data type of the watermark ("datetime", "long", "string").</summary>
        public string WatermarkType { get; set; } = "datetime";

        /// <summary>Inclusive lower bound (null = no lower bound, i.e. first run).</summary>
        public string? FromValue { get; set; }

        /// <summary>Exclusive upper bound (null = open-ended, process all beyond FromValue).</summary>
        public string? ToValue { get; set; }

        /// <summary>True if this is the first incremental run (no prior watermark).</summary>
        public bool IsFirstRun { get; set; }
    }
}
