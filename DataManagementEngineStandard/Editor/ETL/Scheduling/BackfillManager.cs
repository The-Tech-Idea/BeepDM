using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Manages deterministic replay and backfill operations for scheduled pipelines.
    /// Backfill runs are isolated by workload class ("backfill") to avoid impacting live runs.
    /// Each backfill request is persisted for auditability and idempotency.
    /// Storage: {BeepDataPath}/Backfills/{id}.backfill.json
    /// </summary>
    public sealed class BackfillManager
    {
        private readonly IDMEEditor       _editor;
        private readonly WatermarkTracker _watermarks;
        private readonly string           _folder;

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented        = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public BackfillManager(IDMEEditor editor, WatermarkTracker watermarks)
        {
            _editor     = editor ?? throw new ArgumentNullException(nameof(editor));
            _watermarks = watermarks ?? throw new ArgumentNullException(nameof(watermarks));
            _folder     = EnvironmentService.CreateAppfolder("Backfills");
        }

        /// <summary>
        /// Create a backfill request that will be queued as a series of windowed runs.
        /// The request is persisted and can be resumed if interrupted.
        /// </summary>
        public async Task<BackfillRequest> CreateRequestAsync(
            string scheduleId,
            string pipelineId,
            string fromValue,
            string toValue,
            string watermarkType = "datetime",
            string watermarkColumn = "",
            string reason = "",
            string requestedBy = "",
            int windowSizeSeconds = 86400)
        {
            var request = new BackfillRequest
            {
                ScheduleId      = scheduleId,
                PipelineId      = pipelineId,
                FromValue       = fromValue,
                ToValue         = toValue,
                WatermarkType   = watermarkType,
                WatermarkColumn = watermarkColumn,
                Reason          = reason,
                RequestedBy     = requestedBy,
                WindowSizeSeconds = windowSizeSeconds
            };

            // Compute windows for datetime-based watermarks
            if (watermarkType == "datetime" &&
                DateTime.TryParse(fromValue, out var fromDt) &&
                DateTime.TryParse(toValue, out var toDt))
            {
                var current = fromDt;
                while (current < toDt)
                {
                    var windowEnd = current.AddSeconds(windowSizeSeconds);
                    if (windowEnd > toDt) windowEnd = toDt;

                    request.Windows.Add(new BackfillWindow
                    {
                        FromValue = current.ToString("O"),
                        ToValue   = windowEnd.ToString("O"),
                        Status    = BackfillWindowStatus.Pending
                    });

                    current = windowEnd;
                }
            }
            else
            {
                // Non-datetime: single window covering the full range
                request.Windows.Add(new BackfillWindow
                {
                    FromValue = fromValue,
                    ToValue   = toValue,
                    Status    = BackfillWindowStatus.Pending
                });
            }

            await SaveRequestAsync(request).ConfigureAwait(false);

            _editor.AddLogMessage(nameof(BackfillManager),
                $"Created backfill request '{request.Id}' for schedule '{scheduleId}': " +
                $"{request.Windows.Count} window(s) from {fromValue} to {toValue}.",
                DateTime.Now, -1, null, Errors.Ok);

            return request;
        }

        /// <summary>
        /// Get the next pending window from a backfill request.
        /// Returns null if all windows are completed or the request is cancelled.
        /// </summary>
        public BackfillWindow? GetNextPendingWindow(BackfillRequest request)
        {
            if (request.Status == BackfillRequestStatus.Cancelled) return null;

            foreach (var window in request.Windows)
            {
                if (window.Status == BackfillWindowStatus.Pending)
                    return window;
            }
            return null;
        }

        /// <summary>
        /// Build override parameters for a backfill window to pass to the pipeline engine.
        /// </summary>
        public Dictionary<string, object> BuildWindowOverrides(BackfillRequest request, BackfillWindow window)
        {
            return new Dictionary<string, object>
            {
                ["__backfill_id"]       = request.Id,
                ["__backfill_from"]     = window.FromValue,
                ["__backfill_to"]       = window.ToValue,
                ["__backfill_column"]   = request.WatermarkColumn,
                ["__backfill_type"]     = request.WatermarkType,
                ["__is_backfill"]       = true,
                ["__workload_class"]    = "backfill"
            };
        }

        /// <summary>
        /// Mark a window as completed after a successful run.
        /// </summary>
        public async Task CompleteWindowAsync(BackfillRequest request, BackfillWindow window,
            string runId, long recordsProcessed)
        {
            window.Status           = BackfillWindowStatus.Completed;
            window.RunId            = runId;
            window.RecordsProcessed = recordsProcessed;
            window.CompletedAtUtc   = DateTime.UtcNow;

            // Check if all windows are completed
            bool allDone = true;
            foreach (var w in request.Windows)
            {
                if (w.Status == BackfillWindowStatus.Pending ||
                    w.Status == BackfillWindowStatus.Running)
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone)
            {
                request.Status         = BackfillRequestStatus.Completed;
                request.CompletedAtUtc = DateTime.UtcNow;
            }

            await SaveRequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Mark a window as failed after an unsuccessful run.
        /// </summary>
        public async Task FailWindowAsync(BackfillRequest request, BackfillWindow window,
            string runId, string error)
        {
            window.Status         = BackfillWindowStatus.Failed;
            window.RunId          = runId;
            window.Error          = error;
            window.CompletedAtUtc = DateTime.UtcNow;

            request.Status = BackfillRequestStatus.PartialFailure;
            await SaveRequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>Cancel a backfill request. Pending windows will not be executed.</summary>
        public async Task CancelAsync(BackfillRequest request)
        {
            request.Status = BackfillRequestStatus.Cancelled;
            foreach (var w in request.Windows)
            {
                if (w.Status == BackfillWindowStatus.Pending)
                    w.Status = BackfillWindowStatus.Skipped;
            }
            await SaveRequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>Load a backfill request by ID.</summary>
        public async Task<BackfillRequest?> LoadRequestAsync(string requestId)
        {
            string path = RequestPath(requestId);
            if (!File.Exists(path)) return null;

            try
            {
                string text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                return JsonSerializer.Deserialize<BackfillRequest>(text, _json);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(BackfillManager),
                    $"Failed to load backfill request '{requestId}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>Load all backfill requests.</summary>
        public async Task<IReadOnlyList<BackfillRequest>> LoadAllAsync()
        {
            var result = new List<BackfillRequest>();
            try
            {
                foreach (var file in Directory.GetFiles(_folder, "*.backfill.json"))
                {
                    try
                    {
                        string text = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                        var req = JsonSerializer.Deserialize<BackfillRequest>(text, _json);
                        if (req != null) result.Add(req);
                    }
                    catch { /* skip corrupt files */ }
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(BackfillManager),
                    $"LoadAllAsync failed: {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
        }

        private async Task SaveRequestAsync(BackfillRequest request)
        {
            request.ModifiedAtUtc = DateTime.UtcNow;
            string path = RequestPath(request.Id);
            string text = JsonSerializer.Serialize(request, _json);
            string tmp  = path + ".tmp";
            await File.WriteAllTextAsync(tmp, text).ConfigureAwait(false);
            File.Move(tmp, path, overwrite: true);
        }

        private string RequestPath(string id) =>
            Path.Combine(_folder, $"{id}.backfill.json");
    }

    // ── Backfill Models ────────────────────────────────────────────────────────

    /// <summary>
    /// A request to re-process historical data over a defined range.
    /// Contains one or more time-windowed sub-runs for controlled execution.
    /// </summary>
    public class BackfillRequest
    {
        public string Id            { get; set; } = Guid.NewGuid().ToString();
        public string ScheduleId    { get; set; } = string.Empty;
        public string PipelineId    { get; set; } = string.Empty;
        public string FromValue     { get; set; } = string.Empty;
        public string ToValue       { get; set; } = string.Empty;
        public string WatermarkType { get; set; } = "datetime";
        public string WatermarkColumn { get; set; } = string.Empty;
        public string Reason        { get; set; } = string.Empty;
        public string RequestedBy   { get; set; } = string.Empty;
        public int    WindowSizeSeconds { get; set; } = 86400;

        public BackfillRequestStatus Status { get; set; } = BackfillRequestStatus.Pending;

        public List<BackfillWindow> Windows { get; set; } = new();

        public DateTime CreatedAtUtc   { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAtUtc  { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }
    }

    /// <summary>
    /// A single processing window within a backfill request.
    /// </summary>
    public class BackfillWindow
    {
        public string FromValue        { get; set; } = string.Empty;
        public string ToValue          { get; set; } = string.Empty;
        public BackfillWindowStatus Status { get; set; } = BackfillWindowStatus.Pending;
        public string? RunId           { get; set; }
        public long   RecordsProcessed { get; set; }
        public string? Error           { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public enum BackfillRequestStatus
    {
        Pending,
        InProgress,
        Completed,
        PartialFailure,
        Cancelled
    }

    public enum BackfillWindowStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped
    }
}
