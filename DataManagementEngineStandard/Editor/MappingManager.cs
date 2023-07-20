using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;


namespace TheTechIdea.Beep.Mapping
{
    public static class MappingManager
    {
        public static Tuple<IErrorsInfo, EntityDataMap> CreateEntityMap(IDMEEditor DMEEditor, EntityStructure destent , string SourceEntityName, string SourceDataSourceName)
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
                Mapping.MappedEntities.Add(AddEntitytoMappedEntities(DMEEditor,SourceDataSourceName, SourceEntityName, destent));
                DMEEditor.ConfigEditor.SaveMappingValues(DestEntityName, DestDataSourceName, Mapping);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Adding Entity to Map {SourceEntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return new Tuple<IErrorsInfo, EntityDataMap>(DMEEditor.ErrorObject, Mapping);
        }
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
                det.FieldMapping = MapEntityFields(DMEEditor, srcent,det);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Adding Entity Mapping {destent.EntityName}-{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return det;
        }
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
        public static int CheckifMappingEntityExist(IDMEEditor DMEEditor,string entityname, EntityDataMap entityDataMap)
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
