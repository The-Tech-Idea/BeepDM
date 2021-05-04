using DynamicRdlcReport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Report
{
    public class RDLCdynamicReport : IReportDMWriter
    {
        public IReportDefinition Definition { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public bool Html { get ; set ; }
        public bool Text { get ; set ; }
        public bool Csv { get ; set ; }
        public bool PDF { get ; set ; }
        public bool Excel { get ; set ; }

      //  private string mOutputFile;
        public string OutputFile { get; set; }

        public IErrorsInfo RunReport(ReportType reportType, string outputFile)
        {
            try

            {
                var f = new ReportForm();
                //f.ReportColumns = this.dataGridView1.Columns.Cast<DataGridViewColumn>()
                //                      .Select(x => new ReportColumn(x.DataPropertyName)
                //                      {
                //                          Title = x.HeaderText,
                //                          Width = x.Width
                //                      }).ToList();
                f.ReportData = Definition.Blocks[0].EntityID;
                f.ShowDialog();
                

                DMEEditor.AddLogMessage("Success", $"Generating Report", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Generating Report";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
           
        }
    }
}
