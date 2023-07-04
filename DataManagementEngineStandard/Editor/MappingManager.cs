using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;


namespace TheTechIdea.Beep.Mapping
{
    public class MappingManager
    {
        public static EntityDataMap CreateEntityMap(IDMEEditor DMEEditor, string SourceEntityName, string SourceDataSourceName, string DestEntityName, string DestDataSourceName)
        {
            EntityDataMap Mapping = null;
            try
            {

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                IDataSource Destds = null;
                //  IDataSource Srcds=null;
                Mapping = DMEEditor.ConfigEditor.LoadMappingValues(DestEntityName, DestDataSourceName);
                if (Mapping == null)
                {
                    Mapping = new EntityDataMap();
                    Mapping.EntityName = DestEntityName;
                    Mapping.EntityDataSource = DestDataSourceName;
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
                if (Mapping.MappedEntities.Count > 0)
                {
                    if (!Mapping.MappedEntities.Where(p => p.EntityName != null && p.EntityName.Equals(SourceEntityName, StringComparison.OrdinalIgnoreCase) && p.EntityDataSource != null && p.EntityDataSource.Equals(SourceDataSourceName, StringComparison.OrdinalIgnoreCase)).Any())
                    {
                        Mapping.MappedEntities.Add(AddEntitytoMappedEntities(DMEEditor, SourceDataSourceName, SourceEntityName, destent));
                    }

                }
                else
                {
                    Mapping.MappedEntities = new List<EntityDataMap_DTL>();
                    Mapping.MappedEntities.Add(AddEntitytoMappedEntities(DMEEditor, destent));
                }
                DMEEditor.ConfigEditor.SaveMappingValues(DestEntityName, DestDataSourceName, Mapping);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Add Entity Map Entity Fields {ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return Mapping;
        }
        public static EntityDataMap CreateEntityMap(IDMEEditor DMEEditor, string DestEntityName, string DestDataSourceName)
        {
            EntityDataMap Mapping = null;
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                IDataSource Destds;

                Mapping = DMEEditor.ConfigEditor.LoadMappingValues(DestEntityName, DestDataSourceName);
                if (Mapping == null)
                {
                    Mapping = new EntityDataMap();
                    Mapping.EntityName = DestEntityName;
                    Mapping.EntityDataSource = DestDataSourceName;
                }

                Destds = DMEEditor.GetDataSource(DestDataSourceName);
                Destds.Openconnection();
                EntityStructure destent = null;
                if (Destds != null && Destds.ConnectionStatus == ConnectionState.Open)
                {
                    destent = (EntityStructure)Destds.GetEntityStructure(DestEntityName, true).Clone();
                }
                else destent = (EntityStructure)Destds.GetEntityStructure(DestEntityName, false).Clone();
                //EntityDataMap_DTL det = new EntityDataMap_DTL();
                //det.SelectedDestFields = destent.Fields;
                //Mapping.EntityFields = destent.Fields;
                //Mapping.MappedEntities.Add(det);
                if (Mapping.MappedEntities == null)
                {
                    Mapping.MappedEntities = new List<EntityDataMap_DTL>();

                }
                Mapping.MappingName = $"{DestEntityName}_{DestDataSourceName}";
                DMEEditor.ConfigEditor.SaveMappingValues(DestEntityName, DestDataSourceName, Mapping);


            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Create Entity Map Entity Fields {ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return Mapping;
        }
        public static EntityDataMap_DTL AddEntitytoMappedEntities(IDMEEditor DMEEditor, string SourceDataSourceName, string SourceEntityName, EntityStructure destent)
        {
            EntityDataMap_DTL det = new EntityDataMap_DTL();
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                if (!string.IsNullOrEmpty(SourceEntityName))
                {
                    det.EntityDataSource = SourceDataSourceName;
                    det.EntityName = SourceEntityName;
                    IDataSource Srcds = DMEEditor.GetDataSource(SourceDataSourceName);
                    Srcds.Openconnection();
                    EntityStructure srcent = null;
                    if (Srcds != null && Srcds.ConnectionStatus == ConnectionState.Open)
                    {
                        srcent = (EntityStructure)Srcds.GetEntityStructure(SourceEntityName, true).Clone();
                    }
                    else srcent = (EntityStructure)Srcds.GetEntityStructure(SourceEntityName, false).Clone();
                    det.EntityFields = srcent.Fields;
                }
                det.SelectedDestFields = destent.Fields;
                det.FieldMapping = MapEntityFields(DMEEditor, det);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Add Entity Map Entity Fields {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return det;
        }
        public static EntityDataMap_DTL AddEntitytoMappedEntities(IDMEEditor DMEEditor, EntityDataMap_DTL det, string SourceDataSourceName, string SourceEntityName, EntityStructure destent)
        {
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                if (!string.IsNullOrEmpty(SourceEntityName))
                {
                    det.EntityDataSource = SourceDataSourceName;
                    det.EntityName = SourceEntityName;
                    IDataSource Srcds = DMEEditor.GetDataSource(SourceDataSourceName);
                    Srcds.Openconnection();
                    EntityStructure srcent = null;
                    if (Srcds != null && Srcds.ConnectionStatus == ConnectionState.Open)
                    {
                        srcent = (EntityStructure)Srcds.GetEntityStructure(SourceEntityName, true).Clone();
                    }
                    else srcent = (EntityStructure)Srcds.GetEntityStructure(SourceEntityName, false).Clone();
                    det.EntityFields = srcent.Fields;
                }
                det.SelectedDestFields = destent.Fields;
                det.FieldMapping = MapEntityFields(DMEEditor, det);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Add Entity Map Entity Fields {ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return det;
        }
        public static EntityDataMap_DTL AddEntitytoMappedEntities(IDMEEditor DMEEditor, EntityStructure destent)
        {
            EntityDataMap_DTL det = new EntityDataMap_DTL();
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                det.SelectedDestFields = destent.Fields;
                det.FieldMapping = MapEntityFields(DMEEditor, det);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Add Entity Map Entity Fields {ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return det;
        }
        public static List<Mapping_rep_fields> MapEntityFields(IDMEEditor DMEEditor, EntityDataMap_DTL datamap)
        {
            List<Mapping_rep_fields> retval = new List<Mapping_rep_fields>();
            try
            {
                for (int i = 0; i < datamap.SelectedDestFields.Count; i++)
                {
                    Mapping_rep_fields x = new Mapping_rep_fields();
                    x.ToFieldName = datamap.SelectedDestFields[i].fieldname;
                    x.ToFieldType = datamap.SelectedDestFields[i].fieldtype;
                    foreach (EntityField item in datamap.EntityFields)
                    {
                        if (item.fieldname.Equals(x.ToFieldName, StringComparison.OrdinalIgnoreCase))
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
                DMEEditor.AddLogMessage("Beep", $"Could not Map Entity Fields {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                retval = null;
            }
            return retval;
        }
    }
}
