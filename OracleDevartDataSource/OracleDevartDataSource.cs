using Devart.Data.Oracle;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace OracleDevartDataSource
{
    [ClassProperties(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.Oracle)]
    public class OracleDevartDataSource : RDBSource
    {
        public OracleDevartDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
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
        public override string EnableFKConstraints(EntityStructure t1)
        {
            try
            {
                string consname = null;
                foreach (var item in this.GetTablesFKColumnList(t1.EntityName, GetSchemaName(), null))
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
        private string PagedQuery(string originalquery, List<ReportFilter> Filter)
        {

            ReportFilter pagesizefilter = Filter.Where(o => o.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            ReportFilter pagenumberfilter = Filter.Where(o => o.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            int pagesize = Convert.ToInt32(pagesizefilter.FilterValue);
            int pagenumber = Convert.ToInt32(pagenumberfilter.FilterValue);

            string pagedquery = "SELECT * FROM " +
                             "  (SELECT a.*, rownum rn" +
                             "    FROM    (" +
                             $"             {originalquery} ) a " +
                             $"    WHERE rownum < (({pagenumber} * {pagesize}) + 1)) WHERE rn >= ((({pagenumber} - 1) * {pagesize}) + 1)";
            return pagedquery;
        }
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
                    List<ReportFilter> FilterwoPaging = Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator) && !p.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase) && !p.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).ToList();
                    if (FilterwoPaging != null)
                    {
                        if (FilterwoPaging.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {
                            qrystr += Environment.NewLine;
                            if (FoundWhere == false)
                            {
                                qrystr += " where " + Environment.NewLine;
                                FoundWhere = true;
                            }


                            int i = 0;

                            foreach (ReportFilter item in FilterwoPaging)
                            {
                                //item.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase) && item.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)
                                if (!string.IsNullOrEmpty(item.FilterValue) && !string.IsNullOrWhiteSpace(item.FilterValue))
                                {
                                    //  EntityField f = ent.Fields.Where(i => i.fieldname == item.FieldName).FirstOrDefault();
                                    //>= (((pageNumber-1) * pageSize) + 1)

                                    if (item.Operator.ToLower() == "between")
                                    {
                                        qrystr += item.FieldName + " " + item.Operator + " :p_" + item.FieldName + " and  :p_" + item.FieldName + "1 " + Environment.NewLine;
                                    }
                                    else
                                    {
                                        qrystr += item.FieldName + " " + item.Operator + " :p_" + item.FieldName + " " + Environment.NewLine;
                                    }

                                }
                                if (i < Filter.Count - 1)
                                {
                                    qrystr += " and ";
                                }

                                i++;
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
                if (Filter != null)
                {
                    if (Filter.Where(o => o.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase)).Any() || Filter.Where(o => o.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).Any() && Filter.Count >= 2)
                    {
                        if (Filter.Where(o => o.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase)).Any() || Filter.Where(o => o.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).Any())
                        {
                            qrystr = PagedQuery(qrystr, Filter);
                        }
                    }
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
                //OracleDataAdapter adp =GetDataAdapterForOracle(qrystr, Filter);
                //DataSet dataSet = new DataSet();
                //adp.Fill(dataSet);
                //DataTable dt = dataSet.Tables[0];

                return GetDataTableUsingReaderAsync(qrystr, Filter);
            }

            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in getting entity Data({ ex.Message})", DateTime.Now, 0, "", Errors.Failed);

                return null;
            }


        }
        #region "Command "
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
        public OracleCommand GetDataCommandForOracle()
        {
            OracleCommand cmd = null;
            ErrorObject.Flag = Errors.Ok;
            OracleConnection conn = (OracleConnection)RDBMSConnection.DbConn;
            try
            {
                if (Dataconnection.OpenConnection() == ConnectionState.Open)
                {
                    cmd = conn.CreateCommand();
                }
                else
                {
                    cmd = null;

                    DMEEditor.AddLogMessage("Fail", $"Error in Creating Data Command, Cannot get DataSource", DateTime.Now, -1, DatasourceName, Errors.Failed);
                }



            }
            catch (Exception ex)
            {

                cmd = null;

                DMEEditor.AddLogMessage("Fail", $"Error in Creating Data Command {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);

            }
            return cmd;
        }


        private async Task<DataTable> GetDataTableUsingReaderAsync(string Sql, List<ReportFilter> Filter = null)
        {
            DataTable retval = new DataTable();
            OracleDataReader reader;
            OracleCommand cmd = (OracleCommand)GetDataCommandForOracle();
            try
            {
                // Get Filterd Query with parameters
                if (Filter != null)
                {
                    if (Filter.Count > 0)
                    {
                        if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {
                            foreach (ReportFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue)))
                            {
                                OracleParameter parameter = cmd.CreateParameter();
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
                                    OracleParameter parameter1 = cmd.CreateParameter();
                                    parameter1.ParameterName = "p_" + item.FieldName + "1";
                                    parameter1.DbType = DbType.DateTime;
                                    string dr1 = Filter.Where(i => i.FieldName == item.FieldName).FirstOrDefault().FilterValue1;
                                    parameter1.Value = DateTime.Parse(dr1).ToShortDateString();
                                    cmd.Parameters.Add(parameter1);
                                }

                                //  parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                                cmd.Parameters.Add(parameter);

                            }

                        }
                    }

                }
                // Get Table from Reader
                CancellationToken cancellationToken = new CancellationToken();
                cmd.CommandText = Sql;

                reader = (OracleDataReader)await cmd.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);
                
                retval = new DataTable();
                retval.Load(reader);
                reader.Close();
                cmd.Dispose();
                return retval;
            }
            catch (Exception ex)
            {

                return null;
            }
        }
        public OracleDataAdapter GetDataAdapterForOracle(string Sql, List<ReportFilter> Filter = null)
        {
            OracleConnection conn = null;
            OracleDataAdapter adp = null;
            OracleCommandBuilder cmdb = null;

            try
            {
                ConnectionDriversConfig driversConfig = DMEEditor.Utilfunction.LinkConnection2Drivers(Dataconnection.ConnectionProp);


                //string adtype = Dataconnection.DataSourceDriver.AdapterType;
                //string cmdtype = Dataconnection.DataSourceDriver.CommandBuilderType;
                //string cmdbuildername = driversConfig.CommandBuilderType;
                //Type adcbuilderType = Type.GetType("OracleCommandBuilder");
                //List<ConstructorInfo> lsc = DMEEditor.assemblyHandler.GetInstance(adtype).GetType().GetConstructors().ToList(); ;
                //List<ConstructorInfo> lsc2 = DMEEditor.assemblyHandler.GetInstance(cmdbuildername).GetType().GetConstructors().ToList(); ;

                //ConstructorInfo ctor = lsc[GetCtorForAdapter(lsc)];
                //ConstructorInfo BuilderConstructer = lsc2[GetCtorForCommandBuilder(adcbuilderType.GetConstructors().ToList())];
                //ObjectActivator<Oracle.ManagedDataAccess.Client.OracleDataAdapter> adpActivator = GetActivator<Oracle.ManagedDataAccess.Client.OracleDataAdapter>(ctor);
                //ObjectActivator<Oracle.ManagedDataAccess.Client.OracleCommandBuilder> cmdbuilderActivator = GetActivator<Oracle.ManagedDataAccess.Client.OracleCommandBuilder>(BuilderConstructer);
                //create an instance:
                // adp = OracleDataAdapter( RDBMSConnection.DbConn);
                conn = (OracleConnection)RDBMSConnection.DbConn;
                adp = new OracleDataAdapter(Sql, conn);
                cmdb = new OracleCommandBuilder(adp);

                try
                {
                    //Oracle.ManagedDataAccess.Client.OracleCommand cmdBuilder = cmdbuilderActivator(adp);
                    if (Filter != null)
                    {
                        if (Filter.Count > 0)
                        {
                            if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                            {

                                foreach (ReportFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue)))
                                {

                                    OracleParameter parameter = adp.SelectCommand.CreateParameter();
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
                                        OracleParameter parameter1 = adp.SelectCommand.CreateParameter();
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

                    }

                    //  adp.ReturnProviderSpecificTypes = true;
                   
                    adp.InsertCommand = cmdb.GetInsertCommand(true);
                    adp.UpdateCommand = cmdb.GetUpdateCommand(true);
                    adp.DeleteCommand = cmdb.GetDeleteCommand(true);


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

                DMEEditor.AddLogMessage("Fail", $"Error in Creating Adapter {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
                adp = null;
            }

            return adp;
        }
        public override DataTable GetTableSchema(string TableName)
        {
            ErrorObject.Flag = Errors.Ok;
            DataTable tb = new DataTable();
            OracleDataReader reader;
            OracleCommand cmd = (OracleCommand)GetDataCommandForOracle();
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
                DMEEditor.AddLogMessage("Fail", $"unsuccessfully Executed Sql ({ex.Message})", DateTime.Now, 0, TableName, Errors.Failed);

            }

            //}

            return tb;
        }
        public override List<ChildRelation> GetTablesFKColumnList(string tablename, string SchemaName, string Filterparamters)
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
        #endregion
    }
}
