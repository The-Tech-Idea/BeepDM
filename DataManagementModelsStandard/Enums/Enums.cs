﻿
namespace TheTechIdea.Beep.Utilities
{
    public enum FolderFileTypes { Addin, DataView, DataFiles, ProjectClass, ConnectionDriver, ProjectData, GFX, OtherDLL,Entities,Mapping ,WorkFlows,Scripts, ScriptsLogs,Config,Reports,Logs,Misc,LoaderExtensions,Projects,Global,Shared,Private,DataSources, Builtin }

    public enum BeepConfigType
    {
        Application,DataConnector,Container
    }
    public enum ProjectFolderType
    {
        Files,Project,Library
    }
    public enum AppType
    {
        Web,Winform,Andriod,IOS,Linux,WPF
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
    {
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
        DuckDB, GoogleSheets, MiModel, Presto, Trino, TimeScale,WebApi, RestApi, GraphQL, OData, ODBC, OLEDB, ADO, EntityFramework
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
        AnalyticsPlatform
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
    
    public enum DefaultValueType
    {
        DisplayLookup,ReplaceValue,Rule
    }
    public enum AddinType
    {
        Form,Control,Class,Page,Link,Branch,Menu,Button,Tab,Panel,Grid,Tree,Chart,
        Report,Graph,Map,Table,View,Entity,Data,DataView
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
