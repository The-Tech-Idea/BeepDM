

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
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Logger;

namespace TheTechIdea.Util
{
	public class ConfigEditor : IConfigEditor
	{
		public ConfigEditor(IDMLogger logger, IErrorsInfo per, IJsonLoader jsonloader,string folderpath=null, string containerfolder = null)
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
			Init();

		}
		public string ContainerName { get; set; } 
		public IErrorsInfo ErrorObject { get; set; }
		public IJsonLoader JsonLoader { get; set; }
		public ConfigandSettings Config { get; set; } = new ConfigandSettings();
		public IDMLogger Logger { get; set; }
		public List<string> Databasetypes { get; set; }
		public List<QuerySqlRepo> QueryList { get; set; } = new List<QuerySqlRepo>();
		public List<ConnectionDriversConfig> DriverDefinitionsConfig { get; set; } = new List<ConnectionDriversConfig>();
		public List<ConnectionProperties> DataConnections { get; set; } = new List<ConnectionProperties>(); //DataSourceConnectionConfig

		public List<WorkFlow> WorkFlows { get; set; } = new List<WorkFlow>();
		public List<CategoryFolder> CategoryFolders { get; set; } = new List<CategoryFolder>();
		public List<AssemblyClassDefinition> BranchesClasses { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> GlobalFunctions { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> AppWritersClasses { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> AppComponents { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> ReportWritersClasses { get; set; } = new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> PrintManagers { get; set; }=new List<AssemblyClassDefinition>();
		public List<AssemblyClassDefinition> DataSourcesClasses { get; set; } = new List<AssemblyClassDefinition>();
        public List<AssemblyClassDefinition> WorkFlowActions { get; set; }=new List<AssemblyClassDefinition> { };
        public List<AssemblyClassDefinition> WorkFlowEditors { get; set; }=new List<AssemblyClassDefinition>();
        public List<AssemblyClassDefinition> WorkFlowSteps { get; set; }=new	List<AssemblyClassDefinition>();
        public List<AssemblyClassDefinition> WorkFlowStepEditors { get; set; } = new List<AssemblyClassDefinition>();
        public List<AssemblyClassDefinition> FunctionExtensions { get; set; } = new List<AssemblyClassDefinition>();
        public List<AssemblyClassDefinition> Addins { get; set; } = new List<AssemblyClassDefinition>();
        public List<AssemblyClassDefinition> Others { get; set; } = new List<AssemblyClassDefinition>();
        public List<AssemblyClassDefinition> Rules { get; set; } = new List<AssemblyClassDefinition>();
        public List<AddinTreeStructure> AddinTreeStructure { get; set; } = new List<AddinTreeStructure>();
		public List<Function2FunctionAction> Function2Functions { get; set; } = new List<Function2FunctionAction>();
		public List<ObjectTypes> objectTypes { get; set; } = new List<ObjectTypes>();
		public List<Event> Events { get; set; } = new List<Event>();
		public List<AppTemplate> ReportsDefinition { get; set; } = new List<AppTemplate>();
		public List<ReportsList> Reportslist { get; set; } = new List<ReportsList>();
		public List<ReportsList> AIScriptslist { get; set; } = new List<ReportsList>();
		public List<CompositeLayer> CompositeQueryLayers { get; set; } = new List<CompositeLayer>();
		public List<EntityStructure> EntityCreateObjects { get; set; } = new List<EntityStructure>();
		public List<DatatypeMapping> DataTypesMap { get; set; } = new List<DatatypeMapping>();
	//	public List<DataSourceFieldProperties> AppfieldProperties { get; set; } = new List<DataSourceFieldProperties>();
		public Dictionary<string, string> Entities { get; set; } = new Dictionary<string, string>();
		public List<ETLScriptHDR> Scripts { get; set; } = new List<ETLScriptHDR>();
		public List<ETLScriptHDR> SyncedDataSources { get; set; } = new List<ETLScriptHDR>();
		public List<ConnectionDriversConfig> DataDriversClasses { get; set; } = new List<ConnectionDriversConfig>();
		
		public string ExePath { get; set; } // System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); //System.Reflection.Assembly.GetExecutingAssembly().Location
		public string ConfigPath { get; set; } 
		
		public List<Assembly> LoadedAssemblies { get; set; } = new List<Assembly>();
		#region "Scripts and logs L/S"
		public void SaveScriptsValues()
		{
			string path = Path.Combine(Config.ScriptsPath, "Scripts.json");
			JsonLoader.Serialize(path, Scripts);

		}
		public List<ETLScriptHDR> LoadScriptsValues()
		{
			string path = Path.Combine(Config.ScriptsPath, "Scripts.json");
			Scripts = JsonLoader.DeserializeObject<ETLScriptHDR>(path);
			return Scripts;
		}
		public void SaveScriptTrackingValues(SyncErrorsandTracking scriptid)
		{
			string dateformat = scriptid.rundate.ToString("ddmmyyyyHmmss");
			string path = Path.Combine(Config.ScriptsLogsPath, $"{dateformat}_{scriptid.parentscriptid}.json");
			JsonLoader.Serialize(path, Scripts);

		}
		public SyncErrorsandTracking LoadScriptTrackingValues(SyncErrorsandTracking scriptid)
		{
			string dateformat = scriptid.rundate.ToString("ddmmyyyyHmmss");
			string path = Path.Combine(Config.ScriptsLogsPath, $"{dateformat}_{scriptid.parentscriptid}.json");
			
			return JsonLoader.DeserializeSingleObject<SyncErrorsandTracking>(path); ;
		}
		#endregion "Reports L/S"
		#region "Reading and writing Query files"
		public string GetSql(Sqlcommandtype CmdType, string TableName, string SchemaName, string Filterparamters, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType) //string TableName,string SchemaName
		{
			var ret = (from a in QueryList
					   where a.DatabaseType == DatabaseType
					   where a.Sqltype == CmdType
					   select a.Sql).FirstOrDefault();


			return String.Format(ret, TableName, SchemaName, Filterparamters); ;

		}
		public string GetSqlFromCustomQuery(Sqlcommandtype CmdType, string TableName, string customquery, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType) //string TableName,string SchemaName
		{
			var ret = (from a in QueryList
					   where a.DatabaseType == DatabaseType
					   where a.Sqltype == CmdType
					   select a.Sql).FirstOrDefault();


			return String.Format(ret, TableName); ;

		}
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
		public void SaveQueryFile()
		{
			string path = Path.Combine(ConfigPath, "QueryList.json");
			JsonLoader.Serialize(path, QueryList);
		}
		public List<QuerySqlRepo> LoadQueryFile()
		{
			string path = Path.Combine(ConfigPath, "QueryList.json");
			QueryList = JsonLoader.DeserializeObject<QuerySqlRepo>(path);
			return QueryList;
		}

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
		public DatasourceEntities LoadDataSourceEntitiesValues(string dsname)
		{
			string path = Path.Combine(ExePath+@"\Entities\", dsname +"_entities.json");
		

			return JsonLoader.DeserializeSingleObject<DatasourceEntities>(path);

		}
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
		public void SaveDataSourceEntitiesValues(DatasourceEntities datasourceEntities)
		{
			string path = Path.Combine(ExePath + @"\Entities\", datasourceEntities.datasourcename + "_entities.json");
			JsonLoader.Serialize(path, datasourceEntities);
		}
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
		public void SaveEntityStructure(string filepath, EntityStructure entity)
		{
			string filename = entity.DataSourceID + "^" + entity.EntityName + "_ES.json";
			string path = Path.Combine(filepath, filename);
			JsonLoader.Serialize(path, entity);
			
		}
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
		public void SaveMappingSchemaValue(string schemaname,  Map_Schema mapping_Rep)
		{
			
			string path = Path.Combine(Config.MappingPath, $"{schemaname}_Mapping.json");
			JsonLoader.Serialize(path, mapping_Rep);

			
		}
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
		public void SaveMappingValues(string Entityname, string datasource, EntityDataMap mapping_Rep)
		{
			CreateDir(Path.Combine(Config.MappingPath, datasource));
			string path = Path.Combine(Path.Combine(Config.MappingPath, datasource), $"{Entityname}_Mapping.json");
			JsonLoader.Serialize(path, mapping_Rep);
		}
		public EntityDataMap LoadMappingValues(string Entityname,string datasource)
		{
          
			string path = Path.Combine(Path.Combine(Config.MappingPath, datasource), $"{Entityname}_Mapping.json");

			EntityDataMap Mappings = JsonLoader.DeserializeSingleObject<EntityDataMap>(path);
			return Mappings;
		
		}
		#endregion
		#region "Data Connections L/S"
		public void SaveDataconnectionsValues()
		{
			string path = Path.Combine(ConfigPath, "DataConnections.json");
			JsonLoader.Serialize(path, DataConnections);
		 
		}
		public List<ConnectionProperties> LoadDataConnectionsValues()
		{
			string path = Path.Combine(ConfigPath, "DataConnections.json");
			DataConnections = JsonLoader.DeserializeObject<ConnectionProperties>(path);
			
			return DataConnections;
		}
		public bool DataConnectionExist(string ConnectionName)
		{
			return DataConnections.Any(x => x.ConnectionName.Equals(ConnectionName, StringComparison.InvariantCultureIgnoreCase));
		}
		public bool AddDataConnection(ConnectionProperties cn)
		{ try

			{
				if (!DataConnectionExist(cn.ConnectionName))
				{
					DataConnections.Add(cn);
					return true;
				}
				else
				{
					return false;
				}


			   
			}
			catch (Exception )
			{   return false;
			}
		   
		

		}
		public bool UpdateDataConnection(ConnectionProperties conn, string category)
		{

			try
			{
				int idx = DataConnections.FindIndex(0, p => p.ID == conn.ID);
				if (idx == -1)
				{
                    DataConnections.Add(conn);
                }else
					DataConnections[idx] = conn;
                //var cnlist = DataConnections.Where(x => x.Category.ToString().Equals(category,StringComparison.InvariantCultureIgnoreCase)).ToList();
                //foreach (ConnectionProperties dt in cnlist)
                //{
                //	ConnectionProperties dc = ls.Where(x => x.ConnectionName.Equals(dt.ConnectionName,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                //	if (dc == null)

                //	{
                //		DataConnections.Remove(dt);
                //	}
                //}
                //foreach (ConnectionProperties item in ls)
                //{
                //	ConnectionProperties dc = DataConnections.Where(x => x.ConnectionName.Equals(item.ConnectionName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                //	if (dc != null)
                //	{
                //		dc = item;
                //	}
                //	else
                //	{
                //		DataConnections.Add(item);
                //	}
                //}


            }
			catch (Exception )
			{

				return false;
			};
			return true;
		}
		public bool RemoveConnByName(string pname)
		{

			int i = DataConnections.FindIndex(x => x.ConnectionName.Equals(pname, StringComparison.InvariantCultureIgnoreCase));

			return DataConnections.Remove(DataConnections[i]);
		}
		public bool RemoveConnByID(int ID)
		{

			int i = DataConnections.FindIndex(x => x.ID == ID);

			return DataConnections.Remove(DataConnections[i]);
		}
		public bool RemoveDataConnection(string pname)
		{

			try
			{
				ConnectionProperties dc = DataConnections.Where(x => x.ConnectionName.Equals(pname, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
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
			catch (Exception )
			{

				return null;
			}


		}
		public bool RemoveFolderCategory(string pfoldername, string prootname)
		{
			try
			{
				CategoryFolder x = CategoryFolders.Where(y => y.FolderName.Equals(pfoldername,StringComparison.InvariantCultureIgnoreCase) && y.RootName.Equals( prootname,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
				CategoryFolders.Remove(x);
				SaveCategoryFoldersValues();
				return true;
			}
			catch (Exception )
			{

				return false;
			}


		}
		public void LoadCategoryFoldersValues()
		{
			string path = Path.Combine(ConfigPath, "CategoryFolders.json");
			CategoryFolders = JsonLoader.DeserializeObject<CategoryFolder>(path);
		}
		public void SaveCategoryFoldersValues()
		{
			string path = Path.Combine(ConfigPath, "CategoryFolders.json");
			JsonLoader.Serialize(path, CategoryFolders);
		}
		public List<ConnectionDriversConfig> LoadConnectionDriversConfigValues()
		{
			string path = Path.Combine(ConfigPath, "ConnectionConfig.json");
			DataDriversClasses = JsonLoader.DeserializeObject<ConnectionDriversConfig>(path);
			return DataDriversClasses;
		}
		public void SaveConnectionDriversConfigValues()
		{
			string path = Path.Combine(ConfigPath, "ConnectionConfig.json");
			JsonLoader.Serialize(path, DataDriversClasses);
		}
		public List<ConnectionDriversConfig> LoadConnectionDriversDefinition()
		{
			string path = Path.Combine(ConfigPath, "DriversDefinitions.json");
			DriverDefinitionsConfig = JsonLoader.DeserializeObject<ConnectionDriversConfig>(path);
			
			return DriverDefinitionsConfig;
			//  QueryList = ReadQueryFile("QueryList.json");
		}
		public void SaveConnectionDriversDefinitions()
		{
			string path = Path.Combine(ConfigPath, "DriversDefinitions.json");
			JsonLoader.Serialize(path, DriverDefinitionsConfig);
		}
		public void SaveDatabasesValues()
		{
			string path = Path.Combine(ConfigPath, "Databasetypes.json");
			JsonLoader.Serialize(path, Databasetypes);
		}
		public void LoadDatabasesValues()
		{
			string path = Path.Combine(ConfigPath, "Databasetypes.json");
			Databasetypes = JsonLoader.DeserializeObject<string>(path);
		}
		//--
		public void SaveEvents()
		{
			string path = Path.Combine(ConfigPath, "events.json");
			JsonLoader.Serialize(path, Events);
		}
		public void LoadEvents()
		{
			string path = Path.Combine(ConfigPath, "events.json");
			Events = JsonLoader.DeserializeObject<Event>(path);
			
		}
		public void SaveFucntion2Function()
		{

			string path = Path.Combine(ConfigPath, "Function2Function.json");
			JsonLoader.Serialize(path, Function2Functions);
		}
		public void LoadFucntion2Function()
		{
			string path = Path.Combine(ConfigPath, "Function2Function.json");
			Function2Functions = JsonLoader.DeserializeObject<Function2FunctionAction>(path);
			
		}
		public void LoadAddinTreeStructure()
		{
			string path = Path.Combine(ConfigPath, "AddinTreeStructure.json");
			AddinTreeStructure = JsonLoader.DeserializeObject<AddinTreeStructure>(path);
			
		}
		public void SaveAddinTreeStructure()
		{

			string path = Path.Combine(ConfigPath, "AddinTreeStructure.json");
			JsonLoader.Serialize(path, AddinTreeStructure);
		


		}
		#endregion "Configuration L/S"
		#region "Reports L/S"
		public void SaveAIScriptsValues()
		{
			string path = Path.Combine(ConfigPath, "AIScripts.json");
			JsonLoader.Serialize(path, AIScriptslist);

		}
		public List<ReportsList> LoadAIScriptsValues()
		{
			string path = Path.Combine(ConfigPath, "AIScripts.json");
			AIScriptslist = JsonLoader.DeserializeObject<ReportsList>(path);
			return AIScriptslist;
		}
		public void SaveReportsValues()
		{
			string path = Path.Combine(ConfigPath, "Reportslist.json");
			JsonLoader.Serialize(path, Reportslist);
			
		}
		public List<ReportsList> LoadReportsValues()
		{
			string path = Path.Combine(ConfigPath, "Reportslist.json");
			Reportslist = JsonLoader.DeserializeObject<ReportsList>(path);
			return Reportslist;
		}
		public void SaveReportDefinitionsValues()
		{
			string path = Path.Combine(ConfigPath, "reportsDefinition.json");
			JsonLoader.Serialize(path, ReportsDefinition);

		}
		public List<AppTemplate> LoadReportsDefinitionValues()
		{
			string path = Path.Combine(ConfigPath, "reportsDefinition.json");
			ReportsDefinition = JsonLoader.DeserializeObject<AppTemplate>(path);
			return ReportsDefinition;
		}
		#endregion "Reports L/S"
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
		public bool RemoveLayerByName(string LayerName)
		{
			
			int i = CompositeQueryLayers.FindIndex(x => x.LayerName.Equals(LayerName,StringComparison.InvariantCultureIgnoreCase));
			if (i > -1)
			{
				return CompositeQueryLayers.Remove(CompositeQueryLayers[i]);
			}
			else
				return true;
		
		}
		public bool RemoveLayerByID(string ID)
		{
			
			int i = CompositeQueryLayers.FindIndex(x => x.ID.Equals(ID,StringComparison.InvariantCultureIgnoreCase));
			if (i > -1)
			{
				return CompositeQueryLayers.Remove(CompositeQueryLayers[i]);
			}
			else
				return true;

		}
		public void SaveCompositeLayersValues()
		{
			string path = Path.Combine(ConfigPath, "CompositeLayers.json");
			JsonLoader.Serialize(path, CompositeQueryLayers);

		}
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
		public void SaveObjectTypes()
		{
			string path = Path.Combine(ConfigPath, "ObjectTypes.json");
			JsonLoader.Serialize(path, objectTypes);
		}
		public List<ObjectTypes> LoadObjectTypes()
		{
			string path = Path.Combine(ConfigPath, "ObjectTypes.json");
			objectTypes = JsonLoader.DeserializeObject<ObjectTypes>(path);
			return objectTypes;
		}
		#endregion
		#region "WorkFlows L/S"
		public void ReadWork()
		{
			try
			{
				string path = Path.Combine(ExePath + @"\WorkFlow\", "DataWorkFlow.json");
				WorkFlows = JsonLoader.DeserializeObject<WorkFlow>(path);
		
			}
			catch (System.Exception )
			{
			}
		}
		public void SaveWork()
		{
			try
			{
				string path = Path.Combine(ExePath + @"\WorkFlow\", "DataWorkFlow.json");
				JsonLoader.Serialize(path, WorkFlows);
			}
			catch (System.Exception )
			{
				
			}
			
		}
		public List<EntityStructure> LoadTablesEntities()
		{
			string path = Path.Combine(ConfigPath, "DDLCreateTables.json");
			 EntityCreateObjects=JsonLoader.DeserializeObject<EntityStructure>(path);
			return EntityCreateObjects;

		}
		public void SaveTablesEntities()
		{
				string path = Path.Combine(ConfigPath, "DDLCreateTables.json");
				JsonLoader.Serialize(path, EntityCreateObjects);
		}
		#endregion
		#region "DataTypes L/S"
		public void WriteDataTypeFile(string filename = "DataTypeMapping")
		{
			string path = Path.Combine(ConfigPath, $"{filename}.json");
			JsonLoader.Serialize(path, DataTypesMap);


		}
		public List<DatatypeMapping> ReadDataTypeFile(string filename = "DataTypeMapping")
		{
			string path = Path.Combine(ConfigPath, $"{filename}.json");
			DataTypesMap = JsonLoader.DeserializeObject<DatatypeMapping>(path);
			return DataTypesMap;
		}
		#endregion
		#region "SyncDataSource L/S"
		public void WriteSyncDataSource(string filename = "SyncDataSource")
		{
			string path = Path.Combine(ConfigPath, $"{filename}.json");
			JsonLoader.Serialize(path, SyncedDataSources);


		}
		public List<ETLScriptHDR> ReadSyncDataSource(string filename = "SyncDataSource")
		{
			string path = Path.Combine(ConfigPath, $"{filename}.json");
			SyncedDataSources = JsonLoader.DeserializeObject<ETLScriptHDR>(path);
			return SyncedDataSources;
		}
		#endregion
		#region "Init Values"
		private IErrorsInfo InitConnectionConfigDrivers()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				string path = Path.Combine(ConfigPath, "DriversDefinitions.json");
				if (File.Exists(path))
				{
					LoadConnectionDriversDefinition();

				}
				path = Path.Combine(ConfigPath, "ConnectionConfig.json");
				if (File.Exists(path))
				{
					LoadConnectionDriversConfigValues();
					//    Databasetypes = (DataSourceType)Enum.Parse(typeof(DataSourceType), );
				}
				else
					DataConnections = new List<ConnectionProperties>();
					SaveConnectionDriversConfigValues();

				//LoadConnectionDriversConfigValues
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
		private IErrorsInfo InitSqlquerytypes()
		{
			ErrorObject.Flag = Errors.Ok;
			try
			{
				string path = Path.Combine(ConfigPath, "Sqlquerytypes.json");
				if (File.Exists(path))
				{
					//  LoadQueryTypeValues();
				}else
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
				
					if (!Config.ExePath.Equals(exedir,StringComparison.InvariantCultureIgnoreCase))
					{
						Config = new ConfigandSettings();
                        List<StorageFolders> folders = new List<StorageFolders>();
                        foreach (StorageFolders fold in Config.Folders)
                        {
                            var dirName = new DirectoryInfo(fold.FolderPath).Name;
                            folders.Add(new StorageFolders(Path.Combine(ContainerName, dirName), fold.FolderFilesType));
                            //fold.FolderPath = Path.Combine(containerpath, dirName);
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
				//Config.SystemEntryFormName = @"Frm_MainDisplayForm";
				//Check Folders exist
				CreateDirConfig(Path.Combine(ContainerName, "Config"), FolderFileTypes.Config);
				CreateDirConfig(Path.Combine(ContainerName, "Addin"), FolderFileTypes.Addin);
				CreateDirConfig(Path.Combine(ContainerName, "DataFiles"), FolderFileTypes.DataFiles);
				CreateDirConfig(Path.Combine(ContainerName, "DataViews"), FolderFileTypes.DataView);
				CreateDirConfig(Path.Combine(ContainerName, "ProjectData"), FolderFileTypes.ProjectData);
				CreateDirConfig(Path.Combine(ContainerName, "ProjectClasses"), FolderFileTypes.ProjectClass);
				CreateDirConfig(Path.Combine(ContainerName, "ConnectionDrivers"), FolderFileTypes.ConnectionDriver);
				CreateDirConfig(Path.Combine(ContainerName, "GFX"), FolderFileTypes.GFX);
				CreateDirConfig(Path.Combine(ContainerName, "OtherDll"), FolderFileTypes.OtherDLL);
				CreateDirConfig(Path.Combine(ContainerName, "Entities"), FolderFileTypes.Entities);
				CreateDirConfig(Path.Combine(ContainerName, "Mapping"), FolderFileTypes.Mapping);
				CreateDirConfig(Path.Combine(ContainerName, "WorkFlow"), FolderFileTypes.WorkFlows);
				CreateDirConfig(Path.Combine(ContainerName, "Scripts"), FolderFileTypes.Scripts);
				CreateDirConfig(Path.Combine(ContainerName, "Scripts\\Logs"), FolderFileTypes.ScriptsLogs);
				CreateDirConfig(Path.Combine(ContainerName, "AI"), FolderFileTypes.Scripts);
				CreateDirConfig(Path.Combine(ContainerName, "Reports"), FolderFileTypes.Reports);
				
				Config.ExePath = ContainerName;
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
                if (Config.ConnectionDriversPath == null)
				{
					Config.ConnectionDriversPath = Path.Combine(ContainerName, "ConnectionDrivers");

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
		public void CreateDir(string path)
        {
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);

			}

		}
		public void CreateDirConfig(string path, FolderFileTypes foldertype)
		{
			CreateDir(path);


			if (!Config.Folders.Any(item => item.FolderPath.Equals(@path,StringComparison.InvariantCultureIgnoreCase)))
			{
				Config.Folders.Add(new StorageFolders(path, foldertype));
			}
		}
		#endregion "Init Values"
		#region "util"
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
					retval += item + " files(*." + item + ")|*."+item+"|";


				}

			}
			
			retval += "All files(*.*)|*.*";
			return retval;
		}
		
		#endregion
		//----------------------------------------------------------------------------------------------
		public IErrorsInfo Init()
		{
			ErrorObject.Flag = Errors.Ok;
			Logger.WriteLog($"Initlization Values and Lists");
			try
			{
				InitConfig();
				InitConnectionConfigDrivers();
				InitDataSourceConfigDrivers();
				InitDatabaseTypes();
				InitQueryList();
				InitSqlquerytypes();
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
				ReadDataTypeFile();
				//ReadSyncDataSource();
				InitMapping();
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

	}
}
