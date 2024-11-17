using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Utilities;


namespace TheTechIdea.Beep.Mapping
{
    /// <summary>Creates an entity mapping for a given destination entity.</summary>
    /// <param name="DMEEditor">The DMEEditor instance.</param>
    /// <param name="destent">The destination entity structure.</param>
    /// <param name="SourceEntityName">The name of the source entity.</param>
    /// <param name="SourceDataSourceName">The name of the source data source.</param>
    /// <returns>A tuple containing the errors information and the entity data map.</returns>
    /// <remarks>
    /// This method creates an entity mapping for the specified destination entity using the provided DMEEditor instance.
    /// It loads the mapping values for the destination entity from the configuration editor.
    /// If the mapping is not found, it
    public static class MappingManager
    {
        /// <summary>
        /// Creates an entity map for a given DME editor, destination entity structure, source entity name, and source data source name.
        /// </summary>
        /// <param name="DMEEditor">The DME editor instance.</param>
        /// <param name="destent">The destination entity structure.</param>
        /// <param name="SourceEntityName">The name of the source entity.</param>
        /// <param name="SourceDataSourceName">The name of the source data source.</param>
        /// <returns>A tuple containing the errors information and the entity data map.</returns>
        public static Tuple<IErrorsInfo, EntityDataMap> CreateEntityMap(IDMEEditor DMEEditor, EntityStructure destent, string SourceEntityName, string SourceDataSourceName)
        {
            EntityDataMap Mapping = DMEEditor.ConfigEditor.LoadMappingValues(destent.EntityName, destent.DataSourceID);
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                if (Mapping == null)
                {
                    Mapping = new EntityDataMap();
                    Mapping.EntityName = destent.EntityName;
                    Mapping.EntityDataSource = destent.DataSourceID;
                    Mapping.MappedEntities = new List<EntityDataMap_DTL>();
                }
                Mapping.EntityFields = destent.Fields;
                Mapping.MappingName = $"{destent.EntityName}_{destent.DataSourceID}";
                Mapping.MappedEntities.Add(AddEntitytoMappedEntities(DMEEditor, SourceDataSourceName, SourceEntityName, destent));
                DMEEditor.ConfigEditor.SaveMappingValues(destent.EntityName, destent.DataSourceID, Mapping);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Adding Entity to Map {SourceEntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return new Tuple<IErrorsInfo, EntityDataMap>(DMEEditor.ErrorObject, Mapping);
        }
        /// <summary>
        /// Creates an entity map for data migration between two entities.
        /// </summary>
        /// <param name="DMEEditor">The IDMEEditor instance used for data migration.</param>
        /// <param name="SourceEntityName">The name of the source entity.</param>
        /// <param name="SourceDataSourceName">The name of the source data source.</param>
        /// <param name="DestEntityName">The name of the destination entity.</param>
        /// <param name="DestDataSourceName">The name of the destination data source.</param>
        /// <returns>A tuple containing the errors information and the entity data map.</returns>
        public static Tuple<IErrorsInfo, EntityDataMap> CreateEntityMap(IDMEEditor DMEEditor, string SourceEntityName, string SourceDataSourceName, string DestEntityName, string DestDataSourceName)
        {
            EntityDataMap Mapping = DMEEditor.ConfigEditor.LoadMappingValues(DestEntityName, DestDataSourceName);
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                IDataSource Destds = null;

                if (Mapping == null)
                {
                    Mapping = new EntityDataMap();
                    Mapping.EntityName = DestEntityName;
                    Mapping.EntityDataSource = DestDataSourceName;
                    Mapping.MappedEntities = new List<EntityDataMap_DTL>();
                }
                Destds = DMEEditor.GetDataSource(DestDataSourceName);
                Destds.Openconnection();
                EntityStructure destent = null;
                if (Destds != null && Destds.ConnectionStatus == ConnectionState.Open)
                {
                    destent = (EntityStructure)Destds.GetEntityStructure(DestEntityName, true).Clone();
                }
                else destent = (EntityStructure)Destds.GetEntityStructure(DestEntityName, false).Clone();
                Mapping.EntityFields = destent.Fields;
                Mapping.MappingName = $"{DestEntityName}_{DestDataSourceName}";
                Mapping.MappedEntities.Add(AddEntitytoMappedEntities(DMEEditor, SourceDataSourceName, SourceEntityName, destent));
                DMEEditor.ConfigEditor.SaveMappingValues(DestEntityName, DestDataSourceName, Mapping);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Adding Entity to Map {SourceEntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return new Tuple<IErrorsInfo, EntityDataMap>(DMEEditor.ErrorObject, Mapping);
        }
        /// <summary>
        /// Creates an entity map using the specified DME editor, destination entity name, and destination data source name.
        /// </summary>
        /// <param name="DMEEditor">The DME editor used to create the entity map.</param>
        /// <param name="DestEntityName">The name of the destination entity.</param>
        /// <param name="DestDataSourceName">The name of the destination data source.</param>
        /// <returns>A tuple containing the errors information and the entity data map.</returns>
        public static Tuple<IErrorsInfo, EntityDataMap> CreateEntityMap(IDMEEditor DMEEditor, string DestEntityName, string DestDataSourceName)
        {
            EntityDataMap Mapping = DMEEditor.ConfigEditor.LoadMappingValues(DestEntityName, DestDataSourceName);
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                IDataSource Destds;
                //IDataSource Srcds;

                if (Mapping == null)
                {
                    Mapping = new EntityDataMap();
                    Mapping.EntityName = DestEntityName;
                    Mapping.EntityDataSource = DestDataSourceName;
                    Mapping.MappedEntities = new List<EntityDataMap_DTL>();
                }

                Destds = DMEEditor.GetDataSource(DestDataSourceName);
                Destds.Openconnection();
                EntityStructure destent = null;
                if (Destds != null && Destds.ConnectionStatus == ConnectionState.Open)
                {
                    destent = (EntityStructure)Destds.GetEntityStructure(DestEntityName, true).Clone();
                }
                else destent = (EntityStructure)Destds.GetEntityStructure(DestEntityName, false).Clone();
                Mapping.EntityFields = destent.Fields;
                Mapping.MappingName = $"{DestEntityName}_{DestDataSourceName}";
                DMEEditor.ConfigEditor.SaveMappingValues(DestEntityName, DestDataSourceName, Mapping);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Adding Entity to Map {DestEntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return new Tuple<IErrorsInfo, EntityDataMap>(DMEEditor.ErrorObject, Mapping);
        }
        /// <summary>Adds an entity to the mapped entities in the specified DME editor.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="SourceDataSourceName">The name of the source data source.</param>
        /// <param name="SourceEntityName">The name of the source entity.</param>
        /// <param name="destent">The destination entity structure.</param>
        /// <returns>The updated EntityDataMap_DTL object.</returns>
        public static EntityDataMap_DTL AddEntitytoMappedEntities(IDMEEditor DMEEditor, string SourceDataSourceName, string SourceEntityName, EntityStructure destent)
        {
            EntityDataMap_DTL det = new EntityDataMap_DTL();
            try
            {
                IDataSource srcds = null;
                IDataSource destds = null;
                EntityStructure srcent = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                if (!string.IsNullOrEmpty(SourceEntityName))
                {
                    det.EntityDataSource = destent.DataSourceID;
                    det.EntityName = destent.EntityName;
                    det.EntityFields = destent.Fields;
                    srcds = DMEEditor.GetDataSource(SourceDataSourceName);
                    srcds.Openconnection();
                    srcent = null;
                    if (srcent != null && destds.ConnectionStatus == ConnectionState.Open)
                    {
                        srcent = (EntityStructure)srcds.GetEntityStructure(SourceEntityName, true).Clone();
                    }
                    else srcent = (EntityStructure)srcds.GetEntityStructure(SourceEntityName, false).Clone();
                }
                det.SelectedDestFields = srcent.Fields;
                det.FieldMapping = MapEntityFields(DMEEditor, srcent, det);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Adding Entity to Map {SourceEntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return det;
        }
        /// <summary>Adds an entity to the mapped entities in the EntityDataMap_DTL.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="det">The EntityDataMap_DTL instance.</param>
        /// <param name="SourceDataSourceName">The name of the source data source.</param>
        /// <param name="SourceEntityName">The name of the source entity.</param>
        /// <param name="destent">The destination entity structure.</param>
        /// <returns>The updated EntityDataMap_DTL instance.</returns>
        public static EntityDataMap_DTL AddEntitytoMappedEntities(IDMEEditor DMEEditor, EntityDataMap_DTL det, string SourceDataSourceName, string SourceEntityName, EntityStructure destent)
        {
            try
            {
                EntityStructure srcent = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                if (!string.IsNullOrEmpty(SourceEntityName))
                {
                    det.EntityDataSource = destent.DataSourceID;
                    det.EntityName = destent.EntityName;
                    det.EntityFields = destent.Fields;
                    IDataSource Srcds = DMEEditor.GetDataSource(SourceDataSourceName);
                    Srcds.Openconnection();
                    srcent = null;
                    if (Srcds != null && Srcds.ConnectionStatus == ConnectionState.Open)
                    {
                        srcent = (EntityStructure)Srcds.GetEntityStructure(SourceEntityName, true).Clone();
                    }
                    else srcent = (EntityStructure)Srcds.GetEntityStructure(SourceEntityName, false).Clone();

                }
                det.SelectedDestFields = srcent.Fields;
                det.FieldMapping = MapEntityFields(DMEEditor, srcent, det);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Adding Entity Mapping {SourceEntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return det;
        }
        /// <summary>Adds an entity to the mapped entities in the EntityDataMap_DTL.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="det">The EntityDataMap_DTL instance.</param>
        /// <param name="srcent">The source entity structure.</param>
        /// <param name="destent">The destination entity structure.</param>
        /// <returns>The updated EntityDataMap_DTL instance with the added entity.</returns>
        public static EntityDataMap_DTL AddEntitytoMappedEntities(IDMEEditor DMEEditor, EntityDataMap_DTL det, EntityStructure srcent, EntityStructure destent)
        {
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                if (!string.IsNullOrEmpty(srcent.EntityName))
                {
                    det.EntityDataSource = destent.DataSourceID;
                    det.EntityName = destent.EntityName;
                    det.EntityFields = destent.Fields;
                }
                det.SelectedDestFields = destent.Fields;
                det.FieldMapping = MapEntityFields(DMEEditor, srcent, det);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Adding Entity Mapping {destent.EntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return det;
        }
        /// <summary>Adds an entity to the mapped entities in the IDMEEditor.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="srcent">The source entity structure.</param>
        /// <param name="destent">The destination entity structure.</param>
        /// <returns>The updated EntityDataMap_DTL object.</returns>
        public static EntityDataMap_DTL AddEntitytoMappedEntities(IDMEEditor DMEEditor, EntityStructure srcent, EntityStructure destent)
        {
            EntityDataMap_DTL det = new EntityDataMap_DTL();
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                if (!string.IsNullOrEmpty(srcent.EntityName))
                {
                    det.EntityDataSource = destent.DataSourceID;
                    det.EntityName = destent.EntityName;
                    det.EntityFields = destent.Fields;

                }
                det.SelectedDestFields = srcent.Fields;
                det.FieldMapping = MapEntityFields(DMEEditor, srcent, det);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Adding Entity to Map {srcent.EntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return det;
        }
        /// <summary>
        /// Maps the fields of a source entity to a destination entity using a data map.
        /// </summary>
        /// <param name="DMEEditor">The IDMEEditor instance used for mapping.</param>
        /// <param name="srcent">The source entity structure.</param>
        /// <param name="datamap">The data map used for mapping.</param>
        /// <returns>A list of Mapping_rep_fields representing the mapped fields.</returns>
        public static List<Mapping_rep_fields> MapEntityFields(IDMEEditor DMEEditor, EntityStructure srcent, EntityDataMap_DTL datamap)
        {
            List<Mapping_rep_fields> retval = new List<Mapping_rep_fields>();
            try
            {
                datamap.EntityName = srcent.EntityName;
                datamap.EntityDataSource = srcent.DataSourceID;
                for (int i = 0; i < datamap.SelectedDestFields.Count; i++)
                {
                    Mapping_rep_fields x = new Mapping_rep_fields();
                    x.ToFieldName = datamap.SelectedDestFields[i].fieldname;
                    x.ToFieldType = datamap.SelectedDestFields[i].fieldtype;
                    foreach (EntityField item in srcent.Fields)
                    {
                        if (item.fieldname.Equals(x.ToFieldName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            x.FromFieldName = item.fieldname;
                            x.FromFieldType = item.fieldtype;
                        }
                    }
                    retval.Add(x);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Mapping Entities Field {datamap.EntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return retval;
        }
        /// <summary>Checks if a mapping entity exists.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="entityname">The name of the entity.</param>
        /// <param name="entityDataMap">The EntityDataMap instance.</param>
        /// <returns>An integer value indicating if the mapping entity exists.</returns>
        public static int CheckifMappingEntityExist(IDMEEditor DMEEditor, string entityname, EntityDataMap entityDataMap)
        {
            int retval = -1;
            try
            {
                retval = entityDataMap.MappedEntities.FindIndex(p => p.EntityName.Equals(entityname, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                retval = -1;
                DMEEditor.AddLogMessage("Beep", $"Error in finding Entities Field {entityDataMap.EntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return retval;
        }
    }
}
