using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RdbmsHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.MongoDBHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RedisHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.CassandraHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RestApiHelpers;

namespace TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core
{
    /// <summary>
    /// Factory class for creating datasource-specific helper implementations.
    /// Returns the appropriate IDataSourceHelper based on DataSourceType.
    /// </summary>
    public class DataSourceHelperFactory
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly Dictionary<DataSourceType, Func<IDMEEditor, IDataSourceHelper>> _helperFactories;

        public DataSourceHelperFactory(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _helperFactories = InitializeFactories();
        }

        private Dictionary<DataSourceType, Func<IDMEEditor, IDataSourceHelper>> InitializeFactories()
        {
            return new Dictionary<DataSourceType, Func<IDMEEditor, IDataSourceHelper>>
            {
                // RDBMS databases - all use RdbmsHelper
                { DataSourceType.SqlServer, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.SqlServer; return h; } },
                { DataSourceType.Mysql, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Mysql; return h; } },
                { DataSourceType.Postgre, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Postgre; return h; } },
                { DataSourceType.Oracle, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Oracle; return h; } },
                { DataSourceType.SqlLite, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.SqlLite; return h; } },
                { DataSourceType.SqlCompact, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.SqlCompact; return h; } },
                { DataSourceType.DB2, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.DB2; return h; } },
                { DataSourceType.FireBird, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.FireBird; return h; } },
                { DataSourceType.Hana, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Hana; return h; } },
                { DataSourceType.TerraData, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.TerraData; return h; } },
                { DataSourceType.Vertica, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Vertica; return h; } },
                { DataSourceType.AzureSQL, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AzureSQL; return h; } },
                { DataSourceType.AWSRDS, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSRDS; return h; } },
                { DataSourceType.SnowFlake, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.SnowFlake; return h; } },
                { DataSourceType.Cockroach, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Cockroach; return h; } },
                { DataSourceType.Spanner, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Spanner; return h; } },
                { DataSourceType.DuckDB, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.DuckDB; return h; } },
                { DataSourceType.MariaDB, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.MariaDB; return h; } },

                // NoSQL databases
                { DataSourceType.MongoDB, (dme) => { var h = new MongoDBHelper(dme); h.SupportedType = DataSourceType.MongoDB; return h; } },
                { DataSourceType.CouchDB, (dme) => { var h = new MongoDBHelper(dme); h.SupportedType = DataSourceType.CouchDB; return h; } },
                { DataSourceType.RavenDB, (dme) => { var h = new MongoDBHelper(dme); h.SupportedType = DataSourceType.RavenDB; return h; } },
                { DataSourceType.Couchbase, (dme) => { var h = new MongoDBHelper(dme); h.SupportedType = DataSourceType.Couchbase; return h; } },
                { DataSourceType.ArangoDB, (dme) => { var h = new MongoDBHelper(dme); h.SupportedType = DataSourceType.ArangoDB; return h; } },
                { DataSourceType.OrientDB, (dme) => { var h = new MongoDBHelper(dme); h.SupportedType = DataSourceType.OrientDB; return h; } },
                { DataSourceType.LiteDB, (dme) => { var h = new MongoDBHelper(dme); h.SupportedType = DataSourceType.LiteDB; return h; } },
                { DataSourceType.Firebase, (dme) => { var h = new MongoDBHelper(dme); h.SupportedType = DataSourceType.Firebase; return h; } },
                { DataSourceType.DynamoDB, (dme) => { var h = new MongoDBHelper(dme); h.SupportedType = DataSourceType.DynamoDB; return h; } },
                { DataSourceType.VistaDB, (dme) => { var h = new MongoDBHelper(dme); h.SupportedType = DataSourceType.VistaDB; return h; } },

                // Key-Value stores
                { DataSourceType.Redis, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.Redis; return h; } },
                { DataSourceType.Memcached, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.Memcached; return h; } },
                { DataSourceType.GridGain, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.GridGain; return h; } },
                { DataSourceType.Hazelcast, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.Hazelcast; return h; } },
                { DataSourceType.ApacheIgnite, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.ApacheIgnite; return h; } },
                { DataSourceType.ChronicleMap, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.ChronicleMap; return h; } },

                // Column-family databases
                { DataSourceType.Cassandra, (dme) => { var h = new CassandraHelper(dme); h.SupportedType = DataSourceType.Cassandra; return h; } },
                { DataSourceType.ClickHouse, (dme) => { var h = new CassandraHelper(dme); h.SupportedType = DataSourceType.ClickHouse; return h; } },

                // Graph databases
                { DataSourceType.Neo4j, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Neo4j; return h; } },
                { DataSourceType.TigerGraph, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.TigerGraph; return h; } },
                { DataSourceType.JanusGraph, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.JanusGraph; return h; } },

                // Search engines
                { DataSourceType.ElasticSearch, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ElasticSearch; return h; } },
                { DataSourceType.Solr, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Solr; return h; } },

                // Time series databases
                { DataSourceType.InfluxDB, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.InfluxDB; return h; } },
                { DataSourceType.TimeScale, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.TimeScale; return h; } },

                // Vector databases
                { DataSourceType.ChromaDB, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ChromaDB; return h; } },
                { DataSourceType.PineCone, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.PineCone; return h; } },
                { DataSourceType.Qdrant, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Qdrant; return h; } },
                { DataSourceType.Weaviate, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Weaviate; return h; } },
                { DataSourceType.Milvus, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Milvus; return h; } },
                { DataSourceType.Zilliz, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Zilliz; return h; } },
                { DataSourceType.Vespa, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Vespa; return h; } },
                { DataSourceType.ShapVector, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ShapVector; return h; } },
                { DataSourceType.RedisVector, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.RedisVector; return h; } },

                // Big data / columnar
                { DataSourceType.Hadoop, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Hadoop; return h; } },
                { DataSourceType.Kudu, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Kudu; return h; } },
                { DataSourceType.Druid, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Druid; return h; } },
                { DataSourceType.Pinot, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Pinot; return h; } },
                { DataSourceType.Parquet, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Parquet; return h; } },
                { DataSourceType.Avro, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Avro; return h; } },
                { DataSourceType.ORC, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ORC; return h; } },
                { DataSourceType.Feather, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Feather; return h; } },

                // In-memory databases
                { DataSourceType.RealIM, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.RealIM; return h; } },
                { DataSourceType.Petastorm, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.Petastorm; return h; } },
                { DataSourceType.RocketSet, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.RocketSet; return h; } },
                { DataSourceType.H2Database, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.H2Database; return h; } },
                { DataSourceType.InMemoryCache, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.InMemoryCache; return h; } },
                { DataSourceType.CachedMemory, (dme) => { var h = new RedisHelper(dme); h.SupportedType = DataSourceType.CachedMemory; return h; } },

                // Cloud data warehouses
                { DataSourceType.AWSRedshift, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSRedshift; return h; } },
                { DataSourceType.GoogleBigQuery, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.GoogleBigQuery; return h; } },
                { DataSourceType.AWSGlue, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSGlue; return h; } },
                { DataSourceType.AWSAthena, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSAthena; return h; } },
                { DataSourceType.AzureCloud, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AzureCloud; return h; } },
                { DataSourceType.DataBricks, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.DataBricks; return h; } },
                { DataSourceType.Firebolt, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Firebolt; return h; } },
                { DataSourceType.Hologres, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Hologres; return h; } },
                { DataSourceType.Supabase, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Supabase; return h; } },

                // Streaming and messaging
                { DataSourceType.Kafka, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Kafka; return h; } },
                { DataSourceType.RabbitMQ, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.RabbitMQ; return h; } },
                { DataSourceType.ActiveMQ, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ActiveMQ; return h; } },
                { DataSourceType.Pulsar, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Pulsar; return h; } },
                { DataSourceType.MassTransit, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.MassTransit; return h; } },
                { DataSourceType.Nats, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Nats; return h; } },
                { DataSourceType.ZeroMQ, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ZeroMQ; return h; } },
                { DataSourceType.AWSKinesis, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSKinesis; return h; } },
                { DataSourceType.AWSSQS, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSSQS; return h; } },
                { DataSourceType.AWSSNS, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSSNS; return h; } },
                { DataSourceType.AzureServiceBus, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AzureServiceBus; return h; } },

                // Stream processing
                { DataSourceType.ApacheFlink, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ApacheFlink; return h; } },
                { DataSourceType.ApacheStorm, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ApacheStorm; return h; } },
                { DataSourceType.ApacheSparkStreaming, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ApacheSparkStreaming; return h; } },

                // Machine learning data formats
                { DataSourceType.TFRecord, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.TFRecord; return h; } },
                { DataSourceType.ONNX, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ONNX; return h; } },
                { DataSourceType.PyTorchData, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.PyTorchData; return h; } },
                { DataSourceType.ScikitLearnData, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ScikitLearnData; return h; } },

                // File formats
                { DataSourceType.FlatFile, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.FlatFile; return h; } },
                { DataSourceType.CSV, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.CSV; return h; } },
                { DataSourceType.TSV, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.TSV; return h; } },
                { DataSourceType.Text, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Text; return h; } },
                { DataSourceType.YAML, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.YAML; return h; } },
                { DataSourceType.Json, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Json; return h; } },
                { DataSourceType.Markdown, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Markdown; return h; } },
                { DataSourceType.Log, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Log; return h; } },
                { DataSourceType.INI, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.INI; return h; } },
                { DataSourceType.XML, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.XML; return h; } },
                { DataSourceType.Xls, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Xls; return h; } },
                { DataSourceType.Doc, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Doc; return h; } },
                { DataSourceType.Docx, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Docx; return h; } },
                { DataSourceType.PPT, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.PPT; return h; } },
                { DataSourceType.PPTX, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.PPTX; return h; } },
                { DataSourceType.PDF, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.PDF; return h; } },
                { DataSourceType.RecordIO, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.RecordIO; return h; } },

                // Specialized formats
                { DataSourceType.Hdf5, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Hdf5; return h; } },
                { DataSourceType.LibSVM, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.LibSVM; return h; } },
                { DataSourceType.GraphML, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.GraphML; return h; } },
                { DataSourceType.DICOM, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.DICOM; return h; } },
                { DataSourceType.LAS, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.LAS; return h; } },

                // Workflow systems
                { DataSourceType.AWSSWF, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSSWF; return h; } },
                { DataSourceType.AWSStepFunctions, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSStepFunctions; return h; } },

                // IoT
                { DataSourceType.AWSIoT, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSIoT; return h; } },
                { DataSourceType.AWSIoTCore, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSIoTCore; return h; } },
                { DataSourceType.AWSIoTAnalytics, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AWSIoTAnalytics; return h; } },
                { DataSourceType.AzureIoTHub, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.AzureIoTHub; return h; } },

                // Industrial systems
                { DataSourceType.OPC, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.OPC; return h; } },

                // API and web services
                { DataSourceType.RestApi, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.RestApi; return h; } },
                { DataSourceType.WebApi, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.WebApi; return h; } },
                { DataSourceType.GraphQL, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.GraphQL; return h; } },
                { DataSourceType.OData, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.OData; return h; } },

                // Generic connectors
                { DataSourceType.ODBC, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ODBC; return h; } },
                { DataSourceType.OLEDB, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.OLEDB; return h; } },
                { DataSourceType.ADO, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.ADO; return h; } },

                // Cloud services and spreadsheets
                { DataSourceType.GoogleSheets, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.GoogleSheets; return h; } },
                { DataSourceType.MiModel, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.MiModel; return h; } },

                // Query engines
                { DataSourceType.Presto, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Presto; return h; } },
                { DataSourceType.Trino, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.Trino; return h; } },

                // CRM Systems
                { DataSourceType.Salesforce, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Salesforce; return h; } },
                { DataSourceType.HubSpot, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.HubSpot; return h; } },
                { DataSourceType.Zoho, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Zoho; return h; } },
                { DataSourceType.Pipedrive, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Pipedrive; return h; } },
                { DataSourceType.MicrosoftDynamics365, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.MicrosoftDynamics365; return h; } },
                { DataSourceType.Freshsales, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Freshsales; return h; } },
                { DataSourceType.SugarCRM, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SugarCRM; return h; } },
                { DataSourceType.Insightly, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Insightly; return h; } },
                { DataSourceType.Copper, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Copper; return h; } },
                { DataSourceType.Nutshell, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Nutshell; return h; } },
                { DataSourceType.SAPCRM, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SAPCRM; return h; } },
                { DataSourceType.OracleCRM, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.OracleCRM; return h; } },

                // Marketing Automation
                { DataSourceType.Mailchimp, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Mailchimp; return h; } },
                { DataSourceType.Marketo, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Marketo; return h; } },
                { DataSourceType.GoogleAds, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.GoogleAds; return h; } },
                { DataSourceType.ActiveCampaign, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.ActiveCampaign; return h; } },
                { DataSourceType.ConstantContact, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.ConstantContact; return h; } },
                { DataSourceType.Klaviyo, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Klaviyo; return h; } },
                { DataSourceType.Sendinblue, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Sendinblue; return h; } },
                { DataSourceType.CampaignMonitor, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.CampaignMonitor; return h; } },
                { DataSourceType.ConvertKit, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.ConvertKit; return h; } },
                { DataSourceType.Drip, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Drip; return h; } },
                { DataSourceType.MailerLite, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.MailerLite; return h; } },
                { DataSourceType.HootsuiteMarketing, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.HootsuiteMarketing; return h; } },
                { DataSourceType.Mailgun, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Mailgun; return h; } },
                { DataSourceType.SendGrid, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SendGrid; return h; } },
                { DataSourceType.Criteo, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Criteo; return h; } },

                // E-commerce
                { DataSourceType.Shopify, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Shopify; return h; } },
                { DataSourceType.WooCommerce, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.WooCommerce; return h; } },
                { DataSourceType.Magento, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Magento; return h; } },
                { DataSourceType.BigCommerce, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.BigCommerce; return h; } },
                { DataSourceType.Squarespace, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Squarespace; return h; } },
                { DataSourceType.Wix, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Wix; return h; } },
                { DataSourceType.Etsy, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Etsy; return h; } },
                { DataSourceType.OpenCart, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.OpenCart; return h; } },
                { DataSourceType.Ecwid, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Ecwid; return h; } },
                { DataSourceType.Volusion, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Volusion; return h; } },
                { DataSourceType.PrestaShop, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.PrestaShop; return h; } },
                { DataSourceType.BigCartel, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.BigCartel; return h; } },

                // Project Management
                { DataSourceType.Jira, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Jira; return h; } },
                { DataSourceType.Trello, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Trello; return h; } },
                { DataSourceType.Asana, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Asana; return h; } },
                { DataSourceType.Monday, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Monday; return h; } },
                { DataSourceType.ClickUp, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.ClickUp; return h; } },
                { DataSourceType.Basecamp, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Basecamp; return h; } },
                { DataSourceType.Notion, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Notion; return h; } },
                { DataSourceType.Wrike, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Wrike; return h; } },
                { DataSourceType.Smartsheet, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Smartsheet; return h; } },
                { DataSourceType.Teamwork, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Teamwork; return h; } },
                { DataSourceType.Podio, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Podio; return h; } },
                { DataSourceType.AnyDo, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.AnyDo; return h; } },
                { DataSourceType.AzureBoards, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.AzureBoards; return h; } },
                { DataSourceType.SmartsheetPM, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SmartsheetPM; return h; } },

                // Communication
                { DataSourceType.Slack, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Slack; return h; } },
                { DataSourceType.MicrosoftTeams, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.MicrosoftTeams; return h; } },
                { DataSourceType.Zoom, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Zoom; return h; } },
                { DataSourceType.GoogleChat, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.GoogleChat; return h; } },
                { DataSourceType.Discord, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Discord; return h; } },
                { DataSourceType.Telegram, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Telegram; return h; } },
                { DataSourceType.WhatsAppBusiness, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.WhatsAppBusiness; return h; } },
                { DataSourceType.ClickSend, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.ClickSend; return h; } },
                { DataSourceType.Kudosity, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Kudosity; return h; } },
                { DataSourceType.Twist, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Twist; return h; } },
                { DataSourceType.Chanty, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Chanty; return h; } },
                { DataSourceType.RocketChat, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.RocketChat; return h; } },
                { DataSourceType.Flock, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Flock; return h; } },
                { DataSourceType.Mattermost, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Mattermost; return h; } },
                { DataSourceType.RocketChatComm, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.RocketChatComm; return h; } },

                // Cloud Storage
                { DataSourceType.GoogleDrive, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.GoogleDrive; return h; } },
                { DataSourceType.Dropbox, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Dropbox; return h; } },
                { DataSourceType.OneDrive, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.OneDrive; return h; } },
                { DataSourceType.Box, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Box; return h; } },
                { DataSourceType.AmazonS3, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.AmazonS3; return h; } },
                { DataSourceType.pCloud, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.pCloud; return h; } },
                { DataSourceType.iCloud, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.iCloud; return h; } },
                { DataSourceType.Egnyte, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Egnyte; return h; } },
                { DataSourceType.MediaFire, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.MediaFire; return h; } },
                { DataSourceType.CitrixShareFile, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.CitrixShareFile; return h; } },
                { DataSourceType.GoogleCloudStorage, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.GoogleCloudStorage; return h; } },
                { DataSourceType.Mega, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Mega; return h; } },
                { DataSourceType.Backblaze, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Backblaze; return h; } },

                // Payment Gateways
                { DataSourceType.Stripe, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Stripe; return h; } },
                { DataSourceType.PayPal, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.PayPal; return h; } },
                { DataSourceType.Square, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Square; return h; } },
                { DataSourceType.AuthorizeNet, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.AuthorizeNet; return h; } },
                { DataSourceType.Braintree, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Braintree; return h; } },
                { DataSourceType.Worldpay, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Worldpay; return h; } },
                { DataSourceType.Adyen, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Adyen; return h; } },
                { DataSourceType.TwoCheckout, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.TwoCheckout; return h; } },
                { DataSourceType.Razorpay, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Razorpay; return h; } },
                { DataSourceType.Payoneer, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Payoneer; return h; } },
                { DataSourceType.Wise, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Wise; return h; } },
                { DataSourceType.Coinbase, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Coinbase; return h; } },
                { DataSourceType.Venmo, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Venmo; return h; } },
                { DataSourceType.BitPay, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.BitPay; return h; } },

                // Social Media
                { DataSourceType.Facebook, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Facebook; return h; } },
                { DataSourceType.Twitter, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Twitter; return h; } },
                { DataSourceType.Instagram, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Instagram; return h; } },
                { DataSourceType.LinkedIn, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.LinkedIn; return h; } },
                { DataSourceType.Pinterest, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Pinterest; return h; } },
                { DataSourceType.YouTube, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.YouTube; return h; } },
                { DataSourceType.TikTok, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.TikTok; return h; } },
                { DataSourceType.Snapchat, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Snapchat; return h; } },
                { DataSourceType.Reddit, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Reddit; return h; } },
                { DataSourceType.Buffer, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Buffer; return h; } },
                { DataSourceType.Hootsuite, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Hootsuite; return h; } },
                { DataSourceType.TikTokAds, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.TikTokAds; return h; } },
                { DataSourceType.Loomly, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Loomly; return h; } },
                { DataSourceType.Threads, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Threads; return h; } },
                { DataSourceType.Mastodon, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Mastodon; return h; } },
                { DataSourceType.Bluesky, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Bluesky; return h; } },

                // Workflow Automation
                { DataSourceType.Zapier, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Zapier; return h; } },
                { DataSourceType.Make, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Make; return h; } },
                { DataSourceType.Airtable, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Airtable; return h; } },
                { DataSourceType.MicrosoftPowerAutomate, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.MicrosoftPowerAutomate; return h; } },
                { DataSourceType.Calendly, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Calendly; return h; } },
                { DataSourceType.Doodle, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Doodle; return h; } },
                { DataSourceType.Eventbrite, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Eventbrite; return h; } },
                { DataSourceType.Typeform, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Typeform; return h; } },
                { DataSourceType.Jotform, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Jotform; return h; } },
                { DataSourceType.WordPress, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.WordPress; return h; } },
                { DataSourceType.TLDV, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.TLDV; return h; } },
                { DataSourceType.Fathom, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Fathom; return h; } },
                { DataSourceType.Integromat, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Integromat; return h; } },
                { DataSourceType.TrayIO, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.TrayIO; return h; } },

                // Developer Tools
                { DataSourceType.GitHub, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.GitHub; return h; } },
                { DataSourceType.GitLab, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.GitLab; return h; } },
                { DataSourceType.Bitbucket, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Bitbucket; return h; } },
                { DataSourceType.Jenkins, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Jenkins; return h; } },
                { DataSourceType.CircleCI, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.CircleCI; return h; } },
                { DataSourceType.Postman, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Postman; return h; } },
                { DataSourceType.SwaggerHub, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SwaggerHub; return h; } },
                { DataSourceType.AzureDevOps, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.AzureDevOps; return h; } },
                { DataSourceType.SonarQube, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SonarQube; return h; } },
                { DataSourceType.Intercom, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Intercom; return h; } },
                { DataSourceType.Drift, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Drift; return h; } },

                // Customer Support
                { DataSourceType.Zendesk, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Zendesk; return h; } },
                { DataSourceType.Freshdesk, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Freshdesk; return h; } },
                { DataSourceType.HelpScout, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.HelpScout; return h; } },
                { DataSourceType.ZohoDesk, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.ZohoDesk; return h; } },
                { DataSourceType.Kayako, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Kayako; return h; } },
                { DataSourceType.LiveAgent, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.LiveAgent; return h; } },
                { DataSourceType.Front, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Front; return h; } },

                // Analytics and Reporting
                { DataSourceType.GoogleAnalytics, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.GoogleAnalytics; return h; } },
                { DataSourceType.Mixpanel, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Mixpanel; return h; } },
                { DataSourceType.Hotjar, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Hotjar; return h; } },
                { DataSourceType.Amplitude, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Amplitude; return h; } },
                { DataSourceType.Heap, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Heap; return h; } },
                { DataSourceType.Databox, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Databox; return h; } },
                { DataSourceType.Geckoboard, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Geckoboard; return h; } },
                { DataSourceType.Cyfe, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Cyfe; return h; } },
                { DataSourceType.Tableau, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Tableau; return h; } },
                { DataSourceType.PowerBI, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.PowerBI; return h; } },

                // IoT and Smart Devices
                { DataSourceType.Twilio, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Twilio; return h; } },
                { DataSourceType.Plaid, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Plaid; return h; } },
                { DataSourceType.DocuSign, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.DocuSign; return h; } },
                { DataSourceType.PhilipsHue, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.PhilipsHue; return h; } },
                { DataSourceType.Nest, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Nest; return h; } },
                { DataSourceType.SmartThings, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SmartThings; return h; } },
                { DataSourceType.Tuya, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Tuya; return h; } },
                { DataSourceType.Particle, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Particle; return h; } },
                { DataSourceType.ArduinoCloud, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.ArduinoCloud; return h; } },

                // Accounting
                { DataSourceType.FreshBooks, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.FreshBooks; return h; } },
                { DataSourceType.WaveApps, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.WaveApps; return h; } },
                { DataSourceType.SageBusinessCloud, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SageBusinessCloud; return h; } },
                { DataSourceType.MYOB, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.MYOB; return h; } },
                { DataSourceType.QuickBooks, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.QuickBooks; return h; } },
                { DataSourceType.Xero, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Xero; return h; } },
                { DataSourceType.QuickBooksOnline, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.QuickBooksOnline; return h; } },
                { DataSourceType.SageIntacct, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SageIntacct; return h; } },
                { DataSourceType.ZohoBooks, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.ZohoBooks; return h; } },
                { DataSourceType.BenchAccounting, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.BenchAccounting; return h; } },

                // Blockchain
                { DataSourceType.Ethereum, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Ethereum; return h; } },
                { DataSourceType.Hyperledger, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Hyperledger; return h; } },
                { DataSourceType.BitcoinCore, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.BitcoinCore; return h; } },

                // File Transfer
                { DataSourceType.FTP, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.FTP; return h; } },
                { DataSourceType.SFTP, (dme) => { var h = new RdbmsHelper(dme); h.SupportedType = DataSourceType.SFTP; return h; } },

                // Email Protocols
                { DataSourceType.Email, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Email; return h; } },
                { DataSourceType.IMAP, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.IMAP; return h; } },
                { DataSourceType.POP3, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.POP3; return h; } },
                { DataSourceType.SMTP, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SMTP; return h; } },
                { DataSourceType.Gmail, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Gmail; return h; } },
                { DataSourceType.Outlook, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Outlook; return h; } },
                { DataSourceType.Yahoo, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.Yahoo; return h; } },

                // Remote Procedure Calls
                { DataSourceType.XMLRPC, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.XMLRPC; return h; } },
                { DataSourceType.SOAP, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SOAP; return h; } },
                { DataSourceType.JSONRPC, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.JSONRPC; return h; } },
                { DataSourceType.GRPC, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.GRPC; return h; } },
                { DataSourceType.WebSocket, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.WebSocket; return h; } },
                { DataSourceType.SSE, (dme) => { var h = new RestApiHelper(dme); h.SupportedType = DataSourceType.SSE; return h; } }
            };
        }

        /// <summary>
        /// Creates and returns the appropriate IDataSourceHelper for the given datasource type.
        /// </summary>
        /// <param name="datasourceType">The datasource type</param>
        /// <returns>IDataSourceHelper implementation, or null if not supported</returns>
        public IDataSourceHelper CreateHelper(DataSourceType datasourceType)
        {
            if (_helperFactories.TryGetValue(datasourceType, out var factory))
            {
                return factory(_dmeEditor);
            }

            return null;
        }

        /// <summary>
        /// Checks if a helper is available for the specified datasource type.
        /// </summary>
        /// <param name="datasourceType">The datasource type</param>
        /// <returns>True if a helper exists, false otherwise</returns>
        public bool IsHelperAvailable(DataSourceType datasourceType)
        {
            return _helperFactories.ContainsKey(datasourceType);
        }

        /// <summary>
        /// Registers a custom helper factory for a datasource type.
        /// </summary>
        /// <param name="datasourceType">The datasource type</param>
        /// <param name="factory">Factory function to create the helper</param>
        public void RegisterHelper(DataSourceType datasourceType, Func<IDMEEditor, IDataSourceHelper> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _helperFactories[datasourceType] = factory;
        }

        /// <summary>
        /// Gets all supported datasource types that have helpers.
        /// </summary>
        /// <returns>Collection of supported datasource types</returns>
        public IEnumerable<DataSourceType> GetSupportedDataSources()
        {
            return _helperFactories.Keys;
        }
    }
}
