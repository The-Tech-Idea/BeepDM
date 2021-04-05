using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public class SQLServerDataSource : RDBSource
    {
        
        public SQLServerDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }
        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} NOCHECK CONSTRAINT ALL");
                DMEEditor.ErrorObject.Message = "successfull Disabled SQlCompact FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Diabling SQlCompact FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }

        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                this.ExecuteSql($"ALTER TABLE {t1.EntityName} WITH CHECK CHECK CONSTRAINT all");
                DMEEditor.ErrorObject.Message = "successfull Enabled SQlCompact FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Enabing SQlCompact FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
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

        //    SqlCommand command = (SqlCommand)Dataconnection.DbConn.CreateCommand();


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
                                
        //                            SqlParameter parameter = new SqlParameter();

        //                            parameter.Value = r[item.fieldname];
        //                            parameter.ParameterName = "@" + item.fieldname;
        //                            parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
        //                            parameter.Value = r[item.fieldname];
        //                            command.Parameters.Add(parameter);
                               

                                 
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
        //                    //  PassEvent?.Invoke(this, args);
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
        //                    // PassEvent?.Invoke(this, args);
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
    }
}
