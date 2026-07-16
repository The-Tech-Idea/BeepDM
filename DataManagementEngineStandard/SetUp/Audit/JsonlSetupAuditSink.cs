using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.SetUp.Audit;

namespace TheTechIdea.Beep.SetUp.Audit
{
    /// <summary>
    /// Solo-default <see cref="ISetupAuditSink"/>: one append-only <c>.jsonl</c> line per event.
    /// </summary>
    /// <remarks>
    /// Append-only is the whole point — the old checkpoint was overwritten in place, destroying the
    /// prior run's record. Records are appended, never rewritten, so every run's history survives.
    /// </remarks>
    public sealed class JsonlSetupAuditSink : ISetupAuditSink
    {
        private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
        private static readonly JsonSerializerOptions Json = new()
        {
            WriteIndented = false,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        private readonly string _path;
        private readonly ILogger _logger;
        private readonly object _lock = new();

        public JsonlSetupAuditSink(string filePath, ILogger logger = null)
        {
            _path = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _logger = logger;
        }

        public Task RecordAsync(SetupAuditEvent evt, CancellationToken token = default)
        {
            if (evt == null) return Task.CompletedTask;
            try
            {
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var line = JsonSerializer.Serialize(evt, Json) + Environment.NewLine;
                lock (_lock)
                    File.AppendAllText(_path, line, Utf8NoBom);
            }
            catch (Exception ex)
            {
                // Auditing must never fail the run — log and move on. Enterprise deployments that
                // need audit-or-abort wrap the sink.
                _logger?.LogWarning(ex, "Setup audit write failed for run {RunId}.", evt.RunId);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SetupAuditEvent>> QueryAsync(string runId, CancellationToken token = default)
        {
            var results = new List<SetupAuditEvent>();
            try
            {
                if (File.Exists(_path))
                {
                    foreach (var line in File.ReadLines(_path))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        SetupAuditEvent evt;
                        try { evt = JsonSerializer.Deserialize<SetupAuditEvent>(line, Json); }
                        catch { continue; }   // tolerate a torn last line
                        if (evt != null && (runId == null || evt.RunId == runId))
                            results.Add(evt);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Setup audit read failed.");
            }
            return Task.FromResult<IReadOnlyList<SetupAuditEvent>>(results);
        }
    }
}
