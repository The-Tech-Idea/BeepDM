using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Util
{
    public enum FolderFileTypes { Addin, DataView, DataFiles, ProjectClass, ConnectionDriver, ProjectData, GFX, OtherDLL,Entities,Mapping ,WorkFlows,Scripts, ScriptsLogs,Config}

    public enum AppType
    {
        Web,Winform,Andriod,IOS,Linux,WPF
    }
    public enum AppComponentType
    {
        SplashScreen,MainForm,Report,DataEntry
    }
    public enum ViewType
    { Table, Query,Code,File,Url }
    public enum ColumnViewType
    { TextBox, CheckBox, ComboBox, DateCalendar,Label }
    public enum DataSourceType
    {
        Oracle , SqlServer, Mysql, SqlCompact, SqlLite,
        Text,CSV,Xls,WebService,Json,xml,
        Postgre,Firebase,FireBird,Couchbase,RavenDB,MongoDB,CouchDB,VistaDB,DB2,OPC,Kafka
    }
    public enum DatasourceCategory
    {
        RDBMS,FILE,WEBAPI,NOSQL,CLOUD,VIEWS,STREAM,QUEUE
    }
    public enum FileTypes
    {
        Text, Excel
    }
    public enum DbFieldCategory
    {
        String,Numeric,Date
    }
    public enum Sqlcommandtype
    { getTable,getlistoftables, getPKforTable, getFKforTable, getChildTable, getParentTable,getFktableValues,CheckTableExist}
    
    public enum DefaultValueType
    {
        DisplayLookup,ReplaceValue
    }
}
