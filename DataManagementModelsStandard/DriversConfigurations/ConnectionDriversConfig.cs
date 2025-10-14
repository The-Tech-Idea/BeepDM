using System;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DriversConfigurations
{
    public class ConnectionDriversConfig : Entity
    {
        public ConnectionDriversConfig()
        {
            GuidID = Guid.NewGuid().ToString();
        }

        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private string _packagename;
        public string PackageName
        {
            get { return _packagename; }
            set { SetProperty(ref _packagename, value); }
        }

        private string _driverclass;
        public string DriverClass
        {
            get { return _driverclass; }
            set { SetProperty(ref _driverclass, value); }
        }

        private string _version;
        public string version
        {
            get { return _version; }
            set { SetProperty(ref _version, value); }
        }

        private string _dllname;
        public string dllname
        {
            get { return _dllname; }
            set { SetProperty(ref _dllname, value); }
        }

        private string _adaptertype;
        public string AdapterType
        {
            get { return _adaptertype; }
            set { SetProperty(ref _adaptertype, value); }
        }

        private string _commandbuildertype;
        public string CommandBuilderType
        {
            get { return _commandbuildertype; }
            set { SetProperty(ref _commandbuildertype, value); }
        }

        private string _dbconnectiontype;
        public string DbConnectionType
        {
            get { return _dbconnectiontype; }
            set { SetProperty(ref _dbconnectiontype, value); }
        }

        private string _dbtransactiontype;
        public string DbTransactionType
        {
            get { return _dbtransactiontype; }
            set { SetProperty(ref _dbtransactiontype, value); }
        }

        private string _connectionstring;
        public string ConnectionString
        {
            get { return _connectionstring; }
            set { SetProperty(ref _connectionstring, value); }
        }

        private string _parameter1;
        public string parameter1
        {
            get { return _parameter1; }
            set { SetProperty(ref _parameter1, value); }
        }

        private string _parameter2;
        public string parameter2
        {
            get { return _parameter2; }
            set { SetProperty(ref _parameter2, value); }
        }

        private string _parameter3;
        public string parameter3
        {
            get { return _parameter3; }
            set { SetProperty(ref _parameter3, value); }
        }

        private string _iconname;
        public string iconname
        {
            get { return _iconname; }
            set { SetProperty(ref _iconname, value); }
        }

        private string _classhandler;
        public string classHandler
        {
            get { return _classhandler; }
            set { SetProperty(ref _classhandler, value); }
        }

        private bool _adotype;
        public bool ADOType
        {
            get { return _adotype; }
            set { SetProperty(ref _adotype, value); }
        }

        private bool _createlocal;
        public bool CreateLocal
        {
            get { return _createlocal; }
            set { SetProperty(ref _createlocal, value); }
        }

        private bool _inmemory;
        public bool InMemory
        {
            get { return _inmemory; }
            set { SetProperty(ref _inmemory, value); }
        }


        private string _extensionstohandle;
        public string extensionstoHandle
        {
            get { return _extensionstohandle; }
            set { SetProperty(ref _extensionstohandle, value); }
        }

        private bool _favourite;
        public bool Favourite
        {
            get { return _favourite; }
            set { SetProperty(ref _favourite, value); }
        }

        private DatasourceCategory _datasourcecategory;
        public DatasourceCategory DatasourceCategory
        {
            get { return _datasourcecategory; }
            set { SetProperty(ref _datasourcecategory, value); }
        }

        private DataSourceType _datasourcetype;
        public DataSourceType DatasourceType
        {
            get { return _datasourcetype; }
            set { SetProperty(ref _datasourcetype, value); }
        }
        private bool _isMissing;
        public bool IsMissing
        {
            get { return _isMissing; }
            set { SetProperty(ref _isMissing, value); }
        }
        private bool _nuggetmissing;
        public bool NuggetMissing
        {
            get { return _nuggetmissing; }
            set { SetProperty(ref _nuggetmissing, value); }
        }
        private string _nuggetversion;
        public string NuggetVersion
        {
            get { return _nuggetversion; }
            set { SetProperty(ref _nuggetversion, value); }
        }
        private string _nuggetsource;
        public string NuggetSource
        {
            get { return _nuggetsource; }
            set { SetProperty(ref _nuggetsource, value); }
        }
        
    }

}
