
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DoddleReport;
using DoddleReport.Writers;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Report
{
    public class DoodleReportGenerator : IReportDMWriter
    {
        DoddleReport.Report report { get; set; }
        public ReportOutput reportOutput { get; set; }
        Stream outputstream { get; set; }
        public IReportDefinition Definition { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public bool Html { get; set; } = true;
        public bool Text { get; set; } = true;
        public bool Csv { get; set; } = true;
        public bool PDF { get; set; }
        public bool Excel { get; set; } = true;
      
        private string mOutputFile;
        public string OutputFile { get => mOutputFile; }
        //HtmlReportWriter htmlReportWriter;
       // ExcelReportWriter excelReportWriter;
       // DelimitedTextReportWriter delimitedTextReportWriter;
        private string GetSelectedFields()
        {
            string selectedfields = "";
          
            foreach (ReportBlockColumns item in Definition.Blocks[0].BlockColumns.Where(x => x.Show).OrderBy(i=>i.FieldDisplaySeq))
            {

                selectedfields += "," + item.ColumnName + " as " + item.DisplayName;
            }
            selectedfields = selectedfields.Remove(0, 1);
            return selectedfields;
           
        }
        private bool CopyReportDefinition2Doodle()
        {
        try

            {
              report = new DoddleReport.Report(reportOutput.Tables[0].ToReportSource());

                report.TextFields.Title = string.Format(@"<h1 style=""text - align: center;""><strong>{0}</strong></h1>",Definition.Title.Text);// "Products Report";
                report.TextFields.SubTitle = Definition.SubTitle.Text; //"This is a sample report showing how Doddle Report works";
                report.TextFields.Footer = "Copyright 2021 &copy; The Beep Project"; //Definition.Footer.Text;//
                report.TextFields.Header = Definition.Header.Text;
                
    //            report.TextFields.Header = string.Format(@"
    //Report Generated: {0}
    //Total Products: {1}
    //Total Orders: {2}
    //Total Sales: {3:c}", DateTime.Now, totalProducts, totalOrders, totalProducts * totalOrders);


                // Render hints allow you to pass additional hints to the reports as they are being rendered
                report.RenderHints.BooleanCheckboxes = true;
                if (reportOutput.Tables[0].Rows.Count>0)
                {
                    foreach (ReportBlockColumns item in Definition.Blocks[0].BlockColumns.Where(o => o.Show ==true))
                    {
                        report.DataFields[item.ColumnName].Hidden = false;
                    }
                }
               
                

                DMEEditor.AddLogMessage("Success", $"Copying Report Data", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Copying Report Data";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
           
        
        }
        public IErrorsInfo RunReport( ReportType reportType,string outputFile)
        {
            try

            {
                reportOutput = new ReportOutput();
                reportOutput.Definition = Definition;
                reportOutput.DMEEditor = DMEEditor;

                if (reportOutput.GetBlockDataIntoTables())
                {
                    CopyReportDefinition2Doodle();
                    switch (reportType)
                    {
                        case ReportType.html:
                            break;
                        case ReportType.xls:
                            break;
                        case ReportType.csv:
                            break;
                        case ReportType.pdf:
                            break;
                        default:
                            break;
                    }
                    FileStream fileStream = new FileStream(outputFile + "." + reportType.ToString(), FileMode.Create);
                    outputstream = fileStream;
                    var writer = new HtmlReportWriter();
                    writer.WriteReport(report, outputstream);
                    mOutputFile = outputFile + "." + reportType.ToString();
                    DMEEditor.AddLogMessage("Success", $"Creating Doddle Report", DateTime.Now, 0, null, Errors.Ok);
                }
                  

            }
            catch (Exception ex)
            {
                string errmsg = "Error Saving Function Mapping ";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public DoodleReportGenerator()
        {
            report = new DoddleReport.Report();
            report.RenderingRow += Report_RenderingRow;
           
        }
        private void Report_RenderingRow(object sender, ReportRowEventArgs e)
        {
            if (e.Row.RowType == ReportRowType.DataRow)
            {
                //decimal unitPrice = (decimal)e.Row["UnitPrice"];
                //if (unitPrice < 10)
                //{
                //    e.Row.Fields["UnitPrice"].DataStyle.Bold = true;
                //}
            }
        }
    }
}
