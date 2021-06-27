using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    [ClassProperties(Category = DatasourceCategory.RDBMS, DatasourceType =  DataSourceType.SqlServer)]
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
        private string PagedQuery(string originalquery, List<ReportFilter> Filter)
        {

            ReportFilter pagesizefilter = Filter.Where(o => o.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            ReportFilter pagenumberfilter = Filter.Where(o => o.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            int pagesize = Convert.ToInt32(pagesizefilter.FilterValue);
            int pagenumber = Convert.ToInt32(pagenumberfilter.FilterValue);
            string pagedquery = "SELECT ROW_NUMBER() as rn, a.* from " +
                                        $"           (  {originalquery} ) a )" +
                                       $"    WHERE rn >= (({pagenumber} * {pagesize}) + 1) and rn >= (({pagenumber} - 1) * {pagesize}) + 1)";
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
                                    qrystr += item.FieldName + " " + item.Operator + " @p_" + item.FieldName + " and  @p_" + item.FieldName + "1 " + Environment.NewLine;
                                }
                                else
                                {
                                    qrystr += item.FieldName + " " + item.Operator + " @p_" + item.FieldName + " " + Environment.NewLine;
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
                if (Filter.Where(o => o.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase)).Any() || Filter.Where(o => o.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).Any() && Filter.Count >= 2)
                {
                    if (Filter.Where(o => o.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase)).Any() || Filter.Where(o => o.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).Any())
                    {
                        qrystr = PagedQuery(qrystr, Filter);
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
        public override bool CreateEntityAs(EntityStructure entity)
        {
            bool retval = false;

            if (CheckEntityExist(entity.EntityName) == false)
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
            else
            {
                if (Entities.Count > 0)
                {
                    if (Entities.Where(p => p.EntityName.Equals(entity.EntityName, StringComparison.OrdinalIgnoreCase) && p.Created == false).Any())
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
        private string CreateEntity(EntityStructure t1)
        {
            string createtablestring = null;
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            // 
            try
            {

                createtablestring = GenerateCreateEntityScript(t1);

            }
            catch (System.Exception ex)
            {

                createtablestring = null;

                DMEEditor.AddLogMessage("Fail", $"Error in  Creating Table " + t1.EntityName + "   ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);


            }
            return createtablestring;
        }
        private string GenerateCreateEntityScript(EntityStructure t1)
        {
            string createtablestring = "Create table ";
            try
            {//-- Create Create string
                int i = 1;
                t1.EntityName = Regex.Replace(t1.EntityName, @"\s+", "_");
                createtablestring += " " + t1.EntityName + "\n(";
                if (t1.Fields.Count == 0)
                {
                    // t1=ds.GetEntityStructure()
                }
                foreach (EntityField dbf in t1.Fields)
                {

                    createtablestring += "\n [" + dbf.fieldname + "] " + DMEEditor.typesHelper.GetDataType(DatasourceName, dbf) + " ";
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

                    retval += "["+dbf.fieldname + "],";

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
    }
}
