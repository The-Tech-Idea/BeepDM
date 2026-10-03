using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    internal sealed class SyncProviderRejectStore : IImportErrorStore
    {
        private readonly IDataSource _source;
        private readonly string _entity;
        private SyncProviderRejectStore(IDataSource source, string entity) { _source = source; _entity = entity; }

        internal static IImportErrorStore Capture(IDMEEditor editor, DataSyncSchema schema, IImportErrorStore supplied)
        {
            var name = schema.DqPolicy?.RejectChannelDataSourceName;
            var entity = schema.DqPolicy?.RejectChannelEntityName;
            bool hasName = !string.IsNullOrWhiteSpace(name), hasEntity = !string.IsNullOrWhiteSpace(entity);
            if (hasName != hasEntity) throw new ImportQualityAdmissionException();
            if (supplied != null || !hasName) return supplied;
            var source = editor.GetDataSource(name);
            if (source?.ConnectionStatus != ConnectionState.Open || !source.CheckEntityExist(entity))
                throw new ImportQualityAdmissionException();
            return new SyncProviderRejectStore(source, entity);
        }

        public async Task SaveAsync(ImportErrorRecord record, CancellationToken token = default)
        {
            var acknowledgement = await Task.Run(() => _source.InsertEntity(_entity, record), token).ConfigureAwait(false);
            if (acknowledgement?.Flag != Errors.Ok)
                throw new InvalidOperationException("Sync reject-channel write was not acknowledged.");
        }

        // Provider-specific triage/replay is not inferred from a generic reject write acknowledgement.
        public Task<IReadOnlyList<ImportErrorRecord>> LoadAsync(string key, CancellationToken token = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ImportErrorRecord>> LoadPendingAsync(string key, CancellationToken token = default) => throw new NotSupportedException();
        public Task MarkReplayedAsync(string key, int batch, int index, CancellationToken token = default) => throw new NotSupportedException();
        public Task ClearAsync(string key, CancellationToken token = default) => throw new NotSupportedException();
    }
}
