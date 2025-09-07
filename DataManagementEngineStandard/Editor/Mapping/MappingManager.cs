using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping
{
    /// <summary>
    /// Provides utility methods to create and manage entity mappings between source and destination entities.
    /// </summary>
    public  static partial class MappingManager
    {
        private const string LogSource = "Beep";

        /// <summary>
        /// Creates an entity mapping for a given destination entity using a source entity and data source.
        /// </summary>
        /// <param name="DMEEditor">The DMEEditor instance for accessing configuration and data sources.</param>
        /// <param name="destent">The structure of the destination entity.</param>
        /// <param name="SourceEntityName">The name of the source entity.</param>
        /// <param name="SourceDataSourceName">The name of the source data source.</param>
        /// <returns>
        /// A tuple containing the <see cref="IErrorsInfo"/> object and the resulting <see cref="EntityDataMap"/>.
        /// </returns>
        public static Tuple<IErrorsInfo, EntityDataMap> CreateEntityMap(IDMEEditor DMEEditor, EntityStructure destent, string SourceEntityName, string SourceDataSourceName)
        {
            var Mapping = LoadOrInitializeMapping(DMEEditor, destent.EntityName, destent.DataSourceID);
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                Mapping.EntityFields = destent.Fields;
                Mapping.MappingName = $"{destent.EntityName}_{destent.DataSourceID}";
                Mapping.MappedEntities.Add(AddEntityToMappedEntities(DMEEditor, SourceDataSourceName, SourceEntityName, destent));
                SaveMapping(DMEEditor, destent.EntityName, destent.DataSourceID, Mapping);
            }
            catch (Exception ex)
            {
                LogError(DMEEditor, $"Error Adding Entity to Map {SourceEntityName}", ex);
            }
            return new Tuple<IErrorsInfo, EntityDataMap>(DMEEditor.ErrorObject, Mapping);
        }

        /// <summary>
        /// Creates an entity mapping for migration between two entities in different data sources.
        /// </summary>
        /// <param name="DMEEditor">The DMEEditor instance for accessing configuration and data sources.</param>
        /// <param name="SourceEntityName">The name of the source entity.</param>
        /// <param name="SourceDataSourceName">The name of the source data source.</param>
        /// <param name="DestEntityName">The name of the destination entity.</param>
        /// <param name="DestDataSourceName">The name of the destination data source.</param>
        /// <returns>
        /// A tuple containing the <see cref="IErrorsInfo"/> object and the resulting <see cref="EntityDataMap"/>.
        /// </returns>
        public static Tuple<IErrorsInfo, EntityDataMap> CreateEntityMap(IDMEEditor DMEEditor, string SourceEntityName, string SourceDataSourceName, string DestEntityName, string DestDataSourceName)
        {
            var Mapping = LoadOrInitializeMapping(DMEEditor, DestEntityName, DestDataSourceName);
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                var destent = GetEntityStructure(DMEEditor, DestDataSourceName, DestEntityName);
                Mapping.EntityFields = destent.Fields;
                Mapping.MappingName = $"{DestEntityName}_{DestDataSourceName}";
                Mapping.MappedEntities.Add(AddEntityToMappedEntities(DMEEditor, SourceDataSourceName, SourceEntityName, destent));
                SaveMapping(DMEEditor, DestEntityName, DestDataSourceName, Mapping);
            }
            catch (Exception ex)
            {
                LogError(DMEEditor, $"Error Adding Entity to Map {SourceEntityName}", ex);
            }
            return new Tuple<IErrorsInfo, EntityDataMap>(DMEEditor.ErrorObject, Mapping);
        }

        /// <summary>
        /// Creates a new entity mapping for the specified destination entity.
        /// </summary>
        /// <param name="DMEEditor">The DMEEditor instance for accessing configuration and data sources.</param>
        /// <param name="DestEntityName">The name of the destination entity.</param>
        /// <param name="DestDataSourceName">The name of the destination data source.</param>
        /// <returns>
        /// A tuple containing the <see cref="IErrorsInfo"/> object and the resulting <see cref="EntityDataMap"/>.
        /// </returns>
        public static Tuple<IErrorsInfo, EntityDataMap> CreateEntityMap(IDMEEditor DMEEditor, string DestEntityName, string DestDataSourceName)
        {
            var Mapping = LoadOrInitializeMapping(DMEEditor, DestEntityName, DestDataSourceName);
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                var destent = GetEntityStructure(DMEEditor, DestDataSourceName, DestEntityName);
                Mapping.EntityFields = destent.Fields;
                Mapping.MappingName = $"{DestEntityName}_{DestDataSourceName}";
                SaveMapping(DMEEditor, DestEntityName, DestDataSourceName, Mapping);
            }
            catch (Exception ex)
            {
                LogError(DMEEditor, $"Error Adding Entity to Map {DestEntityName}", ex);
            }
            return new Tuple<IErrorsInfo, EntityDataMap>(DMEEditor.ErrorObject, Mapping);
        }

        /// <summary>
        /// Adds a source entity to the mapped entities for a given destination entity.
        /// </summary>
        /// <param name="DMEEditor">The DMEEditor instance for accessing configuration and data sources.</param>
        /// <param name="SourceDataSourceName">The name of the source data source.</param>
        /// <param name="SourceEntityName">The name of the source entity.</param>
        /// <param name="destent">The destination entity structure.</param>
        /// <returns>The updated <see cref="EntityDataMap_DTL"/> object.</returns>
        public static EntityDataMap_DTL AddEntityToMappedEntities(IDMEEditor DMEEditor, string SourceDataSourceName, string SourceEntityName, EntityStructure destent)
        {
            var det = new EntityDataMap_DTL();
            try
            {
                InitializeEntityDataMapDetail(det, destent);
                var srcds = DMEEditor.GetDataSource(SourceDataSourceName);
                srcds?.Openconnection();

                var srcent = srcds?.GetEntityStructure(SourceEntityName, srcds.ConnectionStatus == ConnectionState.Open).Clone() as EntityStructure;

                if (srcent != null)
                {
                    det.SelectedDestFields = srcent.Fields;
                    det.FieldMapping = MapEntityFields(DMEEditor, srcent, det);
                }
            }
            catch (Exception ex)
            {
                LogError(DMEEditor, $"Error Adding Entity to Map {SourceEntityName}", ex);
            }
            return det;
        }

        /// <summary>
        /// Maps fields from the source entity to the destination entity.
        /// </summary>
        /// <param name="DMEEditor">The DMEEditor instance for accessing configuration and data sources.</param>
        /// <param name="srcent">The structure of the source entity.</param>
        /// <param name="datamap">The data map for mapping fields.</param>
        /// <returns>A list of <see cref="Mapping_rep_fields"/> representing the mapped fields.</returns>
        public static List<Mapping_rep_fields> MapEntityFields(IDMEEditor DMEEditor, EntityStructure srcent, EntityDataMap_DTL datamap)
        {
            var retval = new List<Mapping_rep_fields>();
            try
            {
                datamap.EntityName = srcent.EntityName;
                datamap.EntityDataSource = srcent.DataSourceID;

                retval.AddRange(datamap.SelectedDestFields.Select(destField =>
                {
                    var srcField = srcent.Fields.FirstOrDefault(f => f.fieldname.Equals(destField.fieldname, StringComparison.InvariantCultureIgnoreCase));
                    return new Mapping_rep_fields
                    {
                        ToFieldName = destField.fieldname,
                        ToFieldType = destField.fieldtype,
                        FromFieldName = srcField?.fieldname,
                        FromFieldType = srcField?.fieldtype
                    };
                }));
            }
            catch (Exception ex)
            {
                LogError(DMEEditor, $"Error Mapping Entities Field {datamap.EntityName}", ex);
            }
            return retval;
        }
        public static object GetEntityObject(IDMEEditor DMEEditor, string EntityName, List<EntityField> Fields)
        {
            return DMTypeBuilder.CreateNewObject(DMEEditor, EntityName, EntityName, Fields);
        }
        public static object MapObjectToAnother(IDMEEditor DMEEditor, string destentityname, EntityDataMap_DTL SelectedMapping, object sourceobj)
        {
            // Validate parameters
            if (DMEEditor == null)
                throw new ArgumentNullException(nameof(DMEEditor));
            if (string.IsNullOrEmpty(destentityname))
                throw new ArgumentException("Destination entity name cannot be null or empty.", nameof(destentityname));
            if (SelectedMapping == null)
                throw new ArgumentNullException(nameof(SelectedMapping));
            if (sourceobj == null)
                throw new ArgumentNullException(nameof(sourceobj));

            // Create the destination object
            object destobj = GetEntityObject(DMEEditor, destentityname, SelectedMapping.SelectedDestFields);
            if (destobj == null)
                throw new InvalidOperationException($"Failed to create destination object for entity: {destentityname}");

            // Map each property from the source to the destination
            foreach (Mapping_rep_fields mapping in SelectedMapping.FieldMapping)
            {
                try
                {
                    MapProperty(sourceobj, destobj, mapping);
                }
                catch (Exception ex)
                {
                    // Log and continue mapping the next field
                    DMEEditor.AddLogMessage("MappingError", $"Error mapping field '{mapping.FromFieldName}' to '{mapping.ToFieldName}': {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                }
            }

            return destobj;
        }

        #region Helper Methods

        private static EntityStructure GetEntityStructure(IDMEEditor DMEEditor, string dataSourceName, string entityName)
        {
            var dataSource = DMEEditor.GetDataSource(dataSourceName);
            dataSource?.Openconnection();

            return dataSource != null && dataSource.ConnectionStatus == ConnectionState.Open
                ? (EntityStructure)dataSource.GetEntityStructure(entityName, true).Clone()
                : (EntityStructure)dataSource.GetEntityStructure(entityName, false).Clone();
        }

        private static EntityDataMap LoadOrInitializeMapping(IDMEEditor DMEEditor, string entityName, string dataSourceID)
        {
            var Mapping = DMEEditor.ConfigEditor.LoadMappingValues(entityName, dataSourceID);
            if (Mapping == null)
            {
                Mapping = new EntityDataMap
                {
                    EntityName = entityName,
                    EntityDataSource = dataSourceID,
                    MappedEntities = new List<EntityDataMap_DTL>()
                };
            }
            return Mapping;
        }

        private static void SaveMapping(IDMEEditor DMEEditor, string entityName, string dataSourceID, EntityDataMap Mapping)
        {
            DMEEditor.ConfigEditor.SaveMappingValues(entityName, dataSourceID, Mapping);
        }

        private static void InitializeEntityDataMapDetail(EntityDataMap_DTL det, EntityStructure destent)
        {
            det.EntityDataSource = destent.DataSourceID;
            det.EntityName = destent.EntityName;
            det.EntityFields = destent.Fields;
        }

        private static void LogError(IDMEEditor DMEEditor, string message, Exception ex)
        {
            DMEEditor.AddLogMessage(LogSource, $"{message} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        }
        /// <summary>
        /// Creates a new object based on the entity definition and fields.
        /// </summary>
        /// <param name="DMEEditor">The DMEEditor instance for configuration and type building.</param>
        /// <param name="EntityName">The name of the entity.</param>
        /// <param name="Fields">The list of entity fields defining the object structure.</param>
        /// <returns>A dynamically created object for the entity.</returns>
      
        private static void MapProperty(object source, object destination, Mapping_rep_fields mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            // Get the source property
            PropertyInfo sourceProperty = source.GetType().GetProperty(mapping.FromFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (sourceProperty == null)
                throw new InvalidOperationException($"Source property '{mapping.FromFieldName}' not found on object of type '{source.GetType().Name}'.");

            // Get the destination property
            PropertyInfo destinationProperty = destination.GetType().GetProperty(mapping.ToFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (destinationProperty == null)
                throw new InvalidOperationException($"Destination property '{mapping.ToFieldName}' not found on object of type '{destination.GetType().Name}'.");

            // Get the value from the source property
            object value = sourceProperty.GetValue(source, null);

            // Convert the value if the types differ and are compatible
            if (value != null && destinationProperty.PropertyType != sourceProperty.PropertyType)
            {
                value = ConvertValue(value, destinationProperty.PropertyType);
            }

            // Set the value on the destination property
            destinationProperty.SetValue(destination, value, null);
        }

        private static object ConvertValue(object value, Type targetType)
        {
            try
            {
                if (targetType.IsAssignableFrom(value.GetType()))
                {
                    return value;
                }
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert value '{value}' to type '{targetType.Name}'.", ex);
            }
        }

        #endregion
    }
}
