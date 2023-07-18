using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Editor
{
    public partial class DataImportManager
    {
        public string SourceEntityName { get; set; } = string.Empty;
        public string DestEntityName { get; set; } = string.Empty;
        public string SourceDataSourceName { get; set; } = string.Empty;
        public string DestDataSourceName { get; set; } = string.Empty;
        public EntityStructure SourceEntityStructure { get; set; }
        public EntityStructure DestEntityStructure { get; set; }
        public IDataSource SourceData { get; set; }
        public IDataSource DestData { get; set; }

        public UnitofWork<Entity> SrcunitofWork { get; set; }
        public UnitofWork<Entity> DstunitofWork { get; set; }
        public EntityDataMap Mapping { get; set; }
      

        public IDMEEditor DMEEditor { get; }

        public DataImportManager(IDMEEditor dMEEditor)
        {
            DMEEditor = dMEEditor;
            
        }
        public IErrorsInfo LoadSourceData(string datasourcename,string sourceEntityName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error Loading Source Data {datasourcename} -{sourceEntityName}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public Tuple<IErrorsInfo, EntityDataMap> CreateEntityMap(string SourceEntityName, string SourceDataSourceName, string DestEntityName, string DestDataSourceName)
        {
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                IDataSource Destds = null;
                //  IDataSource Srcds=null;
                EntityDataMap Mapping = DMEEditor.ConfigEditor.LoadMappingValues(DestEntityName, DestDataSourceName);
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
                        Mapping.MappedEntities.Add(AddEntitytoMappedEntities(SourceDataSourceName, SourceEntityName, destent));
                    }

                }
                else
                {
                    Mapping.MappedEntities = new List<EntityDataMap_DTL>();
                    Mapping.MappedEntities.Add(AddEntitytoMappedEntities(destent));
                }
                DMEEditor.ConfigEditor.SaveMappingValues(DestEntityName, DestDataSourceName, Mapping);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Flag = Errors.Failed;

            }
            return new Tuple<IErrorsInfo, EntityDataMap>(DMEEditor.ErrorObject,Mapping);
        }
        public Tuple<IErrorsInfo, EntityDataMap> CreateEntityMap(string DestEntityName, string DestDataSourceName)
        {
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                IDataSource Destds;
                //IDataSource Srcds;
                EntityDataMap Mapping = DMEEditor.ConfigEditor.LoadMappingValues(DestEntityName, DestDataSourceName);
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
               
                if (Mapping.MappedEntities == null)
                {
                    Mapping.MappedEntities = new List<EntityDataMap_DTL>();

                }
                Mapping.MappingName = $"{DestEntityName}_{DestDataSourceName}";
                DMEEditor.ConfigEditor.SaveMappingValues(DestEntityName, DestDataSourceName, Mapping);


            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Flag = Errors.Failed;

            }
            return new Tuple<IErrorsInfo, EntityDataMap>(DMEEditor.ErrorObject, Mapping);
        }
        public void Save()
        {
            DMEEditor.ConfigEditor.SaveMappingValues(Mapping.EntityName, Mapping.EntityDataSource, Mapping);
        }
        public EntityDataMap_DTL AddEntitytoMappedEntities(string SourceDataSourceName, string SourceEntityName, EntityStructure destent)
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
                det.FieldMapping = MapEntityFields(det);
            }
            catch (Exception ex)
            {

                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return det;
        }
        public EntityDataMap_DTL AddEntitytoMappedEntities(EntityDataMap_DTL det, string SourceDataSourceName, string SourceEntityName, EntityStructure destent)
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
                det.FieldMapping = MapEntityFields(det);
            }
            catch (Exception ex)
            {

                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return det;
        }
        public EntityDataMap_DTL AddEntitytoMappedEntities(EntityStructure destent)
        {
            EntityDataMap_DTL det = new EntityDataMap_DTL();
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                det.SelectedDestFields = destent.Fields;
                det.FieldMapping = MapEntityFields(det);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return det;
        }
        public List<Mapping_rep_fields> MapEntityFields(EntityDataMap_DTL datamap)
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
                retval = null;
            }
            return retval;
        }
      

        

    }
}
