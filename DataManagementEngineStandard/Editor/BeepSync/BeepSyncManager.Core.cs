using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Common.Retry;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Schema;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.History;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Modern sync orchestrator — split into focused partial classes.
    /// Core: fields, constructors, public state properties, and Dispose.
    /// </summary>
    public partial class BeepSyncManager : IDisposable
    {
        private bool _disposedValue;
        private readonly IDMEEditor _editor;
        private readonly ISyncValidationHelper _validationHelper;
        private readonly ISchemaPersistenceHelper _persistenceHelper;
        private readonly ISyncProgressHelper _progressHelper;
        private IRetryPipeline? _retryPipeline;
        private readonly List<SyncDiagnosticFailureEventArgs> _diagnosticFailures = new();

        /// <summary>Failures in advisory diagnostics, separate from provider and checkpoint outcomes.</summary>
        public event EventHandler<SyncDiagnosticFailureEventArgs> DiagnosticFailed;

        /// <summary>A copy of diagnostic failures for the latest run; no lifetime history is retained.</summary>
        public IReadOnlyList<SyncDiagnosticFailureEventArgs> LastRunDiagnosticFailures => _diagnosticFailures.ToArray();

        /// <summary>Optional integration context: Rule Engine, Defaults Manager, mapping-plan state.</summary>
        public SyncIntegrationContext IntegrationContext { get; set; }

        /// <summary>Conflict evidence from the last bidirectional run (requires <see cref="ConflictPolicy.CaptureEvidence"/>).</summary>
        public List<ConflictEvidence> LastRunConflicts { get; private set; } = new List<ConflictEvidence>();

        /// <summary>Checkpoint saved/resumed during the most recent <see cref="SyncDataAsync"/> call.</summary>
        public SyncCheckpoint LastRunCheckpoint { get; private set; }

        /// <summary>Reconciliation report from the most recent <see cref="SyncDataAsync"/> call.</summary>
        public SyncReconciliationReport LastRunReconciliationReport { get; private set; }

        /// <summary>Attempt threshold evidence; null when explicitly disabled or not evaluated.</summary>
        public SyncBatchThresholdResult LastRunBatchThresholdResult { get; private set; }

        /// <summary>Failed-run checkpoint acknowledgement, separate from the original import/threshold outcome.</summary>
        public PersistenceWriteStatus? LastRunFailureCheckpointStatus { get; private set; }

        public string Filepath { get; set; }
        public IDMEEditor Editor => _editor;
        public ObservableBindingList<DataSyncSchema> SyncSchemas { get; set; }

        /// <summary>
        /// Retry pipeline used by <see cref="BeepSyncManager.Sync.cs"/> to run the
        /// whole-sync attempt loop with backoff, classification, and per-attempt
        /// checkpoint hooks. Lazily resolved.
        /// </summary>
        protected IRetryPipeline RetryPipeline => _retryPipeline ??= new RetryPipeline();

        public BeepSyncManager(IDMEEditor editor)
            : this(editor, new SchemaPersistenceHelper(editor))
        { }

        private BeepSyncManager(IDMEEditor editor, ISchemaPersistenceHelper persistenceHelper)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            SyncSchemas = new ObservableBindingList<DataSyncSchema>();
            ArgumentNullException.ThrowIfNull(persistenceHelper);
            Filepath = persistenceHelper is SchemaPersistenceHelper local
                ? System.IO.Path.GetDirectoryName(local.GetSchemasFilePath()) : null;

            _validationHelper = new SyncValidationHelper(_editor);
            _persistenceHelper = persistenceHelper;
            _progressHelper    = new SyncProgressHelper(_editor, ReportDiagnosticFailure);

            LoadSchemas();
        }

        /// <summary>Advanced constructor with pre-built integration context.</summary>
        public BeepSyncManager(IDMEEditor editor, SyncIntegrationContext integrationContext)
            : this(editor)
        {
            IntegrationContext = integrationContext;
        }

        /// <summary>Explicitly selects host/test storage without changing the process-wide legacy store.</summary>
        public BeepSyncManager(IDMEEditor editor, SyncIntegrationContext integrationContext, string storageDirectory)
            : this(editor, new SchemaPersistenceHelper(editor, storageDirectory))
        { IntegrationContext = integrationContext; }

        /// <summary>Host-owned storage adapter; checkpoint execution requires its acknowledgement capability.</summary>
        public static BeepSyncManager CreateWithPersistence(IDMEEditor editor, ISchemaPersistenceHelper persistenceHelper, SyncIntegrationContext integrationContext = null) =>
            new BeepSyncManager(editor, persistenceHelper) { IntegrationContext = integrationContext };

        // ── Helpers ────────────────────────────────────────────────────────────────

        private DataSyncSchema FindSchema(string id) =>
            string.IsNullOrEmpty(id) ? null : SyncSchemas?.FirstOrDefault(s => s.Id == id);

        private static IProgress<IPassedArgs> CreateProgressAdapter(IProgress<PassedArgs> progress) =>
            progress == null ? null : new Progress<IPassedArgs>(p =>
                progress.Report(p as PassedArgs ?? new PassedArgs { Messege = p?.Messege }));

        private void LogSyncRun(DataSyncSchema schema)
        {
            if (schema?.SyncRuns == null) return;
            var run = new SyncRunData
            {
                SyncSchemaId      = schema.Id,
                SyncDate          = schema.LastSyncDate,
                SyncStatus        = schema.SyncStatus,
                SyncStatusMessage = schema.SyncStatusMessage
            };
            schema.SyncRuns.Add(run);
            schema.LastSyncRunData = run;
        }

        private void RunDiagnostic(string operation, Action action)
        {
            try { action(); }
            catch (Exception ex) { ReportDiagnosticFailure(operation, ex); }
        }

        private void ReportDiagnosticFailure(string operation, Exception error)
        {
            var failure = new SyncDiagnosticFailureEventArgs(operation, error.GetType().Name);
            _diagnosticFailures.Add(failure);
            var subscribers = DiagnosticFailed?.GetInvocationList();
            if (subscribers != null)
            {
                foreach (EventHandler<SyncDiagnosticFailureEventArgs> subscriber in subscribers)
                {
                    try { subscriber(this, failure); }
                    catch (Exception observerError)
                    {
                        // Diagnostic observers cannot change execution or recursively report themselves.
                        System.Diagnostics.Debug.WriteLine($"Sync diagnostic observer failed ({observerError.GetType().Name}).");
                    }
                }
            }
            try { _editor.AddLogMessage("BeepSync", $"{operation} diagnostic failed ({failure.ExceptionType}).", DateTime.Now, -1, "", Errors.Warning); }
            catch (Exception loggerError)
            {
                System.Diagnostics.Debug.WriteLine($"Sync diagnostic logging failed ({loggerError.GetType().Name}).");
            }
        }

        // ── Dispose ────────────────────────────────────────────────────────────────

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;
            if (disposing) SyncSchemas?.Clear();
            _disposedValue = true;
        }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
    }
}
