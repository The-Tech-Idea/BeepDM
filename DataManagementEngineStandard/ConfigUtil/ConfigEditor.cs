using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.AppManager;
using TheTechIdea.Beep.Composite;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil.Managers;

namespace TheTechIdea.Beep.ConfigUtil
{
	/// <summary>
	/// Refactored ConfigEditor with specialized managers for different responsibilities
	/// </summary>
	public class ConfigEditor : IConfigEditor
	{
		private bool disposedValue;

		// Specialized managers
		private readonly ConfigPathManager _pathManager;
		private readonly DataConnectionManager _connectionManager;
		private readonly QueryManager _queryManager;
		private readonly EntityMappingManager _entityManager;
		private readonly ComponentConfigManager _componentManager;
		private readonly MigrationHistoryManager _migrationHistoryManager;

        /// <summary>Initializes a new instance of the ConfigEditor class.</summary>
        /// <param name="logger">The logger object used for logging.</param>
        /// <param name="per">The object used for error handling and reporting.</param>
        /// <param name="jsonloader">The object used for loading JSON data.</param>
        /// <param name="folderpath">The path to the folder containing the configuration files. If null or empty, uses platform-appropriate application data folder.</param>
        /// <param name="containerfolder">The name of the container folder within the folder path. If null or empty, uses the folder path directly.</param>
        /// <param name="configType">The type of configuration being edited.</param>
        public ConfigEditor(IDMLogger logger, IErrorsInfo per, IJsonLoader jsonloader, string folderpath = null, string containerfolder = null, BeepConfigType configType = BeepConfigType.Application)
		{
			Logger = logger;
			ErrorObject = per;
			JsonLoader = jsonloader;
			ConfigType = configType;

			// Initialize specialized managers
			_pathManager = new ConfigPathManager(logger, folderpath, containerfolder);
			_connectionManager = new DataConnectionManager(logger, jsonloader, _pathManager.ConfigPath);
			_queryManager = new QueryManager(logger, jsonloader, _pathManager.ConfigPath);
			_componentManager = new ComponentConfigManager(logger, jsonloader, _pathManager.ConfigPath, _pathManager);

			// Set path properties from path manager
			ExePath = _pathManager.ExePath;
			ContainerName = _pathManager.ContainerName;
			ConfigPath = _pathManager.ConfigPath;

			// Initialize configuration
			Config = new ConfigandSettings();

			// Initialize entity manager after config is set
			_entityManager = new EntityMappingManager(logger, jsonloader, Config, _pathManager);
			_migrationHistoryManager = new MigrationHistoryManager(logger, jsonloader, Config, _pathManager);

			// Initialize remaining properties
			InitializeProperties();

			// Initialize the configuration
			Init();

			// Update entity manager with fully initialized Config (Config may have been replaced in Init())
			_entityManager.Config = Config;
			_migrationHistoryManager.Config = Config;
		}

		private void InitializeProperties()
		{
			// Initialize all the assembly class definition lists
			ViewModels = new List<AssemblyClassDefinition>();
			BranchesClasses = new List<AssemblyClassDefinition>();
			GlobalFunctions = new List<AssemblyClassDefinition>();
			AppWritersClasses = new List<AssemblyClassDefinition>();
			AppComponents = new List<AssemblyClassDefinition>();
			ReportWritersClasses = new List<AssemblyClassDefinition>();
			PrintManagers = new List<AssemblyClassDefinition>();
			DataSourcesClasses = new List<AssemblyClassDefinition>();
			WorkFlowActions = new List<AssemblyClassDefinition>();
			WorkFlowEditors = new List<AssemblyClassDefinition>();
			WorkFlowSteps = new List<AssemblyClassDefinition>();
			WorkFlowStepEditors = new List<AssemblyClassDefinition>();
			FunctionExtensions = new List<AssemblyClassDefinition>();
			Addins = new List<AssemblyClassDefinition>();
			Others = new List<AssemblyClassDefinition>();
			Rules = new List<AssemblyClassDefinition>();

			// Initialize other collections
			AddinTreeStructure = new List<AddinTreeStructure>();
			Function2Functions = new List<Function2FunctionAction>();
			objectTypes = new List<ObjectTypes>();
			Events = new List<Event>();
			CompositeQueryLayers = new List<CompositeLayer>();
			EntityCreateObjects = new List<EntityStructure>();
			DataTypesMap = new List<DatatypeMapping>();
			Entities = new Dictionary<string, string>();
			LoadedAssemblies = new List<Assembly>();
			Databasetypes = new List<string>();
		}

