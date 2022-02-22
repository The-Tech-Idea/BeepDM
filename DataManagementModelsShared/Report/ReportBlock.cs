using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Report
{
    public interface IReportBlock
    {
        string Title { get; set; }
        List<ReportBlockColumns> BlockColumns { get; set; }
        List<ReportFilter> filters { get; set; }
        string EntityID { get; set; }
        string ViewID { get; set; }

        int LocationY { get; set; }
        int LocationX { get; set; }

        Color ForeColor { get; set; }
        Color HeaderForeColor { get; set; }
        Color HeaderBackColor { get; set; }

        Color BackColor { get; set; }
        Color AlternatingBackColor { get; set; }
        Color GridLineColor { get; set; }
        DataGridLineStyle GridLineStyle { get; set; }
        ReportBlockViewType ViewType { get; set; }
        List<EntityField> Fields { get; set; }
        List<EntityParameters> Paramenters { get; set; }
        List<RelationShipKeys> Relations { get; set; }
    }

    public class ReportBlock : IReportBlock
    {
        public string Title { get; set; }
        public string EntityID { get; set; }
        public string ViewID { get; set; }
        public ReportBlockViewType ViewType { get; set; }
        public int LocationY { get; set; } = 0;
        public int LocationX { get; set; } = 0;
      
        public Color ForeColor { get; set; } = Color.Black;
        public Color BackColor { get; set; }
        public Color AlternatingBackColor { get; set; }
        public Color GridLineColor { get; set; }
        public  DataGridLineStyle GridLineStyle { get; set; }
        public List<ReportFilter> filters { get; set; } = new List<ReportFilter>();
        public List<ReportBlockColumns> BlockColumns { get; set; } = new List<ReportBlockColumns>();
        public List<EntityField> Fields { get; set; }=new List<EntityField>();
        public List<EntityParameters> Paramenters { get; set; }=new List<EntityParameters>();
        public List<RelationShipKeys> Relations { get; set; }=new List<RelationShipKeys>();
        public Color HeaderForeColor { get; set; }
        public Color HeaderBackColor { get; set; }
      

        public ReportBlock()
        {

        }
    }
    public enum ReportBlockViewType
    {
        Table, Details, Graph
    }
    public class TextBlock
    {
        public TextBlock()
        {

        }
        public string Text { get; set; }
        public int LocationY { get; set; } = 0;
        public int LocationX { get; set; } = 0;
       
        public Color ForeColor { get; set; } = Color.Black;
        public Color BackColor { get; set; } = Color.Transparent;
        public Color AlternatingBackColor { get; set; } = Color.Transparent;
        public Color LineColor { get; set; } = Color.Black;

    }
    public enum DataGridLineStyle
    {
        //
        // Summary:
        //     No gridlines between cells.
        None = 0,
        //
        // Summary:
        //     Solid gridlines between cells.
        Solid = 1
    }
}
