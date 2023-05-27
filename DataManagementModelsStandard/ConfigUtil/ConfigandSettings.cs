
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Vis;

//using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Util
{
    public class ConfigandSettings
    {
        public ConfigandSettings()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public string ID { get; set; }
        public string GuidID { get; set; }
        public string SystemEntryFormName { get; set; } 
        public string ConfigPath { get; set; } 
        public string ExePath { get; set; } 
        public string DataFilePath { get; set; }
        public string ProjectDataPath { get; set; } 
        public string DataViewPath { get; set; } 
        public string ScriptsPath { get; set; } 
        public string ScriptsLogsPath { get; set; }
        public string AddinPath { get; set; }
        public string ClassPath { get; set; }
        public string EntitiesPath { get; set; }
        public string GFXPath { get; set; }
        public string MappingPath { get; set; }
        public string WorkFlowPath { get; set; }
        public string ConnectionDriversPath { get; set; }
        public string OtherDLLPath { get; set; }
        public string DefaultReportWriter { get; set; }
        public List<StorageFolders> Folders { get; set; } = new List<StorageFolders>();
        
    }
    public class StorageFolders
    {
        public string ID { get; set; }
        public string GuidID { get; set; }
        public string FolderPath { get; set; }
        public FolderFileTypes FolderFilesType { get; set; }
        public string EntrypointClass { get; set; }
        public StorageFolders()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public StorageFolders(string pFolderPath)
        {
            GuidID = Guid.NewGuid().ToString();
            FolderPath = pFolderPath;
        }
        public StorageFolders(string pFolderPath, FolderFileTypes pFoderType)
        {
            GuidID = Guid.NewGuid().ToString();
            FolderPath = pFolderPath;
            FolderFilesType = pFoderType;
        }
    }
    public class AssemblyClassDefinition
    {
        public AssemblyClassDefinition()
        {
            
        }
        public string ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string className { get; set; }
        public string dllname { get; set; }
        public string PackageName { get; set; }
        public int Order { get; set; } = 0;
        public string Imagename { get; set; }
        public string RootName { get; set; }
        //public EnumBranchType BranchType { get; set; }
      
        public Type type { get; set; }
        public string componentType { get; set; }
        public AddinAttribute classProperties { get; set; } = new AddinAttribute();
        public AddinVisSchema  VisSchema { get; set; }=new AddinVisSchema();
        
        public List<MethodsClass> Methods { get; set; } = new List<MethodsClass>();
    }
    public class MethodsClass
    {
        public MethodsClass()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public int ID { get; set; }
        public string GuidID { get; set; }
        public MethodInfo Info { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public bool Hidden { get; set; }
        public bool Click { get; set; } = false;
        public Type type { get; set; }
        public bool DoubleClick { get; set; } = false;
        public string iconimage { get; set; } = null;
        public EnumPointType PointType { get; set; }
        public string ObjectType { get; set; } = null;
        public string ClassType { get; set; } = null;
        public string misc { get; set; } = null;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NONE;
        public DataSourceType DatasourceType { get; set; } = DataSourceType.NONE;
        public ShowinType Showin { get; set; } = ShowinType.Both;
    }
    public class ConnectionDriversConfig
    {
        public ConnectionDriversConfig()
        {
             
        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string PackageName { get; set; }
        public string DriverClass { get; set; }
        public string version { get; set; }
        public string dllname { get; set; }
        public string AdapterType { get; set; }
        public string CommandBuilderType { get; set; }
        public string DbConnectionType { get; set; }
        public string DbTransactionType { get; set; }
        public string ConnectionString { get; set; }
        public string parameter1 { get; set; }
        public string parameter2 { get; set; }
        public string parameter3 { get; set; }
        public string iconname { get; set; }
        public string classHandler { get; set; }
        public bool ADOType { get; set; } = false;
        public bool CreateLocal { get; set; } = false;
        public string extensionstoHandle { get; set; }
        public bool Favourite { get; set; }=false;
        public DatasourceCategory DatasourceCategory { get; set; }
        public DataSourceType DatasourceType { get; set; }
        public bool IsMissing { get;set; }=false;
    }
    public class ConnectionDriversTypes
    {
        public ConnectionDriversTypes()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public int ID { get; set; }
        public string GuidID { get; set; }
        public string PackageName { get; set; }
        public string DriverClass { get; set; }
        public string version { get; set; }
        public string dllname { get; set; }
        public Type AdapterType { get; set; }
        public Type CommandBuilderType { get; set; }
        public Type DbConnectionType { get; set; }

    }
    public class DataSourceConnectionConfig
    {
        public DataSourceConnectionConfig()
        {
            GuidID = Guid.NewGuid().ToString();
        }
        public int ID { get; set; }
        public string GuidID { get; set; }
        public string DataSourceName { get; set; }
        public DatasourceCategory datasourceCategory{get;set;}
        public List<string> ConnectionDrivers { get; set; } = new List<string>();

    }
   
    public class CategoryFolder
    {
        public int ID { get; set; }
        public string GuidID { get; set; }  = Guid.NewGuid().ToString();
        public string FolderName { get; set; }
        public string RootName { get; set; }
        public string ParentName { get; set; }
        public int ParentID { get; set; }
        public bool IsParentRoot { get; set; }=true;
        public bool IsPhysicalFolder { get; set; }=false;
        public BindingList<string> items { get; set; } = new BindingList<string>();
        public CategoryFolder()
        {

        }
    }
    public class Function2FunctionAction
    {
        public Function2FunctionAction()
        {
           
        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string ActionType { get; set; } //Event or Function
        public string Event { get; set; }
        public string FromClass { get; set; }
        public string FromMethod { get; set; }
        public string ToClass { get; set; }
        public string ToMethod { get; set; }
        public int FromID { get; set; }
        public int ToID { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
        public string Param4 { get; set; }
        public string Param5 { get; set; }
        public string Param6 { get; set; }
    }
    public class Event
    {
        public Event()
        {

        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string EventName { get; set; }
    }
    public class DefaultValue
    {
        public DefaultValue()
        {

        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string propertyName { get; set; }
        public string propoertValue { get; set; }
        public string Rule { get; set; }
        public DefaultValueType propertyType { get; set; }
        public string propertyCategory { get; set; }
       
    }
}
