using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Editor.Importing.ErrorStore
{
    /// <summary>
    /// JSONL (one JSON object per line) error store.
    /// Files written to <c>&lt;BeepRoot&gt;/Importing/Errors/&lt;contextKey&gt;.errors.jsonl</c>.
    /// Fully functional without a local database driver — the zero-config fallback.
    /// </summary>
    public sealed class JsonFileImportErrorStore : IImportErrorStore
    {
        private readonly string _folder;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public JsonFileImportErrorStore()
        {
            var root = EnvironmentService.CreateAppfolder("Importing");
            _folder  = Path.Combine(root, "Errors");
            Directory.CreateDirectory(_folder);
        }

        public async Task SaveAsync(ImportErrorRecord record, CancellationToken token = default)
        {
            await _lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var line = JsonSerializer.Serialize(record) + Environment.NewLine;
                await File.AppendAllTextAsync(GetPath(record.ContextKey), line, token).ConfigureAwait(false);
            }
            finally { _lock.Release(); }
        }

        public async Task<IReadOnlyList<ImportErrorRecord>> LoadAsync(string contextKey, CancellationToken token = default)
        {
            var path = GetPath(contextKey);
            if (!File.Exists(path)) return Array.Empty<ImportErrorRecord>();

            var lines = await File.ReadAllLinesAsync(path, token).ConfigureAwait(false);
            return lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => JsonSerializer.Deserialize<ImportErrorRecord>(l)!)
                .ToList();
        }

        public async Task<IReadOnlyList<ImportErrorRecord>> LoadPendingAsync(string contextKey, CancellationToken token = default)
        {
            var all = await LoadAsync(contextKey, token).ConfigureAwait(false);
            return all.Where(r => !r.Replayed).ToList();
        }

        public async Task MarkReplayedAsync(string contextKey, int batchNumber, int recordIndex, CancellationToken token = default)
        {
            await _lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var all = (await LoadAsync(contextKey, token).ConfigureAwait(false)).ToList();
                var rec = all.FirstOrDefault(r => r.BatchNumber == batchNumber && r.RecordIndex == recordIndex);
                if (rec != null) { rec.Replayed = true; rec.ReplayedAt = DateTime.UtcNow; }

                await RewriteAsync(GetPath(contextKey), all, token).ConfigureAwait(false);
            }
            finally { _lock.Release(); }
        }

        public async Task ClearAsync(string contextKey, CancellationToken token = default)
        {
            await _lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var path = GetPath(contextKey);
                if (File.Exists(path)) File.Delete(path);
            }
            finally { _lock.Release(); }
        }

        // ------------------------------------------------------------------
        private string GetPath(string contextKey)
        {
            var safe = string.Join("_", contextKey.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_folder, $"{safe}.errors.jsonl");
        }

        private static async Task RewriteAsync(string path, IEnumerable<ImportErrorRecord> records, CancellationToken token)
        {
            var lines = records.Select(r => JsonSerializer.Serialize(r));
            await File.WriteAllLinesAsync(path, lines, token).ConfigureAwait(false);
        }
    }
}
