
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.WebAPI.Helpers;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Core partial containing only state (properties/fields), constructor and disposal logic.
    /// All IDataSource method implementations are split into separate partial skeleton files.
    /// </summary>
    public partial class WebAPIDataSource : IWebAPIDataSource, IDisposable
    {
        /// <summary>
        /// Delimiter used to separate columns in queries or data representations.
        /// </summary>
        public string ColumnDelimiter { get; set; } = "\"";
        /// <summary>
        /// Delimiter used for parameters in queries.
        /// </summary>
        public string ParameterDelimiter { get; set; } = ":";
        /// <summary>
        /// Unique identifier for the data source.
        /// </summary>
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Type of the data source.
        /// </summary>
        public DataSourceType DatasourceType { get; set; } = DataSourceType.WebApi;
        /// <summary>
        /// Category of the data source.
        /// </summary>
        public DatasourceCategory Category { get; set; } = DatasourceCategory.WEBAPI;
        /// <summary>
        /// Data connection interface.
        /// </summary>
        public IDataConnection Dataconnection { get; set; }
        /// <summary>
        /// Name of the data source.
        /// </summary>
        public string DatasourceName { get; set; }
        /// <summary>
        /// Error handling object.
        /// </summary>
        public IErrorsInfo ErrorObject { get; set; }
        /// <summary>
        /// Secondary identifier for the data source.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Logger for data management activities.
        /// </summary>
        public IDMLogger Logger { get; set; }
        /// <summary>
        /// List of entity names in the data source.
        /// </summary>
        public List<string> EntitiesNames { get; set; } = new();
        /// <summary>
        /// List of entity structures.
        /// </summary>
        public List<EntityStructure> Entities { get; set; } = new();
        /// <summary>
        /// Data manipulation and exploration editor.
        /// </summary>
        public IDMEEditor DMEEditor { get; set; }
        /// <summary>
        /// Current connection status.
        /// </summary>
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        /// <summary>
        /// Event raised when a specific action or event is passed.
        /// </summary>
        public event EventHandler<PassedArgs> PassEvent;
        // IWebAPIDataSource specific
        /// <summary>
        /// List of entity fields.
        /// </summary>
        //public List<EntityField> Fields { get; set; } = new();
        /// <summary>
        /// API key for authentication.
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// Resource endpoint.
        /// </summary>
        public string Resource { get; set; }
        /// <summary>
        /// Parameters for the API request.
        /// </summary>
        public Dictionary<string,string> Parameters { get; set; } = new();

        #region Helper Fields
        private HttpClient _httpClient; // shared client
        private WebAPIConfigurationHelper _configHelper;
        private WebAPIAuthenticationHelper _authHelper;
        private WebAPIRequestHelper _requestHelper;
        private WebAPICacheHelper _cacheHelper;
        private WebAPIDataHelper _dataHelper;
        private WebAPIRateLimitHelper _rateLimitHelper;
        private WebAPISchemaHelper _schemaHelper;
        private WebAPIErrorHelper _errorHelper;
        #endregion

        /// <summary>
        /// Initializes a new instance of the WebAPIDataSource class.
        /// </summary>
        /// <param name="datasourcename">Name of the data source.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="dmeEditor">DME editor instance.</param>
        /// <param name="databasetype">Type of the data source.</param>
        /// <param name="errorObject">Error object.</param>
        public WebAPIDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            DMEEditor = dmeEditor;
            DatasourceType = databasetype;
            ErrorObject = errorObject ?? new ErrorsInfo();
            Category = DatasourceCategory.WEBAPI;

            Dataconnection = new WebAPIDataConnection()
            {
                Logger = logger,
                ErrorObject = ErrorObject,
                DMEEditor = dmeEditor
            };
            Dataconnection.ConnectionProp = dmeEditor?.ConfigEditor?.DataConnections?.FirstOrDefault(c => c.ConnectionName.Equals(datasourcename, StringComparison.InvariantCultureIgnoreCase));

            _configHelper = new WebAPIConfigurationHelper(Dataconnection.ConnectionProp, Logger, DatasourceName);
            
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMilliseconds(_configHelper.TimeoutMs);
            
            _authHelper = new WebAPIAuthenticationHelper(Dataconnection.ConnectionProp, Logger, _httpClient);
            _errorHelper = new WebAPIErrorHelper(Logger, DatasourceName);
            _requestHelper = new WebAPIRequestHelper(_httpClient, Logger, DatasourceName, _errorHelper, _configHelper.MaxConcurrentRequests, _configHelper.MaxRetries, _configHelper.RetryDelayMs);
            _cacheHelper = new WebAPICacheHelper(Logger, DatasourceName, _configHelper.CacheDurationMinutes);
            _dataHelper = new WebAPIDataHelper(Logger, DatasourceName);
            _rateLimitHelper = new WebAPIRateLimitHelper(Logger, DatasourceName);
            _schemaHelper = new WebAPISchemaHelper(Logger, DatasourceName);
        }
  

        #region Dispose Pattern
        private bool _disposed;
        /// <summary>
        /// Disposes the resources used by the WebAPIDataSource.
        /// </summary>
        /// <param name="disposing">True to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                    _schemaHelper?.Dispose();
                    _cacheHelper?.Dispose();
                    _errorHelper?.Dispose();
                    _rateLimitHelper?.Dispose();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Disposes the WebAPIDataSource.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}