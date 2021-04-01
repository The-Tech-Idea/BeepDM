using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.Report
{
    public interface IReportBlockColumns
    {
        string ColumnName { get; set; }
        int ColumnSeq { get; set; }
        string DisplayName { get; set; }
        int FieldDisplaySeq { get; set; }
        bool Show { get; set; }
         int LocationY { get; set; }
         int LocationX { get; set; }
         Font Font { get; set; }
         Color ForeColor { get; set; }
         Color BackColor { get; set; }
         Color AlternatingBackColor { get; set; }
         Color BorderLineColor { get; set; }
    }

    public class ReportBlockColumns : IReportBlockColumns
    {
        public ReportBlockColumns()
        {

        }
        public string ColumnName { get; set; }
        public string DisplayName { get; set; }
        public int ColumnSeq { get; set; }
        public int FieldDisplaySeq { get; set; }
        public bool Show { get; set; } = true;
        public int LocationY { get; set; } = 0;
        public int LocationX { get; set; } = 0;
        public Font Font { get; set; } = new Font("Times New Roman", 12.0f);
        public Color ForeColor { get; set; } = Color.Black;
        public Color BackColor { get; set; } = Color.Transparent;
        public Color AlternatingBackColor { get; set; } = Color.Transparent;
        public Color BorderLineColor { get; set; } = Color.Transparent;
    }
}
