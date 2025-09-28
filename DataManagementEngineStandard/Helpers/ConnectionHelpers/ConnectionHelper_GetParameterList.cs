using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil; // For IConnectionProperties

namespace TheTechIdea.Beep.Helpers
{
    public static partial class ConnectionHelper
    {
        public static List<string> GetParameterList(string connectionString)
        {
            var parameters = new List<string>();

            // Split the connection string into key-value pairs
            var keyValuePairs = connectionString.Split(';');

            foreach (var pair in keyValuePairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    // var value = keyValue[1].Trim();
                    parameters.Add(key);
                }
            }

            return parameters;
        }
        public static string GetParameterValue(string connectionString, string parameterName)
        {
            var all = GetAllParameters(connectionString);
            return all.TryGetValue(parameterName, out var v) ? v : null;
        }
        public static string SetParameterValue(string connectionString, string parameterName, string parameterValue)
        {
            var parameters = connectionString.Split(';').ToList();
            bool replaced = false;
            for (int i = 0; i < parameters.Count; i++)
            {
                var keyValue = parameters[i].Split('=');
                if (keyValue.Length == 2 && keyValue[0].Trim().Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    parameters[i] = $"{parameterName}={parameterValue}";
                    replaced = true;
                    break;
                }
            }
            if (!replaced)
            {
                parameters.Add($"{parameterName}={parameterValue}");
            }
            return string.Join(";", parameters);
        }
        public static string RemoveParameter(string connectionString, string parameterName)
        {
            var parameters = connectionString.Split(';').ToList();
            parameters = parameters.Where(p => !p.Split('=')?.FirstOrDefault()?.Trim().Equals(parameterName, StringComparison.OrdinalIgnoreCase) ?? false).ToList();
            return string.Join(";", parameters);
        }
        public static Dictionary<string, string> GetAllParameters(string connectionString)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var keyValuePairs = connectionString.Split(';');
            foreach (var pair in keyValuePairs)
            {
                if (string.IsNullOrWhiteSpace(pair)) continue;
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();
                    parameters[key] = value;
                }
            }
            return parameters;
        }

        // Returns parameter definitions for a given data source type.
        // Only include parameters typically used in connection strings and not explicitly modeled with the same name in IConnectionProperties.
        public static Dictionary<string, parameterinfo> GetAllParametersForDataSourceTypeNotInConnectionProperties( DataSourceType dataSourceType)
        {
            var defs = new Dictionary<string, parameterinfo>(StringComparer.OrdinalIgnoreCase);
           

            void Add(string name, string desc, string type = "string", bool required = false, string def = null)
            {
                defs[name] = new parameterinfo
                {
                    Name = name,
                    Description = desc,
                    DataType = type,
                    IsRequired = required,
                    DefaultValue = def,
                    Value = def
                };
            }

            // Common options used across many RDBMS providers
            void AddCommonDbPoolOptions()
            {
                Add("Pooling", "Enable connection pooling.", "bool", false, "True");
                Add("Min Pool Size", "Minimum number of connections in the pool.", "int", false, "0");
                Add("Max Pool Size", "Maximum number of connections in the pool.", "int", false, "100");
                Add("Load Balance Timeout", "Maximum time in seconds to keep connections in pool.", "int", false, null);
                Add("Application Name", "Client application name for diagnostics.");
                Add("Packet Size", "Size in bytes of network packets.", "int");
            }

            switch (dataSourceType)
            {
                // RDBMS
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    Add("Encrypt", "Use SSL/TLS encryption between client and server.", "bool", false, "False");
                    Add("TrustServerCertificate", "Skip certificate chain validation when Encrypt=true.", "bool", false, "False");
                    Add("Connection Timeout", "Seconds to wait for a connection.", "int", false, "15");
                    Add("MultipleActiveResultSets", "Allow multiple active result sets.", "bool", false, "False");
                    AddCommonDbPoolOptions();
                    break;
               
                case DataSourceType.Mysql:
                    Add("AllowPublicKeyRetrieval", "Allow public key retrieval (needed for some auth modes).", "bool", false, "False");
                    Add("Convert Zero Datetime", "Convert MySQL zero-dates to DateTime.MinValue.", "bool", false, "False");
                    Add("Default Command Timeout", "Default command timeout in seconds.", "int");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.Postgre:
                    Add("Search Path", "Schema search path.");
                    Add("Ssl Mode", "SSL mode (Disable/Require/VerifyCA/VerifyFull).", "string");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.Oracle:
                    Add("Service Name", "Oracle service name.");
                    Add("SID", "Oracle SID (if not using service name).");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.DB2:
                    Add("Connect Timeout", "Alias for Connection Timeout.", "int", false, "30");
                    Add("Command Timeout", "Command execution timeout in seconds.", "int", false, "30");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.FireBird:
                    Add("Charset", "Character set for the connection.", "string", false, "UTF8");
                    Add("Command Timeout", "Command execution timeout in seconds.", "int", false, "0");
                    Add("Dialect", "SQL dialect version.", "int", false, "3");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.SqlLite:
                    Add("Mode", "SQLite open mode (ReadOnly/ReadWrite/Memory/SharedCache).", "string");
                    Add("Cache", "SQLite cache mode.", "string");
                    break;
                case DataSourceType.SnowFlake:
                    Add("WAREHOUSE", "Virtual warehouse name.");
                    Add("ROLE", "Role name.");
                    Add("DB", "Database alias.");
                    Add("SCHEMA", "Schema name.");
                    Add("CLIENT_SESSION_KEEP_ALIVE", "Keep the session alive.", "bool", false, "False");
                    break;
                case DataSourceType.Hana:
                    Add("Command Timeout", "Command execution timeout in seconds.", "int", false, "30");
                    Add("Auto Commit", "Enable auto commit mode.", "bool", false, "True");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.Cockroach:
                    Add("sslmode", "SSL mode (disable/require/verify-ca/verify-full).", "string", false, "require");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.Spanner:
                    Add("Project", "GCP project id.");
                    Add("Instance", "Spanner instance id.");
                    break;
                case DataSourceType.TerraData:
                    Add("LOGMECH", "Teradata logon mechanism.");
                    Add("CHARSET", "Character set for the connection.", "string", false, "UTF8");
                    Add("SESSIONS", "Number of sessions to establish.", "int", false, "1");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.Vertica:
                    Add("Connection Load Balance", "Enable connection load balancing.", "bool", false, "True");
                    Add("Command Timeout", "Command execution timeout in seconds.", "int", false, "600");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.AWSRDS:
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.AWSRedshift:
                    Add("SSL", "Enable SSL.", "bool", false, "True");
                    Add("Tcp Keepalive", "Enable TCP keepalive.", "bool", false, "True");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.GoogleBigQuery:
                    Add("DatasetId", "Default dataset id.");
                    break;

                // NoSQL / Document / Columnar DBs
                case DataSourceType.MongoDB:
                    Add("ReplicaSet", "Name of the replica set.");
                    Add("RetryWrites", "Enable retryable writes.", "bool", false, "True");
                    break;
                case DataSourceType.CouchDB:
                case DataSourceType.RavenDB:
                case DataSourceType.Couchbase:
                    Add("Bucket", "Default bucket or database.");
                    break;
                case DataSourceType.Redis:
                    Add("DefaultDatabase", "Default database index.", "int");
                    Add("AbortConnect", "Abort on connect fail.", "bool", false, "False");
                    break;
                case DataSourceType.DynamoDB:
                    Add("ServiceURL", "Custom service URL.");
                    break;
                case DataSourceType.Firebase:
                    Add("ApiKey", "Firebase API key.", "string", true);
                    Add("Database", "Firebase database name.", "string");
                    Add("UserID", "Firebase username.", "string");
                    Add("Password", "Firebase password.", "string");
                    Add("Token", "Firebase auth token.", "string");
                    Add("Url", "Firebase database URL.", "string");
                    break;
                case DataSourceType.LiteDB:
                    break;
                case DataSourceType.ArangoDB:
                    Add("Protocol", "http/https.");
                    break;
                case DataSourceType.Neo4j:
                    Add("Bolt", "Bolt protocol URI.");
                    break;
                case DataSourceType.Cassandra:
                    Add("Consistency", "Default consistency level.");
                    break;
                case DataSourceType.OrientDB:
                    Add("ServerUser", "Server admin user.", "string");
                    Add("ServerPassword", "Server admin password.", "string");
                    Add("PoolSize", "Connection pool size.", "int", false, "10");
                    break;
                case DataSourceType.ElasticSearch:
                    Add("Sniffing", "Enable node sniffing.", "bool", false, "False");
                    break;
                case DataSourceType.ClickHouse:
                    Add("Compression", "Enable compression.", "bool", false, "True");
                    break;
                case DataSourceType.InfluxDB:
                    Add("Org", "Organization name.");
                    Add("Bucket", "Bucket name.");
                    break;
                case DataSourceType.VistaDB:
                    Add("Exclusive", "Open database in exclusive mode.", "bool", false, "False");
                    break;

                // Graph DBs
                case DataSourceType.TigerGraph:
                    Add("GraphName", "Default graph name.", "string");
                    Add("RestppPort", "RESTPP port number.", "int", false, "9000");
                    Add("GSqlPort", "GSQL port number.", "int", false, "14240");
                    break;
                case DataSourceType.JanusGraph:
                    Add("GraphName", "Graph instance name.", "string");
                    Add("StorageBackend", "Storage backend type.", "string", false, "cassandra");
                    Add("IndexBackend", "Index backend type.", "string", false, "elasticsearch");
                    break;

                // Big Data / Columnar
                case DataSourceType.Hadoop:
                    Add("CoreSite", "Path to core-site.xml.", "string");
                    Add("HdfsSite", "Path to hdfs-site.xml.", "string");
                    Add("NameNode", "HDFS NameNode URL.", "string");
                    break;
                case DataSourceType.Kudu:
                    Add("MasterAddresses", "Kudu master server addresses.", "string", true);
                    Add("AdminOperationTimeout", "Admin operation timeout in ms.", "int", false, "30000");
                    Add("OperationTimeout", "Operation timeout in ms.", "int", false, "30000");
                    break;
                case DataSourceType.Druid:
                    Add("BrokerUrl", "Druid broker URL.", "string", true);
                    Add("QueryTimeout", "Query timeout in ms.", "int", false, "300000");
                    break;
                case DataSourceType.Pinot:
                    Add("BrokerUrl", "Pinot broker URL.", "string", true);
                    Add("QueryTimeout", "Query timeout in ms.", "int", false, "300000");
                    break;
                case DataSourceType.Parquet:
                case DataSourceType.Avro:
                case DataSourceType.ORC:
                case DataSourceType.Feather:
                    Add("Compression", "File compression codec.");
                    break;

                // In-Memory
                case DataSourceType.RealIM:
                    Add("BufferSize", "Memory buffer size in MB.", "int", false, "1024");
                    Add("CompressionLevel", "Compression level (0-9).", "int", false, "6");
                    break;
                case DataSourceType.Petastorm:
                    Add("WorkersCount", "Number of worker processes.", "int", false, "4");
                    Add("RowGroupSizeBytes", "Row group size in bytes.", "int", false, "134217728");
                    break;
                case DataSourceType.RocketSet:
                    Add("MemoryLimit", "Memory limit in MB.", "int", false, "2048");
                    Add("CacheSize", "Cache size in MB.", "int", false, "512");
                    break;

                // Cloud services / Data platforms
                case DataSourceType.AWSGlue:
                    Add("CrawlerName", "Glue crawler name.", "string");
                    break;
                case DataSourceType.AWSAthena:
                    Add("S3OutputLocation", "S3 output location for query results.", "string", true);
                    break;
                case DataSourceType.AzureCloud:
                    Add("SubscriptionId", "Azure subscription ID.", "string", true);
                    Add("ResourceGroup", "Azure resource group.", "string");
                    break;
                case DataSourceType.DataBricks:
                    Add("WorkspaceUrl", "Databricks workspace URL.", "string", true);
                    Add("ClusterId", "Databricks cluster ID.", "string");
                    Add("HttpPath", "HTTP path for SQL endpoint.", "string");
                    break;
                case DataSourceType.Firebolt:
                    Add("Engine", "Firebolt engine name.", "string", true);
                    Add("Account", "Firebolt account name.", "string", true);
                    break;
                case DataSourceType.Hologres:
                    Add("InstanceId", "Hologres instance ID.", "string", true);
                    break;
                case DataSourceType.Supabase:
                    Add("ProjectUrl", "Supabase project URL.", "string", true);
                    break;

                // Streaming / Messaging
                case DataSourceType.Kafka:
                    Add("BootstrapServers", "Kafka bootstrap servers (host:port list).");
                    Add("SaslMechanism", "SASL mechanism if security is enabled.");
                    break;
                case DataSourceType.RabbitMQ:
                    Add("VirtualHost", "Virtual host name.");
                    break;
                case DataSourceType.ActiveMQ:
                    Add("BrokerUrl", "ActiveMQ broker URL.", "string", true);
                    break;
                case DataSourceType.Pulsar:
                    Add("ServiceUrl", "Pulsar service URL.", "string", true);
                    Add("AdminUrl", "Pulsar admin URL.", "string");
                    Add("AuthParams", "Authentication parameters.", "string");
                    break;
                case DataSourceType.MassTransit:
                    Add("TransportType", "Transport type (RabbitMQ/AzureServiceBus/AmazonSQS).", "string", true);
                    break;
                case DataSourceType.Nats:
                    Add("Servers", "NATS server URLs.", "string", true);
                    Add("MaxReconnects", "Maximum reconnection attempts.", "int", false, "10");
                    break;
                case DataSourceType.ZeroMQ:
                    Add("SocketType", "ZeroMQ socket type.", "string", true);
                    Add("HighWaterMark", "High water mark for message queue.", "int", false, "1000");
                    break;
                case DataSourceType.AWSKinesis:
                    Add("StreamName", "Kinesis stream name.", "string", true);
                    Add("ShardCount", "Number of shards.", "int", false, "1");
                    break;
                case DataSourceType.AWSSQS:
                    Add("QueueUrl", "SQS queue URL.", "string", true);
                    Add("VisibilityTimeoutSeconds", "Message visibility timeout.", "int", false, "30");
                    break;
                case DataSourceType.AWSSNS:
                    Add("TopicArn", "SNS topic ARN.", "string", true);
                    break;
                case DataSourceType.AzureServiceBus:
                    Add("QueueName", "Queue name.", "string");
                    Add("TopicName", "Topic name.", "string");
                    break;

                // ML / TF etc.
                case DataSourceType.TFRecord:
                    Add("CompressionType", "Compression type (GZIP/ZLIB/None).", "string", false, "None");
                    Add("BufferSize", "Read buffer size in bytes.", "int", false, "8192");
                    break;
                case DataSourceType.ONNX:
                    Add("ExecutionProvider", "ONNX execution provider.", "string", false, "CPUExecutionProvider");
                    Add("GraphOptimizationLevel", "Graph optimization level.", "string", false, "ORT_ENABLE_BASIC");
                    break;
                case DataSourceType.PyTorchData:
                    Add("NumWorkers", "Number of data loading workers.", "int", false, "0");
                    Add("BatchSize", "Batch size for data loading.", "int", false, "1");
                    Add("Shuffle", "Shuffle data.", "bool", false, "False");
                    break;
                case DataSourceType.ScikitLearnData:
                    Add("RandomState", "Random state for reproducibility.", "int", false, "42");
                    Add("TestSize", "Test set size ratio.", "double", false, "0.2");
                    break;

                // File formats
                case DataSourceType.FlatFile:
                case DataSourceType.CSV:
                case DataSourceType.TSV:
                case DataSourceType.Text:
                case DataSourceType.YAML:
                case DataSourceType.Json:
                case DataSourceType.Markdown:
                case DataSourceType.Log:
                case DataSourceType.INI:
                case DataSourceType.XML:
                case DataSourceType.Xls:
                case DataSourceType.Doc:
                case DataSourceType.Docx:
                case DataSourceType.PPT:
                case DataSourceType.PPTX:
                case DataSourceType.PDF:
                case DataSourceType.Onnx:
                case DataSourceType.RecordIO:
                    Add("Encoding", "Text encoding (e.g., UTF-8).", "string");
                    break;

                // Specialized formats
                case DataSourceType.Hdf5:
                    Add("CompressionLevel", "HDF5 compression level (0-9).", "int", false, "6");
                    Add("ChunkCache", "Chunk cache size in bytes.", "int", false, "1048576");
                    break;
                case DataSourceType.LibSVM:
                    Add("ZeroBased", "Use zero-based indexing.", "bool", false, "False");
                    Add("Multiclass", "Enable multiclass support.", "bool", false, "False");
                    break;
                case DataSourceType.GraphML:
                    Add("NodeIdKey", "Node ID attribute key.", "string", false, "id");
                    Add("EdgeIdKey", "Edge ID attribute key.", "string", false, "id");
                    break;
                case DataSourceType.DICOM:
                    Add("TransferSyntax", "DICOM transfer syntax.", "string");
                    Add("CharacterSet", "Character set encoding.", "string", false, "ISO_IR 100");
                    break;
                case DataSourceType.LAS:
                    Add("Version", "LAS file version.", "string", false, "2.0");
                    Add("WrapMode", "Line wrap mode.", "string", false, "NO");
                    break;

                // Workflow Systems
                case DataSourceType.AWSSWF:
                    Add("TaskList", "Task list name.", "string");
                    break;
                case DataSourceType.AWSStepFunctions:
                    Add("StateMachineArn", "Step Functions state machine ARN.", "string", true);
                    break;

                // IoT
                case DataSourceType.AWSIoT:
                case DataSourceType.AWSIoTCore:
                    Add("ThingName", "IoT thing name.", "string");
                    break;
                case DataSourceType.AWSIoTAnalytics:
                    Add("ChannelName", "IoT Analytics channel name.", "string", true);
                    Add("DatasetName", "Dataset name.", "string");
                    break;

                // Search
                case DataSourceType.Solr:
                    Add("Collection", "Default collection name.");
                    break;

                // Industrial / Specialized
                case DataSourceType.OPC:
                    Add("UpdateRate", "Data update rate in milliseconds.", "int", false, "1000");
                    Add("GroupActive", "Activate OPC group.", "bool", false, "True");
                    break;

                // Miscellaneous / Connectors / Drivers
                case DataSourceType.DuckDB:
                    Add("AccessMode", "Database access mode.", "string", false, "automatic");
                    break;
                case DataSourceType.GoogleSheets:
                    Add("SpreadsheetId", "Google sheet ID.");
                    Add("Range", "Default range.");
                    break;
                case DataSourceType.MiModel:
                    Add("ModelPath", "Path to machine learning model.", "string", true);
                    Add("ModelFormat", "Model format (ONNX/TensorFlow/PyTorch).", "string", true);
                    break;
                case DataSourceType.Presto:
                case DataSourceType.Trino:
                    Add("Catalog", "Default catalog name.", "string", true);
                    Add("Source", "Query source identifier.", "string");
                    break;
                case DataSourceType.TimeScale:
                    Add("Extension", "TimescaleDB extension name.", "string", false, "timescaledb");
                    Add("ChunkTimeInterval", "Chunk time interval.", "string", false, "7 days");
                    AddCommonDbPoolOptions();
                    break;
                case DataSourceType.WebApi:
                case DataSourceType.RestApi:
                    Add("Accept", "Default Accept header value.");
                    break;
                case DataSourceType.GraphQL:
                    break;
                case DataSourceType.OData:
                    Add("ServiceRoot", "OData service root URI.");
                    break;
                case DataSourceType.ODBC:
                case DataSourceType.OLEDB:
                case DataSourceType.ADO:
                    AddCommonDbPoolOptions();
                    break;

                // Vector DB
                case DataSourceType.ChromaDB:
                    Add("Collection", "Default collection name.", "string");
                    break;
                case DataSourceType.PineCone:
                    Add("Url", "Pinecone endpoint URL.", "string", true);
                    Add("ApiKey", "Pinecone API key.", "string", true);
                    break;
                case DataSourceType.Qdrant:
                    Add("Collection", "Default collection name.", "string");
                    break;
                case DataSourceType.ShapVector:
                    break;
                case DataSourceType.Weaviate:
                    Add("Scheme", "Connection scheme (http/https).", "string", false, "http");
                    break;
                case DataSourceType.Milvus:
                    Add("Collection", "Default collection name.", "string");
                    break;
                case DataSourceType.RedisVector:
                    Add("VectorDimension", "Vector dimension size.", "int", true);
                    Add("IndexName", "Vector index name.", "string");
                    break;
                case DataSourceType.Zilliz:
                    Add("ClusterEndpoint", "Zilliz cluster endpoint.", "string", true);
                    Add("Collection", "Default collection name.", "string");
                    break;
                case DataSourceType.Vespa:
                    Add("Application", "Vespa application name.", "string");
                    break;

                // Extra relational
                case DataSourceType.MariaDB:
                    Add("Convert Zero Datetime", "Convert zero datetime values.", "bool", false, "False");
                    AddCommonDbPoolOptions();
                    break;

                // CRM/Marketing/E-com/etc connectors - Only truly unique parameters
                case DataSourceType.Salesforce:
                    Add("Environment", "Salesforce environment (Production/Sandbox).", "string", false, "Production");
                    Add("ApiVersion", "Salesforce API version.", "string", false, "v59.0");
                    break;
                case DataSourceType.HubSpot:
                    Add("PortalId", "HubSpot portal ID.", "string");
                    break;
                case DataSourceType.Zoho:
                    Add("DataCenter", "Zoho data center (US/EU/IN/AU).", "string", false, "US");
                    Add("Environment", "Environment (Production/Sandbox).", "string", false, "Production");
                    break;
                case DataSourceType.Pipedrive:
                    Add("CompanyDomain", "Pipedrive company domain.", "string", true);
                    Add("ApiToken", "Pipedrive API token.", "string", true);
                    break;
                case DataSourceType.MicrosoftDynamics365:
                    Add("InstanceUrl", "Dynamics 365 instance URL.", "string", true);
                    Add("Version", "API version.", "string", false, "9.1");
                    break;
                case DataSourceType.Freshsales:
                case DataSourceType.SugarCRM:
                case DataSourceType.Insightly:
                case DataSourceType.Copper:
                case DataSourceType.Nutshell:
                case DataSourceType.Mailchimp:
                case DataSourceType.Marketo:
                case DataSourceType.ActiveCampaign:
                case DataSourceType.ConstantContact:
                case DataSourceType.Klaviyo:
                case DataSourceType.Sendinblue:
                case DataSourceType.CampaignMonitor:
                case DataSourceType.ConvertKit:
                case DataSourceType.Drip:
                case DataSourceType.MailerLite:
                    Add("DataCenter", "Data center region.", "string");
                    break;
                case DataSourceType.GoogleAds:
                    Add("DeveloperToken", "Google Ads developer token.", "string", true);
                    Add("CustomerId", "Google Ads customer ID.", "string", true);
                    break;
                case DataSourceType.Shopify:
                case DataSourceType.WooCommerce:
                case DataSourceType.Magento:
                case DataSourceType.BigCommerce:
                case DataSourceType.Squarespace:
                case DataSourceType.Wix:
                case DataSourceType.Etsy:
                case DataSourceType.OpenCart:
                case DataSourceType.Ecwid:
                case DataSourceType.Volusion:
                    Add("StoreUrl", "Store URL.", "string", true);
                    break;
                case DataSourceType.Jira:
                case DataSourceType.Trello:
                case DataSourceType.Asana:
                case DataSourceType.Monday:
                case DataSourceType.ClickUp:
                case DataSourceType.Basecamp:
                case DataSourceType.Notion:
                case DataSourceType.Wrike:
                case DataSourceType.Smartsheet:
                case DataSourceType.Teamwork:
                case DataSourceType.Podio:
                    Add("ApiToken", "API token.", "string", true);
                    break;
                case DataSourceType.Slack:
                case DataSourceType.MicrosoftTeams:
                case DataSourceType.Zoom:
                case DataSourceType.GoogleChat:
                case DataSourceType.Discord:
                case DataSourceType.Telegram:
                case DataSourceType.WhatsAppBusiness:
                case DataSourceType.Twist:
                case DataSourceType.Chanty:
                case DataSourceType.RocketChat:
                case DataSourceType.Flock:
                    Add("BotToken", "Bot token.", "string", true);
                    Add("WebhookUrl", "Webhook URL.", "string");
                    break;
                case DataSourceType.GoogleDrive:
                case DataSourceType.Dropbox:
                case DataSourceType.OneDrive:
                case DataSourceType.Box:
                case DataSourceType.pCloud:
                case DataSourceType.iCloud:
                case DataSourceType.Egnyte:
                case DataSourceType.MediaFire:
                case DataSourceType.CitrixShareFile:
                    Add("RootFolder", "Root folder path.", "string");
                    Add("ChunkSize", "Upload chunk size in bytes.", "int", false, "8388608");
                    break;
                case DataSourceType.AmazonS3:
                    Add("BucketName", "S3 bucket name.", "string", true);
                    Add("Prefix", "Object key prefix.", "string");
                    break;
                case DataSourceType.Mega:
                case DataSourceType.Backblaze:
                    Add("BucketName", "Bucket name.", "string", true);
                    break;
                case DataSourceType.GoogleCloudStorage:
                    Add("BucketName", "GCS bucket name.", "string", true);
                    Add("ProjectId", "GCP project ID.", "string", true);
                    break;
                case DataSourceType.Stripe:
                case DataSourceType.PayPal:
                case DataSourceType.Square:
                case DataSourceType.AuthorizeNet:
                case DataSourceType.Braintree:
                case DataSourceType.Worldpay:
                case DataSourceType.Adyen:
                case DataSourceType.TwoCheckout:
                case DataSourceType.Razorpay:
                case DataSourceType.Payoneer:
                case DataSourceType.Wise:
                case DataSourceType.Coinbase:
                case DataSourceType.Venmo:
                case DataSourceType.BitPay:
                    Add("Environment", "Environment (Sandbox/Production).", "string", false, "Sandbox");
                    break;
                case DataSourceType.Facebook:
                case DataSourceType.Twitter:
                case DataSourceType.Instagram:
                case DataSourceType.LinkedIn:
                case DataSourceType.Pinterest:
                case DataSourceType.YouTube:
                case DataSourceType.TikTok:
                case DataSourceType.Snapchat:
                case DataSourceType.Reddit:
                case DataSourceType.Threads:
                case DataSourceType.Mastodon:
                case DataSourceType.Bluesky:
                    Add("AppId", "Application ID.", "string", true);
                    Add("AppSecret", "Application secret.", "string", true);
                    break;
                case DataSourceType.Buffer:
                case DataSourceType.Hootsuite:
                case DataSourceType.TikTokAds:
                case DataSourceType.Zapier:
                case DataSourceType.Make:
                case DataSourceType.Integromat:
                case DataSourceType.TrayIO:
                case DataSourceType.MicrosoftPowerAutomate:
                    Add("WebhookUrl", "Webhook URL.", "string", true);
                    break;
                case DataSourceType.Airtable:
                    Add("BaseId", "Airtable base ID.", "string", true);
                    break;
                case DataSourceType.Calendly:
                case DataSourceType.Doodle:
                case DataSourceType.Eventbrite:
                case DataSourceType.GitHub:
                case DataSourceType.GitLab:
                case DataSourceType.Bitbucket:
                    Add("PersonalAccessToken", "Personal access token.", "string", true);
                    break;
                case DataSourceType.Jenkins:
                case DataSourceType.CircleCI:
                    Add("ApiToken", "API token.", "string", true);
                    break;
                case DataSourceType.Postman:
                case DataSourceType.SwaggerHub:
                    Add("WorkspaceId", "Workspace ID.", "string");
                    break;
                case DataSourceType.AzureDevOps:
                case DataSourceType.AzureBoards:
                    Add("Organization", "Azure DevOps organization.", "string", true);
                    Add("PersonalAccessToken", "Personal access token.", "string", true);
                    break;
                case DataSourceType.Zendesk:
                case DataSourceType.Freshdesk:
                case DataSourceType.HelpScout:
                case DataSourceType.ZohoDesk:
                case DataSourceType.Kayako:
                case DataSourceType.LiveAgent:
                case DataSourceType.Front:
                    Add("Subdomain", "Service subdomain.", "string", true);
                    Add("ApiToken", "API token.", "string", true);
                    break;
                case DataSourceType.GoogleAnalytics:
                    Add("ViewId", "Google Analytics view ID.", "string", true);
                    Add("PropertyId", "GA4 property ID.", "string");
                    break;
                case DataSourceType.Mixpanel:
                case DataSourceType.Hotjar:
                case DataSourceType.Amplitude:
                case DataSourceType.Heap:
                    Add("ProjectId", "Project ID.", "string", true);
                    Add("ApiSecret", "API secret.", "string", true);
                    break;
                case DataSourceType.Databox:
                case DataSourceType.Geckoboard:
                case DataSourceType.Cyfe:
                case DataSourceType.Mailgun:
                case DataSourceType.SendGrid:
                    break;
                case DataSourceType.Twilio:
                    Add("AccountSid", "Twilio account SID.", "string", true);
                    Add("AuthToken", "Auth token.", "string", true);
                    break;
                case DataSourceType.Plaid:
                    Add("Secret", "Plaid secret.", "string", true);
                    Add("Environment", "Environment (sandbox/development/production).", "string", false, "sandbox");
                    break;
                case DataSourceType.DocuSign:
                    Add("IntegratorKey", "DocuSign integrator key.", "string", true);
                    break;
                case DataSourceType.PhilipsHue:
                case DataSourceType.Nest:
                case DataSourceType.SmartThings:
                case DataSourceType.Tuya:
                    Add("BridgeIp", "Smart device bridge IP.", "string", true);
                    break;
                case DataSourceType.FreshBooks:
                case DataSourceType.WaveApps:
                case DataSourceType.SageBusinessCloud:
                case DataSourceType.MYOB:
                case DataSourceType.QuickBooks:
                case DataSourceType.Xero:
                case DataSourceType.BenchAccounting:
                    Add("CompanyId", "Company ID.", "string", true);
                    break;
                case DataSourceType.SAPCRM:
                case DataSourceType.OracleCRM:
                    break;
                case DataSourceType.HootsuiteMarketing:
                case DataSourceType.Criteo:
                    break;
                case DataSourceType.PrestaShop:
                case DataSourceType.BigCartel:
                    Add("ShopUrl", "Shop URL.", "string", true);
                    break;
                case DataSourceType.SmartsheetPM:
                    break;
                case DataSourceType.Mattermost:
                case DataSourceType.RocketChatComm:
                    Add("PersonalAccessToken", "Personal access token.", "string", true);
                    break;
                case DataSourceType.SonarQube:
                    break;
                case DataSourceType.Intercom:
                case DataSourceType.Drift:
                    break;
                case DataSourceType.Tableau:
                case DataSourceType.PowerBI:
                    Add("SiteName", "Site name.", "string");
                    break;
                case DataSourceType.Particle:
                case DataSourceType.ArduinoCloud:
                    Add("DeviceId", "IoT device ID.", "string", true);
                    break;
                case DataSourceType.Ethereum:
                case DataSourceType.Hyperledger:
                case DataSourceType.BitcoinCore:
                    Add("NodeUrl", "Blockchain node URL.", "string", true);
                    Add("NetworkId", "Network ID.", "string");
                    break;

                // Web APIs already covered above
                // default fallback: do nothing
                default:
                    break;
            }

            return defs;
        }
        // Returns parameter definitions for a given data source type.
        // Only include parameters used in DatasourceType and    in IConnectionProperties.
        public static Dictionary<string, parameterinfo> GetAllParametersForDataSourceTypeInConnectionProperties(DataSourceType dataSourceType)
        {
            var defs = new Dictionary<string, parameterinfo>(StringComparer.OrdinalIgnoreCase);


            void Add(string name, string desc, string type = "string", bool required = false, string def = null)
            {
                defs[name] = new parameterinfo
                {
                    Name = name,
                    Description = desc,
                    DataType = type,
                    IsRequired = required,
                    DefaultValue = def,
                    Value = def
                };
            }

            switch (dataSourceType)
            {
                // RDBMS - Only parameters that exist as properties in ConnectionProperties
                case DataSourceType.SqlServer:
                case DataSourceType.AzureSQL:
                    Add("Host", "Database server hostname or IP address.", "string", true);
                    Add("Port", "Database server port number.", "int", false, "1433");
                    Add("Database", "Database name.", "string", true);
                    Add("UserID", "Database username.", "string");
                    Add("Password", "Database password.", "string");
                    Add("Timeout", "Connection timeout in seconds.", "int", false, "30");
                    break;

                case DataSourceType.Mysql:
                case DataSourceType.MariaDB:
                    Add("Host", "Database server hostname or IP address.", "string", true);
                    Add("Port", "Database server port number.", "int", false, "3306");
                    Add("Database", "Database name.", "string", true);
                    Add("UserID", "Database username.", "string");
                    Add("Password", "Database password.", "string");
                    Add("Timeout", "Connection timeout in seconds.", "int", false, "30");
                    break;

                case DataSourceType.Postgre:
                    Add("Host", "Database server hostname or IP address.", "string", true);
                    Add("Port", "Database server port number.", "int", false, "5432");
                    Add("Database", "Database name.", "string", true);
                    Add("UserID", "Database username.", "string");
                    Add("Password", "Database password.", "string");
                    Add("Timeout", "Connection timeout in seconds.", "int", false, "30");
                    Add("SchemaName", "Default schema name.", "string");
                    break;

                case DataSourceType.Oracle:
                    Add("Host", "Database server hostname or IP address.", "string", true);
                    Add("Port", "Database server port number.", "int", false, "1521");
                    Add("Database", "Database name or SID.", "string", true);
                    Add("OracleSIDorService", "Oracle SID or Service Name.", "string");
                    Add("UserID", "Database username.", "string");
                    Add("Password", "Database password.", "string");
                    Add("Timeout", "Connection timeout in seconds.", "int", false, "30");
                    break;

                case DataSourceType.DB2:
                case DataSourceType.FireBird:
                case DataSourceType.Hana:
                case DataSourceType.Cockroach:
                case DataSourceType.TerraData:
                case DataSourceType.Vertica:
                case DataSourceType.AWSRDS:
                case DataSourceType.AWSRedshift:
                case DataSourceType.TimeScale:
                case DataSourceType.ODBC:
                case DataSourceType.OLEDB:
                case DataSourceType.ADO:
                    Add("Host", "Database server hostname or IP address.", "string", true);
                    Add("Port", "Database server port number.", "int");
                    Add("Database", "Database name.", "string", true);
                    Add("UserID", "Database username.", "string");
                    Add("Password", "Database password.", "string");
                    Add("Timeout", "Connection timeout in seconds.", "int", false, "30");
                    break;

                case DataSourceType.SqlLite:
                case DataSourceType.LiteDB:
                case DataSourceType.VistaDB:
                case DataSourceType.DuckDB:
                    Add("FilePath", "Database file path.", "string", true);
                    Add("Password", "Database password (if encrypted).", "string");
                    break;

                case DataSourceType.SnowFlake:
                    Add("Host", "Snowflake account hostname.", "string", true);
                    Add("Database", "Database name.", "string", true);
                    Add("UserID", "Username.", "string", true);
                    Add("Password", "Password.", "string", true);
                    Add("SchemaName", "Schema name.", "string");
                    break;

                case DataSourceType.Spanner:
                    Add("Host", "Spanner endpoint host.", "string", true);
                    Add("Port", "Spanner endpoint port.", "int");
                    Add("Database", "Spanner database name.", "string", true);
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.GoogleBigQuery:
                    Add("Database", "BigQuery dataset name.", "string");
                    Add("Url", "BigQuery API base URL.", "string");
                    Add("ClientId", "OAuth client ID.", "string");
                    Add("ClientSecret", "OAuth client secret.", "string");
                    Add("OAuthScope", "OAuth scope.", "string");
                    Add("OAuthTokenEndpoint", "OAuth token endpoint.", "string");
                    break;

                // NoSQL / Document / Columnar DBs
                case DataSourceType.MongoDB:
                    Add("Host", "MongoDB server hostname.", "string", true);
                    Add("Port", "MongoDB server port.", "int", false, "27017");
                    Add("Database", "Database name.", "string", true);
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.CouchDB:
                case DataSourceType.RavenDB:
                case DataSourceType.Couchbase:
                    Add("Host", "Server hostname.", "string", true);
                    Add("Port", "Server port.", "int");
                    Add("Database", "Database name.", "string");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.Redis:
                    Add("Host", "Redis server hostname.", "string", true);
                    Add("Port", "Redis server port.", "int", false, "6379");
                    Add("Password", "Redis password.", "string");
                    break;

                case DataSourceType.DynamoDB:
                    Add("Region", "AWS region.", "string", true);
                    Add("ApiKey", "AWS access key.", "string", true);
                    break;

                case DataSourceType.Firebase:
                    Add("Url", "Firebase database URL.", "string", true);
                    Add("ApiKey", "Firebase API key.", "string", true);
                    Add("Database", "Firebase database name.", "string");
                    Add("UserID", "Firebase username.", "string");
                    Add("Password", "Firebase password.", "string");
                    Add("Token", "Firebase auth token.", "string");
                    break;

                case DataSourceType.ArangoDB:
                    Add("Host", "ArangoDB server hostname.", "string", true);
                    Add("Port", "ArangoDB server port.", "int", false, "8529");
                    Add("Database", "Database name.", "string", true);
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.Neo4j:
                    Add("Host", "Neo4j server hostname.", "string", true);
                    Add("Port", "Neo4j server port.", "int", false, "7687");
                    Add("Database", "Database name.", "string");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.Cassandra:
                    Add("Host", "Cassandra server hostname.", "string", true);
                    Add("Port", "Cassandra server port.", "int", false, "9042");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.OrientDB:
                    Add("Host", "OrientDB server hostname.", "string", true);
                    Add("Port", "OrientDB server port.", "int", false, "2424");
                    Add("Database", "Database name.", "string", true);
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.ElasticSearch:
                    Add("Host", "Elasticsearch hostname.", "string", true);
                    Add("Port", "Elasticsearch port.", "int", false, "9200");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;
                case DataSourceType.ClickHouse:
                    Add("Host", "ClickHouse hostname.", "string", true);
                    Add("Port", "ClickHouse port.", "int", false, "8123");
                    Add("Database", "Database name.", "string");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;
                case DataSourceType.InfluxDB:
                    Add("Host", "InfluxDB hostname.", "string", true);
                    Add("Port", "InfluxDB port.", "int", false, "8086");
                    Add("Database", "Database name.", "string");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                // Graph DBs
                case DataSourceType.TigerGraph:
                    Add("Host", "TigerGraph hostname.", "string", true);
                    Add("Port", "TigerGraph port.", "int", false, "14240");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.JanusGraph:
                    Add("Host", "JanusGraph hostname.", "string", true);
                    Add("Port", "JanusGraph port.", "int");
                    break;

                // Big Data / Columnar
                case DataSourceType.Hadoop:
                    Add("Host", "Hadoop NameNode hostname.", "string", true);
                    Add("Port", "Hadoop NameNode port.", "int", false, "9000");
                    break;

                case DataSourceType.Kudu:
                    Add("Host", "Kudu master hostname.", "string", true);
                    Add("Port", "Kudu master port.", "int", false, "7051");
                    break;

                case DataSourceType.Druid:
                case DataSourceType.Pinot:
                    Add("Host", "Broker hostname.", "string", true);
                    Add("Port", "Broker port.", "int");
                    break;

                // File formats
                case DataSourceType.Parquet:
                case DataSourceType.Avro:
                case DataSourceType.ORC:
                case DataSourceType.Feather:
                case DataSourceType.FlatFile:
                case DataSourceType.CSV:
                case DataSourceType.TSV:
                case DataSourceType.Text:
                case DataSourceType.YAML:
                case DataSourceType.Json:
                case DataSourceType.Markdown:
                case DataSourceType.Log:
                case DataSourceType.INI:
                case DataSourceType.XML:
                case DataSourceType.Xls:
                case DataSourceType.Doc:
                case DataSourceType.Docx:
                case DataSourceType.PPT:
                case DataSourceType.PPTX:
                case DataSourceType.PDF:
                case DataSourceType.Onnx:
                case DataSourceType.RecordIO:
                case DataSourceType.Hdf5:
                case DataSourceType.LibSVM:
                case DataSourceType.GraphML:
                case DataSourceType.DICOM:
                case DataSourceType.LAS:
                    Add("FilePath", "File path.", "string", true);
                    Add("FileName", "File name.", "string");
                    break;

                // Cloud services / Data platforms
                case DataSourceType.AWSGlue:
                case DataSourceType.AWSAthena:
                    Add("Region", "AWS region.", "string", true);
                    Add("Database", "Database name.", "string");
                    Add("ApiKey", "Access key ID.", "string");
                    Add("KeyToken", "Secret access key.", "string");
                    Add("Url", "Service endpoint URL.", "string");
                    break;

                case DataSourceType.AzureCloud:
                    Add("Url", "Azure resource URL.", "string");
                    Add("Authority", "Authority (e.g., https://login.microsoftonline.com).", "string");
                    Add("TenantId", "Azure AD tenant ID.", "string");
                    Add("ApplicationId", "Application (client) ID.", "string");
                    Add("ClientId", "Client ID.", "string");
                    Add("ClientSecret", "Client secret.", "string");
                    Add("Resource", "Resource identifier.", "string");
                    Add("Scope", "OAuth scope.", "string");
                    Add("AuthUrl", "Authorization URL.", "string");
                    Add("TokenUrl", "Token URL.", "string");
                    Add("RedirectUri", "Redirect URI.", "string");
                    break;

                case DataSourceType.MicrosoftDynamics365:
                    Add("Url", "Dynamics 365 URL.", "string", true);
                    Add("UserID", "Username.", "string", true);
                    Add("Password", "Password.", "string", true);
                    break;

                // AI/ML Vector Databases
                case DataSourceType.PineCone:
                    Add("ApiKey", "Pinecone API key.", "string", true);
                    Add("Url", "Pinecone API endpoint URL.", "string");
                    break;

                case DataSourceType.Weaviate:
                    Add("Host", "Weaviate server hostname.", "string", true);
                    Add("Port", "Weaviate server port.", "int", false, "8080");
                    Add("ApiKey", "Weaviate API key.", "string");
                    break;

                case DataSourceType.Qdrant:
                    Add("Host", "Qdrant server hostname.", "string", true);
                    Add("Port", "Qdrant server port.", "int", false, "6333");
                    Add("ApiKey", "Qdrant API key.", "string");
                    break;

                case DataSourceType.Milvus:
                    Add("Host", "Milvus server hostname.", "string", true);
                    Add("Port", "Milvus server port.", "int", false, "19530");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.ChromaDB:
                    Add("Host", "ChromaDB server hostname.", "string", true);
                    Add("Port", "ChromaDB server port.", "int", false, "8000");
                    break;

                // Additional APIs / Web Services
                case DataSourceType.RestApi:
                    Add("Url", "API endpoint URL.", "string", true);
                    Add("ApiKey", "API key.", "string");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    Add("HttpMethod", "HTTP method.", "string");
                    break;

                case DataSourceType.GraphQL:
                    Add("Url", "GraphQL endpoint URL.", "string", true);
                    Add("ApiKey", "API key.", "string");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.WebApi:
                    Add("Url", "Web API endpoint URL.", "string", true);
                    Add("ApiKey", "API key.", "string");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    Add("HttpMethod", "HTTP method.", "string");
                    break;

                case DataSourceType.OData:
                    Add("Url", "OData service URL.", "string", true);
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                // Additional Cloud Services
                case DataSourceType.DataBricks:
                    Add("Host", "Databricks workspace URL.", "string", true);
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    Add("ApiKey", "Access token.", "string");
                    break;

                case DataSourceType.Firebolt:
                    Add("Database", "Firebolt database name.", "string", true);
                    Add("UserID", "Username.", "string", true);
                    Add("Password", "Password.", "string", true);
                    break;

                case DataSourceType.Hologres:
                    Add("Host", "Hologres server hostname.", "string", true);
                    Add("Port", "Hologres server port.", "int", false, "80");
                    Add("Database", "Database name.", "string", true);
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.Supabase:
                    Add("Url", "Supabase project URL.", "string", true);
                    Add("ApiKey", "Supabase API key.", "string", true);
                    Add("Database", "Database name.", "string");
                    break;

                // Additional NoSQL Databases
                // MariaDB is handled above with MySQL

                // Additional File Formats
                case DataSourceType.GoogleSheets:
                    Add("Url", "Google Sheets URL.", "string", true);
                    Add("ApiKey", "Google API key.", "string", true);
                    break;

                case DataSourceType.MiModel:
                    Add("FilePath", "Model file path.", "string", true);
                    break;

                case DataSourceType.Presto:
                case DataSourceType.Trino:
                    Add("Host", "Presto/Trino coordinator hostname.", "string", true);
                    Add("Port", "Coordinator port.", "int", false, "8080");
                    Add("UserID", "Username.", "string");
                    Add("Database", "Catalog name.", "string");
                    break;

                // Workflow Systems
                case DataSourceType.AWSSWF:
                    Add("Region", "AWS region.", "string", true);
                    Add("ApiKey", "AWS access key ID.", "string", true);
                    Add("KeyToken", "AWS secret access key.", "string", true);
                    break;

                case DataSourceType.AWSStepFunctions:
                    Add("Region", "AWS region.", "string", true);
                    Add("ApiKey", "AWS access key ID.", "string", true);
                    Add("KeyToken", "AWS secret access key.", "string", true);
                    break;

                // IoT Services
                case DataSourceType.AWSIoT:
                case DataSourceType.AWSIoTCore:
                case DataSourceType.AWSIoTAnalytics:
                    Add("Region", "AWS region.", "string", true);
                    Add("ApiKey", "AWS access key ID.", "string", true);
                    Add("KeyToken", "AWS secret access key.", "string", true);
                    Add("Host", "IoT endpoint.", "string");
                    break;

                // Search Platforms
                case DataSourceType.Solr:
                    Add("Host", "Solr server hostname.", "string", true);
                    Add("Port", "Solr server port.", "int", false, "8983");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                // Additional Vector Databases
                case DataSourceType.RedisVector:
                    Add("Host", "Redis server hostname.", "string", true);
                    Add("Port", "Redis server port.", "int", false, "6379");
                    Add("Password", "Redis password.", "string");
                    break;

                case DataSourceType.Zilliz:
                    Add("Host", "Zilliz server hostname.", "string", true);
                    Add("Port", "Zilliz server port.", "int");
                    Add("ApiKey", "Zilliz API key.", "string", true);
                    break;

                case DataSourceType.Vespa:
                    Add("Host", "Vespa server hostname.", "string", true);
                    Add("Port", "Vespa server port.", "int");
                    break;

                // CRM Systems
                case DataSourceType.Salesforce:
                    Add("Url", "Salesforce instance URL.", "string", true);
                    Add("UserID", "Username.", "string", true);
                    Add("Password", "Password.", "string", true);
                    Add("ClientId", "Connected app consumer key.", "string", true);
                    Add("ClientSecret", "Connected app consumer secret.", "string", true);
                    break;

                case DataSourceType.HubSpot:
                    Add("Url", "HubSpot API URL.", "string", false, "https://api.hubapi.com");
                    Add("ApiKey", "HubSpot API key.", "string", true);
                    Add("PortalId", "HubSpot portal ID.", "string");
                    break;

                case DataSourceType.Zoho:
                    Add("Url", "Zoho API base URL.", "string", true);
                    Add("ApiKey", "Zoho API key.", "string", true);
                    Add("DataCenter", "Zoho data center (US/EU/IN/AU).", "string", false, "US");
                    Add("Environment", "Environment (Production/Sandbox).", "string", false, "Production");
                    break;

                case DataSourceType.Pipedrive:
                    Add("ApiKey", "Pipedrive API token.", "string", true);
                    Add("CompanyDomain", "Pipedrive company domain.", "string", true);
                    break;

                case DataSourceType.Freshsales:
                    Add("Url", "Freshsales domain URL.", "string", true);
                    Add("ApiKey", "Freshsales API key.", "string", true);
                    break;

                case DataSourceType.SugarCRM:
                    Add("Url", "SugarCRM instance URL.", "string", true);
                    Add("UserID", "Username.", "string", true);
                    Add("Password", "Password.", "string", true);
                    break;

                case DataSourceType.Insightly:
                case DataSourceType.Copper:
                case DataSourceType.Nutshell:
                    Add("ApiKey", "API key.", "string", true);
                    Add("Url", "Instance URL.", "string", true);
                    break;

                // Marketing Platforms
                case DataSourceType.Mailchimp:
                    Add("ApiKey", "Mailchimp API key.", "string", true);
                    Add("Url", "Mailchimp API base URL.", "string");
                    break;

                case DataSourceType.Marketo:
                    Add("Url", "Marketo SOAP/REST endpoint URL.", "string", true);
                    Add("ClientId", "Marketo client ID.", "string", true);
                    Add("ClientSecret", "Marketo client secret.", "string", true);
                    break;

                case DataSourceType.GoogleAds:
                    Add("ClientId", "OAuth client ID.", "string", true);
                    Add("ClientSecret", "OAuth client secret.", "string", true);
                    Add("ApiKey", "Developer token.", "string", true);
                    break;

                case DataSourceType.ActiveCampaign:
                    Add("Url", "ActiveCampaign API URL.", "string", true);
                    Add("ApiKey", "ActiveCampaign API key.", "string", true);
                    break;

                case DataSourceType.Klaviyo:
                    Add("Url", "Klaviyo API URL.", "string", false, "https://a.klaviyo.com/api");
                    Add("ApiKey", "Klaviyo API key.", "string", true);
                    break;

                case DataSourceType.Sendinblue:
                    Add("Url", "Sendinblue API URL.", "string", false, "https://api.sendinblue.com");
                    Add("ApiKey", "Sendinblue API key.", "string", true);
                    break;

                case DataSourceType.CampaignMonitor:
                    Add("Url", "Campaign Monitor API URL.", "string", false, "https://api.createsend.com");
                    Add("ApiKey", "Campaign Monitor API key.", "string", true);
                    break;

                case DataSourceType.ConvertKit:
                case DataSourceType.Drip:
                    Add("Url", "API URL.", "string", false, "https://api.convertkit.com");
                    Add("ApiKey", "API key.", "string", true);
                    break;

                case DataSourceType.MailerLite:
                    Add("Url", "MailerLite API URL.", "string", false, "https://api.mailerlite.com");
                    Add("ApiKey", "MailerLite API key.", "string", true);
                    break;

                // E-commerce Platforms
                case DataSourceType.Shopify:
                    Add("Url", "Shopify store URL.", "string", true);
                    Add("ApiKey", "Shopify API key.", "string", true);
                    Add("Password", "Shopify API password.", "string", true);
                    break;

                case DataSourceType.WooCommerce:
                    Add("Url", "WooCommerce store URL.", "string", true);
                    Add("ApiKey", "WooCommerce consumer key.", "string", true);
                    Add("Password", "WooCommerce consumer secret.", "string", true);
                    break;

                case DataSourceType.Magento:
                    Add("Url", "Magento base URL.", "string", true);
                    Add("ApiKey", "Magento API key.", "string");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.BigCommerce:
                    Add("Url", "BigCommerce store URL.", "string", true);
                    Add("ClientId", "BigCommerce client ID.", "string", true);
                    Add("ClientSecret", "BigCommerce client secret.", "string", true);
                    break;

                case DataSourceType.Squarespace:
                case DataSourceType.Wix:
                case DataSourceType.Etsy:
                case DataSourceType.OpenCart:
                case DataSourceType.Ecwid:
                case DataSourceType.Volusion:
                    Add("ApiKey", "API key.", "string", true);
                    Add("Url", "Store URL.", "string");
                    break;

                // Project Management
                case DataSourceType.Jira:
                    Add("Url", "Jira instance URL.", "string", true);
                    Add("UserID", "Username.", "string", true);
                    Add("Password", "Password.", "string", true);
                    Add("ApiKey", "API token.", "string");
                    break;

                case DataSourceType.Trello:
                    Add("Url", "Trello API URL.", "string", false, "https://api.trello.com");
                    Add("ApiKey", "Trello API key.", "string", true);
                    Add("UserID", "Trello token.", "string", true);
                    break;

                case DataSourceType.Asana:
                    Add("Url", "Asana API URL.", "string", false, "https://app.asana.com/api");
                    Add("ApiKey", "Asana personal access token.", "string", true);
                    break;

                case DataSourceType.Monday:
                    Add("Url", "Monday.com API URL.", "string", false, "https://api.monday.com");
                    Add("ApiKey", "Monday.com API token.", "string", true);
                    break;

                case DataSourceType.ClickUp:
                    Add("Url", "ClickUp API URL.", "string", false, "https://api.clickup.com");
                    Add("ApiKey", "ClickUp API token.", "string", true);
                    break;

                case DataSourceType.Basecamp:
                    Add("Url", "Basecamp account URL.", "string", true);
                    Add("UserID", "Username.", "string", true);
                    Add("Password", "Password.", "string", true);
                    break;

                case DataSourceType.Notion:
                    Add("Url", "Notion API URL.", "string", false, "https://api.notion.com");
                    Add("ApiKey", "Notion integration token.", "string", true);
                    break;

                case DataSourceType.Wrike:
                    Add("Url", "Wrike API URL.", "string", false, "https://www.wrike.com/api");
                    Add("ApiKey", "Wrike permanent access token.", "string", true);
                    break;

                case DataSourceType.Smartsheet:
                    Add("Url", "Smartsheet API URL.", "string", false, "https://api.smartsheet.com");
                    Add("ApiKey", "Smartsheet access token.", "string", true);
                    break;

                case DataSourceType.Teamwork:
                case DataSourceType.Podio:
                    Add("Url", "API URL.", "string");
                    Add("ApiKey", "API key.", "string", true);
                    break;

                // Communication Platforms
                case DataSourceType.Slack:
                    Add("Url", "Slack API URL.", "string", false, "https://slack.com/api");
                    Add("ApiKey", "Slack bot token.", "string", true);
                    break;

                case DataSourceType.MicrosoftTeams:
                    Add("ClientId", "Azure app client ID.", "string", true);
                    Add("ClientSecret", "Azure app client secret.", "string", true);
                    Add("TenantId", "Azure tenant ID.", "string", true);
                    break;

                case DataSourceType.Zoom:
                    Add("Url", "Zoom API URL.", "string", false, "https://api.zoom.us");
                    Add("ApiKey", "Zoom API key.", "string", true);
                    Add("ClientSecret", "Zoom API secret.", "string", true);
                    break;

                case DataSourceType.GoogleChat:
                    Add("Url", "Google Chat API URL.", "string", false, "https://chat.googleapis.com");
                    Add("ApiKey", "Google Chat API key.", "string", true);
                    break;

                case DataSourceType.Discord:
                    Add("Url", "Discord API URL.", "string", false, "https://discord.com/api");
                    Add("ApiKey", "Discord bot token.", "string", true);
                    break;

                case DataSourceType.Telegram:
                    Add("Url", "Telegram API URL.", "string", false, "https://api.telegram.org");
                    Add("ApiKey", "Telegram bot token.", "string", true);
                    break;

                case DataSourceType.WhatsAppBusiness:
                    Add("Url", "WhatsApp Business API URL.", "string", false, "https://graph.facebook.com");
                    Add("ApiKey", "WhatsApp Business API key.", "string", true);
                    break;

                case DataSourceType.Twist:
                case DataSourceType.Chanty:
                case DataSourceType.RocketChat:
                case DataSourceType.Flock:
                    Add("ApiKey", "API key.", "string", true);
                    break;

                // Cloud Storage
                case DataSourceType.GoogleDrive:
                    Add("ClientId", "OAuth client ID.", "string", true);
                    Add("ClientSecret", "OAuth client secret.", "string", true);
                    break;

                case DataSourceType.Dropbox:
                    Add("Url", "Dropbox API URL.", "string", false, "https://api.dropboxapi.com");
                    Add("ApiKey", "Dropbox access token.", "string", true);
                    break;

                case DataSourceType.OneDrive:
                    Add("ClientId", "Azure app client ID.", "string", true);
                    Add("ClientSecret", "Azure app client secret.", "string", true);
                    Add("TenantId", "Azure tenant ID.", "string");
                    Add("DriveId", "OneDrive drive ID.", "string");
                    Add("SiteId", "SharePoint site ID (for Business).", "string");
                    Add("Url", "Microsoft Graph API URL.", "string", false, "https://graph.microsoft.com"); 
                    break;

                case DataSourceType.Box:
                    Add("ClientId", "Box client ID.", "string", true);
                    Add("ClientSecret", "Box client secret.", "string", true);
                    Add("Url" , "Box API URL.", "string", false, "https://api.box.com");    
                    break;

                case DataSourceType.AmazonS3:
                    Add("ApiKey", "AWS access key ID.", "string", true);
                    Add("KeyToken", "AWS secret access key.", "string", true);
                    Add("Region", "AWS region.", "string", true);
                    Add("BucketName", "S3 bucket name.", "string", true);
                    Add("Url", "S3 service URL.", "string");
                    break;

                case DataSourceType.pCloud:
                case DataSourceType.iCloud:
                case DataSourceType.Egnyte:
                case DataSourceType.MediaFire:
                case DataSourceType.CitrixShareFile:
                    Add("ApiKey", "API key.", "string", true);
                    Add("Url", "Service URL.", "string");
                    break;

                // Payment Gateways
                case DataSourceType.Stripe:
                    Add("Url", "Stripe API URL.", "string", false, "https://api.stripe.com");
                    Add("ApiKey", "Stripe secret key.", "string", true);
                    break;

                case DataSourceType.PayPal:
                    Add("ClientId", "PayPal client ID.", "string", true);
                    Add("ClientSecret", "PayPal client secret.", "string", true);
                    Add("Url", "PayPal API URL.", "string", false, "https://api.paypal.com");
                    break;

                case DataSourceType.Square:
                    Add("Url", "Square API URL.", "string", false, "https://connect.squareup.com");
                    Add("ApiKey", "Square access token.", "string", true);
                    break;

                case DataSourceType.AuthorizeNet:
                    Add("ApiKey", "Authorize.Net API login ID.", "string", true);
                    Add("KeyToken", "Authorize.Net transaction key.", "string", true);
                    break;

                case DataSourceType.Braintree:
                    Add("ApiKey", "Braintree merchant ID.", "string", true);
                    Add("ClientSecret", "Braintree public key.", "string", true);
                    Add("KeyToken", "Braintree private key.", "string", true);
                    break;

                case DataSourceType.Worldpay:
                case DataSourceType.Adyen:
                case DataSourceType.TwoCheckout:
                case DataSourceType.Razorpay:
                case DataSourceType.Payoneer:
                case DataSourceType.Wise:
                    Add("ApiKey", "API key.", "string", true);
                    Add("KeyToken", "API secret or token.", "string");
                    Add("Url", "Service URL.", "string");
                    break;

                case DataSourceType.BitPay:
                case DataSourceType.Coinbase:
                case DataSourceType.Venmo:
                    Add("ApiKey", "API key.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                // Social Media Platforms
                case DataSourceType.Facebook:
                    Add("ApiKey", "Facebook app access token.", "string", true);
                    Add("ClientId", "Facebook app ID.", "string");
                    Add("ClientSecret", "Facebook app secret.", "string");
                      Add("Url", "Service URL.", "string");
                    break;

                case DataSourceType.Twitter:
                    Add("Url", "Twitter API URL.", "string", false, "https://api.twitter.com");
                    Add("ApiKey", "Twitter API key.", "string", true);
                    Add("ClientSecret", "Twitter API secret.", "string", true);
                    Add("UserID", "Twitter access token.", "string");
                    Add("Password", "Twitter access token secret.", "string");
                    break;

                case DataSourceType.Instagram:
                    Add("Url", "Instagram API URL.", "string", false, "https://graph.instagram.com");
                    Add("ApiKey", "Instagram access token.", "string", true);
                    break;

                case DataSourceType.LinkedIn:
                    Add("ClientId", "LinkedIn client ID.", "string", true);
                    Add("ClientSecret", "LinkedIn client secret.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                case DataSourceType.Pinterest:
                    Add("Url", "Pinterest API URL.", "string", false, "https://api.pinterest.com");
                    Add("ApiKey", "Pinterest access token.", "string", true);
                    break;

                case DataSourceType.YouTube:
                    Add("Url", "YouTube API URL.", "string", false, "https://www.googleapis.com/youtube");
                    Add("ApiKey", "YouTube API key.", "string", true);
                    break;

                case DataSourceType.TikTok:
                    Add("Url", "TikTok API URL.", "string", false, "https://open-api.tiktok.com");
                    Add("ApiKey", "TikTok access token.", "string", true);
                    break;

                case DataSourceType.Snapchat:
                    Add("Url", "Snapchat API URL.", "string", false, "https://adsapi.snapchat.com");
                    Add("ApiKey", "Snapchat access token.", "string", true);
                    break;

                case DataSourceType.Reddit:
                    Add("ClientId", "Reddit client ID.", "string", true);
                    Add("ClientSecret", "Reddit client secret.", "string", true);
                    Add("UserID", "Reddit username.", "string");
                    Add("Password", "Reddit password.", "string");
                      Add("Url", "Service URL.", "string");
                    break;

                case DataSourceType.Buffer:
                case DataSourceType.Hootsuite:
                    Add("Url", "API URL.", "string", false, "https://api.bufferapp.com");
                    Add("ApiKey", "API key.", "string", true);
                    break;

                case DataSourceType.TikTokAds:
                    Add("Url", "TikTok Ads API URL.", "string", false, "https://ads.tiktok.com");
                    Add("ApiKey", "TikTok Ads access token.", "string", true);
                    break;

                // Workflow Automation
                case DataSourceType.Zapier:
                    Add("Url", "Zapier API URL.", "string", false, "https://api.zapier.com");
                    Add("ApiKey", "Zapier API key.", "string", true);
                    break;

                case DataSourceType.Make:
                    Add("Url", "Make API URL.", "string", false, "https://eu1.make.com/api");
                    Add("ApiKey", "Make API key.", "string", true);
                    break;

                case DataSourceType.Airtable:
                    Add("Url", "Airtable API URL.", "string", false, "https://api.airtable.com");
                    Add("ApiKey", "Airtable API key.", "string", true);
                    break;

                case DataSourceType.MicrosoftPowerAutomate:
                    Add("ClientId", "Azure app client ID.", "string", true);
                    Add("ClientSecret", "Azure app client secret.", "string", true);
                    Add("TenantId", "Azure tenant ID.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                case DataSourceType.Calendly:
                    Add("Url", "Calendly API URL.", "string", false, "https://api.calendly.com");
                    Add("ApiKey", "Calendly personal access token.", "string", true);
                    break;

                case DataSourceType.Doodle:
                case DataSourceType.Eventbrite:
                    Add("Url", "API URL.", "string", false, "https://www.eventbriteapi.com");
                    Add("ApiKey", "API key.", "string", true);
                    break;

                // Developer Tools
                case DataSourceType.GitHub:
                    Add("Url", "GitHub API URL.", "string", false, "https://api.github.com");
                    Add("ApiKey", "GitHub personal access token.", "string", true);
                    break;

                case DataSourceType.GitLab:
                    Add("Url", "GitLab instance URL.", "string");
                    Add("ApiKey", "GitLab personal access token.", "string", true);
                    break;

                case DataSourceType.Bitbucket:
                    Add("UserID", "Bitbucket username.", "string", true);
                    Add("Password", "Bitbucket app password.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                case DataSourceType.Jenkins:
                    Add("Url", "Jenkins instance URL.", "string", true);
                    Add("UserID", "Username.", "string");
                    Add("Password", "API token.", "string");

                    break;

                case DataSourceType.CircleCI:
                    Add("Url", "CircleCI API URL.", "string", false, "https://circleci.com/api");
                    Add("ApiKey", "CircleCI personal API token.", "string", true);
                    break;

                case DataSourceType.Postman:
                    Add("Url", "Postman API URL.", "string", false, "https://api.getpostman.com");
                    Add("ApiKey", "Postman API key.", "string", true);
                    break;

                case DataSourceType.SwaggerHub:
                    Add("Url", "SwaggerHub API URL.", "string", false, "https://api.swaggerhub.com");
                    Add("ApiKey", "SwaggerHub API key.", "string", true);
                    break;

                case DataSourceType.AzureDevOps:
                    Add("Url", "Azure DevOps organization URL.", "string", true);
                    Add("UserID", "Username.", "string");
                    Add("Password", "Personal access token.", "string", true);
                    break;

                // Customer Support
                case DataSourceType.Zendesk:
                    Add("Url", "Zendesk subdomain URL.", "string", true);
                    Add("UserID", "Username.", "string", true);
                    Add("Password", "API token.", "string", true);
                    break;

                case DataSourceType.Freshdesk:
                    Add("Url", "Freshdesk domain URL.", "string", true);
                    Add("ApiKey", "Freshdesk API key.", "string", true);
                    break;

                case DataSourceType.HelpScout:
                    Add("Url", "Help Scout API URL.", "string", false, "https://api.helpscout.net");
                    Add("ApiKey", "Help Scout API key.", "string", true);
                    break;

                case DataSourceType.ZohoDesk:
                    Add("Url", "Zoho Desk API URL.", "string", true);
                    Add("ApiKey", "Zoho Desk API key.", "string", true);
                    break;

                case DataSourceType.Kayako:
                case DataSourceType.LiveAgent:
                case DataSourceType.Front:
                    Add("Url", "API URL.", "string", false, "https://api.kayako.com");
                    Add("ApiKey", "API key.", "string", true);
                    break;

                // Analytics and Reporting
                case DataSourceType.GoogleAnalytics:
                    Add("Url", "Google Analytics API URL.", "string", false, "https://www.googleapis.com/analytics");
                    Add("ApiKey", "Google Analytics API key.", "string", true);
                    break;

                case DataSourceType.Mixpanel:
                    Add("Url", "Mixpanel API URL.", "string", false, "https://mixpanel.com/api");
                    Add("ApiKey", "Mixpanel API key.", "string", true);
                    break;

                case DataSourceType.Hotjar:
                    Add("Url", "Hotjar API URL.", "string", false, "https://api.hotjar.io");
                    Add("ApiKey", "Hotjar API key.", "string", true);
                    break;

                case DataSourceType.Amplitude:
                    Add("Url", "Amplitude API URL.", "string", false, "https://api.amplitude.com");
                    Add("ApiKey", "Amplitude API key.", "string", true);
                    break;

                case DataSourceType.Heap:
                    Add("Url", "Heap API URL.", "string", false, "https://heapanalytics.com/api");
                    Add("ApiKey", "Heap API key.", "string", true);
                    break;

                case DataSourceType.Databox:
                case DataSourceType.Geckoboard:
                case DataSourceType.Cyfe:
                    Add("ApiKey", "API key.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                // IoT Platforms
                case DataSourceType.Twilio:
                    Add("AccountSid", "Twilio account SID.", "string", true);
                    Add("AuthToken", "Twilio auth token.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                case DataSourceType.Plaid:
                    Add("ClientId", "Plaid client ID.", "string", true);
                    Add("ClientSecret", "Plaid client secret.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                case DataSourceType.Particle:
                    Add("Url", "Particle API URL.", "string", false, "https://api.particle.io");
                    Add("ApiKey", "Particle access token.", "string", true);
                    break;

                case DataSourceType.ArduinoCloud:
                    Add("Url", "Arduino IoT Cloud API URL.", "string", false, "https://api2.arduino.cc");
                    Add("ClientId", "Arduino IoT Cloud client ID.", "string", true);
                    Add("ClientSecret", "Arduino IoT Cloud client secret.", "string", true);
                    break;

                case DataSourceType.Nest:
                    Add("Url", "Nest API URL.", "string", false, "https://developer-api.nest.com");
                    Add("ApiKey", "Nest API key.", "string", true);
                    break;

                case DataSourceType.SmartThings:
                    Add("Url", "SmartThings API URL.", "string", false, "https://api.smartthings.com");
                    Add("ApiKey", "SmartThings API key.", "string", true);
                    break;

                case DataSourceType.Tuya:
                    Add("ClientId", "Tuya client ID.", "string", true);
                    Add("ClientSecret", "Tuya client secret.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                // Blockchain
                case DataSourceType.Ethereum:
                    Add("Url", "Ethereum node URL.", "string", true);
                    Add("ApiKey", "Infura/Alchemy API key.", "string");
                    break;

                case DataSourceType.Hyperledger:
                    Add("Url", "Hyperledger network URL.", "string", true);
                    Add("ApiKey", "API key.", "string");
                    break;

                case DataSourceType.BitcoinCore:
                    Add("Host", "Bitcoin node hostname.", "string", true);
                    Add("Port", "Bitcoin node port.", "int", false, "8332");
                    Add("UserID", "RPC username.", "string");
                    Add("Password", "RPC password.", "string");
                    break;

                // File Transfer Protocols
                // FTP and SFTP not supported in DataSourceType enum

                // Email Protocols
                // Email, IMAP, POP3, SMTP not supported in DataSourceType enum

                // Additional Social Media
                case DataSourceType.Threads:
                case DataSourceType.Mastodon:
                case DataSourceType.Bluesky:
                    Add("ApiKey", "API key.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                // Additional Workflow Tools
                case DataSourceType.Integromat:
                case DataSourceType.TrayIO:
                    Add("ApiKey", "API key.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                // Additional DevOps Tools
                case DataSourceType.SonarQube:
                    Add("Url", "SonarQube instance URL.", "string", true);
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    break;

                case DataSourceType.Intercom:
                    Add("Url", "Intercom API URL.", "string", false, "https://api.intercom.io");
                    Add("ApiKey", "Intercom access token.", "string", true);
                    break;

                case DataSourceType.Drift:
                    Add("Url", "Drift API URL.", "string", false, "https://api.drift.com");
                    Add("ApiKey", "Drift API key.", "string", true);
                    break;

                // BI and Analytics Tools
                case DataSourceType.Tableau:
                    Add("Url", "Tableau server URL.", "string", true);
                    Add("UserID", "Username.", "string", true);
                    Add("Password", "Password.", "string", true);
                    break;

                case DataSourceType.PowerBI:
                    Add("ClientId", "Azure app client ID.", "string", true);
                    Add("ClientSecret", "Azure app client secret.", "string", true);
                    Add("TenantId", "Azure tenant ID.", "string", true);
                      Add("Url", "Service URL.", "string");
                    break;

                // Accounting Software
                case DataSourceType.QuickBooks:
                    Add("ClientId", "QuickBooks app client ID.", "string", true);
                    Add("ClientSecret", "QuickBooks app client secret.", "string", true);
                    Add("ApiKey", "QuickBooks access token.", "string");
                      Add("Url", "Service URL.", "string");
                    break;

                case DataSourceType.Xero:
                  Add("Url", "Service URL.", "string");
                    Add("ClientId", "Xero app client ID.", "string", true);
                    Add("ClientSecret", "Xero app client secret.", "string", true);
                    break;

                case DataSourceType.FreshBooks:
                    Add("Url", "FreshBooks API URL.", "string", false, "https://api.freshbooks.com");
                    Add("ApiKey", "FreshBooks API key.", "string", true);
                    break;

                case DataSourceType.WaveApps:
                    Add("Url", "Wave API URL.", "string", false, "https://api.waveapps.com");
                    Add("ApiKey", "Wave API key.", "string", true);
                    break;

                case DataSourceType.SageBusinessCloud:
                    Add("Url", "Sage Business Cloud API URL.", "string", false, "https://api.sage.com");
                    Add("ClientId", "Sage app client ID.", "string", true);
                    Add("ClientSecret", "Sage app client secret.", "string", true);
                    break;

                case DataSourceType.MYOB:
                    Add("Url", "MYOB API URL.", "string", false, "https://api.myob.com");
                    Add("ApiKey", "MYOB API key.", "string", true);
                    break;

                case DataSourceType.BenchAccounting:
                    Add("Url", "Bench API URL.", "string", false, "https://bench.co/api");
                    Add("ApiKey", "Bench API key.", "string", true);
                    break;

                // Default case for any unhandled types
                default:
                    Add("Host", "Server hostname.", "string");
                    Add("Port", "Server port.", "int");
                    Add("Database", "Database name.", "string");
                    Add("UserID", "Username.", "string");
                    Add("Password", "Password.", "string");
                    Add("ApiKey", "API key.", "string");
                    Add("Url", "Service URL.", "string");
                    Add("FilePath", "File path.", "string");
                    break;
            }

            return defs;
        }
    }

    public class parameterinfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string DataType { get; set; }
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; }
    }
}
