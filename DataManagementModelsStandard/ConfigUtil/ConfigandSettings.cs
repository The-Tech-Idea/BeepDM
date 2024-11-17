
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
using TheTechIdea.Beep.Utilities;

//using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.ConfigUtil
{
    public partial class ConfigandSettings : Entity
    {
        public ConfigandSettings()
        {
            GuidID = Guid.NewGuid().ToString();
            Folders = new List<StorageFolders>();
        }

        private string _id;
        public string ID
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

        private string _systementryformname;
        public string SystemEntryFormName
        {
            get { return _systementryformname; }
            set { SetProperty(ref _systementryformname, value); }
        }

        private string _configpath;
        public string ConfigPath
        {
            get { return _configpath; }
            set { SetProperty(ref _configpath, value); }
        }

        private string _exepath;
        public string ExePath
        {
            get { return _exepath; }
            set { SetProperty(ref _exepath, value); }
        }

        private string _datafilepath;
        public string DataFilePath
        {
            get { return _datafilepath; }
            set { SetProperty(ref _datafilepath, value); }
        }

        private string _projectdatapath;
        public string ProjectDataPath
        {
            get { return _projectdatapath; }
            set { SetProperty(ref _projectdatapath, value); }
        }

        private string _dataviewpath;
        public string DataViewPath
        {
            get { return _dataviewpath; }
            set { SetProperty(ref _dataviewpath, value); }
        }

        private string _scriptspath;
        public string ScriptsPath
        {
            get { return _scriptspath; }
            set { SetProperty(ref _scriptspath, value); }
        }

        private string _scriptslogspath;
        public string ScriptsLogsPath
        {
            get { return _scriptslogspath; }
            set { SetProperty(ref _scriptslogspath, value); }
        }

        private string _addinpath;
        public string AddinPath
        {
            get { return _addinpath; }
            set { SetProperty(ref _addinpath, value); }
        }

        private string _classpath;
        public string ClassPath
        {
            get { return _classpath; }
            set { SetProperty(ref _classpath, value); }
        }

        private string _entitiespath;
        public string EntitiesPath
        {
            get { return _entitiespath; }
            set { SetProperty(ref _entitiespath, value); }
        }

        private string _gfxpath;
        public string GFXPath
        {
            get { return _gfxpath; }
            set { SetProperty(ref _gfxpath, value); }
        }

        private string _mappingpath;
        public string MappingPath
        {
            get { return _mappingpath; }
            set { SetProperty(ref _mappingpath, value); }
        }

        private string _workflowpath;
        public string WorkFlowPath
        {
            get { return _workflowpath; }
            set { SetProperty(ref _workflowpath, value); }
        }

        private string _connectiondriverspath;
        public string ConnectionDriversPath
        {
            get { return _connectiondriverspath; }
            set { SetProperty(ref _connectiondriverspath, value); }
        }

        private string _otherdllpath;
        public string OtherDLLPath
        {
            get { return _otherdllpath; }
            set { SetProperty(ref _otherdllpath, value); }
        }

        private string _defaultreportwriter;
        public string DefaultReportWriter
        {
            get { return _defaultreportwriter; }
            set { SetProperty(ref _defaultreportwriter, value); }
        }

        private string _projectspath;
        public string ProjectsPath
        {
            get { return _projectspath; }
            set { SetProperty(ref _projectspath, value); }
        }

        private string _sharedpath;
        public string SharedPath
        {
            get { return _sharedpath; }
            set { SetProperty(ref _sharedpath, value); }
        }

        private string _privatepath;
        public string PrivatePath
        {
            get { return _privatepath; }
            set { SetProperty(ref _privatepath, value); }
        }

        private string _globalpath;
        public string GlobalPath
        {
            get { return _globalpath; }
            set { SetProperty(ref _globalpath, value); }
        }

        private string _reportpath;
        public string ReportPath
        {
            get { return _reportpath; }
            set { SetProperty(ref _reportpath, value); }
        }

        private string _datasourcespath;
        public string DataSourcesPath
        {
            get { return _datasourcespath; }
            set { SetProperty(ref _datasourcespath, value); }
        }

        private string _loaderextensionspath;
        public string LoaderExtensionsPath
        {
            get { return _loaderextensionspath; }
            set { SetProperty(ref _loaderextensionspath, value); }
        }

        private List<StorageFolders> _folders;
        public List<StorageFolders> Folders
        {
            get { return _folders; }
            set { SetProperty(ref _folders, value); }
        }

    }
    public class StorageFolders:Entity
    {

        private string _id;
        public string ID
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

        private string _folderpath;
        public string FolderPath
        {
            get { return _folderpath; }
            set { SetProperty(ref _folderpath, value); }
        }

        private FolderFileTypes _folderfilestype;
        public FolderFileTypes FolderFilesType
        {
            get { return _folderfilestype; }
            set { SetProperty(ref _folderfilestype, value); }
        }

        private string _entrypointclass;
        public string EntrypointClass
        {
            get { return _entrypointclass; }
            set { SetProperty(ref _entrypointclass, value); }
        }
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
    public class AssemblyClassDefinition:Entity
    {
        public AssemblyClassDefinition()
        {

        }

        private string _id;
        public string ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid = Guid.NewGuid().ToString();
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        } 

        private string _classname;
        public string className
        {
            get { return _classname; }
            set { SetProperty(ref _classname, value); }
        }

        private string _dllname;
        public string dllname
        {
            get { return _dllname; }
            set { SetProperty(ref _dllname, value); }
        }

        private string _packagename;
        public string PackageName
        {
            get { return _packagename; }
            set { SetProperty(ref _packagename, value); }
        }

        private int _order = 0;
        public int Order
        {
            get { return _order; }
            set { SetProperty(ref _order, value); }
        } 

        private string _imagename;
        public string Imagename
        {
            get { return _imagename; }
            set { SetProperty(ref _imagename, value); }
        }

        private string _rootname;
        public string RootName
        {
            get { return _rootname; }
            set { SetProperty(ref _rootname, value); }
        }

        private bool _localdb = false;
        public bool LocalDB
        {
            get { return _localdb; }
            set { SetProperty(ref _localdb, value); }
        } 

        private bool _inmemory = false;
        public bool InMemory
        {
            get { return _inmemory; }
            set { SetProperty(ref _inmemory, value); }
        }

        private bool _isdatasource = false;
        public bool IsDataSource
        {
            get { return _isdatasource; }
            set { SetProperty(ref _isdatasource, value); }
        } 

        private Type _type;
        public Type type
        {
            get { return _type; }
            set { SetProperty(ref _type, value); }
        }

        private string _componenttype;
        public string componentType
        {
            get { return _componenttype; }
            set { SetProperty(ref _componenttype, value); }
        }

        private AddinAttribute _classproperties = new AddinAttribute();
        public AddinAttribute classProperties
        {
            get { return _classproperties; }
            set { SetProperty(ref _classproperties, value); }
        }

        private AddinVisSchema _visschema = new AddinVisSchema();
        public AddinVisSchema VisSchema
        {
            get { return _visschema; }
            set { SetProperty(ref _visschema, value); }
        }


        private List<MethodsClass> _methods = new List<MethodsClass>();
        public List<MethodsClass> Methods
        {
            get { return _methods; }
            set { SetProperty(ref _methods, value); }
        } 
    }
    public class MethodsClass:Entity
    {
        public MethodsClass()
        {
            GuidID = Guid.NewGuid().ToString();
        }

        private int _id;
        public int ID
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _guidid = Guid.NewGuid().ToString();
        public string GuidID
        {
            get { return _guidid; }
            set { SetProperty(ref _guidid, value); }
        }

        private MethodInfo _info;
        public MethodInfo Info
        {
            get { return _info; }
            set { SetProperty(ref _info, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private string _caption;
        public string Caption
        {
            get { return _caption; }
            set { SetProperty(ref _caption, value); }
        }

        private bool _hidden;
        public bool Hidden
        {
            get { return _hidden; }
            set { SetProperty(ref _hidden, value); }
        }

        private bool _click = false;
        public bool Click
        {
            get { return _click; }
            set { SetProperty(ref _click, value); }
        } 

        private Type _type;
        public Type type
        {
            get { return _type; }
            set { SetProperty(ref _type, value); }
        }

        private bool _doubleclick = false;
        public bool DoubleClick
        {
            get { return _doubleclick; }
            set { SetProperty(ref _doubleclick, value); }
        }

        private string _iconimage = null;
        public string iconimage
        {
            get { return _iconimage; }
            set { SetProperty(ref _iconimage, value); }
        } 

        private EnumPointType _pointtype;
        public EnumPointType PointType
        {
            get { return _pointtype; }
            set { SetProperty(ref _pointtype, value); }
        }

        private string _objecttype = null;
        public string ObjectType
        {
            get { return _objecttype; }
            set { SetProperty(ref _objecttype, value); }
        }

        private string _classtype = null;
        public string ClassType
        {
            get { return _classtype; }
            set { SetProperty(ref _classtype, value); }
        }

        private string _misc = null;
        public string misc
        {
            get { return _misc; }
            set { SetProperty(ref _misc, value); }
        }

        private DatasourceCategory _category = DatasourceCategory.NONE;
        public DatasourceCategory Category
        {
            get { return _category; }
            set { SetProperty(ref _category, value); }
        }

        private DataSourceType _datasourcetype = DataSourceType.NONE;
        public DataSourceType DatasourceType
        {
            get { return _datasourcetype; }
            set { SetProperty(ref _datasourcetype, value); }
        } 

        private ShowinType _showin= ShowinType.Both;
        public ShowinType Showin
        {
            get { return _showin; }
            set { SetProperty(ref _showin, value); }
        } 

        private AddinAttribute _addinattr;
        public AddinAttribute AddinAttr
        {
            get { return _addinattr; }
            set { SetProperty(ref _addinattr, value); }
        }

        private CommandAttribute _commandattr;
        public CommandAttribute CommandAttr
        {
            get { return _commandattr; }
            set { SetProperty(ref _commandattr, value); }
        }
    }


    public class DataSourceConnectionConfig:Entity
    {
        public DataSourceConnectionConfig()
        {
            GuidID = Guid.NewGuid().ToString();
        }

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

        private string _datasourcename;
        public string DataSourceName
        {
            get { return _datasourcename; }
            set { SetProperty(ref _datasourcename, value); }
        }
        public DatasourceCategory datasourceCategory { get; set; }

        private List<string> _connectiondrivers = new List<string>();
        public List<string> ConnectionDrivers
        {
            get { return _connectiondrivers; }
            set { SetProperty(ref _connectiondrivers, value); }
        }

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
