using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.Sync
{
    public partial class SyncManager : IDisposable
    {
        private readonly IDMEEditor _editor;
        private readonly ISchemaRepository _schemaRepository;
        private readonly ISyncService _syncService;
        private readonly ManualResetEventSlim _pauseEvent;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private bool _disposedValue;
        public ObservableBindingList<DataSyncSchema> SyncSchemas { get; private set; }

        private Task _syncTask;
        private IProgress<PassedArgs> _progress;

        public SyncManager(IDMEEditor dME, ISchemaRepository schemaRepository, ISyncService syncService)
        {
            _editor = dME;
            _schemaRepository = schemaRepository;
            _syncService = syncService;

            // Initialize synchronization control objects
            _pauseEvent = new ManualResetEventSlim(true);
            _cancellationTokenSource = new CancellationTokenSource();

            SyncSchemas = _schemaRepository.LoadSchemas();
        }

        /// <summary>
        /// Starts synchronization of all schemas asynchronously.
        /// </summary>
        /// <param name="progress">Optional progress reporter.</param>
        public void StartSyncAllAsync(IProgress<PassedArgs> progress = null)
        {
            _progress = progress;
            _syncTask = Task.Run(() => SyncAllDataAsync(_cancellationTokenSource.Token, _progress), _cancellationTokenSource.Token);
        }

        private async Task SyncAllDataAsync(CancellationToken token, IProgress<PassedArgs> progress = null)
        {
            foreach (var schema in SyncSchemas)
            {
                // Check stop request
                token.ThrowIfCancellationRequested();

                // Check if paused
                _pauseEvent.Wait(token);

                // Synchronize this schema
                await _syncService.SyncSchemaAsync(schema, token, progress);
            }

            _editor.AddLogMessage("Beep", "All schemas synchronized successfully.", DateTime.Now, -1, "", Errors.Ok);
        }

        /// <summary>
        /// Pauses the synchronization. Any long-running sync operation in progress will pause at the next checkpoint.
        /// </summary>
        public void PauseSync()
        {
            _pauseEvent.Reset();
            _editor.AddLogMessage("Beep", "Synchronization paused.", DateTime.Now, -1, "", Errors.Ok);
        }

        /// <summary>
        /// Resumes synchronization if it was previously paused.
        /// </summary>
        public void ResumeSync()
        {
            _pauseEvent.Set();
            _editor.AddLogMessage("Beep", "Synchronization resumed.", DateTime.Now, -1, "", Errors.Ok);
        }

        /// <summary>
        /// Stops the synchronization by canceling the operation.
        /// </summary>
        public void StopSync()
        {
            _cancellationTokenSource.Cancel();
            _editor.AddLogMessage("Beep", "Synchronization stop requested.", DateTime.Now, -1, "", Errors.Ok);
        }

        public void AddSyncSchema(DataSyncSchema schema)
        {
            SyncSchemas.Add(schema);
        }

        public void RemoveSyncSchema(string schemaID)
        {
            var schema = SyncSchemas.Find(x => x.ID == schemaID);
            if (schema != null)
            {
                SyncSchemas.Remove(schema);
            }
        }

        public void UpdateSyncSchema(DataSyncSchema schema)
        {
            var existing = SyncSchemas.Find(x => x.ID == schema.ID);
            if (existing != null)
            {
                var index = SyncSchemas.IndexOf(existing);
                SyncSchemas[index] = schema;
            }
        }

        public void SaveSchemas()
        {
            _schemaRepository.SaveSchemas(SyncSchemas);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    SyncSchemas?.Clear();
                    _pauseEvent?.Dispose();
                    _cancellationTokenSource?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }


}
