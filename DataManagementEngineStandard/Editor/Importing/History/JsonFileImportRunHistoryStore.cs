using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Editor.Importing.History
{
    /// <summary>
    /// JSONL-backed import run history store.
    /// Files written to <c>&lt;BeepRoot&gt;/Importing/History/&lt;contextKey&gt;.history.jsonl</c>.
    /// Zero-config default — no local driver required.
    /// </summary>
    public sealed class JsonFileImportRunHistoryStore : IImportRunHistoryStore
    {
        private readonly string _folder;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public JsonFileImportRunHistoryStore()
        {
            var root = EnvironmentService.CreateAppfolder("Importing");
            _folder  = Path.Combine(root, "History");
            Directory.CreateDirectory(_folder);
        }

        public async Task SaveRunAsync(ImportRunRecord record, CancellationToken token = default)
        {
            await _lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var line = JsonSerializer.Serialize(record) + Environment.NewLine;
                await File.AppendAllTextAsync(GetPath(record.ContextKey), line, token).ConfigureAwait(false);
            }
            finally { _lock.Release(); }
        }

        public async Task<IReadOnlyList<ImportRunRecord>> GetRunsAsync(string contextKey, CancellationToken token = default)
        {
            var path = GetPath(contextKey);
            if (!File.Exists(path)) return Array.Empty<ImportRunRecord>();

            var lines = await File.ReadAllLinesAsync(path, token).ConfigureAwait(false);
            return lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => JsonSerializer.Deserialize<ImportRunRecord>(l)!)
                .OrderByDescending(r => r.StartedAt)
                .ToList();
        }

        public async Task<ImportRunRecord?> GetLastSuccessfulRunAsync(string contextKey, CancellationToken token = default)
        {
            var all = await GetRunsAsync(contextKey, token).ConfigureAwait(false);
            return all.FirstOrDefault(r => r.FinalState == ImportState.Completed);
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

        private string GetPath(string contextKey)
        {
            var safe = string.Join("_", contextKey.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_folder, $"{safe}.history.jsonl");
        }
    }
}
