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
using TheTechIdea.Beep.Pipelines.Attributes;

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

        private bool _isPipelinePlugin = false;
        public bool IsPipelinePlugin
        {
            get { return _isPipelinePlugin; }
            set { SetProperty(ref _isPipelinePlugin, value); }
        }

        private string _pipelinePluginId = string.Empty;
        public string PipelinePluginId
        {
            get { return _pipelinePluginId; }
            set { SetProperty(ref _pipelinePluginId, value); }
        }

        private PipelinePluginType _pipelinePluginType;
        public PipelinePluginType PipelinePluginType
        {
            get { return _pipelinePluginType; }
            set { SetProperty(ref _pipelinePluginType, value); }
        }

        private string _pipelinePluginAuthor = string.Empty;
        public string PipelinePluginAuthor
        {
            get { return _pipelinePluginAuthor; }
            set { SetProperty(ref _pipelinePluginAuthor, value); }
        }

        // ── Rule properties ──────────────────────────────────────────────────────
        private bool _isRule = false;
        public bool IsRule
        {
            get { return _isRule; }
            set { SetProperty(ref _isRule, value); }
        }

        private string _ruleKey = string.Empty;
        /// <summary>Key from <c>[RuleAttribute(ruleKey)]</c>.</summary>
        public string RuleKey
        {
            get { return _ruleKey; }
            set { SetProperty(ref _ruleKey, value); }
        }

        private bool _isRuleParser = false;
        public bool IsRuleParser
        {
            get { return _isRuleParser; }
            set { SetProperty(ref _isRuleParser, value); }
        }

        private string _ruleParserKey = string.Empty;
        /// <summary>Key from <c>[RuleParserAttribute(parserKey)]</c>.</summary>
        public string RuleParserKey
        {
            get { return _ruleParserKey; }
            set { SetProperty(ref _ruleParserKey, value); }
        }

        // ── File Reader plugin properties ────────────────────────────────────

        private bool _isFileReader = false;
        /// <summary>True when this type implements <c>IFileFormatReader</c>
        /// and is decorated with <c>[FileReaderAttribute]</c>.</summary>
        public bool IsFileReader
        {
            get { return _isFileReader; }
            set { SetProperty(ref _isFileReader, value); }
        }

        private string _fileReaderExtension = string.Empty;
        /// <summary>Primary file extension from <c>[FileReaderAttribute(…, defaultExtension)]</c>,
        /// e.g. "parquet" or "db".</summary>
        public string FileReaderExtension
        {
            get { return _fileReaderExtension; }
            set { SetProperty(ref _fileReaderExtension, value); }
        }

        // ── Default Resolver plugin properties ───────────────────────────────

        private bool _isDefaultResolver = false;
        /// <summary>True when this type implements <c>IDefaultValueResolver</c>
        /// and is decorated with <c>[DefaultResolverAttribute]</c>.</summary>
        public bool IsDefaultResolver
        {
            get { return _isDefaultResolver; }
            set { SetProperty(ref _isDefaultResolver, value); }
        }

        private string _defaultResolverName = string.Empty;
        /// <summary>Unique resolver name from <c>[DefaultResolverAttribute(resolverName, …)]</c>,
        /// e.g. "TenantContextResolver".</summary>
        public string DefaultResolverName
        {
            get { return _defaultResolverName; }
            set { SetProperty(ref _defaultResolverName, value); }
        }
    }
}
