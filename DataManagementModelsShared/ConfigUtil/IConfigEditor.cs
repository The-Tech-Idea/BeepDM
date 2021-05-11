using System.Collections.Generic;
using TheTechIdea.DataManagment_Engine.CompositeLayer;
using System.Reflection;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine.Addin;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.AppBuilder;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.ConfigUtil;

namespace TheTechIdea.Util
{
    public interface IConfigEditor
    {
        ConfigandSettings Config { get; set; }
        string ConfigPath { get; set; }
        IJsonLoader JsonLoader { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        string ExePath { get; set; }
        IDMLogger Logger { get; set; }
        List<string> Databasetypes { get; set; }
     
        List<ConnectionProperties> DataConnections { get; set; }
        List<Mapping_rep> Mappings { get; set; }
        List<Map_Schema> MappingSchema { get; set; }
        List<DataWorkFlow> WorkFlows { get; set; }
        List<QuerySqlRepo> QueryList { get; set; }
        List<ObjectTypes> objectTypes { get; set; }
        List<ConnectionDriversConfig> DataDriversClasses { get; set; }
        List<ConnectionDriversConfig> DriverDefinitionsConfig { get; set; }
        List<AssemblyClassDefinition> DataSourcesClasses { get; set; }
        List<AssemblyClassDefinition> BranchesClasses { get; set; }
         List<AssemblyClassDefinition> AppWritersClasses { get; set; }
        List<AssemblyClassDefinition> AppComponents { get; set; }
        List<AssemblyClassDefinition> ReportWritersClasses { get; set; } 
        List<CategoryFolder> CategoryFolders { get; set; }
        List<Assembly> LoadedAssemblies { get; set; }
        List<Function2FunctionAction> Function2Functions { get; set; }
        List<Event> Events { get; set; }
        List<AddinTreeStructure> AddinTreeStructure { get; set; }
        List<ReportTemplate> ReportsDefinition { get; set; }
        List<ReportsList> Reportslist { get; set; }
        List<CompositeLayer> CompositeQueryLayers { get; set; }
        List<App> Apps { get; set; }
        List<EntityStructure> EntityCreateObjects { get; set; }
        List<DatatypeMapping> DataTypesMap { get; set; }
        List<DataSourceFieldProperties> AppfieldProperties { get; set; }
        List<DatatypeMapping> ReadDataTypeFile(string filename = "DataTypeMapping");
        void WriteDataTypeFile(string filename = "DataTypeMapping");
        string GetSql(Sqlcommandtype CmdType, string TableName, string SchemaName, string Filterparamters, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType);
        string GetSqlFromCustomQuery(Sqlcommandtype CmdType, string TableName, string customquery, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType);
        IErrorsInfo Init();
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
      
        bool UpdateDataConnection(IEnumerable<ConnectionProperties> ls, string category);
        List<ConnectionDriversConfig> LoadConnectionDriversConfigValues();
        void SaveConnectionDriversConfigValues();
        void SaveReportsValues();
        List<ReportsList> LoadReportsValues();
        List<QuerySqlRepo> LoadQueryFile();
        void SaveQueryFile();
        void SaveMappingValues();
        List<Mapping_rep> LoadMappingValues();
        void SaveMappingSchemaValue(string mapname);
        Map_Schema LoadMappingSchema(string mapname);
         void SaveMappingSchemaValue();
     
        void LoadMappingSchema();
        void SaveMapsValues();
         void LoadMapsValues();
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
        bool RemoveLayerByID(string ID);
        List<CompositeLayer> LoadCompositeLayersValues();
        void SaveAppValues();
        bool RemoveAppByName(string LayerName);
        bool RemoveAppByID(string ID);
        List<App> LoadAppValues();
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
        void SaveAppFieldPropertiesValues();
        DataSourceFieldProperties LoadAppFieldPropertiesValues(string dsname);

        string CreateFileExtensionString();
        void SaveReportDefinitionsValues();

        List<ReportTemplate> LoadReportsDefinitionValues();



    }
}