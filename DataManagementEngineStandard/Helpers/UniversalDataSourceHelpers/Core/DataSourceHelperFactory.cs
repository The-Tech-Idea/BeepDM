using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
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
    public static class DataSourceHelperFactory
    {
        private static readonly Dictionary<DataSourceType, Func<IDataSourceHelper>> _helperFactories;

        static DataSourceHelperFactory()
        {
            _helperFactories = new Dictionary<DataSourceType, Func<IDataSourceHelper>>
            {
                // RDBMS databases - all use RdbmsHelper
                { DataSourceType.SqlServer, () => new RdbmsHelper() },
                { DataSourceType.Mysql, () => new RdbmsHelper() },
                { DataSourceType.Postgre, () => new RdbmsHelper() },
                { DataSourceType.Oracle, () => new RdbmsHelper() },
                { DataSourceType.SqlLite, () => new RdbmsHelper() },
                { DataSourceType.SqlCompact, () => new RdbmsHelper() },
                { DataSourceType.DB2, () => new RdbmsHelper() },
                { DataSourceType.FireBird, () => new RdbmsHelper() },
                { DataSourceType.Hana, () => new RdbmsHelper() },
                { DataSourceType.TerraData, () => new RdbmsHelper() },
                { DataSourceType.Vertica, () => new RdbmsHelper() },
                { DataSourceType.AzureSQL, () => new RdbmsHelper() },
                { DataSourceType.AWSRDS, () => new RdbmsHelper() },
                { DataSourceType.SnowFlake, () => new RdbmsHelper() },
                { DataSourceType.Cockroach, () => new RdbmsHelper() },
                { DataSourceType.Spanner, () => new RdbmsHelper() },
                { DataSourceType.DuckDB, () => new RdbmsHelper() },
                { DataSourceType.MariaDB, () => new RdbmsHelper() },

                // NoSQL databases
                { DataSourceType.MongoDB, () => new MongoDBHelper() },
                { DataSourceType.CouchDB, () => new MongoDBHelper() }, // Use MongoDB helper as base for document DBs
                { DataSourceType.RavenDB, () => new MongoDBHelper() },
                { DataSourceType.Couchbase, () => new MongoDBHelper() },
                { DataSourceType.ArangoDB, () => new MongoDBHelper() },
                { DataSourceType.OrientDB, () => new MongoDBHelper() },
                { DataSourceType.LiteDB, () => new MongoDBHelper() },
                { DataSourceType.Firebase, () => new MongoDBHelper() },
                { DataSourceType.DynamoDB, () => new MongoDBHelper() },
                { DataSourceType.VistaDB, () => new MongoDBHelper() },

                // Key-Value stores
                { DataSourceType.Redis, () => new RedisHelper() },
                { DataSourceType.Memcached, () => new RedisHelper() },
                { DataSourceType.GridGain, () => new RedisHelper() },
                { DataSourceType.Hazelcast, () => new RedisHelper() },
                { DataSourceType.ApacheIgnite, () => new RedisHelper() },
                { DataSourceType.ChronicleMap, () => new RedisHelper() },

                // Column-family databases
                { DataSourceType.Cassandra, () => new CassandraHelper() },
                { DataSourceType.ClickHouse, () => new CassandraHelper() },

                // Graph databases
                { DataSourceType.Neo4j, () => new RdbmsHelper() }, // Use RDBMS helper as fallback
                { DataSourceType.TigerGraph, () => new RdbmsHelper() },
                { DataSourceType.JanusGraph, () => new RdbmsHelper() },

                // Search engines
                { DataSourceType.ElasticSearch, () => new RdbmsHelper() }, // Use RDBMS helper as fallback
                { DataSourceType.Solr, () => new RdbmsHelper() },

                // Time series databases
                { DataSourceType.InfluxDB, () => new RdbmsHelper() },
                { DataSourceType.TimeScale, () => new RdbmsHelper() },

                // Vector databases
                { DataSourceType.ChromaDB, () => new RdbmsHelper() },
                { DataSourceType.PineCone, () => new RdbmsHelper() },
                { DataSourceType.Qdrant, () => new RdbmsHelper() },
                { DataSourceType.Weaviate, () => new RdbmsHelper() },
                { DataSourceType.Milvus, () => new RdbmsHelper() },
                { DataSourceType.Zilliz, () => new RdbmsHelper() },
                { DataSourceType.Vespa, () => new RdbmsHelper() },
                { DataSourceType.ShapVector, () => new RdbmsHelper() },
                { DataSourceType.RedisVector, () => new RedisHelper() },

                // Big data / columnar
                { DataSourceType.Hadoop, () => new RdbmsHelper() },
                { DataSourceType.Kudu, () => new RdbmsHelper() },
                { DataSourceType.Druid, () => new RdbmsHelper() },
                { DataSourceType.Pinot, () => new RdbmsHelper() },
                { DataSourceType.Parquet, () => new RdbmsHelper() },
                { DataSourceType.Avro, () => new RdbmsHelper() },
                { DataSourceType.ORC, () => new RdbmsHelper() },
                { DataSourceType.Feather, () => new RdbmsHelper() },

                // In-memory databases
                { DataSourceType.RealIM, () => new RedisHelper() },
                { DataSourceType.Petastorm, () => new RedisHelper() },
                { DataSourceType.RocketSet, () => new RedisHelper() },
                { DataSourceType.H2Database, () => new RdbmsHelper() },
                { DataSourceType.InMemoryCache, () => new RedisHelper() },
                { DataSourceType.CachedMemory, () => new RedisHelper() },

                // Cloud data warehouses
                { DataSourceType.AWSRedshift, () => new RdbmsHelper() },
                { DataSourceType.GoogleBigQuery, () => new RdbmsHelper() },
                { DataSourceType.AWSGlue, () => new RdbmsHelper() },
                { DataSourceType.AWSAthena, () => new RdbmsHelper() },
                { DataSourceType.AzureCloud, () => new RdbmsHelper() },
                { DataSourceType.DataBricks, () => new RdbmsHelper() },
                { DataSourceType.Firebolt, () => new RdbmsHelper() },
                { DataSourceType.Hologres, () => new RdbmsHelper() },
                { DataSourceType.Supabase, () => new RdbmsHelper() },

                // Streaming and messaging
                { DataSourceType.Kafka, () => new RdbmsHelper() },
                { DataSourceType.RabbitMQ, () => new RdbmsHelper() },
                { DataSourceType.ActiveMQ, () => new RdbmsHelper() },
                { DataSourceType.Pulsar, () => new RdbmsHelper() },
                { DataSourceType.MassTransit, () => new RdbmsHelper() },
                { DataSourceType.Nats, () => new RdbmsHelper() },
                { DataSourceType.ZeroMQ, () => new RdbmsHelper() },
                { DataSourceType.AWSKinesis, () => new RdbmsHelper() },
                { DataSourceType.AWSSQS, () => new RdbmsHelper() },
                { DataSourceType.AWSSNS, () => new RdbmsHelper() },
                { DataSourceType.AzureServiceBus, () => new RdbmsHelper() },

                // Stream processing
                { DataSourceType.ApacheFlink, () => new RdbmsHelper() },
                { DataSourceType.ApacheStorm, () => new RdbmsHelper() },
                { DataSourceType.ApacheSparkStreaming, () => new RdbmsHelper() },

                // Machine learning data formats
                { DataSourceType.TFRecord, () => new RdbmsHelper() },
                { DataSourceType.ONNX, () => new RdbmsHelper() },
                { DataSourceType.PyTorchData, () => new RdbmsHelper() },
                { DataSourceType.ScikitLearnData, () => new RdbmsHelper() },

                // File formats
                { DataSourceType.FlatFile, () => new RdbmsHelper() },
                { DataSourceType.CSV, () => new RdbmsHelper() },
                { DataSourceType.TSV, () => new RdbmsHelper() },
                { DataSourceType.Text, () => new RdbmsHelper() },
                { DataSourceType.YAML, () => new RdbmsHelper() },
                { DataSourceType.Json, () => new RdbmsHelper() },
                { DataSourceType.Markdown, () => new RdbmsHelper() },
                { DataSourceType.Log, () => new RdbmsHelper() },
                { DataSourceType.INI, () => new RdbmsHelper() },
                { DataSourceType.XML, () => new RdbmsHelper() },
                { DataSourceType.Xls, () => new RdbmsHelper() },
                { DataSourceType.Doc, () => new RdbmsHelper() },
                { DataSourceType.Docx, () => new RdbmsHelper() },
                { DataSourceType.PPT, () => new RdbmsHelper() },
                { DataSourceType.PPTX, () => new RdbmsHelper() },
                { DataSourceType.PDF, () => new RdbmsHelper() },
                { DataSourceType.RecordIO, () => new RdbmsHelper() },

                // Specialized formats
                { DataSourceType.Hdf5, () => new RdbmsHelper() },
                { DataSourceType.LibSVM, () => new RdbmsHelper() },
                { DataSourceType.GraphML, () => new RdbmsHelper() },
                { DataSourceType.DICOM, () => new RdbmsHelper() },
                { DataSourceType.LAS, () => new RdbmsHelper() },

                // Workflow systems
                { DataSourceType.AWSSWF, () => new RdbmsHelper() },
                { DataSourceType.AWSStepFunctions, () => new RdbmsHelper() },

                // IoT
                { DataSourceType.AWSIoT, () => new RdbmsHelper() },
                { DataSourceType.AWSIoTCore, () => new RdbmsHelper() },
                { DataSourceType.AWSIoTAnalytics, () => new RdbmsHelper() },
                { DataSourceType.AzureIoTHub, () => new RdbmsHelper() },

                // Industrial systems
                { DataSourceType.OPC, () => new RdbmsHelper() },

                // API and web services
                { DataSourceType.RestApi, () => new RestApiHelper() },
                { DataSourceType.WebApi, () => new RestApiHelper() },
                { DataSourceType.GraphQL, () => new RestApiHelper() },
                { DataSourceType.OData, () => new RestApiHelper() },

                // Generic connectors
                { DataSourceType.ODBC, () => new RdbmsHelper() },
                { DataSourceType.OLEDB, () => new RdbmsHelper() },
                { DataSourceType.ADO, () => new RdbmsHelper() },

                // Cloud services and spreadsheets
                { DataSourceType.GoogleSheets, () => new RdbmsHelper() },
                { DataSourceType.MiModel, () => new RdbmsHelper() },

                // Query engines
                { DataSourceType.Presto, () => new RdbmsHelper() },
                { DataSourceType.Trino, () => new RdbmsHelper() },
                { DataSourceType.TimeScale, () => new RdbmsHelper() },

                // CRM Systems
                { DataSourceType.Salesforce, () => new RestApiHelper() },
                { DataSourceType.HubSpot, () => new RestApiHelper() },
                { DataSourceType.Zoho, () => new RestApiHelper() },
                { DataSourceType.Pipedrive, () => new RestApiHelper() },
                { DataSourceType.MicrosoftDynamics365, () => new RestApiHelper() },
                { DataSourceType.Freshsales, () => new RestApiHelper() },
                { DataSourceType.SugarCRM, () => new RestApiHelper() },
                { DataSourceType.Insightly, () => new RestApiHelper() },
                { DataSourceType.Copper, () => new RestApiHelper() },
                { DataSourceType.Nutshell, () => new RestApiHelper() },
                { DataSourceType.SAPCRM, () => new RestApiHelper() },
                { DataSourceType.OracleCRM, () => new RestApiHelper() },

                // Marketing Automation
                { DataSourceType.Mailchimp, () => new RestApiHelper() },
                { DataSourceType.Marketo, () => new RestApiHelper() },
                { DataSourceType.GoogleAds, () => new RestApiHelper() },
                { DataSourceType.ActiveCampaign, () => new RestApiHelper() },
                { DataSourceType.ConstantContact, () => new RestApiHelper() },
                { DataSourceType.Klaviyo, () => new RestApiHelper() },
                { DataSourceType.Sendinblue, () => new RestApiHelper() },
                { DataSourceType.CampaignMonitor, () => new RestApiHelper() },
                { DataSourceType.ConvertKit, () => new RestApiHelper() },
                { DataSourceType.Drip, () => new RestApiHelper() },
                { DataSourceType.MailerLite, () => new RestApiHelper() },
                { DataSourceType.HootsuiteMarketing, () => new RestApiHelper() },
                { DataSourceType.Mailgun, () => new RestApiHelper() },
                { DataSourceType.SendGrid, () => new RestApiHelper() },
                { DataSourceType.Criteo, () => new RestApiHelper() },

                // E-commerce
                { DataSourceType.Shopify, () => new RestApiHelper() },
                { DataSourceType.WooCommerce, () => new RestApiHelper() },
                { DataSourceType.Magento, () => new RestApiHelper() },
                { DataSourceType.BigCommerce, () => new RestApiHelper() },
                { DataSourceType.Squarespace, () => new RestApiHelper() },
                { DataSourceType.Wix, () => new RestApiHelper() },
                { DataSourceType.Etsy, () => new RestApiHelper() },
                { DataSourceType.OpenCart, () => new RestApiHelper() },
                { DataSourceType.Ecwid, () => new RestApiHelper() },
                { DataSourceType.Volusion, () => new RestApiHelper() },
                { DataSourceType.PrestaShop, () => new RestApiHelper() },
                { DataSourceType.BigCartel, () => new RestApiHelper() },

                // Project Management
                { DataSourceType.Jira, () => new RestApiHelper() },
                { DataSourceType.Trello, () => new RestApiHelper() },
                { DataSourceType.Asana, () => new RestApiHelper() },
                { DataSourceType.Monday, () => new RestApiHelper() },
                { DataSourceType.ClickUp, () => new RestApiHelper() },
                { DataSourceType.Basecamp, () => new RestApiHelper() },
                { DataSourceType.Notion, () => new RestApiHelper() },
                { DataSourceType.Wrike, () => new RestApiHelper() },
                { DataSourceType.Smartsheet, () => new RestApiHelper() },
                { DataSourceType.Teamwork, () => new RestApiHelper() },
                { DataSourceType.Podio, () => new RestApiHelper() },
                { DataSourceType.AnyDo, () => new RestApiHelper() },
                { DataSourceType.AzureBoards, () => new RestApiHelper() },
                { DataSourceType.SmartsheetPM, () => new RestApiHelper() },

                // Communication
                { DataSourceType.Slack, () => new RestApiHelper() },
                { DataSourceType.MicrosoftTeams, () => new RestApiHelper() },
                { DataSourceType.Zoom, () => new RestApiHelper() },
                { DataSourceType.GoogleChat, () => new RestApiHelper() },
                { DataSourceType.Discord, () => new RestApiHelper() },
                { DataSourceType.Telegram, () => new RestApiHelper() },
                { DataSourceType.WhatsAppBusiness, () => new RestApiHelper() },
                { DataSourceType.ClickSend, () => new RestApiHelper() },
                { DataSourceType.Kudosity, () => new RestApiHelper() },
                { DataSourceType.Twist, () => new RestApiHelper() },
                { DataSourceType.Chanty, () => new RestApiHelper() },
                { DataSourceType.RocketChat, () => new RestApiHelper() },
                { DataSourceType.Flock, () => new RestApiHelper() },
                { DataSourceType.Mattermost, () => new RestApiHelper() },
                { DataSourceType.RocketChatComm, () => new RestApiHelper() },

                // Cloud Storage
                { DataSourceType.GoogleDrive, () => new RestApiHelper() },
                { DataSourceType.Dropbox, () => new RestApiHelper() },
                { DataSourceType.OneDrive, () => new RestApiHelper() },
                { DataSourceType.Box, () => new RestApiHelper() },
                { DataSourceType.AmazonS3, () => new RestApiHelper() },
                { DataSourceType.pCloud, () => new RestApiHelper() },
                { DataSourceType.iCloud, () => new RestApiHelper() },
                { DataSourceType.Egnyte, () => new RestApiHelper() },
                { DataSourceType.MediaFire, () => new RestApiHelper() },
                { DataSourceType.CitrixShareFile, () => new RestApiHelper() },
                { DataSourceType.GoogleCloudStorage, () => new RestApiHelper() },
                { DataSourceType.Mega, () => new RestApiHelper() },
                { DataSourceType.Backblaze, () => new RestApiHelper() },

                // Payment Gateways
                { DataSourceType.Stripe, () => new RestApiHelper() },
                { DataSourceType.PayPal, () => new RestApiHelper() },
                { DataSourceType.Square, () => new RestApiHelper() },
                { DataSourceType.AuthorizeNet, () => new RestApiHelper() },
                { DataSourceType.Braintree, () => new RestApiHelper() },
                { DataSourceType.Worldpay, () => new RestApiHelper() },
                { DataSourceType.Adyen, () => new RestApiHelper() },
                { DataSourceType.TwoCheckout, () => new RestApiHelper() },
                { DataSourceType.Razorpay, () => new RestApiHelper() },
                { DataSourceType.Payoneer, () => new RestApiHelper() },
                { DataSourceType.Wise, () => new RestApiHelper() },
                { DataSourceType.Coinbase, () => new RestApiHelper() },
                { DataSourceType.Venmo, () => new RestApiHelper() },
                { DataSourceType.BitPay, () => new RestApiHelper() },

                // Social Media
                { DataSourceType.Facebook, () => new RestApiHelper() },
                { DataSourceType.Twitter, () => new RestApiHelper() },
                { DataSourceType.Instagram, () => new RestApiHelper() },
                { DataSourceType.LinkedIn, () => new RestApiHelper() },
                { DataSourceType.Pinterest, () => new RestApiHelper() },
                { DataSourceType.YouTube, () => new RestApiHelper() },
                { DataSourceType.TikTok, () => new RestApiHelper() },
                { DataSourceType.Snapchat, () => new RestApiHelper() },
                { DataSourceType.Reddit, () => new RestApiHelper() },
                { DataSourceType.Buffer, () => new RestApiHelper() },
                { DataSourceType.Hootsuite, () => new RestApiHelper() },
                { DataSourceType.TikTokAds, () => new RestApiHelper() },
                { DataSourceType.Loomly, () => new RestApiHelper() },
                { DataSourceType.Threads, () => new RestApiHelper() },
                { DataSourceType.Mastodon, () => new RestApiHelper() },
                { DataSourceType.Bluesky, () => new RestApiHelper() },

                // Workflow Automation
                { DataSourceType.Zapier, () => new RestApiHelper() },
                { DataSourceType.Make, () => new RestApiHelper() },
                { DataSourceType.Airtable, () => new RestApiHelper() },
                { DataSourceType.MicrosoftPowerAutomate, () => new RestApiHelper() },
                { DataSourceType.Calendly, () => new RestApiHelper() },
                { DataSourceType.Doodle, () => new RestApiHelper() },
                { DataSourceType.Eventbrite, () => new RestApiHelper() },
                { DataSourceType.Typeform, () => new RestApiHelper() },
                { DataSourceType.Jotform, () => new RestApiHelper() },
                { DataSourceType.WordPress, () => new RestApiHelper() },
                { DataSourceType.TLDV, () => new RestApiHelper() },
                { DataSourceType.Fathom, () => new RestApiHelper() },
                { DataSourceType.Integromat, () => new RestApiHelper() },
                { DataSourceType.TrayIO, () => new RestApiHelper() },

                // Developer Tools
                { DataSourceType.GitHub, () => new RestApiHelper() },
                { DataSourceType.GitLab, () => new RestApiHelper() },
                { DataSourceType.Bitbucket, () => new RestApiHelper() },
                { DataSourceType.Jenkins, () => new RestApiHelper() },
                { DataSourceType.CircleCI, () => new RestApiHelper() },
                { DataSourceType.Postman, () => new RestApiHelper() },
                { DataSourceType.SwaggerHub, () => new RestApiHelper() },
                { DataSourceType.AzureDevOps, () => new RestApiHelper() },
                { DataSourceType.SonarQube, () => new RestApiHelper() },
                { DataSourceType.Intercom, () => new RestApiHelper() },
                { DataSourceType.Drift, () => new RestApiHelper() },

                // Customer Support
                { DataSourceType.Zendesk, () => new RestApiHelper() },
                { DataSourceType.Freshdesk, () => new RestApiHelper() },
                { DataSourceType.HelpScout, () => new RestApiHelper() },
                { DataSourceType.ZohoDesk, () => new RestApiHelper() },
                { DataSourceType.Kayako, () => new RestApiHelper() },
                { DataSourceType.LiveAgent, () => new RestApiHelper() },
                { DataSourceType.Front, () => new RestApiHelper() },

                // Analytics and Reporting
                { DataSourceType.GoogleAnalytics, () => new RestApiHelper() },
                { DataSourceType.Mixpanel, () => new RestApiHelper() },
                { DataSourceType.Hotjar, () => new RestApiHelper() },
                { DataSourceType.Amplitude, () => new RestApiHelper() },
                { DataSourceType.Heap, () => new RestApiHelper() },
                { DataSourceType.Databox, () => new RestApiHelper() },
                { DataSourceType.Geckoboard, () => new RestApiHelper() },
                { DataSourceType.Cyfe, () => new RestApiHelper() },
                { DataSourceType.Tableau, () => new RestApiHelper() },
                { DataSourceType.PowerBI, () => new RestApiHelper() },

                // IoT and Smart Devices
                { DataSourceType.Twilio, () => new RestApiHelper() },
                { DataSourceType.Plaid, () => new RestApiHelper() },
                { DataSourceType.DocuSign, () => new RestApiHelper() },
                { DataSourceType.PhilipsHue, () => new RestApiHelper() },
                { DataSourceType.Nest, () => new RestApiHelper() },
                { DataSourceType.SmartThings, () => new RestApiHelper() },
                { DataSourceType.Tuya, () => new RestApiHelper() },
                { DataSourceType.Particle, () => new RestApiHelper() },
                { DataSourceType.ArduinoCloud, () => new RestApiHelper() },

                // Accounting
                { DataSourceType.FreshBooks, () => new RestApiHelper() },
                { DataSourceType.WaveApps, () => new RestApiHelper() },
                { DataSourceType.SageBusinessCloud, () => new RestApiHelper() },
                { DataSourceType.MYOB, () => new RestApiHelper() },
                { DataSourceType.QuickBooks, () => new RestApiHelper() },
                { DataSourceType.Xero, () => new RestApiHelper() },
                { DataSourceType.QuickBooksOnline, () => new RestApiHelper() },
                { DataSourceType.SageIntacct, () => new RestApiHelper() },
                { DataSourceType.ZohoBooks, () => new RestApiHelper() },
                { DataSourceType.BenchAccounting, () => new RestApiHelper() },

                // Blockchain
                { DataSourceType.Ethereum, () => new RestApiHelper() },
                { DataSourceType.Hyperledger, () => new RestApiHelper() },
                { DataSourceType.BitcoinCore, () => new RestApiHelper() },

                // File Transfer
                { DataSourceType.FTP, () => new RdbmsHelper() },
                { DataSourceType.SFTP, () => new RdbmsHelper() },

                // Email Protocols
                { DataSourceType.Email, () => new RestApiHelper() },
                { DataSourceType.IMAP, () => new RestApiHelper() },
                { DataSourceType.POP3, () => new RestApiHelper() },
                { DataSourceType.SMTP, () => new RestApiHelper() },
                { DataSourceType.Gmail, () => new RestApiHelper() },
                { DataSourceType.Outlook, () => new RestApiHelper() },
                { DataSourceType.Yahoo, () => new RestApiHelper() },

                // Remote Procedure Calls
                { DataSourceType.XMLRPC, () => new RestApiHelper() },
                { DataSourceType.SOAP, () => new RestApiHelper() },
                { DataSourceType.JSONRPC, () => new RestApiHelper() },
                { DataSourceType.GRPC, () => new RestApiHelper() },
                { DataSourceType.WebSocket, () => new RestApiHelper() },
                { DataSourceType.SSE, () => new RestApiHelper() }
            };
        }

        /// <summary>
        /// Creates and returns the appropriate IDataSourceHelper for the given datasource type.
        /// </summary>
        /// <param name="datasourceType">The datasource type</param>
        /// <returns>IDataSourceHelper implementation, or null if not supported</returns>
        public static IDataSourceHelper CreateHelper(DataSourceType datasourceType)
        {
            if (_helperFactories.TryGetValue(datasourceType, out var factory))
            {
                return factory();
            }

            return null;
        }

        /// <summary>
        /// Checks if a helper is available for the specified datasource type.
        /// </summary>
        /// <param name="datasourceType">The datasource type</param>
        /// <returns>True if a helper exists, false otherwise</returns>
        public static bool IsHelperAvailable(DataSourceType datasourceType)
        {
            return _helperFactories.ContainsKey(datasourceType);
        }

        /// <summary>
        /// Registers a custom helper factory for a datasource type.
        /// </summary>
        /// <param name="datasourceType">The datasource type</param>
        /// <param name="factory">Factory function to create the helper</param>
        public static void RegisterHelper(DataSourceType datasourceType, Func<IDataSourceHelper> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _helperFactories[datasourceType] = factory;
        }

        /// <summary>
        /// Gets all supported datasource types that have helpers.
        /// </summary>
        /// <returns>Collection of supported datasource types</returns>
        public static IEnumerable<DataSourceType> GetSupportedDataSources()
        {
            return _helperFactories.Keys;
        }
    }
}
