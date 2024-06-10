using DataManagementModels.ConfigUtil;
using DataManagementModels.DriversConfigurations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.AppManager;
using TheTechIdea.Beep.CompositeLayer;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Logger;

namespace TheTechIdea.Util
{
	public class ConfigEditor : IConfigEditor
	{
		private bool disposedValue;
		/// <summary>Initializes a new instance of the ConfigEditor class.</summary>
		/// <param name="logger">The logger object used for logging.</param>
		/// <param name="per">The object used for error handling and reporting.</param>
		/// <param name="jsonloader">The object used for loading JSON data.</param>
		/// <param name="folderpath">The path to the folder containing the configuration files. If null or empty, the folder path will be set to the directory of the entry assembly.</param>
		/// <param name="containerfolder">The name of the container folder within the folder path. If null or empty, the container folder will be the same as the folder path.</param>
		/// <param name
		public ConfigEditor(IDMLogger logger, IErrorsInfo per, IJsonLoader jsonloader, string folderpath = null, string containerfolder = null, BeepConfigType configType = BeepConfigType.Application)
		{
			Logger = logger;
			ErrorObject = per;
			JsonLoader = jsonloader;
			if (!string.IsNullOrEmpty(folderpath))
			{
				ExePath = folderpath;// Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), folderpath);
			}
			else
				ExePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
			if (!string.IsNullOrEmpty(containerfolder))
			{
				ExePath = Path.Combine(ExePath, containerfolder);
			}
			ContainerName = ExePath;
			ConfigType = configType;
			Init();
		}
		#region "Properties"
		/// <summary>Gets or sets the configuration type for the beep.</summary>
		/// <value>The configuration type for the beep.</value>
		public BeepConfigType ConfigType { get; set; } = BeepConfigType.Application;
		/// <summary>Checks if the location is loaded.</summary>
		/// <returns>True if the location is loaded, false otherwise.</returns>
		public bool IsLoaded => IsLocationSaved();
		/// <summary>Gets or sets the name of the container.</summary>
		/// <value>The name of the container.</value>
		public string ContainerName { get; set; }
		/// <summary>Gets or sets the error object.</summary>
		/// <value>The error object.</value>
		public IErrorsInfo ErrorObject { get; set; }
		/// <summary>Gets or sets the JSON loader.</summary>
		/// <value>The JSON loader.</value>
		public IJsonLoader JsonLoader { get; set; }
		/// <summary>Gets or sets the configuration and settings object.</summary>
		/// <value>The configuration and settings object.</value>
		public ConfigandSettings Config { get; set; } = new ConfigandSettings();
		/// <summary>Gets or sets the logger used for logging.</summary>
		/// <value>The logger.</value>
		public IDMLogger Logger { get; set; }
		/// <summary>Gets or sets the list of database types.</summary>
		/// <value>The list of database types.</value>
		public List<string> Databasetypes { get; set; }
		/// <summary>Gets or sets the list of QuerySqlRepo objects.</summary>
		/// <value>The list of QuerySqlRepo objects.</value>
		public List<QuerySqlRepo> QueryList { get; set; } = new List<QuerySqlRepo>();
		/// <summary>Gets or sets the list of driver definitions configuration.</summary>
		/// <value>The list of driver definitions configuration.</value>
		//public List<ConnectionDriversConfig> DriverDefinitionsConfig { get; set; } = new List<ConnectionDriversConfig>();
		/// <summary>Gets or sets the list of data connections.</summary>
		/// <value>The list of data connections.</value>
		public List<ConnectionProperties> DataConnections { get; set; } = new List<ConnectionProperties>(); //DataSourceConnectionConfig
		/// <summary>Gets or sets the list of workflows.</summary>
		/// <value>The list of workflows.</value>
		public List<WorkFlow> WorkFlows { get; set; } = new List<WorkFlow>();
		/// <summary>Gets or sets the list of category folders.</summary>
		/// <value>The list of category folders.</value>
		public List<CategoryFolder> CategoryFolders { get; set; } = new List<CategoryFolder>();
		/// <summary>Gets or sets the list of assembly class definitions for the branches.</summary>
		/// <value>The list of assembly class definitions for the branches.</value>
		public List<AssemblyClassDefinition> BranchesClasses { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of global functions.</summary>
		/// <value>The list of global functions.</value>
		public List<AssemblyClassDefinition> GlobalFunctions { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of AssemblyClassDefinition objects representing the classes used for writing in the application.</summary>
		/// <value>The list of AssemblyClassDefinition objects.</value>
		public List<AssemblyClassDefinition> AppWritersClasses { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of application components.</summary>
		/// <value>The list of application components.</value>
		public List<AssemblyClassDefinition> AppComponents { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of assembly class definitions for report writers.</summary>
		/// <value>The list of assembly class definitions for report writers.</value>
		public List<AssemblyClassDefinition> ReportWritersClasses { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of assembly class definitions for print managers.</summary>
		/// <value>The list of assembly class definitions for print managers.</value>
		public List<AssemblyClassDefinition> PrintManagers { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of data source classes.</summary>
		/// <value>The list of data source classes.</value>
		public List<AssemblyClassDefinition> DataSourcesClasses { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of workflow actions.</summary>
		/// <value>The list of workflow actions.</value>
		public List<AssemblyClassDefinition> WorkFlowActions { get; set; } = new List<AssemblyClassDefinition> { };
		/// <summary>Gets or sets the list of assembly class definitions for workflow editors.</summary>
		/// <value>The list of assembly class definitions for workflow editors.</value>
		public List<AssemblyClassDefinition> WorkFlowEditors { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of workflow steps.</summary>
		/// <value>The list of workflow steps.</value>
		public List<AssemblyClassDefinition> WorkFlowSteps { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of assembly class definitions for workflow step editors.</summary>
		/// <value>The list of assembly class definitions.</value>
		public List<AssemblyClassDefinition> WorkFlowStepEditors { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of function extensions.</summary>
		/// <value>The list of function extensions.</value>
		public List<AssemblyClassDefinition> FunctionExtensions { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of assembly class definitions representing add-ins.</summary>
		/// <value>The list of assembly class definitions representing add-ins.</value>
		public List<AssemblyClassDefinition> Addins { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets a list of AssemblyClassDefinition objects.</summary>
		/// <value>The list of AssemblyClassDefinition objects.</value>
		public List<AssemblyClassDefinition> Others { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of assembly class definitions.</summary>
		/// <value>The list of assembly class definitions.</value>
		public List<AssemblyClassDefinition> Rules { get; set; } = new List<AssemblyClassDefinition>();
		/// <summary>Gets or sets the list of add-in tree structures.</summary>
		/// <value>The list of add-in tree structures.</value>
		public List<AddinTreeStructure> AddinTreeStructure { get; set; } = new List<AddinTreeStructure>();
		/// <summary>Gets or sets a list of Function2FunctionAction objects.</summary>
		/// <value>The list of Function2FunctionAction objects.</value>
		public List<Function2FunctionAction> Function2Functions { get; set; } = new List<Function2FunctionAction>();
		/// <summary>Gets or sets the list of object types.</summary>
		/// <value>The list of object types.</value>
		public List<ObjectTypes> objectTypes { get; set; } = new List<ObjectTypes>();
		/// <summary>Gets or sets the list of events.</summary>
		/// <value>The list of events.</value>
		public List<Event> Events { get; set; } = new List<Event>();
		/// <summary>Gets or sets the list of app templates for generating reports.</summary>
		/// <value>The list of app templates.</value>
		public List<AppTemplate> ReportsDefinition { get; set; } = new List<AppTemplate>();
		/// <summary>Gets or sets the list of reports.</summary>
		/// <value>The list of reports.</value>
		public List<ReportsList> Reportslist { get; set; } = new List<ReportsList>();
		/// <summary>Gets or sets the list of AIScripts.</summary>
		/// <value>The list of AIScripts.</value>
		public List<ReportsList> AIScriptslist { get; set; } = new List<ReportsList>();
		/// <summary>Gets or sets the list of composite query layers.</summary>
		/// <value>The list of composite query layers.</value>
		public List<CompositeLayer> CompositeQueryLayers { get; set; } = new List<CompositeLayer>();
		/// <summary>Gets or sets the list of entity structures used for creating objects.</summary>
		/// <value>The list of entity structures.</value>
		public List<EntityStructure> EntityCreateObjects { get; set; } = new List<EntityStructure>();
		/// <summary>Gets or sets the list of datatype mappings.</summary>
		/// <value>The list of datatype mappings.</value>
		public List<DatatypeMapping> DataTypesMap { get; set; } = new List<DatatypeMapping>();
		//	public List<DataSourceFieldProperties> AppfieldProperties { get; set; } = new List<DataSourceFieldProperties>();
		/// <summary>Gets or sets a dictionary of entities.</summary>
		/// <value>The dictionary of entities.</value>
		public Dictionary<string, string> Entities { get; set; } = new Dictionary<string, string>();
		/// <summary>Gets or sets the list of connection driver configurations.</summary>
		/// <value>The list of connection driver configurations.</value>
		public List<ConnectionDriversConfig> DataDriversClasses { get; set; } = new List<ConnectionDriversConfig>();
		/// <summary>Gets or sets the list of root folders representing projects.</summary>
		/// <value>The list of root folders representing projects.</value>
		public List<RootFolder> Projects { get; set; } = new List<RootFolder>();
		/// <summary>Gets or sets the path of the executable file.</summary>
		/// <value>The path of the executable file.</value>
		public string ExePath { get; set; } // System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); //System.Reflection.Assembly.GetExecutingAssembly().Location
		/// <summary>Gets or sets the path to the configuration file.</summary>
		/// <value>The path to the configuration file.</value>
		public string ConfigPath { get; set; }
		/// <summary>Gets or sets the list of loaded assemblies.</summary>
		/// <value>The list of loaded assemblies.</value>
		public List<Assembly> LoadedAssemblies { get; set; } = new List<Assembly>();
		#endregion "Properties"
		#region "Drivers"
		/// <summary>Adds a driver to the connection drivers configuration.</summary>
		/// <param name="dr">The driver to add.</param>
		/// <returns>The index at which the driver was added.</returns>
		public int AddDriver(ConnectionDriversConfig dr)
		{
			if (dr == null)
			{
				return -1;
			}
			if (string.IsNullOrEmpty(dr.PackageName))
			{
				return -1;
			}
			ConnectionDriversConfig founddr = null;
			if (DataDriversClasses.Count == 0)
			{
				DataDriversClasses.Add(dr);
				return 0;
			}
			int idx = DataDriversClasses.FindIndex(c => c.PackageName.Equals(dr.PackageName, StringComparison.InvariantCultureIgnoreCase) && c.version == dr.version);
			if (idx >= 0)
			{
				return idx;

			}
			idx = DataDriversClasses.FindIndex(c => c.PackageName.Equals(dr.PackageName, StringComparison.InvariantCultureIgnoreCase));
			if (idx > -1)
			{
				founddr = DataDriversClasses[idx];
				founddr.version = dr.version;
				return idx;
			}
			return -1;
		}
		#endregion "Drivers"
		#region "Scripts and logs L/S"
		/// <summary>Saves the values of ETL scripts to a JSON file.</summary>
		/// <param name="Scripts">The ETLScriptHDR object containing the scripts to be saved.</param>
		/// <remarks>The values of the ETL scripts are serialized and saved to a JSON file located at the specified path.</remarks>
		public void SaveScriptsValues(ETLScriptHDR Scripts)
		{
			string path = Path.Combine(Config.ScriptsPath, "Scripts.json");
			JsonLoader.Serialize(path, Scripts);

		}
		/// <summary>Loads the values of ETL scripts from a JSON file.</summary>
		/// <returns>An instance of ETLScriptHDR containing the loaded script values.</returns>
		public ETLScriptHDR LoadScriptsValues()
		{
			string path = Path.Combine(Config.ScriptsPath, "Scripts.json");
			ETLScriptHDR Scripts = JsonLoader.DeserializeSingleObject<ETLScriptHDR>(path);
			return Scripts;
		}

		#endregion "Reports L/S"
		#region "Reading and writing Query files"
		/// <summary>Generates a SQL query based on the specified parameters.</summary>
		/// <param name="CmdType">The type of SQL command.</param>
		/// <param name="TableName">The name of the table.</param>
		/// <param name="SchemaName">The name of the schema.</param>
		/// <param name="Filterparamters">The filter parameters for the query.</param>
		/// <param name="QueryList">The list of query SQL repositories.</param>
		/// <param name="DatabaseType">The type of the database.</param>
		/// <returns>A formatted SQL query string.</returns>
		public string GetSql(Sqlcommandtype CmdType, string TableName, string SchemaName, string Filterparamters, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType) //string TableName,string SchemaName
		{
			var ret = (from a in QueryList
					   where a.DatabaseType == DatabaseType
					   where a.Sqltype == CmdType
					   select a.Sql).FirstOrDefault();

			if(ret == null)
			{
                return "";
            }
			return String.Format(ret, TableName, SchemaName, Filterparamters); ;

		}
		/// <summary>Retrieves a list of SQL queries based on the specified parameters.</summary>
		/// <param name="CmdType">The type of SQL command.</param>
		/// <param name="TableName">The name of the table.</param>
		/// <param name="SchemaName">The name of the schema.</param>
		/// <param name="Filterparamters">The filter parameters.</param>
		/// <param name="QueryList">The list of QuerySqlRepo objects.</param>
		/// <param name="DatabaseType">The type of the database.</param>
		/// <returns>A list of SQL queries.</returns>
		public List<string> GetSqlList(Sqlcommandtype CmdType, string TableName, string SchemaName, string Filterparamters, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType) //string TableName,string SchemaName
		{
			var ret = (from a in QueryList
					   where a.DatabaseType == DatabaseType
					   where a.Sqltype == CmdType
					   select a.Sql).ToList();
			List<string> list = new List<string>();
			foreach (var a in ret)
			{
				string av = String.Format(a, TableName, SchemaName, Filterparamters);

				list.Add(av);
			}

			return list;

		}
		/// <summary>Gets the SQL statement from a custom query.</summary>
		/// <param name="CmdType">The type of SQL command.</param>
		/// <param name="TableName">The name of the table.</param>
		/// <param name="customquery">The custom query.</param>
		/// <param name="QueryList">The list of query SQL repositories.</param>
		/// <param name="DatabaseType">The type of the database.</param>
		/// <returns>The formatted SQL statement.</returns>
		public string GetSqlFromCustomQuery(Sqlcommandtype CmdType, string TableName, string customquery, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType) //string TableName,string SchemaName
		{
			var ret = (from a in QueryList
					   where a.DatabaseType == DatabaseType
					   where a.Sqltype == CmdType
					   select a.Sql).FirstOrDefault();


			return String.Format(ret, TableName); ;

		}
		/// <summary>Initializes the query list.</summary>
		/// <returns>An object containing information about any errors that occurred during initialization.</returns>
		/// <remarks>
		/// This method sets the <see cref="ErrorObject.Flag"/> property to <see cref="Errors.Ok"/> before attempting to initialize the query list.
		/// If the "QueryList.json" file exists in the <see cref="ConfigPath"/> directory, the method calls <see cref="LoadQueryFile"/> to load the query list.
		/// If any exceptions occur during initialization, the <see cref="ErrorObject.Flag"/> property is set to <see cref="Errors.Failed"/>,
		/// the exception is stored in the <see cref="ErrorObject.Ex"/> property, and the
		private IErrorsInfo InitQueryList()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				string path = Path.Combine(ConfigPath, "QueryList.json");
				if (File.Exists(path))
				{
					QueryList = LoadQueryFile();
				}
				else
				{
					QueryList = InitQueryDefaultValues();
					SaveQueryFile();
				}

			}
			catch (Exception ex)
			{

				ErrorObject.Flag = Errors.Failed;
				ErrorObject.Ex = ex;
				ErrorObject.Message = ex.Message;
				Logger.WriteLog($"Error Initlization Lists ({ex.Message})");
				//	DMEEditor.AddLogMessage("Fail", $"Error Initlization Lists ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
			}
			return ErrorObject;
		}
		/// <summary>Saves the query list to a JSON file.</summary>
		/// <remarks>
		/// The query list is serialized to a JSON file and saved at the specified path.
		/// </remarks>
		public void SaveQueryFile()
		{
			string path = Path.Combine(ConfigPath, "QueryList.json");
			JsonLoader.Serialize(path, QueryList);
		}
		/// <summary>Loads a query file and returns a list of QuerySqlRepo objects.</summary>
		/// <returns>A list of QuerySqlRepo objects loaded from the query file.</returns>
		public List<QuerySqlRepo> LoadQueryFile()
		{
			string path = Path.Combine(ConfigPath, "QueryList.json");
			QueryList = JsonLoader.DeserializeObject<QuerySqlRepo>(path);
			return QueryList;
		}

		/// <summary>Initializes a list of default query values.</summary>
		/// <returns>A list of QuerySqlRepo objects with default query values.</returns>
		public List<QuerySqlRepo> InitQueryDefaultValues()
		{
			List<QuerySqlRepo> QueryList;
			QueryList = new List<QuerySqlRepo>
			{
				//-------------------------------------- Oracle Query Sql Set -------------------------------------------
				new QuerySqlRepo(DataSourceType.Oracle, "select * from {0} {2}", Sqlcommandtype.getTable),
				new QuerySqlRepo(DataSourceType.Oracle, "select TABLE_NAME from tabs ", Sqlcommandtype.getlistoftables),
				new QuerySqlRepo(DataSourceType.Oracle, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
				new QuerySqlRepo(DataSourceType.Oracle, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
				new QuerySqlRepo(DataSourceType.Oracle, @"SELECT a.position,a.table_name child_table, a.column_name child_column, a.constraint_name, 
											b.table_name parent_table, b.column_name parent_column
									FROM all_cons_columns a,all_constraints c,all_cons_columns b 
										where a.owner = c.owner AND a.constraint_name = c.constraint_name
										and  c.owner = b.owner and c.r_constraint_name = b.constraint_name
										and c.constraint_type = 'R'
										AND b.table_name = '{0}'", Sqlcommandtype.getChildTable),
				new QuerySqlRepo(DataSourceType.Oracle, "select * from {0} {2}", Sqlcommandtype.getParentTable),
				//-------------------------------------- SqlServer Query Sql Set -------------------------------------------
				new QuerySqlRepo(DataSourceType.SqlServer, "select * from {0} {2}", Sqlcommandtype.getTable),
				new QuerySqlRepo(DataSourceType.SqlServer, "select TABLE_NAME from INFORMATION_SCHEMA.TABLES where   TABLE_SCHEMA='{0}'", Sqlcommandtype.getlistoftables),
				new QuerySqlRepo(DataSourceType.SqlServer, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
				new QuerySqlRepo(DataSourceType.SqlServer, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
				new QuerySqlRepo(DataSourceType.SqlServer, @"SELECT    OBJECT_NAME(fkeys.constraint_object_id) constraint_name
												,OBJECT_NAME(fkeys.parent_object_id) child_table
												,COL_NAME(fkeys.parent_object_id, fkeys.parent_column_id) child_column
												,OBJECT_SCHEMA_NAME(fkeys.parent_object_id) referencing_schema_name
												,OBJECT_NAME (fkeys.referenced_object_id) parent_table
												,COL_NAME(fkeys.referenced_object_id, fkeys.referenced_column_id) parent_column
												,OBJECT_SCHEMA_NAME(fkeys.referenced_object_id) referenced_schema_name
												FROM sys.foreign_key_columns AS fkeys
										WHERE OBJECT_NAME(fkeys.parent_object_id) = '{0}' AND 
											  OBJECT_SCHEMA_NAME(fkeys.parent_object_id) = '{1}'", Sqlcommandtype.getChildTable),
				new QuerySqlRepo(DataSourceType.SqlServer, "select * from {0} {1}", Sqlcommandtype.getParentTable),
				//-------------------------------------- Mysql Query Sql Set -------------------------------------------
				new QuerySqlRepo(DataSourceType.Mysql, "select * from {0} {2}", Sqlcommandtype.getTable),
				new QuerySqlRepo(DataSourceType.Mysql, "select table_name from information_schema.tables  where table_schema='{0}'", Sqlcommandtype.getlistoftables),
				new QuerySqlRepo(DataSourceType.Mysql, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
				new QuerySqlRepo(DataSourceType.Mysql, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
				new QuerySqlRepo(DataSourceType.Mysql, @"SELECT a.position,a.table_name child_table, a.column_name child_column, a.constraint_name, 
											b.table_name parent_table, b.column_name parent_column
									FROM all_cons_columns a,all_constraints c,all_cons_columns b 
										where a.owner = c.owner AND a.constraint_name = c.constraint_name
										and  c.owner = b.owner and c.r_constraint_name = b.constraint_name
										and c.constraint_type = 'R'
										AND b.table_name = '{0}'", Sqlcommandtype.getChildTable),
				new QuerySqlRepo(DataSourceType.Mysql, "select * from {0} {2}", Sqlcommandtype.getParentTable),
				//-------------------------------------- SqlCompact Query Sql Set -------------------------------------------
				new QuerySqlRepo(DataSourceType.SqlCompact, "select * from {0} {2}", Sqlcommandtype.getTable),
				new QuerySqlRepo(DataSourceType.SqlCompact, "select TABLE_NAME from INFORMATION_SCHEMA.TABLES where   TABLE_SCHEMA='{0}'", Sqlcommandtype.getlistoftables),
				new QuerySqlRepo(DataSourceType.SqlCompact, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
				new QuerySqlRepo(DataSourceType.SqlCompact, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
				new QuerySqlRepo(DataSourceType.SqlCompact, @"SELECT    OBJECT_NAME(fkeys.constraint_object_id) constraint_name
												,OBJECT_NAME(fkeys.parent_object_id) child_table
												,COL_NAME(fkeys.parent_object_id, fkeys.parent_column_id) child_column
												,OBJECT_SCHEMA_NAME(fkeys.parent_object_id) referencing_schema_name
												,OBJECT_NAME (fkeys.referenced_object_id) parent_table
												,COL_NAME(fkeys.referenced_object_id, fkeys.referenced_column_id) parent_column
												,OBJECT_SCHEMA_NAME(fkeys.referenced_object_id) referenced_schema_name
												FROM sys.foreign_key_columns AS fkeys
										WHERE OBJECT_NAME(fkeys.parent_object_id) = '{0}' AND 
											  OBJECT_SCHEMA_NAME(fkeys.parent_object_id) = '{1}'", Sqlcommandtype.getChildTable),
				new QuerySqlRepo(DataSourceType.SqlCompact, "select * from {0} {2}", Sqlcommandtype.getParentTable),
				//-------------------------------------- SqlLite Query Sql Set -------------------------------------------
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getTable),
				new QuerySqlRepo(DataSourceType.SqlLite, "select TABLE_NAME from tabs ", Sqlcommandtype.getlistoftables),
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
				new QuerySqlRepo(DataSourceType.SqlLite, "select name  table_name from sqlite_master  where type ='table' ", Sqlcommandtype.getChildTable),
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getParentTable),
				//-------------------------------------- Excel Query Sql Set -------------------------------------------
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from [{0}] [{2}]", Sqlcommandtype.getTable),
				new QuerySqlRepo(DataSourceType.SqlLite, "select TABLE_NAME from tabs ", Sqlcommandtype.getlistoftables),
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
				new QuerySqlRepo(DataSourceType.SqlLite, @"SELECT a.position,a.table_name child_table, a.column_name child_column, a.constraint_name, 
											b.table_name parent_table, b.column_name parent_column
									FROM all_cons_columns a,all_constraints c,all_cons_columns b 
										where a.owner = c.owner AND a.constraint_name = c.constraint_name
										and  c.owner = b.owner and c.r_constraint_name = b.constraint_name
										and c.constraint_type = 'R'
										AND b.table_name = '{0}'", Sqlcommandtype.getChildTable),
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getParentTable),
				//-------------------------------------- Text Query Sql Set -------------------------------------------
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from [{0}] [{2}]", Sqlcommandtype.getTable),
				new QuerySqlRepo(DataSourceType.SqlLite, "select TABLE_NAME from tabs ", Sqlcommandtype.getlistoftables),
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getPKforTable),
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getFKforTable),
				new QuerySqlRepo(DataSourceType.SqlLite, @"SELECT a.position,a.table_name child_table, a.column_name child_column, a.constraint_name, 
											b.table_name parent_table, b.column_name parent_column
									FROM all_cons_columns a,all_constraints c,all_cons_columns b 
										where a.owner = c.owner AND a.constraint_name = c.constraint_name
										and  c.owner = b.owner and c.r_constraint_name = b.constraint_name
										and c.constraint_type = 'R'
										AND b.table_name = '{0}'", Sqlcommandtype.getChildTable),
				new QuerySqlRepo(DataSourceType.SqlLite, "select * from {0} {2}", Sqlcommandtype.getParentTable)
			};

			return QueryList;

		}
		#endregion "Reading and writing Query files"
		#region "Entity Structure Loading and Saving Methods"
		/// <summary>Loads the values of a data source's entities from a JSON file.</summary>
		/// <param name="dsname">The name of the data source.</param>
		/// <returns>An instance of DatasourceEntities containing the loaded values.</returns>
		public DatasourceEntities LoadDataSourceEntitiesValues(string dsname)
		{
			string path = Path.Combine(ExePath + @"\Entities\", dsname + "_entities.json");


			return JsonLoader.DeserializeSingleObject<DatasourceEntities>(path);

		}
		/// <summary>Removes the values of a data source's entities.</summary>
		/// <param name="dsname">The name of the data source.</param>
		/// <returns>True if the values were successfully removed, false otherwise.</returns>
		/// <exception cref="IOException">Thrown when an error occurs while deleting the file.</exception>
		public bool RemoveDataSourceEntitiesValues(string dsname)
		{
			string path = Path.Combine(ExePath + @"\Entities\", dsname + "_entities.json");
			try
			{
				File.Delete(path);
				return true;
			}
			catch (IOException ex)
			{

				return false;
			}



		}
		/// <summary>Saves the values of a DataSourceEntities object to a JSON file.</summary>
		/// <param name="datasourceEntities">The DataSourceEntities object containing the values to be saved.</param>
		/// <remarks>The JSON file will be saved in the same directory as the executable, under a folder named "Entities". The file name will be in the format "datasourcename_entities.json".</remarks>
		public void SaveDataSourceEntitiesValues(DatasourceEntities datasourceEntities)
		{
			string path = Path.Combine(ExePath + @"\Entities\", datasourceEntities.datasourcename + "_entities.json");
			JsonLoader.Serialize(path, datasourceEntities);
		}
		/// <summary>Scans the saved entities in the specified folders and adds them to the collection.</summary>
		/// <remarks>
		/// This method iterates through the specified folders and searches for files with the extension "_ES.json".
		/// For each found file, it extracts the entity name and entity ID from the file name and adds them to the Entities collection.
		/// </remarks>
		public void ScanSavedEntities()
		{

			foreach (string path in Config.Folders.Where(x => x.FolderFilesType == FolderFileTypes.ProjectData).Select(f => f.FolderPath))
			{

				foreach (string filename in Directory.GetFiles(path, "*_ES.json", SearchOption.AllDirectories))
				{
					string[] n = filename.Split(new char[] { '^' });
					Entities.Add(n[0], n[1]);


				}

			}

		}
		/// <summary>Checks if the entity structure file exists.</summary>
		/// <param name="filepath">The directory path where the file is located.</param>
		/// <param name="EntityName">The name of the entity.</param>
		/// <param name="DataSourceID">The ID of the data source.</param>
		/// <returns>True if the entity structure file exists, false otherwise.</returns>
		public bool EntityStructureExist(string filepath, string EntityName, string DataSourceID)
		{
			//  EntityStructure retval = new EntityStructure();
			string filename = DataSourceID + "^" + EntityName + "_ES.json";
			string path = Path.Combine(filepath, filename);

			if (File.Exists(path))
			{
				return true;

			}
			else
				return false;

		}
		/// <summary>Saves the structure of an entity to a JSON file.</summary>
		/// <param name="filepath">The directory path where the file will be saved.</param>
		/// <param name="entity">The entity structure to be saved.</param>
		/// <remarks>The file name is constructed using the entity's DataSourceID and EntityName properties.</remarks>
		public void SaveEntityStructure(string filepath, EntityStructure entity)
		{
			string filename = entity.DataSourceID + "^" + entity.EntityName + "_ES.json";
			string path = Path.Combine(filepath, filename);
			JsonLoader.Serialize(path, entity);

		}
		/// <summary>Loads an entity structure from a JSON file.</summary>
		/// <param name="filepath">The path to the directory containing the JSON file.</param>
		/// <param name="EntityName">The name of the entity.</param>
		/// <param name="DataSourceID">The ID of the data source.</param>
		/// <returns>The loaded entity structure.</returns>
		public EntityStructure LoadEntityStructure(string filepath, string EntityName, string DataSourceID)
		{
			EntityStructure retval = new EntityStructure();
			string filename = DataSourceID + "^" + EntityName + "_ES.json";
			string path = Path.Combine(filepath, filename);
			retval = JsonLoader.DeserializeSingleObject<EntityStructure>(path);
			return retval;

		}
		#endregion
		#region"Mapping Save and Load Methods"
		//public void SaveMappingSchemaValue(string mapname)
		//{
		//	Map_Schema retval = MappingSchema.Where(x => x.SchemaName.Equals(mapname,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
		//	if (retval != null)
		//	{
		//		string path = Path.Combine(ConfigPath, mapname + ".json");
		//		JsonLoader.Serialize(path, retval);
		//	}

		//}
		//public Map_Schema LoadMappingSchema(string mapname)
		//{
		//	Map_Schema Existingretval = MappingSchema.Where(x => x.SchemaName .Equals(mapname, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
		//	Map_Schema retval = null;
		//	string path = Path.Combine(ConfigPath, mapname + ".json");
		//	//File.WriteAllText(path, JsonConvert.SerializeObject(ts));
		//	// serialize JSON directly to a file
		//	if (File.Exists(path))
		//	{

		//		retval = JsonLoader.DeserializeSingleObject<Map_Schema>(path);  //JsonConvert.DeserializeObject<Map_Schema>(JSONtxt);
		//	}
		//	if (retval != null)
		//	{
		//		if (Existingretval != null)
		//		{
		//			Existingretval = retval;

		//		}
		//		else
		//		{
		//			MappingSchema.Add(retval);
		//		}
		//	}


		//	return retval;


		//}
		/// <summary>Saves a mapping schema value to a JSON file.</summary>
		/// <param name="schemaname">The name of the schema.</param>
		/// <param name="mapping_Rep">The mapping schema object to be saved.</param>
		/// <remarks>
		/// The method serializes the mapping schema object to JSON format and saves it to a file.
		/// The file name is constructed by combining the schema name with "_Mapping.json".
		/// The file is saved in the directory specified by the "MappingPath" configuration setting.
		/// </remarks>
		public void SaveMappingSchemaValue(string schemaname, Map_Schema mapping_Rep)
		{

			string path = Path.Combine(Config.MappingPath, $"{schemaname}_Mapping.json");
			JsonLoader.Serialize(path, mapping_Rep);


		}
		/// <summary>Loads a mapping schema from a JSON file.</summary>
		/// <param name="schemaname">The name of the schema to load.</param>
		/// <returns>The loaded mapping schema.</returns>
		/// <exception cref="FileNotFoundException">Thrown when the JSON file for the specified schema name is not found.</exception>
		public Map_Schema LoadMappingSchema(string schemaname)
		{

			string path = Path.Combine(Config.MappingPath, $"{schemaname}_Mapping.json");
			Map_Schema MappingSchema = JsonLoader.DeserializeSingleObject<Map_Schema>(path);
			return MappingSchema;



		}
		//public void SaveMapsValues()
		//{
		//	string path = Path.Combine(ConfigPath, "Maps.json");
		//	JsonLoader.Serialize(path, MappingSchema);

		//}
		//public void LoadMapsValues()
		//{
		//	string path = Path.Combine(ConfigPath, "Maps.json");
		//	MappingSchema = JsonLoader.DeserializeObject<Map_Schema>(path);
		//}
		/// <summary>Saves the mapping values for a specific entity.</summary>
		/// <param name="Entityname">The name of the entity.</param>
		/// <param name="datasource">The data source.</param>
		/// <param name="mapping_Rep">The entity data map containing the mapping values.</param>
		/// <exception cref="ArgumentNullException">Thrown when Entityname, datasource, or mapping_Rep is null.</exception>
		public void SaveMappingValues(string Entityname, string datasource, EntityDataMap mapping_Rep)
		{
			CreateDir(Path.Combine(Config.MappingPath, datasource));
			string path = Path.Combine(Path.Combine(Config.MappingPath, datasource), $"{Entityname}_Mapping.json");
			JsonLoader.Serialize(path, mapping_Rep);
		}
		/// <summary>Loads the mapping values for a given entity from a JSON file.</summary>
		/// <param name="Entityname">The name of the entity.</param>
		/// <param name="datasource">The name of the data source.</param>
		/// <returns>An instance of EntityDataMap containing the loaded mapping values.</returns>
		public EntityDataMap LoadMappingValues(string Entityname, string datasource)
		{

			string path = Path.Combine(Path.Combine(Config.MappingPath, datasource), $"{Entityname}_Mapping.json");

			EntityDataMap Mappings = JsonLoader.DeserializeSingleObject<EntityDataMap>(path);
			return Mappings;

		}
		#endregion
		#region "Data Connections L/S"
		/// <summary>Checks if a data connection exists.</summary>
		/// <param name="cn">The connection properties to check.</param>
		/// <returns>True if the data connection exists, false otherwise.</returns>
		public bool DataConnectionExist(ConnectionProperties cn)
		{
			if (DataConnections == null)
			{
				DataConnections = new List<ConnectionProperties>();
				return false;
			}
			// check if the connection exists based or the file path and file name
			if(cn != null)
			{
				if(cn.Category== DatasourceCategory.FILE)
				{
                    string filepath = Path.Combine(cn.FilePath, cn.FileName);
                    return DataConnections != null ? DataConnections.Any(x => x.Category == DatasourceCategory.FILE && !string.IsNullOrEmpty(x.FilePath) && !string.IsNullOrEmpty(x.FileName) && Path.Combine(x.FilePath, x.FileName).Equals(filepath, StringComparison.InvariantCultureIgnoreCase)) : false;
                }else
				{
					//cnnection is not a file connection
					return DataConnections != null ? DataConnections.Any(x => x.ConnectionName.Equals(cn.ConnectionName, StringComparison.InvariantCultureIgnoreCase)) : false;
                     
                }
			}
			return false;
			
		}
		/// <summary>Checks if a data connection with the specified GUID exists.</summary>
		/// <param name="GuidID">The GUID of the data connection to check.</param>
		/// <returns>True if a data connection with the specified GUID exists, false otherwise.</returns>
		public bool DataConnectionGuidExist(string GuidID)
		{
			if (DataConnections == null)
			{
				DataConnections = new List<ConnectionProperties>();
				return false;
			}
			return DataConnections != null ? DataConnections.Any(x => x.GuidID.Equals(GuidID, StringComparison.InvariantCultureIgnoreCase)) : false;
		}
		/// <summary>Saves the values of data connections to a JSON file.</summary>
		/// <remarks>
		/// The data connections values are serialized and saved to a JSON file located at the specified path.
		/// </remarks>
		public void SaveDataconnectionsValues()
		{
			string path = Path.Combine(ConfigPath, "DataConnections.json");
			JsonLoader.Serialize(path, DataConnections);

		}
		/// <summary>Loads the values of data connections from a JSON file.</summary>
		/// <returns>A list of ConnectionProperties objects representing the loaded data connections.</returns>
		public List<ConnectionProperties> LoadDataConnectionsValues()
		{
			string path = Path.Combine(ConfigPath, "DataConnections.json");
			DataConnections = JsonLoader.DeserializeObject<ConnectionProperties>(path);
			if (DataConnections == null)
			{
				DataConnections = new List<ConnectionProperties>();
				return DataConnections;

			}
			return DataConnections;
		}
		/// <summary>Checks if a data connection with the specified name exists.</summary>
		/// <param name="ConnectionName">The name of the data connection to check.</param>
		/// <returns>True if a data connection with the specified name exists, false otherwise.</returns>
		public bool DataConnectionExist(string ConnectionName)
		{
			if (DataConnections == null)
			{
				DataConnections = new List<ConnectionProperties>();
				return false;

			}
			return DataConnections != null ? DataConnections.Any(x =>!string.IsNullOrEmpty(x.ConnectionName) && x.ConnectionName.Equals(ConnectionName, StringComparison.InvariantCultureIgnoreCase)) : false;
		}
		/// <summary>Adds a data connection to the list of data connections.</summary>
		/// <param name="cn">The connection properties to add.</param>
		/// <returns>True if the connection was successfully added, false otherwise.</returns>
		/// <remarks>
		/// If the provided connection properties are null, the method will return false without adding anything to the list.
		/// If the list of data connections is null, a new list will be created before adding the connection properties.
		/// Any exceptions thrown during the process will be caught and the method will return false.
		/// </remarks>
		public bool AddDataConnection(ConnectionProperties cn)
		{
			try
			{
				if (cn == null) { return false; }
                if (string.IsNullOrEmpty(cn.ConnectionName)) { return false; }
                if (DataConnections == null)
				{
					DataConnections = new List<ConnectionProperties>();

				}
				if (!DataConnectionExist(cn.ConnectionName))
				{
					if (cn.ID <= 0)
					{
						if (DataConnections.Count == 0)
						{
							cn.ID = 1;
						}
						else
						{
							cn.ID = DataConnections.Max(p => p.ID) + 1;
						}




					}

					DataConnections.Add(cn);
					return true;
				}
				else
				{
					return false;
				}



			}
			catch (Exception)
			{
				return false;
			}



		}
		/// <summary>Updates the data connection with the specified properties.</summary>
		/// <param name="conn">The connection properties to update.</param>
		/// <param name="category">The category of the connection.</param>
		/// <returns>True if the update was successful, false otherwise.</returns>
		public bool UpdateDataConnection(ConnectionProperties conn, string category)
		{

			try
			{
                if (conn == null) { return false; }
                if (string.IsNullOrEmpty(conn.ConnectionName)) { return false; }
                if (DataConnections == null)
				{
					DataConnections = new List<ConnectionProperties>();

				}
				int idx = DataConnections.FindIndex(0, p => p.ID == conn.ID);
				if (idx < 0)
				{
					idx = DataConnections.FindIndex(x => x.GuidID.Equals(conn.GuidID, StringComparison.InvariantCultureIgnoreCase));
				}
				if (idx == -1)
				{
					DataConnections.Add(conn);
				}
				else
					DataConnections[idx] = conn;

			}
			catch (Exception)
			{

				return false;
			};
			return true;
		}
		/// <summary>Removes a connection from the list of data connections by its name.</summary>
		/// <param name="pname">The name of the connection to remove.</param>
		/// <returns>True if the connection was successfully removed, false otherwise.</returns>
		public bool RemoveConnByName(string pname)
		{
			if (DataConnections == null)
			{
				DataConnections = new List<ConnectionProperties>();
				return false;

			}
			int i = DataConnections.FindIndex(x => x.ConnectionName.Equals(pname, StringComparison.InvariantCultureIgnoreCase));

			return DataConnections.Remove(DataConnections[i]);
		}
		/// <summary>Removes a connection from the list of data connections by its ID.</summary>
		/// <param name="ID">The ID of the connection to be removed.</param>
		/// <returns>True if the connection was successfully removed, false otherwise.</returns>
		public bool RemoveConnByID(int ID)
		{
			if (DataConnections == null)
			{
				DataConnections = new List<ConnectionProperties>();
				return false;

			}
			int i = DataConnections.FindIndex(x => x.ID == ID);

			return DataConnections.Remove(DataConnections[i]);
		}
		/// <summary>Removes a connection from the list of data connections based on its GuidID.</summary>
		/// <param name="GuidID">The GuidID of the connection to be removed.</param>
		/// <returns>True if the connection was successfully removed, false otherwise.</returns>
		public bool RemoveConnByGuidID(string GuidID)
		{
			if (DataConnections == null)
			{
				DataConnections = new List<ConnectionProperties>();
				return false;

			}
			int i = DataConnections.FindIndex(x => x.GuidID.Equals(GuidID, StringComparison.InvariantCultureIgnoreCase));
			return DataConnections.Remove(DataConnections[i]);
		}

		/// <summary>Removes a data connection with the specified name.</summary>
		/// <param name="pname">The name of the data connection to remove.</param>
		/// <returns>True if the data connection was successfully removed, false otherwise.</returns>
		/// <remarks>
		/// If the list of data connections is null, a new list is created and false is returned.
		/// The method then saves the updated data connections and returns true.
		/// If an exception occurs during the process, the exception message is stored in a variable and false is returned.
		/// </remarks>
		public bool RemoveDataConnection(string pname)
		{

			try
			{
				if (DataConnections == null)
				{
					DataConnections = new List<ConnectionProperties>();
					return false;

				}
				ConnectionProperties dc = DataConnections.Where(x => !string.IsNullOrEmpty(x.ConnectionName) && x.ConnectionName.Equals(pname, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
				if (dc != null)
				{
					DataConnections.Remove(dc);
				}
				else
				{
					return false;
				}
				SaveDataconnectionsValues();
				return true;

			}
			catch (Exception ex)
			{
				string mes = ex.Message;
				// AddLogMessage(ex.Message, "Could not Remove data source " + mes, DateTime.Now, -1, mes, Errors.Failed);
				return false;
			};
		}
		#endregion "Data Connections L/S"
		#region "Configuration L/S"
		/// <summary>Loads the configuration values from a JSON file.</summary>
		/// <returns>An instance of the ConfigandSettings class containing the loaded configuration values.</returns>
		public ConfigandSettings LoadConfigValues()
		{
			string path = Path.Combine(ExePath, "Config.json");
			Config = JsonLoader.DeserializeSingleObject<ConfigandSettings>(path);

			return Config;

		}
		/// <summary>Saves the configuration values to a JSON file.</summary>
		/// <remarks>
		/// The configuration values are serialized using JSON format and saved to a file named "Config.json".
		/// The file is saved in the same directory as the executable file.
		/// </remarks>
		public void SaveConfigValues()
		{
			string path = Path.Combine(ExePath, "Config.json");
			JsonLoader.Serialize(path, Config);
		}
		/// <summary>Adds a folder category to the collection of category folders.</summary>
		/// <param name="pfoldername">The name of the folder.</param>
		/// <param name="prootname">The name of the root.</param>
		/// <param name="pparentname">The name of the parent.</param>
		/// <param name="parentguidid">The ID of the parent.</param>
		/// <param name="isparentFolder">Indicates if the folder is a parent folder.</param>
		/// <param name="isparentRoot">Indicates if the folder is a parent root.</param>
		/// <param name="isphysical">Indicates if the folder is a physical folder.</param>
		/// <returns
		public CategoryFolder AddFolderCategory(string pfoldername, string prootname, string pparentname, string parentguidid, bool isparentFolder = false, bool isparentRoot = true, bool isphysical = false)
		{
			try
			{
				CategoryFolder x = new CategoryFolder();
				x.FolderName = pfoldername;
				x.RootName = prootname;
				x.ParentName = pparentname;
				x.ParentGuidID = parentguidid;
				x.IsParentFolder = isparentFolder;
				x.IsParentRoot = isparentRoot;
				x.IsPhysicalFolder = isparentRoot;
				x.IsPhysicalFolder = isphysical;
				CategoryFolders.Add(x);
				SaveCategoryFoldersValues();
				return x;
			}
			catch (Exception)
			{

				return null;
			}
		}
		/// <summary>Adds a new folder category to the collection of category folders.</summary>
		/// <param name="pfoldername">The name of the folder.</param>
		/// <param name="prootname">The name of the root category.</param>
		/// <param name="pparentname">The name of the parent category.</param>
		/// <returns>The newly added CategoryFolder object.</returns>
		/// <remarks>If an exception occurs during the process, null is returned.</remarks>
		public CategoryFolder AddFolderCategory(string pfoldername, string prootname, string pparentname)
		{
			try
			{
				CategoryFolder x = new CategoryFolder();
				x.FolderName = pfoldername;
				x.RootName = prootname;
				x.ParentName = pparentname;

				CategoryFolders.Add(x);
				SaveCategoryFoldersValues();
				return x;
			}
			catch (Exception)
			{

				return null;
			}
		}
		/// <summary>Removes a folder category.</summary>
		/// <param name="pfoldername">The name of the folder.</param>
		/// <param name="prootname">The name of the root.</param>
		/// <param name="parentguidid">The ID of the parent.</param>
		/// <returns>True if the folder category was successfully removed, false otherwise.</returns>
		public bool RemoveFolderCategory(string pfoldername, string prootname, string parentguidid)
		{
			try
			{

				CategoryFolder x = CategoryFolders.Where(y => y.FolderName.Equals(pfoldername, StringComparison.InvariantCultureIgnoreCase) && y.RootName.Equals(prootname, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
				if (x == null)
				{
					x = CategoryFolders.Where(y => y.FolderName.Equals(pfoldername, StringComparison.InvariantCultureIgnoreCase) && y.ParentGuidID.Equals(parentguidid, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
				}
				if (x == null)
				{
					return false;
				}

				CategoryFolders.Remove(x);
				SaveCategoryFoldersValues();
				return true;
			}
			catch (Exception)
			{

				return false;
			}


		}
		/// <summary>Loads the values of category folders from a JSON file.</summary>
		/// <remarks>
		/// The JSON file should be located at the specified path.
		/// The loaded values are stored in the CategoryFolders property.
		/// </remarks>
		public void LoadCategoryFoldersValues()
		{
			string path = Path.Combine(ConfigPath, "CategoryFolders.json");
			CategoryFolders = JsonLoader.DeserializeObject<CategoryFolder>(path);
		}
		/// <summary>Saves the values of category folders to a JSON file.</summary>
		/// <remarks>
		/// The method serializes the CategoryFolders object to JSON format and saves it to the specified file path.
		/// </remarks>
		public void SaveCategoryFoldersValues()
		{
			string path = Path.Combine(ConfigPath, "CategoryFolders.json");
			JsonLoader.Serialize(path, CategoryFolders);
		}
		/// <summary>Loads the connection drivers configuration values from a JSON file.</summary>
		/// <returns>A list of ConnectionDriversConfig objects representing the loaded configuration values.</returns>
		public List<ConnectionDriversConfig> LoadConnectionDriversConfigValues()
		{
			string path = Path.Combine(ConfigPath, "ConnectionConfig.json");
			DataDriversClasses = JsonLoader.DeserializeObject<ConnectionDriversConfig>(path);
			return DataDriversClasses;
		}
		/// <summary>Saves the configuration values of connection drivers to a JSON file.</summary>
		/// <remarks>
		/// The method serializes the <paramref name="DataDriversClasses"/> object to JSON format
		/// and saves it to the specified file path.
		/// </remarks>
		/// <param name="ConfigPath">The path where the JSON file will be saved.</param>
		/// <param name="DataDriversClasses">The object containing the configuration values of connection drivers.</param>
		public void SaveConnectionDriversConfigValues()
		{
			string path = Path.Combine(ConfigPath, "ConnectionConfig.json");
			JsonLoader.Serialize(path, DataDriversClasses);
		}
		/// <summary>Loads the connection drivers definition from a JSON file.</summary>
		/// <returns>A list of ConnectionDriversConfig objects representing the loaded drivers definition.</returns>
		//public List<ConnectionDriversConfig> LoadConnectionDriversDefinition()
		//{
		//	string path = Path.Combine(ConfigPath, "DriversDefinitions.json");
		//	DriverDefinitionsConfig = JsonLoader.DeserializeObject<ConnectionDriversConfig>(path);

		//	return DriverDefinitionsConfig;
		//	//  QueryList = ReadQueryFile("QueryList.json");
		//}
		/// <summary>Saves the connection driver definitions to a JSON file.</summary>
		/// <remarks>
		/// The connection driver definitions are serialized to a JSON file located at the specified path.
		/// </remarks>
		//public void SaveConnectionDriversDefinitions()
		//{
		//	string path = Path.Combine(ConfigPath, "DriversDefinitions.json");
		//	JsonLoader.Serialize(path, DriverDefinitionsConfig);
		//}
		/// <summary>Saves the values of the databases to a JSON file.</summary>
		/// <remarks>
		/// The method serializes the values of the databases to a JSON file located at the specified path.
		/// </remarks>
		public void SaveDatabasesValues()
		{
			string path = Path.Combine(ConfigPath, "Databasetypes.json");
			JsonLoader.Serialize(path, Databasetypes);
		}
		/// <summary>Loads the values of the databases from a JSON file.</summary>
		/// <remarks>
		/// The method reads the JSON file located at the specified path and deserializes its content into a collection of strings.
		/// The deserialized values are then assigned to the Databasetypes property.
		/// </remarks>
		public void LoadDatabasesValues()
		{
			string path = Path.Combine(ConfigPath, "Databasetypes.json");
			Databasetypes = JsonLoader.DeserializeObject<string>(path);
		}
		//--
		/// <summary>Saves the events to a JSON file.</summary>
		/// <remarks>
		/// The events are serialized to JSON format and saved to the specified file path.
		/// </remarks>
		public void SaveEvents()
		{
			string path = Path.Combine(ConfigPath, "events.json");
			JsonLoader.Serialize(path, Events);
		}
		/// <summary>Loads events from a JSON file.</summary>
		/// <remarks>
		/// The method reads the events from a JSON file located at the specified path.
		/// The events are deserialized into a collection of Event objects.
		/// </remarks>

		public void LoadEvents()
		{
			string path = Path.Combine(ConfigPath, "events.json");
			Events = JsonLoader.DeserializeObject<Event>(path);

		}
		/// <summary>Saves the Function2Function data to a JSON file.</summary>
		/// <remarks>
		/// The Function2Function data will be serialized and saved to a JSON file located at the specified path.
		/// </remarks>
		public void SaveFucntion2Function()
		{

			string path = Path.Combine(ConfigPath, "Function2Function.json");
			JsonLoader.Serialize(path, Function2Functions);
		}
		/// <summary>Loads the configuration for Function2Function from a JSON file.</summary>
		/// <remarks>
		/// The JSON file should be located at the specified path.
		/// The loaded configuration is stored in the Function2Functions property.
		/// </remarks>
		public void LoadFucntion2Function()
		{
			string path = Path.Combine(ConfigPath, "Function2Function.json");
			Function2Functions = JsonLoader.DeserializeObject<Function2FunctionAction>(path);

		}
		/// <summary>Loads the add-in tree structure from a JSON file.</summary>
		/// <remarks>
		/// The add-in tree structure is loaded from a JSON file located at the specified path.
		/// The JSON file should contain the serialized representation of the <see cref="AddinTreeStructure"/> object.
		/// </remarks>
		/// <param name="path">The path to the JSON file.</param>
		public void LoadAddinTreeStructure()
		{
			string path = Path.Combine(ConfigPath, "AddinTreeStructure.json");
			AddinTreeStructure = JsonLoader.DeserializeObject<AddinTreeStructure>(path);

		}
		/// <summary>Saves the add-in tree structure to a JSON file.</summary>
		/// <remarks>
		/// The add-in tree structure is serialized to a JSON file and saved at the specified path.
		/// </remarks>
		public void SaveAddinTreeStructure()
		{

			string path = Path.Combine(ConfigPath, "AddinTreeStructure.json");
			JsonLoader.Serialize(path, AddinTreeStructure);



		}
		#endregion "Configuration L/S"
		#region "Reports L/S"
		/// <summary>Saves the values of AI scripts to a JSON file.</summary>
		/// <remarks>
		/// The AI scripts values are serialized and saved to a JSON file located at the specified path.
		/// </remarks>
		public void SaveAIScriptsValues()
		{
			string path = Path.Combine(ConfigPath, "AIScripts.json");
			JsonLoader.Serialize(path, AIScriptslist);

		}
		/// <summary>Loads the values of AI scripts from a JSON file.</summary>
		/// <returns>A list of ReportsList objects representing the loaded AI scripts.</returns>
		public List<ReportsList> LoadAIScriptsValues()
		{
			string path = Path.Combine(ConfigPath, "AIScripts.json");
			AIScriptslist = JsonLoader.DeserializeObject<ReportsList>(path);
			return AIScriptslist;
		}
		/// <summary>Saves the values of the reports list to a JSON file.</summary>
		/// <remarks>
		/// The reports list is serialized using JSON format and saved to the specified file path.
		/// </remarks>
		public void SaveReportsValues()
		{
			string path = Path.Combine(ConfigPath, "Reportslist.json");
			JsonLoader.Serialize(path, Reportslist);

		}
		/// <summary>Loads the values of reports from a JSON file.</summary>
		/// <returns>A list of ReportsList objects containing the loaded report values.</returns>
		public List<ReportsList> LoadReportsValues()
		{
			string path = Path.Combine(ConfigPath, "Reportslist.json");
			Reportslist = JsonLoader.DeserializeObject<ReportsList>(path);
			return Reportslist;
		}
		/// <summary>Saves the values of report definitions to a JSON file.</summary>
		/// <remarks>
		/// The report definitions are serialized using JSON format and saved to the specified file path.
		/// </remarks>
		public void SaveReportDefinitionsValues()
		{
			string path = Path.Combine(ConfigPath, "reportsDefinition.json");
			JsonLoader.Serialize(path, ReportsDefinition);

		}
		/// <summary>Loads the values of the reports definition from a JSON file.</summary>
		/// <returns>A list of AppTemplate objects representing the reports definition.</returns>
		public List<AppTemplate> LoadReportsDefinitionValues()
		{
			string path = Path.Combine(ConfigPath, "reportsDefinition.json");
			ReportsDefinition = JsonLoader.DeserializeObject<AppTemplate>(path);
			return ReportsDefinition;
		}
		#endregion "Reports L/S"
		#region "SaveLocation of App"
		/// <summary>Saves the location information.</summary>
		/// <remarks>
		/// This method creates a directory if it doesn't exist at the specified path.
		/// It then saves the location information to a JSON file and the executable path to a text file.
		/// </remarks>
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
		/// <summary>Checks if the location is saved.</summary>
		/// <returns>True if the location is saved, otherwise false.</returns>
		public bool IsLocationSaved()
		{
			SaveLocation();
			return true;


		}
		#endregion "SaveLocation of App"
		//#region "AppFieldProperties L/S"

		//public void SaveAppFieldPropertiesValues()
		//{
		//	foreach (var item in AppfieldProperties)
		//	{
		//		if (item != null)
		//		{
		//			string path = Path.Combine(ExePath + @"\Entities\", item.DatasourceName + "_properties.json");
		//			JsonLoader.Serialize(path, AppfieldProperties);
		//		}

		//	}


		//}
		//public DataSourceFieldProperties LoadAppFieldPropertiesValues(string dsname)
		//{
		//	DataSourceFieldProperties retval = null ;
		//	if (AppfieldProperties != null)
		//	{
		//		if (AppfieldProperties.Any(i => i.DatasourceName.Equals(dsname,StringComparison.InvariantCultureIgnoreCase)))
		//		{
		//			retval = AppfieldProperties.Where(i => i.DatasourceName.Equals(dsname, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
		//			return retval;
		//		}
		//		else
		//		{
		//			string path = Path.Combine(ExePath + @"\Entities\", dsname + "_properties.json");
		//			retval = JsonLoader.DeserializeSingleObject<DataSourceFieldProperties>(path);
		//			if (retval != null)
		//			{
		//				AppfieldProperties.Add(retval);
		//				return retval;
		//			}else
		//			{
		//				retval= null;
		//			}

		//		}
		//	}
		//	return retval;

		//}
		//#endregion "Reports L/S"
		#region "CompositeLayers L/S"\
		/// <summary>Removes a layer from the composite query layers by its name.</summary>
		/// <param name="LayerName">The name of the layer to remove.</param>
		/// <returns>True if the layer was successfully removed, false otherwise.</returns>
		public bool RemoveLayerByName(string LayerName)
		{

			int i = CompositeQueryLayers.FindIndex(x => x.LayerName.Equals(LayerName, StringComparison.InvariantCultureIgnoreCase));
			if (i > -1)
			{
				return CompositeQueryLayers.Remove(CompositeQueryLayers[i]);
			}
			else
				return true;

		}
		/// <summary>Removes a layer from the composite query layers list based on its ID.</summary>
		/// <param name="ID">The ID of the layer to be removed.</param>
		/// <returns>True if the layer was successfully removed, false otherwise.</returns>
		public bool RemoveLayerByID(int ID)
		{

			int i = CompositeQueryLayers.FindIndex(x => x.ID == ID);
			if (i > -1)
			{
				return CompositeQueryLayers.Remove(CompositeQueryLayers[i]);
			}
			else
				return true;

		}
		/// <summary>Saves the values of composite layers to a JSON file.</summary>
		/// <remarks>
		/// The method serializes the composite query layers and saves them to a JSON file.
		/// The JSON file is saved at the specified path, which is a combination of the configuration path and the file name "CompositeLayers.json".
		/// </remarks>
		public void SaveCompositeLayersValues()
		{
			string path = Path.Combine(ConfigPath, "CompositeLayers.json");
			JsonLoader.Serialize(path, CompositeQueryLayers);

		}
		/// <summary>Loads the values of composite layers from a JSON file.</summary>
		/// <returns>A list of composite layers.</returns>
		public List<CompositeLayer> LoadCompositeLayersValues()
		{
			string path = Path.Combine(ConfigPath, "CompositeLayers.json");
			//File.WriteAllText(path, JsonConvert.SerializeObject(ts));
			// serialize JSON directly to a file
			CompositeQueryLayers = JsonLoader.DeserializeObject<CompositeLayer>(path);
			return CompositeQueryLayers;
		}
		#endregion "CompositeLayers L/S"
		//#region "Apps L/S"\
		//public bool RemoveAppByName(string AppName)
		//{

		//	int i = Apps.FindIndex(x => x.AppName.Equals(AppName,StringComparison.InvariantCultureIgnoreCase));

		//	return Apps.Remove(Apps[i]);
		//}
		//public bool RemoveAppByID(string ID)
		//{

		//	int i = Apps.FindIndex(x => x.ID.Equals(ID,StringComparison.InvariantCultureIgnoreCase));

		//	return Apps.Remove(Apps[i]);
		//}
		//public void SaveAppValues()
		//{
		//	string path = Path.Combine(ConfigPath, "Apps.json");
		//	JsonLoader.Serialize(path, Apps);
		//}
		//public List<App> LoadAppValues()
		//{
		//	string path = Path.Combine(ConfigPath, "Apps.json");
		//	Apps = JsonLoader.DeserializeObject<App>(path);
		//	return Apps;
		//}
		//#endregion "Apps L/S"
		#region"Defaults Values"
		//public void SaveDefaultsFile()
		//{
		//	string path = Path.Combine(ConfigPath, "DefaultValues.json");
		//	JsonLoader.Serialize(path, DatasourceDefaults);
		//}
		//public List<DataSourceDefaults> LoadDefaultsFile()
		//{
		//	string path = Path.Combine(ConfigPath, "DefaultValues.json");
		//	DatasourceDefaults = JsonLoader.DeserializeObject<DataSourceDefaults>(path);
		//	return DatasourceDefaults;
		//}
		#endregion
		#region"Object Types Values"
		/// <summary>Saves the object types to a JSON file.</summary>
		/// <remarks>
		/// The object types are serialized and saved to a JSON file located at the specified path.
		/// </remarks>
		public void SaveObjectTypes()
		{
			string path = Path.Combine(ConfigPath, "ObjectTypes.json");
			JsonLoader.Serialize(path, objectTypes);
		}
		/// <summary>Loads the object types from a JSON file.</summary>
		/// <returns>A list of object types.</returns>
		public List<ObjectTypes> LoadObjectTypes()
		{
			string path = Path.Combine(ConfigPath, "ObjectTypes.json");
			objectTypes = JsonLoader.DeserializeObject<ObjectTypes>(path);
			return objectTypes;
		}
		#endregion
		#region "WorkFlows L/S"
		/// <summary>Reads the work flow data from a JSON file.</summary>
		/// <remarks>
		/// The method attempts to read the work flow data from a JSON file located in the "WorkFlow" directory
		/// within the application's executable path. If the file is found and successfully deserialized, the
		/// work flow data is stored in the "WorkFlows" property. If an exception occurs during the process,
		/// it is caught and ignored.
		/// </remarks>
		public void ReadWork()
		{
			try
			{
				string path = Path.Combine(ExePath + @"\WorkFlow\", "DataWorkFlow.json");
				WorkFlows = JsonLoader.DeserializeObject<WorkFlow>(path);

			}
			catch (System.Exception)
			{
			}
		}
		/// <summary>Saves the work to a JSON file.</summary>
		/// <remarks>
		/// The work is serialized and saved to a JSON file located in the "WorkFlow" directory.
		/// The file name is "DataWorkFlow.json".
		/// </remarks>
		public void SaveWork()
		{
			try
			{
				string path = Path.Combine(ExePath + @"\WorkFlow\", "DataWorkFlow.json");
				JsonLoader.Serialize(path, WorkFlows);
			}
			catch (System.Exception)
			{

			}

		}
		/// <summary>Loads the entities and their structures from a JSON file.</summary>
		/// <returns>A list of EntityStructure objects representing the loaded tables.</returns>
		public List<EntityStructure> LoadTablesEntities()
		{
			string path = Path.Combine(ConfigPath, "DDLCreateTables.json");
			EntityCreateObjects = JsonLoader.DeserializeObject<EntityStructure>(path);
			return EntityCreateObjects;

		}
		/// <summary>Saves the table entities to a JSON file.</summary>
		/// <remarks>
		/// The table entities are serialized into a JSON file at the specified path.
		/// </remarks>
		/// <exception cref="System.IO.IOException">Thrown when an I/O error occurs while saving the file.</exception>
		public void SaveTablesEntities()
		{
			string path = Path.Combine(ConfigPath, "DDLCreateTables.json");
			JsonLoader.Serialize(path, EntityCreateObjects);
		}
		#endregion
		#region "DataTypes L/S"
		/// <summary>Writes the data type mapping to a JSON file.</summary>
		/// <param name="filename">The name of the file to write the data type mapping to. Default value is "DataTypeMapping".</param>
		/// <remarks>The data type mapping is serialized as JSON and written to the specified file.</remarks>
		public void WriteDataTypeFile(string filename = "DataTypeMapping")
		{
			string path = Path.Combine(ConfigPath, $"{filename}.json");
			JsonLoader.Serialize(path, DataTypesMap);


		}
		/// <summary>Reads a JSON file containing datatype mappings and returns a list of DatatypeMapping objects.</summary>
		/// <param name="filename">The name of the JSON file to read. Default value is "DataTypeMapping".</param>
		/// <returns>A list of DatatypeMapping objects representing the datatype mappings.</returns>
		public List<DatatypeMapping> ReadDataTypeFile(string filename = "DataTypeMapping")
		{
			string path = Path.Combine(ConfigPath, $"{filename}.json");
			DataTypesMap = JsonLoader.DeserializeObject<DatatypeMapping>(path);
			return DataTypesMap;
		}
		#endregion
		#region "Init Values"
		/// <summary>Initializes the connection configuration drivers.</summary>
		/// <returns>An object containing information about any errors that occurred during initialization.</returns>
		/// <remarks>
		/// This method loads the connection drivers definitions from a JSON file located at the specified path.
		/// If the file exists, it calls the LoadConnectionDriversDefinition method to load the definitions.
		/// If any errors occur during initialization, the ErrorObject is updated with the appropriate error information.
		/// </remarks>
		//private IErrorsInfo InitConnectionConfigDrivers()
		//{
		//	ErrorObject.Flag = Errors.Ok;
		//	try
		//	{
		//		string path = Path.Combine(ConfigPath, "DriversDefinitions.json");
		//		//if (File.Exists(path))
		//		//{
		//		//	LoadConnectionDriversDefinition();

		//		//}
		//		path = Path.Combine(ConfigPath, "ConnectionConfig.json");
		//		if (File.Exists(path))
		//		{
		//			LoadConnectionDriversConfigValues();
		//			//    Databasetypes = (DataSourceType)Enum.Parse(typeof(DataSourceType), );
		//		}
		//		else
		//			DataConnections = new List<ConnectionProperties>();
		//		SaveConnectionDriversConfigValues();

		//		//LoadConnectionDriversConfigValues
		//	}
		//	catch (Exception ex)
		//	{

		//		ErrorObject.Flag = Errors.Failed;
		//		ErrorObject.Ex = ex;
		//		ErrorObject.Message = ex.Message;
		//		Logger.WriteLog($"Error Initlization Lists ({ex.Message})");
		//	}
		//	return ErrorObject;
		//}
		/// <summary>Initializes the data source configuration drivers.</summary>
		/// <returns>An object containing information about any errors that occurred during initialization.</returns>
		/// <remarks>
		/// This method attempts to read the data source configuration drivers from a JSON file located at the specified path.
		/// If the file exists, it performs the necessary initialization steps.
		/// If any errors occur during initialization, an error object is returned with the appropriate error information.
		/// </remarks>
		private IErrorsInfo InitDataSourceConfigDrivers()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				string path = Path.Combine(ConfigPath, "DataSourceDriversConfig.json");
				if (File.Exists(path))
				{
					//  LoadDataSourceConfigValues();
					//    Databasetypes = (DataSourceType)Enum.Parse(typeof(DataSourceType), );
				}


			}
			catch (Exception ex)
			{

				ErrorObject.Flag = Errors.Failed;
				ErrorObject.Ex = ex;
				ErrorObject.Message = ex.Message;
				Logger.WriteLog($"Error Initlization Lists ({ex.Message})");
			}
			return ErrorObject;
		}
		/// <summary>Initializes the database types.</summary>
		/// <returns>An object containing information about any errors that occurred during initialization.</returns>
		/// <remarks>
		/// This method loads the database types from a JSON file located at the specified path.
		/// If the file exists, it calls the LoadDatabasesValues method to load the values.
		/// If any errors occur during initialization, the ErrorObject is updated with the appropriate information.
		/// </remarks>
		private IErrorsInfo InitDatabaseTypes()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				string path = Path.Combine(ConfigPath, "Databasetypes.json");
				if (File.Exists(path))
				{
					LoadDatabasesValues();
					//    Databasetypes = (DataSourceType)Enum.Parse(typeof(DataSourceType), );
				}
				else
					SaveDatabasesValues();


			}
			catch (Exception ex)
			{

				ErrorObject.Flag = Errors.Failed;
				ErrorObject.Ex = ex;
				ErrorObject.Message = ex.Message;
				Logger.WriteLog($"Error Initlization Lists ({ex.Message})");
			}
			return ErrorObject;
		}
		/// <summary>Initializes the SQL query types.</summary>
		/// <returns>An object containing error information.</returns>
		/// <remarks>
		/// This method attempts to initialize the SQL query types by reading a JSON file located at the specified path.
		/// If the file exists, it performs the necessary initialization steps.
		/// If an exception occurs during the initialization process, the error information is captured and returned.
		/// </remarks>
		private IErrorsInfo InitSqlquerytypes()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				string path = Path.Combine(ConfigPath, "Sqlquerytypes.json");
				if (File.Exists(path))
				{
					//  LoadQueryTypeValues();
				}
				else
					SaveQueryFile();

			}
			catch (Exception ex)
			{

				ErrorObject.Flag = Errors.Failed;
				ErrorObject.Ex = ex;
				ErrorObject.Message = ex.Message;
				Logger.WriteLog($"Error Initlization Sqlquerytypes ({ex.Message})");
			}
			return ErrorObject;
		}
		/// <summary>Initializes the mapping for error handling.</summary>
		/// <returns>An object containing information about any errors that occurred during initialization.</returns>
		private IErrorsInfo InitMapping()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				//string path = Path.Combine(ConfigPath, "Mapping.json");
				//if (File.Exists(path))
				//{
				//	LoadMappingValues();
				//}
				//path = Path.Combine(ConfigPath, "Map.json");
				//if (File.Exists(path))
				//{
				//	LoadMapsValues();
				//}
				string path = Path.Combine(ConfigPath, "CategoryFolders.json");
				if (File.Exists(path))
				{
					LoadCategoryFoldersValues();
				}
				else SaveCategoryFoldersValues();
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
		/// <summary>Initializes data connections.</summary>
		/// <returns>An object containing information about any errors that occurred during initialization.</returns>
		/// <remarks>
		/// This method initializes data connections by loading values from a JSON file located at the specified path.
		/// If the file exists, the method calls the LoadDataConnectionsValues() method to load the values.
		/// If any exceptions occur during initialization, the method sets the ErrorObject properties to indicate the error and logs an error message.
		/// The ErrorObject.Flag property is set to Errors.Ok if initialization is successful, or Errors.Failed if an error occurs.
		/// The ErrorObject.Ex property contains the exception that occurred, if any.
		/// The ErrorObject.Message property contains the error message from the exception
		private IErrorsInfo InitDataConnections()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				string path = Path.Combine(ConfigPath, "DataConnections.json");
				if (File.Exists(path))
				{
					LoadDataConnectionsValues();
				}
				else
					SaveDataconnectionsValues();

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
		/// <summary>Initializes the configuration.</summary>
		/// <returns>An object containing information about any errors that occurred during initialization.</returns>
		/// <remarks>
		/// This method sets the <see cref="ErrorObject.Flag"/> property to <see cref="Errors.Ok"/> before attempting to initialize the configuration.
		/// If the <see cref="ContainerName"/> property is not null, empty, or whitespace, additional initialization steps are performed.
		/// Any exceptions that occur during initialization are caught and handled by setting the <see cref="ErrorObject.Flag"/> property to <see cref="Errors.Failed"/>,
		/// setting the <see cref="ErrorObject.Ex"/> property to the caught exception, setting the <see cref="ErrorObject.Message"/> property to the
		private IErrorsInfo InitConfig()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{

				string exedir = ExePath;
				if (!string.IsNullOrEmpty(ContainerName) && !string.IsNullOrWhiteSpace(ContainerName))
				{
					//ContainerName = Path.Combine(exedir, ContainerName);
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
				else //if file does not exist first run
				{
					Config = new ConfigandSettings();
					Config.ExePath = ContainerName;
				}

				if (Config != null)
				{
					// Check if Application Folder Changed
					if (!Config.ExePath.Equals(exedir, StringComparison.InvariantCultureIgnoreCase))
					{
						Config = new ConfigandSettings();
						List<StorageFolders> folders = new List<StorageFolders>();
						foreach (StorageFolders fold in Config.Folders)
						{
							var dirName = new DirectoryInfo(fold.FolderPath).Name;
							folders.Add(new StorageFolders(Path.Combine(ContainerName, dirName), fold.FolderFilesType));

						}
						Config.ExePath = exedir;
						Config.Folders = folders;
					}
				}
				else
				{
					Config = new ConfigandSettings();
					Config.ExePath = ContainerName;
				}
				Config.ExePath = ContainerName;
				//Check Folders exist
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
					if (Config.ConfigPath == null)
					{
						Config.ConfigPath = Path.Combine(ContainerName, "Config");

					}
					if (ConfigPath == null)
					{
						ConfigPath = Config.ConfigPath;

					}
					if (Config.ScriptsPath == null)
					{
						Config.ScriptsPath = Path.Combine(ContainerName, "Scripts");

					}
					if (Config.ScriptsLogsPath == null)
					{
						Config.ScriptsLogsPath = Path.Combine(ContainerName, "Scripts\\Logs");

					}
					if (Config.ProjectDataPath == null)
					{
						Config.ProjectDataPath = Path.Combine(ContainerName, "ProjectData");

					}
					if (Config.DataViewPath == null)
					{
						Config.DataViewPath = Path.Combine(ContainerName, "DataViews");

					}
					if (Config.DataFilePath == null)
					{
						Config.DataFilePath = Path.Combine(ContainerName, "DataFiles");

					}
					if (Config.AddinPath == null)
					{
						Config.AddinPath = Path.Combine(ContainerName, "Addin");

					}
					if (Config.ClassPath == null)
					{
						Config.ClassPath = Path.Combine(ContainerName, "ProjectClasses");

					}
					if (Config.EntitiesPath == null)
					{
						Config.EntitiesPath = Path.Combine(ContainerName, "Entities");

					}
					if (Config.GFXPath == null)
					{
						Config.GFXPath = Path.Combine(ContainerName, "GFX");

					}
					if (Config.MappingPath == null)
					{
						Config.MappingPath = Path.Combine(ContainerName, "Mapping");

					}
					if (Config.OtherDLLPath == null)
					{
						Config.OtherDLLPath = Path.Combine(ContainerName, "OtherDll");

					}
					if (Config.WorkFlowPath == null)
					{
						Config.WorkFlowPath = Path.Combine(ContainerName, "WorkFlow");

					}
				}
				CreateDirConfig(Path.Combine(ContainerName, "Config"), FolderFileTypes.Config);
				CreateDirConfig(Path.Combine(ContainerName, "ConnectionDrivers"), FolderFileTypes.ConnectionDriver);
				CreateDirConfig(Path.Combine(ContainerName, "DataSources"), FolderFileTypes.DataSources);
				CreateDirConfig(Path.Combine(ContainerName, "LoadingExtensions"), FolderFileTypes.LoaderExtensions);


				if (Config.ConfigPath == null)
				{
					Config.ConfigPath = Path.Combine(ContainerName, "Config");

				}
				if (ConfigPath == null)
				{
					ConfigPath = Config.ConfigPath;

				}
				if (Config.LoaderExtensionsPath == null)
				{
					Config.LoaderExtensionsPath = Path.Combine(ContainerName, "LoadingExtensions");

				}
				if (Config.ConnectionDriversPath == null)
				{
					Config.ConnectionDriversPath = Path.Combine(ContainerName, "ConnectionDrivers");

				}
				if (Config.DataSourcesPath == null)
				{
					Config.DataSourcesPath = Path.Combine(ContainerName, "DataSources");

				}
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
		/// <summary>Creates a directory at the specified path if it doesn't already exist.</summary>
		/// <param name="path">The path of the directory to create.</param>
		public void CreateDir(string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);

			}

		}
		/// <summary>Creates a directory configuration.</summary>
		/// <param name="path">The path of the directory.</param>
		/// <param name="foldertype">The type of the folder.</param>
		/// <remarks>
		/// This method creates a directory at the specified path if it doesn't already exist.
		/// It then adds the directory to the list of folder configurations if it's not already present.
		/// </remarks>
		public void CreateDirConfig(string path, FolderFileTypes foldertype)
		{
			CreateDir(path);


			if (!Config.Folders.Any(item => item.FolderPath.Equals(@path, StringComparison.InvariantCultureIgnoreCase)))
			{
				Config.Folders.Add(new StorageFolders(path, foldertype));
			}
		}
		#endregion "Init Values"
		#region "util"
		/// <summary>Creates a string representing file extensions.</summary>
		/// <returns>A string representing file extensions in the format "FileType1 (*.ext1)|*.ext1|FileType2 (*.ext2)|*.ext2|...|All files (*.*)|*.*".</returns>
		public string CreateFileExtensionString()
		{
			List<ConnectionDriversConfig> clss = DataDriversClasses.Where(p => p.extensionstoHandle != null).ToList();
			string retval = null;
			if (clss != null)
			{
				IEnumerable<string> extensionslist = clss.Select(p => p.extensionstoHandle);
				string extstring = string.Join(",", extensionslist);
				List<string> exts = extstring.Split(',').Distinct().ToList();

				foreach (string item in exts)
				{
					retval += item + " files(*." + item + ")|*." + item + "|";


				}

			}

			retval += "All files(*.*)|*.*";
			return retval;
		}

		#endregion
		#region "Projects L/S"
		public void ReadProjects()
		{
			try
			{
				string path = Path.Combine(ConfigPath, "Projects.json");
				Projects = JsonLoader.DeserializeObject<RootFolder>(path);

			}
			catch (System.Exception)
			{
			}
		}
		public void SaveProjects()
		{
			try
			{
				string path = Path.Combine(ConfigPath, "Projects.json");
				JsonLoader.Serialize(path, Projects);
			}
			catch (System.Exception)
			{

			}

		}

		#endregion
		#region"Defaults"

		/// <summary>Retrieves the default values for a given data source.</summary>
		/// <param name="DMEEditor">The IDMEEditor instance.</param>
		/// <param name="DatasourceName">The name of the data source.</param>
		/// <returns>A list of DefaultValue objects representing the default values for the data source.</returns>
		/// <remarks>
		/// This method retrieves the default values for a given data source by accessing the ConnectionProperties object
		/// associated with the data source in the ConfigEditor's DataConnections collection. If the data source is found,
		/// the method returns the default values. If the data source is not found, an error message is logged and an empty
		/// list is returned.
		/// </remarks
		public List<DefaultValue> Getdefaults(IDMEEditor DMEEditor, string DatasourceName)
		{
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
		/// <summary>Saves default values for a data source in the DMEEditor.</summary>
		/// <param name="DMEEditor">The DMEEditor instance.</param>
		/// <param name="defaults">A list of DefaultValue objects representing the default values.</param>
		/// <param name="DatasourceName">The name of the data source.</param>
		/// <returns>An IErrorsInfo object indicating the result of the operation.</returns>
		/// <remarks>
		/// This method saves the default values for a specific data source in the DMEEditor. It first checks if the data source exists in the configuration editor's data connections. If it exists, it saves the data connections values using the SaveDataconnectionsValues method. If the data source
		public IErrorsInfo Savedefaults(IDMEEditor DMEEditor, List<DefaultValue> defaults, string DatasourceName)
		{
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
		#endregion"Defaults"
		//----------------------------------------------------------------------------------------------
		/// <summary>Initializes the application.</summary>
		/// <returns>An object containing information about any errors that occurred during initialization.</returns>
		/// <remarks>
		/// This method initializes various configuration settings, connection drivers, data source drivers, and database types.
		/// If an exception occurs during initialization, the method sets the ErrorObject's Flag to indicate failure,
		/// stores the exception in the ErrorObject's Ex property, and logs an error message.
		/// </remarks>
		public IErrorsInfo Init()
		{
			ErrorObject.Flag = Errors.Ok;
			Logger.WriteLog($"Initlization Values and Lists");
			try
			{
				InitConfig();
			//	InitConnectionConfigDrivers();
			//	InitDataSourceConfigDrivers();
			//	InitDatabaseTypes();
		//		InitQueryList();
			//	InitSqlquerytypes();
				InitDataConnections();
				LoadFucntion2Function();
				LoadEvents();
				LoadCompositeLayersValues();
				//LoadAppValues();
				LoadReportsValues();
				LoadReportsDefinitionValues();
				ReadWork();
				LoadObjectTypes();
				//	LoadMappingSchema();
				//ReadDataTypeFile();
				ReadProjects();
				//ReadSyncDataSource();
				InitMapping();
				SaveLocation();

			}
			catch (Exception ex)
			{

				ErrorObject.Flag = Errors.Failed;
				ErrorObject.Ex = ex;
				ErrorObject.Message = ex.Message;
				Logger.WriteLog($"Error Initlization Lists ({ex.Message})");
			}
			return ErrorObject;
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
					QueryList = null;
					//DriverDefinitionsConfig = null;
					DataConnections = null;
					WorkFlows = null;
					CategoryFolders = null;
					BranchesClasses = null;
					GlobalFunctions = null;
					AppWritersClasses = null;
					AppComponents = null;
					ReportWritersClasses = null;
					PrintManagers = null;
					DataSourcesClasses = null;
					WorkFlowActions = null;
					WorkFlowEditors = null;
					WorkFlowSteps = null;
					WorkFlowStepEditors = null;
					FunctionExtensions = null;
					Addins = null;
					Others = null;
					Rules = null;
					AddinTreeStructure = null;
					Function2Functions = null;
					objectTypes = null;
					Events = null;
					ReportsDefinition = null;
					Reportslist = null;
					AIScriptslist = null;
					CompositeQueryLayers = null;
					EntityCreateObjects = null;
					DataTypesMap = null;
					Entities = null;
					DataDriversClasses = null;

				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~ConfigEditor()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
