using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Report;


namespace TheTechIdea.Beep.Editor
{
    public class DataSyncSchema : Entity
    {

        private string _id;
      //  [JsonProperty("ID")]
        public string Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _entityname;
        public string EntityName
        {
            get { return _entityname; }
            set { SetProperty(ref _entityname, value); }
        }

        private string _sourceentityname;
        public string SourceEntityName
        {
            get { return _sourceentityname; }
            set { SetProperty(ref _sourceentityname, value); }
        }

        private string _destinationentityname;
        public string DestinationEntityName
        {
            get { return _destinationentityname; }
            set { SetProperty(ref _destinationentityname, value); }
        }

        private string _sourcedatasourcename;
        public string SourceDataSourceName
        {
            get { return _sourcedatasourcename; }
            set { SetProperty(ref _sourcedatasourcename, value); }
        }

        private string _destinationdatasourcename;
        public string DestinationDataSourceName
        {
            get { return _destinationdatasourcename; }
            set { SetProperty(ref _destinationdatasourcename, value); }
        }


        private string _sourcekeyfield;
        public string SourceKeyField
        {
            get { return _sourcekeyfield; }
            set { SetProperty(ref _sourcekeyfield, value); }
        }

        private string _destinationkeyfield;
        public string DestinationKeyField
        {
            get { return _destinationkeyfield; }
            set { SetProperty(ref _destinationkeyfield, value); }
        }

        private string _sourcesyncdatafield;
        public string SourceSyncDataField
        {
            get { return _sourcesyncdatafield; }
            set { SetProperty(ref _sourcesyncdatafield, value); }
        }

        private string _destinationyncdatafield;
        public string DestinationSyncDataField
        {
            get { return _destinationyncdatafield; }
            set { SetProperty(ref _destinationyncdatafield, value); }
        }

        private DateTime _lastsyncdate;
        public DateTime LastSyncDate
        {
            get { return _lastsyncdate; }
            set { SetProperty(ref _lastsyncdate, value); }
        }

        private string _synctype;
        public string SyncType
        {
            get { return _synctype; }
            set { SetProperty(ref _synctype, value); }
        }

        private string _syncdirection;
        public string SyncDirection
        {
            get { return _syncdirection; }
            set { SetProperty(ref _syncdirection, value); }
        }

        private string _syncfrequency;
        public string SyncFrequency
        {
            get { return _syncfrequency; }
            set { SetProperty(ref _syncfrequency, value); }
        }

        private int _batchsize;
        public int BatchSize
        {
            get { return _batchsize; }
            set { SetProperty(ref _batchsize, value); }
        }

        private bool _runpreflight;
        public bool RunPreflight
        {
            get { return _runpreflight; }
            set { SetProperty(ref _runpreflight, value); }
        }

        private string _conflictresolutionstrategy = "SourceWins";
        public string ConflictResolutionStrategy
        {
            get { return _conflictresolutionstrategy; }
            set { SetProperty(ref _conflictresolutionstrategy, value); }
        }

        private bool _createdestinationifnotexists;
        public bool CreateDestinationIfNotExists
        {
            get { return _createdestinationifnotexists; }
            set { SetProperty(ref _createdestinationifnotexists, value); }
        }

        private bool _addmissingcolumns;
        public bool AddMissingColumns
        {
            get { return _addmissingcolumns; }
            set { SetProperty(ref _addmissingcolumns, value); }
        }

        private string _driftpolicy = "Ignore";
        public string DriftPolicy
        {
            get { return _driftpolicy; }
            set { SetProperty(ref _driftpolicy, value); }
        }

        private string _syncstatus;
        public string SyncStatus
        {
            get { return _syncstatus; }
            set { SetProperty(ref _syncstatus, value); }
        }

        private string _syncstatusmessage;
        public string SyncStatusMessage
        {
            get { return _syncstatusmessage; }
            set { SetProperty(ref _syncstatusmessage, value); }
        }

        private ObservableBindingList<AppFilter> _filters;
        public ObservableBindingList<AppFilter> Filters
        {
            get { return _filters; }
            set { SetProperty(ref _filters, value); }
        }

