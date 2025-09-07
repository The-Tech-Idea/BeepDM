using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using System.Data;
using TheTechIdea.Beep.DataBase;
using System.Net.Http;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using System.ComponentModel;
using System.Net;
using TheTechIdea.Beep.WebAPI.Helpers;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Main partial class for WebAPI Data Source implementation using helper classes architecture
    /// Handles core IDataSource implementation, initialization, and configuration
    /// </summary>
    public partial class WebAPIDataSource : IDataSource
    {
        #region Events and Properties
        
        /// <summary>Event raised when operations pass messages</summary>
        public event EventHandler<PassedArgs> PassEvent;
        
        /// <summary>Type of data source</summary>
        public DataSourceType DatasourceType { get; set; }
        
        /// <summary>Category of data source</summary>
        public DatasourceCategory Category { get; set; }
        
        /// <summary>Data connection instance</summary>
        public IDataConnection Dataconnection { get; set; }
        
        /// <summary>Name of the data source</summary>
        public string DatasourceName { get; set; }
        
        /// <summary>Error handling object</summary>
        public IErrorsInfo ErrorObject { get; set; }
        
        /// <summary>Unique identifier</summary>
        public string Id { get; set; }
        
        /// <summary>Logger instance</summary>
        public IDMLogger Logger { get; set; }
        
        /// <summary>List of entity names</summary>
        public List<string> EntitiesNames { get; set; }
        
        /// <summary>List of entity structures</summary>
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        
        /// <summary>Data management editor instance</summary>
        public IDMEEditor DMEEditor { get; set; }
        
        /// <summary>Records collection</summary>
        public List<object> Records { get; set; }
        
        /// <summary>Connection status</summary>
        public ConnectionState ConnectionStatus { get; set; }
        
        /// <summary>Column delimiter for queries</summary>
        public virtual string ColumnDelimiter { get; set; } = "''";
        
        /// <summary>Parameter delimiter</summary>
        public virtual string ParameterDelimiter { get; set; } = ":";
        
        /// <summary>HTTP client for API calls</summary>
        public HttpClient client { get; set; }
        
        /// <summary>Unique GUID identifier</summary>
        public string GuidID { get; set; } = Guid.NewGuid().ToString();

        #endregion

        #region Private Fields - Helper Classes

        private readonly WebAPIDataConnection cn;
        private readonly WebAPIAuthenticationHelper _authHelper;
        private readonly WebAPIRequestHelper _requestHelper;
        private readonly WebAPICacheHelper _cacheHelper;
        private readonly WebAPIDataHelper _dataHelper;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of WebAPIDataSource with helper classes
        /// </summary>
        public WebAPIDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            // Initialize basic properties
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.WEBAPI;
            
            // Initialize connection
            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,
                DMEEditor = pDMEEditor
            };
            
            // Get connection properties
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections
                .Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            
            cn = (WebAPIDataConnection)Dataconnection;
            
            // Initialize HTTP client with optimizations
            InitializeHttpClient();
            
            // Initialize helper classes
            _authHelper = new WebAPIAuthenticationHelper(Dataconnection.ConnectionProp, Logger, client);
            _requestHelper = new WebAPIRequestHelper(client, Logger, DatasourceName, 
                GetConfigurationValue("MaxConcurrentRequests", 10),
                GetConfigurationValue("RetryCount", 3),
                GetConfigurationValue("RetryDelayMs", 1000));
            _cacheHelper = new WebAPICacheHelper(Logger, DatasourceName, GetConfigurationValue("CacheExpiryMinutes", 15));
            _dataHelper = new WebAPIDataHelper(Logger, DatasourceName);
            
            // Open connection
            try
            {
                cn.OpenConnection();
                ConnectionStatus = cn.ConnectionStatus;
                
                Logger?.WriteLog($"WebAPI DataSource {DatasourceName} initialized successfully");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Failed to initialize WebAPI DataSource {DatasourceName}: {ex.Message}");
                ConnectionStatus = ConnectionState.Broken;
            }
        }

        #endregion

        #region Initialization

        private void InitializeHttpClient()
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            
            var timeout = GetConfigurationValue("TimeoutMs", 30000);
            client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(timeout)
            };
            
            // Set default headers
            var userAgent = GetConfigurationValue("UserAgent", "BeepDM-WebAPI/1.0");
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Add("Accept", "application/json, application/xml, text/plain, */*");
            
            if (GetConfigurationValue("EnableCompression", true))
            {
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            }
        }

        private T GetConfigurationValue<T>(string paramName, T defaultValue)
        {
            try
            {
                if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps)
                {
                    var propertyInfo = typeof(WebAPIConnectionProperties).GetProperty(paramName);
                    if (propertyInfo != null)
                    {
                        var value = propertyInfo.GetValue(webApiProps);
                        if (value != null)
                        {
                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                    }
                }
                
                // Try to get from Parameters string
                if (!string.IsNullOrEmpty(Dataconnection?.ConnectionProp?.Parameters))
                {
                    var parameters = ParseParameters(Dataconnection.ConnectionProp.Parameters);
                    if (parameters.ContainsKey(paramName))
                    {
                        return (T)Convert.ChangeType(parameters[paramName], typeof(T));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting configuration value {paramName}: {ex.Message}");
            }

            return defaultValue;
        }

        private Dictionary<string, string> ParseParameters(string parametersString)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrEmpty(parametersString))
                return parameters;

            var pairs = parametersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            return parameters;
        }

        #endregion

        #region Helper Methods

        private Dictionary<string, string> GetCustomHeaders()
        {
            var headers = new Dictionary<string, string>();

            try
            {
                var customHeaders = GetConfigurationValue("CustomHeaders", "");
                if (!string.IsNullOrEmpty(customHeaders))
                {
                    var headerPairs = customHeaders.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pair in headerPairs)
                    {
                        var keyValue = pair.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                        if (keyValue.Length == 2)
                        {
                            headers[keyValue[0].Trim()] = keyValue[1].Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error parsing custom headers: {ex.Message}");
            }

            return headers;
        }

        #endregion

        #region Disposal

        /// <summary>Disposes resources properly</summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _authHelper?.Dispose();
                _requestHelper?.Dispose();
                _cacheHelper?.Dispose();
                client?.Dispose();
            }
        }

        /// <summary>Disposes the WebAPI data source</summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
