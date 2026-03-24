using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Importing;
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

        /// <summary>Optional integration context: Rule Engine, Defaults Manager, mapping-plan state.</summary>
        public SyncIntegrationContext IntegrationContext { get; set; }

        /// <summary>Conflict evidence from the last bidirectional run (requires <see cref="ConflictPolicy.CaptureEvidence"/>).</summary>
        public List<ConflictEvidence> LastRunConflicts { get; private set; } = new List<ConflictEvidence>();

        /// <summary>Checkpoint saved/resumed during the most recent <see cref="SyncDataAsync"/> call.</summary>
        public SyncCheckpoint LastRunCheckpoint { get; private set; }

        /// <summary>Reconciliation report from the most recent <see cref="SyncDataAsync"/> call.</summary>
        public SyncReconciliationReport LastRunReconciliationReport { get; private set; }

        public string Filepath { get; set; }
        public IDMEEditor Editor => _editor;
        public ObservableBindingList<DataSyncSchema> SyncSchemas { get; set; }

        public BeepSyncManager(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            SyncSchemas = new ObservableBindingList<DataSyncSchema>();
            Filepath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TheTechIdea", "Beep", "BeepSyncManager");

            _validationHelper = new SyncValidationHelper(_editor);
            _persistenceHelper = new SchemaPersistenceHelper(_editor);
            _progressHelper    = new SyncProgressHelper(_editor);

            LoadSchemas();
        }

        /// <summary>Advanced constructor with pre-built integration context.</summary>
        public BeepSyncManager(IDMEEditor editor, SyncIntegrationContext integrationContext)
            : this(editor)
        {
            IntegrationContext = integrationContext;
        }

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
