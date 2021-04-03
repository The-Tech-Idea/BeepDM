using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.DataManagment_Engine.Report;

namespace TheTechIdea.Winforms.VIS.ReportGenrerator
{
    public class GenReportusingPrintForm
    {
        private List<System.Windows.Forms.DataGridView> DataGridViews { get; set; }
        private List<System.Windows.Forms.DataGridViewTextBoxColumn> Columns { get; set; }
      //  private System.Windows.Forms.DataGridViewCheckBoxColumn Column3;
      //  private System.Windows.Forms.DataGridViewComboBoxColumn Column4;
      //  private System.Windows.Forms.DataGridViewImageColumn Column5;
        public DataGridView GridView  { get; set; }
      private ImageList imageListOfMembers=new ImageList();
        public IReportDefinition Report { get; set; }
        public GenReportusingPrintForm()
        {
           

        }

        private void Dv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Dv_DoubleClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Dv_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Dv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex > -1 && e.ColumnIndex > -1)
            {
                
                // This image control use to place over cell with the help of drawImage function.  
                Image imgForGridCell = null;
                // Check the column where we need to place the image.  
                if (GridView.Columns[e.ColumnIndex].GetType()== typeof(DataGridViewImageCell))
                {
                    // Check the data of cell of column ImageName  
                    // On the bases of cell data, we will get the specific image from ImageList control.  
                   
                        // Getting image from ImageList control "imageListOfMembers" and assiging it to image control "imgForGridCell"  
                        imgForGridCell = imageListOfMembers.Images[e.Value.ToString()];
                   
                    if (imgForGridCell != null)
                    {
                        SolidBrush gridBrush = new SolidBrush(GridView.GridColor);
                        Pen gridLinePen = new Pen(gridBrush);
                        SolidBrush backColorBrush = new SolidBrush(e.CellStyle.BackColor);
                        e.Graphics.FillRectangle(backColorBrush, e.CellBounds);
                        // Draw lines over cell  
                        e.Graphics.DrawLine(gridLinePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
                        e.Graphics.DrawLine(gridLinePen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);
                        // Draw the image over cell at specific location.  
                        e.Graphics.DrawImage(imgForGridCell, e.CellBounds.Location);
                        GridView.Rows[e.RowIndex].Cells[e.Value.ToString()].ReadOnly = true; // make cell readonly so below text will not dispaly on double click over cell.  
                    }
                    e.Handled = true;
                }
            }
        }

        public DataGridViewTextBoxColumn CreateTextColumnForGrid( string ColumnName,string HeaderText)
        {

            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            DataGridViewTextBoxColumn Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            Column1.DefaultCellStyle = dataGridViewCellStyle2;
            Column1.HeaderText = HeaderText;
            Column1.MinimumWidth = 8;
            Column1.DataPropertyName = ColumnName;
            Column1.Name = ColumnName;
            return Column1;
        }
        public DataGridViewCheckBoxColumn CreateCheckBoxColumnForGrid(string ColumnName, string HeaderText,object falsevalue,object truevalue,object intervalue,bool threestate )
        {

            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            DataGridViewCheckBoxColumn Column1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            Column1.DefaultCellStyle = dataGridViewCellStyle2;
            Column1.HeaderText = HeaderText;
            Column1.MinimumWidth = 8;
            Column1.Name = ColumnName;
            Column1.FalseValue =falsevalue;
            Column1.DataPropertyName = ColumnName;
            Column1.IndeterminateValue = intervalue;
            Column1.MinimumWidth = 8;
           
            Column1.ThreeState = threestate;
            Column1.TrueValue = truevalue;
            return Column1;
        }
        public DataGridViewComboBoxColumn CreateComoboBoxColumnForGrid(string ColumnName, string HeaderText,List<string> lsitems)
        {

            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            DataGridViewComboBoxColumn Column1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            Column1.DefaultCellStyle = dataGridViewCellStyle2;
            Column1.HeaderText = HeaderText;
            Column1.MinimumWidth = 8;
            Column1.DataPropertyName = ColumnName;
            Column1.Name = ColumnName;
            foreach (var item in lsitems)
            {
                Column1.Items.Add(item);
            }
           
            return Column1;
        }
        public DataGridViewComboBoxColumn CreateComoboBoxColumnForGrid(string ColumnName, string HeaderText, DataTable lsitems,string displayfield,string keyfield)
        {

            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            DataGridViewComboBoxColumn Column1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            Column1.DefaultCellStyle = dataGridViewCellStyle2;
            Column1.HeaderText = HeaderText;
            Column1.MinimumWidth = 8;
            Column1.Name = ColumnName;
            Column1.DataSource = lsitems;
            Column1.DataPropertyName = ColumnName;
            Column1.DisplayMember = displayfield;
            Column1.ValueMember = keyfield;
            return Column1;
        }
        public DataGridViewImageColumn CreateImageColumnForGrid(string ColumnName, string HeaderText,List<Image> lsimages)
        {

            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            DataGridViewImageColumn Column1 = new System.Windows.Forms.DataGridViewImageColumn();
            Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            Column1.DefaultCellStyle = dataGridViewCellStyle2;
            Column1.HeaderText = HeaderText;
            Column1.MinimumWidth = 8;
            Column1.DataPropertyName = ColumnName;
            Column1.Name = ColumnName;
            //Column1.i
            return Column1;
        }
        public DataGridView CreateGrid()
        {
            GridView = new DataGridView();
            GridView.CellPainting += Dv_CellPainting;
            //GridView.DataError += Dv_DataError;
            //GridView.DoubleClick += Dv_DoubleClick;
            //GridView.CellDoubleClick += Dv_CellDoubleClick;
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            GridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            GridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            GridView.BackgroundColor = System.Drawing.Color.White;
            GridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            GridView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            GridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.Gainsboro;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            GridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            GridView.ColumnHeadersHeight = 34;
            //for (int i = 0; i < table.Columns.Count; i++)
            //{

            //}
            return GridView;

        }
    }
}
