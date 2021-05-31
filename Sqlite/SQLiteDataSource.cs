using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    [ClassProperties(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlLite)]
    public class SQLiteDataSource : RDBSource, ILocalDB

    {
        private string dateformat = "yyyy-MM-dd HH:mm:ss";
        public bool CanCreateLocal { get ; set; }
        public SQLiteDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.SqlLite;
        }
        public  bool CopyDB( string DestDbName, string DesPath)
        {
            try
            {
                if (!System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                {
                    File.Copy(base.Dataconnection.ConnectionProp.ConnectionString, Path.Combine(DesPath,DestDbName));
                }
                DMEEditor.AddLogMessage("Success", "Copy Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Copy Sqlite Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public  bool CreateDB()
        {
            try
            {
                if (!Path.HasExtension(base.Dataconnection.ConnectionProp.FileName) )
                {
                    base.Dataconnection.ConnectionProp.FileName = base.Dataconnection.ConnectionProp.FileName + ".s3db";
                }
                if (!System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                {
                    SQLiteConnection.CreateFile( Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName ));
                    DMEEditor.AddLogMessage("Success", "Create Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    DMEEditor.AddLogMessage("Success", "Sqlite Database already exist", DateTime.Now, 0, null, Errors.Ok);
                }
                return true;
            }
            catch (Exception ex)
            {
                string mes = "Could not Create Sqlite Database";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public  bool DeleteDB()
        {
            try
            {
                CloseConnection();
                GC.Collect();
                GC.WaitForPendingFinalizers();
              
                    if (System.IO.File.Exists(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName)))
                    {
                        File.Delete(Path.Combine(base.Dataconnection.ConnectionProp.FilePath, base.Dataconnection.ConnectionProp.FileName));
                    }
                    DMEEditor.AddLogMessage("Success", "Deleted Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
                    return true;
               
            }
            catch (Exception ex)
            {
                string mes = "Could not Delete Sqlite Database";
                DMEEditor.AddLogMessage("Fail",ex.Message+ mes, DateTime.Now, -1, mes, Errors.Failed);
                return false;
            };
        }
        public  IErrorsInfo DropEntity(string EntityName)
        {  try

            {

                String cmdText = $"drop table  '{EntityName}'";
                DMEEditor.ErrorObject=base.ExecuteSql(cmdText);
              
                if (!base.CheckEntityExist(EntityName))
                {
                    DMEEditor.AddLogMessage("Success", $"Droping Entity {EntityName}", DateTime.Now, 0, null, Errors.Ok);
                }else
                {
                    
                    DMEEditor.AddLogMessage("Error", $"Droping Entity {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                }
               
            }
            catch (Exception ex)
            {
                string errmsg = $"Error Droping Entity {EntityName}";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        public  IErrorsInfo CloseConnection()
        {
            try
            {
                SQLiteConnection.ClearAllPools();
                if (base.RDBMSConnection.DbConn != null)
                {
                    base.RDBMSConnection.DbConn.Close();
                }
             

                DMEEditor.AddLogMessage("Success", $"Closing connection to Sqlite Database", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Closing connection to Sqlite Database";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public override string DisableFKConstraints( EntityStructure t1)
        {
            try
            {
                this.ExecuteSql("PRAGMA ignore_check_constraints = 0");
                DMEEditor.ErrorObject.Message = "successfull Disabled Sqlite FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Diabling Sqlite FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql("PRAGMA ignore_check_constraints = 1");
                DMEEditor.ErrorObject.Message = "successfull Enabled Sqlite FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Enabing Sqlite FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }
        //public override IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        //{

        //    //  RunCopyDataBackWorker(EntityName,  UploadData,  Mapping );
        //    #region "Update Code"
        //    string str;
        //    IDbTransaction sqlTran;
        //    DataTable tb = (DataTable)UploadData;
        //    // DMEEditor.classCreator.CreateClass();
        //    //List<object> f = DMEEditor.Utilfunction.GetListByDataTable(tb);
        //    ErrorObject.Flag = Errors.Ok;
        //    EntityStructure DataStruct = GetEntityStructure(EntityName);

        //    SQLiteCommand command = (SQLiteCommand) Dataconnection.DbConn.CreateCommand();


        //    int CurrentRecord = 0;
        //    int highestPercentageReached = 0;
        //    int numberToCompute = 0;
        //    try
        //    {
        //        if (tb != null)
        //        {

        //            numberToCompute = tb.Rows.Count;
        //            tb.TableName = EntityName;
        //            // int i = 0;
        //            string updatestring = null;
        //            DataTable changes = tb;//.GetChanges();
        //            for (int i = 0; i < tb.Rows.Count; i++)
        //            {
        //                try
        //                {
        //                    DataRow r = tb.Rows[i];

        //                    CurrentRecord = i;
        //                    switch (r.RowState)
        //                    {
        //                        case DataRowState.Unchanged:
        //                        case DataRowState.Added:
        //                            updatestring = GetInsertString(EntityName, r, DataStruct);


        //                            break;
        //                        case DataRowState.Deleted:
        //                            updatestring = GetDeleteString(EntityName, r, DataStruct);
        //                            break;
        //                        case DataRowState.Modified:
        //                            updatestring = GetUpdateString(EntityName, r, DataStruct);
        //                            break;
        //                        default:
        //                            updatestring = GetInsertString(EntityName, r, DataStruct);
        //                            break;
        //                    }

        //                    command.CommandText = updatestring;
        //                    foreach (EntityField item in DataStruct.Fields)
        //                    {
        //                        SQLiteParameter parameter = new SQLiteParameter();
        //                        parameter.Command = command;
                                
        //                        parameter.Value = r[item.fieldname];
        //                        parameter.ParameterName = "@" + item.fieldname;
        //                        parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
        //                        parameter.Value = r[item.fieldname];
        //                        command.Parameters.Add(parameter);
        //                    }


        //                    command.ExecuteNonQuery();

        //                    int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
        //                    if (percentComplete > highestPercentageReached)
        //                    {
        //                        highestPercentageReached = percentComplete;

        //                    }
        //                    PassedArgs args = new PassedArgs
        //                    {
        //                        CurrentEntity = EntityName,
        //                        DatasourceName = DatasourceName,
        //                        DataSource = this,
        //                        EventType = "UpdateEntity",


        //                    };
        //                    if (DataStruct.PrimaryKeys != null)
        //                    {
        //                        if (DataStruct.PrimaryKeys.Count <= 1)
        //                        {
        //                            args.ParameterString1 = r[DataStruct.PrimaryKeys[0].fieldname].ToString();
        //                        }
        //                        if (DataStruct.PrimaryKeys.Count == 2)
        //                        {
        //                            args.ParameterString2 = r[DataStruct.PrimaryKeys[1].fieldname].ToString();
        //                        }
        //                        if (DataStruct.PrimaryKeys.Count == 3)
        //                        {
        //                            args.ParameterString3 = r[DataStruct.PrimaryKeys[2].fieldname].ToString();

        //                        }
        //                    }
        //                    args.ParameterInt1 = percentComplete;

        //                    LScriptTracker tr = new LScriptTracker();
        //                    tr.currenrecordentity = EntityName;
        //                    tr.currentrecorddatasourcename = DatasourceName;
        //                    tr.currenrecordindex = i;
        //                    tr.scriptType = DDLScriptType.CopyData;
        //                    tr.errorsInfo = DMEEditor.ErrorObject;
        //                    tr.errormessage = $"Copied Data  Record {i} to {EntityName} : {updatestring}";
        //                    DMEEditor.ETL.trackingHeader.trackingscript.Add(tr);

        //                    args.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
        //                  //  PassEvent?.Invoke(this, args);
        //                }
        //                catch (Exception er)
        //                {
        //                    PassedArgs args = new PassedArgs
        //                    {
        //                        CurrentEntity = EntityName,
        //                        DatasourceName = DatasourceName,
        //                        DataSource = this,
        //                        EventType = "UpdateEntity",


        //                    };
        //                    LScriptTracker tr = new LScriptTracker();
        //                    tr.currenrecordentity = EntityName;
        //                    tr.currentrecorddatasourcename = DatasourceName;
        //                    tr.currenrecordindex = i;
        //                    tr.scriptType = DDLScriptType.CopyData;
        //                    tr.errorsInfo = DMEEditor.ErrorObject;
        //                    tr.errormessage = $"Fail to insert/update/delete  Record {i} to {EntityName} : {er.Message} :  {updatestring} ";
        //                    DMEEditor.ETL.trackingHeader.trackingscript.Add(tr);
        //                    args.Objects.Add(new ObjectItem { obj = tr, Name = "TrackingHeader" });
        //                   // PassEvent?.Invoke(this, args);
        //                    //  DMEEditor.RaiseEvent(this, args);
        //                    //  DMEEditor.AddLogMessage("Fail", $"Fail to insert/update/delete  Record {i} to {EntityName} {er.Message}", DateTime.Now, 0, null, Errors.Failed);
        //                }
        //            }



        //            command.Dispose();
        //            //    sqlTran.Commit();
        //            Logger.WriteLog("Successfully Written Data to DataSource ");
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorObject.Ex = ex;

        //        command.Dispose();
        //        try
        //        {
        //            // Attempt to roll back the transaction.
        //            //   sqlTran.Rollback();
        //            str = "Unsuccessfully no Data has been written to Data Source,Rollback Complete";
        //        }
        //        catch (Exception exRollback)
        //        {
        //            // Throws an InvalidOperationException if the connection
        //            // is closed or the transaction has already been rolled
        //            // back on the server.
        //            // Console.WriteLine(exRollback.Message);
        //            str = "Unsuccessfully no Data has been written to Data Source,Rollback InComplete";
        //            ErrorObject.Ex = exRollback;
        //        }
        //        str = "Unsuccessfully no Data has been written to Data Source";


        //    }
        //    #endregion


        //    return ErrorObject;
        //}
        private  List<FkListforSQLlite> GetSqlLiteTableKeysAsync(string tablename)
        {
            var tb =  base.GetData<FkListforSQLlite>("PRAGMA foreign_key_check(" + tablename + ");");
            return tb;
        }
    }
   
}
