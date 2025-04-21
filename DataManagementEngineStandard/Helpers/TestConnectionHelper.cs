using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    public class TestConnectionHelper
    {

        /// <summary>
/// Tests a connection configuration to verify it can connect successfully
/// </summary>
/// <param name="config">The connection configuration to test</param>
/// <param name="timeout">Connection timeout in seconds</param>
/// <returns>A tuple with success flag and error message (if any)</returns>
        public static async Task<(bool success, string message)> TestConnectionAsync(ConnectionDriversConfig config, int timeout = 30)
        {
            if (config == null)
            {
                return (false, "Connection configuration is null");
            }

            try
            {
                // Handle different connection types based on data source category first
                switch (config.DatasourceCategory)
                {
                    case DatasourceCategory.RDBMS:
                        return await TestRdbmsConnectionAsync(config, timeout);

                    case DatasourceCategory.NOSQL:
                        return await TestNoSqlConnectionAsync(config, timeout);

                    case DatasourceCategory.FILE:
                        return await TestFileConnectionAsync(config, timeout);

                    case DatasourceCategory.INMEMORY:
                        return await TestInMemoryConnectionAsync(config, timeout);

                    case DatasourceCategory.STREAM:
                        return await TestStreamConnectionAsync(config, timeout);

                    case DatasourceCategory.WEBAPI:
                        return await TestWebApiConnectionAsync(config, timeout);

                    default:
                        // Fall back to DataSourceType if category doesn't provide specific handling
                        return await TestByDataSourceTypeAsync(config, timeout);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Connection test failed with error: {ex.Message}");
            }
        }

        private static async Task<(bool success, string message)> TestRdbmsConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            // For ADO.NET compatible data sources
            if (config.ADOType)
            {
                try
                {
                    // Try to create the appropriate connection object
                    Type connectionType = Type.GetType(config.DbConnectionType);
                    if (connectionType == null)
                    {
                        return (false, $"Connection type {config.DbConnectionType} not found. Make sure the required assembly is referenced.");
                    }

                    DbConnection connection = Activator.CreateInstance(connectionType) as DbConnection;
                    if (connection == null)
                    {
                        return (false, $"Failed to create instance of {config.DbConnectionType}");
                    }

                    connection.ConnectionString = config.ConnectionString;
                  //  connection.ConnectionTimeout = timeout;

                    await connection.OpenAsync();
                    connection.Close();

                    return (true, "Connection successful");
                }
                catch (Exception ex)
                {
                    return (false, $"Connection failed: {ex.Message}");
                }
            }

            // Handle specific RDBMS types that might not use standard ADO.NET approach
            switch (config.DatasourceType)
            {
                case DataSourceType.SqlServer:
                case DataSourceType.Oracle:
                case DataSourceType.Mysql:
                case DataSourceType.Postgre:
                case DataSourceType.SqlLite:
                case DataSourceType.SqlCompact:
                case DataSourceType.FireBird:
                case DataSourceType.DB2:
                case DataSourceType.VistaDB:
                case DataSourceType.DuckDB:
                    // These are ADO.NET-compatible and should be handled by the code above
                    return (false, $"ADO.NET connection for {config.DatasourceType} failed to initialize");

                default:
                    return (false, $"Testing for RDBMS type {config.DatasourceType} is not directly supported");
            }
        }

        private static async Task<(bool success, string message)> TestNoSqlConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            switch (config.DatasourceType)
            {
                case DataSourceType.MongoDB:
                    return await TestMongoDBConnectionAsync(config);

                case DataSourceType.Couchbase:
                    return await TestCouchbaseConnectionAsync(config);

                case DataSourceType.ElasticSearch:
                    return await TestElasticsearchConnectionAsync(config);

                case DataSourceType.Redis:
                    return await TestRedisConnectionAsync(config);

                case DataSourceType.Cassandra:
                    return await TestCassandraConnectionAsync(config);

                case DataSourceType.RavenDB:
                    return await TestRavenDBConnectionAsync(config);

                case DataSourceType.CouchDB:
                    return await TestCouchDBConnectionAsync(config);

                case DataSourceType.Firebase:
                    return await TestFirebaseConnectionAsync(config);

                case DataSourceType.LiteDB:
                    return TestLiteDBConnection(config);

                case DataSourceType.SnowFlake:
                    return await TestSnowflakeConnectionAsync(config);

                case DataSourceType.Hadoop:
                    return await TestHadoopConnectionAsync(config);

                case DataSourceType.RealIM:
                    return TestRealmConnection(config);

                default:
                    return (false, $"Testing for NoSQL type {config.DatasourceType} is not implemented");
            }
        }

        private static async Task<(bool success, string message)> TestFileConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            switch (config.DatasourceType)
            {
                case DataSourceType.CSV:
                case DataSourceType.Xls:
                case DataSourceType.XML:
                case DataSourceType.Json:
                case DataSourceType.Text:
                case DataSourceType.INI:
                case DataSourceType.YAML:
                case DataSourceType.PDF:
                case DataSourceType.FlatFile:
                    // For file-based data sources, verify file exists or can be created
                    if (!string.IsNullOrEmpty(config.ConnectionString))
                    {
                        // Extract file path from connection string if possible
                        string filePath = ExtractFilePathFromConnectionString(config.ConnectionString);
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            if (File.Exists(filePath))
                            {
                                try
                                {
                                    // Test if the file is accessible (not locked)
                                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        // File can be opened
                                    }
                                    return (true, "File exists and is accessible");
                                }
                                catch (IOException ex)
                                {
                                    return (false, $"File exists but cannot be accessed: {ex.Message}");
                                }
                            }
                            else
                            {
                                // Check if directory exists for creating new file
                                string directory = Path.GetDirectoryName(filePath);
                                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                                {
                                    return (true, "File doesn't exist but can be created");
                                }
                                else
                                {
                                    return (false, "Directory for file doesn't exist");
                                }
                            }
                        }
                    }
                    return (false, "Unable to determine file path from connection string");

                default:
                    return (false, $"Testing for file type {config.DatasourceType} is not implemented");
            }
        }

        private static async Task<(bool success, string message)> TestInMemoryConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            switch (config.DatasourceType)
            {
                case DataSourceType.Redis:
                    return await TestRedisConnectionAsync(config);

                case DataSourceType.DuckDB:
                    // DuckDB can run in in-memory mode
                    return (true, "DuckDB in-memory connection is available");

                case DataSourceType.RealIM:
                    // Realm can run in in-memory mode
                    return (true, "Realm in-memory connection is available");

                default:
                    return (false, $"Testing for in-memory type {config.DatasourceType} is not implemented");
            }
        }

        private static async Task<(bool success, string message)> TestStreamConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            switch (config.DatasourceType)
            {
                case DataSourceType.Kafka:
                    return await TestKafkaConnectionAsync(config);

                case DataSourceType.RabbitMQ:
                    return await TestRabbitMQConnectionAsync(config);

                case DataSourceType.ActiveMQ:
                case DataSourceType.Pulsar:
                case DataSourceType.MassTransit:
                case DataSourceType.Nats:
                case DataSourceType.ZeroMQ:
                case DataSourceType.AWSKinesis:
                case DataSourceType.AWSSQS:
                case DataSourceType.AWSSNS:
                case DataSourceType.AzureServiceBus:
                    // Generic test for these streaming services
                    return (false, $"Connection test for {config.DatasourceType} requires specific implementation");

                default:
                    return (false, $"Testing for streaming type {config.DatasourceType} is not implemented");
            }
        }

        private static async Task<(bool success, string message)> TestWebApiConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            // Generic test for APIs - try to connect to the endpoint
            if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                string endpoint = ExtractEndpointFromConnectionString(config.ConnectionString);
                if (!string.IsNullOrEmpty(endpoint))
                {
                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                            var response = await httpClient.GetAsync(endpoint);
                            return (response.IsSuccessStatusCode,
                                    response.IsSuccessStatusCode
                                        ? "API endpoint responded successfully"
                                        : $"API endpoint returned status: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Failed to connect to API endpoint: {ex.Message}");
                    }
                }
                return (false, "Unable to determine API endpoint from connection string");
            }

            return (false, "Connection string is empty");
        }

        private static async Task<(bool success, string message)> TestByDataSourceTypeAsync(ConnectionDriversConfig config, int timeout)
        {
            // Fallback handling for any data source types not handled by category
            switch (config.DatasourceType)
            {
                // Big Data systems
                case DataSourceType.Hadoop:
                case DataSourceType.Kudu:
                case DataSourceType.Druid:
                case DataSourceType.Pinot:
                    return (false, $"Connection test for big data system {config.DatasourceType} requires specific implementation");

                // Cloud services
                case DataSourceType.AWSRedshift:
                case DataSourceType.GoogleBigQuery:
                case DataSourceType.AWSGlue:
                case DataSourceType.AWSAthena:
                case DataSourceType.AzureCloud:
                case DataSourceType.DataBricks:
                    return (false, $"Connection test for cloud service {config.DatasourceType} requires specific implementation");

                // Graph databases
                case DataSourceType.Neo4j:
                case DataSourceType.TigerGraph:
                case DataSourceType.JanusGraph:
                case DataSourceType.ArangoDB:
                    return (false, $"Connection test for graph database {config.DatasourceType} requires specific implementation");

                default:
                    return (false, $"Testing for {config.DatasourceType} is not implemented");
            }
        }

        // Specific implementations for various database systems

        private static async Task<(bool success, string message)> TestMongoDBConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need the MongoDB driver to be referenced
            return (false, "MongoDB connection test requires MongoDB.Driver");

            // Example implementation if MongoDB.Driver is referenced:
            /*
            try
            {
                var client = new MongoClient(config.ConnectionString);
                var databases = await client.ListDatabaseNamesAsync();
                await databases.MoveNextAsync();
                return (true, "MongoDB connection successful");
            }
            catch (Exception ex)
            {
                return (false, $"MongoDB connection failed: {ex.Message}");
            }
            */
        }

        private static async Task<(bool success, string message)> TestCouchbaseConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need the Couchbase driver to be referenced
            return (false, "Couchbase connection test requires Couchbase.NetClient");
        }

        private static async Task<(bool success, string message)> TestElasticsearchConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need the Elasticsearch.Net package to be referenced
            return (false, "Elasticsearch connection test requires Elasticsearch.Net");
        }

        private static async Task<(bool success, string message)> TestRedisConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need the StackExchange.Redis package to be referenced
            return (false, "Redis connection test requires StackExchange.Redis");
        }

        private static async Task<(bool success, string message)> TestCassandraConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need the Cassandra driver to be referenced
            return (false, "Cassandra connection test requires CassandraCSharpDriver");
        }

        private static async Task<(bool success, string message)> TestRavenDBConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need the RavenDB client to be referenced
            return (false, "RavenDB connection test requires RavenDB.Client");
        }

        private static async Task<(bool success, string message)> TestCouchDBConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need an HTTP client to test CouchDB which uses REST
            try
            {
                string url = ExtractEndpointFromConnectionString(config.ConnectionString);
                if (string.IsNullOrEmpty(url))
                {
                    return (false, "Could not extract CouchDB URL from connection string");
                }

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(url);
                    return (response.IsSuccessStatusCode,
                            response.IsSuccessStatusCode ? "CouchDB connection successful" : $"CouchDB returned status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"CouchDB connection failed: {ex.Message}");
            }
        }

        private static async Task<(bool success, string message)> TestFirebaseConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need Firebase SDK to be referenced
            return (false, "Firebase connection test requires Firebase SDK");
        }

        private static (bool success, string message) TestLiteDBConnection(ConnectionDriversConfig config)
        {
            // This would need LiteDB package to be referenced
            // LiteDB can be tested synchronously
            return (false, "LiteDB connection test requires LiteDB package");
        }

        private static (bool success, string message) TestRealmConnection(ConnectionDriversConfig config)
        {
            // This would need Realm SDK to be referenced
            return (false, "Realm connection test requires Realm SDK");
        }

        private static async Task<(bool success, string message)> TestSnowflakeConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need Snowflake connector to be referenced
            return (false, "Snowflake connection test requires Snowflake.Data");
        }

        private static async Task<(bool success, string message)> TestHadoopConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need Hadoop connectors to be referenced
            return (false, "Hadoop connection test requires specific Hadoop connectors");
        }

        private static async Task<(bool success, string message)> TestKafkaConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need Confluent.Kafka package to be referenced
            return (false, "Kafka connection test requires Confluent.Kafka");
        }

        private static async Task<(bool success, string message)> TestRabbitMQConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need RabbitMQ.Client package to be referenced
            return (false, "RabbitMQ connection test requires RabbitMQ.Client");
        }


        // Additional specific implementations for other data sources can be added here like vector databases , etc.
        private static async Task<(bool success, string message)> TestDuckDBConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need DuckDB package to be referenced
            return (false, "DuckDB connection test requires DuckDB");
        }
        private static async Task<(bool success, string message)> TestWeaviateConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need Weaviate client to be referenced
            return (false, "Weaviate connection test requires Weaviate client");
        }
        private static async Task<(bool success, string message)> TestMilvusConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need Milvus client to be referenced
            return (false, "Milvus connection test requires Milvus client");
        }
        private static async Task<(bool success, string message)> TestRedisVectorConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need Redis client to be referenced
            return (false, "Redis Vector connection test requires Redis client");
        }
        private static async Task<(bool success, string message)> TestZillizConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need Zilliz client to be referenced
            return (false, "Zilliz connection test requires Zilliz client");
        }
        private static async Task<(bool success, string message)> TestVespaConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need Vespa client to be referenced
            return (false, "Vespa connection test requires Vespa client");
        }
        private static async Task<(bool success, string message)> TestQdrantConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need Qdrant client to be referenced
            return (false, "Qdrant connection test requires Qdrant client");
        }
        private static async Task<(bool success, string message)> TestPineConeConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need PineCone client to be referenced
            return (false, "PineCone connection test requires PineCone client");
        }
        private static async Task<(bool success, string message)> TestShapVectorConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need ShapVector client to be referenced
            return (false, "ShapVector connection test requires ShapVector client");
        }
        private static async Task<(bool success, string message)> TestWebApiConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need HttpClient to be referenced
            return (false, "Web API connection test requires HttpClient");
        }
        private static async Task<(bool success, string message)> TestGraphQLConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need GraphQL client to be referenced
            return (false, "GraphQL connection test requires GraphQL client");
        }
        private static async Task<(bool success, string message)> TestODataConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need OData client to be referenced
            return (false, "OData connection test requires OData client");
        }
        private static async Task<(bool success, string message)> TestODBCConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need ODBC client to be referenced
            return (false, "ODBC connection test requires ODBC client");
        }
        private static async Task<(bool success, string message)> TestOLEDBConnectionAsync(ConnectionDriversConfig config)
        {
            // This would need OLEDB client to be referenced
            return (false, "OLEDB connection test requires OLEDB client");
        }
        // Helper methods

        private static string ExtractFilePathFromConnectionString(string connectionString)
        {
            // Extract file path from connection strings like "Data Source=path/to/file.db" or "Path=path/to/file.json"
            var filePathRegex = new Regex(@"(?:Data Source|Path|File)\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            var match = filePathRegex.Match(connectionString);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }

            // If no match found but connection string doesn't contain any key=value pairs,
            // assume the whole string is a file path
            if (!connectionString.Contains('='))
            {
                return connectionString.Trim();
            }

            return null;
        }

        private static string ExtractEndpointFromConnectionString(string connectionString)
        {
            // Extract endpoint from connection strings like "Server=http://example.com:9200" or "Url=http://api.example.com"
            var endpointRegex = new Regex(@"(?:Server|Url|Host|Endpoint|DatabaseURL|Configuration)\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            var match = endpointRegex.Match(connectionString);

            if (match.Success && match.Groups.Count > 1)
            {
                string endpoint = match.Groups[1].Value.Trim();

                // If endpoint doesn't contain protocol, assume http://
                if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    endpoint = "http://" + endpoint;
                }

                return endpoint;
            }

            return null;
        }

        private static string ExtractDatabaseNameFromConnectionString(string connectionString)
        {
            // Extract database name from connection strings like "Database=mydb" or "Initial Catalog=mydb"
            var dbNameRegex = new Regex(@"(?:Database|Initial Catalog)\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            var match = dbNameRegex.Match(connectionString);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
            return null;
        }
        private static string ExtractUserNameFromConnectionString(string connectionString)
        {
            // Extract username from connection strings like "User Id=myuser" or "Uid=myuser"
            var userNameRegex = new Regex(@"(?:User Id|Uid)\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            var match = userNameRegex.Match(connectionString);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
            return null;
        }
        private static string ExtractPasswordFromConnectionString(string connectionString)
        {
            // Extract password from connection strings like "Password=mypassword" or "Pwd=mypassword"
            var passwordRegex = new Regex(@"(?:Password|Pwd)\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            var match = passwordRegex.Match(connectionString);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
            return null;
        }
        private static string ExtractPortFromConnectionString(string connectionString)
        {
            // Extract port from connection strings like "Port=1234" or "Server=myserver,1234"
            var portRegex = new Regex(@"(?:Port|Server)\s*=\s*([^;,:]+)", RegexOptions.IgnoreCase);
            var match = portRegex.Match(connectionString);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
            return null;
        }

    }
}
