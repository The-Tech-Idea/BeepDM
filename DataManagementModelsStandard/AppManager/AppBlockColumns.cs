using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.AppManager
{
    public interface IAppBlockColumns
    {
         string ID { get; set; }
         string GuidID { get; set; } 
        string ColumnName { get; set; }
        int ColumnSeq { get; set; }
        string DisplayName { get; set; }
        int FieldDisplaySeq { get; set; }
        bool Show { get; set; }
         int LocationY { get; set; }
         int LocationX { get; set; }
        
         Color ForeColor { get; set; }
         Color BackColor { get; set; }
         Color AlternatingBackColor { get; set; }
         Color BorderLineColor { get; set; }
    }

    public class AppBlockColumns : IAppBlockColumns
    {
        public AppBlockColumns()
        {

        }
        public string ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string ColumnName { get; set; }
        public string DisplayName { get; set; }
        public int ColumnSeq { get; set; }
        public int FieldDisplaySeq { get; set; }
        public bool Show { get; set; } = true;
        public int LocationY { get; set; } = 0;
        public int LocationX { get; set; } = 0;
       
        public Color ForeColor { get; set; } = Color.Black;
        public Color BackColor { get; set; } = Color.Transparent;
        public Color AlternatingBackColor { get; set; } = Color.Transparent;
        public Color BorderLineColor { get; set; } = Color.Transparent;
    }
}