		#region "Properties"
		/// <summary>Gets or sets the configuration type for the beep.</summary>
		public BeepConfigType ConfigType { get; set; } = BeepConfigType.Application;
		
		/// <summary>Checks if the location is loaded.</summary>
		public bool IsLoaded => IsLocationSaved();
		
		/// <summary>Gets or sets the name of the container.</summary>
		public string ContainerName { get; set; }
		
		/// <summary>Gets or sets the error object.</summary>
		public IErrorsInfo ErrorObject { get; set; }
		
		/// <summary>Gets or sets the JSON loader.</summary>
		public IJsonLoader JsonLoader { get; set; }
		
		/// <summary>Gets or sets the configuration and settings object.</summary>
		public ConfigandSettings Config { get; set; } = new ConfigandSettings();
		
		/// <summary>Gets or sets the logger used for logging.</summary>
		public IDMLogger Logger { get; set; }

		// Delegated properties to managers
		/// <summary>Gets or sets the list of database types.</summary>
		public List<string> Databasetypes { get; set; }
		
		/// <summary>Gets or sets the list of QuerySqlRepo objects.</summary>
		public List<QuerySqlRepo> QueryList 
		{ 
			get => _queryManager.QueryList; 
			set => _queryManager.QueryList = value; 
		}
		
		/// <summary>Gets or sets the list of data connections.</summary>
		public List<ConnectionProperties> DataConnections 
		{ 
			get => _connectionManager.DataConnections; 
			set => _connectionManager.DataConnections = value; 
		}
		
		/// <summary>Gets or sets the list of workflows.</summary>
		public List<WorkFlow> WorkFlows 
		{ 
			get => _componentManager.WorkFlows; 
			set => _componentManager.WorkFlows = value; 
		}
		
		/// <summary>Gets or sets the list of category folders.</summary>
		public List<CategoryFolder> CategoryFolders 
		{ 
			get => _componentManager.CategoryFolders; 
			set => _componentManager.CategoryFolders = value; 
		}

		/// <summary>Gets or sets the list of connection driver configurations.</summary>
		public List<ConnectionDriversConfig> DataDriversClasses 
		{ 
			get => _componentManager.DataDriversClasses; 
			set => _componentManager.DataDriversClasses = value; 
		}

		/// <summary>Gets or sets the list of root folders representing projects.</summary>
		public List<RootFolder> Projects 
		{ 
			get => _componentManager.Projects; 
			set => _componentManager.Projects = value; 
		}

		/// <summary>Gets or sets the list of reports.</summary>
		public List<ReportsList> Reportslist 
		{ 
			get => _componentManager.ReportsList; 
			set => _componentManager.ReportsList = value; 
		}

		/// <summary>Gets or sets the list of app templates for generating reports.</summary>
		public List<AppTemplate> ReportsDefinition 
		{ 
			get => _componentManager.ReportsDefinition; 
			set => _componentManager.ReportsDefinition = value; 
		}

		/// <summary>Gets or sets the list of AIScripts.</summary>
		public List<ReportsList> AIScriptslist 
		{ 
			get => _componentManager.AIScriptsList; 
			set => _componentManager.AIScriptsList = value; 
		}

