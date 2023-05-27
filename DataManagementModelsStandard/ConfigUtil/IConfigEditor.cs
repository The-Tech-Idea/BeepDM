using System.Collections.Generic;
using TheTechIdea.Beep.CompositeLayer;
using System.Reflection;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Report;

using TheTechIdea.Beep.DataBase;

using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.AppManager;
using System.IO;
using TheTechIdea.Beep.FileManager;

namespace TheTechIdea.Util
{
    public interface IConfigEditor
    {
        bool IsLoaded { get;  }
        ConfigandSettings Config { get; set; }
        string ConfigPath { get; set; }
        IJsonLoader JsonLoader { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        string ExePath { get; set; }
        IDMLogger Logger { get; set; }
        List<string> Databasetypes { get; set; }
        string ContainerName { get; set; }
        List<ConnectionProperties> DataConnections { get; set; }
        List<WorkFlow> WorkFlows { get; set; }
        List<QuerySqlRepo> QueryList { get; set; }
        List<ObjectTypes> objectTypes { get; set; }

        List<ConnectionDriversConfig> DataDriversClasses { get; set; }
        List<ConnectionDriversConfig> DriverDefinitionsConfig { get; set; }
        List<AssemblyClassDefinition> DataSourcesClasses { get; set; }
        List<AssemblyClassDefinition> BranchesClasses { get; set; }
         List<AssemblyClassDefinition> AppWritersClasses { get; set; }
        List<AssemblyClassDefinition> GlobalFunctions { get; set; }
        List<AssemblyClassDefinition> AppComponents { get; set; }
        List<AssemblyClassDefinition> ReportWritersClasses { get; set; }
        List<AssemblyClassDefinition> WorkFlowActions { get; set; }
        List<AssemblyClassDefinition> WorkFlowEditors { get; set; }
        List<AssemblyClassDefinition> WorkFlowSteps { get; set; }
        List<AssemblyClassDefinition> WorkFlowStepEditors { get; set; }
        List<AssemblyClassDefinition> FunctionExtensions { get; set; }
        List<AssemblyClassDefinition> Addins { get; set; } 
        List<AssemblyClassDefinition> Rules { get; set; }
        List<AssemblyClassDefinition> PrintManagers { get; set; }
        List<CategoryFolder> CategoryFolders { get; set; }
        List<Assembly> LoadedAssemblies { get; set; }
        List<Function2FunctionAction> Function2Functions { get; set; }
        List<Event> Events { get; set; }
        List<AddinTreeStructure> AddinTreeStructure { get; set; }
        List<AppTemplate> ReportsDefinition { get; set; }
        List<ReportsList> Reportslist { get; set; }
        List<ReportsList> AIScriptslist { get; set; }
        List<CompositeLayer> CompositeQueryLayers { get; set; }
        List<EntityStructure> EntityCreateObjects { get; set; }
        List<DatatypeMapping> DataTypesMap { get; set; }
        List<ETLScriptHDR> SyncedDataSources { get; set; }
        List<IProjectFolder> Projects { get; set; }
        List<DatatypeMapping> ReadDataTypeFile(string filename = "DataTypeMapping");
        void WriteDataTypeFile(string filename = "DataTypeMapping");
        string GetSql(Sqlcommandtype CmdType, string TableName, string SchemaName, string Filterparamters, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType);
        string GetSqlFromCustomQuery(Sqlcommandtype CmdType, string TableName, string customquery, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType);
        IErrorsInfo Init();
        void ReadProjects();
        void SaveProjects();
        List<QuerySqlRepo> InitQueryDefaultValues();
        ConfigandSettings LoadConfigValues();
        void SaveConfigValues();
        void LoadDatabasesValues();
        void SaveDatabasesValues();
        List<ConnectionProperties> LoadDataConnectionsValues();
        void SaveDataconnectionsValues();
        bool DataConnectionExist(string ConnectionName);

        bool AddDataConnection(ConnectionProperties cn);
        bool RemoveDataConnection(string pname);
         bool RemoveConnByName(string pname);

         bool RemoveConnByID(int ID);
      
        bool UpdateDataConnection(ConnectionProperties ls, string category);
        List<ConnectionDriversConfig> LoadConnectionDriversConfigValues();
        void SaveConnectionDriversConfigValues();
        void SaveReportsValues();
        List<ReportsList> LoadReportsValues();
        List<QuerySqlRepo> LoadQueryFile();
        void SaveQueryFile();
        void SaveMappingValues(string Entityname, string datasource, EntityDataMap mapping_Rep);
        EntityDataMap LoadMappingValues(string Entityname, string datasource);

        void SaveMappingSchemaValue(string schemaname,  Map_Schema mapping_Rep);
        Map_Schema LoadMappingSchema(string schemaname );
       // void SaveMappingSchemaValue();
       // void LoadMappingSchema();
        //void SaveMapsValues();
        // void LoadMapsValues();
         List<ConnectionDriversConfig> LoadConnectionDriversDefinition();
         void SaveConnectionDriversDefinitions();
        CategoryFolder AddFolderCategory(string pfoldername, string prootname, string pparentname);
        bool RemoveFolderCategory(string pfoldername, string prootname);
        void LoadCategoryFoldersValues();
         void SaveCategoryFoldersValues();
        void SaveFucntion2Function();
         void LoadFucntion2Function();
         void SaveEvents();
         void LoadEvents();
        void LoadAddinTreeStructure();
        void SaveAddinTreeStructure();
        void SaveCompositeLayersValues();
        bool RemoveLayerByName(string LayerName);
        bool RemoveLayerByID(int ID);
        List<CompositeLayer> LoadCompositeLayersValues();
      //  void SaveAppValues();
     //   bool RemoveAppByName(string LayerName);
      //  bool RemoveAppByID(string ID);
     //   List<App> LoadAppValues();
        List<EntityStructure> LoadTablesEntities();

        void SaveTablesEntities();
        DatasourceEntities LoadDataSourceEntitiesValues(string dsname);
        void SaveDataSourceEntitiesValues(DatasourceEntities datasourceEntities);
        bool RemoveDataSourceEntitiesValues(string dsname);
        void CreateDirConfig(string path, FolderFileTypes foldertype);
         void ReadWork();
      
         void SaveWork();
         void SaveObjectTypes();  
         List<ObjectTypes> LoadObjectTypes();
     //   void SaveAppFieldPropertiesValues();
    //    DataSourceFieldProperties LoadAppFieldPropertiesValues(string dsname);

        string CreateFileExtensionString();
        void SaveReportDefinitionsValues();
        List<AppTemplate> LoadReportsDefinitionValues();
        void SaveAIScriptsValues();
        List<ReportsList> LoadAIScriptsValues();

        void WriteSyncDataSource(string filename = "SyncDataSource");
        List<ETLScriptHDR> ReadSyncDataSource(string filename = "SyncDataSource");
        


    }
}