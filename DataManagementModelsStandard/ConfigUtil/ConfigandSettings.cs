
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;

//using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Util
{
    public partial class ConfigandSettings
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
        public string ProjectsPath { get; set; }
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
        public bool LocalDB { get; set; } = false;
        public bool InMemory { get; set; } = false;
        public bool IsDataSource { get; set; } = false;
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

    public class CategoryFolder : Entity
    {

        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid;
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }
        private string _parentguidid;
        public string ParentGuidID
        {
            get { return _parentguidid; }
            set { SetProperty(ref _parentguidid, value); }
        }
        private string _foldername;
        public string FolderName
        {
            get { return _foldername; }
            set { SetProperty(ref _foldername, value); }
        }

        private string _rootname;
        public string RootName
        {
            get { return _rootname; }
            set { SetProperty(ref _rootname, value); }
        }

        private string _parentname;
        public string ParentName
        {
            get { return _parentname; }
            set { SetProperty(ref _parentname, value); }
        }

        private int _parentid;
        public int ParentID
        {
            get { return _parentid; }
            set { SetProperty(ref _parentid, value); }
        }

        private bool _isparentroot;
        public bool IsParentRoot
        {
            get { return _isparentroot; }
            set { SetProperty(ref _isparentroot, value); }
        }
        private bool _isparentFolder;
        public bool IsParentFolder
        {
            get { return _isparentFolder; }
            set { SetProperty(ref _isparentFolder, value); }
        }

        private bool _isphysicalfolder;
        public bool IsPhysicalFolder
        {
            get { return _isphysicalfolder; }
            set { SetProperty(ref _isphysicalfolder, value); }
        }
        private BindingList<string> _items;
        public BindingList<string> items
        {
            get { return _items; }
            set { SetProperty(ref _items, value); }
        }
      
        public CategoryFolder()
        {
            _items = new BindingList<string>();
            GuidID = Guid.NewGuid().ToString();
        }
    }



}
