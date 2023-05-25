using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.CompositeLayer
{
    public interface ILayer
    {
        List<EntityStructure> Entities { get; set; }
        IDataSource DataSource { get; set; }
        string DataViewDataSourceName { get; set; }
        string DataSourceName { get; set; }
        string LocalDBDriver { get; set; }
        string LocalDBDriverVersion { get; set; }
        DateTime DateCreated { get; set; }
        DateTime DateUpdated { get; set; }
        string  LayerName { get; set; }
        int  ID { get; set; }
        string GuidID { get; set; }

    }
}
