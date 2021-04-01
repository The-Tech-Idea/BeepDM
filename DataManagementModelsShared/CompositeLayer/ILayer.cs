using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.CompositeLayer
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
        string  ID { get; set; }
      

    }
}
