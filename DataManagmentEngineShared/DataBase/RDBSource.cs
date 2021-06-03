using System;
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
using TheTechIdea.DataManagment_Engine.Report;

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
        public RDBDataConnection RDBMSConnection { get { return (RDBDataConnection)Dataconnection; } }
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
                ErrorObject = ErrorObject,
                DMEEditor=pDMEEditor
                
            };


        }
        #region "IDataSource Interface Methods"
        public ConnectionState Openconnection()
        {
           if (RDBMSConnection != null)
            {
                ConnectionStatus= RDBMSConnection.OpenConnection();
                
                Dataconnection.ConnectionStatus = ConnectionStatus;
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            if (RDBMSConnection != null)
            {
                ConnectionStatus = RDBMSConnection.CloseConn();

            }
            return ConnectionStatus;
        }

        #region "Repo Methods"
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
        public virtual object RunQuery(string qrystr)
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
       
        public virtual IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        {
            if (UploadData.GetType().ToString() != "System.Data.DataTable")
            {
                DMEEditor.AddLogMessage("Fail", $"Please use DataTable for this Method {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                return DMEEditor.ErrorObject;
            }
            //  RunCopyDataBackWorker(EntityName,  UploadData,  Mapping );
            #region "Update Code"
            string str;
            IDbTransaction sqlTran;
           
            DataTable tb = (DataTable)UploadData;
            // DMEEditor.classCreator.CreateClass();
            //List<object> f = DMEEditor.Utilfunction.GetListByDataTable(tb);
            ErrorObject.Flag = Errors.Ok;
            EntityStructure DataStruct = GetEntityStructure(EntityName);

            IDbCommand command = RDBMSConnection.DbConn.CreateCommand();


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
                                    updatestring = GetInsertString(EntityName,  DataStruct);


                                    break;
                                case DataRowState.Deleted:
                                    updatestring = GetDeleteString(EntityName,  DataStruct);
                                    break;
                                case DataRowState.Modified:
                                    updatestring = GetUpdateString(EntityName,  DataStruct);
                                    break;
                                default:
                                    updatestring = GetInsertString(EntityName,  DataStruct);
                                    break;
                            }

                            command.CommandText = updatestring;
                            foreach (EntityField item in DataStruct.Fields)
                            {
                                IDbDataParameter parameter = command.CreateParameter();
                                parameter.Value = r[item.fieldname];
                                parameter.ParameterName = "p_" + item.fieldname;
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
                            DMEEditor.AddLogMessage("Success", msg, DateTime.Now, 0, null, Errors.Ok);
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
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {


            // DataRow tb = object UploadDataRow;
            ErrorObject.Flag = Errors.Ok;
            EntityStructure DataStruct = GetEntityStructure(EntityName, true);
            DataRowView dv;
            DataTable tb;
            DataRow dr;
            string msg = "";
            //   var sqlTran = Dataconnection.DbConn.BeginTransaction();
            IDbCommand command = RDBMSConnection.DbConn.CreateCommand();
            Type enttype = GetEntityType(EntityName);
            var ti = Activator.CreateInstance(enttype);
            // ICustomTypeDescriptor, IEditableObject, IDataErrorInfo, INotifyPropertyChanged
            if (UploadDataRow.GetType().FullName == "System.Data.DataRowView")
            {
                dv = (DataRowView)UploadDataRow;
                dr = dv.Row;
                //foreach (EntityField col in DataStruct.Fields)
                //{
                //    // TrySetProperty<enttype>(ti, dr[col.fieldname], null);
                //    //if (dr[col.fieldname] != System.DBNull.Value)
                //    //{
                //        System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(col.fieldname);
                //        PropAInfo.SetValue(ti, dr[col.fieldname], null);
                //    //}

                //}

            } else 
            if (UploadDataRow.GetType().FullName == "System.Data.DataRow")
            {
                dr = (DataRow)UploadDataRow;
            }
            else
            {
                dr = DMEEditor.Utilfunction.ConvertItemClassToDataRow(DataStruct);
                foreach (EntityField col in DataStruct.Fields)
                {
                    System.Reflection.PropertyInfo GetPropAInfo = UploadDataRow.GetType().GetProperty(col.fieldname);

                    //if (GetPropAInfo.GetValue(UploadDataRow) != System.DBNull.Value)
                    //{
                    System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(col.fieldname);
                    dynamic result = GetPropAInfo.GetValue(UploadDataRow);

                    if (result == null)
                    {
                        result = System.DBNull.Value;
                    }


                    dr[col.fieldname] = result;
                }
            }
               
            
            try
            {
                string updatestring = GetUpdateString(EntityName,  DataStruct);


                command.CommandText = updatestring;
                foreach (EntityField item in DataStruct.Fields)
                {
                    IDbDataParameter parameter = command.CreateParameter();
                    //System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(item.fieldname);
                    //var v = PropAInfo.GetValue(ti);

                   // parameter.Value = dr[item.fieldname];
                    parameter.ParameterName = "p_" + item.fieldname;
                    if (item.fieldtype == "System.DateTime")
                    {
                        parameter.DbType = DbType.DateTime;
                        parameter.Value = DateTime.Parse(dr[item.fieldname].ToString());


                    }else
                    { parameter.Value = dr[item.fieldname]; }
                    //  parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                    command.Parameters.Add(parameter);

                }


              
                int rowsUpdated = command.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    msg = $"Successfully Updated  Record  to {EntityName} : {updatestring}";
                    DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    msg = $"Fail to Updated  Record  to {EntityName} : {updatestring}";
                    DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);
                }
                

            }
            catch (Exception ex)
            {
                ErrorObject.Ex = ex;
               
                command.Dispose();
                try
                {
                    // Attempt to roll back the transaction.
                    //     sqlTran.Rollback();
                    msg = "Unsuccessfully no Data has been written to Data Source,Rollback Complete";
                }
                catch (Exception exRollback)
                {
                    // Throws an InvalidOperationException if the connection
                    // is closed or the transaction has already been rolled
                    // back on the server.
                    // Console.WriteLine(exRollback.Message);
                    msg = "Unsuccessfully no Data has been written to Data Source,Rollback InComplete";
                    ErrorObject.Ex = exRollback;
                }
                msg = "Unsuccessfully no Data has been written to Data Source";
                DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);

            }

            return ErrorObject;
        }
        public virtual IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
          
            ErrorObject.Flag = Errors.Ok;
            EntityStructure DataStruct = GetEntityStructure(EntityName, true);
            string msg;
            DataRowView dv;
            DataTable tb;
            DataRow dr;
            var sqlTran = RDBMSConnection.DbConn.BeginTransaction();
            IDbCommand command = RDBMSConnection.DbConn.CreateCommand();
            Type enttype = GetEntityType(EntityName);
            var ti = Activator.CreateInstance(enttype);
            // ICustomTypeDescriptor, IEditableObject, IDataErrorInfo, INotifyPropertyChanged
            if (DeletedDataRow.GetType().FullName == "System.Data.DataRowView")
            {
                dv = (DataRowView)DeletedDataRow;
                dr = dv.Row;
                //foreach (EntityField col in DataStruct.Fields)
                //{
                //    // TrySetProperty<enttype>(ti, dr[col.fieldname], null);
                //    //if (dr[col.fieldname] != System.DBNull.Value)
                //    //{
                //        System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(col.fieldname);
                //        PropAInfo.SetValue(ti, dr[col.fieldname], null);
                //    //}

                //}

            }
            else
               if (DeletedDataRow.GetType().FullName == "System.Data.DataRow")
            {
                dr = (DataRow)DeletedDataRow;
            }
            else
            {
                // Get where condition

                dr = DMEEditor.Utilfunction.ConvertItemClassToDataRow(DataStruct);
                foreach (EntityField col in DataStruct.Fields)
                {
                    System.Reflection.PropertyInfo GetPropAInfo = DeletedDataRow.GetType().GetProperty(col.fieldname);

                    //if (GetPropAInfo.GetValue(UploadDataRow) != System.DBNull.Value)
                    //{
                    System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(col.fieldname);
                    dynamic result = GetPropAInfo.GetValue(DeletedDataRow);

                    if (result == null)
                    {
                        result = System.DBNull.Value;
                    }


                    dr[col.fieldname] = result;
                }
            }
            try
            {
                string updatestring = GetDeleteString(EntityName, DataStruct);
                command.Transaction = sqlTran;
                command.CommandText = updatestring;

                foreach (EntityField item in DataStruct.Fields)
                {
                    IDbDataParameter parameter = command.CreateParameter();
                    //System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(item.fieldname);
                    //var v = PropAInfo.GetValue(ti);

                    parameter.ParameterName = "p_" + item.fieldname;
                    if (item.fieldtype == "System.DateTime")
                    {
                        parameter.DbType = DbType.DateTime;
                        parameter.Value = DateTime.Parse(dr[item.fieldname].ToString());


                    }
                    else
                    { parameter.Value = dr[item.fieldname]; }
                    //  parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                    command.Parameters.Add(parameter);

                }



                int rowsUpdated = command.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    msg = $"Successfully Deleted  Record  to {EntityName} : {updatestring}";
                    DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    msg = $"Fail to Delete Record  from {EntityName} : {updatestring}";
                    DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);
                }
                sqlTran.Commit();
                command.Dispose();
               

            }
            catch (Exception ex)
            {
                ErrorObject.Ex = ex;
               
                command.Dispose();
                try
                {
                    // Attempt to roll back the transaction.
                    sqlTran.Rollback();
                    msg = "Unsuccessfully no Data has been written to Data Source,Rollback Complete";
                }
                catch (Exception exRollback)
                {
                    // Throws an InvalidOperationException if the connection
                    // is closed or the transaction has already been rolled
                    // back on the server.
                    // Console.WriteLine(exRollback.Message);
                    msg = "Unsuccessfully no Data has been written to Data Source,Rollback InComplete";
                    ErrorObject.Ex = exRollback;
                }
                msg = "Unsuccessfully no Data has been written to Data Source";
                DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);

            }

            return ErrorObject;
        }
        public virtual IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            // DataRow tb = object UploadDataRow;
            ErrorObject.Flag = Errors.Ok;
            EntityStructure DataStruct = GetEntityStructure(EntityName, true);
            DataRowView dv;
            DataTable tb;
            DataRow dr;
            string msg = "";
            //   var sqlTran = Dataconnection.DbConn.BeginTransaction();
            IDbCommand command = RDBMSConnection.DbConn.CreateCommand();
            Type enttype = GetEntityType(EntityName);
            var ti = Activator.CreateInstance(enttype);
            // ICustomTypeDescriptor, IEditableObject, IDataErrorInfo, INotifyPropertyChanged
            if (InsertedData.GetType().FullName == "System.Data.DataRowView")
            {
                dv = (DataRowView)InsertedData;
                dr = dv.Row;
                //foreach (EntityField col in DataStruct.Fields)
                //{
                //    // TrySetProperty<enttype>(ti, dr[col.fieldname], null);
                //    //if (dr[col.fieldname] != System.DBNull.Value)
                //    //{
                //        System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(col.fieldname);
                //        PropAInfo.SetValue(ti, dr[col.fieldname], null);
                //    //}

                //}

            }
            else
               if (InsertedData.GetType().FullName == "System.Data.DataRow")
            {
                dr = (DataRow)InsertedData;
            }
            else
            {
                dr = DMEEditor.Utilfunction.ConvertItemClassToDataRow(DataStruct);
                foreach (EntityField col in DataStruct.Fields)
                {
                    System.Reflection.PropertyInfo GetPropAInfo = InsertedData.GetType().GetProperty(col.fieldname);

                    //if (GetPropAInfo.GetValue(UploadDataRow) != System.DBNull.Value)
                    //{
                    System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(col.fieldname);
                    dynamic result = GetPropAInfo.GetValue(InsertedData);

                    if (result == null)
                    {
                        result = System.DBNull.Value;
                    }


                    dr[col.fieldname] = result;
                }
            }
            try
            {
                string updatestring = GetInsertString(EntityName, DataStruct);


                command.CommandText = updatestring;
                foreach (EntityField item in DataStruct.Fields)
                {
                    IDbDataParameter parameter = command.CreateParameter();
                    //System.Reflection.PropertyInfo PropAInfo = enttype.GetProperty(item.fieldname);
                    //var v = PropAInfo.GetValue(ti);

                    parameter.ParameterName = "p_" + item.fieldname;
                    if (item.fieldtype == "System.DateTime")
                    {
                        parameter.DbType = DbType.DateTime;
                        parameter.Value = DateTime.Parse(dr[item.fieldname].ToString());


                    }
                    else
                    { parameter.Value = dr[item.fieldname]; }
                    //  parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                    command.Parameters.Add(parameter);

                }

                int rowsUpdated = command.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    msg = $"Successfully Inserted  Record  to {EntityName} : {updatestring}";
                    DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);
                }
                else
                {
                    msg = $"Fail to Insert  Record  to {EntityName} : {updatestring}";
                    DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
                }
                // DMEEditor.AddLogMessage("Success",$"Successfully Written Data to {EntityName}",DateTime.Now,0,null, Errors.Ok);

            }
            catch (Exception ex)
            {
                ErrorObject.Ex = ex;
              
                command.Dispose();
                try
                {
                    // Attempt to roll back the transaction.
                    //     sqlTran.Rollback();
                    msg = "Unsuccessfully no Data has been written to Data Source,Rollback Complete";
                }
                catch (Exception exRollback)
                {
                    // Throws an InvalidOperationException if the connection
                    // is closed or the transaction has already been rolled
                    // back on the server.
                    // Console.WriteLine(exRollback.Message);
                    msg = "Unsuccessfully no Data has been written to Data Source,Rollback InComplete";
                    ErrorObject.Ex = exRollback;
                }
                msg = "Unsuccessfully no Data has been written to Data Source";
                DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);

            }

            return ErrorObject;
        }
        private string BuildQuery(string originalquery, List<ReportFilter> Filter)
        {
            string retval;
            string[] stringSeparators;
            string[] sp;
            string qrystr="Select ";
            bool FoundWhere = false;
            QueryBuild queryStructure = new QueryBuild();
            try
            {
                //stringSeparators = new string[] {"select ", " from ", " where ", " group by "," having ", " order by " };
                // Get Selected Fields
                stringSeparators = new string[] { "select ", " from " , " where ", " group by ", " having ", " order by " };
                sp = originalquery.ToLower().Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                queryStructure.FieldsString = sp[0];
                string[] Fieldsp = sp[0].Split(',');
                queryStructure.Fields.AddRange(Fieldsp);
                // Get From  Tables
                queryStructure.EntitiesString = sp[1];
                string[] Tablesdsp = sp[1].Split(',');
                queryStructure.Entities.AddRange(Tablesdsp);
                qrystr += queryStructure.FieldsString + " " + " from " + queryStructure.EntitiesString;
                qrystr += Environment.NewLine;
                if (Filter != null)
                {
                    if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                    {
                        qrystr += Environment.NewLine;
                        if (FoundWhere == false)
                        {
                            qrystr += " where " + Environment.NewLine;
                            FoundWhere = true;
                        }

                        foreach (ReportFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)))
                        {
                            if (!string.IsNullOrEmpty(item.FilterValue) && !string.IsNullOrWhiteSpace(item.FilterValue))
                            {
                                //  EntityField f = ent.Fields.Where(i => i.fieldname == item.FieldName).FirstOrDefault();
                                if (item.Operator.ToLower() == "between")
                                {
                                    qrystr += item.FieldName + " " + item.Operator + " @p_" + item.FieldName + " and  @p_" + item.FieldName + "1 " + Environment.NewLine;
                                }
                                else
                                {
                                    qrystr += item.FieldName + " " + item.Operator + " @p_" + item.FieldName + " " + Environment.NewLine;
                                }

                            }



                        }
                    }

                }
                if (originalquery.ToLower().Contains("where"))
                {
                    qrystr += Environment.NewLine;

                    string[] whereSeparators = new string[] { " where " };

                    string[] spwhere = originalquery.ToLower().Split(whereSeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.WhereCondition = spwhere[0];
                    if (FoundWhere == false)
                    {
                        qrystr += " where " + Environment.NewLine;
                        FoundWhere = true;
                    }
                    qrystr +=  spwhere[0];
                    qrystr += Environment.NewLine;
                 
                   

                }
                if (originalquery.ToLower().Contains("group by"))
                {
                    string[] groupbySeparators = new string[] { " group by " };

                    string[] groupbywhere = originalquery.ToLower().Split(groupbySeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.GroupbyCondition = groupbywhere[1];
                    qrystr += " group by " + groupbywhere[1];
                    qrystr += Environment.NewLine;
                }
                if (originalquery.ToLower().Contains("having"))
                {
                    string[] havingSeparators = new string[] { " having " };

                    string[] havingywhere = originalquery.ToLower().Split(havingSeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.HavingCondition = havingywhere[1];
                    qrystr += " having " + havingywhere[1];
                    qrystr += Environment.NewLine;
                }
                if (originalquery.ToLower().Contains("order by"))
                {
                    string[] orderbySeparators = new string[] { " order by " };

                    string[] orderbywhere = originalquery.ToLower().Split(orderbySeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.OrderbyCondition = orderbywhere[1];
                    qrystr += " order by " + orderbywhere[1];

                }


            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Unable Build Query Object {originalquery}", DateTime.Now, 0, "Error", Errors.Failed);
            }
          


            return qrystr;
        }
        public virtual object GetEntity(string EntityName, List<ReportFilter> Filter)
        {
            ErrorObject.Flag = Errors.Ok;
            //  int LoadedRecord;
           
            EntityName = EntityName.ToLower();
            string inname="";
            string qrystr = "select* from ";
            
            if (!string.IsNullOrEmpty(EntityName) && !string.IsNullOrWhiteSpace(EntityName))
            {
                if (!EntityName.Contains("select") && !EntityName.Contains("from"))
                {
                    qrystr = "select * from " + EntityName;
                    inname = EntityName;
                }else
                {
                   
                    string[] stringSeparators = new string[] { " from ", " where ", " group by "," order by " };
                    string[] sp = EntityName.Split(stringSeparators, StringSplitOptions.None);
                    qrystr = EntityName;
                    inname = sp[1].Trim();
                }
               
            }
            // EntityStructure ent = GetEntityStructure(inname);
            qrystr= BuildQuery(qrystr, Filter);
          
            try
            {
                IDataAdapter adp = GetDataAdapter(qrystr,Filter);
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
        public Task<object> GetEntityAsync(string EntityName, List<ReportFilter> Filter)
        {
            return (Task<object>)GetEntity(EntityName, Filter);
        }
        #endregion

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
                retval.DatasourceEntityName = EntityName;
                retval.Caption = EntityName;
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
            string entname = fnd.EntityName;
            if (string.IsNullOrEmpty(fnd.DatasourceEntityName))
            {
                fnd.DatasourceEntityName = fnd.EntityName;
            }
            if (fnd.Created == false)
            {
                fnd.Created = false;
                fnd.Drawn = false;
                fnd.Editable = true;
                return fnd;

            }
            else
            {
                if (refresh)
                {
                    if (!fnd.EntityName.Equals(fnd.DatasourceEntityName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(fnd.DatasourceEntityName))
                    {
                        entname = fnd.DatasourceEntityName;
                    }
                    if (string.IsNullOrEmpty(fnd.DatasourceEntityName))
                    {
                        fnd.DatasourceEntityName=entname;
                    }
                    if (string.IsNullOrEmpty(fnd.Caption))
                    {
                        fnd.Caption = entname;
                    }
                    fnd.DataSourceID = DatasourceName;
                    //  fnd.EntityName = EntityName;
                    if (fnd.Viewtype == ViewType.Query)
                    {

                        tb = GetTableSchema(fnd.CustomBuildQuery);
                    }
                    else
                    {
                       
                            tb = GetTableSchema(entname);
                        
                       
                    }

                    if (tb.Rows.Count > 0)
                    {
                        fnd.Fields = new List<EntityField>();
                        fnd.PrimaryKeys = new List<EntityField>();
                        DataRow rt = tb.Rows[0];
                        fnd.Created = true;
                        fnd.Editable = false;
                        fnd.Drawn = true;
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
                                  if (x.fieldtype == "System.Decimal" || x.fieldtype=="System.Float" || x.fieldtype == "System.Double") 
                                    {
                                        var NumericPrecision = r["NumericPrecision"];
                                        var NumericScale = r["NumericScale"];
                                        if (NumericPrecision != System.DBNull.Value && NumericScale != System.DBNull.Value)
                                        {
                                            x.NumericPrecision = (short)NumericPrecision;
                                            x.NumericScale = (short)NumericScale;
                                        }
                                    }

                                    
                                 
                                 

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
                                fnd.Relations = GetEntityforeignkeys(entname, Dataconnection.ConnectionProp.SchemaName);
                            }
                        }

                        EntityStructure exist = Entities.Where(d => d.EntityName.Equals(fnd.EntityName,StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (exist == null)
                        {
                            Entities.Add(fnd);
                        }
                        else
                        {
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.OrdinalIgnoreCase))].Created = true;
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.OrdinalIgnoreCase))].Editable = false;
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.OrdinalIgnoreCase))].Drawn = true;
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.OrdinalIgnoreCase))].Fields = fnd.Fields;
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.OrdinalIgnoreCase))].Relations = fnd.Relations;
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.OrdinalIgnoreCase))].PrimaryKeys = fnd.PrimaryKeys;
    
                        }
                    }
                  
                }
              
            }
           

         //  DMEEditor.classCreator.CreateClass(fnd.EntityName, fnd.Fields, DMEEditor.ConfigEditor.Config.EntitiesPath, "TheTechIdea");
            return fnd;
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

              //  if (EntitiesNames.Count() == 0)
             //   {
                    string sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getlistoftables, null, Dataconnection.ConnectionProp.SchemaName, null, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                    IDbDataAdapter adp = GetDataAdapter(sql, null);
                    adp.Fill(ds);

                    DataTable tb = new DataTable();
                    tb = ds.Tables[0];
                    EntitiesNames = new List<string>();
                    int i = 0;
                    foreach (DataRow row in tb.Rows)
                    {
                        EntitiesNames.Add(row.Field<string>("TABLE_NAME").ToUpper());
                       
                       
                    i += 1;
                    }
                    if (Entities.Count > 0)
                    {
                        List<string> ename = Entities.Select(p => p.EntityName.ToUpper()).ToList();
                        List<string> diffnames = ename.Except(EntitiesNames).ToList();
                        if (diffnames.Count > 0)
                        {
                            foreach (string item in diffnames)
                            {
                                EntitiesNames.Add(item.ToUpper());
                            }
                        }
                    }

                    //Logger.WriteLog("Successfully Retrieve tables list ");


                //} else
                //{
                //    return EntitiesNames;
                //}


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
            string entspace = Regex.Replace(EntityName, @"\s+", "_");
            retval = EntitiesNames.ConvertAll(d => d.ToUpper()).Contains(entspace.ToUpper());
          

            return retval;
        }
        public bool CreateEntityAs(EntityStructure entity)
        {
            bool retval = false;

            if (CheckEntityExist(entity.EntityName) == false)
            {

                string createstring=CreateEntity(entity);
                DMEEditor.ErrorObject=ExecuteSql(createstring);
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

                //ErrorObject.Flag = Errors.Failed;
                //ErrorObject.Ex = ex;
                //Logger.WriteLog($"Error in Loading Goreign Key for Table View ({ex.Message}) ");

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
                if (!string.IsNullOrEmpty(sql) && !string.IsNullOrWhiteSpace(sql))
                {
                    return GetData<ChildRelation>(sql);
                }
                else
                    return null;
                 
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
        private string GenerateCreateEntityScript(EntityStructure t1)
        {
            string createtablestring = "Create table ";
            try
            {//-- Create Create string
                int i = 1;
                t1.EntityName = Regex.Replace(t1.EntityName, @"\s+", "_");
                createtablestring += "["+t1.EntityName + "]\n(";
                if (t1.Fields.Count == 0)
                {
                    // t1=ds.GetEntityStructure()
                }
                foreach (EntityField dbf in t1.Fields)
                {

                    createtablestring += "\n " + dbf.fieldname + " " + DMEEditor.typesHelper.GetDataType(DatasourceName, dbf) + " ";
                    if (dbf.IsAutoIncrement)
                    {
                        dbf.fieldname = Regex.Replace(dbf.fieldname, @"\s+", "_");
                        string autonumberstring = "";
                        autonumberstring = CreateAutoNumber(dbf);
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
                createtablestring = "";
                Logger.WriteLog($"Error in  Creating Table " + t1.EntityName + "   ({ex.Message})");


            }
            return createtablestring;
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


            //    DMEEditor.AddLogMessage("Success", $"Generated Script", DateTime.Now, 0, null, Errors.Ok);

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

           //     DMEEditor.AddLogMessage("Success", $"Generated Script", DateTime.Now, 0, null, Errors.Ok);

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
                    retval += @" ALTER TABLE [" + t1.EntityName + "] ADD CONSTRAINT " + t1.EntityName + i + r.Next(10, 1000) + "  FOREIGN KEY (" + forkeys + ")  REFERENCES " + item + "(" + refkeys + "); \n";
                }


                if (i ==0)
                
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
                        string relations=CreateAlterRalationString(entity);
                        string[] rels = relations.Split(';');
                        foreach (string rl in rels)
                        {
                            LScript x = new LScript();
                            x.destinationdatasourcename = DatasourceName;
                            ds = DMEEditor.GetDataSource(entity.DataSourceID);
                            x.ddl = rl;
                            x.entityname = entity.EntityName;
                            x.scriptType = DDLScriptType.AlterFor;
                            rt.Add(x);
                        }
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
        public virtual string CreateAutoNumber(EntityField f)
        {
            ErrorObject.Flag = Errors.Ok;
            string AutnumberString = "";
            
            try
            {
                if (f.IsAutoIncrement)
                {
                    switch (Dataconnection.ConnectionProp.DatabaseType)
                    {
                        //case DataSourceType.Excel:
                        //    break;
                        case DataSourceType.Mysql:
                            AutnumberString = "NULL AUTO_INCREMENT";
                            break;
                        case DataSourceType.Oracle:
                            //select substr(banner,instr(banner,'Release')+7,instr(banner,'-')-instr(banner,'.')) from v$version
                            //where instr(banner,'Oracle')> 0

                            AutnumberString = " GENERATED BY DEFAULT ON NULL AS IDENTITY";// "CREATE SEQUENCE " + f.fieldname + "_seq MINVALUE 1 START WITH 1 INCREMENT BY 1 CACHE 1 ";
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

            return AutnumberString;
        }
        private string CreateEntity(EntityStructure t1)
        {
            string createtablestring = null;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            
            Entities.Add(t1);
            try
            {
               
                createtablestring= GenerateCreateEntityScript(t1);

            }
            catch (System.Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                createtablestring = null;
                Logger.WriteLog($"Error in  Creating Table " + t1.EntityName + "   ({ex.Message})");


            }
            return createtablestring;
        }
        public virtual string GetInsertString(string EntityName, EntityStructure DataStruct)
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
        public virtual string GetUpdateString(string EntityName, EntityStructure DataStruct)
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


                t += 1;
            }

            Updatestr = Updatestr.Remove(Updatestr.Length - 1);

            Updatestr += @" where " + Environment.NewLine;
            i = DataStruct.PrimaryKeys.Count();
            t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys)
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
                


                t += 1;
            }
            //  Updatestr = Updatestr.Remove(Valuestr.Length - 1);
            return Updatestr;
        }
        public virtual string GetDeleteString(string EntityName,  EntityStructure DataStruct)
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
                



                t += 1;
            }
            //   Updatestr= Updatestr.Remove(Updatestr.Length - 1);
            return Updatestr;
        }
        public virtual IDataReader GetDataReader(string querystring)
        {
            IDbCommand cmd = GetDataCommand();
            cmd.CommandText = querystring;
            IDataReader dt = cmd.ExecuteReader();
            
            return dt;

        }
        #region "Dapper"
        public virtual List<T> GetData<T>(string sql)
        {
           // DMEEditor.OpenDataSource(ds.DatasourceName);
            if (Dataconnection.OpenConnection() == ConnectionState.Open)
            {
                return RDBMSConnection.DbConn.Query<T>(sql).AsList<T>();

            }
            else
                return null;



            
        }
        public virtual Task SaveData<T>(string sql, T parameters)
        {
            if (Dataconnection.OpenConnection() == ConnectionState.Open)
            {
                return RDBMSConnection.DbConn.ExecuteAsync(sql, parameters);
            }
            else
                return null;
               

        }
        #endregion
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
                if(Dataconnection.OpenConnection()== ConnectionState.Open)
                {
                    cmd = RDBMSConnection.DbConn.CreateCommand();
                }else
                {
                    cmd = null;

                    DMEEditor.AddLogMessage("Fail", $"Error in Creating Data Command, Cannot get DataSource", DateTime.Now, -1,DatasourceName, Errors.Failed);
                }
               
                //    Logger.WriteLog("Created Data Command");

            }
            catch (Exception ex)
            {

                cmd = null;

                DMEEditor.AddLogMessage("Fail", $"Error in Creating Data Command {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);

            }
            return cmd;
        }
        public virtual IDbDataAdapter GetDataAdapter(string Sql, List<ReportFilter> Filter = null)
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
                Type adcbuilderType = DMEEditor.assemblyHandler.GetType(cmdbuildername);
                List<ConstructorInfo> lsc = DMEEditor.assemblyHandler.GetInstance(adtype).GetType().GetConstructors().ToList(); ;
                List<ConstructorInfo> lsc2 = DMEEditor.assemblyHandler.GetInstance(cmdbuildername).GetType().GetConstructors().ToList(); ;

                ConstructorInfo ctor = lsc[GetCtorForAdapter(lsc)];
                ConstructorInfo BuilderConstructer = lsc2[GetCtorForCommandBuilder(adcbuilderType.GetConstructors().ToList())];
                ObjectActivator<IDbDataAdapter> adpActivator = GetActivator<IDbDataAdapter>(ctor);
                ObjectActivator<DbCommandBuilder> cmdbuilderActivator = GetActivator<DbCommandBuilder>(BuilderConstructer);
               
                //create an instance:
                adp = (IDbDataAdapter)adpActivator(Sql, RDBMSConnection.DbConn);


                try
                {
                    DbCommandBuilder cmdBuilder = cmdbuilderActivator(adp);
                    if (Filter != null)
                    {
                        if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {

                            foreach (ReportFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue)))
                            {
                               
                                IDbDataParameter parameter = adp.SelectCommand.CreateParameter();
                                string dr = Filter.Where(i => i.FieldName == item.FieldName).FirstOrDefault().FilterValue;
                                parameter.ParameterName = "p_" + item.FieldName;
                                if (item.valueType == "System.DateTime")
                                {
                                    parameter.DbType = DbType.DateTime;
                                    parameter.Value = DateTime.Parse(dr).ToShortDateString();
                                    
                                }
                                else
                                { parameter.Value = dr; }

                                if (item.Operator.ToLower() == "between")
                                {
                                    IDbDataParameter parameter1 = adp.SelectCommand.CreateParameter();
                                    parameter1.ParameterName = "p_" + item.FieldName + "1";
                                    parameter1.DbType = DbType.DateTime;
                                    string dr1 = Filter.Where(i => i.FieldName == item.FieldName).FirstOrDefault().FilterValue1;
                                    parameter1.Value = DateTime.Parse(dr1).ToShortDateString();
                                    adp.SelectCommand.Parameters.Add(parameter1);
                                }

                                //  parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                                adp.SelectCommand.Parameters.Add(parameter);

                            }

                        }
                    }
                   
                    
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
               // Logger.WriteLog("Executed Sql Successfully");

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
      
        public virtual List<ChildRelation> GetTablesFKColumnList(string tablename, string SchemaName, string Filterparamters)
        {
            ErrorObject.Flag = Errors.Ok;
            DataSet ds = new DataSet();
            try
            {
                string sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getFKforTable, tablename, SchemaName, Filterparamters, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                if (!string.IsNullOrEmpty(sql) && !string.IsNullOrWhiteSpace(sql))
                {
                    return GetData<ChildRelation>(sql);
                }
                else
                    return null;
              
              

            }
            catch (Exception ex)
            {
                
              //  DMEEditor.AddLogMessage("Fail", $"Unsuccessfully Retrieve Child tables list {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
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

