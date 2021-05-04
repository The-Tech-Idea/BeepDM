using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.DataView;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Report
{
    public class GenericReports : IReportDMWriter
    {
        public System.Drawing.Printing.PrintDocument printDocument1 { get; set; }
        public System.Windows.Forms.PrintPreviewDialog printPreviewDialog1 { get; set; } = new System.Windows.Forms.PrintPreviewDialog();
        public System.Windows.Forms.PrintDialog printDialog1 { get; set; } = new System.Windows.Forms.PrintDialog();
        public System.Windows.Forms.PageSetupDialog pageSetupDialog1 { get; set; } = new System.Windows.Forms.PageSetupDialog();
        public PrinterSettings printerSetting1 { get; set; }
        public PageSettings pageSettings1 { get; set; }

        public DataGridPrinter dataGridPrinter;
        public uc_GenericReportView reportView { get; set; }
        private DataGridPrinter gridPrinter { get; set; }
        private int blockid { get; set; } = 0;
        public bool Html { get; set; }
        public bool Text { get; set; } = true;
        public bool Csv { get; set; }
        public bool PDF { get; set; }
        public bool Excel { get; set; }
      //  private string mOutputFile;
        public string OutputFile { get; set; }
        public IReportDefinition Definition { get ; set ; }
        public ReportOutput reportOutput { get; set; }

        public IDMEEditor DMEEditor { get ; set ; }
      //  private System.ComponentModel.Container components;
     //   private System.Windows.Forms.Button printButton;
        private Font printFont;
      //  private StreamWriter streamWriter;
      
     //   int counter ;
        int curPage ;
        #region "Printing Properties"
        float linesPerPage = 0;
        float yPos = 0;
        int count = 0;
        float leftMargin ;
        float topMargin;
        int nPages; // number of pages
        int i; // current line number to display on page
        #endregion
        public IErrorsInfo RunReport( ReportType reportType, string outputFile)
        {
            try

            {
              
                reportOutput = new ReportOutput();
                reportOutput.Definition = Definition;
                reportOutput.DMEEditor = DMEEditor;
                if (reportOutput.GetBlockDataIntoTables())
                {
                    //streamToPrint = new StreamReader(outputFile);
                    //streamToPrint.Close();
                    switch (reportType)
                    {
                        case ReportType.html:
                            OutputFile = outputFile;
                            CreateHtmlReport();
                            break;
                        case ReportType.xls:
                            OutputFile = outputFile;
                            break;
                        case ReportType.csv:
                            OutputFile = outputFile;
                            break;
                        case ReportType.pdf:
                            OutputFile = outputFile;
                            break;
                        case ReportType.txt:
                            try
                            {
                                reportView = new uc_GenericReportView();
                                reportView.DMEEditor = DMEEditor;
                                printDocument1 = new PrintDocument();
                                printDocument1 = reportView.printDocument1;
                                printDocument1.PrinterSettings = printerSetting1;
                                reportView.printDocument1 = printDocument1;
                                reportView.Printbutton.Click += Printbutton_Click;
                                reportView.PageSetUpbutton.Click += PageSetUpbutton_Click;
                                printDocument1.PrintPage += new PrintPageEventHandler(this.pd_PrintPage);
                                printDocument1.BeginPrint += PrintDocument1_BeginPrint;
                                printDocument1.EndPrint += PrintDocument1_EndPrint;
                                Form form = new Form();
                                form.Controls.Add(reportView);
                                reportView.Dock = DockStyle.Fill;
                                OutputFile = null;
                                DMEEditor.AddLogMessage("Success", $"Creating Report", DateTime.Now, 0, null, Errors.Ok);
                                form.ShowDialog();
                            }
                            finally
                            {
                               

                            }

                            break;
                        default:
                            break;
                    }
                
                }
                            
            }
            catch (Exception ex)
            {
                string errmsg = "Error Saving Function Mapping ";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        #region "Print Text"
        private void PrintDocument1_EndPrint(object sender, PrintEventArgs e)
        {
           // throw new NotImplementedException();
        }
        private void PrintDocument1_BeginPrint(object sender, PrintEventArgs e)
        {
           // counter = 0;
            curPage = 1;
        }
        private void pd_PrintPage(object sender, PrintPageEventArgs ev)
        {

             printFont = new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel);
            int totalrows = reportOutput.GetTotalNumberofRows();
            leftMargin = ev.MarginBounds.Left;
            topMargin = ev.MarginBounds.Top;
            linesPerPage = ev.MarginBounds.Height / printFont.GetHeight(ev.Graphics);
            nPages = ((int)((totalrows - 1) / linesPerPage + 1));
            // Printing cycle of one page
            i = 0;
            //------------------ Print Header 
            PrintHeader(ev);
            //------------------ Print Blocks

            //------------------ Print Footer
            PrintFooter(ev);
            while (count < linesPerPage && (totalrows - 1 > i))
            {
                // Get line for output from richTextBox1
                //curLine = PrintData.Rows[counter];
                yPos = topMargin + (count *
                   printFont.GetHeight(ev.Graphics));
             

                count = count + reportOutput.Tables[i].Rows.Count;
                i++;
            }
            // If all the text does not fit on 1 page,
            // then you need to add an additional page for printing
            ev.HasMorePages = false;
            // If more lines exist, print another page.
            if (curPage < nPages)
            {
                curPage++;
                ev.HasMorePages = true;
            }
        }
        private void PrintHeader(PrintPageEventArgs ev)
        {
            ev.Graphics.DrawString(Definition.Title.Text, Definition.Title.Font, new SolidBrush(Definition.Title.ForeColor),
              Definition.Title.LocationX, yPos, new StringFormat());
            count++;
            yPos = topMargin + (count * printFont.GetHeight(ev.Graphics));

            ev.Graphics.DrawString(Definition.SubTitle.Text, Definition.SubTitle.Font, new SolidBrush(Definition.SubTitle.ForeColor),
           Definition.SubTitle.LocationX, yPos, new StringFormat());
            count++;
            yPos = topMargin + (count * printFont.GetHeight(ev.Graphics));

            ev.Graphics.DrawString(Definition.Description.Text, Definition.Description.Font, new SolidBrush(Definition.Description.ForeColor),
           Definition.Description.LocationX, yPos, new StringFormat());
            count++;
        }
        private void PrintFooter(PrintPageEventArgs ev)
        {
            yPos = topMargin + (count * printFont.GetHeight(ev.Graphics));

            ev.Graphics.DrawString(Definition.Footer.Text, Definition.Footer.Font, new SolidBrush(Definition.Footer.ForeColor),
          Definition.Footer.LocationX, yPos, new StringFormat());
            count++;
        }
        private SizeF GetStringWidth(string txt, PrintPageEventArgs ev, Font f)
        {
            return ev.Graphics.MeasureString(txt, f);
        }
        #endregion
        #region "Print Html"
        private void CreateHtmlReport()
        {
            FileStream fileStream = new FileStream(OutputFile, FileMode.Create);
            StreamWriter streamWriter1 = new System.IO.StreamWriter(fileStream);
            string bodystring = ($@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <title>{Definition.Title.Text}</title>
    <style>{Definition.CSS}
    </style>
</head>
<body>
");
            bodystring+=($@"
<table style=""height: 28px; width: 100 %; border - style: none; margin - left: auto; margin - right: auto; "" border=""0"" cellspacing=""2"" cellpadding=""2"">
              <tbody>
    <tr style=""height: 110px;"">
 <td style = ""width: 99.4318%; text-align: center; height: 110px;"" colspan = ""5"" >
    <h1> {Definition.Title.Text} </ h1 >
    <h3> &nbsp;</ h3 >
       </td >
       </tr >
       <tr style = ""height: 53px;"" >
        <td style = ""width: 76.5625%; text-align: left; height: 53px;"" colspan = ""4"" >
           <h3> {Definition.SubTitle.Text} </h3>
           </td>
           <td style = ""width: 22.8693%; text-align: right; height: 53px;"" >
            <h3> {DateTime.Now.ToString()} </h3>
            </td>
            </tr>
       </tbody>
       </table> 
<hr />
    <p>&nbsp;</p>
    <table class=""greyGridTable"">
              ");
            
            for (int i = 0; i <= reportOutput.Tables.Count-1; i++)
            {
               
                bodystring += createTableHeader(i);
                
                bodystring += createTableFooter(i);
              
                bodystring += CreateTableBody(i);
             



            }
            bodystring += @"
 </html>";
            streamWriter1.Write(bodystring);
            streamWriter1.Close();
            fileStream.Close();

        }
        private string createTableHeader(int Blockid)
        {
            string retval = @"<thead>
                              <tr>
                              ";
           
            DataTable dt = reportOutput.Tables[i];
            for (int i = 0; i < Definition.Blocks[Blockid].BlockColumns.Count - 1; i++)
            {
                if (Definition.Blocks[Blockid].BlockColumns[i].Show)
                {
                    retval += $@"<th>{Definition.Blocks[Blockid].BlockColumns[i].ColumnName}</th>
                                ";
                  
                }
                   
            }
            retval += @"</tr>
                        </thead>
                        ";
            return retval;
        }
        private string CreateTableBody(int Blockid)
        {
            string retval = @"<tbody>
                            ";
            DataTable dt = reportOutput.Tables[i];
            for (int k = 0; k < dt.Rows.Count - 1; k++)
            {

                retval += CreateDataRow(i, k);
            }
            retval += @"</tbody>
</table>";
                       
            return retval;
        }
        private string createTableFooter(int Blockid)
        {
            string retval = @"<tfoot>
                              <tr>
                               ";
            DataTable dt = reportOutput.Tables[i];
            for (int i = 0; i < Definition.Blocks[Blockid].BlockColumns.Count - 1; i++)
            {
                if (Definition.Blocks[Blockid].BlockColumns[i].Show)
                {
                    retval += @"<td>foot1</td>
                                ";
                }
            }
            retval += @"</tr>
                        </tfoot>
                        ";
            return retval;
        }
        private string CreateDataRow(int Blockid,int Rowid)
        {
            string retval = @"<tr>
                      ";
            for (int i = 0; i < Definition.Blocks[Blockid].BlockColumns.Count-1; i++)
            {
                if (Definition.Blocks[Blockid].BlockColumns[i].Show)
                {
                    retval += $@"<td>{reportOutput.Tables[Blockid].Rows[Rowid][Definition.Blocks[Blockid].BlockColumns[i].ColumnName]}</td>
                                ";     
                }
               
            }
            retval += @"</tr>
                      ";
            return retval;
        }
        #endregion
        #region "button Clicks"
        private void PageSetUpbutton_Click(object sender, EventArgs e)
        {
            if (pageSetupDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                printDocument1.DefaultPageSettings = pageSetupDialog1.PageSettings;
                printDocument1.PrinterSettings = pageSetupDialog1.PrinterSettings;
            }
        }

        private void Printbutton_Click(object sender, EventArgs e)
        {
            printPreviewDialog1 = new PrintPreviewDialog(); // instantiate new print preview dialog  
            printPreviewDialog1.Document = printDocument1;

            printPreviewDialog1.ShowDialog();

            printDocument1.Print();
        }
        #endregion


        // The PrintPage event is raised for each page to be printed.



    }
}
