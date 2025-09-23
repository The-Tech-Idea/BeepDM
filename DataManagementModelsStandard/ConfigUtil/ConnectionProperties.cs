

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;



    namespace TheTechIdea.Beep.ConfigUtil
  {  
        public class ConnectionProperties : Entity, IConnectionProperties
        {
            #region Identifiers & Naming

            private int _id;
            public int ID
            {
                get { return _id; }
                set { SetProperty(ref _id, value); }
            }

            private string _guidid = Guid.NewGuid().ToString();
            public string GuidID
            {
                get { return _guidid; }
                set { SetProperty(ref _guidid, value); }
            }

            private string _connectionname;
            public string ConnectionName
            {
                get { return _connectionname; }
                set { SetProperty(ref _connectionname, value); }
            }

            private string _compositelayername;
            public string CompositeLayerName
            {
                get { return _compositelayername; }
                set { SetProperty(ref _compositelayername, value); }
            }

            #endregion

            #region Provider & Categorization

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

            #endregion

            #region Endpoints, Files & Data Targets

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

            private string _url;
            public string Url
            {
                get { return _url; }
                set { SetProperty(ref _url, value); }
            }

            private List<string> _databases = new List<string>();
            public List<string> Databases
            {
                get { return _databases; }
                set { SetProperty(ref _databases, value); }
            }

            private List<EntityStructure> _entities = new List<EntityStructure>();
            public List<EntityStructure> Entities
            {
                get { return _entities; }
                set { SetProperty(ref _entities, value); }
            }

            private List<DefaultValue> _datasourceDefaults = new List<DefaultValue>();
            public List<DefaultValue> DatasourceDefaults
            {
                get { return _datasourceDefaults; }
                set { SetProperty(ref _datasourceDefaults, value); }
            }

            #endregion

            #region Credentials & Connection String

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

            private string _parameters;
            public string Parameters
            {
                get { return _parameters; }
                set { SetProperty(ref _parameters, value); }
            }

            private Dictionary<string, string> _additionalparams = new Dictionary<string, string>();
            public Dictionary<string, string> ParameterList
            {
                get { return _additionalparams; }
                set { SetProperty(ref _additionalparams, value); }
            }

            private string _apikey;
            public string ApiKey
            {
                get { return _apikey; }
                set { SetProperty(ref _apikey, value); }
            }

            private string _keytoken;
            public string KeyToken
            {
                get { return _keytoken; }
                set { SetProperty(ref _keytoken, value); }
            }

            #endregion

            #region Request & Behavior Settings

            private bool _drawn;
            public bool Drawn
            {
                get { return _drawn; }
                set { SetProperty(ref _drawn, value); }
            }

            private int _timeout;
            public int Timeout
            {
                get { return _timeout; }
                set { SetProperty(ref _timeout, value); }
            }

            private string _httpMethod;
            public string HttpMethod
            {
                get { return _httpMethod; }
                set { SetProperty(ref _httpMethod, value); }
            }

            private bool _favourite;
            public bool Favourite
            {
                get { return _favourite; }
                set { SetProperty(ref _favourite, value); }
            }

            private bool _readOnly;
            public bool ReadOnly
            {
                get { return _readOnly; }
                set { SetProperty(ref _readOnly, value); }
            }

            #endregion

            #region Feature Flags

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

            #endregion

            #region Certificates & SSL

            private string _certificatepath;
            public string CertificatePath
            {
                get { return _certificatepath; }
                set { SetProperty(ref _certificatepath, value); }
            }

            private bool _useSSL;
            public bool UseSSL
            {
                get { return _useSSL; }
                set { SetProperty(ref _useSSL, value); }
            }

            private bool _requireSSL;
            public bool RequireSSL
            {
                get { return _requireSSL; }
                set { SetProperty(ref _requireSSL, value); }
            }

            private bool _trustServerCertificate;
            public bool TrustServerCertificate
            {
                get { return _trustServerCertificate; }
                set { SetProperty(ref _trustServerCertificate, value); }
            }

            private bool _bypassServerCertificateValidation;
            public bool BypassServerCertificateValidation
            {
                get { return _bypassServerCertificateValidation; }
                set { SetProperty(ref _bypassServerCertificateValidation, value); }
            }

            private string _sslMode;
            public string SSLMode
            {
                get { return _sslMode; }
                set { SetProperty(ref _sslMode, value); }
            }

            private int _sslTimeout;
            public int SSLTimeout
            {
                get { return _sslTimeout; }
                set { SetProperty(ref _sslTimeout, value); }
            }

            private string _clientCertificateThumbprint;
            public string ClientCertificateThumbprint
            {
                get { return _clientCertificateThumbprint; }
                set { SetProperty(ref _clientCertificateThumbprint, value); }
            }

            private string _clientCertificateStoreLocation;
            public string ClientCertificateStoreLocation
            {
                get { return _clientCertificateStoreLocation; }
                set { SetProperty(ref _clientCertificateStoreLocation, value); }
            }

            private string _clientCertificateStoreName;
            public string ClientCertificateStoreName
            {
                get { return _clientCertificateStoreName; }
                set { SetProperty(ref _clientCertificateStoreName, value); }
            }

            private string _clientCertificateSubjectName;
            public string ClientCertificateSubjectName
            {
                get { return _clientCertificateSubjectName; }
                set { SetProperty(ref _clientCertificateSubjectName, value); }
            }

            #endregion

            #region Authentication (General, AAD, Kerberos)

            private bool _integratedSecurity;
            public bool IntegratedSecurity
            {
                get { return _integratedSecurity; }
                set { SetProperty(ref _integratedSecurity, value); }
            }

            private bool _persistSecurityInfo;
            public bool PersistSecurityInfo
            {
                get { return _persistSecurityInfo; }
                set { SetProperty(ref _persistSecurityInfo, value); }
            }

            private bool _trustedConnection;
            public bool TrustedConnection
            {
                get { return _trustedConnection; }
                set { SetProperty(ref _trustedConnection, value); }
            }

            private bool _encryptConnection;
            public bool EncryptConnection
            {
                get { return _encryptConnection; }
                set { SetProperty(ref _encryptConnection, value); }
            }

            private bool _multiSubnetFailover;
            public bool MultiSubnetFailover
            {
                get { return _multiSubnetFailover; }
                set { SetProperty(ref _multiSubnetFailover, value); }
            }

            private bool _allowPublicKeyRetrieval;
            public bool AllowPublicKeyRetrieval
            {
                get { return _allowPublicKeyRetrieval; }
                set { SetProperty(ref _allowPublicKeyRetrieval, value); }
            }

            private bool _useWindowsAuthentication;
            public bool UseWindowsAuthentication
            {
                get { return _useWindowsAuthentication; }
                set { SetProperty(ref _useWindowsAuthentication, value); }
            }

            private bool _useOAuth;
            public bool UseOAuth
            {
                get { return _useOAuth; }
                set { SetProperty(ref _useOAuth, value); }
            }

            private bool _useApiKey;
            public bool UseApiKey
            {
                get { return _useApiKey; }
                set { SetProperty(ref _useApiKey, value); }
            }

            private bool _useCertificate;
            public bool UseCertificate
            {
                get { return _useCertificate; }
                set { SetProperty(ref _useCertificate, value); }
            }

            private bool _useUserAndPassword;
            public bool UseUserAndPassword
            {
                get { return _useUserAndPassword; }
                set { SetProperty(ref _useUserAndPassword, value); }
            }

            private bool _savePassword;
            public bool SavePassword
            {
                get { return _savePassword; }
                set { SetProperty(ref _savePassword, value); }
            }

            private bool _allowLoadLocalInfile;
            public bool AllowLoadLocalInfile
            {
                get { return _allowLoadLocalInfile; }
                set { SetProperty(ref _allowLoadLocalInfile, value); }
            }

            private string _authenticationType;
            public string AuthenticationType
            {
                get { return _authenticationType; }
                set { SetProperty(ref _authenticationType, value); }
            }

            private string _authority;
            public string Authority
            {
                get { return _authority; }
                set { SetProperty(ref _authority, value); }
            }

            private string _tenantId;
            public string TenantId
            {
                get { return _tenantId; }
                set { SetProperty(ref _tenantId, value); }
            }

            private string _applicationId;
            public string ApplicationId
            {
                get { return _applicationId; }
                set { SetProperty(ref _applicationId, value); }
            }

            private string _redirectUriAuth;
            public string RedirectUriAuth
            {
                get { return _redirectUriAuth; }
                set { SetProperty(ref _redirectUriAuth, value); }
            }

            private string _resource;
            public string Resource
            {
                get { return _resource; }
                set { SetProperty(ref _resource, value); }
            }

            private string _audience;
            public string Audience
            {
                get { return _audience; }
                set { SetProperty(ref _audience, value); }
            }

            private string _additionalAuthInfo;
            public string AdditionalAuthInfo
            {
                get { return _additionalAuthInfo; }
                set { SetProperty(ref _additionalAuthInfo, value); }
            }

            private string _domain;
            public string Domain
            {
                get { return _domain; }
                set { SetProperty(ref _domain, value); }
            }

            private string _workstationID;
            public string WorkstationID
            {
                get { return _workstationID; }
                set { SetProperty(ref _workstationID, value); }
            }

            private string _kerberosServiceName;
            public string KerberosServiceName
            {
                get { return _kerberosServiceName; }
                set { SetProperty(ref _kerberosServiceName, value); }
            }

            private string _kerberosRealm;
            public string KerberosRealm
            {
                get { return _kerberosRealm; }
                set { SetProperty(ref _kerberosRealm, value); }
            }

            private string _kerberosKdc;
            public string KerberosKdc
            {
                get { return _kerberosKdc; }
                set { SetProperty(ref _kerberosKdc, value); }
            }

            private string _kerberosConfigPath;
            public string KerberosConfigPath
            {
                get { return _kerberosConfigPath; }
                set { SetProperty(ref _kerberosConfigPath, value); }
            }

            #endregion

            #region Web API Authentication (OAuth2, API Key, Certificates)

            private string _clientId;
            public string ClientId
            {
                get { return _clientId; }
                set { SetProperty(ref _clientId, value); }
            }

            private string _clientSecret;
            public string ClientSecret
            {
                get { return _clientSecret; }
                set { SetProperty(ref _clientSecret, value); }
            }

            private AuthTypeEnum _authType;
            public AuthTypeEnum AuthType
            {
                get { return _authType; }
                set { SetProperty(ref _authType, value); }
            }

            private string _authUrl;
            public string AuthUrl
            {
                get { return _authUrl; }
                set { SetProperty(ref _authUrl, value); }
            }

            private string _tokenUrl;
            public string TokenUrl
            {
                get { return _tokenUrl; }
                set { SetProperty(ref _tokenUrl, value); }
            }

            private string _scope;
            public string Scope
            {
                get { return _scope; }
                set { SetProperty(ref _scope, value); }
            }

            private string _grantType;
            public string GrantType
            {
                get { return _grantType; }
                set { SetProperty(ref _grantType, value); }
            }

            private string _apiKeyHeader;
            public string ApiKeyHeader
            {
                get { return _apiKeyHeader; }
                set { SetProperty(ref _apiKeyHeader, value); }
            }

            private string _redirectUri;
            public string RedirectUri
            {
                get { return _redirectUri; }
                set { SetProperty(ref _redirectUri, value); }
            }

            private string _authCode;
            public string AuthCode
            {
                get { return _authCode; }
                set { SetProperty(ref _authCode, value); }
            }

            private int _timeoutMs;
            public int TimeoutMs
            {
                get { return _timeoutMs; }
                set { SetProperty(ref _timeoutMs, value); }
            }

            private int _maxRetries;
            public int MaxRetries
            {
                get { return _maxRetries; }
                set { SetProperty(ref _maxRetries, value); }
            }

            private int _retryIntervalMs;
            public int RetryIntervalMs
            {
                get { return _retryIntervalMs; }
                set { SetProperty(ref _retryIntervalMs, value); }
            }

            private bool _useProxy;
            public bool UseProxy
            {
                get { return _useProxy; }
                set { SetProperty(ref _useProxy, value); }
            }

            private string _proxyUrl;
            public string ProxyUrl
            {
                get { return _proxyUrl; }
                set { SetProperty(ref _proxyUrl, value); }
            }

            private int _proxyPort;
            public int ProxyPort
            {
                get { return _proxyPort; }
                set { SetProperty(ref _proxyPort, value); }
            }

            private string _proxyUser;
            public string ProxyUser
            {
                get { return _proxyUser; }
                set { SetProperty(ref _proxyUser, value); }
            }

            private string _proxyPassword;
            public string ProxyPassword
            {
                get { return _proxyPassword; }
                set { SetProperty(ref _proxyPassword, value); }
            }

            private bool _bypassProxyOnLocal;
            public bool BypassProxyOnLocal
            {
                get { return _bypassProxyOnLocal; }
                set { SetProperty(ref _bypassProxyOnLocal, value); }
            }

            private bool _useDefaultProxyCredentials;
            public bool UseDefaultProxyCredentials
            {
                get { return _useDefaultProxyCredentials; }
                set { SetProperty(ref _useDefaultProxyCredentials, value); }
            }

            private bool _ignoreSSLErrors;
            public bool IgnoreSSLErrors
            {
                get { return _ignoreSSLErrors; }
                set { SetProperty(ref _ignoreSSLErrors, value); }
            }

            private bool _validateServerCertificate;
            public bool ValidateServerCertificate
            {
                get { return _validateServerCertificate; }
                set { SetProperty(ref _validateServerCertificate, value); }
            }

            private string _clientCertificatePath;
            public string ClientCertificatePath
            {
                get { return _clientCertificatePath; }
                set { SetProperty(ref _clientCertificatePath, value); }
            }

            private string _clientCertificatePassword;
            public string ClientCertificatePassword
            {
                get { return _clientCertificatePassword; }
                set { SetProperty(ref _clientCertificatePassword, value); }
            }

            private bool _requiresAuthentication;
            public bool RequiresAuthentication
            {
                get { return _requiresAuthentication; }
                set { SetProperty(ref _requiresAuthentication, value); }
            }

            private bool _requiresTokenRefresh;
            public bool RequiresTokenRefresh
            {
                get { return _requiresTokenRefresh; }
                set { SetProperty(ref _requiresTokenRefresh, value); }
            }

            #endregion

            #region OAuth Tokens (Runtime)

            private string _oAuthAccessToken;
            public string OAuthAccessToken
            {
                get { return _oAuthAccessToken; }
                set { SetProperty(ref _oAuthAccessToken, value); }
            }

            private string _oAuthRefreshToken;
            public string OAuthRefreshToken
            {
                get { return _oAuthRefreshToken; }
                set { SetProperty(ref _oAuthRefreshToken, value); }
            }

            private string _oAuthTokenEndpoint;
            public string OAuthTokenEndpoint
            {
                get { return _oAuthTokenEndpoint; }
                set { SetProperty(ref _oAuthTokenEndpoint, value); }
            }

            private string _oAuthClientSecret;
            public string OAuthClientSecret
            {
                get { return _oAuthClientSecret; }
                set { SetProperty(ref _oAuthClientSecret, value); }
            }

            private string _oAuthScope;
            public string OAuthScope
            {
                get { return _oAuthScope; }
                set { SetProperty(ref _oAuthScope, value); }
            }

            private string _oAuthGrantType;
            public string OAuthGrantType
            {
                get { return _oAuthGrantType; }
                set { SetProperty(ref _oAuthGrantType, value); }
            }

            private string _oAuthState;
            public string OAuthState
            {
                get { return _oAuthState; }
                set { SetProperty(ref _oAuthState, value); }
            }

            private string _oAuthCodeVerifier;
            public string OAuthCodeVerifier
            {
                get { return _oAuthCodeVerifier; }
                set { SetProperty(ref _oAuthCodeVerifier, value); }
            }

            private string _oAuthCodeChallenge;
            public string OAuthCodeChallenge
            {
                get { return _oAuthCodeChallenge; }
                set { SetProperty(ref _oAuthCodeChallenge, value); }
            }

            private string _oAuthCodeChallengeMethod;
            public string OAuthCodeChallengeMethod
            {
                get { return _oAuthCodeChallengeMethod; }
                set { SetProperty(ref _oAuthCodeChallengeMethod, value); }
            }

            #endregion

            #region HTTP Composition (Headers, Query/Body/Form, Files)

            private List<WebApiHeader> _headers = new List<WebApiHeader>();
            public List<WebApiHeader> Headers
            {
                get { return _headers; }
                set { SetProperty(ref _headers, value); }
            }

            private List<WebApiParameter> _queryParameters;
            public List<WebApiParameter> QueryParameters
            {
                get { return _queryParameters; }
                set { SetProperty(ref _queryParameters, value); }
            }

            private List<WebApiParameter> _bodyParameters;
            public List<WebApiParameter> BodyParameters
            {
                get { return _bodyParameters; }
                set { SetProperty(ref _bodyParameters, value); }
            }

            private List<WebApiParameter> _formParameters;
            public List<WebApiParameter> FormParameters
            {
                get { return _formParameters; }
                set { SetProperty(ref _formParameters, value); }
            }

            private List<WebApiFileParameter> _fileParameters;
            public List<WebApiFileParameter> FileParameters
            {
                get { return _fileParameters; }
                set { SetProperty(ref _fileParameters, value); }
            }

            #endregion

            #region Constructors

            public ConnectionProperties()
            {
                GuidID = Guid.NewGuid().ToString();
            }

            #endregion
        }
    

    public class WebApiFileParameter
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsMandatory { get; set; }
        public string DataType { get; set; }
        public int Order { get; set; }
    }
    public class WebApiParameter
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        public string ValidationPattern { get; set; }
        public string ExampleValue { get; set; }
        public int Order { get; set; }
    }
    public class WebApiHeader:Entity
    {
     

        public WebApiHeader(string datasourcename, string databasename)
        {

        }
        public WebApiHeader()
        {

        }

        private string _headername;
        public string Headername
        {
            get { return _headername; }
            set { SetProperty(ref _headername, value); }
        }

        private string _headervalue;
        public string Headervalue
        {
            get { return _headervalue; }
            set { SetProperty(ref _headervalue, value); }
        }

        
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