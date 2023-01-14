using TheTechIdea.Beep;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataBase
{
    public class DMDataView : IDMDataView
    {
        public List<EntityStructure> Entities { get; set; }=new List<EntityStructure>();
        public string ViewName { get; set; }
        public int ViewID { get; set; }
        public ViewType Viewtype { get; set; }
        public bool     Editable { get; set; }
        public string EntityDataSourceID { get; set; }
        public string DataViewDataSourceID { get ; set ; }
        public string CompositeLayerDataSourceID { get; set; }
        public string VID { get ; set ; }

        public DMDataView()
        {
           
        }
        public DMDataView(string pTableName)
        {
            ViewName = pTableName;
            Viewtype =ViewType.Table;

            VID = Guid.NewGuid().ToString();
           
           

        }
        public DMDataView( string pTableName, ViewType viewtype)
        {
            ViewName = pTableName;
            Viewtype = viewtype;
            VID = Guid.NewGuid().ToString();


        }
    }
}
