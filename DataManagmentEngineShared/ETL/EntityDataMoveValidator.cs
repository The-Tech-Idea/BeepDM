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
    public class EntityDataMoveValidator
    {
        public EntityDataMoveValidator(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public EntityStructure Entity { get; set; }
        public EntityValidatorMesseges CanInsertRecord(object record,string entityname,string datasource)
        {
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (DMEEditor != null)
            {
                DataSource = DMEEditor.GetDataSource(datasource);

                if(DataSource != null && record!=null)
                {
                    if(DataSource.Openconnection()== System.Data.ConnectionState.Open)
                    {
                        Entity = DataSource.GetEntityStructure(entityname,false);
                        if(Entity != null)
                        {
                            retval= CanInsertRecord(record, Entity);
                        }
                    }
                }
            }
           return retval;
        }
        public EntityValidatorMesseges CanInsertRecord(object record, EntityStructure entity)
        {
            EntityValidatorMesseges retval =  EntityValidatorMesseges.OK;
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
                                    if(TrueifNotNull(record, fld.fieldname, fldval))
                                    {
                                        retval = EntityValidatorMesseges.NullField;
                                    }
                                }
                                if (fld.IsUnique)
                                {
                                    if (TrueifNotUnique(record, fld.fieldname, fldval))
                                    {
                                        retval = EntityValidatorMesseges.DuplicateValue;
                                    }
                                    
                                }
                                if (fld.ValueRetrievedFromParent)
                                {
                                    if (TrueifNotUnique(record, fld.fieldname, fldval))
                                    {
                                        retval = EntityValidatorMesseges.DuplicateValue;
                                    }

                                }
                            }
                           
                        }
                    }
                }
            }
            return retval;
        }
        public bool TrueifNotNull(object record, string fieldname,object fldval)
        {
            bool retval = false;
          
            if (fldval != null)
            {
                retval = true;
            }
            return retval;
        }
        public bool TrueifNotUnique(object record, string fieldname, object fldval)
        {
           
            bool retval = false;
            string strval=(string)fldval;
            int intval;
            int cnt = 0;
            if (fldval != null)
            {
                if (int.TryParse(strval,out intval))
                {
                     cnt = (int)DataSource.RunQuery($"select count(*)  from {Entity.EntityName} where {fieldname}={intval}");
                }
                else 
                {
                     cnt = (int)DataSource.RunQuery($"select count(*)  from {Entity.EntityName} where {fieldname}='{strval}'");
                }
                if(cnt==0)
                {
                    retval = true;
                }
                
            }
            return retval;
        }
        public bool TrueifParentExist(object record, string fieldname, object fldval)
         {

            bool retval = true;
            string strval = (string)fldval;
            int intval;
            int cnt = 0;
            if (fldval != null)
            {
                //-------------------------------------------------------------
                //--------------- Check Related Table ------------------------
                List<RelationShipKeys> rels = Entity.Relations.Where(p => p.EntityColumnID == fieldname).ToList() ;
                if (rels.Count > 0)
                {
                    foreach (RelationShipKeys rel in rels)
                    {
                        if (int.TryParse(strval, out intval))
                        {
                             cnt = (int)DataSource.RunQuery($"select count(*)  from {rel.ParentEntityID} where {rel.ParentEntityColumnID}={intval}");
                        }
                        else
                        {
                             cnt = (int)DataSource.RunQuery($"select count(*)  from {rel.ParentEntityID} where {rel.ParentEntityColumnID}='{strval}'");
                        }
                        if (cnt == 0)
                        {
                            retval = false;
                            break;
                        }
                    }
                }
                

                
            }
            return retval;
        }

    }
}
