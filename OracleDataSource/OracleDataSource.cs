
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

    }
}
