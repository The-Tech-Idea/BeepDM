using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.ETL
{
    public partial class EntityDataMoveValidator
    {
        public EntityDataMoveValidator(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public EntityStructure Entity { get; set; }
      
        public EntityValidatorMesseges CanInsertRecord(object record, string entityname, string datasource)
        {
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (DMEEditor != null)
            {
                DataSource = DMEEditor.GetDataSource(datasource);

                if (DataSource != null && record != null)
                {
                    if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                    {
                        Entity = DataSource.GetEntityStructure(entityname, false);
                        if (Entity != null)
                        {
                            retval = CanInsertRecord(record, Entity);
                        }
                    }
                }
            }
            return retval;
        }
        public EntityValidatorMesseges CanInsertRecord(object record, EntityStructure entity)
        {
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            Entity = entity;
            if (DMEEditor != null)
            {
                DataSource = DMEEditor.GetDataSource(entity.DataSourceID);

                if (DataSource != null && record != null)
                {
                    if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                    {
                        if (entity != null)
                        {
                            foreach (EntityField fld in entity.Fields)
                            {
                                var fldval = DMEEditor.Utilfunction.GetFieldValueFromObject(fld.fieldname, record);
                                if (fld.AllowDBNull == false)
                                {

                                    retval = TrueifNotNull(entity.EntityName, entity.DataSourceID,record, fld.fieldname, fldval);
                                    
                                }
                                if (fld.IsUnique)
                                {
                                    retval = TrueifNotUnique(entity.EntityName, entity.DataSourceID, record, fld.fieldname, fldval);
                                    

                                }
                                if (fld.ValueRetrievedFromParent)
                                {
                                    retval = TrueifNotUnique(entity.EntityName, entity.DataSourceID, record, fld.fieldname, fldval);
                                }
                            }

                        }
                    }
                }
            }
            return retval;
        }
        public EntityValidatorMesseges TrueifNotNull(string entityname, string datasource, object record, string fieldname, object fldval)
        {

            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (fldval != null)
            {
                retval =  EntityValidatorMesseges.OK;
            }
            return retval;
        }
        public EntityValidatorMesseges TrueifNotUnique(string entityname, string datasource, object record, string fieldname, object fldval)
        {

           
            string strval = (string)fldval;
            int intval;
            int cnt = 0;
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (DMEEditor != null)
            {
                DataSource = DMEEditor.GetDataSource(datasource);

                if (DataSource != null && record != null)
                {
                    if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                    {
                        Entity = DataSource.GetEntityStructure(entityname, false);
                        if (Entity != null)
                        {
                            if (fldval != null)
                            {
                                if (int.TryParse(strval, out intval))
                                {
                                    cnt = (int)DataSource.RunQuery($"select count(*)  from {Entity.EntityName} where {fieldname}={intval}");
                                }
                                else
                                {
                                    cnt = (int)DataSource.RunQuery($"select count(*)  from {Entity.EntityName} where {fieldname}='{strval}'");
                                }
                                if (cnt == 0)
                                {
                                    retval = EntityValidatorMesseges.OK ;
                                }else
                                    retval = EntityValidatorMesseges.DuplicateValue;

                            }
                        }

                    }
                }

            }
           
            return retval;
        }
        public EntityValidatorMesseges TrueifParentExist(string entityname, string datasource, object record, string fieldname, object fldval)
        {


            string strval = (string)fldval;
            int intval;
            int cnt = 0;

            
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (DMEEditor != null)
            {
                DataSource = DMEEditor.GetDataSource(datasource);
                if(DataSource.Category== DatasourceCategory.RDBMS)
                {
                    RDBSource ds=(RDBSource)DataSource;
                    if (ds != null && record != null)
                    {
                        if (ds.Openconnection() == System.Data.ConnectionState.Open)
                        {
                            Entity = ds.GetEntityStructure(entityname, false);
                            if (Entity != null)
                            {
                                List<RelationShipKeys> rels = Entity.Relations.Where(p => p.EntityColumnID == fieldname).ToList();
                                if (rels.Count > 0)
                                {
                                    foreach (RelationShipKeys rel in rels)
                                    {
                                        if (int.TryParse(strval, out intval))
                                        {
                                            cnt = (int)ds.RunQuery($"select count(*)  from {rel.ParentEntityID} where {rel.ParentEntityColumnID}={intval}");
                                        }
                                        else
                                        {

                                            cnt = (int)ds.RunQuery($"select count(*)  from {rel.ParentEntityID} where {rel.ParentEntityColumnID}='{strval}'");
                                        }
                                        if (cnt == 0)
                                        {
                                            retval = EntityValidatorMesseges.MissingRefernceValue;
                                            break;
                                        }
                                        else
                                        {
                                            retval = EntityValidatorMesseges.OK;
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
               

            }
            return retval;
        }
        

    }
}