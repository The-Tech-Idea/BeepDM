using System.Collections.Generic;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public interface IDMDataView
    {
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