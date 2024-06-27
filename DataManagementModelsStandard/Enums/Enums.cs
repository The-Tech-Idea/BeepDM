
namespace TheTechIdea.Util
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
        NONE, Oracle, SqlServer, Mysql, SqlCompact, SqlLite,
        Text, CSV, Xls, WebService, Json, XML, DuckDB, RealIM, Hdf5, Parquet, Avro, ORC, TSV, Onnx,
        Postgre, Firebase, FireBird, Couchbase, RavenDB, MongoDB, CouchDB, VistaDB, DB2, OPC, Kafka, Redis, Hadoop, LiteDB, SnowFlake, ElasticSearch,
        Petastorm, MiModel, RocketSet, Spanner, Hana, Cockroach, Firebolt, Hologres, Presto, Trino, GoogleBigQuery, GoogleSheets, TerraData, TimeScale,
        Vertica, DataBricks, AzureCloud,
        HTML, SQL, INI, Log, PDF, Doc, Docx, PPT, PPTX,
        YAML, Markdown,
        Feather, TFRecord, RecordIO, LibSVM, GraphML, DICOM, LAS, Cassandra, Neo4j, ArangoDB, OrientDB, InfluxDB, ClickHouse, Kudu, Druid, Pinot, DynamoDB, Supabase
    }

    public enum DatasourceCategory
    {
        RDBMS,FILE,WEBAPI,NOSQL,CLOUD,VIEWS,STREAM,QUEUE,NONE,INMEMORY
    }
    public enum FileTypes
    {
        Text, CSV, XML, Json, Xls, Xlsx, TSV, YAML, Markdown, HTML, SQL, INI, Log, PDF, Doc, Docx, PPT, PPTX,
        Parquet, Avro, ORC, Onnx, Feather, TFRecord, RecordIO, LibSVM, GraphML, DICOM, LAS,Entity
    }

    public enum DbFieldCategory
    {
        String,Numeric,Date
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
