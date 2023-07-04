using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.Beep
{
    public static partial class EntityDataMoveValidator
    {
        public static EntityValidatorMesseges CanInsertRecord(IDMEEditor DMEEditor, IDataSource DataSource, EntityStructure Entity, object record, string entityname, string datasource)
        {
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (DMEEditor != null)
            {
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(Entity.DataSourceID);
                }
                if (DataSource != null)
                {
                    if (DataSource != null && record != null)
                    {
                        if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                        {
                            Entity = DataSource.GetEntityStructure(entityname, false);
                            if (Entity != null)
                            {
                                retval = CanInsertRecord(DMEEditor, DataSource, record, Entity);
                            }
                        }
                    }
                }
            }
            return retval;
        }
        public static EntityValidatorMesseges CanInsertRecord(IDMEEditor DMEEditor, IDataSource DataSource, object record, EntityStructure Entity)
        {
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;

            if (DMEEditor != null)
            {
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(Entity.DataSourceID);
                }
                if (DataSource != null)
                {
                    if (DataSource != null && record != null)
                    {
                        if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                        {
                            if (Entity != null)
                            {
                                foreach (EntityField fld in Entity.Fields)
                                {
                                    var fldval = DMEEditor.Utilfunction.GetFieldValueFromObject(fld.fieldname, record);
                                    if (fld.AllowDBNull == false)
                                    {

                                        retval = TrueifNotNull(fldval);

                                    }
                                    if (fld.IsUnique)
                                    {
                                        retval = TrueifNotUnique(DMEEditor, DataSource, Entity, record, fld.fieldname, fldval);


                                    }
                                    if (fld.ValueRetrievedFromParent)
                                    {
                                        retval = TrueifNotUnique(DMEEditor, DataSource, Entity, record, fld.fieldname, fldval);
                                    }
                                }

                            }
                        }
                    }
                }


            }
            return retval;
        }
        public static EntityValidatorMesseges TrueifNotNull(object fldval)
        {

            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (fldval != null)
            {
                retval = EntityValidatorMesseges.OK;
            }
            return retval;
        }
        public static EntityValidatorMesseges TrueifNotUnique(IDMEEditor DMEEditor, IDataSource DataSource, EntityStructure Entity, object record, string fieldname, object fldval)
        {


            string strval = (string)fldval;
            int intval;
            int cnt = 0;
            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (DMEEditor != null)
            {
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(Entity.DataSourceID);
                }
                if (DataSource != null)
                {
                    if (DataSource != null && record != null)
                    {
                        if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                        {
                            Entity = DataSource.GetEntityStructure(Entity.EntityName, false);
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
                                        retval = EntityValidatorMesseges.OK;
                                    }
                                    else
                                        retval = EntityValidatorMesseges.DuplicateValue;

                                }
                            }

                        }
                    }
                }



            }

            return retval;
        }
        public static EntityValidatorMesseges TrueifParentExist(IDMEEditor DMEEditor, IDataSource DataSource, EntityStructure Entity, object record, string fieldname, object fldval)
        {


            string strval = (string)fldval;
            int intval;
            int cnt = 0;


            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (DMEEditor != null)
            {
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(Entity.DataSourceID);
                }
                if (DataSource != null)
                {
                    if (DataSource.Category == DatasourceCategory.RDBMS)
                    {
                        RDBSource ds = (RDBSource)DataSource;
                        if (ds != null && record != null)
                        {
                            if (ds.Openconnection() == System.Data.ConnectionState.Open)
                            {
                                Entity = ds.GetEntityStructure(Entity.EntityName, false);
                                if (Entity != null)
                                {
                                    List<RelationShipKeys> rels = Entity.Relations.Where(p => p.EntityColumnID == fieldname).ToList();
                                    if (rels.Count > 0)
                                    {
                                        foreach (RelationShipKeys rel in rels)
                                        {
                                            if (int.TryParse(strval, out intval))
                                            {
                                                cnt = (int)ds.RunQuery($"select count(*)  from {rel.RelatedEntityID} where {rel.RelatedEntityColumnID}={intval}");
                                            }
                                            else
                                            {

                                                cnt = (int)ds.RunQuery($"select count(*)  from {rel.RelatedEntityID} where {rel.RelatedEntityColumnID}='{strval}'");
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




            }
            return retval;
        }
        public static bool TrueifEntityExist(IDMEEditor DMEEditor, IDataSource DataSource, EntityStructure Entity)
        {
            bool retval = false;

            if (DMEEditor != null)
            {
                if (DataSource == null)
                {
                    DataSource = DMEEditor.GetDataSource(Entity.DataSourceID);
                }
                if (DataSource != null)
                {
                    if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                    {
                        Entity = DataSource.GetEntityStructure(Entity.EntityName, false);
                        if (Entity == null)
                        {
                            retval = true;
                        }
                    }
                }

            }
            return retval;
        }
    }
}