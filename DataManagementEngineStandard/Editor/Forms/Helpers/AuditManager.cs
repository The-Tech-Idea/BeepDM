using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Manages field-level and commit-level audit recording.
    /// Accumulates pending field changes per block and flushes them to <see cref="IAuditStore"/> on commit.
    /// </summary>
    public class AuditManager : IAuditManager
    {
        #region Fields
        private readonly ConcurrentDictionary<string, List<(string FieldName, object OldValue, object NewValue, int RecordIndex)>> _pending
            = new ConcurrentDictionary<string, List<(string, object, object, int)>>(StringComparer.OrdinalIgnoreCase);

        private readonly object _pendingLock = new object();
        #endregion

        #region Constructor
        public AuditManager(IAuditStore store = null, AuditConfiguration configuration = null)
        {
            Store         = store         ?? new InMemoryAuditStore();
            Configuration = configuration ?? new AuditConfiguration();
        }
        #endregion

        #region IAuditManager
        public AuditConfiguration Configuration { get; }
        public string CurrentUser { get; private set; } = string.Empty;
        public IAuditStore Store  { get; }

        public void SetAuditUser(string userName)
            => CurrentUser = userName ?? string.Empty;

        public void Configure(Action<AuditConfiguration> configure)
            => configure?.Invoke(Configuration);

        // ── Accumulation ───────────────────────────────────────────────────

        public void RecordFieldChange(
            string blockName, string fieldName,
            object oldValue, object newValue,
            int recordIndex)
        {
            if (!Configuration.Enabled) return;
            if (!ShouldAuditBlock(blockName)) return;
            if (Configuration.ExcludedFields.Contains(fieldName)) return;

            lock (_pendingLock)
            {
                var list = _pending.GetOrAdd(blockName,
                    _ => new List<(string, object, object, int)>());
                list.Add((fieldName, oldValue, newValue, recordIndex));
            }
        }

        public void FlushPendingToStore(string formName, AuditOperation operation)
        {
            if (!Configuration.Enabled) return;

            List<(string blockName, List<(string FieldName, object OldValue, object NewValue, int RecordIndex)> changes)> snapshot;

            lock (_pendingLock)
            {
                snapshot = _pending
                    .Select(kv => (kv.Key, new List<(string, object, object, int)>(kv.Value)))
                    .ToList();
                _pending.Clear();
            }

            foreach (var (blockName, changes) in snapshot)
            {
                if (changes.Count == 0) continue;

                // Group changes by record index → one AuditEntry per record
                foreach (var group in changes.GroupBy(c => c.RecordIndex))
                {
                    var entry = new AuditEntry
                    {
                        FormName    = formName,
                        BlockName   = blockName,
                        RecordKey   = group.Key.ToString(),
                        Operation   = operation,
                        UserName    = CurrentUser,
                        Timestamp   = DateTime.UtcNow,
                        FieldChanges = group
                            .Select(c => new AuditFieldChange
                            {
                                FieldName = c.FieldName,
                                OldValue  = c.OldValue,
                                NewValue  = c.NewValue
                            })
                            .ToList()
                    };
                    Store.Save(entry);
                }
            }

            // Auto-purge if configured
            if (Configuration.MaxRetentionDays > 0)
                Store.Purge(Configuration.MaxRetentionDays);
        }

        public void DiscardPending()
        {
            lock (_pendingLock)
                _pending.Clear();
        }

        // ── Query ──────────────────────────────────────────────────────────

        public IReadOnlyList<AuditEntry> GetAuditLog(
            string blockName         = null,
            AuditOperation? operation = null,
            DateTime? from           = null,
            DateTime? to             = null)
            => Store.Query(blockName, operation, from, to);

        public IReadOnlyList<AuditFieldChange> GetFieldHistory(
            string blockName, string recordKey, string fieldName)
        {
            var entries = Store.Query(blockName);
            return entries
                .Where(e => e.RecordKey == recordKey)
                .SelectMany(e => e.FieldChanges.Where(fc =>
                    string.Equals(fc.FieldName, fieldName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // ── Export ─────────────────────────────────────────────────────────

        public async Task ExportToCsvAsync(string filePath, string blockName = null)
        {
            var entries = Store.Query(blockName);
            var sb = new StringBuilder();
            sb.AppendLine("Id,FormName,BlockName,RecordKey,Operation,UserName,Timestamp,FieldName,OldValue,NewValue");

            foreach (var e in entries)
            {
                if (e.FieldChanges.Count == 0)
                {
                    sb.AppendLine(
                        $"{e.Id},{CsvEsc(e.FormName)},{CsvEsc(e.BlockName)},{CsvEsc(e.RecordKey)}," +
                        $"{e.Operation},{CsvEsc(e.UserName)},{e.Timestamp:o},,,");
                }
                else
                {
                    foreach (var fc in e.FieldChanges)
                    {
                        sb.AppendLine(
                            $"{e.Id},{CsvEsc(e.FormName)},{CsvEsc(e.BlockName)},{CsvEsc(e.RecordKey)}," +
                            $"{e.Operation},{CsvEsc(e.UserName)},{e.Timestamp:o}," +
                            $"{CsvEsc(fc.FieldName)},{CsvEsc(fc.OldValue?.ToString())}," +
                            $"{CsvEsc(fc.NewValue?.ToString())}");
                    }
                }
            }

            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        }

        public async Task ExportToJsonAsync(string filePath, string blockName = null)
        {
            var entries = Store.Query(blockName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(entries, options);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
        }

        // ── Maintenance ────────────────────────────────────────────────────

        public void Purge(int olderThanDays) => Store.Purge(olderThanDays);
        public void Clear()                  => Store.Clear();

        #endregion

        #region Private Helpers
        private bool ShouldAuditBlock(string blockName)
        {
            if (Configuration.ExcludedBlocks.Contains(blockName)) return false;
            if (Configuration.AuditedBlocks.Count > 0)
                return Configuration.AuditedBlocks.Contains(blockName);
            return true; // empty AuditedBlocks means "all blocks"
        }

        private static string CsvEsc(string value)
        {
            if (value == null) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
        #endregion
    }
}
