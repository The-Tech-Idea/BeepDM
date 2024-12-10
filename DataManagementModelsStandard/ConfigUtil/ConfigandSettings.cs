
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
  

}
