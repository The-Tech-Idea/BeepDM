using System;
using TheTechIdea.Beep.Editor;


namespace DataManagementModels.DriversConfigurations
{
    public class ConnectionDriversTypes : Entity
    {
        public ConnectionDriversTypes()
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

        private Type _adaptertype;
        public Type AdapterType
        {
            get { return _adaptertype; }
            set { SetProperty(ref _adaptertype, value); }
        }

        private Type _commandbuildertype;
        public Type CommandBuilderType
        {
            get { return _commandbuildertype; }
            set { SetProperty(ref _commandbuildertype, value); }
        }

        private Type _dbconnectiontype;
        public Type DbConnectionType
        {
            get { return _dbconnectiontype; }
            set { SetProperty(ref _dbconnectiontype, value); }
        }
    }
}
