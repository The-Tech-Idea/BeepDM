using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Enhanced connection properties for Web API connections with comprehensive settings
    /// </summary>
    public class WebAPIConnectionProperties : IConnectionProperties
    {
        public int ID { get; set; }
        public string GuidID { get; set; } = System.Guid.NewGuid().ToString();
        public string ConnectionName { get; set; }
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string OracleSIDorService { get; set; }
        public DataSourceType DatabaseType { get; set; } = DataSourceType.WebApi;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.WEBAPI;
        public string DriverName { get; set; }
        public string DriverVersion { get; set; }
        public string Host { get; set; }
        public string Parameters { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string SchemaName { get; set; }
        public string UserID { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Ext { get; set; }
        public bool Drawn { get; set; }
        public string CertificatePath { get; set; }
        public string Url { get; set; }
        public string KeyToken { get; set; }
        public string ApiKey { get; set; }
        public List<string> Databases { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public List<WebApiHeader> Headers { get; set; } = new List<WebApiHeader>();
        public List<DefaultValue> DatasourceDefaults { get; set; } = new List<DefaultValue>();
        public char Delimiter { get; set; }
        public bool Favourite { get; set; }
        public bool IsLocal { get; set; }
        public bool IsRemote { get; set; } = true;
        public bool IsWebApi { get; set; } = true;
        public bool IsFile { get; set; }
        public bool IsDatabase { get; set; }
        public bool IsComposite { get; set; }
        public bool IsCloud { get; set; }
        public bool IsFavourite { get; set; }
        public bool IsDefault { get; set; }
        public bool IsInMemory { get; set; }

        // Web API specific properties
        public string AuthType { get; set; } = "none"; // none, apikey, basic, bearer, oauth2
        public string AuthUrl { get; set; }
        public string TokenUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
        public string GrantType { get; set; } = "client_credentials";
        public string ApiKeyHeader { get; set; } = "X-API-Key";
        public string RedirectUri { get; set; }
        public string AuthCode { get; set; }
        
        // Configuration settings
        public int TimeoutMs { get; set; } = 30000;
        public int RetryCount { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public int CacheExpiryMinutes { get; set; } = 15;
        public int MaxConcurrentRequests { get; set; } = 10;
        public bool EnableCaching { get; set; } = true;
        public bool EnableCompression { get; set; } = true;
        public string UserAgent { get; set; } = "BeepDM-WebAPI/1.0";

        // Rate limiting
        public int RateLimitRequestsPerMinute { get; set; } = 60;
        public bool EnableRateLimit { get; set; } = true;

        // Response handling
        public string ResponseFormat { get; set; } = "json"; // json, xml, csv, text
        public string DataPath { get; set; } // JSONPath or XPath to data in response
        public string TotalCountPath { get; set; } // Path to total count in response

        // Pagination settings
        public string PageNumberParameter { get; set; } = "page";
        public string PageSizeParameter { get; set; } = "limit";
        public int DefaultPageSize { get; set; } = 100;
        public int MaxPageSize { get; set; } = 1000;

        public WebAPIConnectionProperties()
        {
            // Set default headers
            Headers.Add(new WebApiHeader { Headername = "Accept", Headervalue = "application/json" });
            Headers.Add(new WebApiHeader { Headername = "User-Agent", Headervalue = UserAgent });
        }

        /// <summary>
        /// Gets a connection parameter value by name
        /// </summary>
        public string GetParameterValue(string paramName)
        {
            if (string.IsNullOrEmpty(Parameters))
                return null;

            var parameters = ParseParameters(Parameters);
            return parameters.ContainsKey(paramName) ? parameters[paramName] : null;
        }

        /// <summary>
        /// Sets a connection parameter value
        /// </summary>
        public void SetParameterValue(string paramName, string value)
        {
            var parameters = ParseParameters(Parameters ?? string.Empty);
            parameters[paramName] = value;
            Parameters = string.Join(";", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        private Dictionary<string, string> ParseParameters(string parametersString)
        {
            var parameters = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrEmpty(parametersString))
                return parameters;

            var pairs = parametersString.Split(';', System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2, System.StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            return parameters;
        }
    }
}
