using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.ConfigUtil
{
    public class AssemblyClassDefinition : Entity
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
        private string _assemblyName = string.Empty;
        public string AssemblyName
        {
            get { return _assemblyName; }
            set { SetProperty(ref _assemblyName, value); }
        }
        private string _assemblyVersion = string.Empty;
        public string Version
        {
            get { return _assemblyVersion; }
            set { SetProperty(ref _assemblyVersion, value); }
        }
        private DatasourceCategory _category;
        public DatasourceCategory Category
        {
            get { return _category; }
            set
            {
                SetProperty(ref _category, value);
            }
        }
        private DataSourceType _datasourcetype;
        public DataSourceType DatasourceType
        {
            get { return _datasourcetype; }
            set { SetProperty(ref _datasourcetype, value); }

        }
    }
}
