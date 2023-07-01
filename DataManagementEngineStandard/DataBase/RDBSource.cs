using System;
using System.Collections.Generic;
using System.Data;
using TheTechIdea.Util;
using TheTechIdea.Logger;
using TheTechIdea.Beep.Workflow;
using System.Threading.Tasks;

using System.Linq;
using Dapper;

using System.Reflection;
using System.Data.Common;


using static TheTechIdea.Beep.Util;
using System.Text.RegularExpressions;

using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.Report;
using System.Data.SqlTypes;

namespace TheTechIdea.Beep.DataBase
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
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = "@";
        protected static int recNumber = 0;
        protected string recEntity = "";
        public RDBSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.RDBMS;
            Dataconnection = new RDBDataConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,
            };
        }
        #region "IDataSource Interface Methods"
        public virtual ConnectionState Openconnection()
        {
           if (RDBMSConnection != null)
            {
                ConnectionStatus= RDBMSConnection.OpenConnection();
            }
            return ConnectionStatus;
        }
        public virtual ConnectionState Closeconnection()
        {
            if (RDBMSConnection != null)
            {
                ConnectionStatus = RDBMSConnection.CloseConn();
                Dataconnection.CloseConn();
            }
            if (Dataconnection != null)
            {
               
                Dataconnection.CloseConn();
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
                   
                    cmd.Dispose();
                   
                    DMEEditor.AddLogMessage("Fail", $" Could not run Script - {sql} -" + ex.Message, DateTime.Now, -1, ex.Message, Errors.Failed);

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
                if (dt != null)
                {
                    if (dt.Rows.Count == 1)
                    {
                        if (dt.Columns.Count == 1)
                            return dt.Rows[0][0];
                    }
                    else if (dt.Rows.Count > 1)
                    {
                        //EntityStructure st = DMEEditor.Utilfunction.GetEntityStructure(dt);
                        //Type type = DMEEditor.Utilfunction.GetEntityType("tab", st.Fields);
                        return dt; // DMEEditor.Utilfunction.ConvertTableToList(dt, st, type);
                    }
                }
                return null;

            }
            catch (Exception ex)
            {
                cmd.Dispose();
                DMEEditor.AddLogMessage("Fail", $"Error in getting entity Data({ ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                return null;
            }

        }
        private IDbCommand CreateCommandParameters(IDbCommand  command, DataRow r,EntityStructure DataStruct)
        {
            command.Parameters.Clear();
            
            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {

                if (!command.Parameters.Contains("p_" + Regex.Replace(item.fieldname, @"\s+", "_")))
                {
                    IDbDataParameter parameter = command.CreateParameter();
                    //if (!item.fieldtype.Equals("System.String", StringComparison.InvariantCultureIgnoreCase) && !item.fieldtype.Equals("System.DateTime", StringComparison.InvariantCultureIgnoreCase))
                    //{
                    //    if (r[item.fieldname] == DBNull.Value || r[item.fieldname].ToString() == "")
                    //    {
                    //        parameter.Value = Convert.ToDecimal(null);
                    //    }
                    //    else
                    //    {
                    //        parameter.Value = r[item.fieldname];
                    //    }
                    //}
                    //else
                        if (item.fieldtype.Equals("System.DateTime", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (r[item.fieldname] == DBNull.Value || r[item.fieldname].ToString() == "")
                        {

                            parameter.Value = DBNull.Value;
                            parameter.DbType = DbType.DateTime;
                        }
                        else
                        {
                            parameter.DbType = DbType.DateTime;
                            try
                            {
                                parameter.Value = DateTime.Parse(r[item.fieldname].ToString());
                            }
                            catch (FormatException formatex)
                            {

                                parameter.Value = SqlDateTime.Null;
                            }
                        }
                    }
                    else
                        parameter.Value = r[item.fieldname];
                    parameter.ParameterName = "p_" + Regex.Replace(item.fieldname, @"\s+", "_");
                    //   parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                    command.Parameters.Add(parameter);
                }

            }
            return command;
        }
        public virtual IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
            if (UploadData != null)
            {
                if (UploadData.GetType().ToString() != "System.Data.DataTable")
                {
                    DMEEditor.AddLogMessage("Fail", $"Please use DataTable for this Method {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                //  RunCopyDataBackWorker(EntityName,  UploadData,  Mapping );
                #region "Update Code"
                //IDbTransaction sqlTran;
                DataTable tb = (DataTable)UploadData;
                // DMEEditor.classCreator.CreateClass();
                //List<object> f = DMEEditor.Utilfunction.GetListByDataTable(tb);
                ErrorObject.Flag = Errors.Ok;
                EntityStructure DataStruct = GetEntityStructure(EntityName);
                IDbCommand command = RDBMSConnection.DbConn.CreateCommand();
                string str = "";
                string errorstring = "";
                int CurrentRecord = 0;
                DMEEditor.ETL.CurrentScriptRecord = 0;
                DMEEditor.ETL.ScriptCount += tb.Rows.Count;
                int highestPercentageReached = 0;
                int numberToCompute = DMEEditor.ETL.ScriptCount;
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
                                        updatestring = GetInsertString(EntityName, DataStruct);
                                        break;
                                    case DataRowState.Deleted:
                                        updatestring = GetDeleteString(EntityName, DataStruct);
                                        break;
                                    case DataRowState.Modified:
                                        updatestring = GetUpdateString(EntityName, DataStruct);
                                        break;
                                    default:
                                        updatestring = GetInsertString(EntityName, DataStruct);
                                        break;
                                }
                                command.CommandText = updatestring;
                                command = CreateCommandParameters(command, r, DataStruct);
                                errorstring = updatestring.Clone().ToString();
                                foreach (EntityField item in DataStruct.Fields)
                                {
                                    try
                                    {
                                        string s;
                                        string f;
                                        if (r[item.fieldname] == DBNull.Value)
                                        {
                                            s = "\' \'";
                                        }
                                        else
                                        {
                                            s = "\'" + r[item.fieldname].ToString() + "\'";
                                        }
                                        f = "@p_" + Regex.Replace(item.fieldname, @"\s+", "_");
                                        errorstring = errorstring.Replace(f, s);
                                    }
                                    catch (Exception ex1)
                                    {
                                    }
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
                                    if (DataStruct.PrimaryKeys.Count == 1)
                                    {
                                        args.ParameterString1 = r[DataStruct.PrimaryKeys[0].fieldname].ToString();
                                    }
                                    if (DataStruct.PrimaryKeys.Count == 2)
                                    {
                                        args.ParameterString2 = r[DataStruct.PrimaryKeys[1].fieldname].ToString();
                                    }
                                    if (DataStruct.PrimaryKeys.Count == 3)
                                    {
                                        args.ParameterString3 = r[DataStruct.PrimaryKeys[2].fieldname].ToString();
                                    }
                                }
                                args.ParameterInt1 = percentComplete;
                                //         UpdateEvents(EntityName, msg, highestPercentageReached, CurrentRecord, numberToCompute, this);
                                if (progress != null)
                                {
                                    PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentRecord, ParameterInt2 = DMEEditor.ETL.ScriptCount, ParameterString1 = null };
                                    progress.Report(ps);
                                }
                                //   PassEvent?.Invoke(this, args);
                                //   DMEEditor.RaiseEvent(this, args);
                            }
                            catch (Exception er)
                            {
                                string msg = $"Fail to I/U/D  Record {i} to {EntityName} : {updatestring}";
                                if (progress != null)
                                {
                                    PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentRecord, ParameterInt2 = DMEEditor.ETL.ScriptCount, ParameterString1 = msg };
                                    progress.Report(ps);
                                }
                                DMEEditor.AddLogMessage("Fail", msg, DateTime.Now, i, EntityName, Errors.Failed);
                            }
                        }
                        DMEEditor.ETL.CurrentScriptRecord = DMEEditor.ETL.ScriptCount;
                        command.Dispose();
                        DMEEditor.AddLogMessage("Success", $"Finished Uploading Data to {EntityName}", DateTime.Now, 0, null, Errors.Ok);
                    }


                }
                catch (Exception ex)
                {
                    ErrorObject.Ex = ex;
                    command.Dispose();


                }
                #endregion
            }
            return ErrorObject;
        }
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                RDBMSConnection.DbConn.BeginTransaction();
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
            SetObjects(EntityName);
            ErrorObject.Flag = Errors.Ok;
         
            DataRowView dv;
            DataTable tb;
            DataRow dr;
            string msg = "";
            dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, UploadDataRow, DataStruct);
            try
            {
                string updatestring = GetUpdateString(EntityName,  DataStruct);
                command.CommandText = updatestring;
                command = CreateCommandParameters(command,dr, DataStruct);
                int rowsUpdated = command.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    msg = $"Successfully Updated  Record  to {EntityName} : {updatestring}";
                   // DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
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
            SetObjects(EntityName);
            ErrorObject.Flag = Errors.Ok;
         
            string msg;
            DataRowView dv;
            DataTable tb;
            DataRow dr;
            var sqlTran = RDBMSConnection.DbConn.BeginTransaction();
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
           
            dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, DeletedDataRow, DataStruct);
            try
            {
                string updatestring = GetDeleteString(EntityName, DataStruct);
                command.Transaction = sqlTran;
                command.CommandText = updatestring;

                command = CreateCommandParameters(command, dr, DataStruct);
                int rowsUpdated = command.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    msg = $"Successfully Deleted  Record  to {EntityName} : {updatestring}";
                  //  DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
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
        #region "Insert or Update or Delete Objects"
        EntityStructure DataStruct = null; 
        IDbCommand command = null; 
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        #endregion
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname!=lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, true);
                command = RDBMSConnection.DbConn.CreateCommand();
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }
        public virtual IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            SetObjects(EntityName);
            ErrorObject.Flag = Errors.Ok;
            DataRow dr;
            string msg = "";
            string updatestring="";
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;

            dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, InsertedData, DataStruct);
            try
            {
                updatestring = GetInsertString(EntityName, DataStruct);


                command.CommandText = updatestring;
                command = CreateCommandParameters(command, dr, DataStruct);

                int rowsUpdated = command.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    msg = $"Successfully Inserted  Record  to {EntityName} ";
                    DMEEditor.ErrorObject.Message = msg;
                    DMEEditor.ErrorObject.Flag = Errors.Ok;
                    // DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    msg = $"Fail to Insert  Record  to {EntityName} : {updatestring}";
                    DMEEditor.ErrorObject.Message = msg;
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    

                  //  DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);
                }
                // DMEEditor.AddLogMessage("Success",$"Successfully Written Data to {EntityName}",DateTime.Now,0,null, Errors.Ok);

            }
            catch (Exception ex)
            {
                msg = $"Fail to Insert  Record  to {EntityName} : {ex.Message}";
                ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Message = msg;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                command.Dispose();
               
                DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, updatestring, Errors.Failed);

            }

            return ErrorObject;
        }
        private string BuildQuery(string originalquery, List<AppFilter> Filter)
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
                originalquery=GetTableName(originalquery.ToLower());  
                stringSeparators = new string[] { "select", "from" , "where", "group by", "having", "order by" };
                sp = originalquery.ToLower().Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                queryStructure.FieldsString = sp[0];
                string[] Fieldsp = sp[0].Split(',');
                queryStructure.Fields.AddRange(Fieldsp);
                // Get From  Tables
                queryStructure.EntitiesString = sp[1];
                string[] Tablesdsp = sp[1].Split(',');
                queryStructure.Entities.AddRange(Tablesdsp);

                if (GetSchemaName() == null)
                {
                    qrystr += queryStructure.FieldsString + " " + " from " + queryStructure.EntitiesString;
                }
                else
                    qrystr += queryStructure.FieldsString + $" from {GetSchemaName().ToLower()}." + queryStructure.EntitiesString;

                qrystr += Environment.NewLine;

                if (Filter != null)
                {
                    if (Filter.Count > 0)
                    {
                        if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {
                            qrystr += Environment.NewLine;
                            if (FoundWhere == false)
                            {
                                qrystr += " where " + Environment.NewLine;
                                FoundWhere = true;
                            }

                            foreach (AppFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)))
                            {
                                if (!string.IsNullOrEmpty(item.FilterValue) && !string.IsNullOrWhiteSpace(item.FilterValue))
                                {
                                    //  EntityField f = ent.Fields.Where(i => i.fieldname == item.FieldName).FirstOrDefault();
                                    if (item.Operator.ToLower() == "between")
                                    {
                                        qrystr += item.FieldName + " " + item.Operator + $" {ParameterDelimiter}p_" + item.FieldName + $" and  {ParameterDelimiter}p_" + item.FieldName + "1 " + Environment.NewLine;
                                    }
                                    else
                                    {
                                        qrystr += item.FieldName + " " + item.Operator + $" {ParameterDelimiter}p_" + item.FieldName + " " + Environment.NewLine;
                                    }

                                }



                            }
                        }
                    }
                 }
                if (originalquery.ToLower().Contains("where"))
                {
                    qrystr += Environment.NewLine;

                    string[] whereSeparators = new string[] { "where", "group by", "having", "order by" };

                    string[] spwhere = originalquery.ToLower().Split(whereSeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.WhereCondition = spwhere[0];
                    if (FoundWhere == false)
                    {
                        qrystr += " where " + Environment.NewLine;
                        FoundWhere = true;
                    }
                    qrystr += spwhere[1];
                    qrystr += Environment.NewLine;
                 
                   

                }
                if (originalquery.ToLower().Contains("group by"))
                {
                    string[] groupbySeparators = new string[] { "group by","having", "order by" };

                    string[] groupbywhere = originalquery.ToLower().Split(groupbySeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.GroupbyCondition = groupbywhere[1];
                    qrystr += " group by " + groupbywhere[1];
                    qrystr += Environment.NewLine;
                }
                if (originalquery.ToLower().Contains("having"))
                {
                    string[] havingSeparators = new string[] { "having", "order by" };

                    string[] havingywhere = originalquery.ToLower().Split(havingSeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.HavingCondition = havingywhere[1];
                    qrystr += " having " + havingywhere[1];
                    qrystr += Environment.NewLine;
                }
                if (originalquery.ToLower().Contains("order by"))
                {
                    string[] orderbySeparators = new string[] { "order by" };

                    string[] orderbywhere = originalquery.ToLower().Split(orderbySeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.OrderbyCondition = orderbywhere[1];
                    qrystr += " order by " + orderbywhere[1];

                }

            }
            catch (Exception ex )
            {
                DMEEditor.AddLogMessage("Fail", $"Unable Build Query Object {originalquery}- {ex.Message}", DateTime.Now, 0, "Error", Errors.Failed);
            }
            return qrystr;
        }
        public virtual object GetEntity(string EntityName, List<AppFilter> Filter)
        {
            ErrorObject.Flag = Errors.Ok;
            //  int LoadedRecord;
           
            EntityName = EntityName.ToLower();
            string inname="";
            string qrystr = "select * from ";
            
            if (!string.IsNullOrEmpty(EntityName) && !string.IsNullOrWhiteSpace(EntityName))
            {
                if (!EntityName.Contains("select") && !EntityName.Contains("from"))
                {
                    qrystr = "select * from " + EntityName;
                    qrystr = GetTableName(qrystr.ToLower());
                    inname = EntityName;
                }else
                {
                    EntityName = GetTableName(EntityName);
                    string[] stringSeparators = new string[] { " from ", " where ", " group by "," order by " };
                    string[] sp = EntityName.Split(stringSeparators, StringSplitOptions.None);
                    qrystr = EntityName;
                    inname = sp[1].Trim();
                }
               
            }
            EntityStructure ent = GetEntityStructure(inname);
            if(ent != null)
            {
                if (!string.IsNullOrEmpty(ent.CustomBuildQuery))
                {
                    qrystr = ent.CustomBuildQuery;
                }

            }
           
            qrystr= BuildQuery(qrystr, Filter);
          
            try
            {
                IDataAdapter adp = GetDataAdapter(qrystr,Filter);
                DataSet dataSet = new DataSet();
                adp.Fill(dataSet);
                DataTable dt = dataSet.Tables[0];

                return  DMEEditor.Utilfunction.ConvertTableToList(dt,GetEntityStructure(EntityName),GetEntityType(EntityName));
            }

            catch (Exception ex)
            {
                
                DMEEditor.AddLogMessage("Fail", $"Error in getting entity Data({ ex.Message})", DateTime.Now, 0, "", Errors.Failed);
             
                return null;
            }


        }
        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return (Task<object>)GetEntity(EntityName, Filter);
        }
        #endregion
        public virtual EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            EntityStructure retval = new EntityStructure();
            if (Entities.Count == 0)
            {
                GetEntitesList();
            }
            EntityStructure fnd = Entities.Where(d => d.EntityName.Equals(EntityName,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (fnd == null)
            {
                List<EntityStructure> ls = Entities.Where(d => !string.IsNullOrEmpty(d.OriginalEntityName)).ToList();
                fnd = ls.Where(d => d.OriginalEntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            }
            
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
            DataTable tb = new DataTable();
            string entname = fnd.EntityName;
            if (string.IsNullOrEmpty(fnd.DatasourceEntityName))
            {
                fnd.DatasourceEntityName = fnd.EntityName;
            }
            if (fnd.Created == false && fnd.Viewtype!= ViewType.Table)
            {
                fnd.Created = false;
                fnd.Drawn = false;
                fnd.Editable = true;
                return fnd;

            }
            if (refresh)
                {
                    if (!fnd.EntityName.Equals(fnd.DatasourceEntityName, StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty(fnd.DatasourceEntityName))
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
                                DMEEditor.AddLogMessage("Fail", $"Error in Creating Field Type({ ex.Message})", DateTime.Now, 0, entname, Errors.Failed);
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
                                fnd.Relations = new List<RelationShipKeys>();
                                fnd.Relations = GetEntityforeignkeys(entname, Dataconnection.ConnectionProp.SchemaName);
                            }
                        }

                        EntityStructure exist = Entities.Where(d => d.EntityName.Equals(fnd.EntityName,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (exist == null)
                        {
                            Entities.Add(fnd);
                        }
                        else
                        {
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.InvariantCultureIgnoreCase))].Created = true;
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.InvariantCultureIgnoreCase))].Editable = false;
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.InvariantCultureIgnoreCase))].Drawn = true;
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.InvariantCultureIgnoreCase))].Fields = fnd.Fields;
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.InvariantCultureIgnoreCase))].Relations = fnd.Relations;
                            Entities[Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.InvariantCultureIgnoreCase))].PrimaryKeys = fnd.PrimaryKeys;
    
                        }
                    }
                    else
                    {
                        fnd.Created = false;
                    }
                
            }
          return fnd;
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
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting  Table List ({ ex.Message})", DateTime.Now, 0, DatasourceName, Errors.Failed);
              
            }

            return EntitiesNames;



        }
        public string GetSchemaName()
        {
            string schemaname=null;
            
            if(!string.IsNullOrEmpty(Dataconnection.ConnectionProp.SchemaName))
            {
                schemaname = Dataconnection.ConnectionProp.SchemaName.ToUpper();
            }
            if (Dataconnection.ConnectionProp.DatabaseType == DataSourceType.SqlServer && string.IsNullOrEmpty(Dataconnection.ConnectionProp.SchemaName))
            {
                schemaname = "dbo";
            }
            return schemaname;
        }
        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;
            GetEntitesList();
            if (EntitiesNames.Count == 0)
            {
                retval = false;
            }
            if (Entities.Count > 0) {
                retval = Entities.Any(p=>p.EntityName == EntityName || p.OriginalEntityName==EntityName || p.DatasourceEntityName==EntityName);
            }
           
            return retval;
        }
        public int GetEntityIdx(string entityName)
        {
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase));
            }else
            {
                return -1;
            }
        }
        public virtual bool CreateEntityAs(EntityStructure entity)
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
                    Entities.Add(entity);
                    retval = true;
                }
            } else
            {
                if (Entities.Count > 0)
                {
                    if (Entities.Where(p => p.EntityName.Equals(entity.EntityName, StringComparison.InvariantCultureIgnoreCase) && p.Created == false).Any())
                    {
                        string createstring = CreateEntity(entity);
                        DMEEditor.ErrorObject = ExecuteSql(createstring);
                        if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                        {
                            retval = false;
                        }
                        else
                        {
                            Entities.Add(entity);
                            retval = true;
                        }
                    }
                }
                else
                    return false;
            }

            return retval;
        }
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            List<RelationShipKeys> fk = new List<RelationShipKeys>();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                List<ChildRelation> ds = GetTablesFKColumnList(entityname, GetSchemaName(), null);
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
                                RelatedEntityID = r.parent_table,
                                RelatedEntityColumnID = r.parent_column,
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
               DMEEditor.AddLogMessage("Fail", $"Could not get forgien key  for {entityname} ({ ex.Message})", DateTime.Now, 0, entityname, Errors.Failed);
            }
            return fk;
        }
        public virtual List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                string sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getChildTable, tablename, SchemaName, Filterparamters, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                if (!string.IsNullOrEmpty(sql) && !string.IsNullOrWhiteSpace(sql))
                {
                    return GetData<ChildRelation>(sql);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
             DMEEditor.AddLogMessage("Fail", $"Error in getting  child entities for {tablename} ({ ex.Message})", DateTime.Now, 0, tablename, Errors.Failed);
                return null;
            }
        }
        public IErrorsInfo RunScript(ETLScriptDet scripts)
        {
            var t = Task.Run<IErrorsInfo>(() => { return ExecuteSql(scripts.ddl); });
            t.Wait();
            scripts.errorsInfo = t.Result;
            scripts.errormessage = DMEEditor.ErrorObject.Message;
            DMEEditor.ErrorObject = scripts.errorsInfo;
            return DMEEditor.ErrorObject;
        }
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities)
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
                createtablestring += " " +t1.EntityName + "\n(";
                if (t1.Fields.Count == 0)
                {
                    // t1=ds.GetEntityStructure()
                }
                foreach (EntityField dbf in t1.Fields)
                {

                    createtablestring += "\n " + dbf.fieldname + " " + DMEEditor.typesHelper.GetDataType(DatasourceName, dbf) + " ";
                    if (dbf.IsAutoIncrement)
                    {
                      //  dbf.fieldname = Regex.Replace(dbf.fieldname, @"\s+", "_");
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
                DMEEditor.AddLogMessage("Fail", $"Error Creating Entity {t1.EntityName}  ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                createtablestring = "";
            }
            return createtablestring;
        }
        public List<ETLScriptDet> GenerateCreatEntityScript(List<EntityStructure> entities)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            int i = 0;
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                // Generate Create Table First
                foreach (EntityStructure item in entities)
                {
                    ETLScriptDet x = new ETLScriptDet();
                    x.destinationdatasourcename = DatasourceName;

                    x.ddl = CreateEntity(item);
                    x.sourceentityname = item.EntityName;
                    x.sourceDatasourceEntityName = item.DatasourceEntityName;
                    x.scriptType = DDLScriptType.CreateEntity;
                    rt.Add(x);
                    rt.AddRange(CreateForKeyRelationScripts(item));
                    i += 1;
                }
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

        }
        public List<ETLScriptDet> GenerateCreatEntityScript(EntityStructure entity)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            int i = 0;

            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                // Generate Create Table First

                ETLScriptDet x = new ETLScriptDet();
                x.destinationdatasourcename = DatasourceName;
                x.ddl = CreateEntity(entity);
                x.sourceDatasourceEntityName = entity.DatasourceEntityName;
                x.sourceentityname = entity.EntityName;
                x.scriptType = DDLScriptType.CreateEntity;
                rt.Add(x);
                rt.AddRange(CreateForKeyRelationScripts(entity));
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

        }
        private List<ETLScriptDet> GetDDLScriptfromDatabase(string entity)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();

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
                var t2 = Task.Run<List<ETLScriptDet>>(() => { return GenerateCreatEntityScript(entstructure); });
                t2.Wait();
                rt.AddRange(t2.Result);
                t2 = Task.Run<List<ETLScriptDet>>(() => { return CreateForKeyRelationScripts(entstructure); });
                t2.Wait();
                rt.AddRange(t2.Result);
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return rt;
        }
        private List<ETLScriptDet> GetDDLScriptfromDatabase(List<EntityStructure> structureentities)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                if (structureentities.Count > 0)
                {
                    var t = Task.Run<List<ETLScriptDet>>(() => { return GenerateCreatEntityScript(structureentities); });
                    t.Wait();
                    rt.AddRange(t.Result);
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
                foreach (string item in t1.Relations.Select(o => o.RelatedEntityID).Distinct())
                {
                    string forkeys = "";
                    string refkeys = "";
                    foreach (RelationShipKeys fk in t1.Relations.Where(p => p.RelatedEntityID == item))
                    {
                        forkeys += fk.EntityColumnID + ",";
                        refkeys += fk.RelatedEntityColumnID + ",";
                    }
                    i += 1;
                    forkeys = forkeys.Remove(forkeys.Length - 1, 1);
                    refkeys = refkeys.Remove(refkeys.Length - 1, 1);
                    retval += @" ALTER TABLE " + t1.EntityName + " ADD CONSTRAINT " + t1.EntityName + i + r.Next(10, 1000) + "  FOREIGN KEY (" + forkeys + ")  REFERENCES " + item + "(" + refkeys + "); \n";
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
        private List<ETLScriptDet> CreateForKeyRelationScripts(EntityStructure entity)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
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
                            ETLScriptDet x = new ETLScriptDet();
                            x.destinationdatasourcename = DatasourceName;
                            ds = DMEEditor.GetDataSource(entity.DataSourceID);
                            x.sourceDatasourceEntityName = entity.DatasourceEntityName;
                            x.ddl = rl;
                            x.sourceentityname = entity.EntityName;
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
        private List<ETLScriptDet> CreateForKeyRelationScripts(List<EntityStructure> entities)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();

            try
            {
                int i = 0;
                IDataSource ds;
                // Generate Forign Keys
                foreach (EntityStructure item in entities)
                {
                    if (item.Relations != null)
                    {
                        if (item.Relations.Count > 0)
                        {
                            ETLScriptDet x = new ETLScriptDet();
                            x.destinationdatasourcename = item.DataSourceID;
                            ds = DMEEditor.GetDataSource(item.DataSourceID);
                            x.sourceDatasourceEntityName = item.DatasourceEntityName;
                            x.ddl = CreateAlterRalationString(item);
                            x.sourceentityname = item.EntityName;
                            x.scriptType = DDLScriptType.AlterFor;
                            rt.Add(x);
                            //alteraddForignKey.Add(x);
                            i += 1;
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
                        default:
                            AutnumberString = "";
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Creating Auto number Field {f.EntityName} and {f.fieldname} ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
              
            }
            return AutnumberString;
        }
        private string CreateEntity(EntityStructure t1)
        {
            string createtablestring = null;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                createtablestring= GenerateCreateEntityScript(t1);
            }
            catch (System.Exception ex)
            {
                createtablestring = null;
                DMEEditor.AddLogMessage("Fail", $"Error in  Creating Table " + t1.EntityName + "   ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
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
            Insertstr = GetTableName(Insertstr.ToLower());
            string Valuestr = ") values (";
            var insertfieldname = "";
            // string datafieldname = "";
            string typefield = "";
            int i = DataStruct.Fields.Count();
            int t = 0;
            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {
               Insertstr += $"{GetFieldName(item.fieldname)},";
               Valuestr += $"{ParameterDelimiter}p_" + Regex.Replace(item.fieldname, @"\s+", "_") + ",";
                 
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
          
            string Updatestr = @"Update " + EntityName + "  set " + Environment.NewLine;
            Updatestr= GetTableName(Updatestr.ToLower());
            string Valuestr = "";
           
            int i = DataStruct.Fields.Count();
            int t = 0;
            foreach (EntityField item in DataStruct.Fields.OrderBy(o=>o.fieldname))
            {
                if (!DataStruct.PrimaryKeys.Any(l => l.fieldname == item.fieldname))
                {
                    Updatestr += $"{GetFieldName(item.fieldname)}= ";
                    Updatestr += $"{ParameterDelimiter}p_" + item.fieldname + ",";
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
                        Updatestr += $"{GetFieldName(item.fieldname)}= ";
                }
                    else
                    {
                        Updatestr += $" and {GetFieldName(item.fieldname)}= ";
                }
                    Updatestr += $"{ParameterDelimiter}p_" + item.fieldname + "";
                  
                t += 1;
            }
            //  Updatestr = Updatestr.Remove(Valuestr.Length - 1);
            return Updatestr;
        }
        public virtual string GetDeleteString(string EntityName,  EntityStructure DataStruct)
        {

            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();
            string Updatestr = @"Delete from " + EntityName + "  ";
            Updatestr = GetTableName(Updatestr.ToLower());
            int i = DataStruct.Fields.Count();
            int t = 0;
            Updatestr += @" where ";
            i = DataStruct.PrimaryKeys.Count();
            t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys)
            {
                
                    if (t == 1)
                    {
                        Updatestr += $"{GetFieldName(item.fieldname)}= ";
                }
                    else
                    {
                        Updatestr += $" and  {GetFieldName(item.fieldname)}= ";
                    }
                    Updatestr += $"{ParameterDelimiter}p_" + item.fieldname + "";
                t += 1;
            }
            return Updatestr;
        }
        public virtual IDataReader GetDataReader(string querystring)
        {
            IDbCommand cmd = GetDataCommand();
            cmd.CommandText = querystring;
            IDataReader dt = cmd.ExecuteReader();
            
            return dt;

        }
        public virtual string GetFieldName(string fieldname)
        {
            string retval = fieldname;
            if (fieldname.IndexOf(" ") != -1)
            {
                if (ColumnDelimiter.Length==2) //(ColumnDelimiter.Contains("[") || ColumnDelimiter.Contains("]"))
                {
                    
                    retval = $"{ColumnDelimiter[0]}{fieldname}{ColumnDelimiter[1]}";
                }
                else
                {
                    retval = $"{ColumnDelimiter}{fieldname}{ColumnDelimiter}";
                }
              
            }
            return retval;
        }
        #region "Dapper"
        public virtual List<T> GetData<T>(string sql)
        {
           // DMEEditor.OpenDataSource(ds.DatasourceName);
            if (Dataconnection.ConnectionStatus == ConnectionState.Open)
            {
                return RDBMSConnection.DbConn.Query<T>(sql).AsList<T>();

            }
            else
                return null;



            
        }
        public virtual Task SaveData<T>(string sql, T parameters)
        {
            if (Dataconnection.ConnectionStatus == ConnectionState.Open)
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
                if(Dataconnection.ConnectionStatus== ConnectionState.Open)
                {
                    cmd = RDBMSConnection.DbConn.CreateCommand();
                }else
                {
                    cmd = null;

                    DMEEditor.AddLogMessage("Fail", $"Error in Creating Data Command, Cannot get DataSource", DateTime.Now, -1,DatasourceName, Errors.Failed);
                }
               
              

            }
            catch (Exception ex)
            {

                cmd = null;

                DMEEditor.AddLogMessage("Fail", $"Error in Creating Data Command {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);

            }
            return cmd;
        }
        public virtual IDbDataAdapter GetDataAdapter(string Sql, List<AppFilter> Filter = null)
        {
            IDbDataAdapter adp = null;
          
            try
            {
                ConnectionDriversConfig driversConfig = DMEEditor.Utilfunction.LinkConnection2Drivers(Dataconnection.ConnectionProp);
                string adtype = Dataconnection.DataSourceDriver.AdapterType;
                string cmdtype = Dataconnection.DataSourceDriver.CommandBuilderType;
                string cmdbuildername = driversConfig.CommandBuilderType;
                if (string.IsNullOrEmpty(cmdbuildername))
                {
                    return null;
                }
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

                            foreach (AppFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue)))
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

                   // DMEEditor.AddLogMessage("Fail", $"Error in Creating builder commands {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
                }

                adp.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                adp.MissingMappingAction = MissingMappingAction.Passthrough;

               
                ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

             //   DMEEditor.AddLogMessage("Fail", $"Error in Creating Adapter {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
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
            }
            catch (Exception ex)
            {
             //   DMEEditor.AddLogMessage("Fail", $"unsuccessfully Executed Sql ({ex.Message})", DateTime.Now, 0, TableName, Errors.Failed);
            }

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
             //   DMEEditor.AddLogMessage("Fail", $"Unsuccessfully Retrieve Child tables list {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
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
        public virtual string GetTableName(string querystring)
        {
            string schname = Dataconnection.ConnectionProp.SchemaName;
            string userid = Dataconnection.ConnectionProp.UserID;
            if (schname != null)
            {
                if (!schname.Equals(userid, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (querystring.IndexOf("select") > 0)
                    {
                        int frompos = querystring.IndexOf("from", StringComparison.InvariantCultureIgnoreCase);
                        int wherepos = querystring.IndexOf("where", StringComparison.InvariantCultureIgnoreCase);
                        if (wherepos == 0)
                        {
                            wherepos = querystring.Length - 1;

                        }

                        int firstcharindex = querystring.IndexOf(' ', frompos);
                        int lastcharindex = querystring.IndexOf(' ', firstcharindex + 2);
                        string tablename = querystring.Substring(firstcharindex + 1, lastcharindex - firstcharindex - 1);
                        querystring = querystring.Replace(' ' + tablename + ' ', $" {schname}.{tablename} ");
                    }
                    else if (querystring.IndexOf("insert") >= 0)
                    {
                        int intopos = querystring.IndexOf("into", StringComparison.InvariantCultureIgnoreCase);
                        string[] instokens = querystring.Split(' ');
                        querystring = querystring.Replace(instokens[2], $" {schname}.{instokens[2]} ");
                    }
                    else if (querystring.IndexOf("update") >= 0)
                    {
                        int setpos = querystring.IndexOf("set", StringComparison.InvariantCultureIgnoreCase);
                        string[] uptokens = querystring.Split(' ');
                        querystring = querystring.Replace(uptokens[1], $" {schname}.{uptokens[1]} ");
                    }
                    else if (querystring.IndexOf("delete") >= 0)
                    {
                        int frompos = querystring.IndexOf("from", StringComparison.InvariantCultureIgnoreCase);
                        string[] fromtokens = querystring.Split(' ');
                        querystring = querystring.Replace(fromtokens[1], $" {schname}.{fromtokens[2]} ");
                    }
                }
                    
            }
            return querystring;
        }
        #endregion
        #region "dispose"
        private bool disposedValue;
        protected  void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Closeconnection();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RDBSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public virtual void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

       
        #endregion









    }

}

