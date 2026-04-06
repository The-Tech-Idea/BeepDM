using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// File-backed audit store: persists each session's entries to a single JSON file.
    /// Entries are appended lazily; the full file is read on the first query.
    /// </summary>
    public class FileAuditStore : IAuditStore
    {
        private readonly string _filePath;
        private readonly object _lock = new object();
        private List<AuditEntry> _cache;

        /// <param name="filePath">Absolute path to the JSON file. Created if missing.</param>
        public FileAuditStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            _filePath = filePath;
        }

        public void Save(AuditEntry entry)
        {
            if (entry == null) return;
            lock (_lock)
            {
                EnsureLoaded();
                _cache.Add(entry);
                Persist();
            }
        }

        public IReadOnlyList<AuditEntry> Query(
            string blockName         = null,
            AuditOperation? operation = null,
            DateTime? from           = null,
            DateTime? to             = null)
        {
            lock (_lock)
            {
                EnsureLoaded();
                IEnumerable<AuditEntry> q = _cache;

                if (!string.IsNullOrEmpty(blockName))
                    q = q.Where(e => string.Equals(e.BlockName, blockName, StringComparison.OrdinalIgnoreCase));

                if (operation.HasValue)
                    q = q.Where(e => e.Operation == operation.Value);

                if (from.HasValue)
                    q = q.Where(e => e.Timestamp >= from.Value);

                if (to.HasValue)
                    q = q.Where(e => e.Timestamp <= to.Value);

                return q.ToList();
            }
        }

        public void Purge(int olderThanDays)
        {
            var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(olderThanDays));
            lock (_lock)
            {
                EnsureLoaded();
                int before = _cache.Count;
                _cache.RemoveAll(e => e.Timestamp < cutoff);
                if (_cache.Count != before)
                    Persist();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _cache = new List<AuditEntry>();
                Persist();
            }
        }

        #region Private Helpers

        private void EnsureLoaded()
        {
            if (_cache != null) return;

            if (!File.Exists(_filePath))
            {
                _cache = new List<AuditEntry>();
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                _cache = JsonSerializer.Deserialize<List<AuditEntry>>(json)
                         ?? new List<AuditEntry>();
            }
            catch
            {
                _cache = new List<AuditEntry>();
            }
        }

        private void Persist()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_cache,
                new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(_filePath, json);
        }

        #endregion
    }
}