        private ObservableBindingList<FieldSyncData> _mappedfields;
        public ObservableBindingList<FieldSyncData> MappedFields
        {
            get { return _mappedfields; }
            set { SetProperty(ref _mappedfields, value); }
        }


        private SyncRunData _lastsyncrundata;
        public SyncRunData LastSyncRunData
        {
            get { return _lastsyncrundata; }
            set { SetProperty(ref _lastsyncrundata, value); }
        }

        private ObservableBindingList<SyncRunData> _syncruns;
        public ObservableBindingList<SyncRunData> SyncRuns
        {
            get { return _syncruns; }
            set { SetProperty(ref _syncruns, value); }
        }

        // ── Integration policies (Phase 1) ──────────────────────────────────────

        private SyncRulePolicy _rulepolicy;
        /// <summary>Optional Rule Engine wiring for this schema. Null = Rule Engine disabled.</summary>
        public SyncRulePolicy RulePolicy
        {
            get { return _rulepolicy; }
            set { SetProperty(ref _rulepolicy, value); }
        }

        private SyncDefaultsPolicy _defaultspolicy;
        /// <summary>Optional DefaultsManager wiring for this schema. Null = Defaults disabled.</summary>
        public SyncDefaultsPolicy DefaultsPolicy
        {
            get { return _defaultspolicy; }
            set { SetProperty(ref _defaultspolicy, value); }
        }

        private SyncMappingPolicy _mappingpolicy;
        /// <summary>Optional MappingManager wiring for this schema. Null = Mapping integration disabled.</summary>
        public SyncMappingPolicy MappingPolicy
        {
            get { return _mappingpolicy; }
            set { SetProperty(ref _mappingpolicy, value); }
        }

        // ── Schema versioning (Phase 2) ──────────────────────────────────────────

        private SyncSchemaVersion _currentschemaversion;
        /// <summary>Most recent version artifact stamped when this schema was last saved or promoted.</summary>
        public SyncSchemaVersion CurrentSchemaVersion
        {
            get { return _currentschemaversion; }
            set { SetProperty(ref _currentschemaversion, value); }
        }

        // ── Incremental / CDC (Phase 3) ──────────────────────────────────────────

        private WatermarkPolicy _watermarkpolicy;
        /// <summary>
        /// Optional incremental-sync policy.  When set, the orchestrator uses watermark-based
        /// filtering instead of a full table load.  Null = full load (default).
        /// </summary>
        public WatermarkPolicy WatermarkPolicy
        {
            get { return _watermarkpolicy; }
            set { SetProperty(ref _watermarkpolicy, value); }
        }

        // ── Bidirectional conflict resolution (Phase 4) ──────────────────────────

        private ConflictPolicy _conflictpolicy;
        /// <summary>
        /// Optional conflict-resolution policy for bidirectional syncs.
        /// When null the legacy <see cref="ConflictResolutionStrategy"/> string drives behaviour.
        /// </summary>
        public ConflictPolicy ConflictPolicy
        {
            get { return _conflictpolicy; }
            set { SetProperty(ref _conflictpolicy, value); }
        }

        // ── Reliability / Retry & Idempotency (Phase 5) ──────────────────────────

        private RetryPolicy _retrypolicy;
        /// <summary>
        /// Optional retry/backoff policy for transient failures.
        /// When null the sync will not be retried on failure.
        /// </summary>
        public RetryPolicy RetryPolicy
        {
            get { return _retrypolicy; }
            set { SetProperty(ref _retrypolicy, value); }
        }

        private SyncCheckpoint _activecheckpoint;
        /// <summary>
        /// The most recently saved checkpoint for this schema.
        /// Populated during a run when <see cref="RetryPolicy.CheckpointEnabled"/> is true.
        /// </summary>
        public SyncCheckpoint ActiveCheckpoint
        {
            get { return _activecheckpoint; }
            set { SetProperty(ref _activecheckpoint, value); }
        }

        // ── Data Quality & Reconciliation (Phase 6) ───────────────────────────────

        private DqPolicy _dqpolicy;
        /// <summary>
        /// Optional Data Quality gate policy.
        /// When set, each record passes through RuleEngine DQ checks before being written
        /// to the destination.  Failures are routed to the reject channel.
        /// </summary>
        public DqPolicy DqPolicy
        {
            get { return _dqpolicy; }
            set { SetProperty(ref _dqpolicy, value); }
        }

