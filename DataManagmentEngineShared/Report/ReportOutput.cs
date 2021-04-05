using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Report
{
    public class ReportOutput
    {
        public ReportOutput()
        {

        }
        public IReportDefinition Definition { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public string ReportName { get; set; }
        public List<DataTable> Tables { get; set; } = new List<DataTable>();
        private string GetSelectedFields(int blocknumber)
        {
            string selectedfields = "";

            foreach (ReportBlockColumns item in Definition.Blocks[0].BlockColumns.Where(x => x.Show).OrderBy(i => i.FieldDisplaySeq))
            {

                selectedfields += "," + item.ColumnName + " as " + item.DisplayName;
            }
            selectedfields = selectedfields.Remove(0, 1);
            return selectedfields;

        }
        private DataTable CopyReportDefinition(int blocknumber)
        {
            try

            {
                string QueryString = "select " + GetSelectedFields(blocknumber) + " from " + Definition.Blocks[blocknumber].EntityID;
                DataViewDataSource ds = (DataViewDataSource)DMEEditor.GetDataSource(Definition.Blocks[blocknumber].ViewID);
                DataTable PrintData = (DataTable)ds.GetEntity(Definition.Blocks[blocknumber].EntityID, QueryString);
          //      PrintData.Wait();
                DMEEditor.AddLogMessage("Success", $"Copying Report Data", DateTime.Now, 0, null, Errors.Ok);
                return PrintData;
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Copying Report Data";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }


        }
        public bool GetBlockDataIntoTables()
        {
            int blocknumber = 0;
            //bool retval=false;
            Tables = new List<DataTable>();
            try
            {
                for (int i = 0; i < Definition.Blocks.Where(p=>p.ViewID!=null).Count(); i++)
                {
                    blocknumber = i;
                    DataTable tb =  CopyReportDefinition(blocknumber);
                 //  tb.Wait();
                    Tables.Add(tb);
                }
                DMEEditor.AddLogMessage("Success", $"Getting Data into Tables", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Getting Data into Tables";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
           
        

        }
        public int GetTotalNumberofRows()
        {
            return Tables.Sum(x => x.Rows.Count);
        }
    }
}
