﻿using System;
using System.Collections.Generic;
using System.Data;
using TheTechIdea.Util;
using TheTechIdea.Logger;
using TheTechIdea.DataManagment_Engine.Workflow;
using System.Threading.Tasks;

using System.Linq;
using Dapper;

using System.Reflection;
using System.Data.Common;


using static TheTechIdea.DataManagment_Engine.Util;
using System.Text.RegularExpressions;
using System.Dynamic;
using TheTechIdea.DataManagment_Engine.Editor;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public class RDBSource : IRDBSource
    {
        public event EventHandler<PassedArgs> PassEvent;

        static Random r = new Random();


        public string Id { get; set; }
        public string DatasourceName { get; set; }
        public DataSourceType DatasourceType { get; set; }
        public ConnectionState ConnectionStatus { get => Dataconnection.ConnectionStatus; set { } }
        public DatasourceCategory Category { get; set; } = DatasourceCategory.RDBMS;
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public IDMEEditor DMEEditor { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDataConnection Dataconnection { get; set; }
        public LScriptTracker trackingHeader { get; set; } = new LScriptTracker();
        public RDBSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.RDBMS;
            Dataconnection = new RDBDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject

            };


        }
        #region "IDataSource Interface Methods"
        public virtual EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            EntityStructure retval = new EntityStructure();
            EntityStructure fnd = Entities.Where(d => d.EntityName == EntityName).FirstOrDefault();
            //  DataTable tb = new DataTable();

            if (fnd == null)
            {
                refresh = true;
                retval.DataSourceID = DatasourceName;
                retval.EntityName = EntityName;
                if (EntityName.ToUpper().Contains("SELECT") || EntityName.ToUpper().Contains("WHERE"))
                {
                    retval.Viewtype = ViewType.Query;
                    retval.CustomBuildQuery = EntityName;
                }
                else
                {
                    retval.Viewtype = ViewType.Table;
                    retval.CustomBuildQuery = null;
                }
            } else
            {
                retval = fnd;
            }


            return GetEntityStructure(retval, refresh);
        }
        public virtual EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            //    EntityStructure retval = new EntityStructure();
            // 
            DataTable tb = new DataTable();

           
            if (refresh)
            {
                fnd.DataSourceID = DatasourceName;
                //  fnd.EntityName = EntityName;
                if (fnd.Viewtype == ViewType.Query)
                {

                    tb = GetTableSchema(fnd.CustomBuildQuery);
                }
                else
                {
                    tb = GetTableSchema(fnd.EntityName);
                }

                if (tb.Rows.Count > 0)
                {
                    fnd.Fields = new List<EntityField>();
                    fnd.PrimaryKeys = new List<EntityField>();
                    DataRow rt = tb.Rows[0];

                    foreach (DataRow r in rt.Table.Rows)
                    {
                        EntityField x = new EntityField();
                        try
                        {

                            x.fieldname = r.Field<string>("ColumnName");
                            x.fieldtype = (r.Field<Type>("DataType")).ToString(); //"ColumnSize"
                            x.Size1 = r.Field<int>("ColumnSize");
                            try
                            {
                                x.IsAutoIncrement = r.Field<bool>("IsAutoIncrement");
                            }
                            catch (Exception)
                            {

                            }
                            try
                            {
                                x.AllowDBNull = r.Field<bool>("AllowDBNull");
                            }
                            catch (Exception)
                            {


                            }
                            try
                            {
                                x.IsAutoIncrement = r.Field<bool>("IsIdentity");
                            }
                            catch (Exception)
                            {


                            }
                            try
                            {
                                x.IsKey = r.Field<bool>("IsKey");
                            }
                            catch (Exception)
                            {

                            }
                            try
                            {
                                x.NumericPrecision = r.Field<short>("NumericPrecision");
                                x.NumericScale = r.Field<short>("NumericScale");


                            }
                            catch (Exception)
                            {

                            }
                            try
                            {
                                x.IsUnique = r.Field<bool>("IsUnique");
                            }
                            catch (Exception)
                            {

                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog("Error in Creating Field Type");
                            ErrorObject.Flag = Errors.Failed;
                            ErrorObject.Ex = ex;
                        }

                        if (x.IsKey)
                        {
                            fnd.PrimaryKeys.Add(x);
                        }


                        fnd.Fields.Add(x);
                    }
                    if (fnd.Viewtype == ViewType.Table)
                    {
                        if ((fnd.Relations.Count == 0) || refresh)
                        {
                            fnd.Relations = GetEntityforeignkeys(fnd.EntityName, Dataconnection.ConnectionProp.SchemaName);
                        }
                    }

                    EntityStructure exist = Entities.Where(d => d.EntityName == fnd.EntityName).FirstOrDefault();
                    if (exist == null)
                    {
                        Entities.Add(fnd);
                    }
                    else
                    {
                        if (fnd != exist)
                        {
                            Entities.Where(x => x.EntityName == fnd.EntityName).FirstOrDefault().Fields = new List<EntityField>();
                            Entities.Where(x => x.EntityName == fnd.EntityName).FirstOrDefault().Relations = new List<RelationShipKeys>();
                            Entities.Where(x => x.EntityName == fnd.EntityName).FirstOrDefault().Fields = fnd.Fields;
                            Entities.Where(x => x.EntityName == fnd.EntityName).FirstOrDefault().Relations = fnd.Relations;
                        }

                    }

                }

            }
            else
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
            }

         //  DMEEditor.classCreator.CreateClass(fnd.EntityName, fnd.Fields, DMEEditor.ConfigEditor.Config.EntitiesPath, "TheTechIdea");
            return fnd;
        }
        public virtual DataTable RunQuery(string qrystr)
        {
            ErrorObject.Flag = Errors.Ok;
            IDbCommand cmd = GetDataCommand();
            try
            {

                DataTable dt = new DataTable();

                cmd.CommandText = qrystr;
                dt.Load(cmd.ExecuteReader(CommandBehavior.Default));
                cmd.Dispose();
                return dt;
            }

            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                cmd.Dispose();
                Logger.WriteLog($"Error in getting entity Data ({ex.Message}) ");
                return null;
            }


        }
        public virtual Task<object> GetEntityDataAsync(string EntityName, string QueryString)
        {
            ErrorObject.Flag = Errors.Ok;
            EntityStructure enttype = GetEntityStructure(EntityName);
            Type type = GetEntityType(EntityName);
            EntityName = EntityName.ToLower();
            string qrystr = "select * from " + EntityName;
            List<object> recs = new List<object>();
            if (QueryString != null)
            {
                qrystr = QueryString;
            }
            else
            {
                qrystr = "select * from " + EntityName;

            }
            try
            {

                IDataAdapter adp = GetDataAdapter(qrystr);
                DataSet dataSet = new DataSet();
                adp.Fill(dataSet);
                DataTable dt = dataSet.Tables[0];


                recs = DMEEditor.Utilfunction.GetListByDataTable(dt, type, enttype);

            }

            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting entity Data ({ex.Message}) ");
            }

            return Task.FromResult<object>(recs);
        }
        public virtual DataTable GetEntity(string EntityName, string QueryString)
        {
            ErrorObject.Flag = Errors.Ok;
            //  int LoadedRecord;
            // EntityStructure enttype = GetEntityDataType(EntityName);
            EntityName = EntityName.ToLower();
            string qrystr = "select * from " + EntityName;
            if (string.IsNullOrEmpty(QueryString) && string.IsNullOrWhiteSpace(QueryString))
            {
                qrystr = "select * from " + EntityName;
            }
            else
            {
                qrystr = QueryString;
            }
            try
            {

                IDataAdapter adp = GetDataAdapter(qrystr);
                DataSet dataSet = new DataSet();
                adp.Fill(dataSet);
                DataTable dt = dataSet.Tables[0];
              
                return dt;
            }

            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting entity Data ({ex.Message}) ");
                return null;
            }


        }
        public virtual IErrorsInfo UpdateEntities(string EntityName, object UploadData, IMapping_rep Mapping = null)
        {

            //  RunCopyDataBackWorker(EntityName,  UploadData,  Mapping );
            #region "Update Code"
            string str;
            IDbTransaction sqlTran;
            DataTable tb = (DataTable)UploadData;
           // DMEEditor.classCreator.CreateClass();
            //List<object> f = DMEEditor.Utilfunction.GetListByDataTable(tb);
            ErrorObject.Flag = Errors.Ok;
            EntityStructure DataStruct = GetEntityStructure(EntityName);

            IDbCommand command = Dataconnection.DbConn.CreateCommand();


            int CurrentRecord = 0;
            int highestPercentageReached = 0;
            int numberToCompute = 0;
            try
            {
                if (tb != null)
                {

                    numberToCompute = tb.Rows.Count;
                    tb.TableName = EntityName;
                    // int i = 0;
                    string updatestring = null;
                    DataTable changes = tb;//.GetChanges();
                    for (int i = 0; i < tb.Rows.Count; i++)
                    {
                        try
                        {
                            DataRow r = tb.Rows[i];

                            CurrentRecord = i;
                            switch (r.RowState)
                            {
                                case DataRowState.Unchanged:
                                case DataRowState.Added:
                                    updatestring = GetInsertString(EntityName, r, DataStruct);


                                    break;
                                case DataRowState.Deleted:
                                    updatestring = GetDeleteString(EntityName, r, DataStruct);
                                    break;
                                case DataRowState.Modified:
                                    updatestring = GetUpdateString(EntityName, r, DataStruct);
                                    break;
                                default:
                                    updatestring = GetInsertString(EntityName, r, DataStruct);
                                    break;
                            }
                            
                            command.CommandText = updatestring;
                            foreach (EntityField item in DataStruct.Fields)
                            {
                                IDbDataParameter parameter = command.CreateParameter();
                                parameter.Value = r[item.fieldname];
                                parameter.ParameterName = "p_"+item.fieldname;
                              //  parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                                command.Parameters.Add(parameter);
                                
                            }


                            string msg = "";
                            int rowsUpdated = command.ExecuteNonQuery();
                            if (rowsUpdated > 0)
                            {
                                msg = $"Successfully I/U/D  Record {i} to {EntityName} : {updatestring}";
                            }
                            else
                            {
                                msg = $"Fail to I/U/D  Record {i} to {EntityName} : {updatestring}";
                            }
                            int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
                            if (percentComplete > highestPercentageReached)
                            {
                                highestPercentageReached = percentComplete;

                            }
                            PassedArgs args = new PassedArgs
                            {
                                CurrentEntity = EntityName,
                                DatasourceName = DatasourceName,
                                DataSource = this,
                                EventType = "UpdateEntity",


                            };
                            if (DataStruct.PrimaryKeys != null)
                            {
                                if (DataStruct.PrimaryKeys.Count <= 1)
                                {
                                    args.ParameterString1 = r[DataStruct.PrimaryKeys[0].fieldname].ToString();
                                }
                                if (DataStruct.PrimaryKeys.Count <= 2)
                                {
                                    args.ParameterString2 = r[DataStruct.PrimaryKeys[1].fieldname].ToString();
                                }
                                if (DataStruct.PrimaryKeys.Count == 3)
                                {
                                    args.ParameterString3 = r[DataStruct.PrimaryKeys[2].fieldname].ToString();

                                }
                            }
                            args.ParameterInt1 = percentComplete;

                            LScriptTracker tr = new LScriptTracker();
                            tr.currenrecordentity = EntityName;
                            tr.currentrecorddatasourcename = DatasourceName;
                            tr.currenrecordindex = i;
                            tr.scriptType = DDLScriptType.CopyData;
                            tr.errorsInfo = DMEEditor.ErrorObject;
                            tr.errormessage = msg;
                            DMEEditor.ETL.trackingHeader.trackingscript.Add(tr);

                            args.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
                            PassEvent?.Invoke(this, args);
                            DMEEditor.AddLogMessage("Success",msg, DateTime.Now, 0, null, Errors.Ok);
                        }
                        catch (Exception er)
                        {
                            PassedArgs args = new PassedArgs
                            {
                                CurrentEntity = EntityName,
                                DatasourceName = DatasourceName,
                                DataSource = this,
                                EventType = "UpdateEntity",


                            };
                            LScriptTracker tr = new LScriptTracker();
                            tr.currenrecordentity = EntityName;
                            tr.currentrecorddatasourcename = DatasourceName;
                            tr.currenrecordindex = i;
                            tr.scriptType = DDLScriptType.CopyData;
                            tr.errorsInfo = DMEEditor.ErrorObject;
                            tr.errormessage = $"Fail to insert/update/delete  Record {i} to {EntityName} : {er.Message} :  {updatestring} ";
                            DMEEditor.ETL.trackingHeader.trackingscript.Add(tr);
                            args.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
                            PassEvent?.Invoke(this, args);
                            //  DMEEditor.RaiseEvent(this, args);
                            DMEEditor.AddLogMessage("Fail", $"Fail to insert/update/delete  Record {i} to {EntityName} {er.Message}", DateTime.Now, 0, null, Errors.Failed);
                        }
                    }



                    command.Dispose();
                    //    sqlTran.Commit();
                    DMEEditor.AddLogMessage("Success", $"Finished Uploading Data to {EntityName}", DateTime.Now, 0, null, Errors.Ok);
                }


            }
            catch (Exception ex)
            {
                ErrorObject.Ex = ex;

                command.Dispose();
                try
                {
                    // Attempt to roll back the transaction.
                    //   sqlTran.Rollback();
                    str = "Unsuccessfully no Data has been written to Data Source,Rollback Complete";
                }
                catch (Exception exRollback)
                {
                    // Throws an InvalidOperationException if the connection
                    // is closed or the transaction has already been rolled
                    // back on the server.
                    // Console.WriteLine(exRollback.Message);
                    str = "Unsuccessfully no Data has been written to Data Source,Rollback InComplete";
                    ErrorObject.Ex = exRollback;
                }
                str = "Unsuccessfully no Data has been written to Data Source";


            }
            #endregion


            return ErrorObject;
        }
        public static DbType TypeToDbType(Type T)

        {

            switch (T.FullName)

            {

                case "System.Int64":

                    return DbType.Int64;

                    break;

                case "System.Int32":

                    return DbType.Int32;

                    break;

                case "System.Int16":

                    return DbType.Int16;

                    break;

                case "System.Decimal":

                    return DbType.Decimal;

                    break;

                case "System.Double":

                    return DbType.Double;

                    break;

                case "System.Boolean":

                    return DbType.Boolean;

                    break;

                case "System.String":

                    return DbType.String;

                    break;

                case "System.DateTime":

                    return DbType.DateTime;

              

                default:
                    
                    return DbType.Object;

                    break;

            }

        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow, IMapping_rep Mapping = null)
        {


           // DataRow tb = object UploadDataRow;
            ErrorObject.Flag = Errors.Ok;
            EntityStructure DataStruct = GetEntityStructure(EntityName, true);

            //   var sqlTran = Dataconnection.DbConn.BeginTransaction();
            IDbCommand command = Dataconnection.DbConn.CreateCommand();
            try
            {
                //string updatestring = GetUpdateString(EntityName, tb, DataStruct);

                ////  command.Transaction = sqlTran;
                //command.CommandText = updatestring;
                ////foreach (EntityField item in DataStruct.Fields)
                ////{
                ////    Parameter param = new SqlParameter();
                ////    param.ParameterName = "@City";
                ////    param.Value = inputCity;
                ////    command.Parameters.Add("@" + item.fieldname, tb[item.fieldname]) ;
                    

                ////}
                //command.ExecuteNonQuery();
                //command.Dispose();
                //  sqlTran.Commit();
                // DMEEditor.AddLogMessage("Success",$"Successfully Written Data to {EntityName}",DateTime.Now,0,null, Errors.Ok);

            }
            catch (Exception ex)
            {
                ErrorObject.Ex = ex;
                string str;
                command.Dispose();
                try
                {
                    // Attempt to roll back the transaction.
                    //     sqlTran.Rollback();
                    str = "Unsuccessfully no Data has been written to Data Source,Rollback Complete";
                }
                catch (Exception exRollback)
                {
                    // Throws an InvalidOperationException if the connection
                    // is closed or the transaction has already been rolled
                    // back on the server.
                    // Console.WriteLine(exRollback.Message);
                    str = "Unsuccessfully no Data has been written to Data Source,Rollback InComplete";
                    ErrorObject.Ex = exRollback;
                }
                str = "Unsuccessfully no Data has been written to Data Source";
                Logger.WriteLog($"{str}  {ErrorObject.Ex.Message}");
                ErrorObject.Flag = Errors.Failed;

            }

            return ErrorObject;
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow, IMapping_rep Mapping = null)
        {
            DataRow tb = (DataRow)DeletedDataRow;
            ErrorObject.Flag = Errors.Ok;
            EntityStructure DataStruct = GetEntityStructure(EntityName, true);

            var sqlTran = Dataconnection.DbConn.BeginTransaction();
            IDbCommand command = Dataconnection.DbConn.CreateCommand();
            try
            {
                string updatestring = GetDeleteString(EntityName, tb, DataStruct);

                command.Transaction = sqlTran;
                command.CommandText = updatestring;
                command.ExecuteNonQuery();
                sqlTran.Commit();
                command.Dispose();
                Logger.WriteLog("Successfully Written Data to DataSource ");

            }
            catch (Exception ex)
            {
                ErrorObject.Ex = ex;
                string str;
                command.Dispose();
                try
                {
                    // Attempt to roll back the transaction.
                    sqlTran.Rollback();
                    str = "Unsuccessfully no Data has been written to Data Source,Rollback Complete";
                }
                catch (Exception exRollback)
                {
                    // Throws an InvalidOperationException if the connection
                    // is closed or the transaction has already been rolled
                    // back on the server.
                    // Console.WriteLine(exRollback.Message);
                    str = "Unsuccessfully no Data has been written to Data Source,Rollback InComplete";
                    ErrorObject.Ex = exRollback;
                }
                str = "Unsuccessfully no Data has been written to Data Source";
                Logger.WriteLog($"{str}  {ErrorObject.Ex.Message}");
                ErrorObject.Flag = Errors.Failed;

            }

            return ErrorObject;
        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            try
            {
                foreach (var item in entities)
                {
                    try
                    {
                        CreateEntityAs(item);
                    }
                    catch (Exception ex)
                    {

                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = ex.Message;
                        DMEEditor.AddLogMessage("Fail", $"Could not Create Entity {item.EntityName}" + ex.Message, DateTime.Now, -1, ex.Message, Errors.Failed);
                    }

                }
            }
            catch (Exception ex1)
            {

                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex1.Message;
                DMEEditor.AddLogMessage("Fail", " Could not Complete Create Entities" + ex1.Message, DateTime.Now, -1, ex1.Message, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual Type GetEntityType(string EntityName)
        {
            EntityStructure x = GetEntityStructure(EntityName);
            DMTypeBuilder.CreateNewObject(EntityName, EntityName, x.Fields);
            return DMTypeBuilder.myType;
        }
        public virtual List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;
            DataSet ds = new DataSet();
            try
            {

                if (EntitiesNames.Count() == 0)
                {
                    string sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getlistoftables, null, Dataconnection.ConnectionProp.SchemaName, null, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                    IDbDataAdapter adp = GetDataAdapter(sql, false);
                    adp.Fill(ds);

                    DataTable tb = new DataTable();
                    tb = ds.Tables[0];
                    EntitiesNames = new List<string>();
                    int i = 0;
                    foreach (DataRow row in tb.Rows)
                    {
                        EntitiesNames.Add(row.Field<string>("TABLE_NAME"));
                        //GetEntityStructure(row.Field<string>("TABLE_NAME"));
                        i += 1;
                    }


                    Logger.WriteLog("Successfully Retrieve tables list ");


                } else
                {
                    return EntitiesNames;
                }


            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Retrieve tables list {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            return EntitiesNames;



        }
        public string GetSchemaName()
        {
            string schemaname;
            if (Dataconnection.ConnectionProp.Database == null)
            {
                schemaname = null;
            }
            else
            {
                schemaname = Dataconnection.ConnectionProp.Database.ToUpper();
            }
            if (Dataconnection.ConnectionProp.DatabaseType == DataSourceType.SqlServer)
            {
                schemaname = "dbo";
            }
            return schemaname;
        }
        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;
            if (EntitiesNames.Count == 0)
            {
                GetEntitesList();
            }
            retval = EntitiesNames.ConvertAll(d => d.ToUpper()).Contains(EntityName.ToUpper());

            return retval;
        }
        public bool CreateEntityAs(EntityStructure entity)
        {
            bool retval = false;

            if (CheckEntityExist(entity.EntityName) == false)
            {

                CreateEntity(entity);
                if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                {
                    retval = false;
                }
                else
                {
                    retval = true;
                }
            } else
            {
                retval = true;
            }

            return retval;
        }
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            List<RelationShipKeys> fk = new List<RelationShipKeys>();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                List<ChildRelation> ds = GetTablesFKColumnList(entityname, SchemaName, null);
                //  var tbl = ds.Tables[0];

                //-------------------------------
                // Create Parent Record First
                //-------------------------------
                if (ds != null)
                {
                    if (ds.Count > 0)
                    {
                        foreach (ChildRelation r in ds)
                        {
                            RelationShipKeys rfk = new RelationShipKeys
                            {
                                ParentEntityID = r.parent_table,
                                ParentEntityColumnID = r.parent_column,
                                EntityColumnID = r.child_column,





                            };
                            try
                            {
                                rfk.RalationName = r.Constraint_Name;
                            }
                            catch (Exception ex)
                            {
                                ErrorObject.Flag = Errors.Failed;
                                ErrorObject.Ex = ex;
                            }
                            fk.Add(rfk);
                        }
                    }
                }


            }

            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in Loading Goreign Key for Table View ({ex.Message}) ");

            }
            return fk;
        }
        public virtual List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            ErrorObject.Flag = Errors.Ok;
            //  DataSet ds = new DataSet();
            try
            {
                string sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getChildTable, tablename, SchemaName, Filterparamters, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                //IDbDataAdapter adp = GetDataAdapter(sql, false);
                //adp.Fill(ds);
                //return ds;
                return GetData<ChildRelation>(sql);
                //  Logger.WriteLog("Successfully Retrieve Child Table list");

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Retrieve Child tables list {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                return null;
            }




        }
        public LScript RunScript(LScript scripts)
        {
            var t = Task.Run<IErrorsInfo>(() => { return ExecuteSql(scripts.ddl); });
            t.Wait();
            scripts.errorsInfo = t.Result;
            scripts.errormessage = DMEEditor.ErrorObject.Message;
            trackingHeader.currenrecordentity = scripts.entityname;
            trackingHeader.currentrecorddatasourcename = scripts.destinationdatasourcename;

            trackingHeader.scriptType = scripts.scriptType;
            trackingHeader.errorsInfo = scripts.errorsInfo;
            PassedArgs x = new PassedArgs();
            x.CurrentEntity = trackingHeader.currenrecordentity;
            x.DatasourceName = DatasourceName;
            x.CurrentEntity = scripts.entityname;
            x.Objects.Add(new ObjectItem { obj = trackingHeader, Name = "TrackingHeader" });

            return scripts;
        }
        public List<LScript> GetCreateEntityScript(List<EntityStructure> entities)
        {
            return GetDDLScriptfromDatabase(entities);
        }
        #endregion
        #region "RDBSSource Database Methods"
        public static ObservableCollection<T> ToObservable<T>(IEnumerable<T> source)
        {
            return new ObservableCollection<T>(source);
        }
        public List<LScript> GenerateCreatEntityScript(List<EntityStructure> entities)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            int i = 0;

            List<LScript> rt = new List<LScript>();
            try
            {
                // Generate Create Table First
                foreach (EntityStructure item in entities)
                {
                    LScript x = new LScript();
                    x.destinationdatasourcename = DatasourceName;

                    x.ddl = CreateEntity(item);
                    x.entityname = item.EntityName;
                    x.scriptType = DDLScriptType.CreateTable;
                    rt.Add(x);
                    rt.AddRange(CreateForKeyRelationScripts(item));
                    i += 1;
                }


                DMEEditor.AddLogMessage("Success", $"Generated Script", DateTime.Now, 0, null, Errors.Ok);

            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

        }
        public List<LScript> GenerateCreatEntityScript(EntityStructure entity)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            int i = 0;

            List<LScript> rt = new List<LScript>();
            try
            {
                // Generate Create Table First

                LScript x = new LScript();
                x.destinationdatasourcename = DatasourceName;
                x.ddl = CreateEntity(entity);
                x.entityname = entity.EntityName;
                x.scriptType = DDLScriptType.CreateTable;
                rt.Add(x);
                rt.AddRange(CreateForKeyRelationScripts(entity));

                DMEEditor.AddLogMessage("Success", $"Generated Script", DateTime.Now, 0, null, Errors.Ok);

            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

        }
        private List<LScript> GetDDLScriptfromDatabase(string entity)
        {
            List<LScript> rt = new List<LScript>();

            try
            {



                var t = Task.Run<EntityStructure>(() => { return GetEntityStructure(entity, true); });
                t.Wait();
                EntityStructure entstructure = t.Result;

                entstructure.Created = false;
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {

                    Entities[Entities.FindIndex(x => x.EntityName == entity)] = entstructure;

                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", $"Error getting entity structure for {entity}", DateTime.Now, entstructure.Id, entstructure.DataSourceID, Errors.Failed);
                }





                var t2 = Task.Run<List<LScript>>(() => { return GenerateCreatEntityScript(entstructure); });
                t2.Wait();
                rt.AddRange(t2.Result);
                t2 = Task.Run<List<LScript>>(() => { return CreateForKeyRelationScripts(entstructure); });
                t2.Wait();
                rt.AddRange(t2.Result);

            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return rt;
        }
        private List<LScript> GetDDLScriptfromDatabase(List<EntityStructure> structureentities)
        {
            List<LScript> rt = new List<LScript>();
            // List<EntityStructure> structureentities = new List<EntityStructure>();
            try
            {



                if (structureentities.Count > 0)
                {

                    var t = Task.Run<List<LScript>>(() => { return GenerateCreatEntityScript(structureentities); });
                    t.Wait();
                    rt.AddRange(t.Result);
                    //t = Task.Run<List<LScript>>(() => { return CreateForKeyRelationScripts(structureentities); });
                    //t.Wait();
                    //rt.AddRange(t.Result);
                }
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return rt;
        }
        private string CreatePrimaryKeyString(EntityStructure t1)
        {
            string retval = null;
            //  string tmp = null;
            try
            {
                retval = @" PRIMARY KEY ( ";
                ErrorObject.Flag = Errors.Ok;

                int i = 0;

                foreach (EntityField dbf in t1.PrimaryKeys)
                {

                    retval += dbf.fieldname + ",";

                    i += 1;


                }
                if (retval.EndsWith(","))
                {
                    retval = retval.Remove(retval.Length - 1, 1);

                }

                retval += ")\n";
                return retval;
            }

            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not  Create Primery Key" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };




        }
        private string CreateAlterRalationString(EntityStructure t1)
        {
            string retval = "";
            ErrorObject.Flag = Errors.Ok;
            try
            {


                int i = 0;
                //string keyname="";
                //      retval = "FOREIGN KEY (" + fk.EntityColumnSequenceID + ")  REFERENCES " + fk.ParentEntityID + "(" + fk.ParentColumnSequenceID + "), \n";
                foreach (string item in t1.Relations.Select(o => o.ParentEntityID).Distinct())
                {
                    string forkeys = "";
                    string refkeys = "";
                    foreach (RelationShipKeys fk in t1.Relations.Where(p => p.ParentEntityID == item))
                    {

                        forkeys += fk.EntityColumnID + ",";
                        refkeys += fk.ParentEntityColumnID + ",";
                        // keyname = fk.EntityColumnID.Substring(1, 3) + fk.ParentEntityColumnID.Substring(1, 3);


                    }
                    i += 1;


                    forkeys = forkeys.Remove(forkeys.Length - 1, 1);
                    refkeys = refkeys.Remove(refkeys.Length - 1, 1);
                    retval += @" ALTER TABLE " + t1.EntityName + " ADD CONSTRAINT " + t1.EntityName + i + r.Next(10, 1000) + "  FOREIGN KEY (" + forkeys + ")  REFERENCES " + item + "(" + refkeys + ") \n";
                }


                if (i >= 1)
                {
                    retval = retval.Remove(retval.Length - 2);
                    retval += "\n";
                }
                else
                {
                    retval = "";
                }

                return retval;
            }

            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not Create Relation" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };




        }
        private List<LScript> CreateForKeyRelationScripts(EntityStructure entity)
        {
            List<LScript> rt = new List<LScript>();
            try
            {
                int i = 0;
                IDataSource ds;
                // Generate Forign Keys

                if (entity.Relations != null)
                {
                    if (entity.Relations.Count > 0)
                    {
                        LScript x = new LScript();
                        x.destinationdatasourcename = DatasourceName;
                        ds = DMEEditor.GetDataSource(entity.DataSourceID);
                        x.ddl = CreateAlterRalationString(entity);
                        x.entityname = entity.EntityName;
                        x.scriptType = DDLScriptType.AlterFor;
                        rt.Add(x);

                        i += 1;

                    }
                }


            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in getting For. Keys from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }

            return rt;
        }
        private List<LScript> CreateForKeyRelationScripts(List<EntityStructure> entities)
        {
            List<LScript> rt = new List<LScript>();

            try
            {
                int i = 0;
                IDataSource ds;
                // Generate Forign Keys
                //alteraddForignKey = new List<DDLScript>();
                foreach (EntityStructure item in entities)
                {
                    if (item.Relations != null)
                    {
                        if (item.Relations.Count > 0)
                        {
                            LScript x = new LScript();
                            x.destinationdatasourcename = item.DataSourceID;
                            ds = DMEEditor.GetDataSource(item.DataSourceID);
                            x.ddl = CreateAlterRalationString(item);
                            x.entityname = item.EntityName;
                            x.scriptType = DDLScriptType.AlterFor;
                            rt.Add(x);
                            //alteraddForignKey.Add(x);
                            i += 1;
                            //          script += CreateRalationString(ds, item);
                        }
                    }

                }
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in getting For. Keys from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return rt;
        }
        private IErrorsInfo CreateAutoNumber(EntityField f, out string AutnumberString)
        {
            ErrorObject.Flag = Errors.Ok;
            AutnumberString = "";
            try
            {
                if (f.fieldCategory == DbFieldCategory.Numeric)
                {
                    switch (Dataconnection.ConnectionProp.DatabaseType)
                    {
                        //case DataSourceType.Excel:
                        //    break;
                        case DataSourceType.Mysql:
                            AutnumberString = "NULL AUTO_INCREMENT";
                            break;
                        case DataSourceType.Oracle:
                            AutnumberString = "CREATE SEQUENCE " + f.fieldname + "_seq MINVALUE 1 START WITH 1 INCREMENT BY 1 CACHE 1; ";
                            break;
                        case DataSourceType.SqlCompact:
                            AutnumberString = "IDENTITY(1,1)";
                            break;
                        case DataSourceType.SqlLite:
                            AutnumberString = "AUTOINCREMENT";
                            break;
                        case DataSourceType.SqlServer:
                            AutnumberString = "IDENTITY(1,1)";
                            break;
                        //case DataSourceType.Text:
                        //    break;
                        default:
                            AutnumberString = "";
                            break;
                    }
                }

                Logger.WriteLog($"Successed in AutoNumber Creation");
            }
            catch (System.Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;

                Logger.WriteLog($"Error in Relation ({ex.Message})");


            }

            return ErrorObject;
        }
        private string CreateEntity(EntityStructure t1)
        {
            string createtablestring = "Create table ";
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            try
            {//-- Create Create string
                int i = 1;
                t1.EntityName = Regex.Replace(t1.EntityName, @"\s+", "_");
                createtablestring += t1.EntityName + "\n(";
                if (t1.Fields.Count == 0)
                {
                    // t1=ds.GetEntityStructure()
                }
                foreach (EntityField dbf in t1.Fields)
                {

                    createtablestring += "\n" + dbf.fieldname + " " + DMEEditor.typesHelper.GetDataType(DatasourceName, dbf) + " ";
                    if (dbf.IsAutoIncrement)
                    {
                        dbf.fieldname = Regex.Replace(dbf.fieldname, @"\s+", "_");
                        string autonumberstring = "";
                        DMEEditor.ErrorObject = CreateAutoNumber(dbf, out autonumberstring);
                        if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                        {
                            createtablestring += autonumberstring;
                        }
                        else
                        {
                            throw new Exception(ErrorObject.Message);

                        }
                    }

                    if (dbf.AllowDBNull == false)
                    {
                        createtablestring += " NOT NULL ";
                    }
                    if (dbf.IsUnique == true)
                    {
                        createtablestring += " UNIQUE ";
                    }
                    i += 1;

                    if (i <= t1.Fields.Count)
                    {
                        createtablestring += ",";
                    }

                }
                if (t1.PrimaryKeys != null)
                {
                    if (t1.PrimaryKeys.Count > 0)
                    {
                        createtablestring += $",\n" + CreatePrimaryKeyString(t1);
                    }
                }



                if (createtablestring[createtablestring.Length - 1].Equals(","))
                {
                    createtablestring = createtablestring.Remove(createtablestring.Length);
                }

                createtablestring += ")";

            }
            catch (System.Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;

                Logger.WriteLog($"Error in  Creating Table " + t1.EntityName + "   ({ex.Message})");


            }
            return createtablestring;
        }
        public virtual string GetInsertString(string EntityName, DataRow row, EntityStructure DataStruct)
        {
            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();
            // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
            //   map= Mapping.FldMapping;
            //    EntityName = Regex.Replace(EntityName, @"\s+", "");
            string Insertstr = "insert into " + EntityName + " (";
            string Valuestr = ") values (";
            var insertfieldname = "";
            // string datafieldname = "";
            string typefield = "";
            int i = DataStruct.Fields.Count();
            int t = 0;
            foreach (EntityField item in DataStruct.Fields)
            {

                //if (!DBNull.Value.Equals(row[item.fieldname]))
                //{
                    // insertfieldname = Regex.Replace(item.fieldname, @"\s+", "");
                    Insertstr += item.fieldname + ",";
                    Valuestr += "@p_" + item.fieldname + ",";
                    //switch (item.fieldtype)
                    //{
                    //    case "System.String":
                    //        if (row[item.fieldname].ToString().Contains("'"))
                    //        {
                    //            string ve = row[item.fieldname].ToString();
                    //            ve = ve.Replace("'", "''");
                    //            Valuestr += "'" + ve + "',";
                    //        }
                    //        else
                    //        {
                    //            Valuestr += "'" + row[item.fieldname] + "',";
                    //        }


                    //        break;
                    //    case "System.Int":
                    //        Valuestr += "" + row[item.fieldname] + ",";
                    //        break;
                    //    case "System.DateTime":
                    //        DateTime time = (DateTime)row[item.fieldname];
                    //        Valuestr += "'" + time.ToString(dateformat) + "',";
                    //        break;
                    //    default:
                    //        Valuestr += "'" + row[item.fieldname] + "',";
                    //        break;
                    //}
                    //if (t == i)
                    //{
                    //    Insertstr += Valuestr + @"\n";
                    //}
                    //else
                    //{
                    //    Insertstr += Valuestr + @",\n";
                    //}
               // }


                t += 1;

            }



            Insertstr = Insertstr.Remove(Insertstr.Length - 1);
            Valuestr = Valuestr.Remove(Valuestr.Length - 1);
            Valuestr += ")";
            return Insertstr + Valuestr;
        }
        public virtual string GetUpdateString(string EntityName, DataRow row, EntityStructure DataStruct)
        {
            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();
            // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
            //   map= Mapping.FldMapping;
            //     EntityName = Regex.Replace(EntityName, @"\s+", "");
            string Updatestr = @"Update " + EntityName + "  set " + Environment.NewLine;
            string Valuestr = "";
            // var insertfieldname = "";
            //string datafieldname = "";
            //string typefield = "";
            int i = DataStruct.Fields.Count();
            int t = 0;
            foreach (EntityField item in DataStruct.Fields)
            {
                if (!DataStruct.PrimaryKeys.Any(l => l.fieldname == item.fieldname))
                {
                    if (!DBNull.Value.Equals(row[item.fieldname]))
                    {
                        //     insertfieldname = Regex.Replace(item.fieldname, @"\s+", "_");
                        Updatestr += item.fieldname + "=";
                        Updatestr += "@p_" + item.fieldname + ",";
                        //switch (item.fieldtype)
                        //{
                        //    case "System.String":
                        //        if (row[item.fieldname].ToString().Contains("'"))
                        //        {
                        //            string ve = row[item.fieldname].ToString();
                        //            ve = ve.Replace("'", "''");
                        //            Updatestr += "'" + ve + "',";
                        //        }
                        //        else
                        //        {
                        //            Updatestr += "'" + row[item.fieldname] + "',";
                        //        }

                        //        break;
                        //    case "System.Int":
                        //        Updatestr += "" + row[item.fieldname] + ",";
                        //        break;
                        //    case "System.DateTime":
                        //        DateTime time = (DateTime)row[item.fieldname];
                        //        Updatestr += "'" + time.ToString(dateformat) + "'";
                        //        break;
                        //    default:
                        //        Updatestr += "'" + row[item.fieldname] + "',";
                        //        break;
                        //}
                    }


                }


                t += 1;
            }

            Updatestr = Updatestr.Remove(Updatestr.Length - 1);

            Updatestr += @" where " + Environment.NewLine;
            i = DataStruct.PrimaryKeys.Count();
            t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys)
            {
                if (!DBNull.Value.Equals(row[item.fieldname]))
                {
                    if (t == 1)
                    {
                        Updatestr += item.fieldname + "=";
                    }
                    else
                    {
                        Updatestr += " and " + item.fieldname + "=";
                    }
                    Updatestr += "@p_" + item.fieldname + "";
                    //    Updatestr += item.fieldname + "=";
                    //switch (item.fieldtype)
                    //{
                    //    case "System.String":
                    //        Updatestr += "'" + row[item.fieldname] + "'";
                    //        break;
                    //    case "System.Int":
                    //        Updatestr += "" + row[item.fieldname] + "";
                    //        break;
                    //    case "System.DateTime":
                    //        DateTime time = (DateTime)row[item.fieldname];
                    //        Updatestr += "'" + time.ToString(dateformat) + "'";
                    //        break;
                    //    default:
                    //        Updatestr += "'" + row[item.fieldname] + "'";
                    //        break;
                    //}
                }


                t += 1;
            }
            //  Updatestr = Updatestr.Remove(Valuestr.Length - 1);
            return Updatestr;
        }
        public virtual string GetDeleteString(string EntityName, DataRow row, EntityStructure DataStruct)
        {

            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();
            // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
            //   map= Mapping.FldMapping;
            // EntityName = Regex.Replace(EntityName, @"\s+", "");
            string Updatestr = @"Delete from " + EntityName + "  ";
            string Valuestr = "";
            // var insertfieldname = "";
            //string datafieldname = "";
            //string typefield = "";
            int i = DataStruct.Fields.Count();
            int t = 0;

            Updatestr += @" where ";
            i = DataStruct.PrimaryKeys.Count();
            t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys)
            {
                string st = row[item.fieldname, DataRowVersion.Original].ToString();
                if (!DBNull.Value.Equals(row[item.fieldname, DataRowVersion.Original]))
                {
                    if (t == 1)
                    {
                        Updatestr += item.fieldname + "=";
                    }
                    else
                    {
                        Updatestr += " and " + item.fieldname + "=";
                    }

                    switch (item.fieldtype)
                    {
                        case "System.String":
                            Updatestr += "'" + row[item.fieldname, DataRowVersion.Original].ToString() + "'";
                            break;
                        case "System.Int":
                            Updatestr += "" + row[item.fieldname, DataRowVersion.Original].ToString() + "";
                            break;
                        default:
                            Updatestr += "'" + row[item.fieldname, DataRowVersion.Original].ToString() + "'";
                            break;
                    }
                }

                t += 1;
            }
            //   Updatestr= Updatestr.Remove(Updatestr.Length - 1);
            return Updatestr;
        }
        public virtual string GetInsertString(string EntityName, DataRow row, IMapping_rep Mapping, EntityStructure DataStruct)
        {

            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();
            // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
            //   map= Mapping.FldMapping;
            //  EntityName = Regex.Replace(EntityName, @"\s+", "");
            string Insertstr = "insert into " + EntityName + " (";
            string Valuestr = ") values (";
            var insertfieldname = "";
            string datafieldname = "";
            string typefield = "";
            int i = Mapping.FldMapping.Count();
            int t = 0;
            foreach (IMapping_rep_fields item in Mapping.FldMapping)
            {
                if (EntityName == Mapping.EntityName1)
                {
                    insertfieldname = item.FieldName1;
                    datafieldname = item.FieldName2;
                    typefield = item.FieldType2;
                }
                else
                {
                    insertfieldname = item.FieldName2;
                    datafieldname = item.FieldName1;
                    typefield = item.FieldType1;
                }
                if (!DBNull.Value.Equals(row[datafieldname]))
                {
                    //   insertfieldname = Regex.Replace(insertfieldname, @"\s+", "");
                    Insertstr += insertfieldname + ",";
                    switch (typefield)
                    {
                        case "System.String":
                            if (row[datafieldname].ToString().Contains("'"))
                            {
                                string ve = row[datafieldname].ToString();
                                ve = ve.Replace("'", "''");
                                Valuestr += "'" + ve + "',";
                            }
                            else
                            {
                                Valuestr += "'" + row[datafieldname] + "',";
                            }
                           
                            break;
                        case "System.Int":
                            Valuestr += "" + row[datafieldname] + ",";
                            break;
                        default:
                            Valuestr += "'" + row[datafieldname] + "',";
                            break;
                    }
                }

                t += 1;

            }



            Insertstr = Insertstr.Remove(Insertstr.Length - 1);
            Valuestr = Valuestr.Remove(Valuestr.Length - 1);
            Valuestr += ")";
            return Insertstr + Valuestr;
        }
        public virtual string GetUpdateString(string EntityName, DataRow row, IMapping_rep Mapping, EntityStructure DataStruct)
        {

            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();
            // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
            //   map= Mapping.FldMapping;
            // EntityName = Regex.Replace(EntityName, @"\s+", "");
            string Insertstr = @"Update " + EntityName + " ( \n set ";
            string Valuestr = "";
            var insertfieldname = "";
            string datafieldname = "";
            string typefield = "";
            int i = Mapping.FldMapping.Count();
            int t = 0;
            foreach (IMapping_rep_fields item in Mapping.FldMapping)
            {
                if (EntityName == Mapping.EntityName1)
                {
                    insertfieldname = item.FieldName1;
                    datafieldname = item.FieldName2;
                    typefield = item.FieldType2;
                }
                else
                {
                    insertfieldname = item.FieldName2;
                    datafieldname = item.FieldName1;
                    typefield = item.FieldType1;
                }
                if (!DBNull.Value.Equals(row[datafieldname]))
                {
                    // insertfieldname = Regex.Replace(insertfieldname, @"\s+", "");
                    Insertstr += insertfieldname + ",";
                    switch (typefield)
                    {
                        case "System.String":
                            if (row[datafieldname].ToString().Contains("'"))
                            {
                                string ve = row[datafieldname].ToString();
                                ve = ve.Replace("'", "''");
                                Valuestr += "'" + ve + "',";
                            }
                            else
                            {
                                Valuestr += "'" + row[datafieldname] + "',";
                            }
                            break;
                        case "System.Int":
                            Valuestr += "" + row[datafieldname] + ",";
                            break;
                        default:
                            Valuestr += "'" + row[datafieldname] + "',";
                            break;
                    }
                }



                t += 1;

            }



            Insertstr = Insertstr.Remove(Insertstr.Length - 1);
            Valuestr = Valuestr.Remove(Valuestr.Length - 1);
            Valuestr += ")";
            return Insertstr + Valuestr;
        }
        public virtual IDataReader GetDataReader(string querystring)
        {
            IDbCommand cmd = GetDataCommand();
            cmd.CommandText = querystring;
            IDataReader dt = cmd.ExecuteReader();
            return dt;

        }
        //------------- Methods using Dapper --------------------
        public virtual List<T> GetData<T>(string sql)
        {
            var retval = Dataconnection.DbConn.Query<T>(sql);

            return retval.AsList<T>();
        }
        public virtual Task SaveData<T>(string sql, T parameters)
        {
            return Dataconnection.DbConn.ExecuteAsync(sql, parameters);

        }
        //-------------------------------------------------------

        private int GetCtorForAdapter(List<ConstructorInfo> ls)
        {

            int i = 0;
            foreach (ConstructorInfo c in ls)
            {
                ParameterInfo[] d = c.GetParameters();
                if (d.Length == 2)
                {
                    if (d[0].ParameterType == System.Type.GetType("System.String"))
                    {
                        if (d[1].ParameterType != System.Type.GetType("System.String"))
                        {
                            return i;
                        }
                    }
                }

                i += 1;
            }
            return i;

        }
        private int GetCtorForCommandBuilder(List<ConstructorInfo> ls)
        {

            int i = 0;
            foreach (ConstructorInfo c in ls)
            {
                ParameterInfo[] d = c.GetParameters();
                if (d.Length == 1)
                {
                    return i;

                }

                i += 1;
            }
            return i;

        }
        public virtual IDbCommand GetDataCommand()
        {
            IDbCommand cmd = null;
            ErrorObject.Flag = Errors.Ok;
            try
            {
                cmd = Dataconnection.DbConn.CreateCommand();
                //    Logger.WriteLog("Created Data Command");

            }
            catch (Exception ex)
            {

                cmd = null;

                DMEEditor.AddLogMessage("Fail", $"Error in Creating Data Command {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);

            }
            return cmd;
        }
        public virtual IDbDataAdapter GetDataAdapter(string Sql, bool tablequery_CRUD = true)
        {
            IDbDataAdapter adp = null;
            //  DbCommandBuilder cmdb = null;
            //  IDbCommand cmd=null;
            try
            {
                ConnectionDriversConfig driversConfig = DMEEditor.Utilfunction.LinkConnection2Drivers(Dataconnection.ConnectionProp);


                string adtype = Dataconnection.DataSourceDriver.AdapterType;
                string cmdtype = Dataconnection.DataSourceDriver.CommandBuilderType;
                string cmdbuildername = driversConfig.CommandBuilderType;
                Type adcbuilderType = DMEEditor.Utilfunction.GetTypeFromString(cmdbuildername);
                List<ConstructorInfo> lsc = DMEEditor.Utilfunction.GetInstance(adtype).GetType().GetConstructors().ToList(); ;
                List<ConstructorInfo> lsc2 = DMEEditor.Utilfunction.GetInstance(cmdbuildername).GetType().GetConstructors().ToList(); ;

                ConstructorInfo ctor = lsc[GetCtorForAdapter(lsc)];
                ConstructorInfo BuilderConstructer = lsc2[GetCtorForCommandBuilder(adcbuilderType.GetConstructors().ToList())];
                ObjectActivator<IDbDataAdapter> adpActivator = GetActivator<IDbDataAdapter>(ctor);
                ObjectActivator<DbCommandBuilder> cmdbuilderActivator = GetActivator<DbCommandBuilder>(BuilderConstructer);

                //create an instance:
                adp = (IDbDataAdapter)adpActivator(Sql, Dataconnection.DbConn);


                try
                {
                    DbCommandBuilder cmdBuilder = cmdbuilderActivator(adp);
                    adp.InsertCommand = cmdBuilder.GetInsertCommand(true);
                    adp.UpdateCommand = cmdBuilder.GetUpdateCommand(true);
                    adp.DeleteCommand = cmdBuilder.GetDeleteCommand(true);

                }
                catch (Exception ex)
                {

                    DMEEditor.AddLogMessage("Fail", $"Error in Creating builder commands {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
                }

                adp.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                adp.MissingMappingAction = MissingMappingAction.Passthrough;

                Logger.WriteLog("Created Adapter");
                ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in Creating Adapter {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
                adp = null;
            }

            return adp;
        }
        public virtual DataTable GetTableSchema(string TableName)
        {
            ErrorObject.Flag = Errors.Ok;
            DataTable tb = new DataTable();
            IDataReader reader;
            IDbCommand cmd = GetDataCommand();
            try
            {

                if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.SchemaName) && !string.IsNullOrWhiteSpace(Dataconnection.ConnectionProp.SchemaName))
                {
                    TableName = Dataconnection.ConnectionProp.SchemaName + "." + TableName;
                }
                cmd.CommandText = "Select * from " + TableName.ToLower();// + " where 1=2";
                reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);

                tb = reader.GetSchemaTable();
                reader.Close();
                cmd.Dispose();
                Logger.WriteLog("Executed Sql Successfully");

            }
            catch (Exception ex)
            {

                Logger.WriteLog($"unsuccessfully Executed Sql ({ex.Message})");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            //}

            return tb;
        }
        public virtual IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            // CurrentSql = sql;
            IDbCommand cmd = GetDataCommand();
            if (cmd != null)
            {
                try
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                    //    DMEEditor.AddLogMessage("Success", "Executed Sql Successfully", DateTime.Now, -1, "Ok", Errors.Ok);
                }
                catch (Exception ex)
                {
                    // Logger.WriteLog("Error Running Script ");
                    ErrorObject.Flag = Errors.Failed;
                    cmd.Dispose();
                    //ErrorObject.Message = "Error Running Script ";
                    DMEEditor.AddLogMessage("Fail", " Could not run Script" + ex.Message, DateTime.Now, -1, ex.Message, Errors.Failed);

                }

            }

            return ErrorObject;
        }
        public virtual List<ChildRelation> GetTablesFKColumnList(string tablename, string SchemaName, string Filterparamters)
        {
            ErrorObject.Flag = Errors.Ok;
            DataSet ds = new DataSet();
            try
            {
                string sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getFKforTable, tablename, SchemaName, Filterparamters, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                //IDbDataAdapter adp = GetDataAdapter(sql, false);
                // adp.Fill(ds);
                return GetData<ChildRelation>(sql);
                // Logger.WriteLog("Successfully Retrieve Child Table list");

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Retrieve Child tables list {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                DMEEditor.AddLogMessage("Fail", $"Unsuccessfully Retrieve Child tables list {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
                return null;
            }



        }
        public virtual string DisableFKConstraints(EntityStructure t1)
        {
            throw new NotImplementedException();
        }
        public virtual string EnableFKConstraints(EntityStructure t1)
        {
            throw new NotImplementedException();
        }

        #endregion
       






    }

}
