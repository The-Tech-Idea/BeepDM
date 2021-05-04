using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Report
{
    public class PDFSharpReport : IReportDMWriter
    {
        public Document document { get; set; }
        public ReportOutput reportOutput { get; set; }
        public List<Table> tables { get; set; } = new List<Table>();
        private Section section;
        private TextFrame headerFrame;
        public IReportDefinition Definition { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public bool Html { get ; set ; }
        public bool Text { get ; set ; }
        public bool Csv { get ; set ; }
        public bool PDF { get; set; } = true;
        public bool Excel { get ; set ; }
      //  private string mOutputFile;
        public string OutputFile { get; set; }
        public IErrorsInfo RunReport(ReportType reportType, string outputFile)
        {
            try

            {
                reportOutput = new ReportOutput();
                reportOutput.Definition = Definition;
                reportOutput.DMEEditor = DMEEditor;
               
                if (reportOutput.GetBlockDataIntoTables())
                {
                    CreateDocument();
                    PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(true);
                    pdfRenderer.Document = document;
                    pdfRenderer.RenderDocument();
                  
                    pdfRenderer.PdfDocument.Save(outputFile);
                    
                    OutputFile = outputFile;
                }


            }
            catch (Exception ex)
            {
                string errmsg = "Error Saving Function Mapping ";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public Document CreateDocument()
        {
            // Create a new MigraDoc document
            this.document = new Document();
            this.document.Info.Title = Definition.Title.Text;
            this.document.Info.Subject = Definition.SubTitle.Text;
            this.document.Info.Author = "ME";

            DefineStyles();
            CreateSection();
            CreateHeader();
            for (int i = 0; i <= reportOutput.Tables.Count-1; i++)
            {
                CreatBody(i);
            }
           
            CreateFooter();


            return this.document;
        }
        private void CreateSection()
        {
             section = this.document.AddSection();
        }
        private Table CreatBody(int id)
        {
            DataTable tb=reportOutput.Tables[id];
            Table table = new Table();
            table = section.AddTable();
            table.Style = "Table";
            table.Borders.Color = new Color((uint)Definition.Blocks[id].GridLineColor.ToArgb()) ;
            table.Borders.Width = 0.25;
            table.Borders.Left.Width = 0.5;
            table.Borders.Right.Width = 0.5;
            table.Rows.LeftIndent = 0;
            //--------------- Add Columns -------------
            // Before you can add a row, you must define the columns
           
            for (int i = 0; i <= reportOutput.Tables[id].Columns.Count-1; i++)
            {
                Column column = table.AddColumn("3cm");
                column.Format.Alignment = ParagraphAlignment.Center;
            }
            //------------- Create Table Header -------
            
            Row row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.Format.Font.Bold = true;
            row.Shading.Color = new Color((uint)Definition.Blocks[id].BackColor.ToArgb()); ;
            for (int i = 0; i <= reportOutput.Tables[id].Columns.Count-1; i++)
            {
                row.Cells[i].AddParagraph(reportOutput.Tables[id].Columns[i].ColumnName);
                row.Cells[i].Format.Alignment = ParagraphAlignment.Center;
            }
         //   table.SetEdge(0, 0, 6, 2, Edge.Box, BorderStyle.Single, 0.75, Color.Definition.Blocks[id].);
            row = table.AddRow();
            for (int i = 0; i <= reportOutput.Tables[id].Rows.Count-1; i++)
            {
                DataRow dt = reportOutput.Tables[id].Rows[i];
                for (int k = 0; k <= reportOutput.Tables[id].Columns.Count-1; k++)
                {

                    row.Cells[k].AddParagraph(dt[reportOutput.Tables[id].Columns[k].ColumnName].ToString());
                    row.Cells[k].Format.Alignment = ParagraphAlignment.Center;
                }
               
            }
            row = table.AddRow();

            row.Borders.Visible = false;
            return table;
        }
        private void CreateFooter()
        {
            // Create footer
            if (!string.IsNullOrEmpty(Definition.Footer.Text))
            {
                Paragraph paragraph = section.Footers.Primary.AddParagraph();
                paragraph.AddText(Definition.Footer.Text);
                paragraph.Format.Font.Size = Definition.Footer.Font.Size;
                paragraph.Format.Alignment = ParagraphAlignment.Center;

            }
           
        }
        private void CreateHeader()
        {
            // Create the text frame for the address
            headerFrame = section.AddTextFrame();
            headerFrame.Height = "1.0cm";
            headerFrame.Width = "7.0cm";
            headerFrame.Left = ShapePosition.Left;
            headerFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            headerFrame.Top = "1.0cm";
            headerFrame.RelativeVertical = RelativeVertical.Page;

            // Put sender in address frame
            Paragraph paragraph = headerFrame.AddParagraph(Definition.Title.Text);
            paragraph.Style = "Reference";
            paragraph.Format.Font.Name = "Times New Roman";
            paragraph.Format.Font.Size = 7;
            paragraph.Format.SpaceAfter = 3;

            // Add the print date field
            paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = "1cm";
            paragraph.Style = "Reference";
            paragraph.AddFormattedText(Definition.SubTitle.Text, TextFormat.Bold);
            paragraph.AddTab();
            paragraph.AddText("Beep Reports, ");
            paragraph.AddDateField("dd.MM.yyyy");
        }
        void DefineStyles()
        {
            // Get the predefined style Normal.
            Style style = this.document.Styles["Normal"];
            // Because all styles are derived from Normal, the next line changes the 
            // font of the whole document. Or, more exactly, it changes the font of
            // all styles and paragraphs that do not redefine the font.
            style.Font.Name = "Verdana";

            style = this.document.Styles[StyleNames.Header];
            style.ParagraphFormat.AddTabStop("16cm", TabAlignment.Right);

            style = this.document.Styles[StyleNames.Footer];
            style.ParagraphFormat.AddTabStop("8cm", TabAlignment.Center);

            // Create a new style called Table based on style Normal
            style = this.document.Styles.AddStyle("Table", "Normal");
            style.Font.Name = "Verdana";
            style.Font.Name = "Times New Roman";
            style.Font.Size = 9;

            // Create a new style called Reference based on style Normal
            style = this.document.Styles.AddStyle("Reference", "Normal");
            style.ParagraphFormat.SpaceBefore = "5mm";
            style.ParagraphFormat.SpaceAfter = "5mm";
            style.ParagraphFormat.TabStops.AddTabStop("16cm", TabAlignment.Right);
        }
    
     
    }
}
