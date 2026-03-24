using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Observability;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// JSON / JSONL persistence layer for all observability data:
    /// run logs, lineage records, alert events, audit entries, and metrics cache.
    ///
    /// Storage layout under the application data directory:
    /// <code>
    /// RunLogs/          {runId}.run.json
    /// Lineage/          {runId}.lineage.json
    /// Alerts/           alert-events.jsonl   (append-only)
    /// Audit/            audit.jsonl          (append-only)
    /// MetricsCache/     {pipelineId}.metrics.json
    /// </code>
    /// </summary>
    public class ObservabilityStore : ILineageStore
    {
        private readonly string _runLogsFolder;
        private readonly string _lineageFolder;
        private readonly string _alertsFile;
        private readonly string _auditFile;
        private readonly string _metricsFolder;

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = null
        };

        private readonly SemaphoreSlim _appendLock = new SemaphoreSlim(1, 1);
        private string? _lastAuditHash;

        public ObservabilityStore()
        {
            _runLogsFolder = EnvironmentService.CreateAppfolder("Observability", "RunLogs");
            _lineageFolder = EnvironmentService.CreateAppfolder("Observability", "Lineage");
            _metricsFolder = EnvironmentService.CreateAppfolder("Observability", "MetricsCache");

            string alertsFolder = EnvironmentService.CreateAppfolder("Observability", "Alerts");
            string auditFolder  = EnvironmentService.CreateAppfolder("Observability", "Audit");

            _alertsFile = Path.Combine(alertsFolder, "alert-events.jsonl");
            _auditFile  = Path.Combine(auditFolder,  "audit.jsonl");
        }

        // ── Run Logs ──────────────────────────────────────────────────────────

        public async Task SaveRunLogAsync(PipelineRunLog log)
        {
            string path = Path.Combine(_runLogsFolder, $"{log.RunId}.run.json");
            string json = JsonSerializer.Serialize(log, _json);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }

        public async Task<PipelineRunLog?> GetRunLogAsync(string runId)
        {
            string path = Path.Combine(_runLogsFolder, $"{runId}.run.json");
            if (!File.Exists(path)) return null;
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return JsonSerializer.Deserialize<PipelineRunLog>(json, _json);
        }

        public async Task<IReadOnlyList<PipelineRunLog>> QueryRunLogsAsync(RunLogQuery query)
        {
            var files = Directory.GetFiles(_runLogsFolder, "*.run.json");
            var tasks = files.Select(async f =>
            {
                string json = await File.ReadAllTextAsync(f).ConfigureAwait(false);
                return JsonSerializer.Deserialize<PipelineRunLog>(json, _json);
            });
            var all = (await Task.WhenAll(tasks).ConfigureAwait(false))
                      .OfType<PipelineRunLog>();

            if (query.PipelineId != null)
                all = all.Where(r => r.PipelineId == query.PipelineId);
            if (query.Status.HasValue)
                all = all.Where(r => r.Status == query.Status.Value);
            if (query.From.HasValue)
                all = all.Where(r => r.StartedAtUtc >= query.From.Value);
            if (query.To.HasValue)
                all = all.Where(r => r.StartedAtUtc <= query.To.Value);

            // Simple ordering
            bool desc = !query.OrderBy.EndsWith("ASC", StringComparison.OrdinalIgnoreCase);
            all = desc
                ? all.OrderByDescending(r => r.StartedAtUtc)
                : all.OrderBy(r => r.StartedAtUtc);

            return all.Skip(query.Offset).Take(query.Limit).ToList();
        }

        // ── Lineage ───────────────────────────────────────────────────────────

        public async Task AppendLineageAsync(IEnumerable<DataLineageRecord> records)
        {
            var list = records.ToList();
            if (list.Count == 0) return;

            string runId = list[0].RunId;
            string path  = Path.Combine(_lineageFolder, $"{runId}.lineage.json");
            string json  = JsonSerializer.Serialize(list, _json);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<DataLineageRecord>> GetLineageAsync(string runId)
        {
            string path = Path.Combine(_lineageFolder, $"{runId}.lineage.json");
            if (!File.Exists(path)) return Array.Empty<DataLineageRecord>();
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<DataLineageRecord>>(json, _json)
                   ?? new List<DataLineageRecord>();
        }

        // ── ILineageStore ─────────────────────────────────────────────────────

        async Task<IReadOnlyList<DataLineageRecord>> ILineageStore.GetByRunAsync(string runId)
            => await GetLineageAsync(runId).ConfigureAwait(false);

        async Task<IReadOnlyList<DataLineageRecord>> ILineageStore.TraceBackwardAsync(
            string destDataSource, string destEntity, string destField)
        {
            var all = await LoadAllLineageAsync().ConfigureAwait(false);
            return all.Where(r =>
                r.DestDataSource == destDataSource &&
                r.DestEntity     == destEntity &&
                r.DestField      == destField).ToList();
        }

        async Task<IReadOnlyList<DataLineageRecord>> ILineageStore.TraceForwardAsync(
            string srcDataSource, string srcEntity, string srcField)
        {
            var all = await LoadAllLineageAsync().ConfigureAwait(false);
            return all.Where(r =>
                r.SourceDataSource == srcDataSource &&
                r.SourceEntity     == srcEntity &&
                r.SourceField      == srcField).ToList();
        }

        async Task<LineageGraph> ILineageStore.GetGraphAsync(
            string srcDataSource, string destDataSource)
        {
            var all = await LoadAllLineageAsync().ConfigureAwait(false);
            var nodes = all.Where(r =>
                r.SourceDataSource == srcDataSource ||
                r.DestDataSource   == destDataSource).ToList();
            var edges = nodes
                .Where(r => !string.IsNullOrEmpty(r.StepId))
                .Select(r => new LineageEdge(r.SourceField, r.DestField,
                    string.IsNullOrEmpty(r.TransformExpression) ? r.StepName : r.TransformExpression))
                .ToList();
            return new LineageGraph { Nodes = nodes, Edges = edges };
        }

        private async Task<List<DataLineageRecord>> LoadAllLineageAsync()
        {
            var files = Directory.GetFiles(_lineageFolder, "*.lineage.json");
            var results = new List<DataLineageRecord>();
            foreach (var f in files)
            {
                string json = await File.ReadAllTextAsync(f).ConfigureAwait(false);
                var batch = JsonSerializer.Deserialize<List<DataLineageRecord>>(json, _json);
                if (batch != null) results.AddRange(batch);
            }
            return results;
        }

        // ── Alert Events ──────────────────────────────────────────────────────

        public async Task AppendAlertEventAsync(AlertEvent evt)
        {
            string line = JsonSerializer.Serialize(evt, _json);
            await AppendLineAsync(_alertsFile, line).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<AlertEvent>> GetAlertEventsAsync(AlertEventQuery query)
        {
            var events = await ReadJsonlAsync<AlertEvent>(_alertsFile).ConfigureAwait(false);
            var q = events.AsEnumerable();
            if (query.PipelineId    != null)     q = q.Where(e => e.PipelineId == query.PipelineId);
            if (query.RuleId        != null)     q = q.Where(e => e.RuleId     == query.RuleId);
            if (query.Severity.HasValue)          q = q.Where(e => e.Severity  == query.Severity.Value);
            if (query.Acknowledged.HasValue)      q = q.Where(e => e.Acknowledged == query.Acknowledged.Value);
            if (query.From.HasValue)              q = q.Where(e => e.FiredAtUtc >= query.From.Value);
            if (query.To.HasValue)                q = q.Where(e => e.FiredAtUtc <= query.To.Value);
            return q.Skip(query.Offset).Take(query.Limit).ToList();
        }

        public async Task UpdateAlertAcknowledgementAsync(string eventId, string by, DateTime at)
        {
            // Read all, update in memory, rewrite file (append-only JSONL, small files expected)
            var events = await ReadJsonlAsync<AlertEvent>(_alertsFile).ConfigureAwait(false);
            var evt = events.FirstOrDefault(e => e.EventId == eventId);
            if (evt != null)
            {
                evt.Acknowledged    = true;
                evt.AcknowledgedBy  = by;
                evt.AcknowledgedAt  = at;
            }
            await WriteJsonlAsync(_alertsFile, events).ConfigureAwait(false);
        }

        // ── Audit ─────────────────────────────────────────────────────────────

        public async Task AppendAuditAsync(AuditEntry entry)
        {
            // Build hash chain: link to previous entry hash
            await _appendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_lastAuditHash == null)
                    _lastAuditHash = await ReadLastAuditHashAsync().ConfigureAwait(false);

                entry.PreviousHash = _lastAuditHash;
                entry.EntryHash    = ComputeAuditEntryHash(entry);
                _lastAuditHash     = entry.EntryHash;

                string line = JsonSerializer.Serialize(entry, _json);
                await File.AppendAllTextAsync(_auditFile, line + "\n").ConfigureAwait(false);
            }
            finally { _appendLock.Release(); }
        }

        /// <summary>
        /// Verifies tamper-evident hash chain integrity of the audit log.
        /// Returns the index of the first broken link, or -1 if the chain is valid.
        /// </summary>
        public async Task<int> VerifyAuditIntegrityAsync()
        {
            var entries = await ReadJsonlAsync<AuditEntry>(_auditFile).ConfigureAwait(false);
            string? previousHash = null;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.PreviousHash != previousHash)
                    return i;

                string computed = ComputeAuditEntryHash(e);
                if (e.EntryHash != computed)
                    return i;

                previousHash = e.EntryHash;
            }
            return -1;
        }

        public async Task<IReadOnlyList<AuditEntry>> GetAuditTrailAsync(AuditQuery query)
        {
            var entries = await ReadJsonlAsync<AuditEntry>(_auditFile).ConfigureAwait(false);
            var q = entries.AsEnumerable();
            if (query.EntityType  != null) q = q.Where(e => e.EntityType  == query.EntityType);
            if (query.EntityId    != null) q = q.Where(e => e.EntityId    == query.EntityId);
            if (query.Action      != null) q = q.Where(e => e.Action      == query.Action);
            if (query.PerformedBy != null) q = q.Where(e => e.PerformedBy == query.PerformedBy);
            if (query.From.HasValue)        q = q.Where(e => e.PerformedAt >= query.From.Value);
            if (query.To.HasValue)          q = q.Where(e => e.PerformedAt <= query.To.Value);
            return q.Skip(query.Offset).Take(query.Limit).ToList();
        }

        // ── Metrics Cache ─────────────────────────────────────────────────────

        public async Task SaveMetricsCacheAsync(PipelineMetrics metrics)
        {
            string safe = string.Concat(metrics.PipelineId.Split(Path.GetInvalidFileNameChars()));
            string path = Path.Combine(_metricsFolder, $"{safe}.metrics.json");
            string json = JsonSerializer.Serialize(metrics, _json);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }

        public async Task<PipelineMetrics?> GetMetricsCacheAsync(string pipelineId)
        {
            string safe = string.Concat(pipelineId.Split(Path.GetInvalidFileNameChars()));
            string path = Path.Combine(_metricsFolder, $"{safe}.metrics.json");
            if (!File.Exists(path)) return null;
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return JsonSerializer.Deserialize<PipelineMetrics>(json, _json);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private async Task AppendLineAsync(string file, string line)
        {
            await _appendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await File.AppendAllTextAsync(file, line + "\n").ConfigureAwait(false);
            }
            finally { _appendLock.Release(); }
        }

        private static async Task<List<T>> ReadJsonlAsync<T>(string file)
        {
            if (!File.Exists(file)) return new List<T>();
            var lines = await File.ReadAllLinesAsync(file).ConfigureAwait(false);
            var result = new List<T>(lines.Length);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var item = JsonSerializer.Deserialize<T>(line, _json);
                if (item != null) result.Add(item);
            }
            return result;
        }

        private static async Task WriteJsonlAsync<T>(string file, IEnumerable<T> items)
        {
            var lines = items.Select(item => JsonSerializer.Serialize(item, _json));
            await File.WriteAllLinesAsync(file, lines).ConfigureAwait(false);
        }

        private async Task<string?> ReadLastAuditHashAsync()
        {
            if (!File.Exists(_auditFile)) return null;
            var lines = await File.ReadAllLinesAsync(_auditFile).ConfigureAwait(false);
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var entry = JsonSerializer.Deserialize<AuditEntry>(lines[i], _json);
                return entry?.EntryHash;
            }
            return null;
        }

        private static string ComputeAuditEntryHash(AuditEntry entry)
        {
            // Hash over the content fields (excluding EntryHash itself)
            var payload = $"{entry.Id}|{entry.Action}|{entry.EntityType}|{entry.EntityId}|{entry.EntityName}|{entry.PerformedBy}|{entry.PerformedAt:O}|{entry.PreviousHash}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(bytes);
        }
    }
}
