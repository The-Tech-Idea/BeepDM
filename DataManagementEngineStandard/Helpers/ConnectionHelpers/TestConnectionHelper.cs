using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.ConnectionHelpers
{
    public class TestConnectionHelper
    {
        // ── Runtime liveness probe (live IDataSource) ─────────────────────
        // Used by proxy/watchdog to test a datasource that is already open and
        // managed by DMEEditor.  Delegates to ProxyLivenessHelper which selects
        // the correct probe strategy (ping-query, GetEntitesList, ConnectionState, …)
        // based on DatasourceCategory and DatasourceType.

        /// <summary>
        /// Tests a live, DMEEditor-managed <see cref="IDataSource"/> for liveness.
        /// Probe strategy is automatically chosen per datasource category:
        /// <list type="bullet">
        ///   <item>RDBMS → cheapest engine-specific ping query (e.g. SELECT 1)</item>
        ///   <item>NoSQL / File / WebAPI / Cloud / VectorDB → GetEntitesList metadata call</item>
        ///   <item>Stream / Queue / IoT / Blockchain → ConnectionState signal</item>
        ///   <item>InMemory → alive unless ConnectionState is Broken</item>
        /// </list>
        /// Never throws — all exceptions are mapped to <c>(false, message)</c>.
        /// </summary>
        /// <param name="ds">Live datasource instance.</param>
        /// <param name="timeoutMs">Advisory timeout in milliseconds (default 5 s).</param>
        public static Task<(bool success, string message)> TestConnectionAsync(IDataSource ds, int timeoutMs = 5_000)
        {
            if (ds == null)
                return Task.FromResult((false, "DataSource is null"));

            try
            {
                bool alive = ProxyLivenessHelper.IsAlive(ds, timeoutMs);
                string msg  = alive
                    ? $"{ds.DatasourceName} ({ds.Category}) is alive"
                    : $"{ds.DatasourceName} ({ds.Category}) did not respond to liveness probe";
                return Task.FromResult((alive, msg));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, $"Liveness probe threw: {ex.Message}"));
            }
        }

        // ── Setup-time config test (ConnectionDriversConfig) ──────────────

        /// <summary>
        /// Tests a connection configuration to verify it can connect successfully.
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
                    case DatasourceCategory.QUEUE:
                        return await TestStreamConnectionAsync(config, timeout);

                    case DatasourceCategory.WEBAPI:
                    case DatasourceCategory.VIEWS:
                        return await TestWebApiConnectionAsync(config, timeout);

                    case DatasourceCategory.CLOUD:
                        return await TestCloudConnectionAsync(config, timeout);

                    case DatasourceCategory.VectorDB:
                        return await TestVectorDbConnectionAsync(config);

                    case DatasourceCategory.IoT:
                        return await TestIoTConnectionAsync(config, timeout);

                    case DatasourceCategory.Connector:
                        return await TestConnectorConnectionAsync(config, timeout);

                    case DatasourceCategory.Blockchain:
                        return await TestBlockchainConnectionAsync(config, timeout);

                    case DatasourceCategory.Workflow:
                        return await TestWorkflowConnectionAsync(config, timeout);

                    case DatasourceCategory.MLModel:
                        return await TestMLModelConnectionAsync(config, timeout);

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
                case DataSourceType.MariaDB:
                case DataSourceType.Postgre:
                case DataSourceType.SqlLite:
                case DataSourceType.SqlCompact:
                case DataSourceType.FireBird:
                case DataSourceType.DB2:
                case DataSourceType.VistaDB:
                case DataSourceType.DuckDB:
                case DataSourceType.Hana:
                case DataSourceType.Cockroach:
                case DataSourceType.AzureSQL:
                case DataSourceType.AWSRDS:
                case DataSourceType.Spanner:
                case DataSourceType.TerraData:
                case DataSourceType.Vertica:
                case DataSourceType.SnowFlake:
                case DataSourceType.ClickHouse:
                case DataSourceType.TimeScale:
                case DataSourceType.Presto:
                case DataSourceType.Trino:
                case DataSourceType.Firebolt:
                case DataSourceType.Hologres:
                case DataSourceType.Supabase:
                case DataSourceType.ODBC:
                case DataSourceType.OLEDB:
                case DataSourceType.ADO:
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

                case DataSourceType.DynamoDB:
                    return (false, "DynamoDB connection test requires AWSSDK.DynamoDBv2");

                case DataSourceType.ArangoDB:
                    return await TestArangoDBConnectionAsync(config);

                case DataSourceType.Neo4j:
                    return (false, "Neo4j connection test requires Neo4j.Driver");

                case DataSourceType.OrientDB:
                    return (false, "OrientDB connection test requires OrientDB.Net.Community");

                case DataSourceType.ClickHouse:
                    return await TestClickHouseConnectionAsync(config);

                case DataSourceType.InfluxDB:
                    return await TestInfluxDBConnectionAsync(config);

                default:
                    return (false, $"Testing for NoSQL type {config.DatasourceType} is not implemented");
            }
        }

        private static async Task<(bool success, string message)> TestFileConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            switch (config.DatasourceType)
            {
                case DataSourceType.CSV:
                case DataSourceType.TSV:
                case DataSourceType.Xls:
                case DataSourceType.XML:
                case DataSourceType.Json:
                case DataSourceType.Text:
                case DataSourceType.INI:
                case DataSourceType.YAML:
                case DataSourceType.PDF:
                case DataSourceType.FlatFile:
                case DataSourceType.Markdown:
                case DataSourceType.Log:
                case DataSourceType.Doc:
                case DataSourceType.Docx:
                case DataSourceType.PPT:
                case DataSourceType.PPTX:
                case DataSourceType.Parquet:
                case DataSourceType.Avro:
                case DataSourceType.ORC:
                case DataSourceType.Feather:
                case DataSourceType.Onnx:
                case DataSourceType.RecordIO:
                case DataSourceType.Hdf5:
                case DataSourceType.LibSVM:
                case DataSourceType.GraphML:
                case DataSourceType.DICOM:
                case DataSourceType.LAS:
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
                    return (true, "DuckDB in-memory connection is available");

                case DataSourceType.RealIM:
                    return (true, "Realm in-memory connection is available");

                case DataSourceType.InMemoryCache:
                case DataSourceType.CachedMemory:
                    // In-process caches — always reachable within the same process
                    return (true, $"{config.DatasourceType} is an in-process cache, always reachable");

                case DataSourceType.H2Database:
                    return (false, "H2Database connection test requires H2 JDBC driver via Java interop");

                case DataSourceType.Memcached:
                    return (false, "Memcached connection test requires EnyimMemcachedCore or similar client");

                case DataSourceType.GridGain:
                case DataSourceType.ApacheIgnite:
                    return (false, $"{config.DatasourceType} connection test requires Apache.Ignite");

                case DataSourceType.Hazelcast:
                    return (false, "Hazelcast connection test requires Hazelcast.Net");

                case DataSourceType.ChronicleMap:
                case DataSourceType.RocketSet:
                    return (false, $"{config.DatasourceType} is a JVM-based in-memory store; native .NET access is not available");

                case DataSourceType.Petastorm:
                    return (false, "Petastorm requires the petastorm Python package via interop");

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
                case DataSourceType.ApacheFlink:
                case DataSourceType.ApacheStorm:
                case DataSourceType.ApacheSparkStreaming:
                    // Generic test for these streaming / queue services
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
            // Fallback for types that don't match any category handler, or whose category is NONE/Unknown
            switch (config.DatasourceType)
            {
                // Big Data / Columnar
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
                case DataSourceType.Firebolt:
                case DataSourceType.Hologres:
                case DataSourceType.Supabase:
                    return (false, $"Connection test for cloud service {config.DatasourceType} requires specific implementation");

                // Graph databases
                case DataSourceType.Neo4j:
                    return (false, "Neo4j connection test requires Neo4j.Driver");
                case DataSourceType.TigerGraph:
                case DataSourceType.JanusGraph:
                case DataSourceType.ArangoDB:
                    return (false, $"Connection test for graph database {config.DatasourceType} requires specific implementation");

                // Vector databases
                case DataSourceType.ChromaDB:
                    return await TestChromaDBConnectionAsync(config);
                case DataSourceType.PineCone:
                    return await TestPineConeConnectionAsync(config);
                case DataSourceType.Qdrant:
                    return await TestQdrantConnectionAsync(config);
                case DataSourceType.ShapVector:
                    return await TestShapVectorConnectionAsync(config);
                case DataSourceType.Weaviate:
                    return await TestWeaviateConnectionAsync(config);
                case DataSourceType.Milvus:
                    return await TestMilvusConnectionAsync(config);
                case DataSourceType.RedisVector:
                    return await TestRedisVectorConnectionAsync(config);
                case DataSourceType.Zilliz:
                    return await TestZillizConnectionAsync(config);
                case DataSourceType.Vespa:
                    return await TestVespaConnectionAsync(config);

                // Machine Learning artefact formats — not connectable services
                case DataSourceType.TFRecord:
                case DataSourceType.ONNX:
                case DataSourceType.PyTorchData:
                case DataSourceType.ScikitLearnData:
                case DataSourceType.MiModel:
                    return (false, $"{config.DatasourceType} is a model artefact format, not a connectable service");

                // IoT platforms
                case DataSourceType.AWSIoT:
                case DataSourceType.AWSIoTCore:
                case DataSourceType.AWSIoTAnalytics:
                    return (false, $"{config.DatasourceType} connection test requires AWSSDK.IoT");
                case DataSourceType.AzureIoTHub:
                    return (false, "AzureIoTHub connection test requires Microsoft.Azure.Devices");
                case DataSourceType.Particle:
                case DataSourceType.ArduinoCloud:
                    return await TestWebEndpointAsync(config, timeout);

                // Workflow / orchestration
                case DataSourceType.AWSSWF:
                    return (false, "AWSSWF connection test requires AWSSDK.SimpleWorkflow");
                case DataSourceType.AWSStepFunctions:
                    return (false, "AWSStepFunctions connection test requires AWSSDK.StepFunctions");

                // Search platforms
                case DataSourceType.Solr:
                    return await TestWebEndpointAsync(config, timeout);

                // Industrial
                case DataSourceType.OPC:
                    return (false, "OPC connection test requires an OPC UA client library");

                // Generic web / RPC protocols
                case DataSourceType.WebApi:
                case DataSourceType.RestApi:
                    return await TestWebEndpointAsync(config, timeout);
                case DataSourceType.GraphQL:
                    return await TestGraphQLConnectionAsync(config);
                case DataSourceType.OData:
                    return await TestODataConnectionAsync(config);
                case DataSourceType.ODBC:
                    return await TestODBCConnectionAsync(config);
                case DataSourceType.OLEDB:
                    return await TestOLEDBConnectionAsync(config);
                case DataSourceType.XMLRPC:
                case DataSourceType.JSONRPC:
                case DataSourceType.GRPC:
                case DataSourceType.SSE:
                case DataSourceType.WebSocket:
                case DataSourceType.SOAP:
                    return await TestWebEndpointAsync(config, timeout);

                // File transfer
                case DataSourceType.FTP:
                case DataSourceType.SFTP:
                    return (false, $"{config.DatasourceType} connection test — verify host/port/credentials manually");

                // Email protocols
                case DataSourceType.Email:
                case DataSourceType.IMAP:
                case DataSourceType.POP3:
                case DataSourceType.SMTP:
                case DataSourceType.Gmail:
                case DataSourceType.Outlook:
                case DataSourceType.Yahoo:
                    return (false, $"{config.DatasourceType} connection test requires MailKit or similar mail client");

                // Blockchain / distributed ledger
                case DataSourceType.Ethereum:
                    return (false, "Ethereum connection test requires Nethereum");
                case DataSourceType.Hyperledger:
                    return (false, "Hyperledger Fabric connection test requires Hyperledger.Fabric.SDK");
                case DataSourceType.BitcoinCore:
                case DataSourceType.Coinbase:
                    return await TestWebEndpointAsync(config, timeout);

                // Cloud storage
                case DataSourceType.AmazonS3:
                    return (false, "AmazonS3 connection test requires AWSSDK.S3");
                case DataSourceType.GoogleDrive:
                    return (false, "GoogleDrive connection test requires Google.Apis.Drive.v3");
                case DataSourceType.OneDrive:
                    return (false, "OneDrive connection test requires Microsoft.Graph");
                case DataSourceType.Dropbox:
                case DataSourceType.Box:
                    return await TestWebEndpointAsync(config, timeout);

                // Developer tools
                case DataSourceType.GitHub:
                case DataSourceType.GitLab:
                case DataSourceType.Bitbucket:
                case DataSourceType.Jenkins:
                case DataSourceType.CircleCI:
                case DataSourceType.AzureDevOps:
                    return await TestWebEndpointAsync(config, timeout);

                // Analytics
                case DataSourceType.GoogleAnalytics:
                    return (false, "GoogleAnalytics connection test requires Google.Apis.AnalyticsData");

                // Google Sheets
                case DataSourceType.GoogleSheets:
                    return (false, "Google Sheets connection test requires Google.Apis.Sheets.v4");

                // Unknown / none
                case DataSourceType.Unknown:
                case DataSourceType.NONE:
                case DataSourceType.Other:
                    return (false, $"Connection type '{config.DatasourceType}' is unspecified — unable to test");

                default:
                    // Catch-all: most remaining connectors (CRM, ERP, Marketing, etc.) are REST-based
                    return await TestWebEndpointAsync(config, timeout);
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
        // ── Category-level dispatcher methods ─────────────────────────────

        private static async Task<(bool success, string message)> TestVectorDbConnectionAsync(ConnectionDriversConfig config)
        {
            return config.DatasourceType switch
            {
                DataSourceType.ChromaDB    => await TestChromaDBConnectionAsync(config),
                DataSourceType.PineCone    => await TestPineConeConnectionAsync(config),
                DataSourceType.Qdrant      => await TestQdrantConnectionAsync(config),
                DataSourceType.ShapVector  => await TestShapVectorConnectionAsync(config),
                DataSourceType.Weaviate    => await TestWeaviateConnectionAsync(config),
                DataSourceType.Milvus      => await TestMilvusConnectionAsync(config),
                DataSourceType.RedisVector => await TestRedisVectorConnectionAsync(config),
                DataSourceType.Zilliz      => await TestZillizConnectionAsync(config),
                DataSourceType.Vespa       => await TestVespaConnectionAsync(config),
                _                          => (false, $"VectorDB type {config.DatasourceType} connection test is not implemented")
            };
        }

        private static async Task<(bool success, string message)> TestCloudConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            return config.DatasourceType switch
            {
                DataSourceType.AWSRedshift    => (false, "AWSRedshift connection test requires Npgsql or Amazon.Redshift.ODBC"),
                DataSourceType.GoogleBigQuery => (false, "BigQuery connection test requires Google.Cloud.BigQuery.V2"),
                DataSourceType.AWSGlue        => (false, "AWSGlue connection test requires AWSSDK.Glue"),
                DataSourceType.AWSAthena      => (false, "AWSAthena connection test requires AWSSDK.Athena"),
                DataSourceType.AzureCloud     => await TestWebEndpointAsync(config, timeout),
                DataSourceType.DataBricks     => (false, "Databricks connection test requires Databricks.Connect or JDBC"),
                DataSourceType.Firebolt       => (false, "Firebolt connection test requires Firebolt.Net SDK"),
                DataSourceType.Hologres       => (false, "Hologres connection test requires Alibaba Cloud SDK"),
                DataSourceType.Supabase       => await TestWebEndpointAsync(config, timeout),
                DataSourceType.AmazonS3       => (false, "AmazonS3 connection test requires AWSSDK.S3"),
                DataSourceType.GoogleDrive    => (false, "GoogleDrive connection test requires Google.Apis.Drive.v3"),
                DataSourceType.OneDrive       => (false, "OneDrive connection test requires Microsoft.Graph"),
                _                             => await TestWebEndpointAsync(config, timeout)
            };
        }

        private static async Task<(bool success, string message)> TestIoTConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            return config.DatasourceType switch
            {
                DataSourceType.AWSIoT          => (false, "AWSIoT connection test requires AWSSDK.IoT"),
                DataSourceType.AWSIoTCore      => (false, "AWSIoTCore connection test requires AWSSDK.IoT"),
                DataSourceType.AWSIoTAnalytics => (false, "AWSIoTAnalytics connection test requires AWSSDK.IoTAnalytics"),
                DataSourceType.AzureIoTHub     => (false, "AzureIoTHub connection test requires Microsoft.Azure.Devices"),
                DataSourceType.Particle        => await TestWebEndpointAsync(config, timeout),
                DataSourceType.ArduinoCloud    => await TestWebEndpointAsync(config, timeout),
                _                              => (false, $"IoT type {config.DatasourceType} connection test is not implemented")
            };
        }

        private static async Task<(bool success, string message)> TestConnectorConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            // All SaaS connectors (CRM, ERP, Marketing, etc.) expose REST APIs
            return await TestWebEndpointAsync(config, timeout);
        }

        private static async Task<(bool success, string message)> TestBlockchainConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            return config.DatasourceType switch
            {
                DataSourceType.Ethereum    => (false, "Ethereum connection test requires Nethereum"),
                DataSourceType.Hyperledger => (false, "Hyperledger Fabric connection test requires Hyperledger.Fabric.SDK"),
                DataSourceType.BitcoinCore => await TestWebEndpointAsync(config, timeout),
                DataSourceType.Coinbase    => await TestWebEndpointAsync(config, timeout),
                _                          => await TestWebEndpointAsync(config, timeout)
            };
        }

        private static async Task<(bool success, string message)> TestWorkflowConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            return config.DatasourceType switch
            {
                DataSourceType.AWSSWF           => (false, "AWSSWF connection test requires AWSSDK.SimpleWorkflow"),
                DataSourceType.AWSStepFunctions => (false, "AWSStepFunctions connection test requires AWSSDK.StepFunctions"),
                _                               => await TestWebEndpointAsync(config, timeout)
            };
        }

        private static Task<(bool success, string message)> TestMLModelConnectionAsync(ConnectionDriversConfig config, int timeout)
        {
            // ML model artefact formats (TFRecord, ONNX, etc.) are local files, not connectable services
            return Task.FromResult((false, $"{config.DatasourceType} is a model artefact format, not a connectable service"));
        }

        // ── Shared HTTP endpoint probe ─────────────────────────────────────

        private static async Task<(bool success, string message)> TestWebEndpointAsync(ConnectionDriversConfig config, int timeout)
        {
            if (string.IsNullOrEmpty(config.ConnectionString))
                return (false, "Connection string is empty");

            string endpoint = ExtractEndpointFromConnectionString(config.ConnectionString);
            if (string.IsNullOrEmpty(endpoint))
                return (false, $"Unable to determine endpoint for {config.DatasourceType} from connection string");

            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeout) };
                var response = await httpClient.GetAsync(endpoint);
                return (response.IsSuccessStatusCode,
                        response.IsSuccessStatusCode
                            ? $"{config.DatasourceType} endpoint responded successfully"
                            : $"{config.DatasourceType} endpoint returned status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to connect to {config.DatasourceType} endpoint: {ex.Message}");
            }
        }

        // ── Additional specific implementations ────────────────────────────

        private static async Task<(bool success, string message)> TestArangoDBConnectionAsync(ConnectionDriversConfig config)
        {
            // ArangoDB exposes a REST API; probe /_api/version
            try
            {
                string url = ExtractEndpointFromConnectionString(config.ConnectionString);
                if (string.IsNullOrEmpty(url))
                    return (false, "Could not extract ArangoDB URL from connection string");
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{url.TrimEnd('/')}/_api/version");
                return (response.IsSuccessStatusCode,
                        response.IsSuccessStatusCode ? "ArangoDB connection successful" : $"ArangoDB returned status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"ArangoDB connection failed: {ex.Message}");
            }
        }

        private static async Task<(bool success, string message)> TestClickHouseConnectionAsync(ConnectionDriversConfig config)
        {
            // ClickHouse HTTP interface (default port 8123) — run SELECT 1
            try
            {
                string url = ExtractEndpointFromConnectionString(config.ConnectionString);
                if (string.IsNullOrEmpty(url))
                    return (false, "Could not extract ClickHouse URL from connection string");
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{url.TrimEnd('/')}/?query=SELECT+1");
                return (response.IsSuccessStatusCode,
                        response.IsSuccessStatusCode ? "ClickHouse connection successful" : $"ClickHouse returned status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"ClickHouse connection failed: {ex.Message}");
            }
        }

        private static async Task<(bool success, string message)> TestInfluxDBConnectionAsync(ConnectionDriversConfig config)
        {
            // InfluxDB v2 /health endpoint
            try
            {
                string url = ExtractEndpointFromConnectionString(config.ConnectionString);
                if (string.IsNullOrEmpty(url))
                    return (false, "Could not extract InfluxDB URL from connection string");
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{url.TrimEnd('/')}/health");
                return (response.IsSuccessStatusCode,
                        response.IsSuccessStatusCode ? "InfluxDB connection successful" : $"InfluxDB returned status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"InfluxDB connection failed: {ex.Message}");
            }
        }

        private static async Task<(bool success, string message)> TestChromaDBConnectionAsync(ConnectionDriversConfig config)
        {
            // ChromaDB REST API heartbeat endpoint
            try
            {
                string url = ExtractEndpointFromConnectionString(config.ConnectionString);
                if (string.IsNullOrEmpty(url))
                    return (false, "Could not extract ChromaDB URL from connection string");
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{url.TrimEnd('/')}/api/v1/heartbeat");
                return (response.IsSuccessStatusCode,
                        response.IsSuccessStatusCode ? "ChromaDB connection successful" : $"ChromaDB returned status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"ChromaDB connection failed: {ex.Message}");
            }
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
