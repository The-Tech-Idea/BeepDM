using System;

namespace TheTechIdea.Beep.Utilities
{
    public enum EntityType
    {
        Entity,
        View,
        Table,
        Query,
        StoredProcedure,
        Function,
        Report,
        DataView,
        DataSource,
        DataConnector,
        DataSet,
        DataTable,
        InMemory,
        Schema,
        DataBase

    }


    /// <summary>
    /// Cloud provider types supported by the class creator
    /// </summary>
    public enum CloudProviderType
    {
        Azure,
        AWS,
        GCP,
        Custom
    }

    /// <summary>
    /// Architectural patterns that can be applied to generated code
    /// </summary>
    public enum ArchitecturalPattern
    {
        MVVM,
        CleanArchitecture,
        CQRS,
        EventSourcing,
        Hexagonal,
        MicroserviceAPI
    }

    /// <summary>
    /// Contains a code improvement suggestion
    /// </summary>
    public class CodeSuggestion
    {
        public string Description { get; set; }
        public string RecommendedCode { get; set; }
        public int LineNumber { get; set; }
        public string SuggestionType { get; set; }
    }

    public enum FolderFileTypes { Addin, DataView, DataFiles, ProjectClass, ConnectionDriver, ProjectData, GFX, OtherDLL,Entities,Mapping ,WorkFlows,Scripts, ScriptsLogs,Config,Reports,Logs,Misc,LoaderExtensions,Projects,Global,Shared,Private,DataSources, Builtin , Nugget,SharedAssembly }

    public enum BeepConfigType
    {
        Application,DataConnector,Container
    }
    [Flags]
    public enum ProjectFolderType
    {
        None = 0,        // No folder type
        Files = 1,       // Represents a folder for files
        Project = 2,     // Represents a folder for projects
        Library = 4,     // Represents a folder for libraries
    }

    [Flags]
    public enum AppType
    {
        None = 0,          // No app type
        Web = 1,           // Represents a web application
        Winform = 2,       // Represents a WinForms application
        Android = 4,       // Represents an Android application
        IOS = 8,           // Represents an iOS application
        Linux = 16,        // Represents a Linux application
        WPF = 32           // Represents a WPF application
    }

    public enum ReportType
    {
        html, xls, csv, pdf, txt
    }
    public enum AppOrientation
    {
        Portrait = 0,
        Landscape = 1
    }
    public enum ViewType
    { Table, Query,Code,File,Url }
    public enum ColumnViewType
    { TextBox, CheckBox, ComboBox, DateCalendar,Label }
    public enum DataSourceType
    {Unknown,
        // Relational Databases
        NONE, Oracle, SqlServer, Mysql, SqlCompact, SqlLite, Postgre, FireBird, DB2, SnowFlake, Hana, Cockroach, Spanner, TerraData, Vertica, AzureSQL, AWSRDS,

        // NoSQL Databases
        MongoDB, CouchDB, RavenDB, Couchbase, Redis, DynamoDB, Firebase, LiteDB, ArangoDB, Neo4j, Cassandra, OrientDB, ElasticSearch, ClickHouse, InfluxDB, VistaDB,

        // Graph Databases
        TigerGraph, JanusGraph,

        // Big Data / Columnar
        Hadoop, Kudu, Druid, Pinot, Parquet, Avro, ORC, Feather,

        // In-Memory
        RealIM, Petastorm, RocketSet,

        // Cloud Services
        AWSRedshift, GoogleBigQuery, AWSGlue, AWSAthena, AzureCloud, DataBricks, Firebolt, Hologres, Supabase,

        // Streaming and Messaging
        Kafka, RabbitMQ, ActiveMQ, Pulsar, MassTransit, Nats, ZeroMQ, AWSKinesis, AWSSQS, AWSSNS, AzureServiceBus,

        // Machine Learning
        TFRecord, ONNX, PyTorchData, ScikitLearnData,

        // File Formats
        FlatFile, CSV, TSV, Text, YAML,Json, Markdown, Log, INI, JSON, XML, Xls, Doc, Docx, PPT, PPTX, PDF,Onnx,  RecordIO,

        // Specialized Formats
        Hdf5, LibSVM, GraphML, DICOM, LAS,

        // Workflow Systems
        AWSSWF, AWSStepFunctions,

        // Internet of Things (IoT)
        AWSIoT, AWSIoTCore, AWSIoTAnalytics,

        // Search Platforms
         Solr,

        // Industrial and Specialized Systems
        OPC,

        // Miscellaneous
        DuckDB, GoogleSheets, MiModel, Presto, Trino, TimeScale,WebApi, RestApi, GraphQL, OData, ODBC, OLEDB, ADO, 

        //VectorDB
         ChromaDB,
        PineCone,
        Qdrant,
        ShapVector,
        Weaviate,
        Milvus,
        RedisVector,
        Zilliz,
        Vespa, MariaDB,

