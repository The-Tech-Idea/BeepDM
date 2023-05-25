using System;
using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.DataBase
{
    public interface IDMDataView
    {
         int ID { get; set; }
         string GuidID { get; set; } 
        List<EntityStructure> Entities { get; set; }
        string ViewName { get; set; }
        int ViewID { get; set; }
        string VID { get; set; }
        ViewType Viewtype { get; set; }
        bool Editable { get; set; }
        string EntityDataSourceID { get; set; }
        string DataViewDataSourceID { get; set; }
        string CompositeLayerDataSourceID { get; set; }
        


    }
}