        private SyncReconciliationReport _lastreconciliationreport;
        /// <summary>
        /// The reconciliation report produced by the most recent sync run.
        /// Null when no run has completed yet.
        /// </summary>
        public SyncReconciliationReport LastReconciliationReport
        {
            get { return _lastreconciliationreport; }
            set { SetProperty(ref _lastreconciliationreport, value); }
        }

        // ── Phase 7: Observability / SLO ─────────────────────────────────────────

        private SloProfile _sloprofile;
        /// <summary>
        /// SLO thresholds and alert rule keys for this schema.
        /// When set, SLO metrics and alert trigger rules are evaluated at the end of every run.
        /// </summary>
        public SloProfile SloProfile
        {
            get { return _sloprofile; }
            set { SetProperty(ref _sloprofile, value); }
        }

        private System.Collections.Generic.List<SyncAlertRecord> _lastrunalerts;
        /// <summary>
        /// Alert records emitted during the most recent sync run.
        /// Reset at the start of each run.
        /// </summary>
        public System.Collections.Generic.List<SyncAlertRecord> LastRunAlerts
        {
            get { return _lastrunalerts; }
            set { SetProperty(ref _lastrunalerts, value); }
        }

        // ── Phase 8: Performance & Scale ─────────────────────────────────────────

        private SyncPerformanceProfile _perfprofile;
        /// <summary>
        /// Performance and scale knobs for this schema's sync runs.
        /// Controls batch size, parallelism degree, rule policy mode, and cache TTLs.
        /// </summary>
        public SyncPerformanceProfile PerfProfile
        {
            get { return _perfprofile; }
            set { SetProperty(ref _perfprofile, value); }
        }

        public DataSyncSchema()
        {
            Id = Guid.NewGuid().ToString();
            MappedFields = new ObservableBindingList<FieldSyncData>();
            SyncRuns = new ObservableBindingList<SyncRunData>();
            Filters = new ObservableBindingList<AppFilter>();

        }

     
    }
    public class FieldSyncData : Entity
    {
        private string _id;
      //  [JsonProperty("ID")]
        public string Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }
        private string _sourcefield;
        public string SourceField
        {
            get { return _sourcefield; }
            set { SetProperty(ref _sourcefield, value); }
        }

        private string _destinationfield;
        public string DestinationField
        {
            get { return _destinationfield; }
            set { SetProperty(ref _destinationfield, value); }
        }

        private string _sourcefieldtype;
        public string SourceFieldType
        {
            get { return _sourcefieldtype; }
            set { SetProperty(ref _sourcefieldtype, value); }
        }

        private string _destinationfieldtype;
        public string DestinationFieldType
        {
            get { return _destinationfieldtype; }
            set { SetProperty(ref _destinationfieldtype, value); }
        }

        private string _sourcefieldformat;
        public string SourceFieldFormat
        {
            get { return _sourcefieldformat; }
            set { SetProperty(ref _sourcefieldformat, value); }
        }

        private string _destinationfieldformat;
        public string DestinationFieldFormat
        {
            get { return _destinationfieldformat; }
            set { SetProperty(ref _destinationfieldformat, value); }
        }
        public FieldSyncData()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
    public class SyncRunData : Entity
    {

        private string _id;
      //  [JsonProperty("ID")]
        public string Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _syncschemaid;
      //  [JsonProperty("SyncSchemaID")]
        public string SyncSchemaId
        {
            get { return _syncschemaid; }
            set { SetProperty(ref _syncschemaid, value); }
        }

        private DateTime _syncdate;
        public DateTime SyncDate
        {
            get { return _syncdate; }
            set { SetProperty(ref _syncdate, value); }
        }

        private string _syncstatus;
        public string SyncStatus
        {
            get { return _syncstatus; }
            set { SetProperty(ref _syncstatus, value); }
        }

        private string _syncstatusmessage;
        public string SyncStatusMessage
        {
            get { return _syncstatusmessage; }
            set { SetProperty(ref _syncstatusmessage, value); }
        }
        public SyncRunData()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
