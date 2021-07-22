using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Report.RDLC.ReportBuilderEntities;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Report
{
    public class RDLCReports : IReportDMWriter
    {
        public IReportDefinition Definition { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public ReportBuilder reportbuilder { get; set; }
        public bool Html { get; set; } = true;
        public bool Text { get; set; } 
        public bool Csv { get; set; }
        public bool PDF { get; set; } = true;
        public bool Excel { get; set; } = true;
      //  private string mOutputFile;
        public string OutputFile { get; set; }
        public IErrorsInfo RunReport(ReportType reportType, string outputFile)
        {  try

            {

                RDLCEngine.GenerateReport(reportbuilder);

                DMEEditor.AddLogMessage("Success", $"Generating Report", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Generating Report";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
           
        }

      
        private bool CopyReportDefinition2RDLCReportBuilder()
        {
            try

            {
                reportbuilder = new ReportBuilder();
              //  reportbuilder.DataSource=Definition.Blocks[0].


                DMEEditor.AddLogMessage("Success", $"Copied Report ", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Copying Report";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
           
        
        }
    }
}
