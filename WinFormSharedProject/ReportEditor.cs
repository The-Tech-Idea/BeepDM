using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.DataManagment_Engine;

namespace TheTechIdea.DataManagment_Engine.Report
{
    public class ReportEditor
    {
        public Font printFont {get;set;}= new Font("Arial", 10);
        public PrintDocument pd { get; set; } 
        public PrintPreviewControl PPreviewControl { get; set; }
        public PrinterSettings PrinterSetting { get; set; }
        public PageSettings PageSetting { get; set; }
        public IEnumerable<PaperSize> paperSizes { get; set; } 
        public PrintPreviewDialog PreviewDialog { get; set; }
        public Control PrintLayout { get; set; }

        public IDMEEditor DMEEditor { get; set; }
        public ReportEditor()
        {
            PPreviewControl = new PrintPreviewControl();
           
            PreviewDialog = new PrintPreviewDialog();
            pd =new PrintDocument();
            pd.Print();
            PPreviewControl.Document = pd;
            pd.PrintPage += Pd_PrintPage;
        
            paperSizes = PrinterSetting.PaperSizes.Cast<PaperSize>();
            pd.DefaultPageSettings.PaperSize = GetPaperSize(PaperKind.A4);
          
            PrinterSetting = pd.PrinterSettings;
            PageSetting = pd.DefaultPageSettings;
        }

       

        public PaperSize GetPaperSize(PaperKind pk)
        {
            return  paperSizes.First<PaperSize>(size => size.Kind == pk); // setting paper size to A4 size
        }
        private void Pd_PrintPage(object sender, PrintPageEventArgs ev)
        {
            float linesPerPage = 0;
           // float yPos = 0;
          //  int count = 0;
            float leftMargin = ev.MarginBounds.Left;
            float topMargin = ev.MarginBounds.Top;
            string line = null;

            // Calculate the number of lines per page.
            linesPerPage = ev.MarginBounds.Height /
               printFont.GetHeight(ev.Graphics);

            // Print each line of the file.
            //while (count < linesPerPage && ((line = streamToPrint.ReadLine()) != null))
            //{
            //    yPos = topMargin + (count * printFont.GetHeight(ev.Graphics));
            //    ev.Graphics.DrawString(line, printFont, Brushes.Black,leftMargin, yPos, new StringFormat());
            //    count++;
            //}
            //---------------------------------------


            // If more lines exist, print another page.
           
            if (line != null)
                ev.HasMorePages = true;
            else
                ev.HasMorePages = false;

        }
        IErrorsInfo PrintView()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception)
            {

                throw;
            }
            return DMEEditor.ErrorObject;
        }
        //private void DrawForm(Graphics g, int resX, int resY)
        // {
        //     g.FillRectangle(new SolidBrush(this.BackColor), 0, 0, this.Width, this.Height);
        //     float scale = resX / ScreenResolution;
        //     // Cycle through each control on the form and paint it to the printe
        //     foreach (Control c in Controls)
        //     {
        //         // Get the time of the next control so we can unbox it
        //         string strType = c.GetType().ToString().Substring(c.GetType().ToString().LastIndexOf(".") + 1);
        //         switch (strType)
        //         {
        //             case "Button":
        //                 Button b = (Button)c;
        //                 // Use the ControlPaint method DrawButton in order to draw the button of the form
        //                 ControlPaint.DrawButton(g, ((Button)c).Left, ((Button)c).Top, ((Button)c).Width, ((Button)c).Height, ButtonState.Normal);
        //                 // We also need to draw the text
        //                 g.DrawString(b.Text, b.Font, new SolidBrush(b.ForeColor), b.Left + b.Width / 2 - g.MeasureString(b.Text,
        //                 b.Font).Width / 2, b.Top + b.Height / 2 - g.MeasureString("a", b.Font).Height / 2, new StringFormat());
        //                 break;
        //             case "TextBox":
        //                 TextBox t = (TextBox)c;
        //                 // Draw a text box by drawing a pushed in button and filling the rectangle with the background color and the text
        //                 // of the TextBox control
        //                 // First the sunken border
        //                 ControlPaint.DrawButton(g, t.Left, t.Top, t.Width, t.Height, ButtonState.Pushed);
        //                 // Then fill it with the background of the textbox
        //                 g.FillRectangle(new SolidBrush(t.BackColor), t.Left + 1, t.Top + 1, t.Width + 2, t.Height - 2);
        //                 // Finally draw the string inside
        //                 g.DrawString(t.Text, t.Font, new SolidBrush(t.ForeColor), t.Left + 2, t.Top + t.Height / 2 - g.MeasureString("a", t.Font).Height / 2, new StringFormat());
        //                 break;
        //             case "CheckBox":// We have a checkbox to paint, unbox it
        //                 CheckBox cb = (CheckBox)c;
        //                 // Use the DrawCheckBox command to draw a checkbox and pass the button state to paint it checked or unchecked
        //                 if (cb.Checked)
        //                     ControlPaint.DrawCheckBox(g, cb.Left, cb.Top, cb.Height / 2, cb.Height / 2, ButtonState.Checked);
        //                 else
        //                     ControlPaint.DrawCheckBox(g, cb.Left, cb.Top, cb.Height / 2, cb.Height / 2, ButtonState.Normal);
        //                 // Don't forget the checkbox text
        //                 g.DrawString(cb.Text, cb.Font, new SolidBrush(cb.ForeColor), cb.Right - cb.Height - g.MeasureString(cb.Text, cb.Font).Width, cb.Top, new StringFormat());
        //                 break;
        //         }
        //     }
        // }
