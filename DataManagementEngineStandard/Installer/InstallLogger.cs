using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>
    /// File-based structured logger for installation operations.
    /// Writes to %TEMP%\Beep_Install_{timestamp}.log and copies to install dir on completion.
    /// </summary>
    public class InstallLogger
    {
        private readonly string _logPath;
        private readonly List<InstallLogEntry> _entries = new();
        private readonly object _lock = new();

        public string LogFilePath => _logPath;
        public IReadOnlyList<InstallLogEntry> Entries
        {
            get { lock (_lock) return _entries.AsReadOnly(); }
        }

        public InstallLogger(string? customPath = null)
        {
            _logPath = customPath ?? Path.Combine(
                Path.GetTempPath(),
                $"Beep_Install_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        }

        public void Info(string operation, string message)
            => Write(LogLevel.Info, operation, message, null);

        public void Warn(string operation, string message)
            => Write(LogLevel.Warn, operation, message, null);

        public void Error(string operation, string message, Exception? ex = null)
            => Write(LogLevel.Error, operation, message, ex);

        public void Debug(string operation, string message)
            => Write(LogLevel.Debug, operation, message, null);

        public void StepStart(string stepId, string stepName)
            => Write(LogLevel.Info, stepId, $"BEGIN: {stepName}");

        public void StepComplete(string stepId, bool success, string? message = null)
            => Write(success ? LogLevel.Info : LogLevel.Error, stepId,
                $"{(success ? "PASS" : "FAIL")}: {(message ?? (success ? "OK" : "Error"))}");

        public string GetSummary()
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== Installation Log Summary ===");
                sb.AppendLine($"File: {_logPath}");
                sb.AppendLine($"Entries: {_entries.Count}");
                sb.AppendLine();

                foreach (var entry in _entries)
                    sb.AppendLine($"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] {entry.Operation}: {entry.Message}");

                var errors = _entries.FindAll(e => e.Level == LogLevel.Error);
                if (errors.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"=== {errors.Count} ERROR(S) ===");
                    foreach (var e in errors)
                        sb.AppendLine($"  [{e.Timestamp:HH:mm:ss}] {e.Operation}: {e.Message}");
                }

                return sb.ToString();
            }
        }

        public void CopyTo(string destinationDir)
        {
            try
            {
                if (!Directory.Exists(destinationDir))
                    Directory.CreateDirectory(destinationDir);
                var dest = Path.Combine(destinationDir, Path.GetFileName(_logPath));
                File.Copy(_logPath, dest, overwrite: true);
            }
            catch { /* best-effort copy */ }
        }

        private void Write(LogLevel level, string operation, string message, Exception? ex = null)
        {
            var entry = new InstallLogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Operation = operation,
                Message = message,
                Details = ex?.ToString()
            };

            lock (_lock)
            {
                _entries.Add(entry);
                try
                {
                    var line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Operation}: {entry.Message}";
                    if (ex != null) line += $"\n{ex}";
                    File.AppendAllText(_logPath, line + Environment.NewLine);
                }
                catch { /* best-effort file write */ }
            }
        }
    }

    public class InstallLogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Operation { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Details { get; set; }
    }

    public enum LogLevel { Debug, Info, Warn, Error }
}
