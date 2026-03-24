using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Append-only audit log for schedule definition changes.
    /// Tracks who changed what, when, and why — supporting change-control governance.
    /// Storage: {BeepDataPath}/ScheduleAudit/{scheduleId}.audit.jsonl (JSON Lines format)
    /// </summary>
    public sealed class ScheduleAuditLog
    {
        private readonly IDMEEditor _editor;
        private readonly string     _folder;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ScheduleAuditLog(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _folder = EnvironmentService.CreateAppfolder("ScheduleAudit");
        }

        /// <summary>
        /// Append an audit entry for a schedule change.
        /// </summary>
        public async Task LogChangeAsync(ScheduleAuditEntry entry)
        {
            try
            {
                string path = AuditPath(entry.ScheduleId);
                string line = JsonSerializer.Serialize(entry, _json);
                await File.AppendAllTextAsync(path, line + Environment.NewLine).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(ScheduleAuditLog),
                    $"Failed to write audit entry for schedule '{entry.ScheduleId}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Convenience method to log a schedule creation.
        /// </summary>
        public Task LogCreatedAsync(string scheduleId, string scheduleName, string changedBy, string reason = "")
            => LogChangeAsync(new ScheduleAuditEntry
            {
                ScheduleId   = scheduleId,
                ScheduleName = scheduleName,
                Action       = ScheduleAuditAction.Created,
                ChangedBy    = changedBy,
                Reason       = reason,
                Details      = $"Schedule '{scheduleName}' created."
            });

        /// <summary>
        /// Convenience method to log a schedule modification.
        /// </summary>
        public Task LogModifiedAsync(string scheduleId, string scheduleName, string changedBy,
            string fieldChanged, string? oldValue, string? newValue, string reason = "")
            => LogChangeAsync(new ScheduleAuditEntry
            {
                ScheduleId   = scheduleId,
                ScheduleName = scheduleName,
                Action       = ScheduleAuditAction.Modified,
                ChangedBy    = changedBy,
                Reason       = reason,
                FieldChanged = fieldChanged,
                OldValue     = oldValue,
                NewValue     = newValue,
                Details      = $"Field '{fieldChanged}' changed from '{oldValue}' to '{newValue}'."
            });

        /// <summary>
        /// Convenience method to log an enable/disable toggle.
        /// </summary>
        public Task LogToggledAsync(string scheduleId, string scheduleName, bool isEnabled,
            string changedBy, string reason = "")
            => LogChangeAsync(new ScheduleAuditEntry
            {
                ScheduleId   = scheduleId,
                ScheduleName = scheduleName,
                Action       = isEnabled ? ScheduleAuditAction.Enabled : ScheduleAuditAction.Disabled,
                ChangedBy    = changedBy,
                Reason       = reason,
                Details      = isEnabled ? "Schedule enabled." : "Schedule disabled."
            });

        /// <summary>
        /// Log when the circuit breaker trips or resets.
        /// </summary>
        public Task LogCircuitBreakerAsync(string scheduleId, string scheduleName, bool tripped,
            int consecutiveFailures)
            => LogChangeAsync(new ScheduleAuditEntry
            {
                ScheduleId   = scheduleId,
                ScheduleName = scheduleName,
                Action       = tripped ? ScheduleAuditAction.CircuitBreakerTripped : ScheduleAuditAction.CircuitBreakerReset,
                ChangedBy    = "system",
                Details      = tripped
                    ? $"Circuit breaker tripped after {consecutiveFailures} consecutive failures."
                    : "Circuit breaker reset."
            });

        /// <summary>
        /// Read all audit entries for a schedule, ordered by timestamp.
        /// </summary>
        public async Task<IReadOnlyList<ScheduleAuditEntry>> GetHistoryAsync(string scheduleId)
        {
            var result = new List<ScheduleAuditEntry>();
            string path = AuditPath(scheduleId);
            if (!File.Exists(path)) return result;

            try
            {
                var lines = await File.ReadAllLinesAsync(path).ConfigureAwait(false);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var entry = JsonSerializer.Deserialize<ScheduleAuditEntry>(line, _json);
                        if (entry != null) result.Add(entry);
                    }
                    catch { /* skip malformed lines */ }
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(ScheduleAuditLog),
                    $"Failed to read audit history for schedule '{scheduleId}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
            return result;
        }

        private string AuditPath(string scheduleId) =>
            Path.Combine(_folder, $"{scheduleId}.audit.jsonl");
    }

    // ── Audit Entry ────────────────────────────────────────────────────────────

    /// <summary>
    /// A single auditable change event for a schedule definition.
    /// </summary>
    public class ScheduleAuditEntry
    {
        public string Id           { get; set; } = Guid.NewGuid().ToString();
        public string ScheduleId   { get; set; } = string.Empty;
        public string ScheduleName { get; set; } = string.Empty;
        public ScheduleAuditAction Action { get; set; }
        public string ChangedBy    { get; set; } = string.Empty;
        public string Reason       { get; set; } = string.Empty;
        public string Details      { get; set; } = string.Empty;
        public string? FieldChanged { get; set; }
        public string? OldValue    { get; set; }
        public string? NewValue    { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }

    public enum ScheduleAuditAction
    {
        Created,
        Modified,
        Deleted,
        Enabled,
        Disabled,
        CircuitBreakerTripped,
        CircuitBreakerReset
    }
}
