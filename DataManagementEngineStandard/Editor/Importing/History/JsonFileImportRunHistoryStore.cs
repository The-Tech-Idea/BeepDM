using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Editor.Importing.Storage;

namespace TheTechIdea.Beep.Editor.Importing.History
{
    /// <summary>
    /// JSONL-backed import run history store.
    /// Files written to <c>&lt;BeepRoot&gt;/Importing/History/&lt;contextKey&gt;.history.jsonl</c>.
    /// Zero-config default — no local driver required.
    /// </summary>
    public sealed class JsonFileImportRunHistoryStore : IImportRunHistoryStore
    {
        private readonly ImportJsonLinesStore<ImportRunRecord> _store;

        public JsonFileImportRunHistoryStore() : this(Path.Combine(EnvironmentService.CreateAppfolder("Importing"), "History"))
        {
        }

        public JsonFileImportRunHistoryStore(string folder) =>
            _store = new ImportJsonLinesStore<ImportRunRecord>(folder, ".history.jsonl", record => record.ContextKey);

        public Task SaveRunAsync(ImportRunRecord record, CancellationToken token = default) =>
            _store.AppendAsync(record, token);

        public async Task<IReadOnlyList<ImportRunRecord>> GetRunsAsync(string contextKey, CancellationToken token = default)
        {
            return (await _store.ReadAsync(contextKey, token).ConfigureAwait(false))
                .OrderByDescending(r => r.StartedAt)
                .ToList();
        }

        public async Task<ImportRunRecord?> GetLastSuccessfulRunAsync(string contextKey, CancellationToken token = default)
        {
            var all = await GetRunsAsync(contextKey, token).ConfigureAwait(false);
            return all.FirstOrDefault(r => r.FinalState == ImportState.Completed);
        }

        public Task ClearAsync(string contextKey, CancellationToken token = default) =>
            _store.ClearAsync(contextKey, token);
    }
}
