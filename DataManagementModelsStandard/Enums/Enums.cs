using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Util
{
    public enum FolderFileTypes { Addin, DataView, DataFiles, ProjectClass, ConnectionDriver, ProjectData, GFX, OtherDLL,Entities,Mapping ,WorkFlows,Scripts, ScriptsLogs,Config,Reports,Logs,Misc,LoaderExtensions,Projects}

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
        NONE, Oracle , SqlServer, Mysql, SqlCompact, SqlLite,
        Text,CSV,Xls,WebService,Json,xml,DuckDB,RealIM,Hdf5,
        Postgre,Firebase,FireBird,Couchbase,RavenDB,MongoDB,CouchDB,VistaDB,DB2,OPC,Kafka,Redis,Hadoop,LiteDB,SnowFlake
    }
    public enum DatasourceCategory
    {
        RDBMS,FILE,WEBAPI,NOSQL,CLOUD,VIEWS,STREAM,QUEUE,NONE
    }
    public enum FileTypes
    {
        Text, Excel,xml,json,Parquet, Avro, ORC, TSV,Onnx
    }
    public enum DbFieldCategory
    {
        String,Numeric,Date
    }
    public enum Sqlcommandtype
    { getTable,getlistoftables, getPKforTable, getFKforTable, getChildTable, getParentTable,getFktableValues,CheckTableExist}
    
    public enum DefaultValueType
    {
        DisplayLookup,ReplaceValue,Rule
    }
    public enum AddinType
    {
        Form,Control,Class,Page,Link
    }
    public enum TransActionType
    {
        Insert,Update,Delete,Select
    }
    public enum EntityValidatorMesseges
    {
        OK,NullField,DuplicateValue,MissingRefernceValue
    }

}