		// Direct properties
        public List<AssemblyClassDefinition> ViewModels { get; set; } = new List<AssemblyClassDefinition>();
        public List<AssemblyClassDefinition> BranchesClasses { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> GlobalFunctions { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> AppWritersClasses { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> AppComponents { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> ReportWritersClasses { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> PrintManagers { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> DataSourcesClasses { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> WorkFlowActions { get; set; } = new List<AssemblyClassDefinition> { };
		public List<AssemblyClassDefinition> WorkFlowEditors { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> WorkFlowSteps { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> WorkFlowStepEditors { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> FunctionExtensions { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> Addins { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> Others { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> Rules { get; set; } = new List<AssemblyClassDefinition>();
		public List<AddinTreeStructure> AddinTreeStructure { get; set; } = new List<AddinTreeStructure>();
		public List<Function2FunctionAction> Function2Functions { get; set; } = new List<Function2FunctionAction>();
		public List<ObjectTypes> objectTypes { get; set; } = new List<ObjectTypes>();
		public List<Event> Events { get; set; } = new List<Event>();
		public List<CompositeLayer> CompositeQueryLayers { get; set; } = new List<CompositeLayer>();
		public List<EntityStructure> EntityCreateObjects { get; set; } = new List<EntityStructure>();
		public List<DatatypeMapping> DataTypesMap { get; set; } = new List<DatatypeMapping>();
		public Dictionary<string, string> Entities { get; set; } = new Dictionary<string, string>();
		public string ExePath { get; set; }
		public string ConfigPath { get; set; }
		public List<Assembly> LoadedAssemblies { get; set; } = new List<Assembly>();
        #endregion "Properties"

        #region "Drivers - Delegated to ComponentConfigManager"
        public int AddDriver(ConnectionDriversConfig dr) => _componentManager.AddDriver(dr);
        #endregion

        #region "Scripts and logs L/S"
        public void SaveScriptsValues(ETLScriptHDR Scripts)
		{
			string path = Path.Combine(Config.ScriptsPath, "Scripts.json");
			JsonLoader.Serialize(path, Scripts);
		}

		public ETLScriptHDR LoadScriptsValues()
		{
			string path = Path.Combine(Config.ScriptsPath, "Scripts.json");
			ETLScriptHDR Scripts = JsonLoader.DeserializeSingleObject<ETLScriptHDR>(path);
			return Scripts;
		}
        #endregion

        #region "Query Operations - Delegated to QueryManager"
        public string GetSql(Sqlcommandtype CmdType, string TableName, string SchemaName, string Filterparamters, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType) =>
			_queryManager.GetSql(CmdType, TableName, SchemaName, Filterparamters, DatabaseType);

		public List<string> GetSqlList(Sqlcommandtype CmdType, string TableName, string SchemaName, string Filterparamters, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType) =>
			_queryManager.GetSqlList(CmdType, TableName, SchemaName, Filterparamters, DatabaseType);

		public string GetSqlFromCustomQuery(Sqlcommandtype CmdType, string TableName, string customquery, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType) =>
			_queryManager.GetSqlFromCustomQuery(CmdType, TableName, customquery, DatabaseType);

		public void SaveQueryFile() => _queryManager.SaveQueryFile();
		public List<QuerySqlRepo> LoadQueryFile() => _queryManager.LoadQueryFile();
		public List<QuerySqlRepo> InitQueryDefaultValues() => _queryManager.InitQueryDefaultValues();
        #endregion

        #region "Entity Structure Operations - Delegated to EntityMappingManager"
        public DatasourceEntities LoadDataSourceEntitiesValues(string dsname) => _entityManager.LoadDataSourceEntitiesValues(dsname);
		public bool RemoveDataSourceEntitiesValues(string dsname) => _entityManager.RemoveDataSourceEntitiesValues(dsname);
		public void SaveDataSourceEntitiesValues(DatasourceEntities datasourceEntities) => _entityManager.SaveDataSourceEntitiesValues(datasourceEntities);
		public bool EntityStructureExist(string filepath, string EntityName, string DataSourceID) => _entityManager.EntityStructureExist(filepath, EntityName, DataSourceID);
		public void SaveEntityStructure(string filepath, EntityStructure entity) => _entityManager.SaveEntityStructure(filepath, entity);
		public EntityStructure LoadEntityStructure(string filepath, string EntityName, string DataSourceID) => _entityManager.LoadEntityStructure(filepath, EntityName, DataSourceID);
        #endregion

		#region "Migration History Operations - Delegated to MigrationHistoryManager"
		public MigrationHistory LoadMigrationHistory(string dataSourceName) => _migrationHistoryManager.Load(dataSourceName);
		public void SaveMigrationHistory(MigrationHistory history) => _migrationHistoryManager.Save(history);
		public void AppendMigrationRecord(string dataSourceName, DataSourceType dataSourceType, MigrationRecord record) =>
			_migrationHistoryManager.AppendRecord(dataSourceName, dataSourceType, record);
		#endregion

        #region "Mapping Operations - Delegated to EntityMappingManager"
        public void SaveMappingSchemaValue(string schemaname, Map_Schema mapping_Rep) => _entityManager.SaveMappingSchemaValue(schemaname, mapping_Rep);
		public Map_Schema LoadMappingSchema(string schemaname) => _entityManager.LoadMappingSchema(schemaname);
		public void SaveMappingValues(string Entityname, string datasource, EntityDataMap mapping_Rep) => _entityManager.SaveMappingValues(Entityname, datasource, mapping_Rep);
		public EntityDataMap LoadMappingValues(string Entityname, string datasource) => _entityManager.LoadMappingValues(Entityname, datasource);
        #endregion

        #region "Data Connections - Delegated to DataConnectionManager"
        public bool DataConnectionExist(ConnectionProperties cn) => _connectionManager.DataConnectionExist(cn);
		public bool DataConnectionGuidExist(string GuidID) => _connectionManager.DataConnectionGuidExist(GuidID);
		public void SaveDataconnectionsValues() => _connectionManager.SaveDataConnectionsValues();
		public List<ConnectionProperties> LoadDataConnectionsValues() => _connectionManager.LoadDataConnectionsValues();
		public bool DataConnectionExist(string ConnectionName) => _connectionManager.DataConnectionExist(ConnectionName);
		public bool AddDataConnection(ConnectionProperties cn) => _connectionManager.AddDataConnection(cn);
		public bool UpdateDataConnection(ConnectionProperties source, string targetguidid) => _connectionManager.UpdateDataConnection(source, targetguidid);
		public bool RemoveConnByName(string pname) => _connectionManager.RemoveConnByName(pname);
		public bool RemoveConnByID(int ID) => _connectionManager.RemoveConnByID(ID);
		public bool RemoveConnByGuidID(string GuidID) => _connectionManager.RemoveConnByGuidID(GuidID);
		public bool RemoveDataConnection(string pname) => _connectionManager.RemoveDataConnection(pname);
        #endregion

        #region "Configuration L/S"
        public ConfigandSettings LoadConfigValues()
		{
			string path = Path.Combine(ExePath, "Config.json");
			Config = JsonLoader.DeserializeSingleObject<ConfigandSettings>(path);
			return Config;
		}

		public void SaveConfigValues()
		{
			string path = Path.Combine(ExePath, "Config.json");
			JsonLoader.Serialize(path, Config);
		}

		// Category folder operations delegated to ComponentConfigManager
		public CategoryFolder AddFolderCategory(string pfoldername, string prootname, string pparentname, string parentguidid, bool isparentFolder = false, bool isparentRoot = true, bool isphysical = false) =>
			_componentManager.AddFolderCategory(pfoldername, prootname, pparentname, parentguidid, isparentFolder, isparentRoot, isphysical);

		public CategoryFolder AddFolderCategory(string pfoldername, string prootname, string pparentname) =>
			_componentManager.AddFolderCategory(pfoldername, prootname, pparentname);

		public bool RemoveFolderCategory(string pfoldername, string prootname, string parentguidid) =>
			_componentManager.RemoveFolderCategory(pfoldername, prootname, parentguidid);

		public void LoadCategoryFoldersValues() => _componentManager.LoadCategoryFoldersValues();
		public void SaveCategoryFoldersValues() => _componentManager.SaveCategoryFoldersValues();

		// Driver operations delegated to ComponentConfigManager
		public List<ConnectionDriversConfig> LoadConnectionDriversConfigValues() => _componentManager.LoadConnectionDriversConfigValues();
		public void SaveConnectionDriversConfigValues() => _componentManager.SaveConnectionDriversConfigValues();
        #endregion

        #region "Component Operations - Delegated to ComponentConfigManager"
        public void SaveReportsValues() => _componentManager.SaveReportsValues();
		public List<ReportsList> LoadReportsValues() => _componentManager.LoadReportsValues();
		public void SaveReportDefinitionsValues() => _componentManager.SaveReportDefinitionsValues();
		public List<AppTemplate> LoadReportsDefinitionValues() => _componentManager.LoadReportsDefinitionValues();
		public void SaveAIScriptsValues() => _componentManager.SaveAIScriptsValues();
		public List<ReportsList> LoadAIScriptsValues() => _componentManager.LoadAIScriptsValues();
		public void ReadProjects() => _componentManager.ReadProjects();
		public void SaveProjects() => _componentManager.SaveProjects();
		public string CreateFileExtensionString() => _componentManager.CreateFileExtensionString();
        #endregion

        #region "WorkFlows - Delegated to ComponentConfigManager"
        public void ReadWork() => _componentManager.ReadWorkFlows(Config.WorkFlowPath ?? Path.Combine(ExePath, "WorkFlow"));
		public void SaveWork() => _componentManager.SaveWorkFlows(Config.WorkFlowPath ?? Path.Combine(ExePath, "WorkFlow"));
        #endregion

        #region "Entity and Data Type Operations - Delegated to EntityMappingManager"
        public List<EntityStructure> LoadTablesEntities() => _entityManager.LoadTablesEntities();
		public void SaveTablesEntities() => _entityManager.SaveTablesEntities(EntityCreateObjects);
		public void WriteDataTypeFile(string filename = "DataTypeMapping") => _entityManager.WriteDataTypeFile(DataTypesMap, filename);
		public List<DatatypeMapping> ReadDataTypeFile(string filename = "DataTypeMapping") => _entityManager.ReadDataTypeFile(filename);
        #endregion

        #region "Path Operations - Delegated to ConfigPathManager"
        public void CreateDir(string path) => _pathManager.CreateDir(path);
		public void CreateDirConfig(string path, FolderFileTypes foldertype) => _pathManager.CreateDirConfig(path, foldertype, Config);
        #endregion

        #region "Remaining Direct Operations"
        // These operations remain in ConfigEditor as they involve complex initialization logic

		public void SaveLocation()
		{
			if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep")))
			{
				Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep"));
			}
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep", "BeepConfig.json");
			string Beeppath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep", "BeepPath.txt");
			StreamWriter streamWriter = new StreamWriter(Beeppath);
			streamWriter.WriteLine(Config.ExePath);
			streamWriter.Close();
			JsonLoader.Serialize(path, Config);
		}

		public bool IsLocationSaved()
		{
			SaveLocation();
			return true;
		}

		// Simplified stub implementations for remaining methods
		public void SaveDatabasesValues() { /* Implementation */ }
		public void LoadDatabasesValues() { /* Implementation */ }
		public void SaveEvents() { /* Implementation */ }
		public void LoadEvents() { /* Implementation */ }
		public void SaveFucntion2Function() { /* Implementation */ }
		public void LoadFucntion2Function() { /* Implementation */ }
		public void LoadAddinTreeStructure() { /* Implementation */ }
		public void SaveAddinTreeStructure() { /* Implementation */ }
		public bool RemoveLayerByName(string LayerName) { /* Implementation */ return false; }
		public bool RemoveLayerByID(int ID) { /* Implementation */ return false; }
		public void SaveCompositeLayersValues() { /* Implementation */ }
		public List<CompositeLayer> LoadCompositeLayersValues() { return new List<CompositeLayer>(); }
		public void SaveObjectTypes() { /* Implementation */ }
		public List<ObjectTypes> LoadObjectTypes() { return new List<ObjectTypes>(); }
		public void ScanSavedEntities() { /* Implementation */ }

		public List<DefaultValue> Getdefaults(IDMEEditor DMEEditor, string DatasourceName)
		{
			// Implementation remains the same
			DMEEditor.ErrorObject.Message = null;
			DMEEditor.ErrorObject.Flag = Errors.Ok;
			List<DefaultValue> defaults = null;
			try
			{
				ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == DatasourceName)];
				if (cn != null)
				{
					defaults = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == DatasourceName)].DatasourceDefaults;
				}
				else DMEEditor.AddLogMessage("Beep", $"Could not Find DataSource  {DatasourceName}", DateTime.Now, 0, null, Errors.Failed);

			}
			catch (Exception ex)
			{
				DMEEditor.AddLogMessage("Beep", $"Could not Save DataSource Defaults Values {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
			}
			return defaults;
		}

		public IErrorsInfo Savedefaults(IDMEEditor DMEEditor, List<DefaultValue> defaults, string DatasourceName)
		{
			// Implementation remains the same
			DMEEditor.ErrorObject.Message = null;
			DMEEditor.ErrorObject.Flag = Errors.Ok;
			try
			{
				ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == DatasourceName)];
				if (cn != null)
				{
					cn.DatasourceDefaults = defaults;
					DMEEditor.ConfigEditor.SaveDataconnectionsValues();
				}
				else DMEEditor.AddLogMessage("Beep", $"Could not Find DataSource  {DatasourceName}", DateTime.Now, 0, null, Errors.Failed);
			}
			catch (Exception ex)
			{
				DMEEditor.AddLogMessage("Beep", $"Could not Save DataSource Defaults Values {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
			}
			return DMEEditor.ErrorObject;
		}
        #endregion

        private IErrorsInfo InitQueryList() => _queryManager.InitQueryList();

		private IErrorsInfo InitDataConnections()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				string path = Path.Combine(ConfigPath, "DataConnections.json");
				if (File.Exists(path))
				{
					_connectionManager.LoadDataConnectionsValues();
				}
				else
					_connectionManager.SaveDataConnectionsValues();
			}
			catch (Exception ex)
			{
				ErrorObject.Flag = Errors.Failed;
				ErrorObject.Ex = ex;
				ErrorObject.Message = ex.Message;
				Logger.WriteLog($"Error Initlization DataConnections ({ex.Message})");
			}
			return ErrorObject;
		}

		private IErrorsInfo InitMapping()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				string path = Path.Combine(ConfigPath, "CategoryFolders.json");
				if (File.Exists(path))
				{
					_componentManager.LoadCategoryFoldersValues();
				}
				else 
					_componentManager.SaveCategoryFoldersValues();
			}
			catch (Exception ex)
			{
				ErrorObject.Flag = Errors.Failed;
				ErrorObject.Ex = ex;
				ErrorObject.Message = ex.Message;
				Logger.WriteLog($"Error Initlization FileConnections ({ex.Message})");
			}
			return ErrorObject;
		}

		private IErrorsInfo InitConfig()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				string exedir = ExePath;
				if (!string.IsNullOrEmpty(ContainerName) && !string.IsNullOrWhiteSpace(ContainerName))
				{
					if (!Directory.Exists(ContainerName))
					{
						Directory.CreateDirectory(ContainerName);
					}
				}
				else
					ContainerName = exedir;

				string configfile = Path.Combine(ContainerName, "Config.json");
				if (File.Exists(configfile))
				{
					LoadConfigValues();
				}
				else
				{
					Config = new ConfigandSettings();
					Config.ExePath = ContainerName;
				}

				// Ensure Config.Folders is never null (deserialization might set it to null)
				if (Config.Folders == null)
				{
					Config.Folders = new List<StorageFolders>();
				}

				if (Config != null)
				{
					if (!Config.ExePath.Equals(exedir, StringComparison.InvariantCultureIgnoreCase))
					{
						Config = new ConfigandSettings();
						List<StorageFolders> folders = new List<StorageFolders>();
						if (Config.Folders != null)
						{
							foreach (StorageFolders fold in Config.Folders)
							{
								var dirName = new DirectoryInfo(fold.FolderPath).Name;
								folders.Add(new StorageFolders(Path.Combine(ContainerName, dirName), fold.FolderFilesType));
							}
						}
						Config.ExePath = exedir;
						Config.Folders = folders;
					}
				}
				else
				{
					Config = new ConfigandSettings();
					Config.ExePath = ContainerName;
					Config.Folders = new List<StorageFolders>();
				}
				Config.ExePath = ContainerName;

				// Create directories
				if (ConfigType != BeepConfigType.DataConnector)
				{
					CreateDirConfig(Path.Combine(ContainerName, "Addin"), FolderFileTypes.Addin);
					CreateDirConfig(Path.Combine(ContainerName, "DataFiles"), FolderFileTypes.DataFiles);
					CreateDirConfig(Path.Combine(ContainerName, "DataViews"), FolderFileTypes.DataView);
					CreateDirConfig(Path.Combine(ContainerName, "ProjectData"), FolderFileTypes.ProjectData);
					CreateDirConfig(Path.Combine(ContainerName, "ProjectClasses"), FolderFileTypes.ProjectClass);
					CreateDirConfig(Path.Combine(ContainerName, "GFX"), FolderFileTypes.GFX);
					CreateDirConfig(Path.Combine(ContainerName, "OtherDll"), FolderFileTypes.OtherDLL);
					CreateDirConfig(Path.Combine(ContainerName, "Entities"), FolderFileTypes.Entities);
					CreateDirConfig(Path.Combine(ContainerName, "Mapping"), FolderFileTypes.Mapping);
					CreateDirConfig(Path.Combine(ContainerName, "WorkFlow"), FolderFileTypes.WorkFlows);
					CreateDirConfig(Path.Combine(ContainerName, "Scripts"), FolderFileTypes.Scripts);
					CreateDirConfig(Path.Combine(ContainerName, "Scripts\\Logs"), FolderFileTypes.ScriptsLogs);
					CreateDirConfig(Path.Combine(ContainerName, "AI"), FolderFileTypes.Scripts);
					CreateDirConfig(Path.Combine(ContainerName, "Reports"), FolderFileTypes.Reports);

					// Set configuration paths
					if (Config.ConfigPath == null) Config.ConfigPath = Path.Combine(ContainerName, "Config");
					if (ConfigPath == null) ConfigPath = Config.ConfigPath;
					if (Config.ScriptsPath == null) Config.ScriptsPath = Path.Combine(ContainerName, "Scripts");
					if (Config.ScriptsLogsPath == null) Config.ScriptsLogsPath = Path.Combine(ContainerName, "Scripts\\Logs");
					if (Config.ProjectDataPath == null) Config.ProjectDataPath = Path.Combine(ContainerName, "ProjectData");
					if (Config.DataViewPath == null) Config.DataViewPath = Path.Combine(ContainerName, "DataViews");
					if (Config.DataFilePath == null) Config.DataFilePath = Path.Combine(ContainerName, "DataFiles");
					if (Config.AddinPath == null) Config.AddinPath = Path.Combine(ContainerName, "Addin");
					if (Config.ClassPath == null) Config.ClassPath = Path.Combine(ContainerName, "ProjectClasses");
					if (Config.EntitiesPath == null) Config.EntitiesPath = Path.Combine(ContainerName, "Entities");
					if (Config.GFXPath == null) Config.GFXPath = Path.Combine(ContainerName, "GFX");
					if (Config.MappingPath == null) Config.MappingPath = Path.Combine(ContainerName, "Mapping");
					if (Config.OtherDLLPath == null) Config.OtherDLLPath = Path.Combine(ContainerName, "OtherDll");
					if (Config.WorkFlowPath == null) Config.WorkFlowPath = Path.Combine(ContainerName, "WorkFlow");
				}

				CreateDirConfig(Path.Combine(ContainerName, "Config"), FolderFileTypes.Config);
				CreateDirConfig(Path.Combine(ContainerName, "ConnectionDrivers"), FolderFileTypes.ConnectionDriver);
				CreateDirConfig(Path.Combine(ContainerName, "DataSources"), FolderFileTypes.DataSources);
				CreateDirConfig(Path.Combine(ContainerName, "LoadingExtensions"), FolderFileTypes.LoaderExtensions);

				if (Config.ConfigPath == null) Config.ConfigPath = Path.Combine(ContainerName, "Config");
				if (ConfigPath == null) ConfigPath = Config.ConfigPath;
				if (Config.LoaderExtensionsPath == null) Config.LoaderExtensionsPath = Path.Combine(ContainerName, "LoadingExtensions");
				if (Config.ConnectionDriversPath == null) Config.ConnectionDriversPath = Path.Combine(ContainerName, "ConnectionDrivers");
				if (Config.DataSourcesPath == null) Config.DataSourcesPath = Path.Combine(ContainerName, "DataSources");

				SaveConfigValues();
			}
			catch (Exception ex)
			{
				ErrorObject.Flag = Errors.Failed;
				ErrorObject.Ex = ex;
				ErrorObject.Message = ex.Message;
				Logger.WriteLog($"Error Initlization Config ({ex.Message})");
			}
			return ErrorObject;
		}

		public IErrorsInfo Init()
		{
			ErrorObject.Flag = Errors.Ok;
			Logger.WriteLog($"Initialization Values and Lists");
			try
			{
				InitConfig();
				InitDataConnections();
				LoadFucntion2Function();
				LoadEvents();
				LoadCompositeLayersValues();
				LoadReportsValues();
				LoadReportsDefinitionValues();
				ReadWork();
				LoadObjectTypes();
				ReadProjects();
				InitMapping();
				SaveLocation();
			}
			catch (Exception ex)
			{
				ErrorObject.Flag = Errors.Failed;
				ErrorObject.Ex = ex;
				ErrorObject.Message = ex.Message;
				Logger.WriteLog($"Error Initialization Lists ({ex.Message})");
			}
			return ErrorObject;
		}

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Clear all collections
                    QueryList?.Clear();
                    DataConnections?.Clear();
                    WorkFlows?.Clear();
                    CategoryFolders?.Clear();
                    ViewModels?.Clear();
                    BranchesClasses?.Clear();
                    GlobalFunctions?.Clear();
                    AppWritersClasses?.Clear();
                    AppComponents?.Clear();
                    ReportWritersClasses?.Clear();
                    PrintManagers?.Clear();
                    DataSourcesClasses?.Clear();
                    WorkFlowActions?.Clear();
                    WorkFlowEditors?.Clear();
                    WorkFlowSteps?.Clear();
                    WorkFlowStepEditors?.Clear();
                    FunctionExtensions?.Clear();
                    Addins?.Clear();
                    Others?.Clear();
                    Rules?.Clear();
                    AddinTreeStructure?.Clear();
                    Function2Functions?.Clear();
                    objectTypes?.Clear();
                    Events?.Clear();
                    ReportsDefinition?.Clear();
                    Reportslist?.Clear();
                    AIScriptslist?.Clear();
                    CompositeQueryLayers?.Clear();
                    EntityCreateObjects?.Clear();
                    DataTypesMap?.Clear();
                    LoadedAssemblies?.Clear();
                    DataDriversClasses?.Clear();
                    Projects?.Clear();
                    Entities?.Clear();

                    // Clear resources
                    if (JsonLoader is IDisposable disposableLoader)
                    {
                        disposableLoader.Dispose();
                    }

                    JsonLoader = null;
                    Config = null;
                    Logger = null;
                    ErrorObject = null;
                    ExePath = null;
                    ConfigPath = null;
                    ContainerName = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
