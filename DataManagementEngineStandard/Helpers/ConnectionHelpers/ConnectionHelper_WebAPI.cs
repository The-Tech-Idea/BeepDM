using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Partial class for Web API and Web Service connection configurations
    /// </summary>
    public static partial class ConnectionHelper
    {
        /// <summary>
        /// Gets all Web API and Web Service connection configurations
        /// </summary>
        /// <returns>List of Web API and Web Service connection configurations</returns>
        public static List<ConnectionDriversConfig> GetWebAPIConfigs()
        {
            var configs = new List<ConnectionDriversConfig>
            {
                CreateRestApiConfig(),
                CreateGraphQLConfig(),
                CreateODataConfig(),
                CreateWebApiConfig(),
                CreateOPCConfig(),
                CreateSOAPConfig(),
                CreateXMLRPCConfig(),
                CreateJSONRPCConfig(),
                CreateODBCConfig(),
                CreateOLEDBConfig(),
                CreateADOConfig(),
                CreateGRPCConfig(),
                CreateWebSocketConfig(),
                CreateSSEConfig()
            };

            return configs;
        }

        /// <summary>Creates a configuration object for REST API connection drivers.</summary>
        /// <returns>A configuration object for REST API connection drivers.</returns>
        public static ConnectionDriversConfig CreateRestApiConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "RestSharp",
                DriverClass = "RestSharp",
                version = "108.0.0.0",
                dllname = "RestSharp.dll",
                ConnectionString = "BaseUrl={Url};ApiKey={Password};",
                iconname = "restapi.svg",
                classHandler = "RestApiDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.RestApi,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for GraphQL connection drivers.</summary>
        /// <returns>A configuration object for GraphQL connection drivers.</returns>
        public static ConnectionDriversConfig CreateGraphQLConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "GraphQL.Client",
                DriverClass = "GraphQL.Client",
                version = "5.0.0.0",
                dllname = "GraphQL.Client.dll",
                ConnectionString = "Endpoint={Url};ApiKey={Password};",
                iconname = "graphql.svg",
                classHandler = "GraphQLDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.GraphQL,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for OData connection drivers.</summary>
        /// <returns>A configuration object for OData connection drivers.</returns>
        public static ConnectionDriversConfig CreateODataConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Microsoft.OData.Client",
                DriverClass = "Microsoft.OData.Client",
                version = "7.0.0.0",
                dllname = "Microsoft.OData.Client.dll",
                ConnectionString = "ServiceRoot={Url};",
                iconname = "odata.svg",
                classHandler = "ODataDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.OData,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Web API connection drivers.</summary>
        /// <returns>A configuration object for Web API connection drivers.</returns>
        public static ConnectionDriversConfig CreateWebApiConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "System.Net.Http",
                DriverClass = "System.Net.Http",
                version = "4.3.0.0",
                dllname = "System.Net.Http.dll",
                ConnectionString = "BaseUrl={Url};ApiKey={Password};",
                iconname = "webapi.svg",
                classHandler = "WebApiDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = true,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.WebApi,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for OPC connection drivers.</summary>
        /// <returns>A ConnectionDriversConfig object representing the OPC configuration.</returns>
        public static ConnectionDriversConfig CreateOPCConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = "opc-guid",
                PackageName = "OPC",
                DriverClass = "OPC",
                version = "2.0.0.0",
                dllname = "OPC.dll",
                AdapterType = "OPC.OPCDataAdapter",
                CommandBuilderType = "OPC.OPCCommandBuilder",
                DbConnectionType = "OPC.OPCConnection",
                ConnectionString = "Server={host};Port={port};Node={database};",
                iconname = "opc.svg",
                classHandler = "OPCDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.OPC,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for SOAP Web Service connection drivers.</summary>
        /// <returns>A configuration object for SOAP Web Service connection drivers.</returns>
        public static ConnectionDriversConfig CreateSOAPConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "System.ServiceModel",
                DriverClass = "System.ServiceModel",
                version = "4.8.0.0",
                dllname = "System.ServiceModel.dll",
                ConnectionString = "Endpoint={Url};Username={UserID};Password={Password};",
                iconname = "soap.svg",
                classHandler = "SOAPDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.SOAP,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for XML-RPC connection drivers.</summary>
        /// <returns>A configuration object for XML-RPC connection drivers.</returns>
        public static ConnectionDriversConfig CreateXMLRPCConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "CookComputing.XmlRpc",
                DriverClass = "CookComputing.XmlRpc",
                version = "3.0.0.0",
                dllname = "CookComputing.XmlRpc.dll",
                ConnectionString = "Url={Url};",
                iconname = "xmlrpc.svg",
                classHandler = "XMLRPCDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.XML,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for JSON-RPC connection drivers.</summary>
        /// <returns>A configuration object for JSON-RPC connection drivers.</returns>
        public static ConnectionDriversConfig CreateJSONRPCConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "StreamJsonRpc",
                DriverClass = "StreamJsonRpc",
                version = "2.10.0.0",
                dllname = "StreamJsonRpc.dll",
                ConnectionString = "Endpoint={Url};",
                iconname = "jsonrpc.svg",
                classHandler = "JSONRPCDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.Json,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for ODBC connection drivers.</summary>
        /// <returns>A configuration object for ODBC connection drivers.</returns>
        public static ConnectionDriversConfig CreateODBCConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "System.Data.Odbc",
                DriverClass = "System.Data.Odbc",
                version = "6.0.0.0",
                dllname = "System.Data.Odbc.dll",
                AdapterType = "System.Data.Odbc.OdbcDataAdapter",
                CommandBuilderType = "System.Data.Odbc.OdbcCommandBuilder",
                DbConnectionType = "System.Data.Odbc.OdbcConnection",
                DbTransactionType = "System.Data.Odbc.OdbcTransaction",
                ConnectionString = "DSN={DataSourceName};UID={UserID};PWD={Password};",
                iconname = "odbc.svg",
                classHandler = "ODBCDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.ODBC,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for OLEDB connection drivers.</summary>
        /// <returns>A configuration object for OLEDB connection drivers.</returns>
        public static ConnectionDriversConfig CreateOLEDBConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "System.Data.OleDb",
                DriverClass = "System.Data.OleDb",
                version = "6.0.0.0",
                dllname = "System.Data.OleDb.dll",
                AdapterType = "System.Data.OleDb.OleDbDataAdapter",
                CommandBuilderType = "System.Data.OleDb.OleDbCommandBuilder",
                DbConnectionType = "System.Data.OleDb.OleDbConnection",
                DbTransactionType = "System.Data.OleDb.OleDbTransaction",
                ConnectionString = "Provider={Provider};Data Source={DataSource};",
                iconname = "oledb.svg",
                classHandler = "OLEDBDataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.OLEDB,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for ADO connection drivers.</summary>
        /// <returns>A configuration object for ADO connection drivers.</returns>
        public static ConnectionDriversConfig CreateADOConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "System.Data.Common",
                DriverClass = "System.Data.Common",
                version = "4.3.0.0",
                dllname = "System.Data.Common.dll",
                AdapterType = "System.Data.Common.DbDataAdapter",
                CommandBuilderType = "System.Data.Common.DbCommandBuilder",
                DbConnectionType = "System.Data.Common.DbConnection",
                DbTransactionType = "System.Data.Common.DbTransaction",
                ConnectionString = "ConnectionString={ConnectionString};",
                iconname = "ado.svg",
                classHandler = "ADODataSource",
                ADOType = true,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.Connector,
                DatasourceType = DataSourceType.ADO,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for gRPC connection drivers.</summary>
        /// <returns>A configuration object for gRPC connection drivers.</returns>
        public static ConnectionDriversConfig CreateGRPCConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "Grpc.Net.Client",
                DriverClass = "Grpc.Net.Client",
                version = "2.0.0.0",
                dllname = "Grpc.Net.Client.dll",
                ConnectionString = "Address={Url};",
                iconname = "grpc.svg",
                classHandler = "GRPCDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.GRPC,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for WebSocket connection drivers.</summary>
        /// <returns>A configuration object for WebSocket connection drivers.</returns>
        public static ConnectionDriversConfig CreateWebSocketConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "System.Net.WebSockets.Client",
                DriverClass = "System.Net.WebSockets.Client",
                version = "4.3.0.0",
                dllname = "System.Net.WebSockets.Client.dll",
                ConnectionString = "Uri={Url};",
                iconname = "websocket.svg",
                classHandler = "WebSocketDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.WebSocket,
                IsMissing = false
            };
        }

        /// <summary>Creates a configuration object for Server-Sent Events connection drivers.</summary>
        /// <returns>A configuration object for Server-Sent Events connection drivers.</returns>
        public static ConnectionDriversConfig CreateSSEConfig()
        {
            return new ConnectionDriversConfig
            {
                GuidID = Guid.NewGuid().ToString(),
                PackageName = "LaunchDarkly.EventSource",
                DriverClass = "LaunchDarkly.EventSource",
                version = "4.0.0.0",
                dllname = "LaunchDarkly.EventSource.dll",
                ConnectionString = "Url={Url};",
                iconname = "sse.svg",
                classHandler = "SSEDataSource",
                ADOType = false,
                CreateLocal = false,
                InMemory = false,
                Favourite = false,
                DatasourceCategory = DatasourceCategory.WEBAPI,
                DatasourceType = DataSourceType.SSE,
                IsMissing = false
            };
        }
    }
}