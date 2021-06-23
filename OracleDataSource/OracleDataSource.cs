
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    [ClassProperties(Category = DatasourceCategory.RDBMS, DatasourceType =  DataSourceType.Oracle)]
    class OracleDataSource : RDBSource
    {
        public OracleDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            
            
        }
        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                string consname = null;
               
                foreach (var item in this.GetTablesFKColumnList(t1.EntityName, GetSchemaName(), null))
                {
                    consname = item.RalationName;
                    this.ExecuteSql($"ALTER TABLE {t1.EntityName} DISABLE CONSTRAINT {consname}");
                }
               
                DMEEditor.ErrorObject.Message = "successfull Disabled Oracle FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Diabling Oracle FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }
        public override string EnableFKConstraints( EntityStructure t1)
        {
            try
            {
                string consname = null;
                foreach (var item in this.GetTablesFKColumnList(t1.EntityName,GetSchemaName(),null))
                {
                    consname = item.RalationName;
                    this.ExecuteSql($"ALTER TABLE {t1.EntityName} DISABLE CONSTRAINT {consname}");
                }
               
                DMEEditor.ErrorObject.Message = "successfull Enabled Oracle FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Enabing Oracle FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }
        //public virtual string GetInsertString(string EntityName, DataRow row, EntityStructure DataStruct)
        //{
        //    List<EntityField> SourceEntityFields = new List<EntityField>();
        //    List<EntityField> DestEntityFields = new List<EntityField>();
        //    // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
        //    //   map= Mapping.FldMapping;
        //    //    EntityName = Regex.Replace(EntityName, @"\s+", "");
        //    string Insertstr = "insert into " + EntityName + " (";
        //    string Valuestr = ") values (";
        //    var insertfieldname = "";
        //    // string datafieldname = "";
        //    string typefield = "";
        //    int i = DataStruct.Fields.Count();
        //    int t = 0;
        //    foreach (EntityField item in DataStruct.Fields)
        //    {

        //        //if (!DBNull.Value.Equals(row[item.fieldname]))
        //        //{
        //            // insertfieldname = Regex.Replace(item.fieldname, @"\s+", "");
        //            Insertstr += item.fieldname + ",";
        //            Valuestr += ":p_" + item.fieldname + ",";
        //            //switch (item.fieldtype)
        //            //{
        //            //    case "System.String":
        //            //        if (row[item.fieldname].ToString().Contains("'"))
        //            //        {
        //            //            string ve = row[item.fieldname].ToString();
        //            //            ve = ve.Replace("'", "''");
        //            //            Valuestr += "'" + ve + "',";
        //            //        }
        //            //        else
        //            //        {
        //            //            Valuestr += "'" + row[item.fieldname] + "',";
        //            //        }


        //            //        break;
        //            //    case "System.Int":
        //            //        Valuestr += "" + row[item.fieldname] + ",";
        //            //        break;
        //            //    case "System.DateTime":
        //            //        DateTime time = (DateTime)row[item.fieldname];
        //            //        Valuestr += "'" + time.ToString(dateformat) + "',";
        //            //        break;
        //            //    default:
        //            //        Valuestr += "'" + row[item.fieldname] + "',";
        //            //        break;
        //            //}
        //            //if (t == i)
        //            //{
        //            //    Insertstr += Valuestr + @"\n";
        //            //}
        //            //else
        //            //{
        //            //    Insertstr += Valuestr + @",\n";
        //            //}
        //        }


        //        t += 1;

        //   // }



        //    Insertstr = Insertstr.Remove(Insertstr.Length - 1);
        //    Valuestr = Valuestr.Remove(Valuestr.Length - 1);
        //    Valuestr += ")";
        //    return Insertstr + Valuestr;
        //}
        //public override string GetUpdateString(string EntityName, DataRow row, EntityStructure DataStruct)
        //{
        //    List<EntityField> SourceEntityFields = new List<EntityField>();
        //    List<EntityField> DestEntityFields = new List<EntityField>();
        //    // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
        //    //   map= Mapping.FldMapping;
        //    //     EntityName = Regex.Replace(EntityName, @"\s+", "");
        //    string Updatestr = @"Update " + EntityName + "  set " + Environment.NewLine;
        //    string Valuestr = "";
        //    // var insertfieldname = "";
        //    //string datafieldname = "";
        //    //string typefield = "";
        //    int i = DataStruct.Fields.Count();
        //    int t = 0;
        //    foreach (EntityField item in DataStruct.Fields)
        //    {
        //        if (!DataStruct.PrimaryKeys.Any(l => l.fieldname == item.fieldname))
        //        {
        //            //if (!DBNull.Value.Equals(row[item.fieldname]))
        //            //{
        //            //    //     insertfieldname = Regex.Replace(item.fieldname, @"\s+", "_");
        //                Updatestr += item.fieldname + "=";
        //                Updatestr += ":p_" + item.fieldname + ",";
        //                //switch (item.fieldtype)
        //                //{
        //                //    case "System.String":
        //                //        if (row[item.fieldname].ToString().Contains("'"))
        //                //        {
        //                //            string ve = row[item.fieldname].ToString();
        //                //            ve = ve.Replace("'", "''");
        //                //            Updatestr += "'" + ve + "',";
        //                //        }
        //                //        else
        //                //        {
        //                //            Updatestr += "'" + row[item.fieldname] + "',";
        //                //        }

        //                //        break;
        //                //    case "System.Int":
        //                //        Updatestr += "" + row[item.fieldname] + ",";
        //                //        break;
        //                //    case "System.DateTime":
        //                //        DateTime time = (DateTime)row[item.fieldname];
        //                //        Updatestr += "'" + time.ToString(dateformat) + "'";
        //                //        break;
        //                //    default:
        //                //        Updatestr += "'" + row[item.fieldname] + "',";
        //                //        break;
        //                //}
        //            //}


        //        }


        //        t += 1;
        //    }

        //    Updatestr = Updatestr.Remove(Updatestr.Length - 1);

        //    Updatestr += @" where " + Environment.NewLine;
        //    i = DataStruct.PrimaryKeys.Count();
        //    t = 1;
        //    foreach (EntityField item in DataStruct.PrimaryKeys)
        //    {
        //        //if (!DBNull.Value.Equals(row[item.fieldname]))
        //        //{
        //            if (t == 1)
        //            {
        //                Updatestr += item.fieldname + "=";
        //            }
        //            else
        //            {
        //                Updatestr += " and " + item.fieldname + "=";
        //            }
        //            Updatestr += ":p_" + item.fieldname + "";
        //            //    Updatestr += item.fieldname + "=";
        //            //switch (item.fieldtype)
        //            //{
        //            //    case "System.String":
        //            //        Updatestr += "'" + row[item.fieldname] + "'";
        //            //        break;
        //            //    case "System.Int":
        //            //        Updatestr += "" + row[item.fieldname] + "";
        //            //        break;
        //            //    case "System.DateTime":
        //            //        DateTime time = (DateTime)row[item.fieldname];
        //            //        Updatestr += "'" + time.ToString(dateformat) + "'";
        //            //        break;
        //            //    default:
        //            //        Updatestr += "'" + row[item.fieldname] + "'";
        //            //        break;
        //            //}
        //       // }


        //        t += 1;
        //    }
        //    //  Updatestr = Updatestr.Remove(Valuestr.Length - 1);
        //    return Updatestr;
        //}
        //public override string GetDeleteString(string EntityName, DataRow row, EntityStructure DataStruct)
        //{

        //    List<EntityField> SourceEntityFields = new List<EntityField>();
        //    List<EntityField> DestEntityFields = new List<EntityField>();
        //    // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
        //    //   map= Mapping.FldMapping;
        //    // EntityName = Regex.Replace(EntityName, @"\s+", "");
        //    string Updatestr = @"Delete from " + EntityName + "  ";
        //    string Valuestr = "";
        //    // var insertfieldname = "";
        //    //string datafieldname = "";
        //    //string typefield = "";
        //    int i = DataStruct.Fields.Count();
        //    int t = 0;

        //    Updatestr += @" where ";
        //    i = DataStruct.PrimaryKeys.Count();
        //    t = 1;
        //    foreach (EntityField item in DataStruct.PrimaryKeys)
        //    {
        //        string st = row[item.fieldname, DataRowVersion.Original].ToString();
        //        if (!DBNull.Value.Equals(row[item.fieldname, DataRowVersion.Original]))
        //        {
        //            if (t == 1)
        //            {
        //                Updatestr += item.fieldname + "=";
        //            }
        //            else
        //            {
        //                Updatestr += " and " + item.fieldname + "=";
        //            }

        //            switch (item.fieldtype)
        //            {
        //                case "System.String":
        //                    Updatestr += "'" + row[item.fieldname, DataRowVersion.Original].ToString() + "'";
        //                    break;
        //                case "System.Int":
        //                    Updatestr += "" + row[item.fieldname, DataRowVersion.Original].ToString() + "";
        //                    break;
        //                default:
        //                    Updatestr += "'" + row[item.fieldname, DataRowVersion.Original].ToString() + "'";
        //                    break;
        //            }
        //        }

        //        t += 1;
        //    }
        //    //   Updatestr= Updatestr.Remove(Updatestr.Length - 1);
        //    return Updatestr;
        //}
        //public override string GetInsertString(string EntityName, DataRow row, IMapping_rep Mapping, EntityStructure DataStruct)
        //{

        //    List<EntityField> SourceEntityFields = new List<EntityField>();
        //    List<EntityField> DestEntityFields = new List<EntityField>();
        //    // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
        //    //   map= Mapping.FldMapping;
        //    //  EntityName = Regex.Replace(EntityName, @"\s+", "");
        //    string Insertstr = "insert into " + EntityName + " (";
        //    string Valuestr = ") values (";
        //    var insertfieldname = "";
        //    string datafieldname = "";
        //    string typefield = "";
        //    int i = Mapping.FldMapping.Count();
        //    int t = 0;
        //    foreach (IMapping_rep_fields item in Mapping.FldMapping)
        //    {
        //        if (EntityName == Mapping.EntityName1)
        //        {
        //            insertfieldname = item.FieldName1;
        //            datafieldname = item.FieldName2;
        //            typefield = item.FieldType2;
        //        }
        //        else
        //        {
        //            insertfieldname = item.FieldName2;
        //            datafieldname = item.FieldName1;
        //            typefield = item.FieldType1;
        //        }
        //        Valuestr += ":p_" + datafieldname + ",";
        //        //if (!DBNull.Value.Equals(row[datafieldname]))
        //        //{
        //        //    //   insertfieldname = Regex.Replace(insertfieldname, @"\s+", "");
        //        //    Insertstr += insertfieldname + ",";
        //        //    switch (typefield)
        //        //    {
        //        //        case "System.String":
        //        //            if (row[datafieldname].ToString().Contains("'"))
        //        //            {
        //        //                string ve = row[datafieldname].ToString();
        //        //                ve = ve.Replace("'", "''");
        //        //                Valuestr += "'" + ve + "',";
        //        //            }
        //        //            else
        //        //            {
        //        //                Valuestr += "'" + row[datafieldname] + "',";
        //        //            }

        //        //            break;
        //        //        case "System.Int":
        //        //            Valuestr += "" + row[datafieldname] + ",";
        //        //            break;
        //        //        default:
        //        //            Valuestr += "'" + row[datafieldname] + "',";
        //        //            break;
        //        //    }
        //        //}

        //        t += 1;

        //    }



        //    Insertstr = Insertstr.Remove(Insertstr.Length - 1);
        //    Valuestr = Valuestr.Remove(Valuestr.Length - 1);
        //    Valuestr += ")";
        //    return Insertstr + Valuestr;
        //}
        //public override string GetUpdateString(string EntityName, DataRow row, IMapping_rep Mapping, EntityStructure DataStruct)
        //{

        //    List<EntityField> SourceEntityFields = new List<EntityField>();
        //    List<EntityField> DestEntityFields = new List<EntityField>();
        //    // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
        //    //   map= Mapping.FldMapping;
        //    // EntityName = Regex.Replace(EntityName, @"\s+", "");
        //    string Insertstr = @"Update " + EntityName + " ( \n set ";
        //    string Valuestr = "";
        //    var insertfieldname = "";
        //    string datafieldname = "";
        //    string typefield = "";
        //    int i = Mapping.FldMapping.Count();
        //    int t = 0;
        //    foreach (IMapping_rep_fields item in Mapping.FldMapping)
        //    {
        //        if (EntityName == Mapping.EntityName1)
        //        {
        //            insertfieldname = item.FieldName1;
        //            datafieldname = item.FieldName2;
        //            typefield = item.FieldType2;
        //        }
        //        else
        //        {
        //            insertfieldname = item.FieldName2;
        //            datafieldname = item.FieldName1;
        //            typefield = item.FieldType1;
        //        }
        //        Valuestr += ":p_" + datafieldname + ",";
        //        //if (!DBNull.Value.Equals(row[datafieldname]))
        //        //{
        //        //    // insertfieldname = Regex.Replace(insertfieldname, @"\s+", "");
        //        //    Insertstr += insertfieldname + ",";
        //        //    switch (typefield)
        //        //    {
        //        //        case "System.String":
        //        //            if (row[datafieldname].ToString().Contains("'"))
        //        //            {
        //        //                string ve = row[datafieldname].ToString();
        //        //                ve = ve.Replace("'", "''");
        //        //                Valuestr += "'" + ve + "',";
        //        //            }
        //        //            else
        //        //            {
        //        //                Valuestr += "'" + row[datafieldname] + "',";
        //        //            }
        //        //            break;
        //        //        case "System.Int":
        //        //            Valuestr += "" + row[datafieldname] + ",";
        //        //            break;
        //        //        default:
        //        //            Valuestr += "'" + row[datafieldname] + "',";
        //        //            break;
        //        //    }
        //        //}



        //        t += 1;

        //    }



        //    Insertstr = Insertstr.Remove(Insertstr.Length - 1);
        //    Valuestr = Valuestr.Remove(Valuestr.Length - 1);
        //    Valuestr += ")";
        //    return Insertstr + Valuestr;
        //}

        //public override IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        //{

        //    //  RunCopyDataBackWorker(EntityName,  UploadData,  Mapping );
        //    #region "Update Code"
        //    string str;
        //    // IDbTransaction sqlTran;
        //    DataTable tb = (DataTable)UploadData;
        //    // DMEEditor.classCreator.CreateClass();
        //    //List<object> f = DMEEditor.Utilfunction.GetListByDataTable(tb);
        //    ErrorObject.Flag = Errors.Ok;
        //    EntityStructure DataStruct = GetEntityStructure(EntityName);

        //    OracleCommand command = (OracleCommand)Dataconnection.DbConn.CreateCommand();


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
        //                command.Parameters.Clear();
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

        //                       // OracleParameter parameter = new OracleParameter();

        //                       // parameter.Value = r[item.fieldname];
        //                      //  parameter.ParameterName = ":p_" + item.fieldname;
        //                   //     parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
        //                      //  parameter.Value = r[item.fieldname];
        //                       // parameter.Direction = ParameterDirection.InputOutput;
        //                        command.Parameters.Add("p_" + item.fieldname, r[item.fieldname]);
        //                    }

        //                    string msg = "";
        //                    int rowsUpdated = command.ExecuteNonQuery();
        //                    if (rowsUpdated > 0)
        //                    {
        //                        msg= $"Successfully I/U/D  Record {i} to {EntityName} : {updatestring}";
        //                    }else
        //                    {
        //                        msg = $"Fail to I/U/D  Record {i} to {EntityName} : {updatestring}";
        //                    }
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
        //                    tr.errormessage = msg;
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

        //            command.Parameters.Clear();
        //            command.CommandText = "Commit";
        //            command.ExecuteNonQuery();

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
        private string BuildQuery(string originalquery, List<ReportFilter> Filter)
        {
            string retval;
            string[] stringSeparators;
            string[] sp;
            string qrystr = "Select ";
            bool FoundWhere = false;
            QueryBuild queryStructure = new QueryBuild();
            try
            {
                //stringSeparators = new string[] {"select ", " from ", " where ", " group by "," having ", " order by " };
                // Get Selected Fields
                stringSeparators = new string[] { "select ", " from ", " where ", " group by ", " having ", " order by " };
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
                                    qrystr += item.FieldName + " " + item.Operator + " :p_" + item.FieldName + " and  :p_" + item.FieldName + "1 " + Environment.NewLine;
                                }
                                else
                                {
                                    qrystr += item.FieldName + " " + item.Operator + " :p_" + item.FieldName + " " + Environment.NewLine;
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
                    qrystr += spwhere[0];
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
        public override object GetEntity(string EntityName, List<ReportFilter> Filter)
        {
            ErrorObject.Flag = Errors.Ok;
            //  int LoadedRecord;

            EntityName = EntityName.ToLower();
            string inname = "";
            string qrystr = "select* from ";

            if (!string.IsNullOrEmpty(EntityName) && !string.IsNullOrWhiteSpace(EntityName))
            {
                if (!EntityName.Contains("select") && !EntityName.Contains("from"))
                {
                    qrystr = "select * from " + EntityName;
                    inname = EntityName;
                }
                else
                {

                    string[] stringSeparators = new string[] { " from ", " where ", " group by ", " order by " };
                    string[] sp = EntityName.Split(stringSeparators, StringSplitOptions.None);
                    qrystr = EntityName;
                    inname = sp[1].Trim();
                }

            }
            // EntityStructure ent = GetEntityStructure(inname);
            qrystr = BuildQuery(qrystr, Filter);

            try
            {
                IDataAdapter adp = GetDataAdapter(qrystr, Filter);
                DataSet dataSet = new DataSet();
                adp.Fill(dataSet);
                DataTable dt = dataSet.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in getting entity Data({ ex.Message})", DateTime.Now, 0, "", Errors.Failed);

                return null;
            }


        }

    }
}
