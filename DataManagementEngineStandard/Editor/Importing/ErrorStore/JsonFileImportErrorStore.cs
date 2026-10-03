using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Editor.Importing.Storage;

namespace TheTechIdea.Beep.Editor.Importing.ErrorStore
{
    /// <summary>
    /// JSONL (one JSON object per line) error store.
    /// Files written to <c>&lt;BeepRoot&gt;/Importing/Errors/&lt;contextKey&gt;.errors.jsonl</c>.
    /// Fully functional without a local database driver — the zero-config fallback.
    /// </summary>
    public sealed partial class JsonFileImportErrorStore : IImportErrorStore, IImportRejectRecoveryStore
    {
        private readonly ImportJsonLinesStore<ImportErrorRecord> _store;

        public JsonFileImportErrorStore() : this(Path.Combine(EnvironmentService.CreateAppfolder("Importing"), "Errors"))
        {
        }

        public JsonFileImportErrorStore(string folder) =>
            _store = new ImportJsonLinesStore<ImportErrorRecord>(folder, ".errors.jsonl", record => record.ContextKey,
                ValidateRecord, ValidateIdentities, ValidateJson);

        public Task SaveAsync(ImportErrorRecord record, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            if (record.Recovery == null) return _store.AppendAsync(record, token);
            if (record.Recovery.State != ImportRejectState.Pending || record.Recovery.Revision != 0 || record.Replayed)
                throw Invalid();
            // Managed records use the closed typed destination snapshot, not arbitrary RawRecord serialization.
            var snapshot = new ImportErrorRecord
            {
                ContextKey = record.ContextKey, OccurredAt = record.OccurredAt, BatchNumber = record.BatchNumber,
                RecordIndex = record.RecordIndex, RuleName = record.RuleName, Reason = record.Reason,
                Recovery = record.Recovery, TriageNote = record.TriageNote
            };
            return _store.AppendAsync(snapshot, token);
        }

        public async Task<IReadOnlyList<ImportErrorRecord>> LoadAsync(string contextKey, CancellationToken token = default)
        {
            return await _store.ReadAsync(contextKey, token).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<ImportErrorRecord>> LoadPendingAsync(string contextKey, CancellationToken token = default)
        {
            var all = await LoadAsync(contextKey, token).ConfigureAwait(false);
            return all.Where(r => !r.Replayed && (r.Recovery == null || r.Recovery.State == ImportRejectState.Pending ||
                r.Recovery.State == ImportRejectState.Prepared)).ToList();
        }

        public Task MarkReplayedAsync(string contextKey, int batchNumber, int recordIndex, CancellationToken token = default) =>
            _store.MutateAsync(contextKey, all =>
        {
                var matches = all.Where(r => r.BatchNumber == batchNumber && r.RecordIndex == recordIndex).ToArray();
                if (matches.Length != 1 || matches[0].Recovery != null) throw Invalid();
                matches[0].Replayed = true; matches[0].ReplayedAt = DateTime.UtcNow;
        }, token);

        public async Task ClearAsync(string contextKey, CancellationToken token = default)
        {
            // Validate terminal ownership inside the same mutation lease as deletion.
            await _store.ClearValidatedAsync(contextKey, all =>
            {
                if (all.Any(record => record.Recovery != null && record.Recovery.State != ImportRejectState.Acknowledged &&
                    record.Recovery.State != ImportRejectState.ReconciledApplied && record.Recovery.State != ImportRejectState.Dismissed)) throw Invalid();
            }, token).ConfigureAwait(false);
        }
    }
}