        // Connectors (Expanded List)
        Salesforce, HubSpot, Zoho, Pipedrive, MicrosoftDynamics365, Freshsales, SugarCRM, Insightly, Copper, Nutshell, // CRM
        Mailchimp, Marketo, GoogleAds, ActiveCampaign, ConstantContact, Klaviyo, Sendinblue, CampaignMonitor, ConvertKit, Drip, MailerLite, // Marketing
        Shopify, WooCommerce, Magento, BigCommerce, Squarespace, Wix, Etsy, OpenCart, Ecwid, Volusion, // E-commerce
        Jira, Trello, Asana, Monday, ClickUp, Basecamp, Notion, Wrike, Smartsheet, Teamwork, Podio, // Project Management
        Slack, MicrosoftTeams, Zoom, GoogleChat, Discord, Telegram, WhatsAppBusiness, Twist, Chanty, RocketChat, Flock, // Communication
        GoogleDrive, Dropbox, OneDrive, Box, AmazonS3, pCloud, iCloud, Egnyte, MediaFire, CitrixShareFile, // Cloud Storage
        Stripe, PayPal, Square, AuthorizeNet, Braintree, Worldpay, Adyen, TwoCheckout, Razorpay, Payoneer, Wise, // Payment Gateways
        Facebook, Twitter, Instagram, LinkedIn, Pinterest, YouTube, TikTok, Snapchat, Reddit, Buffer, Hootsuite, TikTokAds, // Social Media
        Zapier, Make, Airtable, MicrosoftPowerAutomate, Calendly, Doodle, Eventbrite, // Workflow Automation
        GitHub, GitLab, Bitbucket, Jenkins, CircleCI, Postman, SwaggerHub, AzureDevOps, // Developer Tools
        Zendesk, Freshdesk, HelpScout, ZohoDesk, Kayako, LiveAgent, Front, // Customer Support
        GoogleAnalytics, Mixpanel, Hotjar, Amplitude, Heap, Databox, Geckoboard, Cyfe, // Analytics and Reporting
        Twilio, Plaid, QuickBooks, Xero, DocuSign, PhilipsHue, Nest, SmartThings, Tuya // IoT
    }
    public enum DatasourceCategory
    {
        RDBMS,
        FILE,
        WEBAPI,
        NOSQL,
        CLOUD,
        VIEWS,
        STREAM,
        QUEUE,
        NONE,
        INMEMORY,
        GraphDB,
        TimeSeriesDB,
        MessageQueue,
        BigData,
        DocumentDB,
        KeyValueDB,
        ColumnarDB,
        MLModel,
        SearchEngine,
        StreamProcessing,
        GraphFile,
        Geospatial,
        IoT,
        Workflow,
        AnalyticsPlatform,
        VectorDB,
        DataLake,
        DataWarehouse,
        DataMart,
        DataPipeline,
        DataMesh,
        DataFabric,
        Connector // Added new category for connectors
    }

    public enum FileTypes
    {
        Text, CSV, XML, Json, Xls, Xlsx, TSV, YAML, Markdown, HTML, SQL, INI, Log, PDF, Doc, Docx, PPT, PPTX,
        Parquet, Avro, ORC, Onnx, Feather, TFRecord, RecordIO, LibSVM, GraphML, DICOM, LAS,Entity
    }

    public enum DbFieldCategory
    {
        String,        // Text-based fields
        Char,          // Fixed-length text fields
        Numeric,       // Integer, float, double, decimal, etc.
        Date,          // Date and DateTime fields
        Boolean,       // True/False fields
        Binary,        // Binary data, such as images or files
        Guid,          // Globally Unique Identifier fields
        Json,          // JSON or structured data
        Xml,           // XML formatted data
        Geography,     // Spatial data types, e.g., points, lines, polygons
        Currency,      // Monetary values
        Enum,          // Enum types or lookup values
        Timestamp,     // Timestamp or row version for concurrency
        Complex        // Complex or nested types, possibly serialized
    }
    public enum Sqlcommandtype
    { getTable,getlistoftables, getPKforTable, getFKforTable, getChildTable, getParentTable,getFktableValues,CheckTableExist, getlistoftablesfromotherschema }

    /// <summary>
    /// Enum representing various methods to apply default value rules within Beep DM.
    /// </summary>
    public enum DefaultValueType
    {
        /// <summary>
        /// Retrieves a default value based on display lookup logic.
        /// </summary>
        DisplayLookup,

        /// <summary>
        /// Directly replaces a value with a predefined default.
        /// </summary>
        ReplaceValue,

        /// <summary>
        /// Applies a standard rule-based default value.
        /// </summary>
        Rule,

        /// <summary>
        /// Computes the default value using calculations or formulas.
        /// </summary>
        Computed,

        /// <summary>
        /// Maps or translates a value from one format or system to another.
        /// </summary>
        Mapping,

        /// <summary>
        /// Provides a default based on specific conditions or context.
        /// </summary>
        Conditional,

        /// <summary>
        /// Allows for custom, user-defined logic to determine the default value.
        /// </summary>
        Custom
    }

    public enum AddinType
    {
        Form,Control,Class,Page,Link,Branch,Menu,Button,Tab,Panel,Grid,Tree,Chart,
        Report,Graph,Map,Table,View,Entity,Data,DataView,Config,ConnectionProperties,Workflow,Script,Other
    }
    public enum TransActionType
    {
        Insert,Update,Delete,Select
    }
    public enum EntityValidatorMesseges
    {
        OK,NullField,DuplicateValue,MissingRefernceValue
    }
    public enum LogAction
    {
        Insert,Update,Delete,Select,Error
    }
}
