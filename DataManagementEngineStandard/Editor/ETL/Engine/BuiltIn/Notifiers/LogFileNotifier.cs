using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Attributes;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Observability;
using AlertEvent = TheTechIdea.Beep.Pipelines.Observability.AlertEvent;

namespace TheTechIdea.Beep.Pipelines.Engine.BuiltIn.Notifiers
{
    /// <summary>
    /// Appends alert notifications to a rotating local log file.
    ///
    /// Config parameters:
    /// <list type="bullet">
    ///   <item><c>FilePath</c>  — destination log file path (required)</item>
    ///   <item><c>MaxSizeMb</c> — rotate when file exceeds this size in MB, default 50</item>
    ///   <item><c>Format</c>    — log line template (token-substituted)</item>
    /// </list>
    /// </summary>
    [PipelinePlugin(
        "beep.notify.logfile",
        "Log File Notifier",
        PipelinePluginType.Notifier,
        Category = "Notify",
        Version  = "1.0",
        Author   = "The Tech Idea")]
    public class LogFileNotifier : IPipelineNotifier
    {
        public string PluginId    => "beep.notify.logfile";
        public string DisplayName => "Log File Notifier";
        public string Description => "Appends alert notifications to a rotating log file.";

        private string _filePath   = string.Empty;
        private long   _maxBytes   = 50L * 1024 * 1024;   // 50 MB
        private string _format     = "{Timestamp:O} [{Severity}] {PipelineName}: {Message}";

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        // ── IPipelinePlugin ────────────────────────────────────────────────────

        public IReadOnlyList<PipelineParameterDef> GetParameterDefinitions() => new[]
        {
            new PipelineParameterDef { Name = "FilePath",  Type = ParamType.FilePath, IsRequired = true  },
            new PipelineParameterDef { Name = "MaxSizeMb", Type = ParamType.Integer,  IsRequired = false, DefaultValue = "50" },
            new PipelineParameterDef { Name = "Format",    Type = ParamType.String,   IsRequired = false }
        };

        public void Configure(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("FilePath", out var fp)) _filePath = fp?.ToString() ?? "";
            if (parameters.TryGetValue("Format",   out var f))  _format   = f?.ToString()  ?? _format;

            if (parameters.TryGetValue("MaxSizeMb", out var m) &&
                int.TryParse(m?.ToString(), out int mb))
                _maxBytes = (long)mb * 1024 * 1024;
        }

        // ── IPipelineNotifier ──────────────────────────────────────────────────

        public async Task NotifyAsync(AlertEvent alertEvent, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_filePath)) return;

            string line = ApplyTokens(_format, alertEvent) + Environment.NewLine;

            await _lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                // Ensure directory exists
                string? dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Rotate if needed
                if (File.Exists(_filePath))
                {
                    var info = new FileInfo(_filePath);
                    if (info.Length >= _maxBytes)
                    {
                        string rotated = _filePath + "." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".bak";
                        File.Move(_filePath, rotated);
                    }
                }

                await File.AppendAllTextAsync(_filePath, line, Encoding.UTF8, token)
                    .ConfigureAwait(false);
            }
            finally { _lock.Release(); }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string ApplyTokens(string template, AlertEvent evt)
            => template
                .Replace("{Timestamp:O}",  evt.FiredAtUtc.ToString("O"))
                .Replace("{Timestamp}",    evt.FiredAtUtc.ToString("O"))
                .Replace("{Severity}",     evt.Severity.ToString())
                .Replace("{PipelineName}", evt.PipelineName)
                .Replace("{AlertRule}",    evt.RuleName)
                .Replace("{RunId}",        evt.RunId ?? "")
                .Replace("{Message}",      evt.Message);
    }
}
