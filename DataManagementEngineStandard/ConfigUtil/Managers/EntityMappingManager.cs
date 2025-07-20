using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.ConfigUtil.Managers
{
    /// <summary>
    /// Manages entity structures and mapping operations
    /// </summary>
    public class EntityMappingManager
    {
        private readonly IDMLogger _logger;
        private readonly IJsonLoader _jsonLoader;
        private readonly ConfigandSettings _config;
        private readonly ConfigPathManager _pathManager;

        public EntityMappingManager(IDMLogger logger, IJsonLoader jsonLoader, ConfigandSettings config, ConfigPathManager pathManager)
        {
            _logger = logger;
            _jsonLoader = jsonLoader;
            _config = config;
            _pathManager = pathManager;
        }

        #region "Entity Structure Operations"

        /// <summary>
        /// Checks if the entity structure file exists.
        /// </summary>
        public bool EntityStructureExist(string filepath, string entityName, string dataSourceId)
        {
            string filename = $"{dataSourceId}^{entityName}_ES.json";
            string path = Path.Combine(filepath, filename);
            return File.Exists(path);
        }

        /// <summary>
        /// Saves the structure of an entity to a JSON file.
        /// </summary>
        public void SaveEntityStructure(string filepath, EntityStructure entity)
        {
            try
            {
                string filename = $"{entity.DataSourceID}^{entity.EntityName}_ES.json";
                string path = Path.Combine(filepath, filename);
                
                // Ensure directory exists
                _pathManager.CreateDir(filepath);
                
                _jsonLoader.Serialize(path, entity);
                _logger?.WriteLog($"Saved entity structure: {entity.EntityName}");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving entity structure {entity?.EntityName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads an entity structure from a JSON file.
        /// </summary>
        public EntityStructure LoadEntityStructure(string filepath, string entityName, string dataSourceId)
        {
            try
            {
                string filename = $"{dataSourceId}^{entityName}_ES.json";
                string path = Path.Combine(filepath, filename);
                
                if (File.Exists(path))
                {
                    return _jsonLoader.DeserializeSingleObject<EntityStructure>(path);
                }
                
                _logger?.WriteLog($"Entity structure file not found: {path}");
                return new EntityStructure();
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading entity structure {entityName}: {ex.Message}");
                return new EntityStructure();
            }
        }

        /// <summary>
        /// Loads the values of a data source's entities from a JSON file.
        /// </summary>
        public DatasourceEntities LoadDataSourceEntitiesValues(string dsname)
        {
            try
            {
                string path = Path.Combine(_config.EntitiesPath, $"{dsname}_entities.json");
                
                if (File.Exists(path))
                {
                    return _jsonLoader.DeserializeSingleObject<DatasourceEntities>(path);
                }
                
                return new DatasourceEntities { datasourcename = dsname, Entities = new List<EntityStructure>() };
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading datasource entities {dsname}: {ex.Message}");
                return new DatasourceEntities { datasourcename = dsname, Entities = new List<EntityStructure>() };
            }
        }

        /// <summary>
        /// Saves the values of a DataSourceEntities object to a JSON file.
        /// </summary>
        public void SaveDataSourceEntitiesValues(DatasourceEntities datasourceEntities)
        {
            try
            {
                string path = Path.Combine(_config.EntitiesPath, $"{datasourceEntities.datasourcename}_entities.json");
                
                // Ensure directory exists
                _pathManager.CreateDir(_config.EntitiesPath);
                
                _jsonLoader.Serialize(path, datasourceEntities);
                _logger?.WriteLog($"Saved datasource entities: {datasourceEntities.datasourcename}");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving datasource entities {datasourceEntities?.datasourcename}: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes the values of a data source's entities.
        /// </summary>
        public bool RemoveDataSourceEntitiesValues(string dsname)
        {
            try
            {
                string path = Path.Combine(_config.EntitiesPath, $"{dsname}_entities.json");
                
                if (File.Exists(path))
                {
                    File.Delete(path);
                    _logger?.WriteLog($"Removed datasource entities: {dsname}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error removing datasource entities {dsname}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region "Mapping Operations"

        /// <summary>
        /// Saves a mapping schema value to a JSON file.
        /// </summary>
        public void SaveMappingSchemaValue(string schemaname, Map_Schema mappingSchema)
        {
            try
            {
                string path = Path.Combine(_config.MappingPath, $"{schemaname}_Mapping.json");
                
                // Ensure directory exists
                _pathManager.CreateDir(_config.MappingPath);
                
                _jsonLoader.Serialize(path, mappingSchema);
                _logger?.WriteLog($"Saved mapping schema: {schemaname}");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving mapping schema {schemaname}: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a mapping schema from a JSON file.
        /// </summary>
        public Map_Schema LoadMappingSchema(string schemaname)
        {
            try
            {
                string path = Path.Combine(_config.MappingPath, $"{schemaname}_Mapping.json");
                
                if (File.Exists(path))
                {
                    return _jsonLoader.DeserializeSingleObject<Map_Schema>(path);
                }
                
                _logger?.WriteLog($"Mapping schema file not found: {path}");
                return new Map_Schema { SchemaName = schemaname };
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading mapping schema {schemaname}: {ex.Message}");
                return new Map_Schema { SchemaName = schemaname };
            }
        }

        /// <summary>
        /// Saves the mapping values for a specific entity.
        /// </summary>
        public void SaveMappingValues(string entityName, string datasource, EntityDataMap mappingRep)
        {
            try
            {
                string datasourcePath = Path.Combine(_config.MappingPath, datasource);
                _pathManager.CreateDir(datasourcePath);
                
                string path = Path.Combine(datasourcePath, $"{entityName}_Mapping.json");
                _jsonLoader.Serialize(path, mappingRep);
                _logger?.WriteLog($"Saved entity mapping: {entityName} for {datasource}");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving entity mapping {entityName} for {datasource}: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the mapping values for a given entity from a JSON file.
        /// </summary>
        public EntityDataMap LoadMappingValues(string entityName, string datasource)
        {
            try
            {
                string path = Path.Combine(_config.MappingPath, datasource, $"{entityName}_Mapping.json");
                
                if (File.Exists(path))
                {
                    return _jsonLoader.DeserializeSingleObject<EntityDataMap>(path);
                }
                
                _logger?.WriteLog($"Entity mapping file not found: {path}");
                return new EntityDataMap { EntityName = entityName };
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading entity mapping {entityName} for {datasource}: {ex.Message}");
                return new EntityDataMap { EntityName = entityName };
            }
        }

        #endregion

        #region "Table Entities Operations"

        /// <summary>
        /// Loads the entities and their structures from a JSON file.
        /// </summary>
        public List<EntityStructure> LoadTablesEntities()
        {
            try
            {
                string path = Path.Combine(_config.ConfigPath, "DDLCreateTables.json");
                
                if (File.Exists(path))
                {
                    return _jsonLoader.DeserializeObject<EntityStructure>(path);
                }
                
                return new List<EntityStructure>();
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading table entities: {ex.Message}");
                return new List<EntityStructure>();
            }
        }

        /// <summary>
        /// Saves the table entities to a JSON file.
        /// </summary>
        public void SaveTablesEntities(List<EntityStructure> entityCreateObjects)
        {
            try
            {
                string path = Path.Combine(_config.ConfigPath, "DDLCreateTables.json");
                _jsonLoader.Serialize(path, entityCreateObjects);
                _logger?.WriteLog("Saved table entities");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving table entities: {ex.Message}");
            }
        }

        #endregion

        #region "Data Type Mapping"

        /// <summary>
        /// Writes the data type mapping to a JSON file.
        /// </summary>
        public void WriteDataTypeFile(List<DatatypeMapping> dataTypesMap, string filename = "DataTypeMapping")
        {
            try
            {
                string path = Path.Combine(_config.ConfigPath, $"{filename}.json");
                _jsonLoader.Serialize(path, dataTypesMap);
                _logger?.WriteLog($"Saved data type mapping: {filename}");
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving data type mapping {filename}: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads a JSON file containing datatype mappings and returns a list of DatatypeMapping objects.
        /// </summary>
        public List<DatatypeMapping> ReadDataTypeFile(string filename = "DataTypeMapping")
        {
            try
            {
                string path = Path.Combine(_config.ConfigPath, $"{filename}.json");
                
                if (File.Exists(path))
                {
                    var loadedList = _jsonLoader.DeserializeObject<DatatypeMapping>(path);
                    return loadedList ?? new List<DatatypeMapping>();
                }
                
                return new List<DatatypeMapping>();
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error reading data type mapping {filename}: {ex.Message}");
                return new List<DatatypeMapping>();
            }
        }

        #endregion
    }
}