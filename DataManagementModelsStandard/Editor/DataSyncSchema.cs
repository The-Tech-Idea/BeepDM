using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace DataManagementModels.Editor
{
    public class DataSyncSchema : Entity
    {

        private string _id;
        public string ID
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

        public DataSyncSchema()
        {
            ID = Guid.NewGuid().ToString();
            MappedFields = new ObservableBindingList<FieldSyncData>();
            SyncRuns = new ObservableBindingList<SyncRunData>();
            Filters = new ObservableBindingList<AppFilter>();

        }

     
    }
    public class FieldSyncData : Entity
    {
        private string _id;
        public string ID
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
            ID = Guid.NewGuid().ToString();
        }
    }
    public class SyncRunData : Entity
    {

        private string _id;
        public string ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _syncschemaid;
        public string SyncSchemaID
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
            ID = Guid.NewGuid().ToString();
        }
    }
}