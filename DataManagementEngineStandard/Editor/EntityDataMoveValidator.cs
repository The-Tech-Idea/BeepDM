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
    /// <summary>Validates if a record can be inserted into a data source for a given entity.</summary>
    /// <param name="DMEEditor">The DMEEditor instance.</param>
    /// <param name="DataSource">The data source.</param>
    /// <param name="Entity">The entity structure.</param>
    /// <param name="record">The record to be inserted.</param>
    /// <param name="entityname">The name of the entity.</param>
    /// <param name="datasource">The name of the data source.</param>
    /// <returns>An EntityValidatorMesseges indicating the validation result.</returns>
    /// <remarks>
    /// This method checks if the DMEEditor instance is not null.
    /// If
    public static partial class EntityDataMoveValidator
    {
        /// <summary>
        /// Checks if a record can be inserted into a data source for a specific entity.
        /// </summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="DataSource">The IDataSource instance.</param>
        /// <param name="Entity">The EntityStructure instance.</param>
        /// <param name="record">The record to be inserted.</param>
        /// <param name="entityname">The name of the entity.</param>
        /// <param name="datasource">The name of the data source.</param>
        /// <returns>A list of validation messages indicating whether the record can be inserted or not.</returns>
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
        /// <summary>Checks if a record can be inserted into a data source.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="DataSource">The data source.</param>
        /// <param name="record">The record to be inserted.</param>
        /// <param name="Entity">The entity structure.</param>
        /// <returns>A list of validation messages indicating if the record can be inserted.</returns>
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
        /// <summary>
        /// Checks if the given object is not null.
        /// </summary>
        /// <param name="fldval">The object to be checked.</param>
        /// <returns>True if the object is not null, otherwise false.</returns>
        public static EntityValidatorMesseges TrueifNotNull(object fldval)
        {

            EntityValidatorMesseges retval = EntityValidatorMesseges.OK;
            if (fldval != null)
            {
                retval = EntityValidatorMesseges.OK;
            }
            return retval;
        }
        /// <summary>
        /// Checks if a field value is not unique within a given entity and data source.
        /// </summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="DataSource">The IDataSource instance.</param>
        /// <param name="Entity">The EntityStructure instance.</param>
        /// <param name="record">The record object.</param>
        /// <param name="fieldname">The name of the field to check.</param>
        /// <param name="fldval">The value of the field to check.</param>
        /// <returns>True if the field value is not unique, otherwise false.</returns>
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
        /// <summary>
        /// Checks if the parent entity exists in the data source based on the provided parameters.
        /// </summary>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <param name="DataSource">The data source to check for the parent entity.</param>
        /// <param name="Entity">The structure of the entity.</param>
        /// <param name="record">The record object.</param>
        /// <param name="fieldname">The name of the field.</param>
        /// <param name="fldval">The value of the field.</param>
        /// <returns>True if the parent entity exists, otherwise false.</returns>
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

                        if (DataSource != null && record != null)
                        {
                            if (DataSource.Openconnection() == System.Data.ConnectionState.Open)
                            {
                                Entity = DataSource.GetEntityStructure(Entity.EntityName, false);
                                if (Entity != null)
                                {
                                    List<RelationShipKeys> rels = Entity.Relations.Where(p => p.EntityColumnID == fieldname).ToList();
                                    if (rels.Count > 0)
                                    {
                                        foreach (RelationShipKeys rel in rels)
                                        {
                                            if (int.TryParse(strval, out intval))
                                            {
                                                cnt = (int)DataSource.RunQuery($"select count(*)  from {rel.RelatedEntityID} where {rel.RelatedEntityColumnID}={intval}");
                                            }
                                            else
                                            {

                                                cnt = (int)DataSource.RunQuery($"select count(*)  from {rel.RelatedEntityID} where {rel.RelatedEntityColumnID}='{strval}'");
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
        /// <summary>Checks if an entity exists in a data source.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="DataSource">The data source to check.</param>
        /// <param name="Entity">The entity structure to check.</param>
        /// <returns>True if the entity exists in the data source, false otherwise.</returns>
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