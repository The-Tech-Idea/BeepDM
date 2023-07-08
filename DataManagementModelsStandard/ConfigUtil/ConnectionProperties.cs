using DataManagementModels.ConfigUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Util
{
    public class ConnectionProperties : Entity,IConnectionProperties
    {

        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _connectionname;
        public string ConnectionName
        {
            get { return _connectionname; }
            set { SetProperty(ref _connectionname, value); }
        }

        private string _userid;
        public string UserID
        {
            get { return _userid; }
            set { SetProperty(ref _userid, value); }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        private string _connectionstring;
        public string ConnectionString
        {
            get { return _connectionstring; }
            set { SetProperty(ref _connectionstring, value); }
        }

        private string _host;
        public string Host
        {
            get { return _host; }
            set { SetProperty(ref _host, value); }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            set { SetProperty(ref _port, value); }
        } 

        private string _database;
        public string Database
        {
            get { return _database; }
            set { SetProperty(ref _database, value); }
        }

        private string _parameters;
        public string Parameters
        {
            get { return _parameters; }
            set { SetProperty(ref _parameters, value); }
        }

        private string _schemaname;
        public string SchemaName
        {
            get { return _schemaname; }
            set { SetProperty(ref _schemaname, value); }
        }

        private string _oraclesidorservice;
        public string OracleSIDorService
        {
            get { return _oraclesidorservice; }
            set { SetProperty(ref _oraclesidorservice, value); }
        }

        private char _delimiter;
        public char Delimiter
        {
            get { return _delimiter; }
            set { SetProperty(ref _delimiter, value); }
        }

        private string _ext;
        public string Ext
        {
            get { return _ext; }
            set { SetProperty(ref _ext, value); }
        }

        private DataSourceType _databasetype;
        public DataSourceType DatabaseType
        {
            get { return _databasetype; }
            set { SetProperty(ref _databasetype, value); }
        }

        private DatasourceCategory _category;
        public DatasourceCategory Category
        {
            get { return _category; }
            set { SetProperty(ref _category, value); }
        }

        private string _drivername;
        public string DriverName
        {
            get { return _drivername; }
            set { SetProperty(ref _drivername, value); }
        }

        private string _driverversion;
        public string DriverVersion
        {
            get { return _driverversion; }
            set { SetProperty(ref _driverversion, value); }
        }

        private string _filepath;
        public string FilePath
        {
            get { return _filepath; }
            set { SetProperty(ref _filepath, value); }
        }

        private string _filename;
        public string FileName
        {
            get { return _filename; }
            set { SetProperty(ref _filename, value); }
        }

        private bool _drawn;
        public bool Drawn
        {
            get { return _drawn; }
            set { SetProperty(ref _drawn, value); }
        }  

        private string _certificatepath;
        public string CertificatePath
        {
            get { return _certificatepath; }
            set { SetProperty(ref _certificatepath, value); }
        }

        private string _url;
        public string Url
        {
            get { return _url; }
            set { SetProperty(ref _url, value); }
        }
        public List<string> Databases { get; set; } = new List<string>();
        public string ApiKey { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();

        private string _keytoken;
        public string KeyToken
        {
            get { return _keytoken; }
            set { SetProperty(ref _keytoken, value); }
        }
        public List<WebApiHeader> Headers { get; set; } = new List<WebApiHeader>();

        private bool _compositelayer;
        public bool CompositeLayer
        {
            get { return _compositelayer; }
            set { SetProperty(ref _compositelayer, value); }
        }  

        private string _compositelayername;
        public string CompositeLayerName
        {
            get { return _compositelayername; }
            set { SetProperty(ref _compositelayername, value); }
        }
        public List<DefaultValue> DatasourceDefaults { get; set; } = new List<DefaultValue>();

        private bool _favourite;
        public bool Favourite
        {
            get { return _favourite; }
            set { SetProperty(ref _favourite, value); }
        }  

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        } 

        private bool _islocal;
        public bool IsLocal
        {
            get { return _islocal; }
            set { SetProperty(ref _islocal, value); }
        }  

        private bool _isremote;
        public bool IsRemote
        {
            get { return _isremote; }
            set { SetProperty(ref _isremote, value); }
        }  

        private bool _iswebapi;
        public bool IsWebApi
        {
            get { return _iswebapi; }
            set { SetProperty(ref _iswebapi, value); }
        }  

        private bool _isfile;
        public bool IsFile
        {
            get { return _isfile; }
            set { SetProperty(ref _isfile, value); }
        }  

        private bool _isdatabase;
        public bool IsDatabase
        {
            get { return _isdatabase; }
            set { SetProperty(ref _isdatabase, value); }
        }  

        private bool _iscomposite;
        public bool IsComposite
        {
            get { return _iscomposite; }
            set { SetProperty(ref _iscomposite, value); }
        }  

        private bool _iscloud;
        public bool IsCloud
        {
            get { return _iscloud; }
            set { SetProperty(ref _iscloud, value); }
        }  

        private bool _isfavourite;
        public bool IsFavourite
        {
            get { return _isfavourite; }
            set { SetProperty(ref _isfavourite, value); }
        }  

        private bool _isdefault;
        public bool IsDefault
        {
            get { return _isdefault; }
            set { SetProperty(ref _isdefault, value); }
        }  

        private bool _isinmemory;
        public bool IsInMemory
        {
            get { return _isinmemory; }
            set { SetProperty(ref _isinmemory, value); }
        }  
        public ConnectionProperties()
        {
            GuidID = Guid.NewGuid().ToString();
        }


    }

    public class WebApiHeader
    {
        private IDMEEditor pDMEEditor;
        private IDataConnection pConn;
        private List<EntityField> pfields;

        public WebApiHeader(string datasourcename, string databasename)
        {

        }
        public WebApiHeader()
        {

        }
        public WebApiHeader(string datasourcename, string databasename, IDMEEditor pDMEEditor, IDataConnection pConn, List<EntityField> pfields) : this(datasourcename, databasename)
        {
            this.pDMEEditor = pDMEEditor;
            this.pConn = pConn;
            this.pfields = pfields;
        }

        public string headername { get; set; }
        public string headervalue { get; set; }
    }

    public class ConnectionList
    {
        public string ID { get; set; }
        public ConnectionList()
        {

        }
        public List<ConnectionProperties> Connections { get; set; } = new List<ConnectionProperties>();
        public DatasourceCategory DataSourceCategory { get; set; }

    }
}
