using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Printing;
using System.Data;
using System.Collections;


namespace TheTechIdea.DataManagment_Engine.Report
{
	public class DataGridPrinter
	{

		public PrintDocument ThePrintDocument { get; set; }
		public DataTable TheTable { get; set; }
		public IReportBlock ReportBlock { get; set; }
		public int BlockID { get; set; }
		public int RowCount = 0;  // current count of rows;
		private const int kVerticalCellLeeway = 10;
		public int PageNumber = 1;
		public ArrayList Lines = new ArrayList();

		int PageWidth;
		int PageHeight;
		int TopMargin;
		int BottomMargin;
		public DataGridPrinter(int blockid,IReportBlock reportblock, PrintDocument aPrintDocument, DataTable aTable)
		{
			//
			// TODO: Add constructor logic here
			//
			ReportBlock = reportblock;
			ThePrintDocument = aPrintDocument;
			TheTable = aTable;
			BlockID = blockid;

			PageWidth = ThePrintDocument.DefaultPageSettings.PaperSize.Width;
			PageHeight = ThePrintDocument.DefaultPageSettings.PaperSize.Height;
			TopMargin = ThePrintDocument.DefaultPageSettings.Margins.Top;
			BottomMargin = ThePrintDocument.DefaultPageSettings.Margins.Bottom;

		}
		public void DrawHeader(Graphics g)
		{
			SolidBrush ForeBrush = new SolidBrush(ReportBlock.HeaderForeColor);
			SolidBrush BackBrush = new SolidBrush(ReportBlock.HeaderBackColor);
			Pen TheLinePen = new Pen(ReportBlock.GridLineColor, 1);
			StringFormat cellformat = new StringFormat();
			cellformat.Trimming = StringTrimming.EllipsisCharacter;
			cellformat.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.LineLimit;



			int columnwidth = PageWidth / TheTable.Columns.Count;

			int initialRowCount = RowCount;

			// draw the table header
			float startxposition = ReportBlock.LocationX;
			RectangleF nextcellbounds = new RectangleF(0, 0, 0, 0);

			RectangleF HeaderBounds = new RectangleF(0, 0, 0, 0);

			HeaderBounds.X = ReportBlock.LocationX;
			HeaderBounds.Y = ReportBlock.LocationY + TopMargin + (RowCount - initialRowCount) * (ReportBlock.Font.SizeInPoints + kVerticalCellLeeway);
			HeaderBounds.Height = ReportBlock.Font.SizeInPoints + kVerticalCellLeeway;
			HeaderBounds.Width = PageWidth;

			g.FillRectangle(BackBrush, HeaderBounds);

			for (int k = 0; k < TheTable.Columns.Count; k++)
			{
				string nextcolumn = TheTable.Columns[k].ToString();
				RectangleF cellbounds = new RectangleF(startxposition, ReportBlock.LocationY + TopMargin + (RowCount - initialRowCount) * (ReportBlock.Font.SizeInPoints + kVerticalCellLeeway),
					columnwidth,
					ReportBlock.HeaderFont.SizeInPoints + kVerticalCellLeeway);
				nextcellbounds = cellbounds;

				if (startxposition + columnwidth <= PageWidth)
				{
					g.DrawString(nextcolumn, ReportBlock.HeaderFont, ForeBrush, cellbounds, cellformat);
				}

				startxposition = startxposition + columnwidth;

			}

			if (ReportBlock.GridLineStyle != DataGridLineStyle.None)
				g.DrawLine(TheLinePen, ReportBlock.LocationX, nextcellbounds.Bottom, PageWidth, nextcellbounds.Bottom);
		}
		public bool DrawRows(Graphics g)
		{
			int lastRowBottom = TopMargin;

			try
			{
				SolidBrush ForeBrush = new SolidBrush(ReportBlock.ForeColor);
				SolidBrush BackBrush = new SolidBrush(ReportBlock.BackColor);
				SolidBrush AlternatingBackBrush = new SolidBrush(ReportBlock.AlternatingBackColor);
				Pen TheLinePen = new Pen(ReportBlock.GridLineColor, 1);
				StringFormat cellformat = new StringFormat();
				cellformat.Trimming = StringTrimming.EllipsisCharacter;
				cellformat.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.LineLimit;
				int columnwidth = PageWidth / TheTable.Columns.Count;

				int initialRowCount = RowCount;

				RectangleF RowBounds = new RectangleF(0, 0, 0, 0);

				// draw vertical lines




				// draw the rows of the table
				for (int i = initialRowCount; i < TheTable.Rows.Count; i++)
				{
					DataRow dr = TheTable.Rows[i];
					int startxposition = ReportBlock.LocationX;

					RowBounds.X = ReportBlock.LocationX;
					RowBounds.Y = ReportBlock.LocationY + TopMargin + ((RowCount - initialRowCount) + 1) * (ReportBlock.Font.SizeInPoints + kVerticalCellLeeway);
					RowBounds.Height = ReportBlock.Font.SizeInPoints + kVerticalCellLeeway;
					RowBounds.Width = PageWidth;
					Lines.Add(RowBounds.Bottom);

					if (i % 2 == 0)
					{
						g.FillRectangle(BackBrush, RowBounds);
					}
					else
					{
						g.FillRectangle(AlternatingBackBrush, RowBounds);
					}


					for (int j = 0; j < TheTable.Columns.Count; j++)
					{
						RectangleF cellbounds = new RectangleF(startxposition,
							ReportBlock.LocationY + TopMargin + ((RowCount - initialRowCount) + 1) * (ReportBlock.Font.SizeInPoints + kVerticalCellLeeway),
							columnwidth,
							ReportBlock.Font.SizeInPoints + kVerticalCellLeeway);


						if (startxposition + columnwidth <= PageWidth)
						{
							g.DrawString(dr[j].ToString(), ReportBlock.Font, ForeBrush, cellbounds, cellformat);
							lastRowBottom = (int)cellbounds.Bottom;
						}

						startxposition = startxposition + columnwidth;
					}

					RowCount++;

					if (RowCount * (ReportBlock.Font.SizeInPoints + kVerticalCellLeeway) > (PageHeight * PageNumber) - (BottomMargin + TopMargin))
					{
						DrawHorizontalLines(g, Lines);
						DrawVerticalGridLines(g, TheLinePen, columnwidth, lastRowBottom);
						return true;
					}


				}

				DrawHorizontalLines(g, Lines);
				DrawVerticalGridLines(g, TheLinePen, columnwidth, lastRowBottom);
				return false;

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message.ToString());
				return false;
			}

		}
		void DrawHorizontalLines(Graphics g, ArrayList lines)
		{
			Pen TheLinePen = new Pen(ReportBlock.GridLineColor, 1);

			if (ReportBlock.GridLineStyle == DataGridLineStyle.None)
				return;

			for (int i = 0; i < lines.Count; i++)
			{
				g.DrawLine(TheLinePen, ReportBlock.LocationX, (float)lines[i], PageWidth, (float)lines[i]);
			}
		}
		void DrawVerticalGridLines(Graphics g, Pen TheLinePen, int columnwidth, int bottom)
		{
			if (ReportBlock.GridLineStyle == DataGridLineStyle.None)
				return;

			for (int k = 0; k < TheTable.Columns.Count; k++)
			{
				g.DrawLine(TheLinePen, ReportBlock.LocationX + k * columnwidth,
					ReportBlock.LocationY + TopMargin,
					ReportBlock.LocationX + k * columnwidth,
					bottom);
			}
		}
		public bool DrawDataGrid(Graphics g)
		{

			try
			{
				DrawHeader(g);
				bool bContinue = DrawRows(g);
				return bContinue;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message.ToString());
				return false;
			}

		}

	}
}