//        public bool DrawRows(Graphics g,DataGrid TheDataGrid,DataTable TheTable)
//        {
//            try
//            {
//                int lastRowBottom = TopMargin;
//                // Create an array to save the horizontal positions for drawing horizontal gridlines  
//                ArrayList Lines = new ArrayList();
//                // form brushes based on the color properties of the DataGrid  
//                // These brushes will be used to draw the grid borders and cells  
//                SolidBrush ForeBrush = new SolidBrush(TheDataGrid.ForeColor);
//                SolidBrush BackBrush = new SolidBrush(TheDataGrid.BackColor);
//                SolidBrush AlternatingBackBrush = new SolidBrush(TheDataGrid.AlternatingBackColor);
//                Pen TheLinePen = new Pen(TheDataGrid.GridLineColor, 1);
//                // Create a format for the cell so that the string in the cell is cut off at the end of  
//                //the column width
//                StringFormat cellformat = new StringFormat();
//                cellformat.Trimming = StringTrimming.EllipsisCharacter;
//                cellformat.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.LineLimit;
//                // calculate the column width based on the width of the printed page and the # of  
//               // columns in the DataTable
//                // Note: Column Widths can be made variable in a future program by playing with the GridColumnStyles of the  
//                // DataGrid  
//                int columnwidth = PageWidth / TheTable.Columns.Count;
//                // set the initial row count, this will start at 0 for the first page, and be a different  
//                //value for the 2nd, 3rd, 4th, etc.
//                // pages.  
//                int initialRowCount = RowCount;
//                RectangleF RowBounds = new RectangleF(0, 0, 0, 0);
//                // draw the rows of the table   
//                for (int i = initialRowCount; i < TheTable.Rows.Count; i++)
//                {
//                    // get the next DataRow in the DataTable  
//                    DataRow dr = TheTable.Rows[i];
//                    int startxposition = TheDataGrid.Location.X;
//                    // Calculate the row boundary based on teh RowCount and offsets into the page  
//                    RowBounds.X = TheDataGrid.Location.X; RowBounds.Y = TheDataGrid.Location.Y +
//                     TopMargin + ((RowCount - initialRowCount) + 1) * (TheDataGrid.Font.SizeInPoints +
//                     kVerticalCellLeeway);
//                    RowBounds.Height = TheDataGrid.Font.SizeInPoints + kVerticalCellLeeway;
//                    RowBounds.Width = PageWidth;
//                    // save the vertical row positions for drawing grid lines  
//                    Lines.Add(RowBounds.Bottom);
//                    // paint rows differently for alternate row colors  
//                    if (i % 2 == 0)
//                    {
//                        g.FillRectangle(BackBrush, RowBounds);
//                    }
//                    else
//                    {
//                        g.FillRectangle(AlternatingBackBrush, RowBounds);
//                    }
//                    // Go through each column in the row and draw the information from the  
//                    DataRowfor(int j = 0; j < TheTable.Columns.Count; j++)  
//                {
//                    RectangleF cellbounds = new RectangleF(startxposition,
//                    TheDataGrid.Location.Y + TopMargin + ((RowCount - initialRowCount) + 1) *
//                    (TheDataGrid.Font.SizeInPoints + kVerticalCellLeeway),
//                    columnwidth,
//                    TheDataGrid.Font.SizeInPoints + kVerticalCellLeeway);
//                    // draw the data at the next position in the row  
//                    if (startxposition + columnwidth <= PageWidth)
//                    {
//                        g.DrawString(dr[j].ToString(), TheDataGrid.Font, ForeBrush, cellbounds, cellformat);
//                        lastRowBottom = (int)cellbounds.Bottom;
//                    }
//                    // increment the column position  
//                    startxposition = startxposition + columnwidth;
//                }
//                RowCount++;
//                // when we've reached the bottom of the page, draw the horizontal and vertical grid lines and return true  
//                if (RowCount * (TheDataGrid.Font.SizeInPoints + kVerticalCellLeeway) >
//                PageHeight * PageNumber) -(BottomMargin + TopMargin))  
//            {
//                    DrawHorizontalLines(g, Lines); DrawVerticalGridLines(g, TheLinePen, columnwidth,
//                     lastRowBottom);
//                    return true;
//                }
//            }
//// when we've reached the end of the table, draw the horizontal and vertical gridlines and return false  
//            DrawHorizontalLines(g, Lines);
//            DrawVerticalGridLines(g, TheLinePen, columnwidth, lastRowBottom);
//            return false;
//        }
    }
}
